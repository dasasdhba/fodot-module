shader_type canvas_item;

uniform float zoom = 1.0; 

void fragment() {
	vec2 c = vec2(0.5, 0.5);
	vec2 d = UV - c;
    vec2 uv = c + d / zoom;
	vec4 color = texture(TEXTURE, uv);
	COLOR = color;
}