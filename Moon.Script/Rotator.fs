namespace Moon.Script

open Fodot
open Fodot.Module
open Godot
open Moon
open Moon.Component
open Moon.Utils

[<FScript(typeof<Rotator>)>]
type private RotatorScript(rot : Rotator) =
    let physics = rot.ProcessCallback = Rotator.RotatorProcessCallback.Physics
    let parent = rot |> Node.tryGetParent<CanvasItem>
    
    let update delta =
        parent
        |> Option.filter (fun _ -> rot.Disabled |> not)
        |> Option.iter (fun p ->
            let r = p |> CanvasItem.getRotation
            let d = rot.Speed * delta * (1f |> Math.flip rot.Flip)
            let r = Mathe.WrapAngle(r + d);
            p |> CanvasItem.setRotation r
        )
    
    do rot |> Engine.addDelta32Process physics update |> ignore

