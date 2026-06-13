namespace Fodot.Injection

open System
open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot

module private ComponentStore =

    let findAllWith map scope predictor node =
        map |> MetaDict.findAll (scope node) predictor
    
    let tryFindWith map scope predictor node =
        node |> findAllWith map scope predictor |> List.tryHead
    
    let findWith map scope predictor node =
        node |> findAllWith map scope predictor |> List.head
    
    let ensureChild host scope (value : Lazy<Node * 'a>) =
        lazy (
            let n, a = value.Value
            if n.IsInsideTree() |> not then
                host |> scope |> Node.addChild n 
            n, a
        )
    
    let findOrAddWith<'a> map scope predictor (value : Lazy<Node * 'a>) (node : Node) =
        map
        |> MetaDict.findOrAdd (scope node) predictor (value |> ensureChild node scope)
    
    let getOrAddWith<'a> map scope key predictor (value : Lazy<Node * 'a>) (node : Node) =
        map
        |> MetaDict.getOrAdd (scope node) key predictor (value |> ensureChild node scope)
    
    let predictor<'a when 'a :> Node> (node: Node) =
        match node with
        | :? 'a as a -> Some a
        | _ -> None

    let findAll<'a when 'a :> Node> map scope node =
        node |> findAllWith map scope predictor<'a>

    let tryFind<'a when 'a :> Node> map scope node =
        node |> tryFindWith map scope predictor<'a>

    let find<'a when 'a :> Node> map scope node =
        node |> findWith map scope predictor<'a>

    let findOrAdd<'a when 'a :> Node> map scope (value : Lazy<'a>) (node : Node)=
        node |> findOrAddWith map scope predictor<'a> (lazy (value.Value, value.Value))

    let tryGet<'a when 'a :> Node> map scope path node =
        map
        |> MetaDict.tryGet (scope node) path
        |> Option.bind predictor<'a>

    let get<'a when 'a :> Node> map scope path node =
        node
        |> tryGet<'a> map scope path
        |> Option.defaultWith (fun _ -> failwith $"Component: Node {path} not found")

    let getOrAdd<'a when 'a :> Node> map scope path (value : Lazy<'a>) (node : Node) =
        node |> getOrAddWith map scope path predictor<'a> (lazy (value.Value, value.Value))

    let predictorFs<'a> (node: Node) =
        node |> FScript.tryGet<'a>

    let findAllFs<'a> map scope node =
        node |> findAllWith map scope predictorFs<'a>

    let tryFindFs<'a> map scope node =
        node |> tryFindWith map scope predictorFs<'a>

    let findFs<'a> map scope node =
        node |> findWith map scope predictorFs<'a>

    let findOrAddFs<'a> map scope (value : Lazy<Node>) (node : Node)=
        node |> findOrAddWith map scope predictorFs<'a> (lazy (value.Value, value.Value |> FScript.attach<'a>))

    let tryGetFs<'a> map scope path node =
        map
        |> MetaDict.tryGet (scope node) path
        |> Option.bind predictorFs<'a>

    let getFs<'a> map scope path node =
        node
        |> tryGetFs<'a> map scope path
        |> Option.defaultWith (fun _ -> failwith $"Component: FScript {path} not found")

    let getOrAddFs<'a> map scope path (value : Lazy<Node>) (node : Node) =
        node |> getOrAddWith map scope path predictorFs<'a> (lazy (value.Value, value.Value |> FScript.attach<'a>))
    
    let predictorScript<'a> (node: Node) =
        node |> GodotObject.tryGetScript<'a>

    let findAllScript<'a> map scope node =
        node |> findAllWith map scope predictorScript<'a>

    let tryFindScript<'a> map scope node =
        node |> tryFindWith map scope predictorScript<'a>

    let findScript<'a> map scope node =
        node |> findWith map scope predictorScript<'a>

    let findOrAddScript<'a> map scope (value : Lazy<Node>) (node : Node)=
        node |> findOrAddWith map scope predictorScript<'a> (lazy (value.Value, value.Value |> GodotObject.getScript<'a>))

    let tryGetScript<'a> map scope path node =
        map
        |> MetaDict.tryGet (scope node) path
        |> Option.bind predictorScript<'a>

    let getScript<'a> map scope path node =
        node
        |> tryGetScript<'a> map scope path
        |> Option.defaultWith (fun _ -> failwith $"Component: Script {path} not found")

    let getOrAddScript<'a> map scope path (value : Lazy<Node>) (node : Node) =
        node |> getOrAddWith map scope path predictorScript<'a> (lazy (value.Value, value.Value |> GodotObject.getScript<'a>))
    
    let inject map target (node : Node) =
        node
        |> target
        |> Option.map (fun o ->
            map |> MetaDict.appendDict o [o.GetPathTo(node, true) |> string, node]
        )

