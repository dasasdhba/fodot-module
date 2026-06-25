namespace Moon.Script.Bind

open Fodot
open Godot

type StageUi(obj : Control) =

    let _back_prop_target_node = GDProp<string>.From("target_node") obj
    let _back_prop_value_target_node = _back_prop_target_node.Get()
    let _back_prop_offset = GDProp<Vector2>.From("offset") obj
    let _back_prop_value_offset = _back_prop_offset.Get()
    let _back_prop_sync_rotation = GDProp<bool>.From("sync_rotation") obj
    let _back_prop_value_sync_rotation = _back_prop_sync_rotation.Get()
    let _back_prop_sync_scale = GDProp<bool>.From("sync_scale") obj
    let _back_prop_value_sync_scale = _back_prop_sync_scale.Get()
    let _back_prop_sync_visibility = GDProp<bool>.From("sync_visibility") obj
    let _back_prop_value_sync_visibility = _back_prop_sync_visibility.Get()
    let _back_prop_physics_process = GDProp<bool>.From("physics_process") obj
    let _back_prop_value_physics_process = _back_prop_physics_process.Get()

    member val TargetNode = _back_prop_value_target_node
    member val Offset = _back_prop_value_offset
    member val SyncRotation = _back_prop_value_sync_rotation
    member val SyncScale = _back_prop_value_sync_scale
    member val SyncVisibility = _back_prop_value_sync_visibility
    member val PhysicsProcess = _back_prop_value_physics_process

    static member private _bind_map = WeakMeta<StageUi>()
    static member From o = StageUi._bind_map |> WeakMeta.getOrAdd o (fun () -> StageUi o)
