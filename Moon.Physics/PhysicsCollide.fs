module Moon.Physics.PhysicsCollide

open FSharp.Generic
open Godot

type PhysicsQueryRaycast2D with

    member this.QueryCollideGlobal (from : Vector2, to' : Vector2, ?maxResult : int, ?hitFromInside : bool) =
        let dir = to' - from
        this.QueryGlobal(from, to', ?maxResult = maxResult, ?hitFromInside = hitFromInside)
        |> Seq.filter (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters2D with
            | Some (d, _) when d.Dot dir <= 0f -> false
            | _ -> true
        )
        
    member this.QueryCollide (target : Vector2, ?offset : Vector2, ?maxResult : int, ?hitFromInside : bool) =
        this.Query(target, ?offset = offset, ?maxResult = maxResult, ?hitFromInside = hitFromInside)
        |> Seq.filter (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters2D with
            | Some (d, _) when d.Dot target <= 0f -> false
            | _ -> true
        )

type PhysicsQueryRaycast3D with

    member this.QueryCollideGlobal (from : Vector3, to' : Vector3, ?maxResult : int, ?hitFromInside : bool, ?hitBackFaces : bool) =
        let dir = to' - from
        this.QueryGlobal(from, to', ?maxResult = maxResult, ?hitFromInside = hitFromInside, ?hitBackFaces = hitBackFaces)
        |> Seq.filter (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters3D with
            | Some (d, _) when d.Dot dir <= 0f -> false
            | _ -> true
        )
        
    member this.QueryCollide (target : Vector3, ?offset : Vector3, ?maxResult : int, ?hitFromInside : bool, ?hitBackFaces : bool) =
        this.Query(target, ?offset = offset, ?maxResult = maxResult, ?hitFromInside = hitFromInside, ?hitBackFaces = hitBackFaces)
        |> Seq.filter (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters3D with
            | Some (d, _) when d.Dot target <= 0f -> false
            | _ -> true
        )

type PhysicsShapeQuerier2D with

    member this.QuerySolid (?offset : Vector2, ?maxResult : int, ?margin : float32) =
        this.QueryInside(?offset = offset, ?maxResult = maxResult, ?margin = margin)
        |> Seq.filter (fun r ->
            r |> PhysicsQueryResult.getOneWayParameters2D |> Option.isNone
        )

    member this.QueryCollide (motion : Vector2, ?offset : Vector2, ?maxResult : int, ?margin : float32, ?hitFromInside : bool) =
        this.Cast(motion, ?offset = offset, ?maxResult = maxResult, ?margin = margin, ?hitFromInside = hitFromInside)
        |> Seq.filter (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters2D with
            | Some (d, _) when d.Dot motion <= 0f -> false
            | _ -> true
        )
    
    /// One should make sure the querier is inside at start.
    member this.PushOut (motion :Vector2, ?offset : Vector2, ?maxResult : int, ?margin : float32) =
        let offset = defaultArg offset Vector2.Zero
        let outTravel =
            this.QueryCollide(motion, offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
            |> Option.map _.SafeFraction
            |> Option.defaultValue 1f
        let offset = offset + motion * outTravel
        if
            this.QuerySolid(offset, ?maxResult = maxResult, ?margin = margin)
            |> Seq.isEmpty |> not
        then
            None
        else
            this.QueryCollide(-motion, offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
            |> Option.map (fun r ->
                {
                    r with
                        SafeFraction = outTravel - r.SafeFraction
                        UnsafeFraction = outTravel - r.UnsafeFraction
                }
            )
    
    member this.Collide (motion : Vector2, ?maxDepth: float32, ?offset : Vector2, ?maxResult : int, ?margin : float32) =
        
        let cast m offset =
            this.QueryCollide(m, ?offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
        
        // check for initial overlap
        
        let platforms, solids =
            this.Query(?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> Seq.map (fun r ->
                match r |> PhysicsQueryResult.getOneWayParameters2D with
                | Some o -> Ok (r, o)
                | _ -> Result.Error r
            )
            |> Seq.partitionResult
        
        let travelSolid (rep : PhysicsQueryShapeResult2D) : PhysicsQueryCollisionResult2D =
            let maxDepth = defaultArg maxDepth MoonPhysics2D.bodyMaxRecovery
            if motion = Vector2.Zero || maxDepth <= 0f then
                rep
                |> PhysicsQueryShapeCastResult2D.From
                |> PhysicsQueryCollisionResult2D.From
            else
            
            let dir = motion.Normalized()
            let len = motion.Length()
            
            this.PushOut(-dir * maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> Option.map (
                _.ChangeStep(maxDepth, -len)
                >> PhysicsQueryCollisionResult2D.From
            )
            |> Option.orElseWith (fun _ ->
                this.PushOut(motion + dir * maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
                |> Option.map (
                    _.ChangeStep(len + maxDepth, len)
                    >> PhysicsQueryCollisionResult2D.FromRecovered
                )
            )
            |> Option.defaultWith (fun _ ->
                rep
                |> PhysicsQueryShapeCastResult2D.From
                |> PhysicsQueryCollisionResult2D.From
            )
        
        let travelPlatform () =
            if motion = Vector2.Zero || platforms.Count = 0 then None else
            
            let offset = defaultArg offset Vector2.Zero
            let dir = motion.Normalized()
            let len = motion.Length()
            
            platforms
            |> Seq.map snd
            |> Seq.choose (function
                | v, m when m > 0f && v.Dot dir >= 0f -> Some m
                | _ -> None
            )
            |> Seq.choose (fun m ->
                this.PushOut(-dir * m, offset = offset, ?maxResult = maxResult, ?margin = margin)
                |> Option.map (
                    _.ChangeStep(m, -len)
                    >> PhysicsQueryCollisionResult2D.From
                )
            )
            |> Seq.tryMinBy _.Result.SafeFraction
        
        if solids.Count > 0 then
            solids[0]
            |> travelSolid
            |> Some

        else
            travelPlatform()
            |> Option.orElseWith (fun _ ->
                cast motion offset
                |> Option.map PhysicsQueryCollisionResult2D.From
            )

type PhysicsShapeQuerier3D with

    member this.QuerySolid (?offset : Vector3, ?maxResult : int, ?margin : float32) =
        this.QueryInside(?offset = offset, ?maxResult = maxResult, ?margin = margin)
        |> Seq.filter (fun r ->
            r |> PhysicsQueryResult.getOneWayParameters3D |> Option.isNone
        )

    member this.QueryCollide (motion : Vector3, ?offset : Vector3, ?maxResult : int, ?margin : float32, ?hitFromInside : bool) =
        this.Cast(motion, ?offset = offset, ?maxResult = maxResult, ?margin = margin, ?hitFromInside = hitFromInside)
        |> Seq.filter (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters3D with
            | Some (d, _) when d.Dot motion <= 0f -> false
            | _ -> true
        )
    
    /// One should make sure the querier is inside at start.
    member this.PushOut (motion :Vector3, ?offset : Vector3, ?maxResult : int, ?margin : float32) =
        let offset = defaultArg offset Vector3.Zero
        let outTravel =
            this.QueryCollide(motion, offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
            |> Option.map _.SafeFraction
            |> Option.defaultValue 1f
        let offset = offset + motion * outTravel
        if
            this.QuerySolid(offset, ?maxResult = maxResult, ?margin = margin)
            |> Seq.isEmpty |> not
        then
            None
        else
            this.QueryCollide(-motion, offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
            |> Option.map (fun r ->
                {
                    r with
                        SafeFraction = outTravel - r.SafeFraction
                        UnsafeFraction = outTravel - r.UnsafeFraction
                }
            )
    
    member this.Collide (motion : Vector3, ?maxDepth: float32, ?offset : Vector3, ?maxResult : int, ?margin : float32) =
        
        let cast m offset =
            this.QueryCollide(m, ?offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
        
        // check for initial overlap
        
        let platforms, solids =
            this.Query(?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> Seq.map (fun r ->
                match r |> PhysicsQueryResult.getOneWayParameters3D with
                | Some o -> Ok (r, o)
                | _ -> Result.Error r
            )
            |> Seq.partitionResult
        
        let travelSolid (rep : PhysicsQueryShapeResult3D) : PhysicsQueryCollisionResult3D =
            let maxDepth = defaultArg maxDepth MoonPhysics3D.bodyMaxRecovery
            if motion = Vector3.Zero || maxDepth <= 0f then
                rep
                |> PhysicsQueryShapeCastResult3D.From
                |> PhysicsQueryCollisionResult3D.From
            else
            
            let dir = motion.Normalized()
            let len = motion.Length()
            
            this.PushOut(-dir * maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> Option.map (
                _.ChangeStep(maxDepth, -len)
                >> PhysicsQueryCollisionResult3D.From
            )
            |> Option.orElseWith (fun _ ->
                this.PushOut(motion + dir * maxDepth, ?offset = offset, ?maxResult = maxResult, ?margin = margin)
                |> Option.map (
                    _.ChangeStep(len + maxDepth, len)
                    >> PhysicsQueryCollisionResult3D.FromRecovered
                )
            )
            |> Option.defaultWith (fun _ ->
                rep
                |> PhysicsQueryShapeCastResult3D.From
                |> PhysicsQueryCollisionResult3D.From
            )
        
        let travelPlatform () =
            if motion = Vector3.Zero || platforms.Count = 0 then None else
            
            let offset = defaultArg offset Vector3.Zero
            let dir = motion.Normalized()
            let len = motion.Length()
            
            platforms
            |> Seq.map snd
            |> Seq.choose (function
                | v, m when m > 0f && v.Dot dir >= 0f -> Some m
                | _ -> None
            )
            |> Seq.choose (fun m ->
                this.PushOut(-dir * m, offset = offset, ?maxResult = maxResult, ?margin = margin)
                |> Option.map (
                    _.ChangeStep(m, -len)
                    >> PhysicsQueryCollisionResult3D.From
                )
            )
            |> Seq.tryMinBy _.Result.SafeFraction
        
        if solids.Count > 0 then
            solids[0]
            |> travelSolid
            |> Some

        else
            travelPlatform()
            |> Option.orElseWith (fun _ ->
                cast motion offset
                |> Option.map PhysicsQueryCollisionResult3D.From
            )
