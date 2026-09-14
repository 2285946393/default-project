import "./style.css";
import { createContext } from "./gl";
import { Simulation, QUALITIES, paramsFromGenome, type SimParams } from "./simulation";
import { genomeFromSeed, randomSeedWord, SPAWN_PATTERNS, type SpawnPattern } from "./genome";
import { PALETTES } from "./palettes";
import { UI } from "./ui";

const PARAM_ORDER: (keyof SimParams)[] = [
  "sensorAngle", "sensorDistance", "turnSpeed", "stepSize", "decay",
  "diffuse", "deposit", "species", "crossAttraction", "wander", "exposure", "hueShift",
];

function encodeParams(p: SimParams): string {
  return PARAM_ORDER.map((k) => Number(p[k]).toFixed(4)).join("_");
}
function decodeParams(s: string): Partial<SimParams> {
  const parts = s.split("_").map(Number);
  const out: Partial<SimParams> = {};
  PARAM_ORDER.forEach((k, i) => {
    if (i < parts.length && Number.isFinite(parts[i])) out[k] = parts[i];
  });
  return out;
}

function fatal(message: string): void {
  const root = document.getElementById("ui-root")!;
  root.innerHTML = "";
  const box = document.createElement("div");
  box.style.cssText =
    "position:fixed;inset:0;display:grid;place-items:center;text-align:center;padding:30px;color:#eaf2ff;font-family:system-ui,sans-serif";
  box.innerHTML = `<div><div style="font-size:22px;letter-spacing:.2em;margin-bottom:14px">菌丝 · MYCELIA</div>
    <div style="color:#8593ab;max-width:440px;line-height:1.6">${message}</div>
    <div style="color:#56617a;margin-top:16px;font-size:12px">需要支持 WebGL2 与浮点渲染目标的浏览器（Chrome、Edge、Firefox、Safari 15+）。</div></div>`;
  root.append(box);
}

