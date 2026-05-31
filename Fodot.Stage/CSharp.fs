module Fodot.CSharp.StageExtensions

open System.Runtime.CompilerServices
open Fodot.Stage

[<Extension>]
let GetCurrentScene node =
    node |> Node.getCurrentScene
    
[<Extension>]
let GetUniquePath node =
    node |> Node.getUniquePath
