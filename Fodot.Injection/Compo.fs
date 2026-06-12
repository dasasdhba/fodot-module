namespace Fodot.Injection

open System
open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot

module OwnerCompo =

    let map = MetaDict<Node> ()
    
    let findAllWith predictor node =
        map |> MetaDict.findAll (node |> Node.getOwnerOrSelf) predictor
    
    let tryFindWith predictor node =
        node |> findAllWith predictor |> List.tryHead
    
    let findWith predictor node =
        node |> findAllWith predictor |> List.head
    
    let findOrAddWith<'a> predictor (value : Lazy<Node * 'a>) (node : Node) =
        map
        |> MetaDict.findOrAdd (node |> Node.getOwnerOrSelf) predictor (lazy (
            let n, a = value.Value
            if n.IsInsideTree() |> not then
                node |> Node.getOwnerOrSelf |> Node.addChild n 
            n, a
        ))
    
    let getOrAddWith<'a> key predictor (value : Lazy<Node * 'a>) (node : Node) =
        map
        |> MetaDict.getOrAdd (node |> Node.getOwnerOrSelf) key predictor (lazy (
            let n, a = value.Value
            if n.IsInsideTree() |> not then
                node |> Node.getOwnerOrSelf |> Node.addChild n 
            n, a
        ))
    
    let private predictor<'a when 'a :> Node> (node: Node) =
        match node with
        | :? 'a as a -> Some a
        | _ -> None

    let findAll<'a when 'a :> Node> node =
        node |> findAllWith predictor<'a>

    let tryFind<'a when 'a :> Node> node =
        node |> tryFindWith predictor<'a>

    let find<'a when 'a :> Node> node =
        node |> findWith predictor<'a>

    let findOrAdd<'a when 'a :> Node> (value : Lazy<'a>) (node : Node)=
        node |> findOrAddWith predictor<'a> (lazy (value.Value, value.Value))

    let tryGet<'a when 'a :> Node> path node =
        map
        |> MetaDict.tryGet (node |> Node.getOwnerOrSelf) path
        |> Option.bind predictor<'a>

    let get<'a when 'a :> Node> path node =
        node
        |> tryGet<'a> path
        |> Option.defaultWith (fun _ -> failwith $"Component: Node {path} not found")

    let getOrAdd<'a when 'a :> Node> path (value : Lazy<'a>)  (node : Node) =
        node |> getOrAddWith path predictor<'a> (lazy (value.Value, value.Value))

    let private predictorFs<'a> (node: Node) =
        node |> FScript.tryGet<'a>

    let findAllFs<'a> node =
        node |> findAllWith predictorFs<'a>

    let tryFindFs<'a> node =
        node |> tryFindWith predictorFs<'a>

    let findFs<'a> node =
        node |> findWith predictorFs<'a>

    let findOrAddFs<'a> (value : Lazy<Node>) (node : Node)=
        node |> findOrAddWith predictorFs<'a> (lazy (value.Value, value.Value |> FScript.attach<'a>))

    let tryGetFs<'a> path node =
        map
        |> MetaDict.tryGet (node |> Node.getOwnerOrSelf) path
        |> Option.bind predictorFs<'a>

    let getFs<'a> path node =
        node
        |> tryGetFs<'a> path
        |> Option.defaultWith (fun _ -> failwith $"Component: FScript {path} not found")

    let getOrAddFs<'a> path (value : Lazy<Node>) (node : Node) =
        node |> getOrAddWith path predictorFs<'a> (lazy (value.Value, value.Value |> FScript.attach<'a>))
    
    let private predictorScript<'a> (node: Node) =
        node |> GodotObject.tryGetScript<'a>

    let findAllScript<'a> node =
        node |> findAllWith predictorScript<'a>

    let tryFindScript<'a> node =
        node |> tryFindWith predictorScript<'a>

    let findScript<'a> node =
        node |> findWith predictorScript<'a>

    let findOrAddScript<'a> (value : Lazy<Node>) (node : Node)=
        node |> findOrAddWith predictorScript<'a> (lazy (value.Value, value.Value |> GodotObject.getScript<'a>))

    let tryGetScript<'a> path node =
        map
        |> MetaDict.tryGet (node |> Node.getOwnerOrSelf) path
        |> Option.bind predictorScript<'a>

    let getScript<'a> path node =
        node
        |> tryGetScript<'a> path
        |> Option.defaultWith (fun _ -> failwith $"Component: Script {path} not found")

    let getOrAddScript<'a> path (value : Lazy<Node>) (node : Node) =
        node |> getOrAddWith path predictorScript<'a> (lazy (value.Value, value.Value |> GodotObject.getScript<'a>))
    
    [<FScript("owner_component")>]
    type private ComponentScript(node : Node) =

        // inject self into owner
        let mutable injected = false
        let inject() =
            if injected then () else
            
            node
            |> Node.getSceneOwner
            |> Option.iter (fun o ->
                injected <- true
                map |> MetaDict.appendDict o [o.GetPathTo(node, true) |> string, node]
            )
            
        do
            inject ()
            node.add_Ready inject
            
