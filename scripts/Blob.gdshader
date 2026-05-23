shader_type spatial;

uniform float amplitude = 0.15;
uniform float frequency = 4.0;
uniform float speed = 0.8;
varying vec3 v_pos;
varying vec3 v_normal;

float displace(vec3 n) {
    return sin(n.x * frequency + TIME * speed) * 0.5
         + sin(n.y * frequency * 1.3 + TIME * speed * 0.7) * 0.3
         + sin(n.z * frequency * 1.7 + TIME * speed * 0.5) * 0.2;
}

void vertex() {
	v_pos = VERTEX;
    v_normal = NORMAL;
    vec3 n = normalize(VERTEX);
    VERTEX = n * (1.0 + displace(n) * amplitude);
    vec3 t = normalize(cross(n, vec3(0.0, 1.0, 0.001)));
    vec3 b = cross(n, t);
    float eps = 0.01;
    vec3 na = normalize(n + t * eps);
    vec3 nb = normalize(n + b * eps);
    vec3 pa = na * (1.0 + displace(na) * amplitude);
    vec3 pb = nb * (1.0 + displace(nb) * amplitude);
    NORMAL = normalize(cross(pa - VERTEX, pb - VERTEX));
}
uniform float tex_scale = 1.0;
uniform float blend_sharpness = 4.0;
uniform sampler2D dirt : filter_nearest;
uniform sampler2D normal_tex : filter_linear;
uniform float normal_strength = 1.0;

void fragment() {
    vec3 b = pow(abs(v_normal), vec3(blend_sharpness));
    b /= b.x + b.y + b.z;

    vec2 ux = v_pos.yz * tex_scale;
    vec2 uy = v_pos.xz * tex_scale;
    vec2 uz = v_pos.xy * tex_scale;

    ALBEDO = texture(dirt, ux).rgb * b.x
           + texture(dirt, uy).rgb * b.y
           + texture(dirt, uz).rgb * b.z;

    vec3 nx = texture(normal_tex, ux).xyz * 2.0 - 1.0;
    vec3 ny = texture(normal_tex, uy).xyz * 2.0 - 1.0;
    vec3 nz = texture(normal_tex, uz).xyz * 2.0 - 1.0;
    nx.xy *= normal_strength;
    ny.xy *= normal_strength;
    nz.xy *= normal_strength;
    nx = vec3(nx.xy + v_normal.zy, abs(nx.z) * v_normal.x);
    ny = vec3(ny.xy + v_normal.xz, abs(ny.z) * v_normal.y);
    nz = vec3(nz.xy + v_normal.xy, abs(nz.z) * v_normal.z);

    vec3 obj_n = normalize(nx.zyx * b.x + ny.xzy * b.y + nz.xyz * b.z);
    NORMAL = normalize((VIEW_MATRIX * MODEL_MATRIX * vec4(obj_n, 0.0)).xyz);
}