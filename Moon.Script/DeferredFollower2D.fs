namespace Moon.Script

open Fodot
open Fodot.Module
open Godot
open Moon
open Moon.Component
open Moon.Module

[<FScript(typeof<DeferredFollower2D>)>]
type private DeferredFollower2DScript(follower : DeferredFollower2D) =
    let physics =
        follower.ProcessCallback =
            DeferredFollower2D.DeferredFollower2DProcessCallback.Physics

    let tracking =
        follower |> Node.tryGetNode<CanvasItem> follower.TrackingNode

    let update delta =
        tracking
        |> Option.filter GodotObject.IsInstanceValid
        |> Option.filter _.IsInsideTree()
        |> Option.filter _.IsVisibleInTree()
        |> Option.map (fun t ->
            let targetPosition = t |> CanvasItem.getGlobalPosition
            
            if follower.Visible then
                follower.GlobalPosition <-
                    follower.GlobalPosition.ConvToward(targetPosition, follower.Rate * delta)
            else
                follower.Show()
                follower.GlobalPosition <- targetPosition
        )
        |> Option.defaultWith (fun _ ->
            follower.Hide()
        )

    do
        follower |> Node.whenReady (fun () ->
            follower
            |> Node.tryGetNode follower.ReparentRoot
            |> Option.iter (fun root ->
                follower |> Node.reparentKeep root
            )
        )

        follower |> Engine.addDelta32Process physics update |> ignore
