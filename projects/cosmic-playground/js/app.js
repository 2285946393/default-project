// ============================================
// Cosmic Playground - 粒子宇宙引擎
// ============================================

const THEMES = {
  cosmic: ['#667eea', '#764ba2', '#f093fb', '#a18cd1', '#fbc2eb'],
  fire:   ['#f12711', '#f5af19', '#ff6b35', '#ffd166', '#ff8c42'],
  ocean:  ['#00c6ff', '#0072ff', '#48c6ef', '#6f86d6', '#a1c4fd'],
  nature: ['#11998e', '#38ef7d', '#56ab2f', '#a8e063', '#c6ffdd'],
  neon:   ['#ff00ff', '#00ffff', '#ff006e', '#8338ec', '#3a86ff'],
};

const MODE_NAMES = {
  galaxy: '星系', aurora: '极光', nebula: '星云',
  matrix: '矩阵', fireworks: '烟花', starfield: '星场',
};

// ---- 工具函数 ----
const rand = (min, max) => Math.random() * (max - min) + min;
const lerp = (a, b, t) => a + (b - a) * t;
const dist = (x1, y1, x2, y2) => Math.hypot(x2 - x1, y2 - y1);
const hsl = (h, s, l, a = 1) => `hsla(${h},${s}%,${l}%,${a})`;

// ---- 粒子类 ----
class Particle {
  constructor(x, y, config) {
    this.x = x;
    this.y = y;
    this.ox = x;
    this.oy = y;
    this.vx = rand(-1, 1) * (config.speed || 1);
    this.vy = rand(-1, 1) * (config.speed || 1);
    this.size = rand(1, config.maxSize || 3);
    this.life = 1;
    this.maxLife = rand(100, 600);
    this.age = 0;
    this.hue = rand(0, 360);
    this.color = config.colors[Math.floor(rand(0, config.colors.length))];
    this.angle = rand(0, Math.PI * 2);
    this.orbitRadius = rand(50, 300);
    this.orbitSpeed = rand(0.001, 0.005) * (Math.random() > 0.5 ? 1 : -1);
    this.char = String.fromCharCode(0x30A0 + Math.floor(rand(0, 96)));
    this.trail = [];
    this.maxTrail = Math.floor(rand(5, 15));
  }

  reset(x, y, config) {
    this.x = x; this.y = y;
    this.ox = x; this.oy = y;
    this.vx = rand(-1, 1) * (config.speed || 1);
    this.vy = rand(-1, 1) * (config.speed || 1);
    this.life = 1; this.age = 0;
    this.color = config.colors[Math.floor(rand(0, config.colors.length))];
    this.trail = [];
  }
}

// ---- 主应用 ----
class CosmicApp {
  constructor() {
    this.canvas = document.getElementById('canvas');
    this.ctx = this.canvas.getContext('2d');
    this.particles = [];
    this.mouse = { x: -9999, y: -9999, down: false, right: false };
    this.config = {
      count: 800, maxSize: 3, speed: 1, gravity: 1,
      mode: 'galaxy', theme: 'cosmic',
      colors: THEMES.cosmic,
    };
    this.paused = false;
    this.zoom = 1;
    this.time = 0;
    this.fps = 60;
    this.frameCount = 0;
    this.lastFpsTime = performance.now();
    this.matrixColumns = [];

    this.resize();
    this.initParticles();
    this.bindEvents();
    this.bindUI();
    this.animate();
  }

  resize() {
    this.w = this.canvas.width = window.innerWidth;
    this.h = this.canvas.height = window.innerHeight;
    this.cx = this.w / 2;
    this.cy = this.h / 2;
    this.initMatrixColumns();
  }

  initMatrixColumns() {
    const cols = Math.floor(this.w / 18);
    this.matrixColumns = Array.from({ length: cols }, () => ({
      y: rand(-this.h, 0), speed: rand(2, 8), chars: [],
    }));
  }

