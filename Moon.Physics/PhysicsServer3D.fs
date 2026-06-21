namespace Moon.Physics

open FSharp.Extend
open Fodot
open Fodot.Injection
open Fodot.Module.PhysicsServer
open Godot
open Moon
open Moon.Library
open Moon.Physics.PhysicsCollide
open Moon.Physics.PhysicsMotion

module private MoonPhysicsServer3D =

    let bodies =
        FlushPool<CollisionObject3D * MoonBody3D>(fun (col, _) -> col :> Node)

    let blocks =
        FlushPool<CollisionObject3D * MoonBlock3D>(fun (col, _) -> col :> Node)

    [<FScript(typeof<MoonBody3D>)>]
    type MoonBody3DScript(arg : MoonBody3D) =
        do
            arg
            |> Node.tryGetParent<CollisionObject3D>
            |> Option.iter (fun parent ->
                bodies.Track(parent, arg)
            )

    [<FScript(typeof<MoonBlock3D>)>]
    type MoonBlock3DScript(arg : MoonBlock3D) =
        do
            arg
            |> Node.tryGetParent<CollisionObject3D>
            |> Option.iter (fun parent ->
                blocks.Track(parent, arg)
            )

    let blockQueries =
        WeakMeta<PhysicsQueryShape3D>()

    let getBlockQuery (block : CollisionObject3D) =
        let query =
            blockQueries
            |> WeakMeta.getOrAdd block (lazy (PhysicsQueryShape3D block))
        query |> PhysicsQuery.setCollisionMask block.CollisionMask
        query

    let bodyGetSnap (normal : Vector3) (arg : MoonBody3D) =
        arg.Snaps
        |> Seq.choose (MoonPhysics3D.snapCheckNormal normal)
        |> Seq.tryMinBy snd
        |> Option.map fst

    let bodyCheckSnap
        (blockRid : Rid, blockArg : MoonBlock3D)
        (body : CollisionObject3D, bodyArg : MoonBody3D) =

        let query = MoonPhysics3D.getBodyQuery body
        let q = query.Build()

        bodyArg.Snaps
        |> Seq.filter (fun snap ->
            blockArg.InvalidSnapNormals
            |> Seq.exists (fun normal ->
                snap
                |> MoonPhysics3D.snapCheckNormal normal
                |> Option.isSome
            )
            |> not
        )
        |> Seq.map (fun snap ->
            Vector3(snap.X, snap.Y, snap.Z).Normalized()
        )
        |> Seq.tryFind (fun direction ->
            q.QueryInside(offset = direction, maxResult = bodyArg.MaxCollision)
            |> PhysicsQueryResult.existsAndExclude q (fun r ->
                r.Rid = blockRid
            )
        )

    let updateBlock
        (delta : float32)
        (block : CollisionObject3D, arg : MoonBlock3D) =

        let rid = block.GetRid()
        let origin = PhysicsServer3D.BodyGetTransform(rid)
        let current = block.GlobalTransform
        let change =
            Transform3D(
                Basis.FromEuler(arg.ConstAngularVelocity * delta),
                arg.ConstLinearVelocity * delta
            )
        let shift = change * current

        if shift = origin then Seq.empty else

        let platform =
            block |> Compo.tryFind<MoonPlatform3D>

        let platformDir =
            platform
            |> Option.map (fun p ->
                (shift.Basis * p.Direction).Normalized()
            )

        let query = getBlockQuery block
        block.GlobalTransform <- origin
        let originQuery = query.Build()
        block.GlobalTransform <- shift
        let currentQuery = query.Build()

        let getOverlappedInside margin (q : PhysicsShapeQuerier3D) =
            q.QueryInside(margin = margin, maxResult = arg.MaxCollision)
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
            q.Query(margin = margin, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.chooseAndExclude q (fun r ->
                match r.Collider with
                | :? CollisionObject3D as col ->
                    col
                    |> Compo.tryFind<MoonBody3D>
                    |> Option.map (fun body ->
                        (col, body, r.Position, r.Normal), r.Rid
                    )
                | _ -> None
            )
            |> Seq.filter (fun ((col, _, _, _), _) -> col.CanProcess())

        let originInsideMargin =
            platform
            |> Option.map (fun p ->
                -(max p.Margin MoonPhysics3D.binaryEps)
            )
            |> Option.defaultValue -MoonPhysics3D.binaryEps

        let originExclude =
            originQuery
            |> getOverlappedInside originInsideMargin
            |> Seq.map snd
            |> List.ofSeq

        let originBlocking =
            match platform with
            | Some p ->
                let direction =
                    (origin.Basis * p.Direction).Normalized()
                originQuery
                |> getOverlapped MoonPhysics3D.blockSnapMargin
                |> Seq.choose (fun ((_, _, _, normal), bodyRid) ->
                    if originExclude |> List.contains bodyRid ||
                       direction.Dot(normal) >= 0f then
                        None
                    else
                        Some bodyRid
                )
                |> Seq.distinct
            | None ->
                Seq.empty

        originQuery |> PhysicsQuery.appendExclude originExclude
        currentQuery |> PhysicsQuery.appendExclude originExclude

        let originSnapped =
            originQuery
            |> getOverlapped MoonPhysics3D.blockSnapMargin
            |> Seq.map fst
            |> Seq.choose (fun (col, bodyArg, contact, _) ->
                (col, bodyArg)
                |> bodyCheckSnap (rid, arg)
                |> Option.filter (fun direction ->
                    match platformDir with
                    | Some p when p.Dot(direction) <= 0f -> false
                    | _ -> true
                )
                |> Option.map (fun direction ->
                    col, bodyArg, direction, contact
                )
            )
            |> Array.ofSeq

        let currentInverse = shift.AffineInverse()

        // Queries must be materialized before first-contact searches mutate
        // the block transform in PhysicsServer3D.
        let currentOverlapped =
            currentQuery
            |> getOverlapped query.Margin
            |> Array.ofSeq

        let currentPushed =
            currentOverlapped
            |> Seq.choose (fun ((col, bodyArg, contact, finalNormal), bodyRid) ->
                let local = currentInverse * contact
                let previous = origin * local
                let motion = previous - contact

                let canPush =
                    match platformDir with
                    | Some direction ->
                        direction.Dot(motion) > 0f ||
                        originBlocking |> Seq.contains bodyRid
                    | None ->
                        true

                if not canPush then None else

                let bodyQuery = MoonPhysics3D.getBodyQuery col
                let qr = bodyQuery.Build()

                let pickNormalAt (t : float32) =
                    let transform = shift.InterpolateWith(origin, t)
                    PhysicsServer3D.BodySetTransform(rid, transform)
                    qr.Query(maxResult = bodyArg.MaxCollision)
                    |> PhysicsQueryResult.tryPickAndExclude qr (fun r ->
                        if r.Rid = rid then Some r.Normal else None
                    )

                let contactSearch =
                    match pickNormalAt 1f, platform with
                    | Some normal, Some _ ->
                        Some ((1f, 1f), Some normal)
                    | Some _, None ->
                        None
                    | None, _ ->
                        Some (MoonPhysics3D.binarySearchAndPick pickNormalAt)

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
                        $"MoonPhysicsServer3D: platform skipped as no contact normal was found, block={rid}, body={bodyRid}, contact={contact}"

                resolvedContact
                |> Option.bind (fun (search, contactNormal) ->
                    PhysicsServer3D.BodySetTransform(rid, shift)

                    let (_, contactOutside), _ = search
                    let contactTransform =
                        shift.InterpolateWith(origin, contactOutside)
                    let firstContact = contactTransform * local
                    let surfaceMotion = contact - firstContact

                    let overlapped (direction : Vector3) (push : float32) =
                        qr.QueryInside(
                            offset = push * direction,
                            maxResult = bodyArg.MaxCollision,
                            margin = MoonPhysics3D.binaryEps
                        )
                        |> PhysicsQueryResult.existsAndExclude qr (fun r ->
                            r.Rid = rid
                        )

                    let maxPush =
                        surfaceMotion.Length() +
                        MoonPhysics3D.blockPushTolerance

                    let tryDirection (direction : Vector3) =
                        if direction.Dot(surfaceMotion) <= MoonPhysics3D.binaryEps ||
                           overlapped direction maxPush then
                            None
                        else
                            let _, push =
                                MoonPhysics3D.binarySearch (fun t ->
                                    overlapped direction (t * maxPush)
                                )
                            Some (direction, push * maxPush)

                    let push =
                        tryDirection contactNormal
                        |> Option.orElseWith (fun _ ->
                            tryDirection -contactNormal
                        )
                        |> Option.orElseWith (fun _ ->
                            tryDirection finalNormal
                        )
                        |> Option.orElseWith (fun _ ->
                            tryDirection -finalNormal
                        )

                    match push with
                    | None ->
                        Logger.pushWarn
                            $"MoonPhysicsServer3D: push rejected: rid={rid}, contactNormal={contactNormal}, finalNormal={finalNormal}, surfaceMotion={surfaceMotion}, maxPush={maxPush}"
                        None
                    | Some (pushDirection, pushDistance) ->
                        let travel = pushDirection * pushDistance

                        let qr = bodyQuery.Build()
                        qr |> PhysicsQuery.addExclude rid

                        let inside =
                            qr.QueryInside(
                                maxResult = bodyArg.MaxCollision,
                                margin = bodyArg.SafeMargin
                            )
                            |> Seq.map _.Rid
                            |> List.ofSeq

                        let skipped =
                            qr.QueryCollide (
                                motion = travel,
                                margin = bodyArg.SafeMargin,
                                maxResult = bodyArg.MaxCollision
                            )
                            |> Seq.filter PhysicsQueryResult.allowTravelWhenCrash
                            |> Seq.map _.Rid
                            |> List.ofSeq

                        qr |> PhysicsQuery.appendExclude inside
                        qr |> PhysicsQuery.appendExclude skipped

                        let pushMotion, result =
                            col.CastMotionBy(
                                qr,
                                travel,
                                margin = bodyArg.SafeMargin,
                                maxResult = bodyArg.MaxCollision
                            )

                        PhysicsServer3D.BodySetTransform(bodyRid, col.GlobalTransform)
                        bodyArg.LastPushMotion <- bodyArg.LastPushMotion + pushMotion
                        bodyArg.EmitSignalPushed(block, pushMotion)

                        // report crash
                        
                        if arg.CrashBodies && (result |> Option.isSome) then
                            bodyArg.EmitSignalCrashed()

                        PhysicsServer3D.BodySetTransform(rid, origin)

                        bodyArg
                        |> bodyGetSnap pushDirection
                        |> Option.bind (fun snap ->
                            let snapMotion =
                                motion - pushDirection * motion.Dot(pushDirection)
                            if snapMotion = Vector3.Zero then
                                None
                            else
                                Ok (col, bodyArg, snap, snapMotion)
                                |> Some
                        )
                        |> Option.orElseWith (fun _ ->
                            Result.Error col |> Some
                        )
                )
            )
            |> List.ofSeq

        block.GlobalTransform <- current
        PhysicsServer3D.BodySetTransform(rid, current)

        let pushSnapped, pushOnly =
            currentPushed |> List.partition Result.isOk

        let pushSnapped =
            pushSnapped
            |> List.choose (function Ok x -> Some x | _ -> None)

        let pushOnly =
            pushOnly
            |> List.choose (function Error x -> Some x | _ -> None)

        let remain =
            originSnapped
            |> Seq.filter (fun (col, _, _, _) ->
                pushSnapped
                |> Seq.exists (fun (pushed, _, _, _) -> pushed = col)
                |> not
            )

        let snapped = seq {
            yield!
                pushSnapped
                |> Seq.map (fun (col, bodyArg, snap, motion) ->
                    bodyArg.EmitSignalSnapped(block, snap)
                    bodyArg.SnapMotions <-
                        motion :: bodyArg.SnapMotions
                    col, bodyArg
                )

            yield!
                remain
                |> Seq.map (fun (col, bodyArg, snap, contact) ->
                    bodyArg.EmitSignalSnapped(block, snap)
                    let local = currentInverse * contact
                    let previous = origin * local
                    let motion = contact - previous
                    bodyArg.SnapMotions <-
                        motion :: bodyArg.SnapMotions
                    col, bodyArg
                )
        }

        let snapped = snapped |> Array.ofSeq
        arg.LastPushed <- lazy (pushOnly |> Array.ofList)
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
            |> List.filter ((<>) Vector3.Zero)
            |> getMotion

        let snapMotion =
            body.CastMotion(
                motion,
                margin = arg.SafeMargin,
                maxResult = arg.MaxCollision
            )
            |> fst

        PhysicsServer3D.BodySetTransform(body.GetRid(), body.GlobalTransform)
        arg.LastSnapMotion <- snapMotion
        arg.SnapMotions <- []

    [<FScript("moon_physics_server_3d")>]
    type MoonPhysicsServer3DScript(node : Node) =

        let update delta =
            bodies.Flush()

            bodies.Iter()
            |> Seq.filter (fun (body, _) -> body.CanProcess())
            |> Seq.iter (fun (body, arg) ->
                MoonPhysics3D.updateBodyCollisionMask body
                PhysicsServer3D.BodySetTransform(
                    body.GetRid(),
                    body.GlobalTransform
                )
                arg.LastPushMotion <- Vector3.Zero
                arg.LastSnapMotion <- Vector3.Zero
            )

            blocks.Flush()

            blocks.Iter()
            |> Seq.filter (fun (block, _) -> block.CanProcess())
            |> Seq.map (fun (block, arg) ->
                arg.LastPushed <- lazy [||]
                arg.LastSnapped <- lazy [||]
                updateBlock delta (block, arg)
            )
            |> Seq.concat
            |> Array.ofSeq
            |> Seq.distinct
            |> Seq.iter updateBody

        do
            node.ProcessPhysicsPriority <- 1000000
            node |> Engine.addPhysicsDelta32Process update |> ignore
