namespace Moon.Physics

open Fodot.Injection
open Fodot.Module.PhysicsServer
open Godot

type IPhysicsQueryResult =
    abstract member Collider : GodotObject with get
    abstract member Rid : Rid with get
    abstract member Shape : int with get

type PhysicsQueryResult =
    {
        Collider : GodotObject
        Rid : Rid
        Shape : int
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Collider
        member this.Rid = this.Rid
        member this.Shape = this.Shape
    
    static member From (result : PhysicsShapeQueryResults2D) =
        seq {
            for i in 0 .. result.GetCollisionCount() - 1 do
                let col = result.GetCollider i
                let rid = result.GetRid i
                let shape = result.GetShape i
                yield {
                    Collider = col
                    Rid = rid
                    Shape = shape
                }
        }
        
    static member From (result : PhysicsShapeQueryResults3D) =
        seq {
            for i in 0 .. result.GetCollisionCount() - 1 do
                let col = result.GetCollider i
                let rid = result.GetRid i
                let shape = result.GetShape i
                yield {
                    Collider = col
                    Rid = rid
                    Shape = shape
                }
        }

type PhysicsQueryRayResult2D =
    {
        Collider : GodotObject
        Rid : Rid
        Shape : int
        Position : Vector2
        Normal : Vector2
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Collider
        member this.Rid = this.Rid
        member this.Shape = this.Shape
    
    static member From (result : PhysicsRayQueryResult2D) =
        {
            Collider = result.GetCollider()
            Rid = result.GetRid()
            Shape = result.GetShape()
            Position = result.GetPosition()
            Normal = result.GetNormal()
        }

type PhysicsQueryRayResult3D =
    {
        Collider : GodotObject
        Rid : Rid
        Shape : int
        Position : Vector3
        Normal : Vector3
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Collider
        member this.Rid = this.Rid
        member this.Shape = this.Shape
    
    static member From (result : PhysicsRayQueryResult3D) =
        {
            Collider = result.GetCollider()
            Rid = result.GetRid()
            Shape = result.GetShape()
            Position = result.GetPosition()
            Normal = result.GetNormal()
        }

type PhysicsQueryShapeResult2D =
    {
        Collider : GodotObject
        Rid : Rid
        Shape : int
        Position : Vector2
        Normal : Vector2
        Velocity : Vector2
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Collider
        member this.Rid = this.Rid
        member this.Shape = this.Shape
    
    static member From (result : PhysicsShapeRestInfo2D) =
        {
            Collider = result.GetColliderId() |> GodotObject.InstanceFromId
            Rid = result.GetRid()
            Shape = result.GetShape()
            Position = result.GetPoint()
            Normal = result.GetNormal()
            Velocity = result.GetLinearVelocity()
        }
        
type PhysicsQueryShapeResult3D =
    {
        Collider : GodotObject
        Rid : Rid
        Shape : int
        Position : Vector3
        Normal : Vector3
        Velocity : Vector3
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Collider
        member this.Rid = this.Rid
        member this.Shape = this.Shape
    
    static member From (result : PhysicsShapeRestInfo3D) =
        {
            Collider = result.GetColliderId() |> GodotObject.InstanceFromId
            Rid = result.GetRid()
            Shape = result.GetShape()
            Position = result.GetPoint()
            Normal = result.GetNormal()
            Velocity = result.GetLinearVelocity()
        }

type PhysicsQueryShapeCastResult2D =
    {
        SafeFraction : float32
        UnsafeFraction : float32
        Collider : GodotObject
        Rid : Rid
        Shape : int
        Position : Vector2
        Normal : Vector2
        Velocity : Vector2
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Collider
        member this.Rid = this.Rid
        member this.Shape = this.Shape
        
    static member From (result : PhysicsShapeCastResult2D) =
        {
            SafeFraction = result.GetClosestSafe()
            UnsafeFraction = result.GetClosestUnsafe()
            Collider = result.GetColliderId() |> GodotObject.InstanceFromId
            Rid = result.GetRid()
            Shape = result.GetShape()
            Position = result.GetPoint()
            Normal = result.GetNormal()
            Velocity = result.GetLinearVelocity()
        }
        
    static member From (result : PhysicsQueryShapeResult2D, ?safe : float32, ?unsafe : float32) =
        {
            SafeFraction = defaultArg safe 0f
            UnsafeFraction = defaultArg unsafe 0f
            Collider = result.Collider
            Rid = result.Rid
            Shape = result.Shape
            Position = result.Position
            Normal = result.Normal
            Velocity = result.Velocity
        }
        
type PhysicsQueryShapeCastResult3D =
    {
        SafeFraction : float32
        UnsafeFraction : float32
        Collider : GodotObject
        Rid : Rid
        Shape : int
        Position : Vector3
        Normal : Vector3
        Velocity : Vector3
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Collider
        member this.Rid = this.Rid
        member this.Shape = this.Shape
        
    static member From (result : PhysicsShapeCastResult3D) =
        {
            SafeFraction = result.GetClosestSafe()
            UnsafeFraction = result.GetClosestUnsafe()
            Collider = result.GetColliderId() |> GodotObject.InstanceFromId
            Rid = result.GetRid()
            Shape = result.GetShape()
            Position = result.GetPoint()
            Normal = result.GetNormal()
            Velocity = result.GetLinearVelocity()
        }
        
    static member From (result : PhysicsQueryShapeResult3D, ?safe : float32, ?unsafe : float32)=
        {
            SafeFraction = defaultArg safe 0f
            UnsafeFraction = defaultArg unsafe 0f
            Collider = result.Collider
            Rid = result.Rid
            Shape = result.Shape
            Position = result.Position
            Normal = result.Normal
            Velocity = result.Velocity
        }

type PhysicsQueryCollisionResult2D =
    {
        Result : PhysicsQueryShapeCastResult2D
        Recovered : bool
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Result.Collider
        member this.Rid = this.Result.Rid
        member this.Shape = this.Result.Shape
        
    static member From (result : PhysicsQueryShapeCastResult2D) = {
        Result = result
        Recovered = false
    }
    
    static member FromRecovered (result : PhysicsQueryShapeCastResult2D) = {
        Result = result
        Recovered = true
    }
    
type PhysicsQueryCollisionResult3D =
    {
        Result : PhysicsQueryShapeCastResult3D
        Recovered : bool
    }
    
    interface IPhysicsQueryResult with
        member this.Collider = this.Result.Collider
        member this.Rid = this.Result.Rid
        member this.Shape = this.Result.Shape
    
    static member From (result : PhysicsQueryShapeCastResult3D) = {
        Result = result
        Recovered = false
    }
    
    static member FromRecovered (result : PhysicsQueryShapeCastResult3D) = {
        Result = result
        Recovered = true
    }
    
module PhysicsQueryResult =

    let getOneWayParameters2D (r : IPhysicsQueryResult) =
        let fallback() =
            if PhysicsServer2D.BodyIsShapeSetAsOneWayCollision(r.Rid, r.Shape) &&
               PhysicsServer2D.ShapeIsOneWayCollisionAllowed(
                    PhysicsServer2D.BodyGetShape(r.Rid, r.Shape)
               ) then
                let margin = PhysicsServer2D.BodyGetShapeOneWayCollisionMargin(r.Rid, r.Shape)
                let dir = PhysicsServer2D.BodyGetShapeOneWayCollisionDirection(r.Rid, r.Shape)
                let bt = PhysicsServer2D.BodyGetTransform(r.Rid)
                let st = PhysicsServer2D.BodyGetShapeTransform(r.Rid, r.Shape)
                let gt = bt * st
                let dir =
                    dir
                    |> gt.BasisXform
                    |> _.Normalized()
                Some (dir, margin)
            else
                None
        
        match r.Collider with
        | :? CollisionObject2D as col ->
            col
            |> Compo.tryFind<MoonPlatform2D>
            |> Option.map(fun p ->
                let margin = p.Margin
                let dir =
                    p.Direction
                    |> col.GlobalTransform.BasisXform
                    |> _.Normalized()
                dir, margin
            )
            |> Option.orElseWith fallback
        | _ -> fallback()
    
    let getOneWayDirection2D (r : IPhysicsQueryResult) =
        r
        |> getOneWayParameters2D
        |> Option.map fst
    
    let getOneWayParameters3D (r : IPhysicsQueryResult) =
        match r.Collider with
        | :? CollisionObject3D as col ->
            col
            |> Compo.tryFind<MoonPlatform3D>
            |> Option.map(fun p ->
                let margin = p.Margin
                let dir =
                    col.GetGlobalTransform().Basis * p.Direction
                    |> _.Normalized()
                dir, margin
            )
            |> Option.orElseWith (fun _ ->
                let owner =
                    r.Shape
                    |> col.ShapeFindOwner
                    |> col.ShapeOwnerGetOwner
                match owner :> obj with
                | :? IPlatformShape3D as p when p.OneWayCollision ->
                    let margin = p.OneWayCollisionMargin
                    let dir = p.OneWayCollisionDirection.Normalized ()
                    let dir =
                        p.GetGlobalTransform().Basis * dir
                        |> _.Normalized()
                    Some (dir, margin)
                | _ ->
                    None
            )
        | _ -> None
    
    let getOneWayDirection3D (r : IPhysicsQueryResult) =
        r
        |> getOneWayParameters3D
        |> Option.map fst
        
    let allowTravelWhenCrash (r : IPhysicsQueryResult) =
        match r.Collider with
        | :? CollisionObject2D as col ->
            col
            |> Compo.tryFind<MoonPlatform2D>
            |> Option.map _.CrashAsTravel
            |> Option.defaultWith (fun _ ->
                let owner =
                    r.Shape
                    |> col.ShapeFindOwner
                    |> col.ShapeOwnerGetOwner
                match owner :> obj with
                | :? IPlatformShape as p ->
                    p.CrashAsTravel
                | _ ->
                    false
            )
        | :? CollisionObject3D as col ->
            col
            |> Compo.tryFind<MoonPlatform3D>
            |> Option.map _.CrashAsTravel
            |> Option.defaultWith (fun _ ->
                let owner =
                    r.Shape
                    |> col.ShapeFindOwner
                    |> col.ShapeOwnerGetOwner
                match owner :> obj with
                | :? IPlatformShape as p ->
                    p.CrashAsTravel
                | _ ->
                    false
            )
        | _ -> false
        
    let chooseAndExclude<'a, 'b when 'a :> IPhysicsQueryResult>
        (query : IPhysicsQuery)
        (pattern : 'a -> 'b option)
        (results : 'a seq) : 'b seq =
        
        results
        |> Seq.choose (fun r ->
            match pattern r with
            | Some s -> Some s
            | None ->
                query |> PhysicsQuery.addExclude r.Rid
                None
        )
        
    let filterAndExclude<'a when 'a :> IPhysicsQueryResult>
        (query : IPhysicsQuery)
        (pattern : 'a -> bool)
        (results : 'a seq) : 'a seq =
        
        results
        |> chooseAndExclude query (fun r ->
            if pattern r then
                Some r
            else
                None
        )
        
    let existsAndExclude<'a when 'a :> IPhysicsQueryResult>
        (query : IPhysicsQuery)
        (pattern : 'a -> bool)
        (results : 'a seq) : bool =
        
        results
        |> filterAndExclude query pattern
        |> Seq.isEmpty |> not
        
    let tryFindAndExclude<'a when 'a :> IPhysicsQueryResult>
        (query : IPhysicsQuery)
        (pattern : 'a -> bool)
        (results : 'a seq) : 'a option =
        
        results
        |> filterAndExclude query pattern
        |> Seq.tryHead
        
    let tryPickAndExclude<'a, 'b when 'a :> IPhysicsQueryResult>
        (query : IPhysicsQuery)
        (pattern : 'a -> 'b option)
        (results : 'a seq) : 'b option =
        
        results
        |> chooseAndExclude query pattern
        |> Seq.tryHead