namespace Fodot.Injection.Bind

open Fodot
open Godot

type ResourceProvider(obj : Node) =

    static let _bind_map = WeakMeta<ResourceProvider>()

    let _back_prop_mode = GDProp<int64>.From("mode") obj
    let _back_prop_value_mode = _back_prop_mode.Get()
    let _back_prop_readonly = GDProp<bool>.From("readonly") obj
    let _back_prop_value_readonly = _back_prop_readonly.Get()
    let _back_prop_lib = GDPropDictionary<string, Resource>.From("lib") obj
    let _back_prop_value_lib = _back_prop_lib.Get()

    member val Mode = _back_prop_value_mode
    member val Readonly = _back_prop_value_readonly
    member val Lib = _back_prop_value_lib

    static member From o = _bind_map |> WeakMeta.getOrAdd o (fun () -> ResourceProvider o)
