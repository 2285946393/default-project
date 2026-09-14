// All GLSL ES 3.00 shader sources for the simulation, as string constants.

export const QUAD_VERT = /* glsl */ `#version 300 es
layout(location = 0) in vec2 aPos;
out vec2 vUv;
void main() {
  vUv = aPos * 0.5 + 0.5;
  gl_Position = vec4(aPos, 0.0, 1.0);
}`;

// ---------------------------------------------------------------------------
// AGENT UPDATE — each texel is one agent (x_px, y_px, heading, species).
// Agents sense the pheromone field along three whiskers, steer toward the
// strongest of their own species, move, and wrap toroidally.
// ---------------------------------------------------------------------------
export const UPDATE_FRAG = /* glsl */ `#version 300 es
precision highp float;
precision highp int;

uniform sampler2D uAgents;
uniform sampler2D uTrail;
uniform vec2 uResolution;
uniform float uSensorAngle;
uniform float uSensorDist;
uniform float uTurn;
uniform float uStep;
uniform float uWander;
uniform float uCross;
uniform float uFrame;
uniform vec2  uMouse;      // px
uniform float uMouseMode;  // 0 none, 1 attract, 2 repel
uniform float uMouseRadius;

out vec4 frag;

float hash21(vec2 p) {
  p = fract(p * vec2(123.34, 456.21));
  p += dot(p, p + 45.32);
  return fract(p.x * p.y);
}

vec3 speciesMask(float sp) {
  float m0 = step(sp, 0.5);
  float m1 = step(0.5, sp) * step(sp, 1.5);
  float m2 = step(1.5, sp);
  return vec3(m0, m1, m2);
}

float senseAt(vec2 pos, float ang, vec3 mask) {
  vec2 p = pos + vec2(cos(ang), sin(ang)) * uSensorDist;
  vec3 t = texture(uTrail, fract(p / uResolution)).rgb;
  float own = dot(t, mask);
  float others = (t.r + t.g + t.b) - own;
  return own + uCross * others;
}

void main() {
  ivec2 texel = ivec2(gl_FragCoord.xy);
  vec4 a = texelFetch(uAgents, texel, 0);
  vec2 pos = a.xy;
  float heading = a.z;
  float sp = a.w;
  vec3 mask = speciesMask(sp);

  float c = senseAt(pos, heading, mask);
  float l = senseAt(pos, heading + uSensorAngle, mask);
  float r = senseAt(pos, heading - uSensorAngle, mask);

  float rnd = hash21(gl_FragCoord.xy + uFrame * 0.61803);

  if (c > l && c > r) {
    // straight ahead is best — hold course
  } else if (c < l && c < r) {
    heading += (rnd - 0.5) * 2.0 * uTurn; // trapped: random escape
  } else if (r > l) {
    heading -= uTurn;
  } else if (l > r) {
    heading += uTurn;
  }

  heading += (hash21(gl_FragCoord.yx + uFrame * 0.31) - 0.5) * uWander;

  // Cursor as food (attract) or predator (repel).
  if (uMouseMode > 0.5) {
    vec2 toM = uMouse - pos;
    float d = length(toM);
    if (d < uMouseRadius && d > 0.001) {
      float desired = atan(toM.y, toM.x);
      if (uMouseMode > 1.5) desired += 3.14159265;
      float diff = atan(sin(desired - heading), cos(desired - heading));
      heading += clamp(diff, -uTurn, uTurn) * (1.0 - d / uMouseRadius) * 0.9;
    }
  }

  pos += vec2(cos(heading), sin(heading)) * uStep;
  pos = mod(pos, uResolution);

  frag = vec4(pos, heading, sp);
}`;

// ---------------------------------------------------------------------------
// DEPOSIT — draw one GL_POINT per agent (pulled from the agent texture by
// gl_VertexID) into the trail field, additively, in the agent's own channel.
// ---------------------------------------------------------------------------
export const DEPOSIT_VERT = /* glsl */ `#version 300 es
precision highp float;
uniform sampler2D uAgents;
uniform vec2 uResolution;
uniform int uAgentTexW;
flat out float vSpecies;
void main() {
  int id = gl_VertexID;
  int x = id % uAgentTexW;
  int y = id / uAgentTexW;
  vec4 a = texelFetch(uAgents, ivec2(x, y), 0);
  vec2 p = a.xy / uResolution;
  gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
  gl_PointSize = 1.0;
  vSpecies = a.w;
}`;

