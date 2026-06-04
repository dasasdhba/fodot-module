module Moon.Script.Resource

open System.Collections.Generic
open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot
open Moon.Library

let private map = OwnerDict<Resource> ()

let private predictor<'a when 'a :> Resource> (res: Resource) =
    match res with
    | :? 'a as a -> Some a
    | _ -> None

let getAll<'a when 'a :> Resource> node =
    map |> OwnerDict.getAll node predictor<'a>

let tryGet<'a when 'a :> Resource> node =
    map |> OwnerDict.tryGet node predictor<'a>

let get<'a when 'a :> Resource> node =
    map |> OwnerDict.get node predictor<'a>

let tryFind<'a when 'a :> Resource> key node =
    map |> OwnerDict.tryFind node key predictor<'a>

let find<'a when 'a :> Resource> key node =
    map |> OwnerDict.find node key predictor<'a>

[<FScript("resource_provider")>]
type ResourceProvider(node : Node) =
    let bind = Bind.ResourceProvider.From node
    
    // inject all resources to owner
    let owner = node |> Node.getOwnerOrSelf
    let dict = map |> WeakMeta.getOrAdd owner (lazy
        Dictionary<string, Resource>()
    )
    
    do for k in bind.Lib.Keys do
        let v =
            if bind.Readonly |> not then
                let dup = bind.Lib[k].Duplicate true
                bind.Lib[k] <- dup
                dup
            else
                bind.Lib[k]
                
        dict[k] <- v