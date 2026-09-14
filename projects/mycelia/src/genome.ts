// Deterministic genome: a string seed -> a fixed set of organism parameters.
// The same seed always produces byte-identical behaviour, so a shared URL
// regrows the exact same creature. (This is the whole conceptual hook.)

import { PALETTES, type Palette } from "./palettes";

/** xmur3 string hash -> 32-bit seed generator. */
function xmur3(str: string): () => number {
  let h = 1779033703 ^ str.length;
  for (let i = 0; i < str.length; i++) {
    h = Math.imul(h ^ str.charCodeAt(i), 3432918353);
    h = (h << 13) | (h >>> 19);
  }
  return () => {
    h = Math.imul(h ^ (h >>> 16), 2246822507);
    h = Math.imul(h ^ (h >>> 13), 3266489909);
    h ^= h >>> 16;
    return h >>> 0;
  };
}

/** mulberry32 PRNG — tiny, fast, deterministic, good enough for genome draws. */
function mulberry32(a: number): () => number {
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export type SpawnPattern = "scatter" | "ring" | "core" | "orbit";
export const SPAWN_PATTERNS: SpawnPattern[] = ["scatter", "ring", "core", "orbit"];

export interface Genome {
  seed: string;
  // Steering & motion (pixel-space units at reference resolution).
  sensorAngle: number; // radians between centre and side sensors
  sensorDistance: number; // px ahead the agent samples
  turnSpeed: number; // radians per step it can rotate
  stepSize: number; // px moved per step
  // Trail field dynamics.
  decay: number; // 0..1 multiplicative persistence per frame
  diffuse: number; // 0..1 blur blend
  deposit: number; // amount dropped per agent per frame
  // Ecology.
  species: number; // 1..3 interacting colonies
  crossAttraction: number; // how species react to each other (-1 avoid .. +1 attract)
  wander: number; // random jitter added to turns
  spawn: SpawnPattern;
  // Look.
  palette: Palette;
  hueShift: number;
  exposure: number;
}

function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

/** Build a fully-specified genome from an arbitrary seed string. */
export function genomeFromSeed(seed: string): Genome {
  const clean = seed.trim() === "" ? "mycelia" : seed;
  const rngSeed = xmur3(clean)();
  const r = mulberry32(rngSeed);

  // Bias species toward 2–3 (the interacting colonies are the prettiest).
  const speciesRoll = r();
  const species = speciesRoll < 0.18 ? 1 : speciesRoll < 0.62 ? 2 : 3;

  const palette = PALETTES[Math.floor(r() * PALETTES.length) % PALETTES.length];
  const spawn = SPAWN_PATTERNS[Math.floor(r() * SPAWN_PATTERNS.length) % SPAWN_PATTERNS.length];

  return {
    seed: clean,
    // Ranges chosen to stay inside the regime where agents reliably
    // self-organize into bold filamentary transport networks — not saturated
    // blobs, and not undifferentiated dust. Cross-attraction stays negative so
    // multiple colonies always carve out distinct, membrane-bounded territories.
    sensorAngle: lerp(0.4, 0.9, r()),
    sensorDistance: lerp(12, 22, r()),
    turnSpeed: lerp(0.4, 0.9, r()),
    stepSize: lerp(0.8, 1.3, r()),
    decay: lerp(0.86, 0.93, r()),
    diffuse: lerp(0.3, 0.62, r()),
    deposit: lerp(4.0, 6.0, r()),
    species,
    crossAttraction: lerp(-1.0, -0.2, r()),
    wander: lerp(0.0, 0.1, r()),
    spawn,
    palette,
    hueShift: lerp(-0.08, 0.08, r()),
    exposure: lerp(1.0, 1.6, r()),
  };
}

/** A short human-friendly word list for the dice / random-seed button. */
const WORDS = [
  "aurora", "cinder", "halcyon", "verdant", "obsidian", "seraph", "nimbus",
  "vellum", "quartz", "umbra", "tundra", "solace", "ember", "cortex",
  "gossamer", "lattice", "meridian", "hollow", "prism", "vesper", "koi",
  "marrow", "fathom", "zephyr", "cobalt", "murmur", "thorn", "glyph",
];

export function randomSeedWord(): string {
  const a = WORDS[Math.floor(Math.random() * WORDS.length)];
  const b = Math.floor(Math.random() * 900 + 100);
  return `${a}-${b}`;
}