export const DEPOSIT_FRAG = /* glsl */ `#version 300 es
precision highp float;
flat in float vSpecies;
uniform float uDeposit;
out vec4 frag;
void main() {
  float sp = vSpecies;
  float m0 = step(sp, 0.5);
  float m1 = step(0.5, sp) * step(sp, 1.5);
  float m2 = step(1.5, sp);
  frag = vec4(vec3(m0, m1, m2) * uDeposit, 0.0);
}`;

// ---------------------------------------------------------------------------
// DIFFUSE + DECAY — 3x3 blur blended by diffuse rate, then multiplied by
// decay. Also injects "food" at the cursor when feeding.
// ---------------------------------------------------------------------------
export const DIFFUSE_FRAG = /* glsl */ `#version 300 es
precision highp float;
in vec2 vUv;
uniform sampler2D uTrail;
uniform vec2 uTexel;
uniform float uDecay;
uniform float uDiffuse;
uniform vec2 uFeedPos;    // uv
uniform float uFeedRadius; // uv
uniform float uFeedAmt;
out vec4 frag;
void main() {
  vec3 c = texture(uTrail, vUv).rgb;
  vec3 sum = vec3(0.0);
  for (int y = -1; y <= 1; y++) {
    for (int x = -1; x <= 1; x++) {
      sum += texture(uTrail, fract(vUv + vec2(float(x), float(y)) * uTexel)).rgb;
    }
  }
  sum /= 9.0;
  vec3 outc = mix(c, sum, uDiffuse) * uDecay;

  if (uFeedAmt > 0.0) {
    float d = distance(vUv, uFeedPos) / uFeedRadius;
    outc += exp(-d * d * 4.0) * uFeedAmt;
  }

  frag = vec4(outc, 1.0);
}`;

// ---------------------------------------------------------------------------
// DISPLAY — map the three species fields to colour, add a cheap bloom glow,
// tone-map, hue-shift, vignette, and film grain.
// ---------------------------------------------------------------------------
export const DISPLAY_FRAG = /* glsl */ `#version 300 es
precision highp float;
in vec2 vUv;
uniform sampler2D uTrail;
uniform vec2 uTexel;
uniform vec3 uBg;
uniform vec3 uCol0;
uniform vec3 uCol1;
uniform vec3 uCol2;
uniform float uExposure;
uniform float uHueShift;
uniform float uRef; // per-genome equilibrium level = deposit / (1 - decay)
uniform float uTime;
out vec4 frag;

vec3 hueRotate(vec3 col, float a) {
  const vec3 k = vec3(0.57735026);
  float cosA = cos(a);
  return col * cosA + cross(k, col) * sin(a) + k * dot(k, col) * (1.0 - cosA);
}

// Narkowicz ACES filmic approximation.
vec3 aces(vec3 x) {
  const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
  return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

vec3 sampleField(vec2 uv) {
  return texture(uTrail, uv).rgb;
}

void main() {
  vec3 f = sampleField(vUv);

  // Cheap bloom: a few wide taps of the brightest energy.
  vec3 bloom = vec3(0.0);
  for (int i = 0; i < 8; i++) {
    float ang = float(i) * 0.7853982;
    vec2 o = vec2(cos(ang), sin(ang)) * uTexel * 3.0;
    bloom += sampleField(vUv + o);
  }
  f += bloom * 0.06;

  // Normalised emission (Reinhard) per species channel. Dividing by the
  // genome's own equilibrium level makes gaps read dark, ridges read bright,
  // and mid-density paths land at ~0.6 — so structure shows for EVERY seed
  // regardless of its absolute field magnitude. Exposure is pure brightness.
  vec3 e = f / (f + uRef * 0.55);
  vec3 emissive = (uCol0 * e.r + uCol1 * e.g + uCol2 * e.b) * uExposure;
  emissive = hueRotate(emissive, uHueShift * 6.2831853);

  vec3 col = uBg + emissive;
  col = aces(col * 1.05);

  // Vignette.
  vec2 q = vUv - 0.5;
  col *= 1.0 - dot(q, q) * 0.85;

  // Film grain.
  float g = fract(sin(dot(gl_FragCoord.xy + uTime, vec2(12.9898, 78.233))) * 43758.5453);
  col += (g - 0.5) * 0.025;

  frag = vec4(col, 1.0);
}`;
