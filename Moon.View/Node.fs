[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Moon.View.Node

open Fodot
open Godot
open Moon.Library

/// add view transformer and bind with node's life cycle
let addViewTransformerBy (vf : View2D -> ProcessOption<View2DTransformer>) (node : Node) =
    node
    |> View2D.tryGet
    |> Option.map (fun v ->
        let f = vf v |> ProcessOption.bind node
        v.AddTransformer f
    )
    |> Option.isSome

/// add view transformer and bind with node's life cycle
let addViewTransformer (f : ProcessFunc<View2DTransformer option>) node =
    node
    |> addViewTransformerBy (fun _ -> f)
