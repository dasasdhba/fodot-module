namespace Moon.Physics

open FSharp.Generic
open Fodot.Module
open Godot

module PhysicsQueryShape2D =
    
    let getShapes (col : CollisionObject2D) =
        col.GetShapeOwners()
        |> Array.map uint
        |> Array.filter (fun i -> col.IsShapeOwnerDisabled i |> not)
        |> Array.map (fun i ->
            seq {
                for j in 0 .. col.ShapeOwnerGetShapeCount(i) - 1 do
                    let owner = col.ShapeOwnerGetOwner(i)
                    match owner with
                    | :? CanvasItem as c ->
                        let shape = col.ShapeOwnerGetShape(i, j)
                        let gt = c |> CanvasItem.getGlobalTransform
                        yield (shape, gt)
                    | _ -> ()
            }
        )
        |> Seq.concat
        
module PhysicsQueryShape3D =
    
    let getShapes (col : CollisionObject3D) =
        col.GetShapeOwners()
        |> Array.map uint
        |> Array.filter (fun i -> col.IsShapeOwnerDisabled i |> not)
        |> Array.map (fun i ->
            seq {
                for j in 0 .. col.ShapeOwnerGetShapeCount(i) - 1 do
                    let owner = col.ShapeOwnerGetOwner(i)
                    match owner with
                    | :? Node3D as n ->
                        let shape = col.ShapeOwnerGetShape(i, j)
                        let gt = n.GlobalTransform
                        yield (shape, gt)
                    | _ -> ()
            }
        )
        |> Seq.concat

type PhysicsQueryShape2D(node : CollisionObject2D, param : PhysicsQueryBasicParameters) =
    
    let state = PhysicsState2D.Get node
    
    new (node : CollisionObject2D) = PhysicsQueryShape2D(node, PhysicsQueryBasicParameters.FromBody node)
    
    interface IPhysicsQuery with
        member val Param = param with get, set
    
    member val private Col = node
    member val State = state
    member val Margin = -1e-2f with get, set
    member val HitFromInside = false with get, set
    member val MaxResult = 32 with get, set

