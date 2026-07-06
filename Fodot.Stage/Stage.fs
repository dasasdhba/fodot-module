namespace Fodot.Stage

open System.Threading.Tasks
open FSharp.Threading
open Fodot
open Godot

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
     | Exiting

[<FScript("stage")>]
type Stage(node : Control) =
     static let viewportPath = new NodePath "%Viewport"
     static let cutscenePath = new NodePath "%Cutscene"
     
     let sceneCleared = Event<unit>()
     let sceneReady = Event<unit>()
     let sceneExited = Event<unit>()
     
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
     member val SceneExited = sceneExited.Publish
     
     member private this.ClearScene () = task {
          match this.CurrentScene with
          | Some n ->
               n.QueueFree ()
               if n.IsInsideTree () then
                    do! n.ToSignalTreeExited ()
               sceneCleared.Trigger ()
          | None -> ()
     }
     
     member private this.ExitScene() = task {
          do! this.ClearScene ()
          
          this.CurrentScene <- None
          this.Status <- Pending
          sceneExited.Trigger ()
          Logger.push $"Stage {this.Root.GetPath()} has exited."
     }
     
     member private this.ChangeScene (scene : Node, ?middleTask: unit -> Task<unit>)= task {
          do! this.ClearScene ()
          match middleTask with
          | Some task -> do! task()
          | _ -> ()
          
          this.CurrentScene <- Some scene
          this.Viewport |> Node.addChild scene
          if scene.IsNodeReady() |> not then
               do! scene.ToSignalReady ()
         
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
          Task.run (fun () ->
               GD.loadAs<PackedScene> path |> PackedScene.instantiate
          )
     
     // queued change scene
     
     member this.IsQueueReady() =
          this.Status = Ready || this.Status = Pending
          
     member this.QueueChange (path : string, ?cutscene : CutsceneConfig, ?middleTask: unit -> Task<unit>) = task {
          if this.IsQueueReady() |> not then
               Logger.pushWarn $"Stage {this.Root.GetPath()} has been queued for changing scene before, changing task to {path} will be cancelled."
               ()
          else
               this.Status <- Loading
               let loading = this.LoadScene path
               let changing () = task {
                    let! scene = loading
                    do! this.ChangeScene(scene, ?middleTask = middleTask)
               }
               let cutscene = defaultArg cutscene CutsceneConfig.None
               do! this.FadeInOut(cutscene, changing)
     }
     
     member this.QueueReload (?cutscene, ?middleTask) =
          this.QueueChange("", ?cutscene = cutscene, ?middleTask = middleTask)

     member this.QueueExit (?cutscene : CutsceneConfig)= task {
          if this.IsQueueReady() |> not then
               Logger.pushWarn $"Stage {this.Root.GetPath()} has been queued for changing scene before, exiting task will be cancelled."
               ()
          else
               this.Status <- Exiting
               let cutscene = defaultArg cutscene CutsceneConfig.None
               do! this.FadeInOut(cutscene, this.ExitScene)
     }
