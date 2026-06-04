namespace Fodot.Stage

open System.Threading.Tasks
open Fodot.Async.GD
open Fodot.Common
open Fodot.Core
open Godot

type ICutscene =
    abstract member Root : Node2D
    abstract member SetSize : Vector2 -> unit
    abstract member FadeInInit : unit -> unit
    abstract member FadeOutInit : unit -> unit
    abstract member FadeIn : unit -> Task<unit>
    abstract member FadeOut : unit -> Task<unit>

type CutsceneConfig =
    {
        In : ICutscene option
        Out : ICutscene option
    }

    static member From cutscene = {
        In = Some cutscene
        Out = Some cutscene
    }

    static member None = {
        In = None
        Out = None
    }

// glue for gdscript

[<FScript("cutscene")>]
type Cutscene(node : Node2D) =
    static let fadeInInit = new StringName "fade_in_init"
    static let fadeOutInit = new StringName "fade_out_init"
    static let fadeIn = new StringName "fade_in"
    static let fadeOut = new StringName "fade_out"
    
    let size = node |> GDProp<Vector2>.From "size"
    let fadeInFinished = node |> GDSignal<unit>.From "fade_in_finished"
    let fadeOutFinished = node |> GDSignal<unit>.From "fade_out_finished"

    member this.Size
        with get() = size.Get ()
        and set value = size.Set value

    interface ICutscene with
        member val Root = node
        
        member this.SetSize value =
            this.Size <- value
            
        member this.FadeInInit () =
            node |> GodotObject.tryInvoke fadeInInit |> ignore
            
        member this.FadeOutInit () =
            node |> GodotObject.tryInvoke fadeOutInit |> ignore
            
        member this.FadeIn () = task {
            node |> GodotObject.invoke fadeIn |> ignore
            let! _ = fadeInFinished.AsTask()
            return ()
        }
        
        member this.FadeOut () = task {
            node |> GodotObject.invoke fadeOut |> ignore
            let! _ = fadeOutFinished.AsTask()
            return ()
        }