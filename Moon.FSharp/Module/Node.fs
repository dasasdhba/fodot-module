module Moon.Module.Node

open Godot
open Fodot.Core
open Fodot.Stage

let getUniquePath (node : Node) =
    node
    |> Node.tryGetStage
    |> Option.map _.Viewport.GetPathTo(node)
    |> Option.defaultWith (fun _ -> node.GetPath())
    
let getSubBinding meta (creator : Node -> 'a) (node : Node)=
    node |> GodotObject.getMetaWithDefaultAs<'a> meta (lazy (
        let sub = new Node()
        let result = creator sub
        node |> Node.bindChild sub
        node |> Node.addChildInternalFront sub
        result
    ))

let whenReady (action : unit -> unit) (node : Node) =
    if node.IsInsideTree() then
        action()
    else
        node.add_Ready (fun _ -> action())