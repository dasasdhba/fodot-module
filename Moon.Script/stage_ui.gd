extends Control

class_name StageUI

@export_custom(PROPERTY_HINT_NONE, "") var target_node : String = ""
@export_custom(PROPERTY_HINT_NONE, "suffix:px") var offset : Vector2 = Vector2.ZERO
@export_custom(PROPERTY_HINT_NONE, "") var physics_process : bool = true
@export_group("Sync", "sync")
@export_custom(PROPERTY_HINT_NONE, "") var sync_rotation : bool = false
@export_custom(PROPERTY_HINT_NONE, "") var sync_scale : bool = false
@export_custom(PROPERTY_HINT_NONE, "") var sync_visibility : bool = true

static func _get_fscripts():
	return ["stage_ui"]