module OwnerCompo =

    let map = MetaDict<Node> ()
    let private scope = Node.getOwnerOrSelf
    
    let findAllWith predictor node =
        node |> ComponentStore.findAllWith map scope predictor
    
    let tryFindWith predictor node =
        node |> ComponentStore.tryFindWith map scope predictor
    
    let findWith predictor node =
        node |> ComponentStore.findWith map scope predictor
    
    let findOrAddWith<'a> predictor (value : Lazy<Node * 'a>) (node : Node) =
        node |> ComponentStore.findOrAddWith map scope predictor value
    
    let getOrAddWith<'a> key predictor (value : Lazy<Node * 'a>) (node : Node) =
        node |> ComponentStore.getOrAddWith map scope key predictor value

    let findAll<'a when 'a :> Node> node =
        node |> ComponentStore.findAll<'a> map scope

    let tryFind<'a when 'a :> Node> node =
        node |> ComponentStore.tryFind<'a> map scope

    let find<'a when 'a :> Node> node =
        node |> ComponentStore.find<'a> map scope

    let findOrAdd<'a when 'a :> Node> (value : Lazy<'a>) (node : Node)=
        node |> ComponentStore.findOrAdd<'a> map scope value

    let tryGet<'a when 'a :> Node> path node =
        node |> ComponentStore.tryGet<'a> map scope path

    let get<'a when 'a :> Node> path node =
        node |> ComponentStore.get<'a> map scope path

    let getOrAdd<'a when 'a :> Node> path (value : Lazy<'a>)  (node : Node) =
        node |> ComponentStore.getOrAdd<'a> map scope path value

    let findAllFs<'a> node =
        node |> ComponentStore.findAllFs<'a> map scope

    let tryFindFs<'a> node =
        node |> ComponentStore.tryFindFs<'a> map scope

    let findFs<'a> node =
        node |> ComponentStore.findFs<'a> map scope

    let findOrAddFs<'a> (value : Lazy<Node>) (node : Node)=
        node |> ComponentStore.findOrAddFs<'a> map scope value

    let tryGetFs<'a> path node =
        node |> ComponentStore.tryGetFs<'a> map scope path

    let getFs<'a> path node =
        node |> ComponentStore.getFs<'a> map scope path

    let getOrAddFs<'a> path (value : Lazy<Node>) (node : Node) =
        node |> ComponentStore.getOrAddFs<'a> map scope path value

    let findAllScript<'a> node =
        node |> ComponentStore.findAllScript<'a> map scope

    let tryFindScript<'a> node =
        node |> ComponentStore.tryFindScript<'a> map scope

    let findScript<'a> node =
        node |> ComponentStore.findScript<'a> map scope

    let findOrAddScript<'a> (value : Lazy<Node>) (node : Node)=
        node |> ComponentStore.findOrAddScript<'a> map scope value

    let tryGetScript<'a> path node =
        node |> ComponentStore.tryGetScript<'a> map scope path

    let getScript<'a> path node =
        node |> ComponentStore.getScript<'a> map scope path

    let getOrAddScript<'a> path (value : Lazy<Node>) (node : Node) =
        node |> ComponentStore.getOrAddScript<'a> map scope path value
    
    [<FScript("owner_component")>]
    type private ComponentScript(node : Node) =

        // inject self into owner
        let mutable injected = false
        let inject() =
            if injected then () else
            
            node
            |> ComponentStore.inject map Node.getSceneOwner
            |> Option.iter (fun _ ->
                injected <- true
            )
            
        do
            inject ()
            node.add_Ready inject
            
module Compo =

    let map = MetaDict<Node> ()
    let private scope (node : Node) = node
    
    let findAllWith predictor node =
        node |> ComponentStore.findAllWith map scope predictor
    
    let tryFindWith predictor node =
        node |> ComponentStore.tryFindWith map scope predictor
    
    let findWith predictor node =
        node |> ComponentStore.findWith map scope predictor
    
    let findOrAddWith<'a> predictor (value : Lazy<Node * 'a>) (node : Node) =
        node |> ComponentStore.findOrAddWith map scope predictor value
    
    let getOrAddWith<'a> key predictor (value : Lazy<Node * 'a>) (node : Node) =
        node |> ComponentStore.getOrAddWith map scope key predictor value

    let findAll<'a when 'a :> Node> node =
        node |> ComponentStore.findAll<'a> map scope

    let tryFind<'a when 'a :> Node> node =
        node |> ComponentStore.tryFind<'a> map scope

    let find<'a when 'a :> Node> node =
        node |> ComponentStore.find<'a> map scope

    let findOrAdd<'a when 'a :> Node> (value : Lazy<'a>) (node : Node)=
        node |> ComponentStore.findOrAdd<'a> map scope value

    let tryGet<'a when 'a :> Node> path node =
        node |> ComponentStore.tryGet<'a> map scope path

    let get<'a when 'a :> Node> path node =
        node |> ComponentStore.get<'a> map scope path

    let getOrAdd<'a when 'a :> Node> path (value : Lazy<'a>)  (node : Node) =
        node |> ComponentStore.getOrAdd<'a> map scope path value

    let findAllFs<'a> node =
        node |> ComponentStore.findAllFs<'a> map scope

    let tryFindFs<'a> node =
        node |> ComponentStore.tryFindFs<'a> map scope

    let findFs<'a> node =
        node |> ComponentStore.findFs<'a> map scope

    let findOrAddFs<'a> (value : Lazy<Node>) (node : Node)=
        node |> ComponentStore.findOrAddFs<'a> map scope value

    let tryGetFs<'a> path node =
        node |> ComponentStore.tryGetFs<'a> map scope path

    let getFs<'a> path node =
        node |> ComponentStore.getFs<'a> map scope path

    let getOrAddFs<'a> path (value : Lazy<Node>) (node : Node) =
        node |> ComponentStore.getOrAddFs<'a> map scope path value

    let findAllScript<'a> node =
        node |> ComponentStore.findAllScript<'a> map scope

    let tryFindScript<'a> node =
        node |> ComponentStore.tryFindScript<'a> map scope

    let findScript<'a> node =
        node |> ComponentStore.findScript<'a> map scope

    let findOrAddScript<'a> (value : Lazy<Node>) (node : Node)=
        node |> ComponentStore.findOrAddScript<'a> map scope value

    let tryGetScript<'a> path node =
        node |> ComponentStore.tryGetScript<'a> map scope path

    let getScript<'a> path node =
        node |> ComponentStore.getScript<'a> map scope path

    let getOrAddScript<'a> path (value : Lazy<Node>) (node : Node) =
        node |> ComponentStore.getOrAddScript<'a> map scope path value
    
    [<FScript("component")>]
    type private ComponentScript(node : Node) =

        // inject self into owner
        let mutable injected = false
        let inject() =
            if injected then () else
            
            node
            |> ComponentStore.inject map Node.tryGetParent
            |> Option.iter (fun _ ->
                injected <- true
            )
            
        do
            inject ()
            node.add_Ready inject
