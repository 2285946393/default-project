import type { SimParams } from "./simulation";
import type { SpawnPattern } from "./genome";
import { SPAWN_PATTERNS } from "./genome";

export interface UICallbacks {
  onGrow: (seed: string) => void;
  onRandom: () => void;
  onParam: (key: keyof SimParams, value: number) => void;
  onSpecies: (n: number) => void;
  onSpawn: (s: SpawnPattern) => void;
  onPaletteCycle: (dir: number) => void;
  onPauseToggle: () => void;
  onSave: () => void;
  onShare: () => void;
  onReset: () => void;
  onTogglePanel: () => void;
}

interface SliderSpec {
  key: keyof SimParams;
  label: string;
  min: number;
  max: number;
  step: number;
  format: (v: number) => string;
}

const SLIDERS: SliderSpec[] = [
  { key: "sensorAngle", label: "感知角度", min: 0.1, max: 1.6, step: 0.01, format: (v) => `${Math.round((v * 180) / Math.PI)}°` },
  { key: "sensorDistance", label: "感知距离", min: 3, max: 30, step: 0.5, format: (v) => `${v.toFixed(1)}px` },
  { key: "turnSpeed", label: "转向速度", min: 0.1, max: 1.6, step: 0.01, format: (v) => v.toFixed(2) },
  { key: "stepSize", label: "移动步长", min: 0.3, max: 2.2, step: 0.02, format: (v) => v.toFixed(2) },
  { key: "decay", label: "痕迹留存", min: 0.85, max: 0.99, step: 0.002, format: (v) => v.toFixed(3) },
  { key: "diffuse", label: "扩散程度", min: 0.0, max: 0.9, step: 0.01, format: (v) => v.toFixed(2) },
  { key: "crossAttraction", label: "异种牵引", min: -1, max: 0.4, step: 0.02, format: (v) => v.toFixed(2) },
  { key: "exposure", label: "辉光强度", min: 0.5, max: 1.8, step: 0.02, format: (v) => v.toFixed(2) },
];

// 萌发形态：内部保留英文枚举（用于分享链接），界面显示中文
const SPAWN_LABEL: Record<SpawnPattern, string> = {
  scatter: "散播",
  ring: "环形",
  core: "聚核",
  orbit: "轨道",
};

function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  props: Partial<HTMLElementTagNameMap[K]> = {},
  ...children: (Node | string)[]
): HTMLElementTagNameMap[K] {
  const node = document.createElement(tag);
  Object.assign(node, props);
  for (const c of children) node.append(c);
  return node;
}

export class UI {
  private seedInput: HTMLInputElement;
  private panel: HTMLDivElement;
  private sliderEls = new Map<keyof SimParams, { input: HTMLInputElement; val: HTMLSpanElement }>();
  private speciesBtns: HTMLButtonElement[] = [];
  private spawnBtns = new Map<SpawnPattern, HTMLButtonElement>();
  private pauseBtn: HTMLButtonElement;
  private paletteReadout: HTMLElement;
  private statFps: HTMLElement;
  private statAgents: HTMLElement;
  private toastEl: HTMLDivElement;
  private veil: HTMLDivElement;
  private toastTimer = 0;

