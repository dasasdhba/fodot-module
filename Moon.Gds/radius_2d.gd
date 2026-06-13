extends Node2D
class_name Radius2D

@export var speed : float = 200.0
@export var radius := Vector2(0.0, 16.0)
@export_range(-360.0, 360.0, 0.1, "degrees") var phase_degrees = 0.0
@export var transposed := false
@export var disabled := false
@export_enum("Idle", "Physics") var proc_mode = 1

var phase:
	get:
		return deg_to_rad(phase_degrees)
	set(value):
		phase_degrees = rad_to_deg(value)

@onready var origin := position

func _ready():
	set_process(proc_mode == 0)
	set_physics_process(proc_mode == 1)

func update(delta):
	phase_degrees = wrap(phase_degrees + speed * delta, -180, 180)
	var rotated := Vector2(cos(phase), sin(phase)) * radius
	if transposed:
		rotated = Vector2(rotated.y, rotated.x)
	position = origin + rotated

func _physics_process(delta):
	update(delta)

func _process(delta):
	update(delta)