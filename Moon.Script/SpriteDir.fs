namespace Moon.Script

open Fodot
open Godot
open Moon.Component
open Moon.Interface
open Moon.Library
open Moon.Module

[<FScript(typeof<SpriteDir>)>]
type private SpriteDirScript(d : SpriteDir) =
    let spr = d |> Node.tryGetParent<CanvasItem>
    let mov = d |> Node.tryGetNode d.TrackingNode
    let rot = d |> Node.tryGetNode<Rotator> d.RotatorNode
    
    let recorder = mov |> Option.map Recorder2D.get
    
    let setFlip value =
        let result = if d.Flip then value |> not else value
        spr |> Option.iter (fun s -> s |> Flip.setH result)
        rot |> Option.iter (fun r -> r.Flip <- result)
        
    let update () =
        if d.Disabled then () else
        recorder
        |> Option.iter (fun r ->
            let s = r.LastMotion.X
            if s <> 0f then setFlip (s < 0f)
        )
        
    do d |> Engine.addPhysicsProcess update |> ignore
    
    interface IFlipHInit with
        member this.InitFlipH () =
            d |> Node.whenReady (fun _ -> setFlip true)
