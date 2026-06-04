module Fodot.Stage.Node

open Fodot.Core
open Godot
open Fodot.Extend

// access

let private stageMap = Node.ParentCache<Stage>()

let tryGetStage (node : Node) =
    node |> Node.findParentFsCached stageMap

let getStage (node : Node) =
    node
    |> tryGetStage
    |> Option.defaultWith (fun () -> failwith $"{node} does not have a parent stage.")

let getUniquePath (node : Node) =
    node
    |> tryGetStage
    |> Option.map _.Viewport.GetPathTo(node)
    |> Option.defaultWith (fun _ -> node.GetPath())

let getCurrentScene (node : Node) =
    node
    |> tryGetStage
    |> Option.bind _.CurrentScene
    |> Option.defaultWith (fun _ -> Engine.getTree().CurrentScene)

let getCutsceneConfig path (node : Node) =
    node
    |> Node.tryGetNodeFs<CutsceneProvider> path
    |> Option.map (fun c -> c.CreateConfig ())
    |> Option.defaultValue CutsceneConfig.None