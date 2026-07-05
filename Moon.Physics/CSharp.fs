namespace Moon.CSharp

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.FSharp.Core
open Godot
open Moon.Physics
open Moon.Physics.MoonPhysics
open Moon.Physics.PhysicsCollide
open Moon.Physics.PhysicsMotion

module private PhysicsExtStore =

    let optInt (value : Nullable<int>) =
        if value.HasValue then Some value.Value else None

    let optBool (value : Nullable<bool>) =
        if value.HasValue then Some value.Value else None

    let optSingle (value : Nullable<float32>) =
        if value.HasValue then Some value.Value else None

    let optVector2 (value : Nullable<Vector2>) =
        if value.HasValue then Some value.Value else None

    let optVector3 (value : Nullable<Vector3>) =
        if value.HasValue then Some value.Value else None
        
module PhysicsQueryExt =

    [<Extension>]
    let IsCollideWithAreas (query : IPhysicsQuery) =
        PhysicsQuery.isCollideWithAreas query

    [<Extension>]
    let SetCollideWithAreas (query : IPhysicsQuery) (value : bool) =
        PhysicsQuery.setCollideWithAreas value query

    [<Extension>]
    let IsCollideWithBodies (query : IPhysicsQuery) =
        PhysicsQuery.isCollideWithBodies query

    [<Extension>]
    let SetCollideWithBodies (query : IPhysicsQuery) (value : bool) =
        PhysicsQuery.setCollideWithBodies value query

    [<Extension>]
    let GetCollisionMask (query : IPhysicsQuery) =
        PhysicsQuery.getCollisionMask query

    [<Extension>]
    let SetCollisionMask (query : IPhysicsQuery) (value : uint32) =
        PhysicsQuery.setCollisionMask value query

    [<Extension>]
    let GetCollisionMaskValue (query : IPhysicsQuery) (layer : int) =
        PhysicsQuery.getCollisionMaskValue layer query

    [<Extension>]
    let SetCollisionMaskValue (query : IPhysicsQuery) (layer : int) (value : bool) =
        PhysicsQuery.setCollisionMaskValue value layer query

    [<Extension>]
    let GetExclude (query : IPhysicsQuery) =
        query |> PhysicsQuery.getExclude |> List.toArray

    [<Extension>]
    let SetExclude (query : IPhysicsQuery) (exclude : IEnumerable<Rid>) =
        query |> PhysicsQuery.setExclude (exclude |> Seq.toList)

    [<Extension>]
    let AppendExclude (query : IPhysicsQuery) (exclude : IEnumerable<Rid>) =
        query |> PhysicsQuery.appendExclude (exclude |> Seq.toList)

    [<Extension>]
    let AddExclude (query : IPhysicsQuery) (exclude : Rid) =
        PhysicsQuery.addExclude exclude query

    [<Extension>]
    let RemoveExclude (query : IPhysicsQuery) (exclude : Rid) =
        PhysicsQuery.removeExclude exclude query

    [<Extension>]
    let ClearExclude (query : IPhysicsQuery) =
        PhysicsQuery.clearExclude query