function boot(): void {
  const canvas = document.getElementById("stage") as HTMLCanvasElement;

  let gl: WebGL2RenderingContext;
  try {
    gl = createContext(canvas);
  } catch (e) {
    fatal((e as Error).message);
    return;
  }

  // ---- State from URL (shareable) ----
  const url = new URL(window.location.href);
  const initialSeed = url.searchParams.get("seed") ?? randomSeedWord();
  let genome = genomeFromSeed(initialSeed);
  let paletteIndex = PALETTES.indexOf(genome.palette);
  let curSpawn: SpawnPattern = genome.spawn;

  // Full million agents on desktop (physarum this size is cheap on real GPUs);
  // step down on narrow/mobile screens where fill-rate is the bottleneck.
  const quality = window.innerWidth < 820 ? QUALITIES.medium : QUALITIES.high;
  const sim = new Simulation(gl, genome, quality);

  // The display framebuffer follows the screen (crisp UI + grain); the
  // simulation runs at a fixed, dense resolution so the organism has a
  // consistent scale and per-species density on every monitor, then is
  // upscaled smoothly. SIM_MAX is the long edge of the simulation grid.
  const SIM_MAX = 1024;
  function sizeCanvas(): void {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const vw = window.innerWidth || 1280;
    const vh = window.innerHeight || 720;
    canvas.width = Math.max(2, Math.min(2560, Math.floor(vw * dpr)));
    canvas.height = Math.max(2, Math.min(1600, Math.floor(vh * dpr)));

    const aspect = vw / vh;
    let sw: number;
    let sh: number;
    if (aspect >= 1) {
      sw = SIM_MAX;
      sh = Math.max(2, Math.round(SIM_MAX / aspect));
    } else {
      sh = SIM_MAX;
      sw = Math.max(2, Math.round(SIM_MAX * aspect));
    }
    sim.resize(sw, sh);
  }
  sizeCanvas();

  // ---- Apply any shared overrides on top of the seed's genome ----
  const sharedParams = url.searchParams.get("p");
  if (sharedParams) {
    sim.setParams(decodeParams(sharedParams));
    const pal = Number(url.searchParams.get("pal"));
    if (Number.isFinite(pal) && PALETTES[pal]) {
      paletteIndex = pal;
      sim.setPalette(PALETTES[pal]);
    }
    const sp = url.searchParams.get("spawn") as SpawnPattern | null;
    if (sp && SPAWN_PATTERNS.includes(sp)) {
      curSpawn = sp;
      sim.setSpawn(sp);
    }
  }

  // ---- UI ----
  const ui = new UI(document.getElementById("ui-root")!, genome.seed, {
    onGrow: (seed) => grow(seed),
    onRandom: () => {
      const s = randomSeedWord();
      ui.setSeed(s);
      grow(s);
    },
    onParam: (key, value) => sim.setParams({ [key]: value } as Partial<SimParams>),
    onSpecies: (n) => {
      sim.setParams({ species: n });
      ui.syncParams(sim.params);
    },
    onSpawn: (s) => {
      curSpawn = s;
      sim.setSpawn(s);
      ui.setSpawn(s);
    },
    onPaletteCycle: (dir) => {
      paletteIndex = (paletteIndex + dir + PALETTES.length) % PALETTES.length;
      sim.setPalette(PALETTES[paletteIndex]);
    },
    onPauseToggle: () => {
      sim.paused = !sim.paused;
      ui.setPaused(sim.paused);
    },
    onSave: () => savePNG(),
    onShare: () => share(),
    onReset: () => {
      sim.regrow();
      ui.toast("已重新生长");
    },
    onTogglePanel: () => ui.togglePanel(),
  });

  ui.syncParams(sim.params);
  ui.setSpawn(curSpawn);
  ui.onVeilDismiss(() => ui.dismissVeil());

  function grow(seed: string): void {
    genome = genomeFromSeed(seed);
    paletteIndex = PALETTES.indexOf(genome.palette);
    curSpawn = genome.spawn;
    sim.applyGenome(genome);
    ui.setSeed(genome.seed);
    ui.syncParams(sim.params);
    ui.setSpawn(curSpawn);
    const clean = new URL(window.location.href);
    clean.search = `?seed=${encodeURIComponent(genome.seed)}`;
    history.replaceState(null, "", clean.pathname + `?seed=${encodeURIComponent(genome.seed)}`);
    ui.toast(`已生长 · ${genome.palette.name}`);
  }

  function shareURL(): string {
    const base = window.location.origin + window.location.pathname;
    const sp = new URLSearchParams();
    sp.set("seed", genome.seed);
    const baseParams = paramsFromGenome(genome);
    const tweaked =
      PARAM_ORDER.some((k) => Math.abs(Number(sim.params[k]) - Number(baseParams[k])) > 1e-4) ||
      paletteIndex !== PALETTES.indexOf(genome.palette) ||
      curSpawn !== genome.spawn;
    if (tweaked) {
      sp.set("p", encodeParams(sim.params));
      sp.set("pal", String(paletteIndex));
      sp.set("spawn", curSpawn);
    }
    return `${base}?${sp.toString()}`;
  }

  async function share(): Promise<void> {
    const link = shareURL();
    try {
      await navigator.clipboard.writeText(link);
      ui.toast("链接已复制——同一种子，同一只生物");
    } catch {
      ui.toast(link);
    }
  }

  function savePNG(): void {
    sim.draw(); // guarantee a fresh frame in the buffer
    canvas.toBlob((blob) => {
      if (!blob) return;
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = `mycelia-${genome.seed.replace(/[^a-z0-9-_]/gi, "_")}.png`;
      a.click();
      URL.revokeObjectURL(a.href);
      ui.toast("图片已保存");
    }, "image/png");
  }

  // ---- Pointer / touch interaction ----
  function updatePointer(e: PointerEvent, pressing?: boolean): void {
    const rect = canvas.getBoundingClientRect();
    sim.mouse.x = e.clientX - rect.left;
    sim.mouse.y = e.clientY - rect.top;
    sim.mouse.inside = true;
    sim.mouse.repel = e.shiftKey || e.button === 2 || (pressing === true && e.ctrlKey);
    if (pressing !== undefined) sim.mouse.pressing = pressing;
  }
  canvas.addEventListener("pointermove", (e) => updatePointer(e));
  canvas.addEventListener("pointerdown", (e) => {
    updatePointer(e, true);
    canvas.setPointerCapture(e.pointerId);
  });
  canvas.addEventListener("pointerup", (e) => {
    updatePointer(e, false);
  });
  canvas.addEventListener("pointerleave", () => {
    sim.mouse.inside = false;
    sim.mouse.pressing = false;
  });
  canvas.addEventListener("contextmenu", (e) => e.preventDefault());

  // ---- Keyboard shortcuts ----
  window.addEventListener("keydown", (e) => {
    if (document.activeElement instanceof HTMLInputElement) return;
    switch (e.key) {
      case " ":
        e.preventDefault();
        sim.paused = !sim.paused;
        ui.setPaused(sim.paused);
        break;
      case "s": savePNG(); break;
      case "r": sim.regrow(); ui.toast("已重新生长"); break;
      case "g": ui.togglePanel(); break;
      case "ArrowRight": paletteIndex = (paletteIndex + 1) % PALETTES.length; sim.setPalette(PALETTES[paletteIndex]); break;
      case "ArrowLeft": paletteIndex = (paletteIndex - 1 + PALETTES.length) % PALETTES.length; sim.setPalette(PALETTES[paletteIndex]); break;
    }
  });

  // ---- Resize (debounced) ----
  let resizeTimer = 0;
  window.addEventListener("resize", () => {
    window.clearTimeout(resizeTimer);
    resizeTimer = window.setTimeout(sizeCanvas, 160);
  });

  // ---- Dev-only fast-forward hook (stripped from production builds) ----
  if (import.meta.env.DEV) {
    (window as unknown as Record<string, unknown>).__mycelia = {
      sim,
      burn: (n = 400) => {
        for (let i = 0; i < n; i++) sim.step();
        sim.draw();
      },
    };
  }

  // ---- Main loop ----
  let last = performance.now();
  let fps = 60;
  function frame(now: number): void {
    const dt = now - last;
    last = now;
    fps += ((1000 / Math.max(1, dt)) - fps) * 0.1;
    // Two substeps per frame so the organism grows in about ~15s rather than
    // a minute — the sim is far cheaper than the display pass.
    sim.step();
    sim.step();
    sim.draw();
    ui.setStats(fps, sim.agentCount, sim.paletteName);
    requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
}

boot();
