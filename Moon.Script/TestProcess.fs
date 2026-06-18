namespace Moon.Script

open Fodot
open Fodot.Extend
open Godot

[<FScript("test_process")>]
type private TestProcess(node : Node) =
    let gd = node |> Node.loadAs<PackedScene> "gd.tscn"
    let fd = node |> Node.loadAs<PackedScene> "fd.tscn"
    let hack = node |> Node.loadAs<PackedScene> "hack.tscn"
    
    let create scene =
        for i in 1..1000 do
            let n = scene |> PackedScene.instantiate
            node |> Node.addChild n
    
    do
        node |> Engine.addPhysicsProcess (fun _ ->
            if Input.IsActionJustPressed "ui_up" then
                create gd
            if Input.IsActionJustPressed "ui_down" then
                create fd
            if Input.IsActionJustPressed "ui_left" then
                create hack
        ) |> ignore