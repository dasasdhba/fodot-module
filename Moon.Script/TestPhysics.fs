namespace Moon.Script

open System
open Fodot
open Fodot.Injection
open Godot
open Moon
open Moon.Physics
open Moon.Physics.PhysicsCollide
open Moon.Physics.PhysicsMotion

[<FScript("test_physics")>]
type TestPhysics(col : CollisionObject2D) =
    
    let query = PhysicsQueryShape2D col

    do
        col |> Engine.addPhysicsProcess (fun _ ->
            let result =
                query.Build().Collide(Vector2.Down * 16f)
                |> Option.map _.Result.SafeFraction
                |> Option.defaultValue -1f
            Logger.push result
        ) |> ignore

[<FScript("test_random_physics")>]
type TestRandomPhysics(col : CollisionObject2D) =
    
    let speed = Mathe.RandfRange(100f, 200f)
    let angle = Mathe.RandfRange(-Single.Pi, Single.Pi)
    let margin =
        col
        |> Compo.tryFind<MoonBody2D>
        |> Option.map _.SafeMargin
        |> Option.defaultValue 0.08f

    do
        col |> Engine.addPhysicsDelta32Process (fun delta ->
            let motion = speed * delta * Vector2.Right.Rotated(angle)
            col.CastMotion(motion, margin = margin) |> ignore
            ()
        ) |> ignore
