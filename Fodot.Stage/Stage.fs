namespace Fodot.Stage

open System.Threading.Tasks
open Fodot.Core
open Godot
open Fodot.Common

module Stage =
     
     let asRelativePath (path : string) (current : string)=
          match path with
          
          | s when s.StartsWith('@') ->
               let body = s[1..]
               
               let dir = current.GetBaseDir ()
               let name = current.GetFile().GetBaseName ()
               let ext = current.GetExtension ()
               
               let idx = name.LastIndexOf '_'
               let name = if idx < 0 then name else name[..(idx - 1)]
               
               dir + "/" + name + "_" + body + ext
          
          | "" -> current
          | s -> s

type StageStatus =
     | Pending
     | Loading
     | Ready

[<FScript("stage")>]
type Stage(node : Control) =
     static let viewportPath = new NodePath "%Viewport"
     static let cutscenePath = new NodePath "%Cutscene"
     
     let sceneCleared = Event<unit>()
     let sceneReady = Event<unit>()
     
     member val Root = node
     member val Viewport : Node =
          node
          |> Node.tryGetNode viewportPath
          |> Option.defaultValue node
     member val CutsceneRoot : Node =
          node
          |> Node.tryGetNode cutscenePath
          |> Option.defaultValue node
     
     member val Status = Pending with get, set
     member val CurrentScene : Node option = None with get, set
     member this.CurrentScenePath
          with get () =
               match this.CurrentScene with
               | Some n -> n.SceneFilePath
               | None -> ""
     
     // scene
     
     member val SceneCleared = sceneCleared.Publish
     member val SceneReady = sceneReady.Publish
     
     member this.ClearScene () = task {
          match this.CurrentScene with
          | Some n ->
               n.QueueFree ()
               if n.IsInsideTree () then
                    do! n.ToSignalTreeExited ()
               sceneCleared.Trigger ()
          | None -> ()
     }
     
     member this.ChangeScene (scene : Node, ?middleTask: unit -> Task<unit>)= task {
          do! this.ClearScene ()
          match middleTask with
          | Some task -> do! task()
          | _ -> ()
          
          this.CurrentScene <- Some scene
          this.Viewport |> Node.addChild scene
          if scene.IsNodeReady() |> not then
               do! scene.ToSignalReady ()
         
          Engine.treeUpdateCache ()
          this.Status <- Ready
          sceneReady.Trigger ()
          Logger.push $"Stage {this.Root.GetPath()} is now at {this.CurrentScenePath}"
     }
     
     // cutscene
     
     member private this.initCutscene (cutscene : ICutscene)= task {
          let node = cutscene.Root
          node.SetPosition Vector2.Zero
          cutscene.SetSize this.Root.Size
          
          if node.GetParent() <> this.CutsceneRoot then
               let waiting = node.ToSignalTreeEntered ()
               if node.IsInsideTree() |> not then
                    this.CutsceneRoot |> Node.addChild node
               else
                    node |> Node.reparentDirectly this.CutsceneRoot
               do! waiting
     }
     
     member this.FadeIn (cutscene : ICutscene)=
          cutscene.FadeInInit ()
          task {
               do! this.initCutscene cutscene
               do! cutscene.FadeIn ()
          }
          
     member this.FadeOut (cutscene : ICutscene) =
          cutscene.FadeOutInit ()
          task {
               do! this.initCutscene cutscene
               do! cutscene.FadeOut ()
          }
          
     member this.FadeInOut (cutscene : CutsceneConfig, ?middleTask: unit -> Task<unit>) = task {
          match cutscene.In with
          | Some i -> do! this.FadeIn i
          | None -> ()
          
          match middleTask with
          | Some task -> do! task ()
          | None -> ()
          
          match cutscene.Out with
          | Some o ->
               o.FadeOutInit ()
               do! this.initCutscene o
               
               match cutscene.In with
               | Some i when i <> o -> i.Root.Hide()
               | _ -> ()
               
               do! o.FadeOut ()
               o.Root.QueueFree ()
          | None -> ()
          
          cutscene.In
          |> Option.map (fun i -> i.Root.QueueFree ())
          |> ignore
     }
     
     member this.LoadScene (path : string) =
          let path = this.CurrentScenePath |> Stage.asRelativePath path
          Task.Run (fun () ->
               GD.loadAs<PackedScene> path |> PackedScene.instantiate
          )
     
     // queued change scene
     
     member private this.fadeInOutAndChangeSceneWith (path : string, ?cutscene : CutsceneConfig, ?middleTask: unit -> Task<unit>) = 
          this.Status <- Loading
          let loading = this.LoadScene path
          let changing () = task {
               let! scene = loading
               do! this.ChangeScene(scene, ?middleTask = middleTask)
          }
          let cutscene = defaultArg cutscene CutsceneConfig.None
          this.FadeInOut(cutscene, changing)
          
     member this.QueueChangeScene (path : string, ?cutscene : CutsceneConfig, ?middleTask: unit -> Task<unit>) = task {
          if this.Status = Loading then
               Logger.pushWarn $"Stage {this.Root.GetPath()} has been queued for changing scene before, changing task to {path} will be cancelled."
               ()
          else
               do! this.fadeInOutAndChangeSceneWith (path, ?cutscene = cutscene, ?middleTask = middleTask)
     }
     
     member this.QueueReload (?cutscene, ?middleTask) =
          this.QueueChangeScene("", ?cutscene = cutscene, ?middleTask = middleTask)

     member this.QueueExit (?cutscene : CutsceneConfig)= task {
          if this.Status = Loading then
               Logger.pushWarn $"Stage {this.Root.GetPath()} has been queued for changing scene before, exiting task will be cancelled."
               ()
          else
               let exit () = task {
                    do! this.ClearScene ()
                    this.Status <- Pending
               }
               let cutscene = defaultArg cutscene CutsceneConfig.None
               do! this.FadeInOut(cutscene, exit)
     }