module PhysicsQueryResultExt =

    [<Extension>]
    let GetCollider (result : IPhysicsQueryResult) =
        result.Collider

    [<Extension>]
    let GetRid (result : IPhysicsQueryResult) =
        result.Rid

    [<Extension>]
    let GetShape (result : IPhysicsQueryResult) =
        result.Shape

    [<Extension>]
    let GetOneWayDirection2D (result : IPhysicsQueryResult) =
        match result |> PhysicsQueryResult.getOneWayDirection2D with
        | Some value -> Nullable<Vector2> value
        | None -> Nullable<Vector2>()

    [<Extension>]
    let GetOneWayDirection3D (result : IPhysicsQueryResult) =
        match result |> PhysicsQueryResult.getOneWayDirection3D with
        | Some value -> Nullable<Vector3> value
        | None -> Nullable<Vector3>()

    [<Extension>]
    let AllowTravelWhenCrash (result : IPhysicsQueryResult) =
        result |> PhysicsQueryResult.allowTravelWhenCrash

    [<Extension>]
    let FilterAndExclude<'a when 'a :> IPhysicsQueryResult> (results : IEnumerable<'a>) (query : IPhysicsQuery) (pattern : Func<'a, bool>) =
        results
        |> PhysicsQueryResult.filterAndExclude query pattern.Invoke

    [<Extension>]
    let ExistsAndExclude<'a when 'a :> IPhysicsQueryResult> (results : IEnumerable<'a>) (query : IPhysicsQuery) (pattern : Func<'a, bool>) =
        results
        |> PhysicsQueryResult.existsAndExclude query pattern.Invoke

    [<Extension>]
    let TryFindAndExclude<'a when 'a :> IPhysicsQueryResult and 'a : null> (results : IEnumerable<'a>) (query : IPhysicsQuery) (pattern : Func<'a, bool>) =
        match results |> PhysicsQueryResult.tryFindAndExclude query pattern.Invoke with
        | Some value -> value
        | None -> null

module PhysicsPoint2DQueriesExt =

    [<Extension>]
    let QueryGlobal (query : PhysicsQueryPoint2D) (position : Vector2) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) =
        query.QueryGlobal(position, ?maxResult = PhysicsExtStore.optInt maxResult)

    [<Extension>]
    let Query (query : PhysicsQueryPoint2D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) =
        query.Query(?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult)

module PhysicsPoint3DQueriesExt =

    [<Extension>]
    let QueryGlobal (query : PhysicsQueryPoint3D) (position : Vector3) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) =
        query.QueryGlobal(position, ?maxResult = PhysicsExtStore.optInt maxResult)

    [<Extension>]
    let Query (query : PhysicsQueryPoint3D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) =
        query.Query(?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult)

