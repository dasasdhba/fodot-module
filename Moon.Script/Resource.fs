module Moon.Script.Resource

open System
open System.Collections.Generic
open Fodot.Common
open Fodot.Core
open Godot
open Moon.Library

let private map = OwnerDict<Resource> ()

let private predictor<'a when 'a :> Resource> (res: Resource) =
    match res with
    | :? 'a as a -> Some a
    | _ -> None

let findAll<'a when 'a :> Resource> node =
    map |> OwnerDict.findAll node predictor<'a>

let tryFind<'a when 'a :> Resource> node =
    node |> findAll<'a> |> Seq.tryHead

let find<'a when 'a :> Resource> node =
    node |> findAll<'a> |> Seq.head

let findOrAdd<'a when 'a :> Resource> (value : Lazy<'a>)  (node : Node)=
    map
    |> OwnerDict.findOrAdd node predictor<'a> (lazy (
        value.Value :> Resource, value.Value
    ))

let tryGet<'a when 'a :> Resource> key node =
    map
    |> OwnerDict.tryGet node key
    |> Option.bind predictor<'a>

let get<'a when 'a :> Resource> key node =
    node
    |> tryGet<'a> key
    |> Option.defaultWith (fun _ -> failwith $"ResourceProvider: Key {key} not found")

let getOrAdd<'a when 'a :> Resource> key (value : Lazy<'a>)  (node : Node) =
    map
    |> OwnerDict.getOrAdd node key predictor<'a> (lazy (
        value.Value :> Resource, value.Value
    ))

[<FScript("resource_provider")>]
type ResourceProvider(node : Node) =
    let bind = Bind.ResourceProvider.From node
    
    // inject all resources to owner
    let add k (dict: Dictionary<string, Resource>) =
        dict[k] <-
            if bind.Readonly |> not then
                let dup = bind.Lib[k].Duplicate true
                bind.Lib[k] <- dup
                dup
            else
                bind.Lib[k]
    
    let injectTo owner =
        let dict = map |> WeakMeta.getOrAdd owner (lazy
                Dictionary<string, Resource>()
            )
            
        for k in bind.Lib.Keys do
            dict |> add k

    let mutable injected = false
    let inject() =
        if injected || node.Owner = null then
            ()
        else
            injected <- true
            injectTo node.Owner
    
    do
        inject ()
        node.add_Ready inject