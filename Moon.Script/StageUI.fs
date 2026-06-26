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

    let applyTransform (node : Control) offset syncRotation syncScale syncVisibility (target : CanvasItem) =
        let transform = target |> CanvasItem.getStageTransform
        node |> CanvasItem.setGlobalPosition (transform.Origin + offset)

        if syncRotation then
            node |> CanvasItem.setRotation transform.Rotation

        if syncScale then
            node |> CanvasItem.setScale transform.Scale

        if syncVisibility then
            node.Visible <- target.IsVisibleInTree()

[<FScript("stage_ui")>]
type private StageUIScript(node : Control) =
    let bind = Bind.StageUi.From node
    let parent = node |> Node.tryGetParent<Node>
    let tracking =
        parent |> Option.bind tryUnbox<CanvasItem>

    let update (target : CanvasItem) =
        target
        |> GodotObject.validate
        |> Option.filter _.IsInsideTree()
        |> Option.iter (StageUI.applyTransform node bind.Offset bind.SyncRotation bind.SyncScale bind.SyncVisibility)

    do
        parent
        |> Option.iter (fun parent -> parent |> Node.bindNode node)

        node |> Node.whenReady (fun () ->
            node |> StageUI.reparentToStage bind.TargetNode
            tracking |> Option.iter update
        )

        tracking
        |> Option.iter (fun t ->
            let update () = update t
            node |> Engine.addProcess bind.PhysicsProcess update |> ignore
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
            |> StageUI.applyTransform
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
