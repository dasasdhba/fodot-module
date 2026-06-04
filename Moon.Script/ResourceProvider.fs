module Moon.Script.ResourceProvider

open FSharpPlus
open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot
open Godot.Collections

let private resMap = WeakMeta<Dictionary<string, Resource>>()

let getAll<'a when 'a :> Resource> (node : Node) =
    resMap
    |> WeakMeta.tryGet (node |> Node.getOwnerOrSelf)
    |> Option.map (fun dict ->
        dict.Values
        |> Seq.choose (fun r ->
            match r with
            | :? 'a as r -> Some r
            | _ -> None
        )
    )
    |> Option.defaultValue Seq.empty

let tryGet<'a when 'a :> Resource> (node : Node) =
    node |> getAll<'a> |> Seq.tryHead
    
let get<'a when 'a :> Resource> (node : Node) =
    node |> getAll<'a> |> Seq.head

let tryFind<'a when 'a :> Resource> (key : string) (node : Node) =
    resMap
    |> WeakMeta.tryGet (node |> Node.getOwnerOrSelf)
    |> Option.bind (fun d -> d |> Dict.tryGetValue key)

let find<'a when 'a :> Resource> (key : string) (node : Node) =
    node
    |> tryFind<'a> key
    |> Option.defaultWith (fun _ -> failwith $"Resource key {key} not found")

[<FScript("resource_provider")>]
type ResourceProviderScript(node : Node) =
    let bind = Bind.ResourceProvider.From node
    
    // inject all resource to owner
    let owner = node |> Node.getOwnerOrSelf
    
    do if bind.Readonly |> not then
        for k in bind.Lib.Keys do
            let dup = bind.Lib[k].Duplicate true
            bind.Lib[k] <- dup
        
    do resMap |> WeakMeta.addOrUpdate owner bind.Lib