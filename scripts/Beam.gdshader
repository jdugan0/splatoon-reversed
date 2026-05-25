shader_type spatial;
render_mode blend_mix, cull_disabled, unshaded;
uniform vec4 beam_color : source_color = vec4(0.0, 0.6, 1.0, 1.0);
uniform sampler2D noise_texture : repeat_enable;
uniform vec2 flow = vec2(0.0,0.5);
uniform float displacement = 0.01;
void fragment() {
	vec2 uv = UV - flow * TIME;
    float n = texture(noise_texture, uv).r;
    ALBEDO = beam_color.rgb;
    ALPHA = beam_color.a * n;
}
void vertex(){
	vec2 uv = UV - flow * TIME;
    float n = texture(noise_texture, uv).r;
    VERTEX += NORMAL * (n - 0.5) * displacement;
}