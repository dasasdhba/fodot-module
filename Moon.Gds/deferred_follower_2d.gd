extends Node2D

@export var rate : float = 15.0
@export_enum("Idle", "Physics") var proc_mode = 1
@export_node_path("CanvasItem") var tracking_node : NodePath = ".."
@export_node_path var reparent_root: NodePath = "../.."

var parent

func _ready():
	set_process(proc_mode == 0)
	set_physics_process(proc_mode == 1)
	
	parent = get_node(tracking_node)
	var root = get_node(reparent_root)
	reparent.call_deferred(root)

func update(delta):
	if not is_instance_valid(parent) or not parent.is_inside_tree() or not parent.is_visible_in_tree():
		hide()
		return

	var target = parent.global_position
	if visible:
		global_position = global_position.move_toward(
			target, (target - global_position).length() * delta * rate)
	else:
		show()
		global_position = target

func _physics_process(delta):
	update(delta)

func _process(delta):
	update(delta)