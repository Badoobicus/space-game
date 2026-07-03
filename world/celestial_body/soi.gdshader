shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_opaque; 

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;

void vertex() {
	vec3 scale = vec3(
		length(MODELVIEW_MATRIX[0].xyz),
		length(MODELVIEW_MATRIX[1].xyz),
		length(MODELVIEW_MATRIX[2].xyz)
	);

	MODELVIEW_MATRIX[0].xyz = vec3(scale.x, 0.0, 0.0);
	MODELVIEW_MATRIX[1].xyz = vec3(0.0, scale.y, 0.0);
	MODELVIEW_MATRIX[2].xyz = vec3(0.0, 0.0, scale.z);
}

void fragment() {
	float ring_thickness = 0.01;
	float ring_sharpness = 0.001;
	
	vec2 center_uv = UV - vec2(0.5);
	float dist = length(center_uv);
	float target_radius = 0.5;
	
	float outer_edge = smoothstep(target_radius, target_radius - ring_sharpness, dist);
	float inner_edge = pow(dist * 2.0, 50.0);
	float ring_mask = outer_edge * inner_edge;
	
	vec4 tex_color = texture(albedo_texture, UV);
	
	ALBEDO = vec3(1.0);
	ALPHA = ring_mask;
}