module PhysicsRaycast2DQueriesExt =

    [<Extension>]
    let QueryGlobal (query : PhysicsQueryRaycast2D) (from : Vector2) (to' : Vector2) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.QueryGlobal(from, to', ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

    [<Extension>]
    let Query (query : PhysicsQueryRaycast2D) (target : Vector2) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.Query(target, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

    [<Extension>]
    let QueryCollideGlobal (query : PhysicsQueryRaycast2D) (from : Vector2) (to' : Vector2) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.QueryCollideGlobal(from, to', ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

    [<Extension>]
    let QueryCollide (query : PhysicsQueryRaycast2D) (target : Vector2) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.QueryCollide(target, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

module PhysicsRaycast3DQueriesExt =

    [<Extension>]
    let QueryGlobal (query : PhysicsQueryRaycast3D) (from : Vector3) (to' : Vector3) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) ([<Optional; DefaultParameterValue(null)>] hitBackFaces : Nullable<bool>) =
        query.QueryGlobal(from, to', ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside, ?hitBackFaces = PhysicsExtStore.optBool hitBackFaces)

    [<Extension>]
    let Query (query : PhysicsQueryRaycast3D) (target : Vector3) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) ([<Optional; DefaultParameterValue(null)>] hitBackFaces : Nullable<bool>) =
        query.Query(target, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside, ?hitBackFaces = PhysicsExtStore.optBool hitBackFaces)

    [<Extension>]
    let QueryCollideGlobal (query : PhysicsQueryRaycast3D) (from : Vector3) (to' : Vector3) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) ([<Optional; DefaultParameterValue(null)>] hitBackFaces : Nullable<bool>) =
        query.QueryCollideGlobal(from, to', ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside, ?hitBackFaces = PhysicsExtStore.optBool hitBackFaces)

    [<Extension>]
    let QueryCollide (query : PhysicsQueryRaycast3D) (target : Vector3) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) ([<Optional; DefaultParameterValue(null)>] hitBackFaces : Nullable<bool>) =
        query.QueryCollide(target, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?hitFromInside = PhysicsExtStore.optBool hitFromInside, ?hitBackFaces = PhysicsExtStore.optBool hitBackFaces)

module PhysicsShapeQuerier2DQueriesExt =

    [<Extension>]
    let QueryInside (query : PhysicsShapeQuerier2D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.QueryInside(?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let Cast (query : PhysicsShapeQuerier2D) (motion : Vector2) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.Cast(motion, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

    [<Extension>]
    let Query (query : PhysicsShapeQuerier2D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.Query(?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let QueryOverlap (query : PhysicsShapeQuerier2D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.QueryOverlap(?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let QueryCollide (query : PhysicsShapeQuerier2D) (motion : Vector2) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.QueryCollide(motion, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

    [<Extension>]
    let PushOut (query : PhysicsShapeQuerier2D) (motion : Vector2) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.PushOut(motion, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let Collide (query : PhysicsShapeQuerier2D) (motion : Vector2) ([<Optional; DefaultParameterValue(null)>] maxDepth : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.Collide(motion, ?maxDepth = PhysicsExtStore.optSingle maxDepth, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

module PhysicsShapeQuerier3DQueriesExt =

    [<Extension>]
    let QueryInside (query : PhysicsShapeQuerier3D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.QueryInside(?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let Cast (query : PhysicsShapeQuerier3D) (motion : Vector3) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.Cast(motion, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

    [<Extension>]
    let Query (query : PhysicsShapeQuerier3D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.Query(?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let QueryOverlap (query : PhysicsShapeQuerier3D) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.QueryOverlap(?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let QueryCollide (query : PhysicsShapeQuerier3D) (motion : Vector3) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] hitFromInside : Nullable<bool>) =
        query.QueryCollide(motion, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin, ?hitFromInside = PhysicsExtStore.optBool hitFromInside)

    [<Extension>]
    let PushOut (query : PhysicsShapeQuerier3D) (motion : Vector3) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.PushOut(motion, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let Collide (query : PhysicsShapeQuerier3D) (motion : Vector3) ([<Optional; DefaultParameterValue(null)>] maxDepth : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        query.Collide(motion, ?maxDepth = PhysicsExtStore.optSingle maxDepth, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

module PhysicsCollisionObject2DQueriesExt =
    
    [<Extension>]
    let GetShapeCast (body : CollisionObject2D) =
        body.GetShapeCast()
        
    [<Extension>]
    let GetRaycast (body : CollisionObject2D) =
        body.GetRaycast()

    [<Extension>]
    let CastMotionBy (body : CollisionObject2D) (query : PhysicsShapeQuerier2D) (motion : Vector2) ([<Optional; DefaultParameterValue(null)>] maxDepth : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        body.CastMotionBy(query, motion, ?maxDepth = PhysicsExtStore.optSingle maxDepth, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let CastMotion (body : CollisionObject2D) (motion : Vector2) ([<Optional; DefaultParameterValue(null)>] maxDepth : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector2>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] updateMask : Nullable<bool>) =
        body.CastMotion(motion, ?maxDepth = PhysicsExtStore.optSingle maxDepth, ?offset = PhysicsExtStore.optVector2 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin, ?updateMask = PhysicsExtStore.optBool updateMask)

module PhysicsCollisionObject3DQueriesExt =
    
    [<Extension>]
    let GetShapeCast (body : CollisionObject3D) =
        body.GetShapeCast()
        
    [<Extension>]
    let GetRaycast (body : CollisionObject3D) =
        body.GetRaycast()
    
    [<Extension>]
    let CastMotionBy (body : CollisionObject3D) (query : PhysicsShapeQuerier3D) (motion : Vector3) ([<Optional; DefaultParameterValue(null)>] maxDepth : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) =
        body.CastMotionBy(query, motion, ?maxDepth = PhysicsExtStore.optSingle maxDepth, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin)

    [<Extension>]
    let CastMotion (body : CollisionObject3D) (motion : Vector3) ([<Optional; DefaultParameterValue(null)>] maxDepth : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] offset : Nullable<Vector3>) ([<Optional; DefaultParameterValue(null)>] maxResult : Nullable<int>) ([<Optional; DefaultParameterValue(null)>] margin : Nullable<float32>) ([<Optional; DefaultParameterValue(null)>] updateMask : Nullable<bool>) =
        body.CastMotion(motion, ?maxDepth = PhysicsExtStore.optSingle maxDepth, ?offset = PhysicsExtStore.optVector3 offset, ?maxResult = PhysicsExtStore.optInt maxResult, ?margin = PhysicsExtStore.optSingle margin, ?updateMask = PhysicsExtStore.optBool updateMask)
