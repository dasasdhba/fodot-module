module Moon.Script.Component

open System.Collections.Generic
open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot
open Moon.Library

let private map = OwnerDict<Node> ()

let private predictor<'a when 'a :> Node> (node: Node) =
    match node with
    | :? 'a as a -> Some a
    | _ -> None
    
let getAll<'a when 'a :> Node> node =
    map |> OwnerDict.getAll node predictor<'a>

let tryGet<'a when 'a :> Node> node =
    map |> OwnerDict.tryGet node predictor<'a>

let get<'a when 'a :> Node> node =
    map |> OwnerDict.get node predictor<'a>

let tryFind<'a when 'a :> Node> key node =
    map |> OwnerDict.tryFind node key predictor<'a>

let find<'a when 'a :> Node> key node =
    map |> OwnerDict.find node key predictor<'a>

let private predictorFs<'a> (node: Node) =
    node |> FScript.tryGet<'a>

let getAllFs<'a> node =
    map |> OwnerDict.getAll node predictorFs<'a>

let tryGetFs<'a> node =
    map |> OwnerDict.tryGet node predictorFs<'a>

let getFs<'a> node =
    map |> OwnerDict.get node predictorFs<'a>

let tryFindFs<'a> key node =
    map |> OwnerDict.tryFind node key predictorFs<'a>

let findFs<'a> key node =
    map |> OwnerDict.find node key predictorFs<'a>

[<FScript("compo")>]
type ComponentScript(node : Node) =

    // inject self into owner
    let owner = node |> Node.getOwnerOrSelf
    let dict = map |> WeakMeta.getOrAdd owner (lazy
        Dictionary<string, Node>()
    )
    
    do dict[node |> Node.getNameInOwner] <- node