  initParticles() {
    this.particles = [];
    for (let i = 0; i < this.config.count; i++) {
      this.particles.push(new Particle(
        rand(0, this.w), rand(0, this.h), this.config
      ));
    }
  }

  adjustParticleCount(target) {
    while (this.particles.length < target) {
      this.particles.push(new Particle(
        rand(0, this.w), rand(0, this.h), this.config
      ));
    }
    if (this.particles.length > target) {
      this.particles.length = target;
    }
  }

  // ---- 事件绑定 ----
  bindEvents() {
    window.addEventListener('resize', () => this.resize());

    this.canvas.addEventListener('mousemove', e => {
      this.mouse.x = e.clientX;
      this.mouse.y = e.clientY;
    });

    this.canvas.addEventListener('mousedown', e => {
      if (e.button === 0) { this.mouse.down = true; this.explode(e.clientX, e.clientY); }
      if (e.button === 2) this.mouse.right = true;
    });

    this.canvas.addEventListener('mouseup', e => {
      if (e.button === 0) this.mouse.down = false;
      if (e.button === 2) this.mouse.right = false;
    });

    this.canvas.addEventListener('contextmenu', e => e.preventDefault());

    this.canvas.addEventListener('wheel', e => {
      this.zoom = Math.max(0.3, Math.min(3, this.zoom - e.deltaY * 0.001));
    }, { passive: true });

    // 触摸支持
    this.canvas.addEventListener('touchmove', e => {
      e.preventDefault();
      this.mouse.x = e.touches[0].clientX;
      this.mouse.y = e.touches[0].clientY;
    }, { passive: false });

    this.canvas.addEventListener('touchstart', e => {
      this.mouse.down = true;
      this.mouse.x = e.touches[0].clientX;
      this.mouse.y = e.touches[0].clientY;
      this.explode(this.mouse.x, this.mouse.y);
    });

    this.canvas.addEventListener('touchend', () => { this.mouse.down = false; });

    document.addEventListener('keydown', e => {
      if (e.code === 'Space') { e.preventDefault(); this.paused = !this.paused; }
    });
  }

  explode(x, y) {
    const count = 30;
    for (let i = 0; i < count; i++) {
      const angle = (Math.PI * 2 / count) * i;
      const speed = rand(3, 10);
      const p = new Particle(x, y, this.config);
      p.vx = Math.cos(angle) * speed;
      p.vy = Math.sin(angle) * speed;
      p.maxLife = rand(30, 80);
      p.size = rand(1, 4);
      this.particles.push(p);
    }
    // 保持粒子数量不超过上限太多
    if (this.particles.length > this.config.count * 1.5) {
      this.particles.splice(0, this.particles.length - this.config.count);
    }
  }

