namespace Fodot.Injection

open System
open Fodot
open Godot

type MetaDict<'a> = WeakMeta<(string * 'a) list>

module MetaDict =
    
    let tryGetDict (node : Node) (dict : MetaDict<'a>) =
        dict |> WeakMeta.tryGet node
        
    let getDict (node : Node) (dict : MetaDict<'a>) =
        dict |> WeakMeta.getOrAdd node (lazy [])
    
    let appendDict (node : Node) list (dict : MetaDict<'a>) =
        dict |> WeakMeta.addOrUpdate node
                (lazy list)
                (fun old -> list @ old)
    
    let tryGet node key (dict : MetaDict<'a>) =
        dict
        |> tryGetDict node
        |> Option.bind (fun d -> d |> List.tryFind (fun (k, _) -> k = key))
        |> Option.map snd
        
    let findAll node (predictor: 'a -> 'b option) (dict : MetaDict<'a>) =
        dict
        |> tryGetDict node
        |> Option.map (fun d -> d |> List.choose (fun (_, a) -> predictor a))
        |> Option.defaultValue List.empty
        
    let findOrAdd (node : Node) (predictor: 'a -> 'b option) (value : Lazy<'a * 'b>) (dict : MetaDict<'a>)  =
        dict
        |> getDict node
        |> List.choose (fun (_, a) -> predictor a)
        |> List.tryHead
        |> Option.defaultWith (fun _ ->
            let k = Guid.NewGuid().ToString()
            let a, b = value.Value
            dict |> appendDict node [k, a]
            b
        )
        
    let getOrAdd (node : Node) key (predictor: 'a -> 'b option) (value : Lazy<'a * 'b>) (dict : MetaDict<'a>) =
        dict
        |> getDict node
        |> List.tryFind (fun (k, _) -> k = key)
        |> Option.bind (fun (_, a) -> predictor a)
        |> Option.defaultWith (fun _ ->
            let a, b = value.Value
            dict |> appendDict node [key, a]
            b
        )