/// Querier owns an independent PhysicsQueryParam inherited from builder.
/// This allows you to change param without affecting parent or vice versa.
type PhysicsShapeQuerier2D(parent : PhysicsQueryShape2D, shapes: (Shape2D * Transform2D) array) =
    
    let state = parent.State
    
    let restInfo offset (dss : PhysicsDirectSpaceState2D) (query : PhysicsShapeQueryParameters2D) =
        shapes
        |> Array.tryPick (fun (s, gt) ->
            query.Shape <- s
            query.Transform <- gt |> Transform2D.withOrigin (gt.Origin + offset)
            dss.GetRestInfo query
            |> Option.ofObj
            |> Option.map (fun r ->
                let res = PhysicsQueryShapeResult2D.From r
                query.Exclude <-
                    let ex = query.Exclude
                    ex.Add res.Rid
                    ex
                res
            )
        )
    
    interface IPhysicsQuery with
        member val Param = (parent :> IPhysicsQuery).Param with get, set
    
    /// This one is not lazy and contains less information.
    /// You may use Query if lazy enum matters.
    member this.QueryInside (?offset : Vector2, ?maxResult : int, ?margin : float32) =
        state.SpaceState
        |> Option.map (fun dss ->
            let offset = defaultArg offset Vector2.Zero
            let maxResult = defaultArg maxResult parent.MaxResult
            let margin = (defaultArg margin parent.Margin)
            
            seq {
                let query = new PhysicsShapeQueryParameters2D()
                query |> (this :> IPhysicsQuery).Param.Attach
                query.Margin <- margin
                
                for s, gt in shapes do
                    query.Shape <- s
                    query.Transform <- gt |> Transform2D.withOrigin (gt.Origin + offset)
                    let results = dss.IntersectShape(query, maxResult) |> PhysicsQueryResult.From
                    yield! results
                    query.Exclude <-
                        let ex = query.Exclude
                        ex.AddRange (results |> Seq.map _.Rid)
                        ex
            }
            |> Seq.truncate maxResult
        )
        |> Option.defaultValue Seq.empty
    
    member this.Cast (motion : Vector2, ?offset : Vector2, ?maxResult : int, ?margin : float32, ?hitFromInside : bool) =
        state.SpaceState
        |> Option.map (fun dss ->
            let offset = defaultArg offset Vector2.Zero
            let maxResult = defaultArg maxResult parent.MaxResult
            let margin = defaultArg margin parent.Margin
            let hitFromInside = defaultArg hitFromInside parent.HitFromInside
            
            seq {
                let query = new PhysicsShapeQueryParameters2D()
                query |> (this :> IPhysicsQuery).Param.Attach
                query.Margin <- margin
                
                let insideUnfold() =
                    restInfo offset dss query
                    |> Option.map (fun r -> r |> PhysicsQueryShapeCastResult2D.From, ())
                 
                let castUnfold() =
                    shapes
                    |> Seq.choose (fun (shape, transform) ->
                        query.Shape <- shape
                        query.Transform <-
                            transform
                            |> Transform2D.withOrigin (transform.Origin + offset)
                        dss.CastMotion query
                        |> Option.ofObj
                        |> Option.filter (fun r ->
                            r.GetColliderId()
                            |> GodotObject.IsInstanceIdValid
                        )
                    )
                    |> Seq.tryMinBy _.GetClosestSafe()
                    |> Option.map (fun r ->
                        let res = PhysicsQueryShapeCastResult2D.From r
                        query.Exclude <-
                            let ex = query.Exclude
                            ex.Add res.Rid
                            ex
                        res, ()
                    )
                
                if hitFromInside then
                    let inside =
                        this.QueryInside(offset = offset, maxResult = maxResult, margin = margin)
                        |> Seq.map _.Rid
                        |> Array.ofSeq
                    
                    yield!
                        ()
                        |> Seq.unfold insideUnfold
                        |> Seq.truncate maxResult
                        |> Seq.filter (fun r -> inside |> Array.exists (fun x -> x = r.Rid))
                
                if motion <> Vector2.Zero then
                    query.Motion <- motion
                    yield! () |> Seq.unfold castUnfold
            }
            |> Seq.truncate maxResult
        )
        |> Option.defaultValue Seq.empty
    
    member this.Query (?offset : Vector2, ?maxResult : int, ?margin : float32) =
        state.SpaceState
        |> Option.map (fun dss ->
            let offset = defaultArg offset Vector2.Zero
            let maxResult = defaultArg maxResult parent.MaxResult
            let margin = defaultArg margin parent.Margin
            
            seq {
                let query = new PhysicsShapeQueryParameters2D()
                query |> (this :> IPhysicsQuery).Param.Attach
                query.Margin <- margin
                
                let insideUnfold() =
                    restInfo offset dss query
                    |> Option.map (fun r -> r, ())
                
                let inside =
                    this.QueryInside(offset = offset, maxResult = maxResult, margin = margin)
                    |> Seq.map _.Rid
                    |> Array.ofSeq
                    
                yield!
                    ()
                    |> Seq.unfold insideUnfold
                    |> Seq.truncate maxResult
                    |> Seq.filter (fun r -> inside |> Array.exists (fun x -> x = r.Rid))
            }
        )
        |> Option.defaultValue Seq.empty
        
type PhysicsQueryShape2D with

    member this.BuildBy (shapes : (Shape2D * Transform2D) array) =
        PhysicsShapeQuerier2D(this, shapes)
        
    member this.Build ()=
        this.BuildBy (PhysicsQueryShape2D.getShapes this.Col |> Array.ofSeq)
        
type PhysicsQueryShape3D(node : CollisionObject3D, param : PhysicsQueryBasicParameters) =
    
    let state = PhysicsState3D.Get node
    
    new (node : CollisionObject3D) = PhysicsQueryShape3D(node, PhysicsQueryBasicParameters.FromBody node)
    
    interface IPhysicsQuery with
        member val Param = param with get, set
    
    member val private Col = node
    member val State = state
    member val Margin = -0.1f with get, set
    member val HitFromInside = false with get, set
    member val MaxResult = 32 with get, set

