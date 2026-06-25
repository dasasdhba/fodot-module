namespace Moon.Script

open Fodot
open Fodot.Module
open Fodot.Stage
open Godot
open Moon.Module

module private StageUI =
    let getTargetRoot (path : string) (stage : Stage) =
        if System.String.IsNullOrWhiteSpace path then
            stage.Root :> Node
        else
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

    let applyTransform (target : CanvasItem) =
        let transform = target |> getStageTransform
        node |> CanvasItem.setGlobalPosition (transform.Origin + bind.Offset)

        if bind.SyncRotation then
            node |> CanvasItem.setRotation transform.Rotation

        if bind.SyncScale then
            node |> CanvasItem.setScale transform.Scale

        if bind.SyncVisibility then
            node.Visible <- target.IsVisibleInTree()

    let update () =
        tracking
        |> Option.filter GodotObject.IsInstanceValid
        |> Option.filter _.IsInsideTree()
        |> Option.iter applyTransform

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
