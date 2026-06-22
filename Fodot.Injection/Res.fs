namespace Fodot.Injection

open Fodot
open Fodot.Extend
open Godot

module private ResourceStore =

    let findAllWith map scope predictor node =
        map |> MetaDict.findAll (scope node) predictor
    
    let tryFindWith map scope predictor node =
        node |> findAllWith map scope predictor |> List.tryHead
    
    let findWith map scope predictor node =
        node |> findAllWith map scope predictor |> List.head
    
    let findOrAddWith map scope predictor (valueFunc : unit -> Resource * 'a) (node : Node) =
        map |> MetaDict.findOrAdd (scope node) predictor valueFunc
    
    let getOrAddWith map scope key predictor (valueFunc : unit -> Resource * 'a) (node : Node) =
        map |> MetaDict.getOrAdd (scope node) key predictor valueFunc
    
    let predictor<'a when 'a :> Resource> (res: Resource) =
        match res with
        | :? 'a as a -> Some a
        | _ -> None
    
    let findAll<'a when 'a :> Resource> map scope node =
        node |> findAllWith map scope predictor<'a>

    let tryFind<'a when 'a :> Resource> map scope node =
        node |> tryFindWith map scope predictor<'a>

    let find<'a when 'a :> Resource> map scope node =
        node |> findWith map scope predictor<'a>

    let findOrAdd<'a when 'a :> Resource> map scope (valueFunc : unit -> 'a) (node : Node) =
        node |> findOrAddWith map scope predictor<'a> (fun () ->
            let value = valueFunc ()
            value, value
        )

    let tryGet<'a when 'a :> Resource> map scope key node =
        map
        |> MetaDict.tryGet (scope node) key
        |> Option.bind predictor<'a>

    let get<'a when 'a :> Resource> map scope key node =
        node
        |> tryGet<'a> map scope key
        |> Option.defaultWith (fun _ -> failwith $"ResourceProvider: Key {key} not found")

    let getOrAdd<'a when 'a :> Resource> map scope key (valueFunc : unit -> 'a) (node : Node) =
        node |> getOrAddWith map scope key predictor<'a> (fun () ->
            let value = valueFunc ()
            value, value
        )

module OwnerRes =

    let map = MetaDict<Resource> ()
    let private scope = Node.getOwnerOrSelf
    
    let findAllWith predictor node =
        node |> ResourceStore.findAllWith map scope predictor
    
    let tryFindWith predictor node =
        node |> ResourceStore.tryFindWith map scope predictor
    
    let findWith predictor node =
        node |> ResourceStore.findWith map scope predictor
    
    let findOrAddWith predictor (valueFunc : unit -> Resource * 'a)   (node : Node) =
        node |> ResourceStore.findOrAddWith map scope predictor valueFunc
    
    let getOrAddWith key predictor (valueFunc : unit -> Resource * 'a)  (node : Node) =
        node |> ResourceStore.getOrAddWith map scope key predictor valueFunc
    
    let findAll<'a when 'a :> Resource> node =
        node |> ResourceStore.findAll<'a> map scope

    let tryFind<'a when 'a :> Resource> node =
        node |> ResourceStore.tryFind<'a> map scope

    let find<'a when 'a :> Resource> node =
        node |> ResourceStore.find<'a> map scope

    let findOrAdd<'a when 'a :> Resource> (valueFunc : unit -> 'a)  (node : Node)=
        node |> ResourceStore.findOrAdd<'a> map scope valueFunc

    let tryGet<'a when 'a :> Resource> key node =
        node |> ResourceStore.tryGet<'a> map scope key

    let get<'a when 'a :> Resource> key node =
        node |> ResourceStore.get<'a> map scope key

    let getOrAdd<'a when 'a :> Resource> key (valueFunc : unit -> 'a)  (node : Node) =
        node |> ResourceStore.getOrAdd<'a> map scope key valueFunc
        
module Res =

    let map = MetaDict<Resource> ()
    let private scope (node : Node) = node
    
    let findAllWith predictor node =
        node |> ResourceStore.findAllWith map scope predictor
    
    let tryFindWith predictor node =
        node |> ResourceStore.tryFindWith map scope predictor
    
    let findWith predictor node =
        node |> ResourceStore.findWith map scope predictor
    
    let findOrAddWith predictor (valueFunc : unit -> Resource * 'a)   (node : Node) =
        node |> ResourceStore.findOrAddWith map scope predictor valueFunc
    
    let getOrAddWith key predictor (valueFunc : unit -> Resource * 'a)  (node : Node) =
        node |> ResourceStore.getOrAddWith map scope key predictor valueFunc

    let findAll<'a when 'a :> Resource> node =
        node |> ResourceStore.findAll<'a> map scope

    let tryFind<'a when 'a :> Resource> node =
        node |> ResourceStore.tryFind<'a> map scope

    let find<'a when 'a :> Resource> node =
        node |> ResourceStore.find<'a> map scope

    let findOrAdd<'a when 'a :> Resource> (valueFunc : unit -> 'a)  (node : Node)=
        node |> ResourceStore.findOrAdd<'a> map scope valueFunc

    let tryGet<'a when 'a :> Resource> key node =
        node |> ResourceStore.tryGet<'a> map scope key

    let get<'a when 'a :> Resource> key node =
        node |> ResourceStore.get<'a> map scope key

    let getOrAdd<'a when 'a :> Resource> key (valueFunc : unit -> 'a)  (node : Node) =
        node |> ResourceStore.getOrAdd<'a> map scope key valueFunc

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