module Compo =

    let map = MetaDict<Node> ()
    
    let findAllWith predictor node =
        map |> MetaDict.findAll node predictor
    
    let tryFindWith predictor node =
        node |> findAllWith predictor |> List.tryHead
    
    let findWith predictor node =
        node |> findAllWith predictor |> List.head
    
    let findOrAddWith<'a> predictor (value : Lazy<Node * 'a>) (node : Node) =
        map
        |> MetaDict.findOrAdd node predictor (lazy (
            let n, a = value.Value
            if n.IsInsideTree() |> not then
                node |> Node.getOwnerOrSelf |> Node.addChild n 
            n, a
        ))
    
    let getOrAddWith<'a> key predictor (value : Lazy<Node * 'a>) (node : Node) =
        map
        |> MetaDict.getOrAdd node key predictor (lazy (
            let n, a = value.Value
            if n.IsInsideTree() |> not then
                node |> Node.getOwnerOrSelf |> Node.addChild n 
            n, a
        ))
    
    let private predictor<'a when 'a :> Node> (node: Node) =
        match node with
        | :? 'a as a -> Some a
        | _ -> None

    let findAll<'a when 'a :> Node> node =
        node |> findAllWith predictor<'a>

    let tryFind<'a when 'a :> Node> node =
        node |> tryFindWith predictor<'a>

    let find<'a when 'a :> Node> node =
        node |> findWith predictor<'a>

    let findOrAdd<'a when 'a :> Node> (value : Lazy<'a>) (node : Node)=
        node |> findOrAddWith predictor<'a> (lazy (value.Value, value.Value))

    let tryGet<'a when 'a :> Node> path node =
        map
        |> MetaDict.tryGet node path
        |> Option.bind predictor<'a>

    let get<'a when 'a :> Node> path node =
        node
        |> tryGet<'a> path
        |> Option.defaultWith (fun _ -> failwith $"Component: Node {path} not found")

    let getOrAdd<'a when 'a :> Node> path (value : Lazy<'a>)  (node : Node) =
        node |> getOrAddWith path predictor<'a> (lazy (value.Value, value.Value))

    let private predictorFs<'a> (node: Node) =
        node |> FScript.tryGet<'a>

    let findAllFs<'a> node =
        node |> findAllWith predictorFs<'a>

    let tryFindFs<'a> node =
        node |> tryFindWith predictorFs<'a>

    let findFs<'a> node =
        node |> findWith predictorFs<'a>

    let findOrAddFs<'a> (value : Lazy<Node>) (node : Node)=
        node |> findOrAddWith predictorFs<'a> (lazy (value.Value, value.Value |> FScript.attach<'a>))

    let tryGetFs<'a> path node =
        map
        |> MetaDict.tryGet node path
        |> Option.bind predictorFs<'a>

    let getFs<'a> path node =
        node
        |> tryGetFs<'a> path
        |> Option.defaultWith (fun _ -> failwith $"Component: FScript {path} not found")

    let getOrAddFs<'a> path (value : Lazy<Node>) (node : Node) =
        node |> getOrAddWith path predictorFs<'a> (lazy (value.Value, value.Value |> FScript.attach<'a>))
    
    let private predictorScript<'a> (node: Node) =
        node |> GodotObject.tryGetScript<'a>

    let findAllScript<'a> node =
        node |> findAllWith predictorScript<'a>

    let tryFindScript<'a> node =
        node |> tryFindWith predictorScript<'a>

    let findScript<'a> node =
        node |> findWith predictorScript<'a>

    let findOrAddScript<'a> (value : Lazy<Node>) (node : Node)=
        node |> findOrAddWith predictorScript<'a> (lazy (value.Value, value.Value |> GodotObject.getScript<'a>))

    let tryGetScript<'a> path node =
        map
        |> MetaDict.tryGet node path
        |> Option.bind predictorScript<'a>

    let getScript<'a> path node =
        node
        |> tryGetScript<'a> path
        |> Option.defaultWith (fun _ -> failwith $"Component: Script {path} not found")

    let getOrAddScript<'a> path (value : Lazy<Node>) (node : Node) =
        node |> getOrAddWith path predictorScript<'a> (lazy (value.Value, value.Value |> GodotObject.getScript<'a>))
    
    [<FScript("component")>]
    type private ComponentScript(node : Node) =

        // inject self into owner
        let mutable injected = false
        let inject() =
            if injected then () else
            
            node
            |> Node.tryGetParent
            |> Option.iter (fun o ->
                injected <- true
                map |> MetaDict.appendDict o [o.GetPathTo(node, true) |> string, node]
            )
            
        do
            inject ()
            node.add_Ready inject