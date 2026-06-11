namespace Moon.Script

open Fodot.Common
open Fodot.Core
open Godot
open Moon.Library
open Moon.Library.PhysicsMotion

[<FScript("test_physics")>]
type TestPhysics(col : CollisionObject2D) =
    
    let query = PhysicsQueryShape2D col

    do
        col |> Engine.addPhysicsProcess (fun _ ->
            let result =
                query.Build().Collide(Vector2.Down * 16f)
                |> Option.map (fst >> _.SafeFraction)
                |> Option.defaultValue -1f
            Logger.push result
        ) |> ignore