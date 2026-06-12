namespace Fodot.Injection

open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot

module OwnerRes =

    let map = MetaDict<Resource> ()
    
    let findAllWith predictor node =
        map |> MetaDict.findAll (node |> Node.getOwnerOrSelf) predictor
    
    let tryFindWith predictor node =
        node |> findAllWith predictor |> List.tryHead
    
    let findWith predictor node =
        node |> findAllWith predictor |> List.head
    
    let findOrAddWith predictor (value : Lazy<Resource * 'a>)   (node : Node) =
        map
        |> MetaDict.findOrAdd (node |> Node.getOwnerOrSelf) predictor value
    
    let getOrAddWith key predictor (value : Lazy<Resource * 'a>)  (node : Node) =
        map
        |> MetaDict.getOrAdd (node |> Node.getOwnerOrSelf) key predictor value
    
    let private predictor<'a when 'a :> Resource> (res: Resource) =
        match res with
        | :? 'a as a -> Some a
        | _ -> None
    
    let findAll<'a when 'a :> Resource> node =
        node |> findAllWith predictor<'a>

    let tryFind<'a when 'a :> Resource> node =
        node |> tryFindWith predictor<'a>

    let find<'a when 'a :> Resource> node =
        node |> findWith predictor<'a>

    let findOrAdd<'a when 'a :> Resource> (value : Lazy<'a>)  (node : Node)=
        node |> findOrAddWith predictor (lazy (value.Value, value.Value))

    let tryGet<'a when 'a :> Resource> key node =
        map
        |> MetaDict.tryGet (node |> Node.getOwnerOrSelf) key
        |> Option.bind predictor<'a>

    let get<'a when 'a :> Resource> key node =
        node
        |> tryGet<'a> key
        |> Option.defaultWith (fun _ -> failwith $"ResourceProvider: Key {key} not found")

    let getOrAdd<'a when 'a :> Resource> key (value : Lazy<'a>)  (node : Node) =
        node |> getOrAddWith key predictor<'a> (lazy (value.Value, value.Value))
        
module Res =

    let map = MetaDict<Resource> ()
    
    let findAllWith predictor node =
        map |> MetaDict.findAll node predictor
    
    let tryFindWith predictor node =
        node |> findAllWith predictor |> List.tryHead
    
    let findWith predictor node =
        node |> findAllWith predictor |> List.head
    
    let findOrAddWith predictor (value : Lazy<Resource * 'a>)   (node : Node) =
        map
        |> MetaDict.findOrAdd node predictor value
    
    let getOrAddWith key predictor (value : Lazy<Resource * 'a>)  (node : Node) =
        map
        |> MetaDict.getOrAdd node key predictor value
    
    let private predictor<'a when 'a :> Resource> (res: Resource) =
        match res with
        | :? 'a as a -> Some a
        | _ -> None

    let findAll<'a when 'a :> Resource> node =
        node |> findAllWith predictor<'a>

    let tryFind<'a when 'a :> Resource> node =
        node |> tryFindWith predictor<'a>

    let find<'a when 'a :> Resource> node =
        node |> findWith predictor<'a>

    let findOrAdd<'a when 'a :> Resource> (value : Lazy<'a>)  (node : Node)=
        node |> findOrAddWith predictor (lazy (value.Value, value.Value))

    let tryGet<'a when 'a :> Resource> key node =
        map
        |> MetaDict.tryGet node key
        |> Option.bind predictor<'a>

    let get<'a when 'a :> Resource> key node =
        node
        |> tryGet<'a> key
        |> Option.defaultWith (fun _ -> failwith $"ResourceProvider: Key {key} not found")

    let getOrAdd<'a when 'a :> Resource> key (value : Lazy<'a>)  (node : Node) =
        node |> getOrAddWith key predictor<'a> (lazy (value.Value, value.Value))

[<FScript("resource_provider")>]
type private ResourceProvider(node : Node) =
    let bind = Bind.ResourceProvider.From node
    
    // inject all resources to owner
    let list =
        bind.Lib |> Seq.fold (fun l d ->
            let k, v = d.Key, d.Value
            let next =
                if bind.Readonly |> not then
                    let dup = v.Duplicate true
                    bind.Lib[k] <- dup
                    dup
                else
                    v
            (k, next) :: l
        ) []
    
    let map, target =
        match bind.Mode with
        | 0L -> OwnerRes.map, Node.getSceneOwner
        | _ -> Res.map, Node.tryGetParent
    
    let mutable injected = false
    let inject() =
        if injected then () else
        
        node
        |> target
        |> Option.iter (fun o ->
            injected <- true
            map |> MetaDict.appendDict o list
        )
    
    do
        inject ()
        node.add_Ready inject