namespace Moon.Physics

open FSharp.Generic
open Fodot
open Fodot.Injection
open Fodot.Module.PhysicsServer
open Godot
open Moon
open Moon.Physics.PhysicsCollide
open Moon.Physics.PhysicsMotion

// one should make sure physics server is located
// at the end of game physics process

module private MoonPhysicsServer3D =

    let bodies =
        SortedFlushPhysicsPool<CollisionObject3D * MoonBody3D>(fun d -> d |> fst :> Node)
    
    let blocks =
        SortedFlushPhysicsPool<CollisionObject3D * (MoonBlock3D * Lazy<MoonPlatform3D option>)>(fun d -> d |> fst :> Node)

    [<FScript(typeof<MoonBody3D>)>]
    type MoonBody3DScript(arg : MoonBody3D) =
        do
            arg
            |> Node.tryGetParent<CollisionObject3D>
            |> Option.iter (fun p ->
                let data = p, arg
                bodies.Track data
            )

    [<FScript(typeof<MoonBlock3D>)>]
    type MoonBlock3DScript(arg : MoonBlock3D) =
        do
            arg
            |> Node.tryGetParent<CollisionObject3D>
            |> Option.iter (fun p ->
                let platform = lazy (p |> Compo.tryFind<MoonPlatform3D>)
                let data = p, (arg, platform)
                blocks.Track data
            )

    let blockQueries =
        WeakMeta<PhysicsQueryShape3D>()

    let getBlockQuery (block : CollisionObject3D) =
        let query =
            blockQueries |> WeakMeta.getOrAdd block (fun () ->
                PhysicsQueryShape3D block
            )
        query |> PhysicsQuery.setCollisionMask block.CollisionMask
        query

    let bodyGetSnap (normal: Vector3) (arg : MoonBody3D) =
        arg.Snaps
        |> Seq.choose (MoonPhysics3D.snapCheckNormal normal)
        |> Seq.tryMinBy snd
        |> Option.map fst

    let bodyCheckSnap
        (block : Rid, brg : MoonBlock3D)
        (body : CollisionObject3D, arg : MoonBody3D) =

        let query = MoonPhysics3D.getBodyQuery body
        let q = query.Build ()

        arg.Snaps
        |> Seq.filter (fun v ->
            brg.InvalidSnapNormals
            |> Seq.exists (fun s ->
                v
                |> MoonPhysics3D.snapCheckNormal s
                |> Option.isSome
            )
            |> not
        )
        |> Seq.map (fun v -> Vector3(v.X, v.Y, v.Z).Normalized())
        |> Seq.tryFind (fun v ->
            q.QueryInside (offset = v, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.existsAndExclude q (fun r -> r.Rid = block)
        )

    let updateBlock
        (delta: float32)
        (block : CollisionObject3D, data : MoonBlock3D * Lazy<MoonPlatform3D option>) =

        let rid = block.GetRid()
        let origin = PhysicsServer3D.BodyGetTransform(rid)
        let current = block.GlobalTransform
        let arg, platform = data
        let ct =
            Transform3D(
                Basis.FromEuler(arg.ConstAngularVelocity * delta),
                arg.ConstLinearVelocity * delta
            )
        let shift = ct * current

        if shift = origin then Seq.empty else

        let getPlatformDir (transform: Transform3D) =
            platform.Value
            |> Option.map (fun p ->
                (transform.Basis * p.Direction).Normalized()
            )

        let platformDir = getPlatformDir origin
        let platformDirNext = getPlatformDir shift

        let query = getBlockQuery block
        block.GlobalTransform <- origin
        let originQuery = query.Build ()
        block.GlobalTransform <- shift
        let currentQuery = query.Build ()

        let getOverlappedInside margin (q : PhysicsShapeQuerier3D) =
            q.QueryInside (margin = margin, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.chooseAndExclude q (fun r ->
                match r.Collider with
                | :? CollisionObject3D as col ->
                    col
                    |> Compo.tryFind<MoonBody3D>
                    |> Option.map (fun _ -> col, r.Rid)
                | _ -> None
            )
            |> Seq.filter (fun (col, _) -> col.CanProcess())

        let getOverlapped margin (q : PhysicsShapeQuerier3D) =
            q.Query (margin = margin, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.chooseAndExclude q (fun r ->
                match r.Collider with
                | :? CollisionObject3D as col ->
                    col
                    |> Compo.tryFind<MoonBody3D>
                    |> Option.map (fun b -> (col, b, r.Position), r.Rid)
                | _ -> None
            )
            |> Seq.filter (fun ((col, _, _), _) -> col.CanProcess())

        // for platform, it's necessary to skip
        // bodies that already inside

        let originExclude =
            match platform.Value with
            | Some p when p.Margin > 0f ->
                originQuery
                |> getOverlappedInside -p.Margin
                |> Seq.map snd
                |> List.ofSeq
            | _ -> []

        originQuery
        |> PhysicsQuery.appendExclude originExclude

        currentQuery
        |> PhysicsQuery.appendExclude originExclude

        // first snap

        let originSnapped =
            originQuery
            |> getOverlapped MoonPhysics3D.blockSnapMargin
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

        let pushThrough
            (body : CollisionObject3D) (brg :MoonBody3D) (bodyId : Rid)
            (q: PhysicsQueryShape3D) (guess: Vector3) (normal: Vector3) =
            
            PhysicsServer3D.BodySetTransform(rid, shift)
            let qr = q.Build ()
            
            // we use a safe margin in recovery and snap stage
            // as an inaccurate normal can fail the recovery
            
            // first, travel though guess from transform change
            
            let guessInside =
                qr.QueryInside (
                    offset = guess,
                    margin = brg.SafeMargin,
                    maxResult = brg.MaxCollision
                )
                |> PhysicsQueryResult.existsAndExclude qr (fun r -> r.Rid = rid)
            
            // then snap on it as possible
            
            let len = max 1f (guess.Length() + MoonPhysics3D.blockPushTolerance)
            let push = normal * len
            
            // if we are already inside, try push out
            
            let recovery() =
                
                // we ignore everything else
                
                let ignored =
                    qr.QueryCollide (push, offset = guess, margin = brg.SafeMargin, maxResult = brg.MaxCollision, hitFromInside = true)
                    |> Seq.filter (fun r -> r.Rid <> rid)
                    |> Seq.map _.Rid
                    |> List.ofSeq
            
                qr |> PhysicsQuery.appendExclude ignored
                
                let travel, normal =
                    qr.PushOut (
                        push,
                        offset = guess,
                        margin = brg.SafeMargin,
                        maxResult = brg.MaxCollision
                    )
                    |> Option.map (fun r -> r.SafeFraction, r.Normal)
                    |> Option.defaultValue (0f, normal)
                
                (push * travel), normal
            
            // otherwise, try cast back on it
            
            let castBack() =
                
                let travel, normal =
                    qr.Cast (
                        -push,
                        offset = guess,
                        margin = brg.SafeMargin,
                        maxResult = brg.MaxCollision
                    )
                    |> Seq.tryFind (fun r -> r.Rid = rid)
                    |> Option.map (fun r -> r.SafeFraction, r.Normal)
                    |> Option.defaultValue (1f, normal)
                
                (-push) * travel, normal
            
            let snap, newNormal =
                if guessInside then recovery () else castBack ()
            
            let normal =
                platformDirNext
                |> Option.map (fun d -> -d)
                |> Option.defaultWith (fun _ ->
                    if newNormal.Dot normal > 0f then
                        newNormal
                    else
                        normal
                )
            
            // next, cancel all sliding motion through normal
            // from now margin can be omitted
            
            let motion = guess + snap
            let pushMotion = normal * (motion.Dot normal)
            let snapMotion = motion - pushMotion
            
            // however normal can still be inaccurate
            // we need another recovery if necessary
            
            let inside =
                qr.QueryInside (
                    offset = pushMotion,
                    maxResult = brg.MaxCollision
                )
                |> Seq.exists (fun r -> r.Rid = rid)
            
            let pushMotion =
                if inside |> not then pushMotion else
                
                qr.PushOut (
                    pushMotion * MoonPhysics3D.bodyRecoveryScale,
                    maxResult = brg.MaxCollision
                )
                |> Option.map (fun r ->
                    pushMotion * MoonPhysics3D.bodyRecoveryScale * r.SafeFraction
                )
                |> Option.defaultValue pushMotion
            
            // recover the block as we don't need it anymore
            
            PhysicsServer3D.BodySetTransform(rid, origin)
            
            // now do real cast
            // ignore everything already inside, including the block
            
            qr |> PhysicsQuery.setExclude [bodyId; rid]
            
            let insides =
                qr.QueryInside (
                    maxResult = brg.MaxCollision
                )
                |> Seq.map _.Rid
                |> List.ofSeq
            
            // and ignore obstacles that can travel through
            
            let ignores =
                qr.QueryCollide (
                    pushMotion,
                    maxResult = brg.MaxCollision
                )
                |> Seq.filter PhysicsQueryResult.allowTravelWhenCrash
                |> Seq.map _.Rid
                |> List.ofSeq
            
            qr |> PhysicsQuery.appendExclude insides
            qr |> PhysicsQuery.appendExclude ignores
            
            let pushMotion, collide =
                body.CastMotionBy(qr, pushMotion, maxResult = brg.MaxCollision)
            
            PhysicsServer3D.BodySetTransform(bodyId, body.GlobalTransform)
            brg.LastPushMotion <- brg.LastPushMotion + pushMotion
            brg.EmitSignalPushed(block, pushMotion)
            if arg.CrashBodies && collide.IsSome then
                // this must be a crash
                brg.EmitSignalCrashed ()
            
            snapMotion
        
        let currentAf = shift.AffineInverse()
        
        let currentPushed =
            currentQuery
            |> getOverlapped query.Margin
            |> Seq.choose (fun ((col, b, contact), cid) ->
                // guess travel by last frame info

                let local = currentAf * contact
                let prev = origin * local
                let diff = prev - contact

                if diff = Vector3.Zero then None else

                let q = MoonPhysics3D.getBodyQuery col
                let qr = q.Build ()
                qr.Cast (motion = diff, maxResult = b.MaxCollision, margin = b.SafeMargin)

                |> PhysicsQueryResult.tryPickAndExclude qr (fun r ->
                    if r.Rid = rid then
                        Some r.Normal
                    else
                        None
                )

                // fallback as -diff when failed or almost opposite

                |> Option.map (fun v ->
                    if v.Dot diff >= 0f then
                        (-diff).Normalized ()
                    else
                        v
                )

                |> Option.defaultWith (fun _ -> (-diff).Normalized())

                |> fun v ->
                    // a platform can only push on it's one way direction

                    match platformDir with
                    | Some dir when dir.Dot v < 0f -> Some (-dir)
                    | Some _ -> None
                    | _ -> Some v

                |> Option.map (fun v ->
                    let m = pushThrough col b cid q -diff v

                    b
                    |> bodyGetSnap v
                    |> Option.map (fun s -> col, b, s, m)
                    |> Option.map Ok
                    |> Option.defaultWith (fun _ -> Result.Error col)
                )
            )
        
        // record snap and push
        
        let pushSnapped, pushOnly =
            currentPushed
            |> Seq.partitionResult
        
        // update block's transform is necessary for snap
        
        block.GlobalTransform <- current
        PhysicsServer3D.BodySetTransform(rid, current)

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
                    if motion = Vector3.Zero then None else

                    b.EmitSignalSnapped(block, s)
                    b.SnapMotions <- motion :: b.SnapMotions

                    Some (col, b)
                )

            yield!
                remain
                |> Seq.choose (fun (col, b, s, contact) ->
                    let local = currentAf * contact
                    let prev = origin * local
                    let motion = contact - prev
                    if motion = Vector3.Zero then None else

                    b.EmitSignalSnapped(block, s)
                    b.SnapMotions <- motion :: b.SnapMotions

                    Some (col, b)
                )
        }

        let snapped = snapped |> Array.ofSeq

        arg.LastPushed <- lazy (pushOnly |> Array.ofSeq)
        arg.LastSnapped <- lazy (snapped |> Array.map fst)

        snapped

    let updateBody (body : CollisionObject3D, arg : MoonBody3D) =
        let rec getMotion (motions : Vector3 list) =
            match motions with
            | [] -> Vector3.Zero
            | motion :: _ ->
                let oriented, remaining =
                    motions
                    |> List.partition (fun v -> v.OrientedTo motion)
                (oriented |> List.maxBy _.LengthSquared()) + getMotion remaining

        let motion =
            arg.SnapMotions
            |> List.filter (fun v -> v <> Vector3.Zero)
            |> getMotion

        let snapMotion =
            body.CastMotion(motion, maxResult = arg.MaxCollision)
            |> fst

        PhysicsServer3D.BodySetTransform(body.GetRid(), body.GlobalTransform)
        arg.LastSnapMotion <- snapMotion
        arg.SnapMotions <- []

    [<FScript("moon_physics_server_3d")>]
    type MoonPhysicsServer3DScript(node : Node) =

        let update delta =
            bodies.Flush()

            bodies.Iter()
            |> Seq.filter (fst >> _.CanProcess())
            |> Seq.iter (fun (body, arg) ->
                MoonPhysics3D.updateBodyCollisionMask body
                PhysicsServer3D.BodySetTransform(body.GetRid(), body.GlobalTransform)
                arg.LastPushMotion <- Vector3.Zero
                arg.LastSnapMotion <- Vector3.Zero
            )

            blocks.Flush()

            blocks.Iter()
            |> Seq.filter (fst >> _.CanProcess())
            |> Seq.map (fun (b, data) ->
                let arg = data |> fst
                arg.LastPushed <- lazy [||]
                arg.LastSnapped <- lazy [||]
                (b, data) |> updateBlock delta
            )
            |> Seq.concat
            |> Array.ofSeq
            |> Seq.distinct
            |> Seq.iter updateBody

        do
            // Moving bodies must finish their own physics movement before
            // blocks inspect origin overlap and apply push/snap motion.
            node.ProcessPhysicsPriority <- 1000
            node |> Engine.addPhysicsDelta32Process update |> ignore
