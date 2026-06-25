extends Control

class_name StageUI

@export_custom(PROPERTY_HINT_NONE, "") var target_node : String = ""

static func _get_fscripts():
	return ["stage_ui"]
