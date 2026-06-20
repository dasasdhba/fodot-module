namespace Moon.Script

open System
open System.Threading
open Fodot
open Fodot.Async
open Fodot.Module
open Godot
open Moon.Class
open Moon.Component
open Moon.Module

[<FScript(typeof<ShadowCaster2D>)>]
type private ShadowCaster2DScript(caster : ShadowCaster2D) =
    let physics =
        caster.ProcessCallback = ShadowCaster2D.ShadowCaster2DProcessCallback.Physics
    
    let items =
        caster.ShadowItems
        |> Seq.map (fun p -> caster |> Node.tryGetNode<CanvasItem> p)
        |> Seq.choose id
        |> Array.ofSeq
        
    let root =
        caster |> Node.tryGetNode<CanvasItem> caster.Root
    
    let mutable cleared = false
    
    let clear (drawer : DrawProcess2D) =
        let a = AsyncNode.New drawer physics CancellationToken.None
        task {
            if cleared then () else
            cleared <- true
            
            do! a.Until (Unit (fun _ -> drawer.GetQueuedTaskCount() <= 0))
            drawer.QueueFree ()
        }
    
    let mutable idx = 0
    
    let emit (drawer : DrawProcess2D) =
        let infos =
            items
            |> Array.filter (fun c ->
                caster.ForceVisible |> not || c.IsVisibleInTree ()
            )
            |> Array.choose (fun c ->
                match c |> CanvasItem.tryGetTexture with
                | Some t ->
                    let material =
                        if c.Material = null then
                            null
                        elif caster.DuplicateMaterial then
                            c.Material.Duplicate(true) :?> Material
                        else
                            c.Material
                    
                    Some (
                        c.GetGlobalTransform(),
                        c.ZIndex + caster.ZIndex,
                        c.Modulate,
                        c.SelfModulate,
                        material,
                        t
                    )
                | _ -> None
            )
        
        if infos.Length = 0 then () else
        
        let index = idx
        let time = caster.ShadowTime
        let mutable timer = 0.0
        drawer.AddDrawProcess (fun delta ->
            timer <- timer + delta
            let s = timer / time
            for info in infos do
                let transform, zIndex, modulate, selfModulate, material, texture = info
                let m = modulate |> Color.withA (modulate.A * float32 (1.0 - s))
                drawer.SetDrawGlobal true
                drawer.SetDrawIndex index
                drawer.SetDrawTransform transform
                drawer.SetDrawZIndex zIndex
                drawer.SetDrawModulate m
                drawer.SetDrawSelfModulate selfModulate
                drawer.SetDrawMaterial material
                drawer.QueuedDrawTexture(texture, Vector2.Zero)
            
            s >= 1.0
        )
    
    let update (drawer : DrawProcess2D) =
        drawer |> Action.repeat caster.Interval physics (fun _ ->
            if caster.Emitting then
                emit drawer
                idx <- idx + 1
            else
                idx <- 0
        )
    
    do caster |> Node.whenReady (fun _ ->
        root |> Option.iter (fun r ->
            let draw = new DrawProcess2D()
            draw.MaxDrawingTask <-
                (Math.Ceiling(caster.ShadowTime / caster.Interval) |> uint) + 1u
            r |> Node.addSibling draw
            r
            |> Node.getDeleteEvent
            |> _.Add(fun _ -> clear draw |> ignore)
            
            update draw |> ignore
        )
    )
