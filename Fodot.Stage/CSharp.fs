module Fodot.CSharp.StageExtensions

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading.Tasks
open FSharp.Threading
open Fodot.Stage
open Godot

let private opt value =
    if obj.ReferenceEquals(value, null) then None else Some value

let private middleTask (task : Func<Task>) =
    task
    |> opt
    |> Option.map (fun work -> fun () ->
        work.Invoke() |> Task.asUnit
    )

[<Extension>]
let GetStage node =
    node |> Node.getStage

[<Extension>]
let GetCurrentScene node =
    node |> Node.getCurrentScene
    
[<Extension>]
let GetUniquePath node =
    node |> Node.getUniquePath

[<Extension>]
let FadeInOut
    (
        stage : Stage,
        cutscene : CutsceneConfig,
        [<Optional; DefaultParameterValue(null)>] middle : Func<Task>
    ) : Task =
    stage.FadeInOut(cutscene, ?middleTask = middleTask middle) :> Task

[<Extension>]
let QueueChangeBy
    (
        stage : Stage,
        loader: Func<Task<Node>>,
        [<Optional; DefaultParameterValue(null)>] cutscene : CutsceneConfig,
        [<Optional; DefaultParameterValue(null)>] middle : Func<Task>
    ) : Task =
    stage.QueueChangeBy(loader.Invoke, ?cutscene = opt cutscene, ?middleTask = middleTask middle) :> Task

[<Extension>]
let QueueChange
    (
        stage : Stage,
        path : string,
        [<Optional; DefaultParameterValue(null)>] cutscene : CutsceneConfig,
        [<Optional; DefaultParameterValue(null)>] middle : Func<Task>
    ) : Task =
    stage.QueueChange(path, ?cutscene = opt cutscene, ?middleTask = middleTask middle) :> Task

[<Extension>]
let QueueReload
    (
        stage : Stage,
        [<Optional; DefaultParameterValue(null)>] cutscene : CutsceneConfig,
        [<Optional; DefaultParameterValue(null)>] middle : Func<Task>
    ) : Task =
    stage.QueueReload(?cutscene = opt cutscene, ?middleTask = middleTask middle) :> Task

[<Extension>]
let QueueExit
    (
        stage : Stage,
        [<Optional; DefaultParameterValue(null)>] cutscene : CutsceneConfig
    ) : Task =
    stage.QueueExit(?cutscene = opt cutscene) :> Task
