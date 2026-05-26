shader_type spatial;
render_mode unshaded, depth_draw_never, blend_mix;

void fragment() {
	ALPHA = pow(1.0 - dot(NORMAL, VIEW), 8.0);
}
