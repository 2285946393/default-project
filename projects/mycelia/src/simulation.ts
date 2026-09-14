import {
  createProgram,
  createTarget,
  createAgentTarget,
  createFullscreenTriangle,
  deleteTarget,
  Uniforms,
  type Target,
} from "./gl";
import {
  QUAD_VERT,
  UPDATE_FRAG,
  DEPOSIT_VERT,
  DEPOSIT_FRAG,
  DIFFUSE_FRAG,
  DISPLAY_FRAG,
} from "./shaders/index";
import type { Genome, SpawnPattern } from "./genome";
import type { Palette } from "./palettes";

export interface Quality {
  label: string;
  agentTex: number; // agent texture is agentTex x agentTex
}

export const QUALITIES: Record<string, Quality> = {
  low: { label: "Calm", agentTex: 512 },
  medium: { label: "Balanced", agentTex: 768 },
  high: { label: "Dense", agentTex: 1024 },
};

/** Runtime parameters — start as a copy of the genome, live-editable by sliders. */
export interface SimParams {
  sensorAngle: number;
  sensorDistance: number;
  turnSpeed: number;
  stepSize: number;
  decay: number;
  diffuse: number;
  deposit: number;
  species: number;
  crossAttraction: number;
  wander: number;
  exposure: number;
  hueShift: number;
}

export function paramsFromGenome(g: Genome): SimParams {
  return {
    sensorAngle: g.sensorAngle,
    sensorDistance: g.sensorDistance,
    turnSpeed: g.turnSpeed,
    stepSize: g.stepSize,
    decay: g.decay,
    diffuse: g.diffuse,
    deposit: g.deposit,
    species: g.species,
    crossAttraction: g.crossAttraction,
    wander: g.wander,
    exposure: g.exposure,
    hueShift: g.hueShift,
  };
}

interface Ping {
  read: Target;
  write: Target;
  swap(): void;
}

function ping(read: Target, write: Target): Ping {
  return {
    read,
    write,
    swap() {
      const t = this.read;
      this.read = this.write;
      this.write = t;
    },
  };
}

export interface MouseState {
  x: number; // css px
  y: number;
  inside: boolean;
  pressing: boolean;
  repel: boolean;
}

export class Simulation {
  private gl: WebGL2RenderingContext;
  private vao: WebGLVertexArrayObject;

  private updateProg: WebGLProgram;
  private depositProg: WebGLProgram;
  private diffuseProg: WebGLProgram;
  private displayProg: WebGLProgram;
  private uU: Uniforms;
  private uDep: Uniforms;
  private uDif: Uniforms;
  private uDis: Uniforms;

  private agents!: Ping;
  private trail!: Ping;

  private agentTex = 1024;
  private simW = 1;
  private simH = 1;
  private trailFilter: number;

  params: SimParams;
  private palette: Palette;
  private spawn: SpawnPattern;
  seed: string;

  frame = 0;
  private time = 0;
  mouse: MouseState = { x: 0, y: 0, inside: false, pressing: false, repel: false };
  paused = false;

  constructor(gl: WebGL2RenderingContext, genome: Genome, quality: Quality) {
    this.gl = gl;
    this.vao = createFullscreenTriangle(gl);
    this.updateProg = createProgram(gl, QUAD_VERT, UPDATE_FRAG);
    this.depositProg = createProgram(gl, DEPOSIT_VERT, DEPOSIT_FRAG);
    this.diffuseProg = createProgram(gl, QUAD_VERT, DIFFUSE_FRAG);
    this.displayProg = createProgram(gl, QUAD_VERT, DISPLAY_FRAG);
    this.uU = new Uniforms(gl, this.updateProg);
    this.uDep = new Uniforms(gl, this.depositProg);
    this.uDif = new Uniforms(gl, this.diffuseProg);
    this.uDis = new Uniforms(gl, this.displayProg);

    this.params = paramsFromGenome(genome);
    this.palette = genome.palette;
    this.spawn = genome.spawn;
    this.seed = genome.seed;
    this.agentTex = quality.agentTex;
    // Smooth upscale of the trail field when supported; blocky fallback otherwise.
    this.trailFilter = gl.getExtension("OES_texture_half_float_linear") ? gl.LINEAR : gl.NEAREST;
  }

  get agentCount(): number {
    return this.agentTex * this.agentTex;
  }

  /** Splitting agents across N colonies thins each one; scale deposit to keep
   *  every colony as vivid as a single-species organism. */
  private get effectiveDeposit(): number {
    return this.params.deposit * Math.max(1, Math.round(this.params.species));
  }

  get paletteName(): string {
    return this.palette.name;
  }

  /** (Re)allocate the trail field for a new resolution and reseed agents. */
  resize(simW: number, simH: number): void {
    const gl = this.gl;
    this.simW = Math.max(2, Math.floor(simW));
    this.simH = Math.max(2, Math.floor(simH));

    if (this.trail) {
      deleteTarget(gl, this.trail.read);
      deleteTarget(gl, this.trail.write);
    }
    const blank = new Float32Array(this.simW * this.simH * 4);
    this.trail = ping(
      createTarget(gl, this.simW, this.simH, blank, this.trailFilter),
      createTarget(gl, this.simW, this.simH, blank, this.trailFilter),
    );
    this.reseed();
  }

