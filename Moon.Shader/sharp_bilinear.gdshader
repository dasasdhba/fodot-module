shader_type canvas_item;

// make sure filter = linear

uniform vec2 zoom = vec2(1.0);
uniform vec2 zoom_pivot = vec2(0.5);

varying vec4 modulate;

void vertex() {
    modulate = COLOR;
}

vec2 apply_zoom(vec2 uv) {
    return (uv - zoom_pivot) / zoom + zoom_pivot;
}

vec4 sharp_bilinear(sampler2D tex, vec2 uv, vec2 texel_size) {
    vec2 ddx = dFdx(uv);
    vec2 ddy = dFdy(uv);
    vec2 uv_per_screen_pixel = sqrt(ddx * ddx + ddy * ddy);

    vec2 texel_uv = uv / texel_size;
    vec2 cell = round(texel_uv) - vec2(0.5);
    vec2 local = texel_uv - cell;

    local = clamp(
        (local - vec2(0.5)) * texel_size / uv_per_screen_pixel + vec2(0.5),
        0.0,
        1.0
    );

    vec2 snapped_uv = (cell + local) * texel_size;
    return textureGrad(tex, snapped_uv, ddx, ddy);
}

void fragment() {
    vec2 zoomed_uv = apply_zoom(UV);

    if (any(lessThan(zoomed_uv, vec2(0.0))) ||
        any(greaterThan(zoomed_uv, vec2(1.0)))) {
        COLOR = vec4(0.0);
    } else {
        COLOR = sharp_bilinear(TEXTURE, zoomed_uv, TEXTURE_PIXEL_SIZE) * modulate;
    }
}