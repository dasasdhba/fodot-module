namespace Moon.View

open Fodot
open Fodot.Module
open Fodot.Stage
open Godot
open Moon
open Moon.Library
open Moon.Module

type View2DTransformer =
    {
        Transform : Transform2D
        
        /// Calculate the rotation / scale origin, with current center pos as input
        Translation: Vector2 -> Vector2
    }
    
    static member Id =
        {
            Transform = Transform2D.Identity
            Translation = id
        }
    
    static member From(trans: Transform2D, ?origin : Vector2) =
        {
            Transform = trans
            Translation =
                match origin with
                | Some o -> fun _ -> o
                | None -> id
        }
        
    static member FromOffset(offset : Vector2) =
        View2DTransformer.From(
            Transform2D.Identity |> Transform2D.withOrigin offset
        )

type View2D(view : Viewport) =
    
    static let cameraMoved = new StringName "_camera_moved"
    let cameraCall = new StringName $"__cameras_{view.GetViewportRid().Id}"
    let container = view |> Node.tryGetParent<CanvasItem>
    
    let updateView pos (transformers : View2DTransformer seq) =
        
        let transform =
            transformers
            |> Seq.map _.Transform
            |> Seq.fold (*) Transform2D.Identity
        
        // use shader param if possible
        
        let zoom = transform.Scale
        let transZoom =
            container
            |> Option.bind (fun c ->
                c
                |> CanvasItem.tryGetShaderParamAs<Vector2> "zoom"
                |> Option.map (fun _ ->
                    let sz = Vector2(max 1f zoom.X, max 1f zoom.Y)
                    c |> CanvasItem.setShaderParam "zoom" sz
                    Vector2(min 1f zoom.X, min 1f zoom.Y)
                )
            )
            |> Option.defaultValue zoom
        
        let transform = transform |> Transform2D.withScale transZoom
        
        // calc center pos
        
        let center =
            transformers
            |> Seq.fold (fun p t ->
                let o = t.Translation p
                let d = p - o
                o + t.Transform.AffineInverse() * d
            ) pos
        
        let trans = transform * center
        let size = view.GetVisibleRect().Size
        let origin = -trans + size / 2f
        let transform = transform |> Transform2D.withOrigin origin
        
        view.CanvasTransform <- transform
        Engine.callGroup cameraCall cameraMoved (transform, size / 2f, -origin)
        
        // calc real rect
        
        let size = size / zoom
        let topLeft = center - size / 2f
        Rect2(topLeft, size)
    
    let getLimit (region: Rect2) (zoom : Vector2) (pos :Vector2) =
        let size = view.GetVisibleRect().Size / zoom
        let sx, sy = size.X, size.Y
        let rw, rh = region.Size.X, region.Size.Y
        let xu, yu = rw <= 0f || rw < sx, rh <= 0f || rh < sy
        
        let x =
            if xu then pos.X else
            let hx = sx / 2f
            max (region.Position.X + hx) (min (region.End.X - hx) pos.X)
        
        let y =
            if yu then pos.Y else
            let hy = sy / 2f
            max (region.Position.Y + hy) (min (region.End.Y - hy) pos.Y)
                
        Vector2(x, y)
    
    let getLimitZoom (region: Rect2) (minZoom : float32) (zoom : Vector2) =
        let size = view.GetVisibleRect().Size
        let zx, zy = size.X / region.Size.X, size.Y / region.Size.Y
        let mx, my =
            max (max zx zoom.X) minZoom, max (max zy zoom.Y) minZoom
        if mx > my then
            Vector2(mx, zoom.Y / zoom.X * mx)
        else
            Vector2(zoom.X / zoom.Y * my, my)
        
    let travelMargin (current :Vector2) (margin : Rect2) (target :Vector2)=
        let get cur tar left right =
            match cur with
            | c when right >= 0f && tar > c + right -> tar - right
            | c when left <= 0f && tar < c + left -> tar - left
            | _ -> cur
            
        Vector2(
            get current.X target.X margin.Position.X margin.End.X,
            get current.Y target.Y margin.Position.Y margin.End.Y
        )
    
    let mutable position = Vector2.Zero
    let mutable trackingItem : CanvasItem Option = None
    
    let updatePosition() =
        trackingItem
        |> Option.filter GodotObject.IsInstanceValid
        |> Option.iter (fun c -> position <- c |> CanvasItem.getGlobalPosition)
    
    let mutable region = view.GetVisibleRect ()
    let mutable margin = Rect2()
    
    let mutable zoom = Vector2.One
    let mutable minZoom = 1f
    let mutable rotation = 0f
    
    let mutable smoothPositionRate = Some 20.0
    let mutable smoothZoomRate = Some 10.0
    let mutable smoothRotationRate = Some 10.0
    
    let mutable currentPosition = Vector2.Zero
    let mutable currentZoom = Vector2.One
    let mutable currentRotation = 0f
    
    let getCurrentTransformer() =
        let transform = Transform2D(currentRotation, currentZoom, 0f, Vector2.Zero)
        View2DTransformer.From(transform)
    
    let mutable isChanging = false
    let mutable changingFunc : float -> float = id
    let mutable changingTimer = 0.0
    let mutable changingTime = 0.0
    let mutable changingPosition = Vector2.Zero
    let mutable changingZoom = Vector2.One
    
    let changingStarted = Event<unit>()
    let changingFinished = Event<unit>()
    
    let getTrackingTargets (target : Vector2) =
        let targetZoom = zoom |> getLimitZoom region minZoom
        let target =
            target
            |> travelMargin currentPosition margin
            |> getLimit region targetZoom
        target, targetZoom
    
    let forceTrack (target : Vector2) =
        let target, targetZoom = getTrackingTargets target
        currentPosition <- target
        currentZoom <- targetZoom
        currentRotation <- rotation
    
    let track (target : Vector2) (delta : float) =
        let target, targetZoom = getTrackingTargets target
        
        if isChanging then
            changingTimer <- changingTimer + delta
            let t = min 1.0 (changingTimer / changingTime)
            let p = changingFunc t
            currentPosition <- changingPosition.Lerp(target, float32 p)
            currentZoom <- changingZoom.Lerp(targetZoom, float32 p)
            if t >= 1.0 then
                isChanging <- false
                changingFinished.Trigger()
        else
            currentPosition <-
                match smoothPositionRate with
                | Some r ->
                    currentPosition.ConvToward(target, float32 (delta * r))
                | None ->
                    target
                    
            currentZoom <-
                match smoothZoomRate with
                | Some r ->
                    currentZoom.ConvToward(targetZoom, float32 (delta * r))
                | None ->
                    targetZoom
                    
        currentRotation <-
            match smoothRotationRate with
            | Some r ->
                Mathe.MoveTowardAngle(
                    currentRotation,
                    rotation,
                    float32 (delta * r)
                )
            | None ->
                rotation
    
    let mutable currentRect = Rect2()
    
    let forceUpdate () =
        updatePosition ()
        forceTrack position
        currentRect <- updateView currentPosition (getCurrentTransformer() |> Seq.singleton)
    
    let changeRegion (r : Rect2) time func=
        region <- r
        if time <= 0.0 then
            isChanging <- false
            if time < 0.0 then
                forceUpdate ()
        else
            isChanging <- true
            changingTime <- time
            changingTimer <- 0.0
            changingFunc <- func
            changingPosition <- position
            changingZoom <- zoom
            changingStarted.Trigger()
    
    let mutable queuedTransformers : ProcessOption<View2DTransformer> list = []
    
    let update delta =
        updatePosition ()
        track position delta
        
        let remain, transformers =
            queuedTransformers
            |> List.choose (fun f ->
                f.Invoke delta
                |> Option.map (fun t -> f, t)
            )
            |> List.unzip
        
        queuedTransformers <- remain
        let transformers = seq {
            yield getCurrentTransformer()
            yield! transformers
        }
        
        currentRect <- updateView currentPosition transformers
    
    let reset () =
        region <- view.GetVisibleRect ()
        zoom <- Vector2.One
        minZoom <- 1f
        rotation <- 0f
        position <- Vector2.Zero
        trackingItem <- None
        currentPosition <- Vector2.Zero
        currentZoom <- Vector2.One
        currentRotation <- 0f
        forceUpdate ()
    
    do
        view
        |> Node.tryGetStage
        |> Option.filter (fun s -> s.Viewport = view)
        |> Option.iter (fun s ->
            s.SceneCleared.Add reset
        )
    
    static let viewMap = WeakMeta<View2D>()
    
    member private this.Update delta =
        update delta
    
    /// May return None if no valid viewport was found.
    static member tryGet(node : Node) =
        let viewport =
            match node with
            | :? Viewport as v -> Some v
            | n -> n.GetViewport() |> Option.ofObj
        
        viewport
        |> Option.map (fun v ->
            v |> Node.getSubBindingBack viewMap (fun n ->
                let view = View2D(v)
                n |> Engine.addPhysicsDeltaProcess view.Update |> ignore
                view
            )
        )
        
    member this.Region
        with get() = region
        and set v = changeRegion v 0.0 id
        
    member this.Margin
        with get() = margin
        and set v = margin <- v
        
    /// Avoid setting this when TrackingItem is valid,
    /// in which case the position will be overriden.
    member this.Position
        with get() = position
        and set v =
            if trackingItem
               |> Option.filter GodotObject.IsInstanceValid
               |> Option.isSome then
                Logger.pushWarn "View2D: trying to set position when TrackingItem is valid"
            
            position <- v
        
    member this.TrackingItem
        with get() = trackingItem
        and set v = trackingItem <- v
        
    member this.Zoom
        with get() = zoom
        and set v = zoom <- v
        
    member this.MinZoom
        with get() = minZoom
        and set v = minZoom <- v
        
    member this.Rotation
        with get() = rotation
        and set v = rotation <- v
        
    member this.SmoothPositionRate
        with get() = smoothPositionRate
        and set v = smoothPositionRate <- v
        
    member this.SmoothZoomRate
        with get() = smoothZoomRate
        and set v = smoothZoomRate <- v
    
    member this.SmoothRotationRate
        with get() = smoothRotationRate
        and set v = smoothRotationRate <- v
        
    member this.CurrentPosition with get() = currentPosition
    member this.CurrentZoom with get() = currentZoom
    member this.CurrentRotation with get() = currentRotation
    member this.CurrentRect with get() = currentRect
    
    member val RegionChangeStarted = changingStarted.Publish
    member val RegionChangeFinished = changingFinished.Publish
    
    member this.ForceUpdate () =
        forceUpdate ()
    
    /// Use negative time to force change region
    member this.ChangeRegion (r : Rect2, ?time : float, ?func : float -> float) =
        let time = defaultArg time 0.0
        let func = defaultArg func (fun x -> (3.0 - 2.0 * x) * x * x)
        changeRegion r time func
    
    member this.IsChangingRegion () =
        isChanging
    
    /// Given a position p, return the bounded position
    /// considering current's region, zoom, etc.
    /// Will keep identity if view is changing region
    member this.GetLimitedPosition (p : Vector2) =
        if isChanging then
            p
        else
            p |> getLimit region currentZoom
    
    member this.AddTransformer t =
        queuedTransformers <- queuedTransformers @ [t]
        
    member this.AddTransformers ts =
        queuedTransformers <- queuedTransformers @ ts
