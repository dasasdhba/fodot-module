namespace Fodot.Injection

open Fodot.Common
open Fodot.Core
open Fodot.Extend
open Godot

module private InjectorStore =
    let inject res compo scope (node : Node) =
        node
        |> scope
        |> Option.map (fun o ->
            res
            |> MetaDict.tryGetDict node
            |> Option.iter (fun r ->
                res |> MetaDict.appendDict o r
            )
            
            compo
            |> MetaDict.tryGetDict node
            |> Option.map (fun l -> l |> List.map (fun (_, n) ->
                let k = o.GetPathTo(n, true) |> string
                k, n
            ))
            |> Option.iter (fun r ->
                compo |> MetaDict.appendDict o r
            )
        )

[<FScript("owner_injector")>]
type private OwnerInjector(node : Node) =
    
    let mutable injected = false
    let inject() =
        if injected then () else
        
        node
        |> InjectorStore.inject OwnerRes.map OwnerCompo.map Node.getSceneOwner
        |> Option.iter (fun _ ->
            injected <- true
        )
        
    do
        if node.SceneFilePath = "" then
            Logger.pushWarn $"Injector {node.GetPath()} is not a scene root, injection failed."
        else
            inject ()
            node.add_Ready inject
            
[<FScript("injector")>]
type private Injector(node : Node) =
    
    let mutable injected = false
    let inject() =
        if injected then () else
        
        node
        |> InjectorStore.inject Res.map Compo.map Node.tryGetParent
        |> Option.iter (fun _ ->
            injected <- true
        )
        
    do
        inject ()
        node.add_Ready inject