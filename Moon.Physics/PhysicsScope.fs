namespace Moon.Physics

open Fodot
open Godot

module MoonPhysics2D =
    
    let blockSnapMargin = 1f
    let bodyMaxRecovery = 4f
    let blockRecoveryMargin = 0.1f
    let bodyRecoveryScale = 2f
    let blockPushTolerance = 8f

    let private bodyQueries =
        WeakMeta<PhysicsQueryShape2D>()
        
    let private bodyRays =
        WeakMeta<PhysicsQueryRaycast2D>()
    
    let getBodyQuery (body : CollisionObject2D) =
        bodyQueries |> WeakMeta.getOrAdd body (fun () ->
            PhysicsQueryShape2D body
        )
        
    let getBodyRaycast (body : CollisionObject2D) =
        bodyRays |> WeakMeta.getOrAdd body (fun () ->
            PhysicsQueryRaycast2D body
        )
    
    let updateBodyCollisionMask (body : CollisionObject2D) =
        let mask = body.CollisionMask
        
        bodyQueries
        |> WeakMeta.tryGet body
        |> Option.iter (fun query ->
            query |> PhysicsQuery.setCollisionMask mask
        )
        
        bodyRays
        |> WeakMeta.tryGet body
        |> Option.iter (fun query ->
            query |> PhysicsQuery.setCollisionMask mask
        )
    
    let snapCheckAngle (angle : float32) (snap : Vector3)=
        let v = Vector2(snap.X, snap.Y)
        if v = Vector2.Zero then None else
        
        let d = v.Normalized()
        let a = Mathf.DegToRad (if snap.Z > 0f then snap.Z else 45f)
        let diff =
            (angle, d.Angle())
            |> Mathf.AngleDifference
            |> Mathf.Abs
        if diff < a then
            Some (d, diff)
        else
            None
            
    let snapCheckNormal (normal : Vector2) (snap : Vector3)=
        let angle = (-normal).Angle()
        snap |> snapCheckAngle angle

module MoonPhysics3D =
    
    let blockSnapMargin = 0.04f
    let bodyMaxRecovery = 0.2f
    let blockRecoveryMargin = 0.004f
    let bodyRecoveryScale = 2f
    let blockPushTolerance = 0.32f

    let private bodyQueries =
        WeakMeta<PhysicsQueryShape3D>()
        
    let private bodyRays =
        WeakMeta<PhysicsQueryRaycast3D>()
    
    let getBodyQuery (body : CollisionObject3D) =
        bodyQueries |> WeakMeta.getOrAdd body (fun () ->
            PhysicsQueryShape3D body
        )
        
    let getBodyRaycast (body : CollisionObject3D) =
        bodyRays |> WeakMeta.getOrAdd body (fun () ->
            PhysicsQueryRaycast3D body
        )
    
    let updateBodyCollisionMask (body : CollisionObject3D) =
        let mask = body.CollisionMask
        
        bodyQueries
        |> WeakMeta.tryGet body
        |> Option.iter (fun query ->
            query |> PhysicsQuery.setCollisionMask mask
        )
        
        bodyRays
        |> WeakMeta.tryGet body
        |> Option.iter (fun query ->
            query |> PhysicsQuery.setCollisionMask mask
        )
    
    let snapCheckNormal (normal : Vector3) (snap : Vector4)=
        let v = Vector3(snap.X, snap.Y, snap.Z)
        if v = Vector3.Zero then None else
        
        let d = v.Normalized()
        let a = Mathf.DegToRad (if snap.W > 0f then snap.W else 45f)
        let diff =
            (-normal).AngleTo(v)
            |> Mathf.Abs
        if diff < a then
            Some (d, diff)
        else
            None
            
module MoonPhysics =
    
    type CollisionObject2D with
    
        member this.UpdateQueries () =
            MoonPhysics2D.updateBodyCollisionMask this
        
        member this.GetShapeCast (?updateMask: bool) =
            let update = defaultArg updateMask true
            let query = MoonPhysics2D.getBodyQuery this
            if update then query |> PhysicsQuery.setCollisionMask this.CollisionMask
            query
            
        member this.GetRaycast (?updateMask: bool) =
            let update = defaultArg updateMask true
            let query = MoonPhysics2D.getBodyRaycast this
            if update then query |> PhysicsQuery.setCollisionMask this.CollisionMask
            query
            
    type CollisionObject3D with
    
        member this.UpdateQueries () =
            MoonPhysics3D.updateBodyCollisionMask this
    
        member this.GetShapeCast (?updateMask: bool) =
            let update = defaultArg updateMask true
            let query = MoonPhysics3D.getBodyQuery this
            if update then query |> PhysicsQuery.setCollisionMask this.CollisionMask
            query
            
        member this.GetRaycast (?updateMask: bool) =
            let update = defaultArg updateMask true
            let query = MoonPhysics3D.getBodyRaycast this
            if update then query |> PhysicsQuery.setCollisionMask this.CollisionMask
            query
