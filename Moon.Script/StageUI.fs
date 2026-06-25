namespace Moon.Script

open Fodot
open Fodot.Module
open Fodot.Stage
open Godot
open Moon.Component
open Moon.Module

module private StageUI =
    let getTargetRoot (path : string) (stage : Stage) =
        stage.Root
        |> Node.tryGetNode<Node> (new NodePath(path))
        |> Option.defaultValue stage.Root

    let reparentToStage (path : string) (node : Control) =
        node
        |> Node.tryGetStage
        |> Option.iter (fun stage ->
            let root = stage |> getTargetRoot path
            if node.GetParent() <> root then
                node |> Node.reparentKeep root
        )

    let getViewportContainer (viewport : Viewport) =
        viewport |> Node.tryGetParent<SubViewportContainer>

    let getViewportScale (viewport : Viewport) (container : SubViewportContainer) =
        if container.Stretch |> not then
            Vector2.One
        else
            let size = viewport.GetVisibleRect().Size
            if size.X = 0f || size.Y = 0f then
                Vector2.One
            else
                container.Size / size

    let getStageTransform (target : CanvasItem) =
        let viewport = target.GetViewport()
        let targetTransform = target.GetGlobalTransformWithCanvas()

        viewport
        |> Option.ofObj
        |> Option.bind getViewportContainer
        |> Option.map (fun container ->
            let scale = container |> getViewportScale viewport
            let scaleTransform = Transform2D(0f, scale, 0f, Vector2.Zero)
            container.GetGlobalTransform() * scaleTransform * targetTransform
        )
        |> Option.defaultValue targetTransform

    let applyTransform (node : Control) offset syncRotation syncScale syncVisibility (target : CanvasItem) =
        let transform = target |> getStageTransform
        node |> CanvasItem.setGlobalPosition (transform.Origin + offset)

        if syncRotation then
            node |> CanvasItem.setRotation transform.Rotation

        if syncScale then
            node |> CanvasItem.setScale transform.Scale

        if syncVisibility then
            node.Visible <- target.IsVisibleInTree()

    let hide (node : Control) =
        if GodotObject.IsInstanceValid node then
            node.Hide()

    let tryGetPersistantNode (key : string) (root : Node) =
        root
        |> Node.tryGetNode<Control> (new NodePath (key))

    let tryCreatePersistantNode (owner : StagePersistantUI) =
        owner.UiScene
        |> Option.ofObj
        |> Option.map PackedScene.instantiateTo<Control>
        
    let tryGetOrCreatePersistantNode (owner : StagePersistantUI) =
        owner
        |> Node.tryGetStage
        |> Option.bind (fun stage ->
            let root = stage |> getTargetRoot owner.TargetNode

            root
            |> tryGetPersistantNode owner.Key
            |> Option.orElseWith (fun _ ->
                owner
                |> tryCreatePersistantNode
                |> Option.map (fun control ->
                    control.Name <- owner.Key
                    root |> Node.addChild control
                    control
                )
            )
        )

[<FScript("stage_ui")>]
type private StageUIScript(node : Control) =
    let bind = Bind.StageUi.From node
    let parent = node |> Node.tryGetParent<Node>
    let tracking =
        parent
        |> Option.bind (function
            | :? CanvasItem as item -> Some item
            | _ -> None
        )

    let update () =
        tracking
        |> Option.filter GodotObject.IsInstanceValid
        |> Option.filter _.IsInsideTree()
        |> Option.iter (StageUI.applyTransform node bind.Offset bind.SyncRotation bind.SyncScale bind.SyncVisibility)

    do
        parent
        |> Option.iter (fun parent -> parent |> Node.bindNode node)

        node |> Node.whenReady (fun () ->
            node |> StageUI.reparentToStage bind.TargetNode
            tracking |> Option.iter (fun _ -> update ())
        )

        tracking
        |> Option.iter (fun _ ->
            node |> Engine.addProcess bind.PhysicsProcess update |> ignore
        )

[<FScript(typeof<StagePersistantUI>)>]
type private StagePersistantUIScript(marker : StagePersistantUI) =
    let mutable ui : Control option = None

    let ensureUi () =
        ui
        |> Option.filter GodotObject.IsInstanceValid
        |> Option.orElseWith (fun () ->
            marker.UiNode
            |> Option.ofObj
            |> Option.filter GodotObject.IsInstanceValid
            |> Option.map (fun node ->
                ui <- Some node
                node
            )
        )
        |> Option.orElseWith (fun () ->
            ui <- marker |> StageUI.tryGetOrCreatePersistantNode
            marker.UiNode <- ui |> Option.toObj
            ui
        )

    let update () =
        match ensureUi () with
        | Some node when
            GodotObject.IsInstanceValid marker &&
            marker.IsInsideTree() ->
            marker
            |> StageUI.applyTransform
                node
                marker.Offset
                marker.SyncRotation
                marker.SyncScale
                marker.SyncVisibility
        | Some node -> node |> StageUI.hide
        | None -> ()

    do
        marker |> Node.whenReady update
        marker.add_TreeExited (fun () -> ui |> Option.iter StageUI.hide)
        marker |> Engine.addProcess marker.PhysicsProcess update |> ignore
