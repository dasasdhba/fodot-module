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
    
    let bodies =
        ResizeArray<CollisionObject2D * MoonBody2D>()
    let blocks =
        ResizeArray<CollisionObject2D * MoonBlock2D>()
    
    [<FScript(typeof<CollisionObject2D>)>]
    type MoonPhysicsObject2D(col : CollisionObject2D) =
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
        let rid = block.GetRid()
        let origin = PhysicsServer2D.BodyGetTransform(rid)
        let current = block.GetGlobalTransform ()
        let ct = Transform2D(arg.ConstAngularVelocity * delta, arg.ConstLinearVelocity * delta)
        let shift = ct * current
        
        if shift = origin then Seq.empty else
        
        let platform =
            block
            |> Compo.tryFind<MoonPlatform2D>

        let platformDir =
            platform
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
                    |> Option.map (fun b ->
                        (col, b, r.Position, r.Normal), r.Rid
                    )
                | _ -> None
            )
            |> Seq.filter (fun ((col, _, _, _), _) -> col.CanProcess())
        
        let originInsideMargin =
            platform
            |> Option.map (fun p -> -(max p.Margin 1e-3f))
            |> Option.defaultValue -1e-3f

        let originExclude =
            originQuery
            |> getOverlappedInside originInsideMargin
            |> Seq.map snd
            |> List.ofSeq

        let originBlocking =
            match platform with
            | Some p ->
                let dir =
                    p.Direction
                    |> origin.BasisXform
                    |> _.Normalized()
                originQuery
                |> getOverlapped 1f
                |> Seq.choose (fun ((_, _, _, normal), bodyRid) ->
                    if originExclude |> List.contains bodyRid ||
                       dir.Dot normal >= 0f then
                        None
                    else
                        Some bodyRid
                )
                |> Seq.distinct
            | None ->
                Seq.empty

        originQuery
        |> PhysicsQuery.appendExclude originExclude
        
        currentQuery
        |> PhysicsQuery.appendExclude originExclude
        
        // first snap
        
        let originSnapped =
            originQuery
            |> getOverlapped 1f
            |> Seq.map fst
            |> Seq.choose (fun (col, b, contact, _) ->
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
        
        // Materialize every final-transform contact before the push loop.
        // The loop temporarily moves the platform while searching first
        // contact, so leaving this sequence lazy corrupts later rest_info
        // results with an arbitrary intermediate platform transform.
        let currentOverlapped =
            currentQuery
            |> getOverlapped query.Margin
            |> Array.ofSeq

        let currentPushed =
            currentOverlapped
            |> Seq.choose (fun ((col, b, contact, finalNormal), cid) ->
                // Sample the platform transform at the contact area so
                // rotation contributes the appropriate tangential motion.
                let local = currentAf * contact
                let prev = origin * local
                let motion = prev - contact
                
                // ignore push for platform
                let canPush =
                    match platformDir with
                    | Some dir ->
                        dir.Dot motion > 0f ||
                        originBlocking |> Seq.contains cid
                    | _ -> true
                
                if canPush |> not then None else

                let q = getBodyQuery col
                let qr = q.Build ()

                let pickNormalAt (t : float32) =
                    let transform = shift.InterpolateWith(origin, t)
                    PhysicsServer2D.BodySetTransform(rid, transform)
                    qr.Query (maxResult = b.MaxCollision)
                    |> PhysicsQueryResult.tryPickAndExclude qr (fun r ->
                        if r.Rid = rid then
                            Some r.Normal
                        else
                            None
                    )
                
                // binarySearchAndPick expands past 1 when pick(1) succeeds.
                // First contact is only meaningful between shift and origin,
                // so do not extrapolate. For one-way platforms, a body that
                // reaches this point was not deeply overlapped enough for
                // originExclude; treat that shallow residual contact as a
                // contact beginning at the origin transform.
                let contactSearch =
                    match pickNormalAt 1f, platform with
                    | Some normal, Some _ ->
                        Some ((1f, 1f), Some normal)
                    | Some _, None ->
                        None
                    | None, _ ->
                        Some (Math.binarySearchAndPick 16 1e-3f pickNormalAt)

                let resolvedContact =
                    contactSearch
                    |> Option.bind (fun search ->
                        search
                        |> snd
                        |> Option.map (fun normal -> search, normal)
                    )

                if platform |> Option.isSome &&
                   resolvedContact |> Option.isNone &&
                   (contactSearch |> Option.exists (snd >> Option.isNone)) then
                    Logger.pushWarn
                        $"physics platform skipped: stage=no-contact-normal, block={rid}, body={cid}, contact={contact}"

                resolvedContact
                |> Option.bind (fun (contactSearch, v) ->
                    
                    // push through normal
                    
                    PhysicsServer2D.BodySetTransform(rid, shift)

                    // A valid push cannot be farther than the contact surface
                    // advanced along its normal since first contact. Without
                    // this bound, a bad normal can search through the whole
                    // block and find the opposite side as the first exit.
                    let (_, contactOutside), _ = contactSearch
                    let contactTransform =
                        shift.InterpolateWith(origin, contactOutside)
                    let firstContact = contactTransform * local
                    let surfaceMotion = contact - firstContact
                    let overlapped (dir: Vector2) (push: float32) =
                        qr.QueryInside (offset = push * dir, maxResult = b.MaxCollision, margin = 1e-3f)
                        |> PhysicsQueryResult.existsAndExclude qr (fun r ->
                            r.Rid = rid
                        )

                    let maxPush = surfaceMotion.Length() + 1f
                    let tryDirection (dir: Vector2) =
                        if dir.Dot surfaceMotion <= 1e-3f ||
                           overlapped dir maxPush then
                            None
                        else
                            let _, push =
                                Math.binarySearch 16 1e-3f (fun t ->
                                    overlapped dir (t * maxPush)
                                )
                            Some (dir, push * maxPush)

                    // rest_info can occasionally return the opposite member
                    // of the contact pair around corners/rotation. Preserve
                    // the normal as the primary direction and only try its
                    // inverse when the primary direction cannot separate
                    // within this contact point's finite displacement.
                    let push =
                        tryDirection v
                        |> Option.orElseWith (fun _ -> tryDirection -v)
                        |> Option.orElseWith (fun _ ->
                            tryDirection finalNormal
                        )
                        |> Option.orElseWith (fun _ ->
                            tryDirection -finalNormal
                        )

                    match push with
                    | None ->
                        Logger.pushWarn
                            $"physics push rejected: rid={rid}, contactNormal={v}, finalNormal={finalNormal}, surfaceMotion={surfaceMotion}, maxPush={maxPush}"
                        None
                    | Some (pushDir, push) ->
                        let travel = pushDir * push

                        col.GlobalPosition <- col.GlobalPosition + travel
                        PhysicsServer2D.BodySetTransform(cid, col.GlobalTransform)
                        b.LastPushMotion <- b.LastPushMotion + travel
                        b.EmitSignalPushed(block, travel)

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
                        |> bodyGetSnap pushDir
                        |> Option.bind (fun s ->
                            let motion =
                                motion - pushDir * motion.Dot(pushDir)
                            if motion = Vector2.Zero then
                                None
                            else
                                (col, b, s, motion) |> Ok |> Some
                        )
                        |> Option.orElseWith (fun _ ->
                            col |> Result.Error |> Some
                        )
                )
            )
            |> List.ofSeq
        
        // update block's transform is necessary for snap
        
        block.GlobalTransform <- current
        PhysicsServer2D.BodySetTransform(rid, current)
        
        // record snap and push
        
        let pushSnapped, pushOnly =
            currentPushed
            |> List.partition Result.isOk
            
        let pushSnapped =
            pushSnapped
            |> List.choose (function Ok x -> Some x | _ -> None)
        
        let pushOnly =
            pushOnly
            |> List.choose (function Error e -> Some e | _ -> None)
        
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
        
        let snapped = seq {
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
        
        let snapped = snapped |> Array.ofSeq
        
        arg.LastPushed <- lazy (pushOnly |> Array.ofList)
        arg.LastSnapped <- lazy (snapped |> Array.map fst)
        
        snapped
    
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
            query.Collide (motion, margin = arg.SafeMargin, maxResult = arg.MaxCollision)
            |> Option.map _.Result.SafeFraction
            |> Option.defaultValue 1f
       
        let snapMotion = motion * travel
        body.GlobalPosition <- body.GlobalPosition + snapMotion
        arg.LastSnapMotion <- snapMotion
        arg.SnapMotions <- []

    [<FScript("moon_physics_server_2d")>]
    type MoonPhysicsServer2D(node : Node) =
        
        let update delta =
            bodies
            |> Seq.filter (fun (body, _) -> body.CanProcess())
            |> Seq.iter (fun (body, arg) ->
                PhysicsServer2D.BodySetTransform(body.GetRid(), body.GlobalTransform)
                arg.LastPushMotion <- Vector2.Zero
                arg.LastSnapMotion <- Vector2.Zero
            )
            
            blocks
            |> Seq.filter (fun (b, _) -> b.CanProcess()) 
            |> Seq.map (fun (b, arg) ->
                arg.LastPushed <- lazy [||]
                arg.LastSnapped <- lazy [||]
                (b, arg) |> updateBlock delta
            )
            |> Seq.concat
            |> Seq.distinct
            |> Seq.iter updateBody

        do
            // Moving bodies must finish their own physics movement before
            // blocks inspect origin overlap and apply push/snap motion.
            node.ProcessPhysicsPriority <- 1000000
            node |> Engine.addPhysicsDelta32Process update |> ignore
