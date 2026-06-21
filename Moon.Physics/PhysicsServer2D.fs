namespace Moon.Physics

open FSharp.Extend
open Fodot
open Fodot.Injection
open Fodot.Module.PhysicsServer
open Godot
open Moon
open Moon.Library
open Moon.Physics.PhysicsMotion

// one should make sure physics server is located
// at the end of game physics process

module private MoonPhysicsServer2D =
    
    let bodies =
        FlushPool<CollisionObject2D * MoonBody2D>(fun (col, _) -> col :> Node)
    let blocks =
        FlushPool<CollisionObject2D * MoonBlock2D>(fun (col, _) -> col :> Node)
    
    [<FScript(typeof<MoonBody2D>)>]
    type MoonBody2DScript(arg : MoonBody2D) =
        do
            arg
            |> Node.tryGetParent<CollisionObject2D>
            |> Option.iter (fun p ->
                let data = p, arg
                bodies.Track data
            )
    
    [<FScript(typeof<MoonBlock2D>)>]
    type MoonBlock2DScript(arg : MoonBlock2D) =
        do
            arg
            |> Node.tryGetParent<CollisionObject2D>
            |> Option.iter (fun p ->
                let data = p, arg
                blocks.Track data
            )
    
    let blockQueries =
        WeakMeta<PhysicsQueryShape2D>()
        
    let getBlockQuery (block : CollisionObject2D) =
        let query =
            blockQueries |> WeakMeta.getOrAdd block (lazy (
                PhysicsQueryShape2D block
            ))
        query |> PhysicsQuery.setCollisionMask block.CollisionMask
        query
    
    let bodyGetSnap (normal: Vector2) (arg : MoonBody2D) =
        let angle = (-normal).Angle()
        arg.Snaps
        |> Seq.choose (fun s -> s |> MoonPhysics2D.snapCheckAngle angle)
        |> Seq.tryMinBy snd
        |> Option.map fst
    
    let bodyCheckSnap (block : Rid, brg : MoonBlock2D) (body : CollisionObject2D, arg : MoonBody2D) =
        let query = MoonPhysics2D.getBodyQuery body
        let q = query.Build ()
        arg.Snaps
        |> Seq.filter (fun v ->
            brg.InvalidSnapNormals
            |> Seq.exists (fun s ->
                v
                |> MoonPhysics2D.snapCheckNormal s
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
                |> getOverlapped MoonPhysics2D.blockSnapMargin
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
            |> getOverlapped MoonPhysics2D.blockSnapMargin
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

                let q = MoonPhysics2D.getBodyQuery col
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
                        Some (MoonPhysics2D.binarySearchAndPick pickNormalAt)

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
                        $"MoonPhysicsServer2D: physics platform skipped as no contact normal was found, block={rid}, body={cid}, contact={contact}"

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

                    let maxPush =
                        surfaceMotion.Length() +
                        MoonPhysics2D.blockPushTolerance
                    let tryDirection (dir: Vector2) =
                        if dir.Dot surfaceMotion <= 1e-3f ||
                           overlapped dir maxPush then
                            None
                        else
                            let _, push =
                                MoonPhysics2D.binarySearch (fun t ->
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
                            $"MoonPhysicsServer2D: physics push rejected: rid={rid}, contactNormal={v}, finalNormal={finalNormal}, surfaceMotion={surfaceMotion}, maxPush={maxPush}"
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
        
        let motion =
            arg.SnapMotions
            |> List.filter (fun v -> v <> Vector2.Zero)
            |> getMotion

        let snapMotion =
            body.CastMotion(motion, margin = arg.SafeMargin, maxResult = arg.MaxCollision)
            |> fst
        arg.LastSnapMotion <- snapMotion
        arg.SnapMotions <- []

    [<FScript("moon_physics_server_2d")>]
    type MoonPhysicsServer2DScript(node : Node) =
        
        let update delta =
            bodies.Flush()
            
            bodies.Iter()
            |> Seq.filter (fun (body, _) -> body.CanProcess())
            |> Seq.iter (fun (body, arg) ->
                MoonPhysics2D.updateBodyCollisionMask body
                PhysicsServer2D.BodySetTransform(body.GetRid(), body.GlobalTransform)
                arg.LastPushMotion <- Vector2.Zero
                arg.LastSnapMotion <- Vector2.Zero
            )
            
            blocks.Flush()
            
            blocks.Iter()
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