  setQuality(q: Quality): void {
    this.agentTex = q.agentTex;
    this.reseed();
  }

  applyGenome(genome: Genome): void {
    this.params = paramsFromGenome(genome);
    this.palette = genome.palette;
    this.spawn = genome.spawn;
    this.seed = genome.seed;
    this.reseed();
  }

  /** Change look/params live without regrowing (unless species/spawn changed). */
  setParams(patch: Partial<SimParams>, reseedIfSpeciesChanged = true): void {
    const speciesBefore = this.params.species;
    this.params = { ...this.params, ...patch };
    if (reseedIfSpeciesChanged && patch.species !== undefined && patch.species !== speciesBefore) {
      this.reseed();
    }
  }

  setPalette(p: Palette): void {
    this.palette = p;
  }

  setSpawn(s: SpawnPattern): void {
    this.spawn = s;
    this.reseed();
  }

  clearField(): void {
    const gl = this.gl;
    const blank = new Float32Array(this.simW * this.simH * 4);
    for (const t of [this.trail.read, this.trail.write]) {
      gl.bindTexture(gl.TEXTURE_2D, t.tex);
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA16F, this.simW, this.simH, 0, gl.RGBA, gl.FLOAT, blank);
    }
  }

  /** Wipe the field and re-scatter agents, keeping current params. */
  regrow(): void {
    this.clearField();
    this.reseed();
  }

  /** Seed agents on the CPU according to the spawn pattern, upload to GPU. */
  private reseed(): void {
    const gl = this.gl;
    const n = this.agentCount;
    const data = new Float32Array(n * 4);
    const cx = this.simW * 0.5;
    const cy = this.simH * 0.5;
    const maxR = Math.min(this.simW, this.simH) * 0.5;
    const species = Math.max(1, Math.min(3, Math.round(this.params.species)));

    for (let i = 0; i < n; i++) {
      const o = i * 4;
      let x: number;
      let y: number;
      const ang = Math.random() * Math.PI * 2;
      switch (this.spawn) {
        case "ring": {
          const rr = maxR * (0.62 + Math.random() * 0.12);
          x = cx + Math.cos(ang) * rr;
          y = cy + Math.sin(ang) * rr;
          break;
        }
        case "core": {
          const rr = maxR * 0.14 * Math.sqrt(Math.random());
          x = cx + Math.cos(ang) * rr;
          y = cy + Math.sin(ang) * rr;
          break;
        }
        case "orbit": {
          const rr = maxR * (0.2 + Math.random() * 0.6);
          x = cx + Math.cos(ang) * rr;
          y = cy + Math.sin(ang) * rr;
          break;
        }
        default: {
          x = Math.random() * this.simW;
          y = Math.random() * this.simH;
        }
      }
      data[o] = x;
      data[o + 1] = y;
      // Orbit spawn faces tangentially for a swirl; others face outward/random.
      data[o + 2] =
        this.spawn === "orbit"
          ? ang + Math.PI * 0.5
          : this.spawn === "scatter"
            ? Math.random() * Math.PI * 2
            : ang;
      data[o + 3] = i % species;
    }

    if (this.agents) {
      deleteTarget(gl, this.agents.read);
      deleteTarget(gl, this.agents.write);
    }
    this.agents = ping(
      createAgentTarget(gl, this.agentTex, this.agentTex, data),
      createAgentTarget(gl, this.agentTex, this.agentTex, new Float32Array(n * 4)),
    );
    this.frame = 0;
  }

  private mousePx(): { x: number; y: number } {
    // Mouse arrives in CSS px over the canvas; map to sim (device) px, flip Y.
    const canvas = this.gl.canvas as HTMLCanvasElement;
    const sx = this.simW / canvas.clientWidth;
    const sy = this.simH / canvas.clientHeight;
    return { x: this.mouse.x * sx, y: this.simH - this.mouse.y * sy };
  }

  step(): void {
    if (this.paused) return;
    const gl = this.gl;
    this.frame++;
    this.time += 0.016;
    const p = this.params;

    // Mouse steering: press feeds (attract), shift/right-drag repels. Hover is
    // neutral so the colony grows organically until you actually touch it.
    let mode = 0;
    if (this.mouse.inside && this.mouse.pressing) {
      mode = this.mouse.repel ? 2 : 1;
    }
    const m = this.mousePx();
    const feeding = this.mouse.inside && this.mouse.pressing && !this.mouse.repel;

    gl.disable(gl.BLEND);
    gl.bindVertexArray(this.vao);

    // --- 1. UPDATE AGENTS ---
    gl.useProgram(this.updateProg);
    gl.viewport(0, 0, this.agentTex, this.agentTex);
    gl.bindFramebuffer(gl.FRAMEBUFFER, this.agents.write.fbo);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, this.agents.read.tex);
    gl.uniform1i(this.uU.loc("uAgents"), 0);
    gl.activeTexture(gl.TEXTURE1);
    gl.bindTexture(gl.TEXTURE_2D, this.trail.read.tex);
    gl.uniform1i(this.uU.loc("uTrail"), 1);
    gl.uniform2f(this.uU.loc("uResolution"), this.simW, this.simH);
    gl.uniform1f(this.uU.loc("uSensorAngle"), p.sensorAngle);
    gl.uniform1f(this.uU.loc("uSensorDist"), p.sensorDistance);
    gl.uniform1f(this.uU.loc("uTurn"), p.turnSpeed);
    gl.uniform1f(this.uU.loc("uStep"), p.stepSize);
    gl.uniform1f(this.uU.loc("uWander"), p.wander);
    gl.uniform1f(this.uU.loc("uCross"), p.crossAttraction);
    gl.uniform1f(this.uU.loc("uFrame"), this.frame);
    gl.uniform2f(this.uU.loc("uMouse"), m.x, m.y);
    gl.uniform1f(this.uU.loc("uMouseMode"), mode);
    gl.uniform1f(this.uU.loc("uMouseRadius"), Math.min(this.simW, this.simH) * 0.18);
    gl.drawArrays(gl.TRIANGLES, 0, 3);
    this.agents.swap();

    // --- 2. DIFFUSE + DECAY (+ feed) ---
    gl.useProgram(this.diffuseProg);
    gl.viewport(0, 0, this.simW, this.simH);
    gl.bindFramebuffer(gl.FRAMEBUFFER, this.trail.write.fbo);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, this.trail.read.tex);
    gl.uniform1i(this.uDif.loc("uTrail"), 0);
    gl.uniform2f(this.uDif.loc("uTexel"), 1 / this.simW, 1 / this.simH);
    gl.uniform1f(this.uDif.loc("uDecay"), p.decay);
    gl.uniform1f(this.uDif.loc("uDiffuse"), p.diffuse);
    gl.uniform2f(this.uDif.loc("uFeedPos"), m.x / this.simW, m.y / this.simH);
    gl.uniform1f(this.uDif.loc("uFeedRadius"), 0.14);
    gl.uniform1f(this.uDif.loc("uFeedAmt"), feeding ? this.effectiveDeposit * 2.2 : 0);
    gl.drawArrays(gl.TRIANGLES, 0, 3);

    // --- 3. DEPOSIT (additive points into the just-diffused field) ---
    gl.useProgram(this.depositProg);
    gl.enable(gl.BLEND);
    gl.blendFunc(gl.ONE, gl.ONE);
    gl.bindFramebuffer(gl.FRAMEBUFFER, this.trail.write.fbo);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, this.agents.read.tex); // updated agents
    gl.uniform1i(this.uDep.loc("uAgents"), 0);
    gl.uniform2f(this.uDep.loc("uResolution"), this.simW, this.simH);
    gl.uniform1i(this.uDep.loc("uAgentTexW"), this.agentTex);
    gl.uniform1f(this.uDep.loc("uDeposit"), this.effectiveDeposit);
    gl.bindVertexArray(null); // deposit needs no attributes; drive by gl_VertexID
    gl.drawArrays(gl.POINTS, 0, this.agentCount);
    gl.disable(gl.BLEND);
    this.trail.swap();
  }

  /** Draw the current field to the default framebuffer (the visible canvas). */
  draw(): void {
    const gl = this.gl;
    const canvas = gl.canvas as HTMLCanvasElement;
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    gl.viewport(0, 0, canvas.width, canvas.height);
    gl.useProgram(this.displayProg);
    gl.bindVertexArray(this.vao);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, this.trail.read.tex);
    gl.uniform1i(this.uDis.loc("uTrail"), 0);
    gl.uniform2f(this.uDis.loc("uTexel"), 1 / this.simW, 1 / this.simH);
    const bg = this.palette.background;
    const [c0, c1, c2] = this.palette.species;
    gl.uniform3f(this.uDis.loc("uBg"), bg[0], bg[1], bg[2]);
    gl.uniform3f(this.uDis.loc("uCol0"), c0[0], c0[1], c0[2]);
    gl.uniform3f(this.uDis.loc("uCol1"), c1[0], c1[1], c1[2]);
    gl.uniform3f(this.uDis.loc("uCol2"), c2[0], c2[1], c2[2]);
    gl.uniform1f(this.uDis.loc("uExposure"), this.params.exposure);
    gl.uniform1f(this.uDis.loc("uHueShift"), this.params.hueShift);
    gl.uniform1f(this.uDis.loc("uRef"), this.effectiveDeposit / Math.max(0.01, 1 - this.params.decay));
    gl.uniform1f(this.uDis.loc("uTime"), this.time);
    gl.drawArrays(gl.TRIANGLES, 0, 3);
    gl.bindVertexArray(null);
  }
}
