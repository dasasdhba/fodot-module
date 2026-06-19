// fragment zoom
// this can be used for camera scaling
// (especially for pixel art games)

shader_type canvas_item;

uniform vec2 zoom = vec2(1.0, 1.0); 
varying vec4 modulate;

void vertex() {
	modulate = COLOR;
}

void fragment() {
	vec2 c = vec2(0.5, 0.5);
	vec2 d = UV - c;
    vec2 uv = c + d / zoom;
	vec4 color = texture(TEXTURE, uv);
	COLOR = color * modulate;
}