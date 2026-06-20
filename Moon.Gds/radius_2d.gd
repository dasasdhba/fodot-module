extends Node2D
class_name Radius2D

@export_custom(PROPERTY_HINT_NONE, "radians_as_degrees,suffix:\u00B0/s") var speed : float = deg_to_rad(200.0)
@export_custom(PROPERTY_HINT_NONE, "suffix:px") var radius := Vector2(0.0, 16.0)
@export_range(-360.0, 360.0, 0.1, "radians_as_degrees") var phase = 0.0
@export var transposed := false
@export var disabled := false
@export_enum("Godot", "Fodot") var proc_type = 0
@export_enum("Idle", "Physics") var proc_mode = 1

@onready var origin := position

func _ready():
	if proc_type == 0:
		set_process(proc_mode == 0)
		set_physics_process(proc_mode == 1)
	else:
		set_process(false)
		set_physics_process(false)

func update(delta):
	phase = wrap(phase + speed * delta, -PI, PI)
	var rotated := Vector2(cos(phase), sin(phase)) * radius
	if transposed:
		rotated = Vector2(rotated.y, rotated.x)
	position = origin + rotated

func _physics_process(delta):
	update(delta)

func _process(delta):
	update(delta)

func _fs_process():
	if proc_type == 1 && proc_mode == 0:
		return &"update"
	return null

func _fs_physics_process():
	if proc_type == 1 && proc_mode == 1:
		return &"update"
	return null