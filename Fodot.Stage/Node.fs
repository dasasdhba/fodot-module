module Fodot.Stage.Node

open Fodot.Core.Engine
open Godot
open Fodot.Core
open Fodot.Module.Node

// access

let tryGetStage (node : Node) =
    node
    |> findParentCachedWith (fun p -> p |> FScript.contains<Stage> ) "_fs_parent_stage"
    |> Option.bind (fun p -> p |> FScript.tryGet<Stage> )
      
let getStage (node : Node) =
    node
    |> tryGetStage
    |> Option.defaultWith (fun () -> failwith $"{node} does not have a parent stage.")
  
let getCurrentScene (node : Node) =
    node
    |> tryGetStage
    |> Option.bind (fun s -> s.CurrentScene)
    |> Option.defaultWith (fun _ -> getTree().CurrentScene)
  
let getCutsceneConfig path (node : Node) =
    node
    |> Node.tryGetNode path
    |> Option.bind (fun n -> n |> FScript.tryGet<CutsceneProvider>)
    |> Option.map (fun c -> c.CreateConfig ())
    |> Option.defaultValue CutsceneConfig.None