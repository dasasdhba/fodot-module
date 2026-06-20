namespace Moon.Physics

open Fodot
open Fodot.Injection
open Fodot.Module.PhysicsServer
open Godot
open Moon
open Moon.Physics.PhysicsCollide

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
    
    let snapCheckAngle (angle : float32) (snap : Vector3)=
        let d = Vector2(snap.X, snap.Y).Normalized()
        let a = Mathf.DegToRad snap.Z
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
                |> block.GlobalTransform.BasisXform
                |> _.Normalized()
            )
        
        let query = getBlockQuery block
        block.GlobalTransform <- origin
        let originQuery = query.Build ()
        block.GlobalTransform <- shift
        let currentQuery = query.Build ()
        
        // first check overlapped bodies and ignore them later
        
        let getOverlapped margin (q : PhysicsShapeQuerier2D) =
            q.QueryInside (margin = margin, maxResult = arg.MaxCollision)
            |> PhysicsQueryResult.chooseAndExclude query (fun r ->
                match r.Collider with
                | :? CollisionObject2D as col ->
                    col
                    |> Compo.tryFind<MoonBody2D>
                    |> Option.map (fun b -> (col, b), r.Rid)
                | _ -> None
            )
            |> Seq.filter (fun (col, _) -> col |> fst |> _.CanProcess())
        
        let originExclude =
            originQuery
            |> getOverlapped query.Margin
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
            |> Seq.choose (fun (col, b) ->
                (col, b)
                |> bodyCheckSnap (rid, arg)
                |> Option.filter (fun v ->
                    match platformDir with
                    | Some dir when dir.Dot v <= 0f -> false
                    | _ -> true
                )
                |> Option.map (fun v -> col, b, v)
            )
            // must build seq here, as pushing will lead to side effect
            |> Array.ofSeq
        
        // push and snap
        
        let currentAf = shift.AffineInverse()
        
        let pushSnapped =
            currentQuery
            |> getOverlapped query.Margin
            |> Seq.choose (fun ((col, b), cid) ->
                let local = currentAf * col.GlobalPosition
                let prev = origin * local
                let diff = prev - col.GlobalPosition
                
                // ignore push for platform
                let diff =
                    match platformDir with
                    | Some dir ->
                        let proj = dir.Dot diff
                        if proj > 0f then
                            proj * dir
                        else
                            Vector2.Zero
                    | _ -> diff
                
                if diff = Vector2.Zero then None else
                
                let q = getBodyQuery col
                let qr = q.Build ()
                qr.CastAndQuery (motion = diff, maxResult = b.MaxCollision)
                |> PhysicsQueryResult.tryPickAndExclude qr (fun r ->
                    if r.Result.Rid = rid then
                        Some (r.Result.Normal, cid)
                    else
                        None
                )
                |> Option.bind (fun (v, cid) ->
                    
                    // push through normal
                    
                    PhysicsServer2D.BodySetTransform(rid, shift)
                    let len = max 1f (diff.Length())
                    let mutable push = len
                    let overlapped () =
                        qr.QueryInside (offset = v * push, maxResult = b.MaxCollision)
                        |> PhysicsQueryResult.existsAndExclude qr (fun r ->
                            r.Rid = rid
                        )
                    
                    while overlapped () do
                        push <- push + len
                    
                    let offset = v * push
                    let motion = -v * (push + 1f)
                    let travel =
                        qr.CastAndQuery (motion = motion, offset = offset)
                        |> PhysicsQueryResult.filterAndExclude qr (fun r ->
                            r.Result.Rid = rid
                        )
                        |> Seq.head
                        |> _.Motion.SafeFraction
                    
                    col.GlobalPosition <- col.GlobalPosition + offset + motion * travel
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
                        let motion = -diff
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
            |> Seq.filter (fun (col, _, _) ->
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
                |> Seq.choose (fun (col, b, s) ->
                    b.EmitSignalSnapped(block, s)
                    let local = currentAf * col.GlobalPosition
                    let prev = origin * local
                    let motion = col.GlobalPosition - prev
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