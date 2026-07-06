extends Node3D
class_name Z_WorldTicker

const TICK_RATE: int = 30
const TICK_DURATION: float = 1.0 / TICK_RATE  # 33.33 мс

var _accumulator: float = 0.0
var _tick_count: int = 0
var _current_tps: float = 30.0

signal tick(tick_id: int, delta: float)

static var instance: Z_WorldTicker

func _ready() -> void:
	instance = self
	Panku.gd_exprenv.register_env("WorldTicker", self)

static func subscribe_to_tick_event(callable: Callable) -> void:
	instance.tick.connect(callable)

static func unsubscribe_from_tick_event(callable: Callable) -> void:
	instance.tick.disconnect(callable)

func _physics_process(delta: float) -> void:
	_accumulator += delta
	var start_time = Time.get_ticks_usec()
	var ticks_processed: int = 0

	while _accumulator >= TICK_DURATION:
		_game_tick()
		_accumulator -= TICK_DURATION
		ticks_processed += 1

	if ticks_processed > 0:
		_update_tps_measurement(start_time, ticks_processed)

func _game_tick() -> void:
	_tick_count += 1
	tick.emit(_tick_count, TICK_DURATION)

var _tps_samples: Array[float] = []
const SAMPLE_COUNT: int = 20

func _update_tps_measurement(start_time: int, ticks_processed: int) -> void:
	var elapsed_us: int = Time.get_ticks_usec() - start_time
	var elapsed_ms: float = elapsed_us / 1000.0
	var mspt: float = elapsed_ms / float(ticks_processed)

	var current_tps: float = 1000.0 / mspt if mspt > 0 else 30.0
	current_tps = min(current_tps, 30.0)

	_tps_samples.append(current_tps)
	if _tps_samples.size() > SAMPLE_COUNT:
		_tps_samples.pop_front()

	_current_tps = 0.0
	for sample in _tps_samples:
		_current_tps += sample
	_current_tps /= float(_tps_samples.size())

static func get_tps() -> float:
	return instance._current_tps

static func get_mspt() -> float:
	if instance._current_tps > 0:
		return 1000.0 / instance._current_tps
	return 0.0

static func get_tick_count() -> int:
	return instance._tick_count
