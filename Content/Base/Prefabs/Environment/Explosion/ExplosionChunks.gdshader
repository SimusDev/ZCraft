shader_type spatial;
render_mode unshaded, cull_back;

varying vec4 tint;
varying float facet;
varying vec3 local_position;
varying flat float instance_seed;

void vertex() {
	tint = COLOR;
	local_position = VERTEX;
	instance_seed = float(INSTANCE_ID) * 0.75487767;
	vec3 direction = normalize(MODEL_NORMAL_MATRIX * NORMAL);
	vec3 light_direction = normalize(vec3(-0.45, 0.85, -0.31));
	facet = 0.67 + 0.33 * max(dot(direction, light_direction), 0.0);
}

void fragment() {
	vec3 cell = floor(local_position * 23.0 + vec3(0.17, 0.31, 0.43));
	float mask = fract(52.9829189 * fract(dot(cell, vec3(0.06711056, 0.00583715, 0.04352717)) + instance_seed));
	if (tint.a <= mask) {
		discard;
	}
	ALBEDO = tint.rgb * facet;
}
