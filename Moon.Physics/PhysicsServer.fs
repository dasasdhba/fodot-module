namespace Moon.Physics

open Fodot
open Fodot.Injection
open Fodot.Module.PhysicsServer
open Godot
open Moon
open Moon.Physics.PhysicsCollide
open Moon.Utils

// one should make sure physics server is located
// at the end of game physics process

module private MoonPhysics2D =
    
    let blocks =
        ResizeArray<CollisionObject2D * MoonBlock2D>()
    
    [<FScript(typeof<CollisionObject2D>)>]
    type MoonPhysicsObject2D(col : CollisionObject2D) =
        do
            col
            |> Compo.tryFind<MoonBlock2D>
            |> Option.iter (fun block ->
                let data = col, block
                blocks.AddWithNode(data, fun d -> d |> fst :> Node)
            )
    
    let bodyQueries =
        WeakMeta<PhysicsQueryShape2D>()
    let blockQueries =
        WeakMeta<PhysicsQueryShape2D>()
        
    let getBodyQuery (body : CollisionObject2D) =
        bodyQueries |> WeakMeta.getOrAdd body (lazy (
            PhysicsQueryShape2D body
        ))
        
    let getBlockQuery (block : CollisionObject2D) =
        blockQueries |> WeakMeta.getOrAdd block (lazy (
            PhysicsQueryShape2D block
        ))
    
    let bodyRays = WeakMeta<PhysicsQueryRaycast2D>()
    
    let getBodyRay (body : CollisionObject2D) =
        bodyRays |> WeakMeta.getOrAdd body (lazy (
            PhysicsQueryRaycast2D body
        ))
    
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
            Some d
        else
            None
    
    let bodyGetSnap (dir : Vector2) (arg : MoonBody2D) =
        let angle = (-dir).Angle()
        arg.Snaps
        |> Seq.tryPick (fun s -> s |> snapCheckAngle angle)
    
    let bodyCheckSnap (block : Rid, brg : MoonBlock2D) (body : CollisionObject2D, arg : MoonBody2D) =
        let query = getBodyQuery body
        let q = query.Build ()
        arg.Snaps
        |> Seq.filter (fun v ->
            brg.InvalidSnaps
            |> Seq.exists (fun s ->
                v
                |> snapCheckAngle (s.Angle())
                |> Option.isSome
            )
            |> not
        )
        |> Seq.map (fun v -> Vector2(v.X, v.Y).Normalized())
        |> Seq.tryFind (fun v ->
            q.QueryInside (offset = v, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.existsAndExclude q (fun r -> r.Rid = block)
        )
    
    let updateBlock (delta: float32) (block : CollisionObject2D, arg : MoonBlock2D) =
        if block.CanProcess() |> not then Seq.empty else
        
        let rid = block.GetRid()
        let origin = PhysicsServer2D.BodyGetTransform(rid)
        let current = block.GetGlobalTransform ()
        let ct = Transform2D(arg.ConstAngularVelocity * delta, arg.ConstLinearVelocity * delta)
        let shift = ct * current
        
        if shift = origin then Seq.empty else
        
        let platformDir =
            block
            |> Compo.tryFind<MoonPlatform2D>
            |> Option.map (fun p ->
                p.Direction
                |> shift.BasisXform
                |> _.Normalized()
            )
        
        let query = getBlockQuery block
        block.GlobalTransform <- origin
        let originQuery = query.Build ()
        block.GlobalTransform <- shift
        let currentQuery = query.Build ()
        
        // first check overlapped bodies and ignore them later
        
        let getOverlappedInside margin (q : PhysicsShapeQuerier2D) =
            q.QueryInside (margin = margin, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.chooseAndExclude q (fun r ->
                match r.Collider with
                | :? CollisionObject2D as col ->
                    col
                    |> Compo.tryFind<MoonBody2D>
                    |> Option.map (fun _ -> col, r.Rid)
                | _ -> None
            )
            |> Seq.filter (fun (col, _) -> col.CanProcess())
        
        let getOverlapped margin (q : PhysicsShapeQuerier2D) =
            q.Query (margin = margin, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.chooseAndExclude q (fun r ->
                match r.Collider with
                | :? CollisionObject2D as col ->
                    col
                    |> Compo.tryFind<MoonBody2D>
                    |> Option.map (fun b -> (col, b, r.Position), r.Rid)
                | _ -> None
            )
            |> Seq.filter (fun ((col, _, _), _) -> col.CanProcess())
        
        let originExclude =
            originQuery
            |> getOverlappedInside -1e-3f
            |> Seq.map snd
            |> List.ofSeq
        
        originQuery
        |> PhysicsQuery.appendExclude originExclude
        
        currentQuery
        |> PhysicsQuery.appendExclude originExclude
        
        // first snap
        
        let originSnapped =
            originQuery
            |> getOverlapped 1f
            |> Seq.map fst
            |> Seq.choose (fun (col, b, contact) ->
                (col, b)
                |> bodyCheckSnap (rid, arg)
                |> Option.filter (fun v ->
                    match platformDir with
                    | Some dir when dir.Dot v <= 0f -> false
                    | _ -> true
                )
                |> Option.map (fun v -> col, b, v, contact)
            )
            // must build seq here, as pushing will lead to side effect
            |> Array.ofSeq
        
        // push and snap
        
        let currentAf = shift.AffineInverse()
        
        let pushSnapped =
            currentQuery
            |> getOverlapped query.Margin
            |> Seq.choose (fun ((col, b, contact), cid) ->
                // Sample the platform transform at the contact area so
                // rotation contributes the appropriate tangential motion.
                let local = currentAf * contact
                let prev = origin * local
                let motion = prev - contact
                
                // ignore push for platform
                let canPush =
                    match platformDir with
                    | Some dir -> dir.Dot motion > 0f
                    | _ -> true
                
                if canPush |> not then None else

                let q = getBodyQuery col
                let qr = q.Build ()

                let pickOverlappedAt (t : float32) =
                    let transform = shift.InterpolateWith(origin, t)
                    PhysicsServer2D.BodySetTransform(rid, transform)
                    qr.Query (maxResult = b.MaxCollision)
                    |> PhysicsQueryResult.tryPickAndExclude qr (fun r ->
                        if r.Rid = rid then
                            Some r.Normal
                        else
                            None
                    )

                let tryFirstContactNormal () =
                    let result =
                        Math.binarySearchAndPick 16 1e-3f pickOverlappedAt
                        |> snd
                    
                    PhysicsServer2D.BodySetTransform(rid, origin)
                    result

                tryFirstContactNormal ()
                |> Option.filter (fun normal ->
                    match platformDir with
                    | Some d -> d.Dot normal < 0f
                    | None -> true
                )
                |> Option.bind (fun v ->
                    
                    // push through normal
                    
                    PhysicsServer2D.BodySetTransform(rid, shift)
                    let overlapped (push: float32) =
                        qr.QueryInside (offset = v * push, maxResult = b.MaxCollision, margin = 1e-3f)
                        |> PhysicsQueryResult.existsAndExclude qr (fun r ->
                            r.Rid = rid
                        )
                    
                    let _, push = Math.binarySearch 16 1e-3f overlapped
                    let travel = v * push
                    
                    col.GlobalPosition <- col.GlobalPosition + travel
                    PhysicsServer2D.BodySetTransform(cid, col.GlobalTransform)
                    
                    // report crash
                    
                    if arg.CrashBodies then
                        let qr = q.Build ()
                        qr.QueryInside (maxResult = b.MaxCollision)
                        |> Seq.tryFind (fun r ->
                            r |> PhysicsQueryResult.allowTravelWhenCrash |> not
                        )
                        |> Option.iter (fun _ ->
                            b.EmitSignalCrashed ()
                        )
                    
                    PhysicsServer2D.BodySetTransform(rid, origin)
                    
                    b
                    |> bodyGetSnap v
                    |> Option.bind (fun s ->
                        let motion = motion - v * motion.Dot(v)
                        if motion = Vector2.Zero then
                            None
                        else
                            Some (col, b, s, motion)
                    )
                )
            )
            |> Array.ofSeq
        
        // update block's transform is necessary for snap
        
        block.GlobalTransform <- current
        PhysicsServer2D.BodySetTransform(rid, current)
        
        // accumulate snap speed
        
        let remain =
            originSnapped
            |> Seq.filter (fun (col, _, _, _) ->
                pushSnapped
                |> Seq.exists (fun (c, _, _, _) ->
                    col = c
                )
                |> not
            )
        
        seq {
            yield!
                pushSnapped
                |> Seq.choose (fun (col, b, s, motion) ->

                    b.EmitSignalSnapped(block, s)
                    b.SnapMotions <- motion :: b.SnapMotions

                    Some (col, b)
                )
                
            yield!
                remain
                |> Seq.choose (fun (col, b, s, contact) ->
                    b.EmitSignalSnapped(block, s)
                    let local = currentAf * contact
                    let prev = origin * local
                    let motion = contact - prev
                    b.SnapMotions <- motion :: b.SnapMotions

                    Some (col, b)
                )
        }
    
    let updateBody (body : CollisionObject2D, arg : MoonBody2D) =
        let rec getMotion (motions : Vector2 list) =
            if motions |> List.isEmpty then Vector2.Zero else
            
            let motion = motions |> List.head
            let para, remain =
                motions
                |> List.partition (fun v -> v.ParallelTo motion)
                
            let motion = para |> List.sum
            motion + getMotion remain
        
        let query = getBodyQuery body
        let query = query.Build ()
        let motion =
            arg.SnapMotions
            |> List.filter (fun v -> v <> Vector2.Zero)
            |> getMotion

        let travel =
            query.Collide (motion, maxResult = arg.MaxCollision)
            |> Option.map _.Motion.SafeFraction
            |> Option.defaultValue 1f
       
        body.GlobalPosition <- body.GlobalPosition + motion * travel
        arg.SnapMotions <- []

    [<FScript("moon_physics_server_2d")>]
    type MoonPhysicsServer2D(node : Node) =
        
        let update delta =
            blocks
            |> Seq.map (fun b -> b |> updateBlock delta)
            |> Seq.concat
            |> Seq.distinct
            |> Seq.iter updateBody

        do node |> Engine.addPhysicsDelta32Process update |> ignore
