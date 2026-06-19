namespace Moon.Script

open Fodot
open Godot
open Moon.Physics
open Moon.Physics.PhysicsCollide

[<FScript("test_physics")>]
type TestPhysics(col : CollisionObject2D) =
    
    let query = PhysicsQueryShape2D col

    do
        col |> Engine.addPhysicsProcess (fun _ ->
            let result =
                query.Build().Collide(Vector2.Down * 16f)
                |> Option.map _.Motion.SafeFraction
                |> Option.defaultValue -1f
            Logger.push result
        ) |> ignore
