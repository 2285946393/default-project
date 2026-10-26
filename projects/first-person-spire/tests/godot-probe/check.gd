extends SceneTree

var checks := 0
var failures := 0

func check(condition: bool, label: String) -> void:
	checks += 1
	if not condition:
		failures += 1
		push_error(label)

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var source := SubViewport.new()
	source.size = Vector2i(1280, 720)
	root.add_child(source)
	source.canvas_transform = Transform2D(0.0, Vector2(31, 47)).scaled(Vector2(1.2, 1.2))
	source.global_canvas_transform = Transform2D(0.0, Vector2(13, 17)).scaled(Vector2(0.8, 0.8))
	var parent := Node2D.new()
	parent.position = Vector2(120, 90)
	parent.scale = Vector2(0.7, 0.7)
	source.add_child(parent)
	var body := Control.new()
	body.position = Vector2(100, 80)
	parent.add_child(body)
	var hitbox := Control.new()
	hitbox.position = Vector2(-30, -40)
	hitbox.size = Vector2(60, 80)
	body.add_child(hitbox)
	await process_frame
	var actual := hitbox.get_global_transform_with_canvas()
	var full := source.global_canvas_transform * source.canvas_transform * hitbox.get_global_transform()
	var canvas_only := source.canvas_transform * hitbox.get_global_transform()
	check(actual.is_equal_approx(canvas_only), "Godot 4.5.1 global-with-canvas excludes the viewport global canvas transform")
	var before := full * (hitbox.size / 2)
	var shift := Vector2(37, -28)
	body.global_position += body.get_canvas_transform().affine_inverse().basis_xform(shift)
	await process_frame
	var after := source.global_canvas_transform * hitbox.get_global_transform_with_canvas() * (hitbox.size / 2)
	check((after - before).distance_to(shift * source.global_canvas_transform.get_scale()) < 0.001, "affine inverse must produce the expected viewport shift")

	var capture := SubViewport.new()
	capture.size = Vector2i(100, 100)
	capture.world_2d = source.find_world_2d()
	source.add_child(capture)
	var crop_origin := Vector2(50, 60)
	var factor := 0.5
	capture.canvas_transform = Transform2D(0.0, -crop_origin) * source.canvas_transform
	capture.global_canvas_transform = Transform2D.IDENTITY.scaled(Vector2.ONE * factor)
	var capture_point := capture.global_canvas_transform * capture.canvas_transform * hitbox.get_global_transform() * (hitbox.size / 2)
	var source_point := source.global_canvas_transform.affine_inverse() * after
	check(capture_point.distance_to((source_point - crop_origin) * factor) < 0.001, "capture crop must follow the source canvas transform")
	var overlay := Node2D.new()
	parent.add_child(overlay)
	overlay.global_transform = overlay.get_canvas_transform().affine_inverse()
	await process_frame
	var aligned := overlay.get_global_transform_with_canvas()
	# 浮点矩阵连乘允许千分之一像素误差，不能用接近零时的相对误差判断。
	check(aligned.origin.length() < 0.001 and aligned.x.distance_to(Vector2.RIGHT) < 0.001 and aligned.y.distance_to(Vector2.DOWN) < 0.001, "overlay must cancel the parent and canvas scale")
	check(capture.find_world_2d() == source.find_world_2d(), "capture must share the original World2D")
	check(body.get_canvas() == source.find_world_2d().canvas, "default canvas must be capturable")
	for index in range(20, 28):
		body.visibility_layer = 1 << index
		capture.canvas_cull_mask = 1 << index
		check(body.visibility_layer == capture.canvas_cull_mask, "high layer bits must survive the engine properties")
	# 固定身体参考点：动画点击框位移不能累计改变站位；重复转头后目标仍对齐。
	var stable_anchor: Vector2 = body.get_global_transform().affine_inverse() * (hitbox.get_global_transform() * (hitbox.size / 2))
	var stage := SubViewport.new()
	stage.size = source.size
	stage.own_world_3d = true
	root.add_child(stage)
	var camera := Camera3D.new()
	camera.position = Vector3(0, 1.3, 0)
	camera.fov = 60
	stage.add_child(camera)
	await process_frame
	var companion_target := Vector2(180, 475)
	for frame in range(60):
		hitbox.position = Vector2(-30 + sin(frame * 0.2) * 12, -40)
		var anchor_before := body.get_global_transform_with_canvas() * stable_anchor
		body.global_position += body.get_canvas_transform().affine_inverse().basis_xform(companion_target - anchor_before)
		check((body.get_global_transform_with_canvas() * stable_anchor).distance_to(companion_target) < 0.01, "animated hitbox must not move the stable body anchor")
		camera.rotation = Vector3.ZERO
		var center := hitbox.get_global_transform_with_canvas() * (hitbox.size / 2)
		var projected := camera.project_position(center, 3.8)
		camera.rotation = Vector3(-0.03, sin(frame * 0.2) * 0.11, 0)
		var rotated_center := camera.unproject_position(projected)
		body.global_position += body.get_canvas_transform().affine_inverse().basis_xform(rotated_center - center)
		var moved_center := hitbox.get_global_transform_with_canvas() * (hitbox.size / 2)
		check(moved_center.distance_to(rotated_center) < 0.01, "companion hitbox must follow the rotated projection")
	stage.queue_free()
	source.queue_free()
	print("Coordinate checks: %d; failures: %d (no rendering proof)" % [checks, failures])
	quit(0 if failures == 0 else 1)
