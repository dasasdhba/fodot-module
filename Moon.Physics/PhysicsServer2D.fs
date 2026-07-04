namespace Moon.Physics

open FSharp.Extend
open Fodot
open Fodot.Injection
open Fodot.Module.PhysicsServer
open Godot
open Moon
open Moon.Physics.PhysicsCollide
open Moon.Physics.PhysicsMotion

// one should make sure physics server is located
// at the end of game physics process

module private MoonPhysicsServer2D =
    
    let bodies =
        SortedFlushPhysicsNodes<CollisionObject2D, MoonBody2D>()
    
    let blocks =
        SortedFlushPhysicsNodes<CollisionObject2D, MoonBlock2D * Lazy<MoonPlatform2D option>>()
    
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
                let platform = lazy (p |> Compo.tryFind<MoonPlatform2D>)
                let data = p, (arg, platform)
                blocks.Track data
            )
    
    let blockQueries =
        WeakMeta<PhysicsQueryShape2D>()
        
    let getBlockQuery (block : CollisionObject2D) =
        let query =
            blockQueries |> WeakMeta.getOrAdd block (fun () ->
                PhysicsQueryShape2D block
            )
        query |> PhysicsQuery.setCollisionMask block.CollisionMask
        query
    
    let bodyGetSnap (normal: Vector2) (arg : MoonBody2D) =
        let angle = (-normal).Angle()
        arg.Snaps
        |> Seq.choose (fun s -> s |> MoonPhysics2D.snapCheckAngle angle)
        |> Seq.tryMinBy snd
        |> Option.map fst
    
    let bodyCheckSnap
        (block : Rid, brg : MoonBlock2D)
        (body : CollisionObject2D, arg : MoonBody2D) =
        
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
    
    let updateBlock
        (delta: float32)
        (block : CollisionObject2D, data : MoonBlock2D * Lazy<MoonPlatform2D option>) =
        
        let rid = block.GetRid()
        let origin = PhysicsServer2D.BodyGetTransform(rid)
        let current = block.GetGlobalTransform ()
        let arg, platform = data
        let ct = Transform2D(arg.ConstAngularVelocity * delta, arg.ConstLinearVelocity * delta)
        let shift = ct * current
        
        if shift = origin then Seq.empty else
        
        let getPlatformDir (transform: Transform2D) =
            platform.Value
            |> Option.map (fun p ->
                p.Direction
                |> transform.BasisXform
                |> _.Normalized()
            )
        
        let platformDir = getPlatformDir origin
        let platformDirNext = getPlatformDir shift
        
        let query = getBlockQuery block
        block.GlobalTransform <- origin
        let originQuery = query.Build ()
        block.GlobalTransform <- shift
        let currentQuery = query.Build ()
        
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
            |> getOverlapped MoonPhysics2D.blockSnapMargin
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
            (body : CollisionObject2D) (brg :MoonBody2D) (bodyId : Rid)
            (q: PhysicsQueryShape2D) (guess: Vector2) (normal: Vector2) =
            
            PhysicsServer2D.BodySetTransform(rid, shift)
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
            
            let len = max 1f (guess.Length() + MoonPhysics2D.blockPushTolerance)
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
                    pushMotion * MoonPhysics2D.bodyRecoveryScale,
                    maxResult = brg.MaxCollision
                )
                |> Option.map (fun r ->
                    pushMotion * MoonPhysics2D.bodyRecoveryScale * r.SafeFraction
                )
                |> Option.defaultValue pushMotion
            
            // recover the block as we don't need it anymore
            
            PhysicsServer2D.BodySetTransform(rid, origin)
            
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
            
            PhysicsServer2D.BodySetTransform(bodyId, body.GlobalTransform)
            brg.LastPushMotion <- brg.LastPushMotion + pushMotion
            brg.EmitSignalPushed(block, pushMotion)
            if arg.CrashBodies && collide.IsSome then
                // this must be a crash
                brg.EmitSignalCrashed ()
            
            Some snapMotion
        
        let currentAf = shift.AffineInverse()
        
        let currentPushed =
            currentQuery
            |> getOverlapped query.Margin 
            |> Seq.choose (fun ((col, b, contact), cid) ->
                // guess travel by last frame info
                
                let local = currentAf * contact
                let prev = origin * local
                let diff = prev - contact
                
                if diff = Vector2.Zero then None else
                
                let q = MoonPhysics2D.getBodyQuery col
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
                
                |> Option.bind (fun v ->
                    pushThrough col b cid q -diff v
                    |> Option.filter (fun m -> m <> Vector2.Zero)
                    |> Option.map (fun m -> v, m)
                )
                
                |> Option.map (fun (v, motion) ->
                    b
                    |> bodyGetSnap v
                    |> Option.map (fun s -> col, b, s, motion)
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
        
        arg.LastPushed <- lazy (pushOnly |> Array.ofSeq)
        arg.LastSnapped <- lazy (snapped |> Array.map fst)
        
        snapped
    
    let updateBody (body : CollisionObject2D, arg : MoonBody2D) =
        let rec getMotion (motions : Vector2 list) =
            match motions with
            | [] -> Vector2.Zero
            | motion :: _ ->
                let oriented, remaining =
                    motions
                    |> List.partition (fun v -> v.OrientedTo motion)
                (oriented |> List.maxBy _.LengthSquared()) + getMotion remaining
        
        let motion =
            arg.SnapMotions
            |> List.filter (fun v -> v <> Vector2.Zero)
            |> getMotion
            
        let snapMotion =
            body.CastMotion(motion, maxResult = arg.MaxCollision)
            |> fst

        PhysicsServer2D.BodySetTransform(body.GetRid(), body.GlobalTransform)
        arg.LastSnapMotion <- snapMotion
        arg.SnapMotions <- []

    [<FScript("moon_physics_server_2d")>]
    type MoonPhysicsServer2DScript(node : Node) =
        
        let update delta =
            bodies.Flush()
            
            bodies.Iter()
            |> Seq.filter (_.Key >> _.CanProcess())
            |> Seq.map _.Deconstruct()
            |> Seq.iter (fun (body, arg) ->
                MoonPhysics2D.updateBodyCollisionMask body
                PhysicsServer2D.BodySetTransform(body.GetRid(), body.GlobalTransform)
                arg.LastPushMotion <- Vector2.Zero
                arg.LastSnapMotion <- Vector2.Zero
            )
            
            blocks.Flush()
            
            blocks.Iter()
            |> Seq.filter (_.Key >> _.CanProcess())
            |> Seq.map _.Deconstruct() 
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
            node.ProcessPhysicsPriority <- 1000000
            node |> Engine.addPhysicsDelta32Process update |> ignore
