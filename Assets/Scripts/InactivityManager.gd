extends Node

# 1.5 minutes in seconds.
var inactivity_kick_time: float = 90;
var timer: float = 0;

func _enter_tree() -> void:
    process_mode = Node.PROCESS_MODE_ALWAYS;

func _process(_delta: float) -> void:
    if (timer >= inactivity_kick_time):
        get_tree().quit();
    if (Input.is_anything_pressed()):
        timer = 0;
    else:
        timer += _delta;
