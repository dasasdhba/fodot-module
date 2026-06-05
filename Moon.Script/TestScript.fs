module Moon.Script.TestScript

open System.Threading
open Fodot.Async
open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot
open Moon

[<FScript("test_script")>]
type TestScript (node : Node) =
    let scene = node |> Node.loadAs<PackedScene> "marker.tscn"
    let loader = node |> AsyncScene.create<Node2D> scene 1 0
    let anode = AsyncNode.NewPhysics node CancellationToken.None
    
    do node |> GDTask.post (fun _ ->
        let _ = task {
            while true do
                do! anode.Delay 0.2
                Logger.push $"proc theard delay {GodotThread.IsMainThread()}"
                let child = node |> Node.getChildren |> Seq.randomChoice
                child.QueueFree ()
        }
        
        let _ = task {
            while true do
                do! anode.Delay 0.1
                Logger.push $"proc theard create {GodotThread.IsMainThread()}"
                let child = loader.Get ()
                child.Position <- Vector2(Mathe.RandfRange(128f, 960f), Mathe.RandfRange(128f, 640f))
                node |> Node.addChild child
        }
        
        ()
    )