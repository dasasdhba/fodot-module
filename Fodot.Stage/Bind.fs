namespace Fodot.Stage.Bind

open Fodot
open Godot

type CutsceneProvider(obj : Node) =

    static let _bind_map = WeakMeta<CutsceneProvider>()

    let _back_prop_in_scene = GDNullProp<PackedScene>.From("in_scene") obj
    let _back_prop_value_in_scene = _back_prop_in_scene.Get()
    let _back_prop_out_scene = GDNullProp<PackedScene>.From("out_scene") obj
    let _back_prop_value_out_scene = _back_prop_out_scene.Get()
    let _back_prop_reuse_in_if_out_is_null = GDProp<bool>.From("reuse_in_if_out_is_null") obj
    let _back_prop_value_reuse_in_if_out_is_null = _back_prop_reuse_in_if_out_is_null.Get()

    member val InScene = _back_prop_value_in_scene
    member val OutScene = _back_prop_value_out_scene
    member val ReuseInIfOutIsNull = _back_prop_value_reuse_in_if_out_is_null

    static member From o = _bind_map |> WeakMeta.getOrAdd o (fun () -> CutsceneProvider o)
