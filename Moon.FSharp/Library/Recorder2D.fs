namespace Moon.Library

open System
open Fodot.Core
open Fodot.Module
open Godot

type Recorder2D(node : Node, target : CanvasItem) =
    inherit RefCounted()
    
    let mutable velocity = Vector2.Zero
    let mutable motion = Vector2.Zero
    let mutable lastPosition = Vector2.Zero
    let mutable firstRecorded = false
    
    new (node : CanvasItem) = new Recorder2D(node, node)
    
    member val Disabled = false with get, set
    member val LastVelocity = velocity with get
    member val LastMotion = motion with get
    member val LastPosition = lastPosition with get
    
    member private this.Process = node |> Engine.addPhysicsDelta32Process (fun delta ->
        if GodotObject.IsInstanceValid target |> not then
            (this :> IDisposable).Dispose()
        
        elif this.Disabled then
            velocity <- Vector2.Zero
            motion <- Vector2.Zero
            firstRecorded <- false
            
        else
            
            let pos = target |> CanvasItem.getGlobalPosition
            
            if firstRecorded |> not then
                lastPosition <- pos
                firstRecorded <- true
            else
                motion <- pos - lastPosition
                velocity <- motion / delta
                lastPosition <- pos
    )
    
    interface IDisposable with
        member this.Dispose() =
            node |> Engine.removePhysicsProcess this.Process |> ignore
            
module Recorder2D =
    
    let private meta = new StringName "_moon_recorder2d"
    
    let get (item: CanvasItem)=
        item |> GodotObject.getMetaWithDefaultAs<Recorder2D> meta (lazy (
            let node = new Node()
            let recorder = new Recorder2D(node, item)
            item |> Node.bindChild node
            item |> Node.addChildInternalFront node
            item |> GodotObject.setMeta meta recorder
            recorder
        ))