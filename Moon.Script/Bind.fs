namespace Moon.Script.Bind

open Fodot.Core
open Godot

type ResourceProvider(obj : Node) =

    let _back_prop_readonly = GDProp<bool>.From("readonly") obj
    let _back_prop_value_readonly = _back_prop_readonly.Get()
    let _back_prop_lib = GDPropDictionary<string, Resource>.From("lib") obj
    let _back_prop_value_lib = _back_prop_lib.Get()

    member this.Readonly = _back_prop_value_readonly
    member this.Lib = _back_prop_value_lib

    static member private _bind_map = WeakMeta<ResourceProvider>()
    static member From o = ResourceProvider._bind_map |> WeakMeta.getOrAdd o (lazy ResourceProvider o)
