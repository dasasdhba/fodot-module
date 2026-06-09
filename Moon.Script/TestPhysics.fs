namespace Moon.Script

open Fodot.Common
open Fodot.Core
open Godot
open Moon.Library

[<FScript("test_physics")>]
type TestPhysics(col : CollisionObject2D) =
    
    let query = PhysicsQueryShape2D col
    
    do
        col |> Engine.addPhysicsProcess (fun _ ->
            let result =
                query.Build().Query (Vector2.Down * 16f)
                |> Seq.isEmpty
            Logger.push result
        ) |> ignore