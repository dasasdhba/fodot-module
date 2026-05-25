namespace Fodot.Stage

open System.Threading.Tasks
open Fodot.Async
open Fodot.Core
open Godot
open Fodot.Common

type StageStatus =
     | Pending
     | Loading
     | Ready

[<FScript("stage")>]
type Stage(node : Control) =
     static let viewportPath = new NodePath "%Viewport"
     static let cutscenePath = new NodePath "%Cutscene"
     
     member this.Root = node
     member this.Viewport : Node =
          node
          |> Node.tryGetNode viewportPath
          |> Option.defaultValue node
     member this.CutsceneRoot : Node =
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

module Stage =
     
     // cutscene
     
     let private initCutscene (cutscene : ICutscene) (stage : Stage) = task {
          let node = cutscene.Root
          node.SetPosition Vector2.Zero
          cutscene.SetSize stage.Root.Size
          
          if node.GetParent() <> stage.CutsceneRoot then
               let waiting = node.ToSignalTreeEntered ()
               if node.IsInsideTree() |> not then
                    stage.CutsceneRoot |> Node.addChild node
               else
                    node |> Node.reparentDirectly stage.CutsceneRoot
               do! waiting
     }
     
     let fadeIn (cutscene : ICutscene) (stage : Stage) =
          cutscene.FadeInInit ()
          task {
               do! stage |> initCutscene cutscene
               do! cutscene.FadeIn ()
          }
          
     let fadeOut (cutscene : ICutscene) (stage : Stage) =
          cutscene.FadeOutInit ()
          task {
               do! stage |> initCutscene cutscene
               do! cutscene.FadeOut ()
          }
          
     let fadeInOutWith (middleTask: unit -> Task<unit>) (cutscene : CutsceneConfig) (stage : Stage) = task {
          match cutscene.In with
          | Some i -> do! stage |> fadeIn i
          | None -> ()
          
          do! middleTask ()
          
          match cutscene.Out with
          | Some o ->
               o.FadeOutInit ()
               do! stage |> initCutscene o
               
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
     
     let fadeInOut (cutscene : CutsceneConfig) (stage : Stage) =
          stage |> fadeInOutWith (fun () -> task {}) cutscene
     
     // load scene
     
     let asRelativePath (path : string) (current : string)=
          match path with
          
          | s when s.StartsWith('@') ->
               let body = s[1..]
               
               let dir = current.GetBaseDir ()
               let name = current.GetBaseName ()
               let ext = current.GetExtension ()
               
               let idx = name.LastIndexOf '_'
               let name = if idx < 0 then name else name[..(idx - 1)]
               
               dir + "/" + name + "_" + body + ext
          
          | "" -> current
          | s -> s
          
     let loadScene (path : string) (stage : Stage) =
          let path = stage.CurrentScenePath |> asRelativePath path
          GDTask.runOnThread (fun () ->
               GD.loadAs<PackedScene> path |> PackedScene.instantiate
          )
     
     // change scene
     
     let clearScene (stage : Stage) = task {
          match stage.CurrentScene with
          | Some n ->
               n.QueueFree ()
               if n.IsInsideTree () then
                    do! n.ToSignalTreeExited ()
          | None -> ()
     }
     
     let changeSceneWith (middleTask: unit -> Task<unit>) (scene : Node) (stage : Stage) = task {
          do! stage |> clearScene
          do! middleTask ()
          
          stage.CurrentScene <- Some scene
          stage.Viewport |> Node.addChild scene
          if scene.IsNodeReady() |> not then
               do! scene.ToSignalReady ()
          
          Engine.treeUpdateCache ()
          stage.Status <- Ready
          Logger.push $"Stage {stage.Root.GetPath()} is now at {stage.CurrentScenePath}"
     }
     
     let changeScene (scene : Node) (stage : Stage) =
          stage |> changeSceneWith (fun () -> task {}) scene
     
     // queued change scene
     
     let private fadeInOutAndChangeSceneWith (middleTask: unit -> Task<unit>) (path : string) (cutscene : CutsceneConfig) (stage : Stage) = 
          stage.Status <- Loading
          let loading = stage |> loadScene path
          let changing () = task {
               let! scene = loading
               do! stage |> changeSceneWith middleTask scene
          }
          stage |> fadeInOutWith changing cutscene
          
     let queueChangeSceneWith (middleTask: unit -> Task<unit>) (path : string) (cutscene : CutsceneConfig) (stage : Stage) = task {
          if stage.Status = Loading then
               Logger.pushWarn $"Stage {stage.Root.GetPath()} has been queued for changing scene before, changing task to {path} will be cancelled."
               ()
          else
               do! stage |> fadeInOutAndChangeSceneWith middleTask path cutscene
     }
     
     let queueChangeScene (path : string) (cutscene : CutsceneConfig) (stage : Stage) =
          stage |> queueChangeSceneWith (fun () -> task {}) path cutscene
     
     let queueReloadWith (middleTask: unit -> Task<unit>) (cutscene : CutsceneConfig) (stage : Stage) =
          stage |> queueChangeSceneWith middleTask "" cutscene
     
     let queueReload cutscene stage =
          stage |> queueChangeScene "" cutscene

     let queueExit (cutscene : CutsceneConfig) (stage : Stage) = task {
          if stage.Status = Loading then
               Logger.pushWarn $"Stage {stage.Root.GetPath()} has been queued for changing scene before, exiting task will be cancelled."
               ()
          else
               let exit () = task {
                    do! stage |> clearScene
                    stage.Status <- Pending
               }
               do! stage |> fadeInOutWith exit cutscene
     }