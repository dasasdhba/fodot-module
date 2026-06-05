module Moon.Script.Res

open System
open Fodot.Common
open Godot
open Moon.Library

let private map = OwnerMeta<Resource> ()

let private predictor<'a when 'a :> Resource> (res: Resource) =
    match res with
    | :? 'a as a -> Some a
    | _ -> None

let findAll<'a when 'a :> Resource> node =
    map |> OwnerMeta.findAll node predictor<'a>

let tryFind<'a when 'a :> Resource> node =
    node |> findAll<'a> |> List.tryHead

let find<'a when 'a :> Resource> node =
    node |> findAll<'a> |> List.head

let findOrAdd<'a when 'a :> Resource> (value : Lazy<'a>)  (node : Node)=
    map
    |> OwnerMeta.findOrAdd node predictor<'a> (lazy (
        value.Value :> Resource, value.Value
    ))

let tryGet<'a when 'a :> Resource> key node =
    map
    |> OwnerMeta.tryGet node key
    |> Option.bind predictor<'a>

let get<'a when 'a :> Resource> key node =
    node
    |> tryGet<'a> key
    |> Option.defaultWith (fun _ -> failwith $"ResourceProvider: Key {key} not found")

let getOrAdd<'a when 'a :> Resource> key (value : Lazy<'a>)  (node : Node) =
    map
    |> OwnerMeta.getOrAdd node key predictor<'a> (lazy (
        value.Value :> Resource, value.Value
    ))

[<FScript("resource_provider")>]
type ResourceProvider(node : Node) =
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

    let mutable injected = false
    let inject() =
        if injected || node.Owner = null then () else
        
        injected <- true
        map |> OwnerMeta.updateDict node list
    
    do
        inject ()
        node.add_Ready inject