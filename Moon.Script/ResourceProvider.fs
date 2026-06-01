module Moon.Script.ResourceProvider

open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot

let private meta = "_moon_res_provider_"

let getAll<'a when 'a :> Resource> (node : Node) =
    node
    |> Node.getOwnerOrSelf
    |> GodotObject.getMetaList
    |> Seq.choose (fun m ->
        if (string m).StartsWith meta then
            node |> GodotObject.tryGetMetaAs<'a> m
        else
            None
    )

let tryGet<'a when 'a :> Resource> (node : Node) =
    node |> getAll<'a> |> Seq.tryHead
    
let get<'a when 'a :> Resource> (node : Node) =
    node |> getAll<'a> |> Seq.head

[<FScript("resource_provider")>]
type ResourceProviderScript(node : Node) =
    let bind = Bind.ResourceProvider.From node
    
    // inject all resource to owner
    let owner = node |> Node.getOwnerOrSelf
    do for k in bind.Lib.Keys do
        let res =
            if bind.Readonly then
                bind.Lib[k]
            else
                let dup = bind.Lib[k].Duplicate true
                bind.Lib[k] <- dup
                dup
        
        let meta = new StringName $"{meta}{k}"
        owner |> GodotObject.setMeta meta res