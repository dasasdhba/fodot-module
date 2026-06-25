extends Control

class_name StageSyncUI

@export_custom(PROPERTY_HINT_NONE, "") var target_node : String = ""
@export_custom(PROPERTY_HINT_NONE, "") var offset : Vector2 = Vector2.ZERO
@export_custom(PROPERTY_HINT_NONE, "") var sync_rotation : bool = false
@export_custom(PROPERTY_HINT_NONE, "") var sync_scale : bool = false
@export_custom(PROPERTY_HINT_NONE, "") var sync_visibility : bool = true
@export_custom(PROPERTY_HINT_NONE, "") var physics_process : bool = true

static func _get_fscripts():
	return ["stage_sync_ui"]
