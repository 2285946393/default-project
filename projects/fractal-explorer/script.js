(function(){
  const canvas = document.getElementById('gl');
  const errBox = document.getElementById('err');
  const toast = document.getElementById('toast');

  function showError(msg){
    errBox.style.display = 'block';
    errBox.textContent = msg;
    document.getElementById('boot').classList.add('hidden');
  }
  function showToast(msg){
    if (!toast) return;
    toast.textContent = msg;
    toast.classList.add('show');
    clearTimeout(showToast._t);
    showToast._t = setTimeout(() => toast.classList.remove('show'), 1800);
  }
  window.addEventListener('error', (e) => showError('Runtime error: ' + e.message));

  const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
  if (!gl){
    showError('WebGL is not available in this browser/context, so the fractal renderer cannot run. Try a desktop browser with hardware acceleration enabled.');
    return;
  }

  // ---------------------------------------------------------------------
  // Shader compile / program helpers
  // ---------------------------------------------------------------------
  function compile(type, src){
    const s = gl.createShader(type);
    gl.shaderSource(s, src);
    gl.compileShader(s);
    if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)){
      const log = gl.getShaderInfoLog(s);
      gl.deleteShader(s);
      throw new Error('Shader compile failed: ' + log);
    }
    return s;
  }

  function makeProgram(vsSrc, fsSrc){
    const vs = compile(gl.VERTEX_SHADER, vsSrc);
    const fs = compile(gl.FRAGMENT_SHADER, fsSrc);
    const p = gl.createProgram();
    gl.attachShader(p, vs);
    gl.attachShader(p, fs);
    gl.linkProgram(p);
    if (!gl.getProgramParameter(p, gl.LINK_STATUS)){
      throw new Error('Program link failed: ' + gl.getProgramInfoLog(p));
    }
    return p;
  }

  function uniforms(program, names){
    const out = {};
    for (const n of names) out[n] = gl.getUniformLocation(program, n);
    return out;
  }

  let sceneProg, bloomHProg, bloomVProg, compositeProg;
  let uScene, uBloomH, uBloomV, uComposite;
  let quadBuf;

  try {
    const vsSrc = document.getElementById('vs').textContent;
    sceneProg = makeProgram(vsSrc, document.getElementById('fs-scene').textContent);
    bloomHProg = makeProgram(vsSrc, document.getElementById('fs-bloomh').textContent);
    bloomVProg = makeProgram(vsSrc, document.getElementById('fs-bloomv').textContent);
    compositeProg = makeProgram(vsSrc, document.getElementById('fs-composite').textContent);

    uScene = uniforms(sceneProg, ['u_resolution','u_camPos','u_camRight','u_camUp','u_camFwd','u_power','u_iter','u_hue','u_time','u_mode','u_juliaC']);
    uBloomH = uniforms(bloomHProg, ['u_tex','u_srcTexel']);
    uBloomV = uniforms(bloomVProg, ['u_tex','u_srcTexel']);
    uComposite = uniforms(compositeProg, ['u_scene','u_bloom','u_sceneTexel','u_focus','u_dofStrength','u_bloomStrength']);

    const quad = new Float32Array([-1,-1, 1,-1, -1,1, -1,1, 1,-1, 1,1]);
    quadBuf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, quadBuf);
    gl.bufferData(gl.ARRAY_BUFFER, quad, gl.STATIC_DRAW);
  } catch(e){
    showError(e.message);
    return;
  }

  function bindQuad(program){
    gl.bindBuffer(gl.ARRAY_BUFFER, quadBuf);
    const loc = gl.getAttribLocation(program, 'a_pos');
    gl.enableVertexAttribArray(loc);
    gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
  }

  // ---------------------------------------------------------------------
  // Framebuffers: scene (full internal res, rgb + depth in alpha) and a
  // pair of low-res targets for the bloom bright-pass / blur.
  // ---------------------------------------------------------------------
  function makeTarget(w, h){
    const tex = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    const fbo = gl.createFramebuffer();
    gl.bindFramebuffer(gl.FRAMEBUFFER, fbo);
    gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
    const status = gl.checkFramebufferStatus(gl.FRAMEBUFFER);
    if (status !== gl.FRAMEBUFFER_COMPLETE){
      console.warn('Framebuffer incomplete:', status);
    }
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    return { tex, fbo, w, h };
  }

  let sceneRT, bloomA, bloomB;
  function setupTargets(w, h){
    if (sceneRT) { gl.deleteTexture(sceneRT.tex); gl.deleteFramebuffer(sceneRT.fbo); }
    if (bloomA) { gl.deleteTexture(bloomA.tex); gl.deleteFramebuffer(bloomA.fbo); }
    if (bloomB) { gl.deleteTexture(bloomB.tex); gl.deleteFramebuffer(bloomB.fbo); }
    sceneRT = makeTarget(w, h);
    const bw = Math.max(1, Math.floor(w / 4));
    const bh = Math.max(1, Math.floor(h / 4));
    bloomA = makeTarget(bw, bh);
    bloomB = makeTarget(bw, bh);
  }

  // ---------------------------------------------------------------------
  // Adaptive internal render scale — doubles as supersampled anti-aliasing
  // when there's GPU headroom, and as a performance safety valve when there
  // isn't.
  // ---------------------------------------------------------------------
  let renderScale = 1.0;
  const MIN_SCALE = 0.55, MAX_SCALE = 1.6;
  let dtHistory = [];
  let lastScaleCheck = 0;

  function currentInternalSize(){
    const dpr = Math.min(window.devicePixelRatio || 1, 1.75);
    const w = Math.max(1, Math.floor(window.innerWidth * dpr * renderScale));
    const h = Math.max(1, Math.floor(window.innerHeight * dpr * renderScale));
    return { w, h };
  }

  function resize(){
    const dpr = Math.min(window.devicePixelRatio || 1, 1.75);
    const w = Math.floor(window.innerWidth * dpr);
    const h = Math.floor(window.innerHeight * dpr);
    if (canvas.width !== w || canvas.height !== h){
      canvas.width = w; canvas.height = h;
    }
    canvas.style.width = window.innerWidth + 'px';
    canvas.style.height = window.innerHeight + 'px';
    const { w: iw, h: ih } = currentInternalSize();
    setupTargets(iw, ih);
  }
  window.addEventListener('resize', resize);

  // ---------------------------------------------------------------------
  // Orbit camera
  // ---------------------------------------------------------------------
  let azimuth = 0.9, elevation = 0.35, radius = 3.1;
  let dragging = false, lastX = 0, lastY = 0;
  let autoDrift = true;

  canvas.addEventListener('pointerdown', e => {
    dragging = true; lastX = e.clientX; lastY = e.clientY;
    canvas.setPointerCapture(e.pointerId);
  });
  canvas.addEventListener('pointerup', () => dragging = false);
  canvas.addEventListener('pointercancel', () => dragging = false);
  canvas.addEventListener('pointermove', e => {
    if (!dragging) return;
    const dx = e.clientX - lastX, dy = e.clientY - lastY;
    lastX = e.clientX; lastY = e.clientY;
    azimuth -= dx * 0.006;
    elevation = Math.max(-1.4, Math.min(1.4, elevation + dy * 0.006));
  });
  canvas.addEventListener('wheel', e => {
    e.preventDefault();
    radius = Math.max(1.3, Math.min(7.0, radius + e.deltaY * 0.0022));
  }, { passive:false });

  let pinchDist = null;
  canvas.addEventListener('touchmove', e => {
    if (e.touches.length === 2){
      const dx = e.touches[0].clientX - e.touches[1].clientX;
      const dy = e.touches[0].clientY - e.touches[1].clientY;
      const d = Math.hypot(dx,dy);
      if (pinchDist !== null){
        radius = Math.max(1.3, Math.min(7.0, radius - (d - pinchDist) * 0.01));
      }
      pinchDist = d;
    }
  }, { passive:true });
  canvas.addEventListener('touchend', () => pinchDist = null);

  // ---------------------------------------------------------------------
  // UI wiring
  // ---------------------------------------------------------------------
  const powerEl = document.getElementById('power');
  const iterEl = document.getElementById('iter');
  const hueEl = document.getElementById('hue');
  const bloomEl = document.getElementById('bloom');
  const dofEl = document.getElementById('dof');
  const jxEl = document.getElementById('jx');
  const jyEl = document.getElementById('jy');
  const juliaDial = document.getElementById('juliaDial');

  const vPower = document.getElementById('vPower');
  const vIter = document.getElementById('vIter');
  const vHue = document.getElementById('vHue');
  const vBloom = document.getElementById('vBloom');
  const vDof = document.getElementById('vDof');
  const vJx = document.getElementById('vJx');
  const vJy = document.getElementById('vJy');

  const spinBtn = document.getElementById('spinBtn');
  const resetBtn = document.getElementById('resetBtn');
  const saveBtn = document.getElementById('saveBtn');
  const modeBtn = document.getElementById('modeBtn');
  const micBtn = document.getElementById('micBtn');
  const shareBtn = document.getElementById('shareBtn');

  const MODE_NAMES = ['Bulb', 'Julia', 'Box', 'Bulb2', 'Menger', 'Kifs'];

  const state = {
    power: 8.0, iter: 9, hue: 0.62,
    bloom: 0.6, dof: 0.35,
    jx: 0.35, jy: 0.05,
    mode: 0
  };

  function applyModeUI(){
    modeBtn.textContent = MODE_NAMES[state.mode];
    juliaDial.style.display = (state.mode === 1) ? 'flex' : 'none';
  }

  // ---------------------------------------------------------------------
  // Presets — named, ready-made looks (mode + power + hue + bloom + dof),
  // so someone can just pick a style instead of hand-tuning every slider.
  // ---------------------------------------------------------------------
  const presetSelect = document.getElementById('presetSelect');
  const PRESETS = {
    classicGold:  { mode: 0, power: 8.0, hue: 0.08, bloom: 0.55, dof: 0.30, iter: 9  },
    cosmicBlue:   { mode: 0, power: 6.5, hue: 0.58, bloom: 0.85, dof: 0.45, iter: 9  },
    alienVenom:   { mode: 1, power: 8.0, hue: 0.30, bloom: 0.70, dof: 0.25, iter: 8, jx: 0.35, jy: 0.05 },
    emberCore:    { mode: 3, power: 2.0, hue: 0.02, bloom: 1.10, dof: 0.55, iter: 8  },
    voidCrystal:  { mode: 4, power: 8.0, hue: 0.62, bloom: 0.40, dof: 0.20, iter: 10 },
    magmaBox:     { mode: 2, power: 10.0, hue: 0.03, bloom: 0.65, dof: 0.35, iter: 9 }
  };

  function applyPreset(name){
    const p = PRESETS[name];
    if (!p) return;
    state.mode = p.mode;
    state.power = p.power;
    state.hue = p.hue;
    state.bloom = p.bloom;
    state.dof = p.dof;
    state.iter = p.iter;
    if (p.jx !== undefined) state.jx = p.jx;
    if (p.jy !== undefined) state.jy = p.jy;

    powerEl.value = state.power; vPower.textContent = state.power.toFixed(1);
    iterEl.value = state.iter; vIter.textContent = state.iter;
    hueEl.value = state.hue; vHue.textContent = state.hue.toFixed(2);
    bloomEl.value = state.bloom; vBloom.textContent = state.bloom.toFixed(2);
    dofEl.value = state.dof; vDof.textContent = state.dof.toFixed(2);
    jxEl.value = state.jx; vJx.textContent = state.jx.toFixed(2);
    jyEl.value = state.jy; vJy.textContent = state.jy.toFixed(2);
    applyModeUI();
    showToast(presetSelect.options[presetSelect.selectedIndex].textContent);
  }

  presetSelect.addEventListener('change', () => {
    if (presetSelect.value) applyPreset(presetSelect.value);
  });

  // Any manual slider/mode tweak means we've drifted from the named
  // preset, so fall back to "Custom" in the dropdown.
  function markCustom(){ presetSelect.value = ''; }

  powerEl.addEventListener('input', () => { state.power = parseFloat(powerEl.value); vPower.textContent = state.power.toFixed(1); markCustom(); });
  iterEl.addEventListener('input', () => { state.iter = parseInt(iterEl.value,10); vIter.textContent = state.iter; markCustom(); });
  hueEl.addEventListener('input', () => { state.hue = parseFloat(hueEl.value); vHue.textContent = state.hue.toFixed(2); markCustom(); });
  bloomEl.addEventListener('input', () => { state.bloom = parseFloat(bloomEl.value); vBloom.textContent = state.bloom.toFixed(2); markCustom(); });
  dofEl.addEventListener('input', () => { state.dof = parseFloat(dofEl.value); vDof.textContent = state.dof.toFixed(2); markCustom(); });
  jxEl.addEventListener('input', () => { state.jx = parseFloat(jxEl.value); vJx.textContent = state.jx.toFixed(2); markCustom(); });
  jyEl.addEventListener('input', () => { state.jy = parseFloat(jyEl.value); vJy.textContent = state.jy.toFixed(2); markCustom(); });

  modeBtn.addEventListener('click', () => {
    state.mode = (state.mode + 1) % MODE_NAMES.length;
    applyModeUI();
    markCustom();
  });

  spinBtn.addEventListener('click', () => {
    autoDrift = !autoDrift;
    spinBtn.classList.toggle('active', autoDrift);
  });
  resetBtn.addEventListener('click', () => {
    azimuth = 0.9; elevation = 0.35; radius = 3.1;
    state.power = 8.0; state.iter = 9; state.hue = 0.62;
    state.bloom = 0.6; state.dof = 0.35;
    state.jx = 0.35; state.jy = 0.05;
    state.mode = 0;
    powerEl.value = 8; iterEl.value = 9; hueEl.value = 0.62;
    bloomEl.value = 0.6; dofEl.value = 0.35;
    jxEl.value = 0.35; jyEl.value = 0.05;
    vPower.textContent = '8.0'; vIter.textContent = '9'; vHue.textContent = '0.62';
    vBloom.textContent = '0.60'; vDof.textContent = '0.35';
    vJx.textContent = '0.35'; vJy.textContent = '0.05';
    presetSelect.value = '';
    applyModeUI();
  });

  // Save / download canvas as PNG
  function downloadCanvasPNG(){
    if (canvas.toBlob){
      canvas.toBlob(function(blob){
        if (!blob) return;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'bulb.png';
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
      }, 'image/png');
    } else {
      const data = canvas.toDataURL('image/png');
      const a = document.createElement('a');
      a.href = data;
      a.download = 'bulb.png';
      document.body.appendChild(a);
      a.click();
      a.remove();
    }
  }
  saveBtn.addEventListener('click', downloadCanvasPNG);

  window.addEventListener('keydown', (e) => {
    if (e.code === 'Space'){
      e.preventDefault();
      autoDrift = !autoDrift;
      spinBtn.classList.toggle('active', autoDrift);
    } else if (e.key === 'r' || e.key === 'R'){
      resetBtn.click();
    }
  });

  // ---------------------------------------------------------------------
  // URL preset sharing
  // ---------------------------------------------------------------------
  function encodeStateToParams(){
    const p = new URLSearchParams();
    p.set('power', state.power.toFixed(2));
    p.set('iter', state.iter);
    p.set('hue', state.hue.toFixed(2));
    p.set('bloom', state.bloom.toFixed(2));
    p.set('dof', state.dof.toFixed(2));
    p.set('mode', state.mode);
    p.set('jx', state.jx.toFixed(2));
    p.set('jy', state.jy.toFixed(2));
    p.set('az', azimuth.toFixed(3));
    p.set('el', elevation.toFixed(3));
    p.set('rad', radius.toFixed(2));
    return p;
  }

  function applyParamsToState(p){
    if (!p || [...p.keys()].length === 0) return false;
    const num = (k, d) => p.has(k) ? parseFloat(p.get(k)) : d;
    state.power = num('power', state.power);
    state.iter = Math.round(num('iter', state.iter));
    state.hue = num('hue', state.hue);
    state.bloom = num('bloom', state.bloom);
    state.dof = num('dof', state.dof);
    state.mode = Math.round(num('mode', state.mode));
    state.jx = num('jx', state.jx);
    state.jy = num('jy', state.jy);
    azimuth = num('az', azimuth);
    elevation = num('el', elevation);
    radius = num('rad', radius);

    powerEl.value = state.power; vPower.textContent = state.power.toFixed(1);
    iterEl.value = state.iter; vIter.textContent = state.iter;
    hueEl.value = state.hue; vHue.textContent = state.hue.toFixed(2);
    bloomEl.value = state.bloom; vBloom.textContent = state.bloom.toFixed(2);
    dofEl.value = state.dof; vDof.textContent = state.dof.toFixed(2);
    jxEl.value = state.jx; vJx.textContent = state.jx.toFixed(2);
    jyEl.value = state.jy; vJy.textContent = state.jy.toFixed(2);
    applyModeUI();
    return true;
  }

  // apply any preset baked into the current URL on load
  applyParamsToState(new URLSearchParams(window.location.search));
  applyModeUI();

  shareBtn.addEventListener('click', () => {
    const params = encodeStateToParams();
    const url = window.location.origin + window.location.pathname + '?' + params.toString();
    try { window.history.replaceState(null, '', '?' + params.toString()); } catch(e){}
    if (navigator.clipboard && navigator.clipboard.writeText){
      navigator.clipboard.writeText(url).then(() => showToast('Link copied — this view is saved in it')).catch(() => showToast(url));
    } else {
      showToast(url);
    }
  });

  // ---------------------------------------------------------------------
  // Audio-reactive input (mic drives power / hue / bloom subtly)
  // ---------------------------------------------------------------------
  let audioCtx = null, analyser = null, audioData = null, audioEnabled = false;
  const tAudio = document.getElementById('tAudio');

  async function enableMic(){
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      audioCtx = new (window.AudioContext || window.webkitAudioContext)();
      const src = audioCtx.createMediaStreamSource(stream);
      analyser = audioCtx.createAnalyser();
      analyser.fftSize = 256;
      analyser.smoothingTimeConstant = 0.75;
      src.connect(analyser);
      audioData = new Uint8Array(analyser.frequencyBinCount);
      audioEnabled = true;
      micBtn.classList.add('active');
      tAudio.textContent = 'live';
    } catch (e){
      audioEnabled = false;
      micBtn.classList.remove('active');
      tAudio.textContent = 'denied';
      showToast('Microphone access was denied or unavailable');
    }
  }

  function disableMic(){
    audioEnabled = false;
    micBtn.classList.remove('active');
    tAudio.textContent = 'off';
    if (audioCtx) { audioCtx.close().catch(()=>{}); audioCtx = null; }
    analyser = null;
  }

  micBtn.addEventListener('click', () => {
    if (audioEnabled) disableMic(); else enableMic();
  });

  function getAudioBands(){
    if (!audioEnabled || !analyser) return { bass: 0, mid: 0, treble: 0 };
    analyser.getByteFrequencyData(audioData);
    const n = audioData.length;
    const avg = (a, b) => {
      let s = 0; for (let i = a; i < b; i++) s += audioData[i];
      return s / ((b - a) * 255);
    };
    return {
      bass: avg(0, Math.floor(n * 0.12)),
      mid: avg(Math.floor(n * 0.12), Math.floor(n * 0.5)),
      treble: avg(Math.floor(n * 0.5), n)
    };
  }

  // ---------------------------------------------------------------------
  // Render loop
  // ---------------------------------------------------------------------
  const tRays = document.getElementById('tRays');
  const tSteps = document.getElementById('tSteps');
  const tFps = document.getElementById('tFps');
  const tScale = document.getElementById('tScale');

  let lastFpsUpdate = performance.now();
  let lastFrameTime = performance.now();

  function maybeAdaptScale(now, dt){
    dtHistory.push(dt);
    if (dtHistory.length > 40) dtHistory.shift();
    if (now - lastScaleCheck < 700) return;
    lastScaleCheck = now;
    if (dtHistory.length < 10) return;
    const avg = dtHistory.reduce((a,b)=>a+b,0) / dtHistory.length;
    let changed = false;
    if (avg > 26 && renderScale > MIN_SCALE){
      renderScale = Math.max(MIN_SCALE, renderScale - 0.15);
      changed = true;
    } else if (avg < 13 && renderScale < MAX_SCALE){
      renderScale = Math.min(MAX_SCALE, renderScale + 0.1);
      changed = true;
    }
    if (changed){
      const { w, h } = currentInternalSize();
      setupTargets(w, h);
      dtHistory = [];
    }
  }

  function render(now){
    if (autoDrift && !dragging) azimuth += 0.0016;

    const bands = getAudioBands();
    const effPower = state.power + bands.bass * 2.2;
    const effHue = (state.hue + bands.mid * 0.18) % 1.0;
    const effBloom = state.bloom + bands.treble * 0.9;

    const camPos = [
      radius * Math.cos(elevation) * Math.sin(azimuth),
      radius * Math.sin(elevation),
      radius * Math.cos(elevation) * Math.cos(azimuth)
    ];
    const target = [0,0,0];
    let fwd = [target[0]-camPos[0], target[1]-camPos[1], target[2]-camPos[2]];
    const fl = Math.hypot(fwd[0],fwd[1],fwd[2]);
    fwd = [fwd[0]/fl, fwd[1]/fl, fwd[2]/fl];
    const worldUp = [0,1,0];
    let right = [
      fwd[1]*worldUp[2]-fwd[2]*worldUp[1],
      fwd[2]*worldUp[0]-fwd[0]*worldUp[2],
      fwd[0]*worldUp[1]-fwd[1]*worldUp[0]
    ];
    const rl = Math.hypot(right[0],right[1],right[2]) || 1;
    right = [right[0]/rl, right[1]/rl, right[2]/rl];
    const up = [
      right[1]*fwd[2]-right[2]*fwd[1],
      right[2]*fwd[0]-right[0]*fwd[2],
      right[0]*fwd[1]-right[1]*fwd[0]
    ];

    // ---- PASS 1: scene -> sceneRT ----
    gl.bindFramebuffer(gl.FRAMEBUFFER, sceneRT.fbo);
    gl.viewport(0, 0, sceneRT.w, sceneRT.h);
    gl.useProgram(sceneProg);
    bindQuad(sceneProg);
    gl.uniform2f(uScene.u_resolution, sceneRT.w, sceneRT.h);
    gl.uniform3f(uScene.u_camPos, camPos[0], camPos[1], camPos[2]);
    gl.uniform3f(uScene.u_camRight, right[0], right[1], right[2]);
    gl.uniform3f(uScene.u_camUp, up[0], up[1], up[2]);
    gl.uniform3f(uScene.u_camFwd, fwd[0], fwd[1], fwd[2]);
    gl.uniform1f(uScene.u_power, effPower);
    gl.uniform1i(uScene.u_iter, state.iter);
    gl.uniform1f(uScene.u_hue, effHue);
    gl.uniform1f(uScene.u_time, now * 0.001);
    gl.uniform1i(uScene.u_mode, state.mode);
    gl.uniform3f(uScene.u_juliaC, state.jx, state.jy, 0.0);
    gl.drawArrays(gl.TRIANGLES, 0, 6);

    // ---- PASS 2: bright-pass + horizontal blur -> bloomA ----
    gl.bindFramebuffer(gl.FRAMEBUFFER, bloomA.fbo);
    gl.viewport(0, 0, bloomA.w, bloomA.h);
    gl.useProgram(bloomHProg);
    bindQuad(bloomHProg);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, sceneRT.tex);
    gl.uniform1i(uBloomH.u_tex, 0);
    gl.uniform2f(uBloomH.u_srcTexel, 1/sceneRT.w, 1/sceneRT.h);
    gl.drawArrays(gl.TRIANGLES, 0, 6);

    // ---- PASS 3: vertical blur -> bloomB ----
    gl.bindFramebuffer(gl.FRAMEBUFFER, bloomB.fbo);
    gl.viewport(0, 0, bloomB.w, bloomB.h);
    gl.useProgram(bloomVProg);
    bindQuad(bloomVProg);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, bloomA.tex);
    gl.uniform1i(uBloomV.u_tex, 0);
    gl.uniform2f(uBloomV.u_srcTexel, 1/bloomA.w, 1/bloomA.h);
    gl.drawArrays(gl.TRIANGLES, 0, 6);

    // ---- PASS 4: composite -> canvas ----
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    gl.viewport(0, 0, canvas.width, canvas.height);
    gl.useProgram(compositeProg);
    bindQuad(compositeProg);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, sceneRT.tex);
    gl.uniform1i(uComposite.u_scene, 0);
    gl.activeTexture(gl.TEXTURE1);
    gl.bindTexture(gl.TEXTURE_2D, bloomB.tex);
    gl.uniform1i(uComposite.u_bloom, 1);
    gl.uniform2f(uComposite.u_sceneTexel, 1/sceneRT.w, 1/sceneRT.h);
    gl.uniform1f(uComposite.u_focus, Math.min(radius / 16.0, 1.0));
    gl.uniform1f(uComposite.u_dofStrength, state.dof);
    gl.uniform1f(uComposite.u_bloomStrength, effBloom);
    gl.drawArrays(gl.TRIANGLES, 0, 6);

    const dt = now - lastFrameTime;
    lastFrameTime = now;
    maybeAdaptScale(now, dt);

    if (now - lastFpsUpdate > 400){
      tFps.textContent = dt.toFixed(1);
      tRays.textContent = (sceneRT.w * sceneRT.h).toLocaleString();
      tSteps.textContent = Math.round(20 + state.iter * 6);
      tScale.textContent = renderScale.toFixed(2);
      lastFpsUpdate = now;
    }

    requestAnimationFrame(render);
  }

  requestAnimationFrame((t) => {
    resize();
    document.getElementById('boot').classList.add('hidden');
    render(t);
  });
})();
