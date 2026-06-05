module Moon.Script.Compo

open System
open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot
open Moon.Library

let private map = OwnerMeta<Node> ()

let private predictor<'a when 'a :> Node> (node: Node) =
    match node with
    | :? 'a as a -> Some a
    | _ -> None

let findAll<'a when 'a :> Node> node =
    map |> OwnerMeta.findAll node predictor<'a>

let tryFind<'a when 'a :> Node> node =
    node |> findAll<'a> |> List.tryHead

let find<'a when 'a :> Node> node =
    node |> findAll<'a> |> List.head

let findOrAdd<'a when 'a :> Node> (value : Lazy<'a>)  (node : Node)=
    map
    |> OwnerMeta.findOrAdd node predictor<'a> (lazy (
        value.Value :> Node, value.Value
    ))

let tryGet<'a when 'a :> Node> path node =
    map
    |> OwnerMeta.tryGet node path
    |> Option.bind predictor<'a>

let get<'a when 'a :> Node> path node =
    node
    |> tryGet<'a> path
    |> Option.defaultWith (fun _ -> failwith $"Component: Node {path} not found")

let getOrAdd<'a when 'a :> Node> key (value : Lazy<'a>)  (node : Node) =
    map
    |> OwnerMeta.getOrAdd node key predictor<'a> (lazy (
        value.Value :> Node, value.Value
    ))

let private predictorFs<'a> (node: Node) =
    node |> FScript.tryGet<'a>

let findAllFs<'a> node =
    map |> OwnerMeta.findAll node predictorFs<'a>

let tryFindFs<'a> node =
    node |> findAllFs<'a> |> List.tryHead

let findFs<'a> node =
    node |> findAllFs<'a> |> List.head

let findOrAddFs<'a> (value : Lazy<Node>) (node : Node)=
    map
    |> OwnerMeta.findOrAdd node predictorFs<'a> (lazy (
        if value.Value.IsInsideTree() |> not then
            node |> Node.getOwnerOrSelf |> Node.addChild value.Value
        value.Value, value.Value |> FScript.attach<'a>
    ))

let tryGetFs<'a> path node =
    map
    |> OwnerMeta.tryGet node path
    |> Option.bind predictorFs<'a>

let getFs<'a> path node =
    node
    |> tryGetFs<'a> path
    |> Option.defaultWith (fun _ -> failwith $"Component: FScript {path} not found")

let getOrAddFs<'a> key (value : Lazy<Node>) (node : Node) =
    map
    |> OwnerMeta.getOrAdd node key predictorFs<'a> (lazy (
        if value.Value.IsInsideTree() |> not then
            node |> Node.getOwnerOrSelf |> Node.addChild value.Value
        value.Value, value.Value |> FScript.attach<'a>
    ))

[<FScript("component")>]
type ComponentScript(node : Node) =

    // inject self into owner
    let mutable injected = false
    let inject() =
        if injected || node.Owner = null then () else
        
        injected <- true
        map |> OwnerMeta.updateDict node [node |> Node.getNameInOwner, node]
        
    do
        inject ()
        node.add_Ready inject