extends Node2D

@export_category("Fade")
@export var color = Color(0, 0, 0)
@export var fade_in_time = 0.4
@export var fade_in_wait = 0.1
@export var fade_out_time = 0.4
@export var fade_out_wait = 0.1
@export var size = Vector2(800, 600)

signal fade_in_finished
signal fade_out_finished

func _draw():
	draw_rect(Rect2(Vector2(0, 0), size), color)

func _get_fscripts():
	return [ "cutscene" ]

func fade_in_init():
	modulate.a = 0.0

func fade_out_init():
	modulate.a = 1.0

func fade_in():
	modulate.a = 0.0
	var tween = create_tween()
	tween.tween_property(self, "modulate:a", 1.0, fade_in_time)
	tween.tween_interval(fade_in_wait)
	tween.tween_callback(fade_in_finished.emit)

func fade_out():
	modulate.a = 1.0
	var tween = create_tween()
	tween.tween_interval(fade_out_wait)
	tween.tween_property(self, "modulate:a", 0.0, fade_out_time)
	tween.tween_callback(fade_out_finished.emit)