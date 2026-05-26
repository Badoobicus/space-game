shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, unshaded;

varying vec3 local_pos;

void vertex() {
    // In the vertex function, VERTEX is local to the model's origin (0,0,0)
    local_pos = VERTEX;
}

void fragment() {
    float dist = length(local_pos);
    vec3 color;
	float alpha;

    if (dist < 1.1) {
        color = vec3(0.0, 0.0, 0.0);
		alpha = 0.0;
    } else if (dist < 1.4) {
        color = vec3(0.55, 0.5, 0.4);
		alpha = 1.0;
    } else if (dist < 1.7) {
        color = vec3(0.0, 0.0, 0.0);
		alpha = 0.0;
    } else {
        color = vec3(0.45, 0.4, 0.4);
		alpha = 1.0;
    }

    ALBEDO = color;
	ALPHA = alpha;
}