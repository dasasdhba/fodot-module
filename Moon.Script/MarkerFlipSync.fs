namespace Moon.Script

open Fodot
open Godot
open Moon
open Moon.Module
open Moon.Component

[<FScript(typeof<MarkerFlipSync>)>]
type private MarkerFlipSyncScript (marker : MarkerFlipSync) =
    
    let physics = marker.ProcessCallback = MarkerFlipSync.MarkerFlipSyncProcessCallback.Physics
    let anim = marker |> Node.tryGetParent
    
    let update () =
        anim
        |> Option.filter GodotObject.IsInstanceValid
        |> Option.iter (fun a ->
            marker.FlipH <- a |> Flip.getH
            marker.FlipV <- a |> Flip.getV
            marker.Position <- marker.Origin.Flip(marker.FlipH, marker.FlipV)
        )
    
    do
        marker.add_Updated update
    
        marker |> Node.whenReady (fun () ->
            marker.Origin <- marker.Position
            update ()
        )
        
        marker |> Engine.addProcess physics update |> ignore
