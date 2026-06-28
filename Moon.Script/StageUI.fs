namespace Moon.Script

open Fodot
open Fodot.Extend
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
        let stage = node |> Node.getStage
        let root = stage |> getTargetRoot path
        node |> Node.reparentKeep root

    let applyTransform2D (node : Control) offset syncRotation syncScale syncVisibility (target : CanvasItem) =
        let transform = target |> CanvasItem.getGlobalTransformWithViewport
        node |> CanvasItem.setGlobalPosition (transform.Origin + offset)

        if syncRotation then
            node |> CanvasItem.setGlobalRotation transform.Rotation

        if syncScale then
            node |> CanvasItem.setGlobalScale transform.Scale

        if syncVisibility then
            node.Visible <- target.IsVisibleInTree()

    let applyTransform3D (node : Control) offset syncVisibility (target : Node3D) =
        let pos = target |> Node3D.getGlobalPositionWithViewport
        node |> CanvasItem.setGlobalPosition (pos + offset)

        if syncVisibility then
            node.Visible <- target.IsVisibleInTree() && not (target |> Node3D.isBehindCamera)

[<FScript(typeof<StageUI>)>]
type private StageUIScript(node : StageUI) =
    let parent = node |> Node.tryGetParent<Node>
    let tracking2D =
        parent |> Option.bind tryUnbox<CanvasItem>
    let tracking3D =
        parent |> Option.bind tryUnbox<Node3D>
    
    let update2D (target : CanvasItem) =
        target
        |> GodotObject.validate
        |> Option.filter _.IsInsideTree()
        |> Option.iter (StageUI.applyTransform2D node node.Offset node.SyncRotation node.SyncScale node.SyncVisibility)

    let update3D (target : Node3D) =
        target
        |> GodotObject.validate
        |> Option.filter _.IsInsideTree()
        |> Option.iter (StageUI.applyTransform3D node node.Offset node.SyncVisibility)

    do
        parent
        |> Option.iter (fun parent -> parent |> Node.bindNode node)

        node |> Node.whenReady (fun () ->
            node |> StageUI.reparentToStage node.TargetNode
            tracking2D |> Option.iter update2D
            tracking3D |> Option.iter update3D
        )

        tracking2D
        |> Option.iter (fun t ->
            let update () = update2D t
            node |> Engine.addProcess node.PhysicsProcess update |> ignore
        )

        tracking3D
        |> Option.iter (fun t ->
            let update () = update3D t
            node |> Engine.addProcess node.PhysicsProcess update |> ignore
        )

[<FScript(typeof<StagePersistantUI>)>]
type private StagePersistantUIScript(marker : StagePersistantUI) =
    let getUi () =
        let stage = marker |> Node.getStage
        let root = stage |> StageUI.getTargetRoot marker.TargetNode
        
        let ctrl, enter =
            root
            |> Node.tryGetNode<Control> (new NodePath(marker.Key))
            |> Option.map (fun p -> p, true)
            |> Option.defaultWith (fun _ ->
                let p = marker.UiScene |> PackedScene.instantiateTo<Control>
                p.Name <- marker.Key
                root |> Node.addChild p
                p, false
            )
        
        let interf =
            ctrl |> tryUnbox<IPersistantUI>
        
        if enter then
            interf |> Option.iter _.OnReturn()
        
        ctrl, interf

    let update (ui : Control) =
        marker
        |> GodotObject.validate
        |> Option.filter _.IsInsideTree()
        |> Option.map (fun m ->
            m
            |> StageUI.applyTransform2D
                ui
                m.Offset
                m.SyncRotation
                m.SyncScale
                m.SyncVisibility
        )
        |> Option.defaultWith (fun _ ->
            ui.Hide()
        )
    
    let ui = lazy (getUi ())
    let ctrl = lazy (ui.Value |> fst)
    let interf = lazy (ui.Value |> snd)
    
    let update () =
        update ctrl.Value
        
    let init () =
        if marker.Sync then
            update ()
        else
            ctrl.Value.GlobalPosition <- marker.GlobalPosition
    
    let enter () =
        interf.Value |> Option.iter _.OnReturn()
    
    let exit () =
        ctrl.Value.Hide()
        interf.Value |> Option.iter _.OnExit()
    
    do
        marker |> Node.whenReady (fun _ ->
            init ()
            marker.add_TreeEntered enter
        )
        marker.add_TreeExited exit
        if marker.Sync then
            marker |> Engine.addProcess marker.PhysicsProcess update |> ignore