  // ---- UI 绑定 ----
  bindUI() {
    const $ = s => document.querySelector(s);
    const $$ = s => document.querySelectorAll(s);

    // 面板折叠
    $('#panel-toggle').addEventListener('click', () => {
      $('#panel').classList.toggle('collapsed');
    });

    // 模式切换
    $$('.mode-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        $$('.mode-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        this.config.mode = btn.dataset.mode;
        $('#mode-label').textContent = `模式: ${MODE_NAMES[this.config.mode]}`;
        if (this.config.mode === 'matrix') this.initMatrixColumns();
      });
    });

    // 滑块
    $('#particle-slider').addEventListener('input', e => {
      this.config.count = +e.target.value;
      $('#count-val').textContent = e.target.value;
      this.adjustParticleCount(this.config.count);
    });
    $('#size-slider').addEventListener('input', e => {
      this.config.maxSize = +e.target.value;
      $('#size-val').textContent = e.target.value;
    });
    $('#speed-slider').addEventListener('input', e => {
      this.config.speed = +e.target.value;
      $('#speed-val').textContent = e.target.value;
    });
    $('#gravity-slider').addEventListener('input', e => {
      this.config.gravity = +e.target.value;
      $('#gravity-val').textContent = e.target.value;
    });

    // 配色
    $$('.theme-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        $$('.theme-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        this.config.theme = btn.dataset.theme;
        this.config.colors = THEMES[btn.dataset.theme];
        this.particles.forEach(p => {
          p.color = this.config.colors[Math.floor(rand(0, this.config.colors.length))];
        });
      });
    });

    // 操作按钮
    $('#btn-fullscreen').addEventListener('click', () => {
      if (!document.fullscreenElement) document.documentElement.requestFullscreen();
      else document.exitFullscreen();
    });
    $('#btn-screenshot').addEventListener('click', () => this.screenshot());
    $('#btn-reset').addEventListener('click', () => {
      this.zoom = 1; this.time = 0;
      this.initParticles();
    });
  }

  screenshot() {
    const flash = document.getElementById('flash');
    flash.classList.add('active');
    setTimeout(() => flash.classList.remove('active'), 150);
    const link = document.createElement('a');
    link.download = `cosmic-${Date.now()}.png`;
    link.href = this.canvas.toDataURL();
    link.click();
  }

  // ---- 鼠标引力 ----
  applyMouseForce(p) {
    const dx = this.mouse.x - p.x;
    const dy = this.mouse.y - p.y;
    const d = Math.max(dist(p.x, p.y, this.mouse.x, this.mouse.y), 1);
    const force = this.config.gravity * 80 / (d * d + 200);
    const dir = this.mouse.right ? -1 : 1;
    p.vx += dx * force * dir * 0.5;
    p.vy += dy * force * dir * 0.5;
  }

  // ============ 各模式更新逻辑 ============

  updateGalaxy(p) {
    p.angle += p.orbitSpeed * this.config.speed;
    const targetX = this.cx + Math.cos(p.angle) * p.orbitRadius * this.zoom;
    const targetY = this.cy + Math.sin(p.angle) * p.orbitRadius * 0.4 * this.zoom;
    p.vx += (targetX - p.x) * 0.01;
    p.vy += (targetY - p.y) * 0.01;
    p.vx *= 0.96; p.vy *= 0.96;
    this.applyMouseForce(p);
    p.x += p.vx; p.y += p.vy;
  }

  updateAurora(p) {
    const wave = Math.sin(this.time * 0.01 + p.ox * 0.005) * 100;
    const targetY = this.cy * 0.6 + wave;
    p.vy += (targetY - p.y) * 0.003;
    p.vx += Math.sin(this.time * 0.005 + p.y * 0.01) * 0.3 * this.config.speed;
    p.vx *= 0.98; p.vy *= 0.97;
    this.applyMouseForce(p);
    p.x += p.vx; p.y += p.vy;
    if (p.x < 0) p.x = this.w;
    if (p.x > this.w) p.x = 0;
  }

  updateNebula(p) {
    const noiseX = Math.sin(p.x * 0.003 + this.time * 0.008) * Math.cos(p.y * 0.004);
    const noiseY = Math.cos(p.y * 0.003 + this.time * 0.008) * Math.sin(p.x * 0.004);
    p.vx += noiseX * 0.15 * this.config.speed;
    p.vy += noiseY * 0.15 * this.config.speed;
    p.vx *= 0.97; p.vy *= 0.97;
    this.applyMouseForce(p);
    p.x += p.vx; p.y += p.vy;
    // 边界环绕
    if (p.x < -50) p.x = this.w + 50;
    if (p.x > this.w + 50) p.x = -50;
    if (p.y < -50) p.y = this.h + 50;
    if (p.y > this.h + 50) p.y = -50;
  }

  updateFireworks(p) {
    p.vy += 0.04; // 重力
    p.vx *= 0.99; p.vy *= 0.99;
    this.applyMouseForce(p);
    p.x += p.vx * this.config.speed;
    p.y += p.vy * this.config.speed;
    p.age++;
    p.life = Math.max(0, 1 - p.age / p.maxLife);
    // 记录轨迹
    p.trail.push({ x: p.x, y: p.y });
    if (p.trail.length > p.maxTrail) p.trail.shift();
    if (p.life <= 0) p.reset(rand(0, this.w), rand(0, this.h), this.config);
  }

  updateStarfield(p) {
    const dx = p.x - this.cx;
    const dy = p.y - this.cy;
    const speed = this.config.speed * 2 * this.zoom;
    p.x += dx * 0.008 * speed;
    p.y += dy * 0.008 * speed;
    p.size = Math.min(5, p.size + 0.01 * speed);
    this.applyMouseForce(p);
    p.x += p.vx * 0.3; p.y += p.vy * 0.3;
    p.vx *= 0.95; p.vy *= 0.95;
    if (p.x < -10 || p.x > this.w + 10 || p.y < -10 || p.y > this.h + 10) {
      p.x = this.cx + rand(-50, 50);
      p.y = this.cy + rand(-50, 50);
      p.size = rand(0.5, 1.5);
    }
  }

  // ============ 渲染逻辑 ============

  renderParticle(p, ctx) {
    const mode = this.config.mode;
    ctx.globalAlpha = Math.max(0, p.life);

    if (mode === 'fireworks' && p.trail.length > 1) {
      ctx.beginPath();
      ctx.moveTo(p.trail[0].x, p.trail[0].y);
      for (let i = 1; i < p.trail.length; i++) {
        ctx.lineTo(p.trail[i].x, p.trail[i].y);
      }
      ctx.strokeStyle = p.color;
      ctx.lineWidth = p.size * 0.5;
      ctx.stroke();
    }

    if (mode === 'aurora') {
      const grad = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.size * 8);
      grad.addColorStop(0, p.color);
      grad.addColorStop(1, 'transparent');
      ctx.fillStyle = grad;
      ctx.fillRect(p.x - p.size * 8, p.y - p.size * 8, p.size * 16, p.size * 16);
      return;
    }

    if (mode === 'nebula') {
      const grad = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.size * 5);
      grad.addColorStop(0, p.color + 'aa');
      grad.addColorStop(0.5, p.color + '44');
      grad.addColorStop(1, 'transparent');
      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.size * 5, 0, Math.PI * 2);
      ctx.fill();
      return;
    }

    // 默认圆形粒子 (galaxy, starfield, fireworks)
    ctx.fillStyle = p.color;
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
    ctx.fill();

    // 星系模式加发光
    if (mode === 'galaxy' || mode === 'starfield') {
      ctx.globalAlpha = 0.15;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.size * 3, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.globalAlpha = 1;
  }

  renderMatrix(ctx) {
    ctx.fillStyle = 'rgba(0, 0, 0, 0.05)';
    ctx.fillRect(0, 0, this.w, this.h);
    const colors = this.config.colors;
    ctx.font = '16px monospace';

    for (const col of this.matrixColumns) {
      col.y += col.speed * this.config.speed;
      const x = this.matrixColumns.indexOf(col) * 18;
      const char = String.fromCharCode(0x30A0 + Math.floor(rand(0, 96)));

      // 头部亮色
      ctx.fillStyle = '#fff';
      ctx.fillText(char, x, col.y);

      // 尾部渐隐
      for (let i = 1; i < 20; i++) {
        const alpha = 1 - i / 20;
        ctx.fillStyle = colors[i % colors.length] + Math.floor(alpha * 255).toString(16).padStart(2, '0');
        const trailChar = String.fromCharCode(0x30A0 + Math.floor(rand(0, 96)));
        ctx.fillText(trailChar, x, col.y - i * 18);
      }

      if (col.y > this.h + 200) {
        col.y = rand(-200, -50);
        col.speed = rand(2, 8);
      }
    }

    // 鼠标引力扭曲效果
    if (this.mouse.x > 0) {
      const grad = ctx.createRadialGradient(
        this.mouse.x, this.mouse.y, 0,
        this.mouse.x, this.mouse.y, 120
      );
      grad.addColorStop(0, colors[0] + '30');
      grad.addColorStop(1, 'transparent');
      ctx.fillStyle = grad;
      ctx.fillRect(this.mouse.x - 120, this.mouse.y - 120, 240, 240);
    }
  }

  // ---- 连线效果 (galaxy/nebula) ----
  renderConnections(ctx) {
    const maxDist = 80 * this.zoom;
    const len = Math.min(this.particles.length, 120);
    ctx.lineWidth = 0.5;
    for (let i = 0; i < len; i++) {
      const a = this.particles[i];
      for (let j = i + 1; j < len; j++) {
        const b = this.particles[j];
        const dx = a.x - b.x, dy = a.y - b.y;
        const d = Math.sqrt(dx*dx + dy*dy);
        if (d < maxDist) {
          const alpha = (1 - d / maxDist) * 0.15;
          ctx.globalAlpha = alpha;
          ctx.strokeStyle = a.color;
          ctx.beginPath();
          ctx.moveTo(a.x, a.y);
          ctx.lineTo(b.x, b.y);
          ctx.stroke();
        }
      }
    }
    ctx.globalAlpha = 1;
  }

  // ---- 鼠标光标效果 ----
  renderCursor(ctx) {
    if (this.mouse.x < 0) return;
    const radius = this.mouse.right ? 80 : 50;
    const color = this.mouse.right ? '#ff4444' : this.config.colors[0];
    const grad = ctx.createRadialGradient(
      this.mouse.x, this.mouse.y, 0,
      this.mouse.x, this.mouse.y, radius
    );
    grad.addColorStop(0, color + '25');
    grad.addColorStop(0.5, color + '10');
    grad.addColorStop(1, 'transparent');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.arc(this.mouse.x, this.mouse.y, radius, 0, Math.PI * 2);
    ctx.fill();
  }

  // ============ 主循环 ============

  animate() {
    requestAnimationFrame(() => this.animate());
    if (this.paused) return;

    this.time++;
    this.frameCount++;
    const now = performance.now();
    if (now - this.lastFpsTime >= 1000) {
      this.fps = this.frameCount;
      this.frameCount = 0;
      this.lastFpsTime = now;
      document.getElementById('fps').textContent = `FPS: ${this.fps}`;
      document.getElementById('particle-count').textContent = `粒子: ${this.particles.length}`;
    }

    const ctx = this.ctx;
    const mode = this.config.mode;

    // 矩阵模式有自己的背景处理
    if (mode === 'matrix') {
      this.renderMatrix(ctx);
      return;
    }

    // 清屏 (带拖尾效果)
    if (mode === 'fireworks') {
      ctx.fillStyle = 'rgba(0, 0, 0, 0.12)';
      ctx.fillRect(0, 0, this.w, this.h);
    } else {
      ctx.fillStyle = 'rgba(0, 0, 0, 0.15)';
      ctx.fillRect(0, 0, this.w, this.h);
    }

    // 更新和渲染粒子
    const updater = {
      galaxy: p => this.updateGalaxy(p),
      aurora: p => this.updateAurora(p),
      nebula: p => this.updateNebula(p),
      fireworks: p => this.updateFireworks(p),
      starfield: p => this.updateStarfield(p),
    }[mode];

    for (const p of this.particles) {
      if (updater) updater(p);
      this.renderParticle(p, ctx);
    }

    // 连线
    if (mode === 'galaxy' || mode === 'nebula') {
      this.renderConnections(ctx);
    }

    // 鼠标光标
    this.renderCursor(ctx);
  }
}

// ---- 启动 ----
window.addEventListener('DOMContentLoaded', () => new CosmicApp());
