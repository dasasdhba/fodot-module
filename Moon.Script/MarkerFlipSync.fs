namespace Moon

open Godot

[<FScript(typeof<MarkerFlipSync>)>]
type private MarkerFlipSyncScript (marker : MarkerFlipSync) =

    let physics = marker.ProcessCallback = MarkerFlipSync.MarkerFlipSyncProcessCallback.Physics
    let anim = marker |> Node.tryGetNode marker.Target

    let update () =
        anim
        |> Option.filter GodotObject.IsInstanceValid
        |> Option.iter (fun a ->
            marker.FlipH <- a |> Flip.getH
            marker.FlipV <- a |> Flip.getV
            marker.Position <- marker.Origin.Flip(marker.FlipH, marker.FlipV)
        )

    do

        marker |> Node.whenReady (fun () ->
            marker.Origin <- marker.Position
            update ()
        )

        marker |> Engine.addProcess physics update |> ignore

    [<GDMember>]
    member this.Update() = update()
