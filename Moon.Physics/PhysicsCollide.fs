module Moon.Physics.PhysicsCollide

open FSharp.Extend
open Godot
open Moon.Utils

type PhysicsQueryRaycast2D with

    member this.QueryCollideGlobal (from : Vector2, to' : Vector2, ?maxResult : int, ?hitFromInside : bool) =
        let dir = to' - from
        this.QueryGlobal(from, to', ?maxResult = maxResult, ?hitFromInside = hitFromInside)
        |> Seq.choose (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters2D with
            | Some (d, _) when d.Dot dir <= 0f -> None
            | _ -> Some r
        )
        
    member this.QueryCollide (target : Vector2, ?offset : Vector2, ?maxResult : int, ?hitFromInside : bool) =
        this.Query(target, ?offset = offset, ?maxResult = maxResult, ?hitFromInside = hitFromInside)
        |> Seq.choose (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters2D with
            | Some (d, _) when d.Dot target <= 0f -> None
            | _ -> Some r
        )

type PhysicsQueryRaycast3D with

    member this.QueryCollideGlobal (from : Vector3, to' : Vector3, ?maxResult : int, ?hitFromInside : bool, ?hitBackFaces : bool) =
        let dir = to' - from
        this.QueryGlobal(from, to', ?maxResult = maxResult, ?hitFromInside = hitFromInside, ?hitBackFaces = hitBackFaces)
        |> Seq.choose (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters3D with
            | Some (d, _) when d.Dot dir <= 0f -> None
            | _ -> Some r
        )
        
    member this.QueryCollide (target : Vector3, ?offset : Vector3, ?maxResult : int, ?hitFromInside : bool, ?hitBackFaces : bool) =
        this.Query(target, ?offset = offset, ?maxResult = maxResult, ?hitFromInside = hitFromInside, ?hitBackFaces = hitBackFaces)
        |> Seq.choose (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters3D with
            | Some (d, _) when d.Dot target <= 0f -> None
            | _ -> Some r
        )

type PhysicsShapeQuerier2D with

    member this.QueryCollide (motion : Vector2, ?offset : Vector2, ?maxResult : int, ?margin : float32, ?hitFromInside : bool) =
        this.Cast(motion, ?offset = offset, ?maxResult = maxResult, ?margin = margin, ?hitFromInside = hitFromInside)
        |> Seq.choose (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters2D with
            | Some (d, _) when d.Dot motion <= 0f -> None
            | _ -> Some r
        )

    member this.Collide (motion : Vector2, ?maxDepth: float32, ?offset : Vector2, ?maxResult : int, ?margin : float32) =
        
        let cast m offset =
            this.QueryCollide(m, ?offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
        
        // check for initial overlap
        
        let solids, platforms =
            this.Query(?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> Seq.map (fun r -> r, r |> PhysicsQueryResult.getOneWayParameters2D)
            |> Array.ofSeq
            |> Array.partition (fun (_, o) -> o |> Option.isNone)
        
        let recover (dir : Vector2) offset depth =
            let overlap d =
                this.QueryInside(offset + d * dir, ?maxResult = maxResult, ?margin = margin)
                |> Seq.exists (fun r ->
                    r
                    |> PhysicsQueryResult.getOneWayParameters2D
                    |> Option.isNone
                )
            
            let shift = depth * dir
            let travel =
                cast shift (Some offset) 
                |> Option.map _.SafeFraction
                |> Option.defaultValue 1f
            let travel = travel * depth
            if overlap travel |> not then
                let in', out =
                    Math.binarySearch 16 1e-3f (fun d -> overlap (d * travel))
                Some (in' * travel, out * travel)
            else
                None
        
        let travelSolid (rep : PhysicsQueryShapeResult2D) : PhysicsQueryCollisionResult2D =
            let maxDepth = defaultArg maxDepth 4f
            if motion = Vector2.Zero || maxDepth <= 0f then
                rep
                |> PhysicsQueryShapeCastResult2D.From
                |> PhysicsQueryCollisionResult2D.From
            else
            
            let offset = defaultArg offset Vector2.Zero
            let dir = motion.Normalized()
            let len = motion.Length ()
            
            recover -dir offset maxDepth
            |> Option.map (fun (unsafe, safe) ->
                (rep, -safe / len, -unsafe / len)
                |> PhysicsQueryShapeCastResult2D.From
                |> PhysicsQueryCollisionResult2D.From
            )
            |> Option.orElseWith (fun _ ->
                 recover dir offset maxDepth
                 |> Option.map (fun (unsafe, safe) ->
                    (rep, safe / len, unsafe / len)
                    |> PhysicsQueryShapeCastResult2D.From
                    |> PhysicsQueryCollisionResult2D.FromRecovered
                 )
            )
            |> Option.defaultWith (fun _ ->
                rep
                |> PhysicsQueryShapeCastResult2D.From
                |> PhysicsQueryCollisionResult2D.From
            )
        
        let travelPlatform () =
            if motion = Vector2.Zero then None else
            
            let offset = defaultArg offset Vector2.Zero
            let dir = motion.Normalized()
            let len = motion.Length()
            
            platforms
            |> Seq.choose (fun (r, o) ->
                o
                |> Option.bind (function
                    | v, m when m > 0f && v.Dot dir >= 0f -> Some (r, m)
                    | _ -> None
                )
            )
            |> Seq.choose (fun (r, m) ->
                let overlap d =
                    this.QueryInside(offset - d * dir, ?maxResult = maxResult, ?margin = margin)
                    |> Seq.exists (fun i -> i.Rid = r.Rid)
                if overlap m then
                    None
                else
                    let unsafe, safe =
                        Math.binarySearch 16 1e-3f (fun d -> overlap (d * m))
                    (r, -safe * m / len, -unsafe * m / len)
                    |> PhysicsQueryShapeCastResult2D.From
                    |> PhysicsQueryCollisionResult2D.From
                    |> Some
            )
            |> Seq.tryMinBy _.Result.SafeFraction
        
        if solids |> Array.isEmpty |> not then
            solids
            |> Array.head
            |> fst
            |> travelSolid
            |> Some

        elif platforms |> Array.isEmpty |> not then
            travelPlatform()

        else
            
        // now do normal casting
        
        cast motion offset
        |> Option.map PhysicsQueryCollisionResult2D.From

type PhysicsShapeQuerier3D with

    member this.QueryCollide (motion : Vector3, ?offset : Vector3, ?maxResult : int, ?margin : float32, ?hitFromInside : bool) =
        this.Cast(motion, ?offset = offset, ?maxResult = maxResult, ?margin = margin, ?hitFromInside = hitFromInside)
        |> Seq.choose (fun r ->
            match r |> PhysicsQueryResult.getOneWayParameters3D with
            | Some (d, _) when d.Dot motion <= 0f -> None
            | _ -> Some r
        )

    member this.Collide (motion : Vector3, ?maxDepth: float32, ?offset : Vector3, ?maxResult : int, ?margin : float32) =
        
        let cast m offset =
            this.QueryCollide(m, ?offset = offset, ?maxResult = maxResult, ?margin = margin, hitFromInside = false)
            |> Seq.tryHead
        
        // check for initial overlap
        
        let solids, platforms =
            this.Query(?offset = offset, ?maxResult = maxResult, ?margin = margin)
            |> Seq.map (fun r -> r, r |> PhysicsQueryResult.getOneWayParameters3D)
            |> Array.ofSeq
            |> Array.partition (fun (_, o) -> o |> Option.isNone)
        
        let recover (dir : Vector3) offset depth =
            let overlap d =
                this.QueryInside(offset + d * dir, ?maxResult = maxResult, ?margin = margin)
                |> Seq.exists (fun r ->
                    r
                    |> PhysicsQueryResult.getOneWayParameters3D
                    |> Option.isNone
                )
            
            let shift = depth * dir
            let travel =
                cast shift (Some offset) 
                |> Option.map _.SafeFraction
                |> Option.defaultValue 1f
            let travel = travel * depth
            if overlap travel |> not then
                let in', out =
                    Math.binarySearch 16 1e-3f (fun d -> overlap (d * travel))
                Some (in' * travel, out * travel)
            else
                None
        
        let travelSolid (rep : PhysicsQueryShapeResult3D) : PhysicsQueryCollisionResult3D =
            let maxDepth = defaultArg maxDepth 0.25f
            if motion = Vector3.Zero || maxDepth <= 0f then
                rep
                |> PhysicsQueryShapeCastResult3D.From
                |> PhysicsQueryCollisionResult3D.From
            else
            
            let offset = defaultArg offset Vector3.Zero
            let dir = motion.Normalized()
            let len = motion.Length ()
            
            recover -dir offset maxDepth
            |> Option.map (fun (unsafe, safe) ->
                (rep, -safe / len, -unsafe / len)
                |> PhysicsQueryShapeCastResult3D.From
                |> PhysicsQueryCollisionResult3D.From
            )
            |> Option.orElseWith (fun _ ->
                 recover dir offset maxDepth
                 |> Option.map (fun (unsafe, safe) ->
                    (rep, safe / len, unsafe / len)
                    |> PhysicsQueryShapeCastResult3D.From
                    |> PhysicsQueryCollisionResult3D.FromRecovered
                 )
            )
            |> Option.defaultWith (fun _ ->
                rep
                |> PhysicsQueryShapeCastResult3D.From
                |> PhysicsQueryCollisionResult3D.From
            )
        
        let travelPlatform () =
            if motion = Vector3.Zero then None else
            
            let offset = defaultArg offset Vector3.Zero
            let dir = motion.Normalized()
            let len = motion.Length()
            
            platforms
            |> Seq.choose (fun (r, o) ->
                o
                |> Option.bind (function
                    | v, m when m > 0f && v.Dot dir >= 0f -> Some (r, m)
                    | _ -> None
                )
            )
            |> Seq.choose (fun (r, m) ->
                let overlap d =
                    this.QueryInside(offset - d * dir, ?maxResult = maxResult, ?margin = margin)
                    |> Seq.exists (fun i -> i.Rid = r.Rid)
                if overlap m then
                    None
                else
                    let unsafe, safe =
                        Math.binarySearch 16 1e-3f (fun d -> overlap (d * m))
                    (r, -safe * m / len, -unsafe * m / len)
                    |> PhysicsQueryShapeCastResult3D.From
                    |> PhysicsQueryCollisionResult3D.From
                    |> Some
            )
            |> Seq.tryMinBy _.Result.SafeFraction
        
        if solids |> Array.isEmpty |> not then
            solids
            |> Array.head
            |> fst
            |> travelSolid
            |> Some

        elif platforms |> Array.isEmpty |> not then
            travelPlatform()
            
        else
            
        // now do normal casting
        
        cast motion offset
        |> Option.map PhysicsQueryCollisionResult3D.From
