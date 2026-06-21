module Moon.Physics.PhysicsMotion

open Fodot.Injection
open Godot
open Fodot.Module.PhysicsServer
open Moon.Physics.MoonPhysics
open Moon.Physics.PhysicsCollide

let private checkIgnored<'a when 'a :> IPhysicsQueryResult> body (results : 'a seq) =
    results
    |> Seq.choose (fun r ->
        match r.Collider with
        | :? Node as n ->
            n
            |> Compo.tryFind<MoonObstacle>
            |> Option.filter _.IgnoreWhenInside
            |> Option.map (fun o ->
                o.EmitSignalIgnoredBy body
                r.Rid
            )
        | _ -> None
    )
    |> List.ofSeq

type CollisionObject2D with

    member this.CastMotion (motion : Vector2, ?maxDepth: float32, ?offset : Vector2, ?maxResult : int, ?margin : float32, ?updateMask : bool) =
        let query = this.GetShapeCast(?updateMask = updateMask)
        let q = query.Build()
        
        let ignored =
            q.QueryInside(?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> checkIgnored this
        q |> PhysicsQuery.appendExclude ignored
        
        let result =
            q.Collide(motion, ?maxDepth = maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
        let travel =
            result
            |> Option.map _.Result.SafeFraction
            |> Option.defaultValue 1f
        let motion = motion * travel
        this.GlobalPosition <- this.GlobalPosition + motion
        PhysicsServer2D.BodySetTransform(this.GetRid(), this.GlobalTransform)
        motion, result
        
type CollisionObject3D with

    member this.CastMotion (motion : Vector3, ?maxDepth: float32, ?offset : Vector3, ?maxResult : int, ?margin : float32, ?updateMask : bool) =
        let query = this.GetShapeCast(?updateMask = updateMask)
        let q = query.Build()
        
        let ignored =
            q.QueryInside(?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> checkIgnored this
        q |> PhysicsQuery.appendExclude ignored
        
        let result =
            q.Collide(motion, ?maxDepth = maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
        let travel =
            result
            |> Option.map _.Result.SafeFraction
            |> Option.defaultValue 1f
        let motion = motion * travel
        this.GlobalPosition <- this.GlobalPosition + motion
        PhysicsServer3D.BodySetTransform(this.GetRid(), this.GlobalTransform)
        motion, result