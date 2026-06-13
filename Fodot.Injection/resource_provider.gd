extends Node

class_name ResourceProvider

@export_custom(PROPERTY_HINT_ENUM, "Owner,Parent") var mode : int = 0
@export_custom(PROPERTY_HINT_NONE, "") var readonly : bool = true
@export_custom(PROPERTY_HINT_DICTIONARY_TYPE, "String;Resource") var lib : Dictionary[String, Resource] = {}

static func _get_fscripts():
	return ["resource_provider"]
