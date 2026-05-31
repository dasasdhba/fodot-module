module Fodot.Stage.Node

open Fodot.Core.Engine
open Godot
open Fodot.Extend

// access

let private stageMeta = new StringName "_fs_parent_stage"

let tryGetStage (node : Node) =
    node |> Node.findParentFScriptCached<Stage> stageMeta

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
    |> Option.bind (fun s -> s.CurrentScene)
    |> Option.defaultWith (fun _ -> getTree().CurrentScene)

let getCutsceneConfig path (node : Node) =
    node
    |> Node.tryGetNodeFScript<CutsceneProvider> path
    |> Option.map (fun c -> c.CreateConfig ())
    |> Option.defaultValue CutsceneConfig.None