shader_type spatial;
uniform sampler2D clean_texture : source_color, filter_nearest, hint_default_transparent;
uniform sampler2D dirty_texture : source_color, repeat_enable, filter_nearest;
uniform sampler2D dirt_mask : hint_default_black, repeat_disable, filter_nearest;
uniform vec2 dirty_tiling = vec2(1.0);
render_mode depth_prepass_alpha;

void fragment() {
    vec4  c = texture(clean_texture, UV).rgba;
    vec4 d = texture(dirty_texture, UV * dirty_tiling).rgba;
    float m = texture(dirt_mask, UV).r;
    ALBEDO = mix(d.rgb, c.rgb, m);
	ALPHA  = mix(d.a, c.a,m);
}