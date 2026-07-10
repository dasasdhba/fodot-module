module Moon.Physics.PhysicsMotion

open Godot
open Moon.Physics.MoonPhysics
open Moon.Physics.PhysicsCollide

type CollisionObject2D with

    member this.CastMotionBy (q : PhysicsShapeQuerier2D, motion : Vector2, ?maxDepth: float32, ?offset : Vector2, ?maxResult : int, ?margin : float32) =
        let result =
            q.Collide(motion, ?maxDepth = maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
        let travel =
            result
            |> Option.map _.Result.SafeFraction
            |> Option.defaultValue 1f
        let motion = motion * travel
        this.GlobalPosition <- this.GlobalPosition + motion
        motion, result
        
    member this.CastMotion (motion : Vector2, ?maxDepth: float32, ?offset : Vector2, ?maxResult : int, ?margin : float32, ?updateMask : bool) =
        let query = this.GetShapeCast(?updateMask = updateMask)
        let q = query.Build()
        this.CastMotionBy(q, motion, ?maxDepth = maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
        
type CollisionObject3D with

    member this.CastMotionBy (q : PhysicsShapeQuerier3D, motion : Vector3, ?maxDepth: float32, ?offset : Vector3, ?maxResult : int, ?margin : float32) =
        let result =
            q.Collide(motion, ?maxDepth = maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
        let travel =
            result
            |> Option.map _.Result.SafeFraction
            |> Option.defaultValue 1f
        let motion = motion * travel
        this.GlobalPosition <- this.GlobalPosition + motion
        motion, result

    member this.CastMotion (motion : Vector3, ?maxDepth: float32, ?offset : Vector3, ?maxResult : int, ?margin : float32, ?updateMask : bool) =
        let query = this.GetShapeCast(?updateMask = updateMask)
        let q = query.Build()
        this.CastMotionBy(q, motion, ?maxDepth = maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