/// Querier owns an independent PhysicsQueryParam inherited from builder.
/// This allows you to change param without affecting parent or vice versa.
type PhysicsShapeQuerier3D(parent : PhysicsQueryShape3D, shapes: (Shape3D * Transform3D) array) =
    
    let state = parent.State
    
    let restInfo offset (dss : PhysicsDirectSpaceState3D) (query : PhysicsShapeQueryParameters3D) =
        shapes
        |> Array.tryPick (fun (s, gt) ->
            query.Shape <- s
            query.Transform <- gt |> Transform3D.withOrigin (gt.Origin + offset)
            dss.GetRestInfo query
            |> Option.ofObj
            |> Option.map (fun r ->
                let res = PhysicsQueryShapeResult3D.From r
                query.Exclude <-
                    let ex = query.Exclude
                    ex.Add res.Rid
                    ex
                res
            )
        )
    
    interface IPhysicsQuery with
        member val Param = (parent :> IPhysicsQuery).Param with get, set
    
    /// This one is not lazy and contains less information.
    /// You may use Query if lazy enum matters.
    member this.QueryInside (?offset : Vector3, ?maxResult : int, ?margin : float32) =
        state.SpaceState
        |> Option.map (fun dss ->
            let offset = defaultArg offset Vector3.Zero
            let maxResult = defaultArg maxResult parent.MaxResult
            let margin = (defaultArg margin parent.Margin)
            
            seq {
                let query = new PhysicsShapeQueryParameters3D()
                query |> (this :> IPhysicsQuery).Param.Attach
                query.Margin <- margin
                
                for s, gt in shapes do
                    query.Shape <- s
                    query.Transform <- gt |> Transform3D.withOrigin (gt.Origin + offset)
                    let results = dss.IntersectShape(query, maxResult) |> PhysicsQueryResult.From
                    yield! results
                    query.Exclude <-
                        let ex = query.Exclude
                        ex.AddRange (results |> Seq.map _.Rid)
                        ex
            }
            |> Seq.truncate maxResult
        )
        |> Option.defaultValue Seq.empty
    
    member this.Cast (motion : Vector3, ?offset : Vector3, ?maxResult : int, ?margin : float32, ?hitFromInside : bool) =
        state.SpaceState
        |> Option.map (fun dss ->
            let offset = defaultArg offset Vector3.Zero
            let maxResult = defaultArg maxResult parent.MaxResult
            let margin = defaultArg margin parent.Margin
            let hitFromInside = defaultArg hitFromInside parent.HitFromInside
            
            seq {
                let query = new PhysicsShapeQueryParameters3D()
                query |> (this :> IPhysicsQuery).Param.Attach
                query.Margin <- margin
                
                let insideUnfold() =
                    restInfo offset dss query
                    |> Option.map (fun r -> r |> PhysicsQueryShapeCastResult3D.From, ())
                 
                let castUnfold() =
                    shapes
                    |> Seq.choose (fun (shape, transform) ->
                        query.Shape <- shape
                        query.Transform <-
                            transform
                            |> Transform3D.withOrigin (transform.Origin + offset)
                        dss.CastMotion query
                        |> Option.ofObj
                        |> Option.filter (fun r ->
                            r.GetColliderId()
                            |> GodotObject.IsInstanceIdValid
                        )
                    )
                    |> Seq.tryMinBy _.GetClosestSafe()
                    |> Option.map (fun r ->
                        let res = PhysicsQueryShapeCastResult3D.From r
                        query.Exclude <-
                            let ex = query.Exclude
                            ex.Add res.Rid
                            ex
                        res, ()
                    )
                
                if hitFromInside then
                    let inside =
                        this.QueryInside(offset = offset, maxResult = maxResult, margin = margin)
                        |> Seq.map _.Rid
                        |> Array.ofSeq
                    
                    yield!
                        ()
                        |> Seq.unfold insideUnfold
                        |> Seq.truncate maxResult
                        |> Seq.filter (fun r -> inside |> Array.exists (fun x -> x = r.Rid))
                
                if motion <> Vector3.Zero then
                    query.Motion <- motion
                    yield! () |> Seq.unfold castUnfold
            }
            |> Seq.truncate maxResult
        )
        |> Option.defaultValue Seq.empty
    
    member this.Query (?offset : Vector3, ?maxResult : int, ?margin : float32) =
        state.SpaceState
        |> Option.map (fun dss ->
            let offset = defaultArg offset Vector3.Zero
            let maxResult = defaultArg maxResult parent.MaxResult
            let margin = defaultArg margin parent.Margin
            
            seq {
                let query = new PhysicsShapeQueryParameters3D()
                query |> (this :> IPhysicsQuery).Param.Attach
                query.Margin <- margin
                
                let insideUnfold() =
                    restInfo offset dss query
                    |> Option.map (fun r -> r, ())
                
                let inside =
                    this.QueryInside(offset = offset, maxResult = maxResult, margin = margin)
                    |> Seq.map _.Rid
                    |> Array.ofSeq
                    
                yield!
                    ()
                    |> Seq.unfold insideUnfold
                    |> Seq.truncate maxResult
                    |> Seq.filter (fun r -> inside |> Array.exists (fun x -> x = r.Rid))
            }
        )
        |> Option.defaultValue Seq.empty
    
type PhysicsQueryShape3D with

    member this.BuildBy (shapes : (Shape3D * Transform3D) array) =
        PhysicsShapeQuerier3D(this, shapes)
        
    member this.Build ()=
        this.BuildBy (PhysicsQueryShape3D.getShapes this.Col |> Array.ofSeq)
