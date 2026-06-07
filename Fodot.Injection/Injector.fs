namespace Fodot.Injection

open Fodot.Common
open Fodot.Extend
open Godot

[<FScript("injector")>]
type private Injector(node : Node) =
    
    let mutable injected = false
    let inject() =
        if injected then () else
        
        node
        |> Node.getSceneOwner
        |> Option.iter (fun o ->
            injected <- true
            
            Res.map
            |> OwnerMeta.tryGetDict node
            |> Option.iter (fun r ->
                Res.map |> OwnerMeta.appendDict o r
            )
            
            Compo.map
            |> OwnerMeta.tryGetDict node
            |> Option.map (fun l -> l |> List.map (fun (_, n) ->
                let k = o.GetPathTo(n, true) |> string
                k, n
            ))
            |> Option.iter (fun r ->
                Compo.map |> OwnerMeta.appendDict o r
            )
        )
        
    do
        if node.SceneFilePath = "" then
            Logger.pushWarn $"Injector {node.GetPath()} is not a scene root, injection failed."
        else
            inject ()
            node.add_Ready inject