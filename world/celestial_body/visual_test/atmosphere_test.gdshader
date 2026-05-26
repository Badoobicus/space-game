shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, unshaded;

uniform vec4 atmosphere_color : source_color = vec4(0.3, 0.7, 1.0, 1.0);
uniform float thickness : hint_range(0.0, 5.0) = 2.0;
uniform float halo_spread : hint_range(0.1, 5.0) = 1.5;
uniform float expansion : hint_range(1.0, 2.0) = 1.2;

varying vec3 v_pos;
varying vec3 v_normal;

void vertex() {
    VERTEX *= expansion;
    v_pos = VERTEX;
    v_normal = NORMAL;
}

void fragment() {
    float alignment = dot(NORMAL, VIEW);
    float intensity = 1.0 - alignment;
	float final_alpha = intensity;

    ALBEDO = atmosphere_color.rgb;
    ALPHA = clamp(final_alpha, 0.0, 1.0);
}