  constructor(root: HTMLElement, initialSeed: string, cb: UICallbacks) {
    // --- Masthead ---
    root.append(
      el("div", { className: "masthead" },
        el("div", { className: "wordmark" }, "菌丝 · Mycelia"),
        el("div", { className: "tagline" }, "输入一个词，长出一只专属活物"),
      ),
    );

    // --- Seed bar ---
    this.seedInput = el("input", {
      className: "seed-input",
      value: initialSeed,
      spellcheck: false,
      autocomplete: "off",
    }) as HTMLInputElement;
    this.seedInput.setAttribute("aria-label", "种子词");
    this.seedInput.addEventListener("keydown", (e) => {
      if (e.key === "Enter") cb.onGrow(this.seedInput.value);
    });
    const growBtn = el("button", { className: "primary" }, "生长 ↵");
    growBtn.onclick = () => cb.onGrow(this.seedInput.value);
    const diceBtn = el("button", { className: "icon", title: "随机种子" }, "⚄");
    diceBtn.onclick = () => cb.onRandom();

    root.append(
      el("div", { className: "seedbar" },
        el("span", { className: "seed-label" }, "种子"),
        this.seedInput,
        diceBtn,
        growBtn,
      ),
    );

    // --- Genome panel ---
    this.paletteReadout = el("b", {}, "—");
    const panelChildren: (Node | string)[] = [
      el("h2", {}, "基因组", this.paletteReadout),
    ];

    for (const s of SLIDERS) {
      const val = el("span", {}, "");
      const input = el("input", {
        type: "range",
        min: String(s.min),
        max: String(s.max),
        step: String(s.step),
      }) as HTMLInputElement;
      input.addEventListener("input", () => {
        const v = parseFloat(input.value);
        val.textContent = s.format(v);
        cb.onParam(s.key, v);
      });
      this.sliderEls.set(s.key, { input, val });
      panelChildren.push(
        el("div", { className: "row" },
          el("div", { className: "rlabel" }, el("span", {}, s.label), val),
          input,
        ),
      );
    }

    panelChildren.push(el("div", { className: "divider" }));

    // Species selector
    const speciesSeg = el("div", { className: "seg" });
    for (let n = 1; n <= 3; n++) {
      const b = el("button", {}, String(n)) as HTMLButtonElement;
      b.onclick = () => cb.onSpecies(n);
      this.speciesBtns.push(b);
      speciesSeg.append(b);
    }
    panelChildren.push(
      el("div", { className: "row" }, el("div", { className: "rlabel" }, el("span", {}, "菌落数量"), el("span", {}, "")), speciesSeg),
    );

    // Spawn selector
    const spawnSeg = el("div", { className: "seg" });
    for (const s of SPAWN_PATTERNS) {
      const b = el("button", {}, SPAWN_LABEL[s]) as HTMLButtonElement;
      b.onclick = () => cb.onSpawn(s);
      this.spawnBtns.set(s, b);
      spawnSeg.append(b);
    }
    panelChildren.push(
      el("div", { className: "row" }, el("div", { className: "rlabel" }, el("span", {}, "萌发形态"), el("span", {}, "")), spawnSeg),
    );

    // Palette cycle
    const palSeg = el("div", { className: "seg" });
    const palPrev = el("button", {}, "‹ 配色") as HTMLButtonElement;
    palPrev.onclick = () => cb.onPaletteCycle(-1);
    const palNext = el("button", {}, "配色 ›") as HTMLButtonElement;
    palNext.onclick = () => cb.onPaletteCycle(1);
    palSeg.append(palPrev, palNext);
    panelChildren.push(el("div", { className: "row" }, palSeg));

    this.panel = el("div", { className: "panel" }, ...panelChildren) as HTMLDivElement;
    root.append(this.panel);

    // --- Stats ---
    this.statFps = el("b", {}, "—");
    this.statAgents = el("b", {}, "—");
    root.append(
      el("div", { className: "stats" },
        el("span", {}, "个体 ", this.statAgents),
        el("span", {}, this.statFps, " fps"),
      ),
    );

    // --- Toolbar ---
    this.pauseBtn = el("button", { className: "icon", title: "暂停 / 播放（空格）" }, "⏸") as HTMLButtonElement;
    this.pauseBtn.onclick = () => cb.onPauseToggle();
    const resetBtn = el("button", { className: "icon", title: "重新生长（R）" }, "↻") as HTMLButtonElement;
    resetBtn.onclick = () => cb.onReset();
    const saveBtn = el("button", { title: "保存为 PNG（S）" }, "保存") as HTMLButtonElement;
    saveBtn.onclick = () => cb.onSave();
    const shareBtn = el("button", { title: "复制分享链接" }, "分享") as HTMLButtonElement;
    shareBtn.onclick = () => cb.onShare();
    const panelBtn = el("button", { className: "icon", title: "开关基因组面板（G）" }, "⚙") as HTMLButtonElement;
    panelBtn.onclick = () => cb.onTogglePanel();

    root.append(
      el("div", { className: "toolbar" }, this.pauseBtn, resetBtn, saveBtn, shareBtn, panelBtn),
    );

    // --- Toast ---
    this.toastEl = el("div", { className: "toast" }) as HTMLDivElement;
    root.append(this.toastEl);

    // --- Intro veil ---
    this.veil = el("div", { className: "veil" },
      el("div", {},
        el("div", { className: "big" }, "菌 丝"),
        el("div", { className: "sub" }, "百万只微小个体循着自己留下的化学气息，自组织成一张会呼吸的生命网络。每个词都长出不同的生物——而同一个词，永远长出同一只。"),
        el("div", { className: "hint" }, "点击任意处开始 · 按住拖动喂食，Shift+拖动驱散"),
      ),
    ) as HTMLDivElement;
    root.append(this.veil);
  }

  onVeilDismiss(fn: () => void): void {
    this.veil.addEventListener("click", fn, { once: true });
  }

  dismissVeil(): void {
    this.veil.classList.add("gone");
    // Belt-and-suspenders: also drop it out of the DOM once faded so it can
    // never intercept pointer events over the controls.
    this.veil.style.pointerEvents = "none";
    window.setTimeout(() => this.veil.remove(), 950);
  }

  togglePanel(): void {
    this.panel.classList.toggle("hidden");
  }

  setSeed(seed: string): void {
    this.seedInput.value = seed;
  }

  setPaused(paused: boolean): void {
    this.pauseBtn.textContent = paused ? "▶" : "⏸";
  }

  syncParams(p: SimParams): void {
    for (const [key, { input, val }] of this.sliderEls) {
      const spec = SLIDERS.find((s) => s.key === key)!;
      input.value = String(p[key]);
      val.textContent = spec.format(p[key] as number);
    }
    const sp = Math.round(p.species);
    this.speciesBtns.forEach((b, i) => b.classList.toggle("on", i + 1 === sp));
  }

  setSpawn(s: SpawnPattern): void {
    for (const [key, btn] of this.spawnBtns) btn.classList.toggle("on", key === s);
  }

  setStats(fps: number, agents: number, palette: string): void {
    this.statFps.textContent = String(Math.round(fps));
    this.statAgents.textContent = agents >= 1e6 ? `${(agents / 1e6).toFixed(2)}M` : `${(agents / 1e3).toFixed(0)}K`;
    this.paletteReadout.textContent = palette;
  }

  toast(msg: string): void {
    this.toastEl.textContent = msg;
    this.toastEl.classList.add("show");
    window.clearTimeout(this.toastTimer);
    this.toastTimer = window.setTimeout(() => this.toastEl.classList.remove("show"), 1900);
  }
}
