namespace Moon.Physics

open Fodot
open Fodot.Injection
open Godot
open Moon

// one should make sure physics server is located
// at the end of game physics process

module private MoonPhysicsScope2D =
    
    let bodies =
        ResizeArray<CollisionObject2D * MoonBody2D>()
    let blocks =
        ResizeArray<CollisionObject2D * MoonBlock2D>()
    
    let private bodyQueries =
        WeakMeta<PhysicsQueryShape2D>()
    let private blockQueries =
        WeakMeta<PhysicsQueryShape2D>()
        
    let getBodyQuery (body : CollisionObject2D) =
        bodyQueries |> WeakMeta.getOrAdd body (lazy (
            PhysicsQueryShape2D body
        ))
        
    let getBlockQuery (block : CollisionObject2D) =
        blockQueries |> WeakMeta.getOrAdd block (lazy (
            PhysicsQueryShape2D block
        ))
    
    [<FScript(typeof<CollisionObject2D>)>]
    type private MoonPhysicsObject2D(col : CollisionObject2D) =
        do
            col
            |> Compo.tryFind<MoonBlock2D>
            |> Option.map (fun block ->
                let data = col, block
                blocks.AddWithNode(data, fun d -> d |> fst :> Node)
            )
            |> Option.orElseWith (fun _ ->
                col
                |> Compo.tryFind<MoonBody2D>
                |> Option.map (fun body ->
                    let data = col, body
                    bodies.AddWithNode(data, fun d -> d |> fst :> Node)
                )
            ) |> ignore

[<FScript("moon_physics_server_2d")>]
type private MoonPhysicsServer2D(node : Node) =
    
    let update (delta : float) =
        ()
        
    do node |> Engine.addPhysicsDeltaProcess update |> ignore