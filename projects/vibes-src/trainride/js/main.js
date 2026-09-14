// ==================== 全局变量 ====================
let scene, camera, renderer;
let tracks = [];
let sceneryObjects = [];
let clock = new THREE.Clock();

// 轨道参数
const TRACK_SEGMENT_LENGTH = 25;
const MAX_TRACKS = 25;
let currentTrackIndex = 0;
let trainPosition = new THREE.Vector3(0, 0.5, 0);
let trainDirection = new THREE.Vector3(0, 0, 1);
let currentTrackT = 0;
let lastSceneryUpdatePos = new THREE.Vector3();
const SCENERY_UPDATE_DIST = 60;

// 火车控制
let trainSpeed = 12;
let targetSpeed = 12;
const MIN_SPEED = 0;
const MAX_SPEED = 40;

// 时间系统
let timeOfDay = 0.32; // 0=午夜 0.25=日出 0.5=正午 0.75=日落
let timeFlowSpeed = 0.008; // 时间流逝速度
let paused = false;

// 光照
let sunLight, moonLight, ambientLight;
let sunMesh, moonMesh;
let stars;
let skyMesh;

// 环境
let groundMesh;
let clouds = [];
let birds = [];
let lakes = [];

// UI 状态
let started = false;

// ==================== 颜色工具 ====================
function lerpColor(c1, c2, t) {
  return new THREE.Color(c1).lerp(new THREE.Color(c2), t);
}

// 根据时间获取天空颜色（顶部、中部、地平线）
function getSkyColors(t) {
  // t: 0=午夜 0.25=日出 0.5=正午 0.75=日落
  const phases = [
    { t: 0.0,  top: 0x0a0a2a, mid: 0x1a1a3a, horizon: 0x2a2a4a }, // 深夜
    { t: 0.2,  top: 0x1a2a4a, mid: 0x4a3a5a, horizon: 0x8a5a4a }, // 黎明前
    { t: 0.27, top: 0x4a6a9a, mid: 0xc88a6a, horizon: 0xf0b080 }, // 日出
    { t: 0.35, top: 0x6a9ad8, mid: 0x8ab8e8, horizon: 0xb8d8f0 }, // 上午
    { t: 0.5,  top: 0x4a8ad8, mid: 0x6aa8e8, horizon: 0x98c8f0 }, // 正午
    { t: 0.65, top: 0x5a8ac8, mid: 0x8ab0d8, horizon: 0xb0c8e0 }, // 下午
    { t: 0.73, top: 0x6a5a8a, mid: 0xc87a5a, horizon: 0xf09060 }, // 日落
    { t: 0.8,  top: 0x2a2a5a, mid: 0x4a3a6a, horizon: 0x6a4a5a }, // 黄昏后
    { t: 0.9,  top: 0x0f0f2f, mid: 0x1f1f3f, horizon: 0x2f2f4f }, // 夜晚
    { t: 1.0,  top: 0x0a0a2a, mid: 0x1a1a3a, horizon: 0x2a2a4a }, // 回到深夜
  ];

  let i = 0;
  while (i < phases.length - 1 && t > phases[i + 1].t) i++;
  const p1 = phases[i];
  const p2 = phases[Math.min(i + 1, phases.length - 1)];
  const localT = (t - p1.t) / Math.max(0.001, p2.t - p1.t);

  return {
    top: lerpColor(p1.top, p2.top, localT),
    mid: lerpColor(p1.mid, p2.mid, localT),
    horizon: lerpColor(p1.horizon, p2.horizon, localT)
  };
}

// 获取太阳位置（角度）
function getSunPosition(t) {
  // 日出 t=0.25 时太阳从地平线升起，正午 t=0.5 在头顶，日落 t=0.75 落下
  const angle = (t - 0.25) * Math.PI * 2; // 0=日出时角度0
  const elevation = Math.sin(angle); // -1到1
  const azimuth = Math.cos(angle);
  return { elevation, azimuth, angle };
}

// 获取是否是白天
function isDaytime(t) {
  return t > 0.22 && t < 0.78;
}

// 获取星星可见度
function getStarOpacity(t) {
  if (t < 0.2 || t > 0.82) return 1;
  if (t > 0.25 && t < 0.75) return 0;
  if (t <= 0.25) return 1 - (t - 0.2) / 0.05;
  return (t - 0.75) / 0.07;
}

// ==================== 初始化 ====================
function init() {
  scene = new THREE.Scene();

  // 相机
  camera = new THREE.PerspectiveCamera(70, window.innerWidth / window.innerHeight, 0.1, 800);
  camera.position.set(0, 2.5, -3);

  // 渲染器
  renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.0;
  document.body.appendChild(renderer.domElement);

  // 光照
  ambientLight = new THREE.AmbientLight(0xffffff, 0.3);
  scene.add(ambientLight);

  sunLight = new THREE.DirectionalLight(0xfff5e0, 1.0);
  sunLight.castShadow = true;
  sunLight.shadow.mapSize.width = 2048;
  sunLight.shadow.mapSize.height = 2048;
  sunLight.shadow.camera.near = 0.5;
  sunLight.shadow.camera.far = 300;
  sunLight.shadow.camera.left = -80;
  sunLight.shadow.camera.right = 80;
  sunLight.shadow.camera.top = 80;
  sunLight.shadow.camera.bottom = -80;
  scene.add(sunLight);

  moonLight = new THREE.DirectionalLight(0x8090ff, 0.2);
  scene.add(moonLight);

  // 天空
  createSky();

  // 太阳月亮
  createCelestialBodies();

  // 星星
  createStars();

  // 地形
  createGround();

  // 初始轨道
  for (let i = 0; i < MAX_TRACKS; i++) {
    addTrackSegment();
  }

  // 初始环境
  addScenery();

  // 事件
  window.addEventListener('resize', onResize);
  window.addEventListener('keydown', onKeyDown);

  // 隐藏加载
  document.getElementById('loading').style.display = 'none';

  animate();
}

// ==================== 天空 ====================
function createSky() {
  const skyGeo = new THREE.SphereGeometry(500, 32, 16);
  const skyMat = new THREE.ShaderMaterial({
    uniforms: {
      topColor: { value: new THREE.Color(0x4a8ad8) },
      midColor: { value: new THREE.Color(0x6aa8e8) },
      bottomColor: { value: new THREE.Color(0x98c8f0) }
    },
    vertexShader: `
      varying vec3 vWorldPosition;
      void main() {
        vec4 worldPosition = modelMatrix * vec4(position, 1.0);
        vWorldPosition = worldPosition.xyz;
        gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
      }
    `,
    fragmentShader: `
      uniform vec3 topColor;
      uniform vec3 midColor;
      uniform vec3 bottomColor;
      varying vec3 vWorldPosition;
      void main() {
        float h = normalize(vWorldPosition).y;
        vec3 color;
        if (h > 0.0) {
          color = mix(midColor, topColor, pow(h, 0.6));
        } else {
          color = mix(midColor, bottomColor, pow(-h, 0.5));
        }
        gl_FragColor = vec4(color, 1.0);
      }
    `,
    side: THREE.BackSide
  });
  skyMesh = new THREE.Mesh(skyGeo, skyMat);
  scene.add(skyMesh);
}

function updateSky() {
  const colors = getSkyColors(timeOfDay);
  skyMesh.material.uniforms.topColor.value.copy(colors.top);
  skyMesh.material.uniforms.midColor.value.copy(colors.mid);
  skyMesh.material.uniforms.bottomColor.value.copy(colors.horizon);

  // 天空跟随火车
  skyMesh.position.copy(trainPosition);

  // 雾
  const fogColor = colors.horizon.clone().lerp(colors.mid, 0.3);
  scene.fog = new THREE.Fog(fogColor, 60, 350);
}

// ==================== 天体 ====================
function createCelestialBodies() {
  // 太阳
  const sunGeo = new THREE.SphereGeometry(8, 16, 16);
  const sunMat = new THREE.MeshBasicMaterial({ color: 0xfff0c0 });
  sunMesh = new THREE.Mesh(sunGeo, sunMat);
  scene.add(sunMesh);

  // 太阳光晕
  const glowGeo = new THREE.SphereGeometry(14, 16, 16);
  const glowMat = new THREE.MeshBasicMaterial({
    color: 0xffdd88,
    transparent: true,
    opacity: 0.3,
    side: THREE.BackSide
  });
  const glow = new THREE.Mesh(glowGeo, glowMat);
  sunMesh.add(glow);

  // 月亮
  const moonGeo = new THREE.SphereGeometry(5, 16, 16);
  const moonMat = new THREE.MeshBasicMaterial({ color: 0xe8e8f0 });
  moonMesh = new THREE.Mesh(moonGeo, moonMat);
  scene.add(moonMesh);
}

function updateCelestialBodies() {
  const sun = getSunPosition(timeOfDay);
  const dist = 300;

  // 太阳位置
  sunMesh.position.set(
    trainPosition.x + Math.cos(sun.angle) * dist,
    trainPosition.y + sun.elevation * dist * 0.8 + 20,
    trainPosition.z + Math.sin(sun.angle) * dist
  );

  // 太阳光照
  const dayFactor = Math.max(0, Math.min(1, (sun.elevation + 0.2) * 3));
  sunLight.intensity = dayFactor * 1.2;
  sunLight.position.copy(sunMesh.position).sub(trainPosition).normalize().multiplyScalar(100).add(trainPosition);
  sunLight.position.y = 50 + sun.elevation * 50;

  // 太阳颜色（日出日落偏红）
  const sunColor = lerpColor(0xff8855, 0xfff5e0, dayFactor);
  sunLight.color.copy(sunColor);
  sunMesh.material.color.copy(sunColor).lerp(new THREE.Color(0xffffff), 0.3);

  // 月亮位置（和太阳相对）
  const moonAngle = sun.angle + Math.PI;
  moonMesh.position.set(
    trainPosition.x + Math.cos(moonAngle) * dist,
    trainPosition.y + Math.sin(moonAngle) * dist * 0.5 + 30,
    trainPosition.z + Math.sin(moonAngle) * dist
  );
  moonLight.position.copy(moonMesh.position).sub(trainPosition).normalize().multiplyScalar(80).add(trainPosition);
  moonLight.intensity = (1 - dayFactor) * 0.25;

  // 环境光
  ambientLight.intensity = 0.15 + dayFactor * 0.25;

  // 渲染器曝光
  renderer.toneMappingExposure = 0.7 + dayFactor * 0.5;
}

// ==================== 星星 ====================
function createStars() {
  const starCount = 1500;
  const positions = new Float32Array(starCount * 3);
  const sizes = new Float32Array(starCount);

  for (let i = 0; i < starCount; i++) {
    const theta = Math.random() * Math.PI * 2;
    const phi = Math.acos(2 * Math.random() - 1);
    const r = 400;
    positions[i * 3] = r * Math.sin(phi) * Math.cos(theta);
    positions[i * 3 + 1] = Math.abs(r * Math.cos(phi)) * 0.7 + 50;
    positions[i * 3 + 2] = r * Math.sin(phi) * Math.sin(theta);
    sizes[i] = Math.random() * 2 + 0.5;
  }

  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  geo.setAttribute('size', new THREE.BufferAttribute(sizes, 1));

  const mat = new THREE.PointsMaterial({
    color: 0xffffff,
    size: 1.5,
    transparent: true,
    opacity: 0,
    sizeAttenuation: true
  });

  stars = new THREE.Points(geo, mat);
  scene.add(stars);
}

function updateStars() {
  stars.position.copy(trainPosition);
  stars.material.opacity = getStarOpacity(timeOfDay) * 0.9;
}

// ==================== 地形 ====================
function createGround() {
  const groundGeo = new THREE.PlaneGeometry(600, 600, 80, 80);
  const pos = groundGeo.attributes.position;

  // 平缓起伏地形，轨道附近保持平坦
  for (let i = 0; i < pos.count; i++) {
    const x = pos.getX(i);
    const y = pos.getY(i);
    // 距离中心越远起伏越大，轨道附近(x小)保持平坦
    const distFromCenter = Math.abs(x);
    const flattenFactor = Math.min(1, Math.max(0, (distFromCenter - 8) / 30));
    const h = (Math.sin(x * 0.015) * Math.cos(y * 0.012) * 0.8
            + Math.sin(x * 0.04 + y * 0.025) * 0.3
            + Math.cos(x * 0.008 - y * 0.01) * 0.6) * flattenFactor;
    pos.setZ(i, h - 0.15);
  }
  groundGeo.computeVertexNormals();

  const groundMat = new THREE.MeshStandardMaterial({
    color: 0x6a9a4a,
    roughness: 0.95,
    metalness: 0.0,
    flatShading: false
  });

  groundMesh = new THREE.Mesh(groundGeo, groundMat);
  groundMesh.rotation.x = -Math.PI / 2;
  groundMesh.receiveShadow = true;
  scene.add(groundMesh);
}

function updateGround() {
  groundMesh.position.x = trainPosition.x;
  groundMesh.position.z = trainPosition.z;
}

// ==================== 轨道 ====================
function createTrackSegment(startPoint, endPoint) {
  const group = new THREE.Group();
  const path = new THREE.LineCurve3(startPoint, endPoint);

  // 钢轨
  const railGeo = new THREE.TubeGeometry(path, 16, 0.08, 6, false);
  const railMat = new THREE.MeshStandardMaterial({ color: 0x999999, roughness: 0.3, metalness: 0.7 });

  const leftRail = new THREE.Mesh(railGeo, railMat);
  leftRail.position.x = 0.7;
  leftRail.position.y = 0.15;
  leftRail.receiveShadow = true;
  group.add(leftRail);

  const rightRail = new THREE.Mesh(railGeo, railMat);
  rightRail.position.x = -0.7;
  rightRail.position.y = 0.15;
  rightRail.receiveShadow = true;
  group.add(rightRail);

  // 枕木
  const sleeperGeo = new THREE.BoxGeometry(1.8, 0.12, 0.35);
  const sleeperMat = new THREE.MeshStandardMaterial({ color: 0x4a3520, roughness: 0.9 });
  const dist = startPoint.distanceTo(endPoint);
  const numSleepers = Math.floor(dist / 1.8);

  const dir = new THREE.Vector3().subVectors(endPoint, startPoint).normalize();
  const angle = Math.atan2(dir.x, dir.z);

  for (let i = 0; i < numSleepers; i++) {
    const t = (i + 0.5) / numSleepers;
    const pos = new THREE.Vector3().lerpVectors(startPoint, endPoint, t);
    const sleeper = new THREE.Mesh(sleeperGeo, sleeperMat);
    sleeper.position.copy(pos);
    sleeper.position.y = 0.05;
    sleeper.rotation.y = angle;
    sleeper.receiveShadow = true;
    group.add(sleeper);
  }

  // 道砟（碎石）
  const ballastGeo = new THREE.BoxGeometry(2.2, 0.15, dist);
  const ballastMat = new THREE.MeshStandardMaterial({ color: 0x7a7060, roughness: 1.0 });
  const ballast = new THREE.Mesh(ballastGeo, ballastMat);
  ballast.position.copy(startPoint).lerp(endPoint, 0.5);
  ballast.position.y = -0.05;
  ballast.rotation.y = angle;
  ballast.receiveShadow = true;
  group.add(ballast);

  scene.add(group);
  return group;
}

function addTrackSegment() {
  let startPoint, endPoint, direction;

  if (tracks.length === 0) {
    startPoint = new THREE.Vector3(0, 0, 0);
    endPoint = new THREE.Vector3(0, 0, TRACK_SEGMENT_LENGTH);
    direction = new THREE.Vector3(0, 0, 1);
  } else {
    const last = tracks[tracks.length - 1];
    const lastDir = last.userData.direction;
    startPoint = last.userData.endPoint;

    let secondLastDir = lastDir.clone();
    if (tracks.length > 1) {
      secondLastDir = tracks[tracks.length - 2].userData.direction;
    }
    const currentCurve = lastDir.angleTo(secondLastDir);
    const curveProb = Math.max(0.15, 0.35 - currentCurve);

    if (Math.random() < curveProb) {
      const maxAngle = Math.max(0.05, 0.22 - currentCurve);
      const curveAngle = Math.random() * (maxAngle - 0.05) + 0.05;
      let curveDir = Math.random() < 0.5 ? 1 : -1;
      if (currentCurve > 0.05 && tracks.length > 2) {
        const lastCurveDir = Math.sign(lastDir.clone().cross(secondLastDir).y);
        if (Math.random() < 0.7) curveDir = lastCurveDir;
      }
      direction = lastDir.clone().applyAxisAngle(new THREE.Vector3(0, 1, 0), curveAngle * curveDir);
    } else {
      direction = lastDir.clone();
    }

    endPoint = startPoint.clone().add(direction.clone().multiplyScalar(TRACK_SEGMENT_LENGTH));
  }

  const seg = createTrackSegment(startPoint, endPoint);
  seg.userData = { startPoint, endPoint, direction: direction.normalize(), length: startPoint.distanceTo(endPoint) };
  tracks.push(seg);

  if (tracks.length > MAX_TRACKS) {
    const old = tracks.shift();
    scene.remove(old);
    old.traverse(c => { if (c.geometry) c.geometry.dispose(); if (c.material) c.material.dispose(); });
    currentTrackIndex = Math.max(0, currentTrackIndex - 1);
  }
}

// ==================== 环境景物 ====================
function addScenery() {
  const forward = trainDirection.clone().normalize();
  const right = new THREE.Vector3().crossVectors(forward, new THREE.Vector3(0, 1, 0)).normalize();

  // 树
  for (let i = 0; i < 40; i++) {
    const angle = (Math.random() - 0.5) * Math.PI * 1.2;
    const dist = 20 + Math.random() * 120;
    const x = Math.sin(angle) * dist;
    const z = Math.cos(angle) * dist;
    if (Math.abs(x) < 6) continue;

    const pos = trainPosition.clone()
      .add(forward.clone().multiplyScalar(z))
      .add(right.clone().multiplyScalar(x));

    const tree = createTree();
    tree.position.copy(pos);
    tree.position.y = 0.2;
    const s = 1.0 + Math.random() * 1.8;
    tree.scale.set(s, s, s);
    tree.rotation.y = Math.random() * Math.PI * 2;
    scene.add(tree);
    sceneryObjects.push(tree);
  }

  // 花田（小片彩色点）
  for (let i = 0; i < 8; i++) {
    const angle = (Math.random() - 0.5) * Math.PI;
    const dist = 30 + Math.random() * 80;
    const x = Math.sin(angle) * dist;
    const z = Math.cos(angle) * dist;
    if (Math.abs(x) < 8) continue;

    const pos = trainPosition.clone()
      .add(forward.clone().multiplyScalar(z))
      .add(right.clone().multiplyScalar(x));

    const flowers = createFlowerPatch();
    flowers.position.copy(pos);
    flowers.position.y = 0.05;
    scene.add(flowers);
    sceneryObjects.push(flowers);
  }

  // 湖泊
  if (Math.random() < 0.5 && lakes.length < 3) {
    const angle = (Math.random() - 0.5) * Math.PI * 0.8;
    const dist = 50 + Math.random() * 80;
    const x = Math.sin(angle) * dist;
    const z = Math.cos(angle) * dist;
    if (Math.abs(x) > 15) {
      const pos = trainPosition.clone()
        .add(forward.clone().multiplyScalar(z))
        .add(right.clone().multiplyScalar(x));
      const lake = createLake();
      lake.position.copy(pos);
      lake.position.y = 0.02;
      scene.add(lake);
      sceneryObjects.push(lake);
      lakes.push(lake);
    }
  }

  // 小房子
  if (Math.random() < 0.4) {
    const angle = (Math.random() - 0.5) * Math.PI * 0.8;
    const dist = 40 + Math.random() * 70;
    const x = Math.sin(angle) * dist;
    const z = Math.cos(angle) * dist;
    if (Math.abs(x) > 12) {
      const pos = trainPosition.clone()
        .add(forward.clone().multiplyScalar(z))
        .add(right.clone().multiplyScalar(x));
      const house = createHouse();
      house.position.copy(pos);
      house.position.y = 0;
      house.rotation.y = Math.random() * Math.PI;
      scene.add(house);
      sceneryObjects.push(house);
    }
  }

  // 云
  for (let i = 0; i < 4; i++) {
    const cloud = createCloud();
    const angle = (Math.random() - 0.5) * Math.PI;
    const dist = 60 + Math.random() * 100;
    cloud.position.copy(trainPosition)
      .add(forward.clone().multiplyScalar(Math.cos(angle) * dist))
      .add(right.clone().multiplyScalar(Math.sin(angle) * dist));
    cloud.position.y = 40 + Math.random() * 30;
    scene.add(cloud);
    sceneryObjects.push(cloud);
    clouds.push(cloud);
  }

  // 飞鸟
  if (birds.length < 6 && Math.random() < 0.5) {
    const bird = createBird();
    bird.position.copy(trainPosition);
    bird.position.x += (Math.random() - 0.5) * 60;
    bird.position.y = 15 + Math.random() * 20;
    bird.position.z += 30 + Math.random() * 50;
    scene.add(bird);
    sceneryObjects.push(bird);
    birds.push(bird);
  }
}

function createTree() {
  const group = new THREE.Group();
  const type = Math.random();

  if (type < 0.5) {
    // 松树
    const trunkGeo = new THREE.CylinderGeometry(0.15, 0.25, 1.5, 6);
    const trunkMat = new THREE.MeshStandardMaterial({ color: 0x5a3a20, roughness: 0.9 });
    const trunk = new THREE.Mesh(trunkGeo, trunkMat);
    trunk.position.y = 0.75;
    trunk.castShadow = true;
    group.add(trunk);

    for (let i = 0; i < 3; i++) {
      const coneGeo = new THREE.ConeGeometry(1.2 - i * 0.25, 1.2, 7);
      const coneMat = new THREE.MeshStandardMaterial({
        color: new THREE.Color().setHSL(0.32, 0.5, 0.25 + Math.random() * 0.1),
        roughness: 0.9
      });
      const cone = new THREE.Mesh(coneGeo, coneMat);
      cone.position.y = 1.5 + i * 0.7;
      cone.castShadow = true;
      group.add(cone);
    }
  } else {
    // 阔叶树
    const trunkGeo = new THREE.CylinderGeometry(0.2, 0.35, 2, 6);
    const trunkMat = new THREE.MeshStandardMaterial({ color: 0x6a4a2a, roughness: 0.9 });
    const trunk = new THREE.Mesh(trunkGeo, trunkMat);
    trunk.position.y = 1;
    trunk.castShadow = true;
    group.add(trunk);

    const foliageGeo = new THREE.SphereGeometry(1.5, 8, 6);
    const foliageMat = new THREE.MeshStandardMaterial({
      color: new THREE.Color().setHSL(0.28 + Math.random() * 0.08, 0.55, 0.3 + Math.random() * 0.1),
      roughness: 0.9
    });
    const foliage = new THREE.Mesh(foliageGeo, foliageMat);
    foliage.position.y = 2.8;
    foliage.scale.y = 0.85;
    foliage.castShadow = true;
    group.add(foliage);
  }

  return group;
}

function createFlowerPatch() {
  const group = new THREE.Group();
  const colors = [0xff6b8a, 0xffd93d, 0xffffff, 0xff8c42, 0xc78bff];
  const color = colors[Math.floor(Math.random() * colors.length)];

  for (let i = 0; i < 15; i++) {
    const flowerGeo = new THREE.SphereGeometry(0.12, 5, 4);
    const flowerMat = new THREE.MeshStandardMaterial({ color, roughness: 0.8 });
    const flower = new THREE.Mesh(flowerGeo, flowerMat);
    flower.position.set(
      (Math.random() - 0.5) * 4,
      0.1 + Math.random() * 0.2,
      (Math.random() - 0.5) * 4
    );
    group.add(flower);
  }
  return group;
}

function createLake() {
  const group = new THREE.Group();
  const size = 8 + Math.random() * 12;

  const lakeGeo = new THREE.CircleGeometry(size, 24);
  const lakeMat = new THREE.MeshStandardMaterial({
    color: 0x3a7ab8,
    roughness: 0.1,
    metalness: 0.3,
    transparent: true,
    opacity: 0.85
  });
  const lake = new THREE.Mesh(lakeGeo, lakeMat);
  lake.rotation.x = -Math.PI / 2;
  lake.receiveShadow = true;
  group.add(lake);

  // 湖岸石头
  for (let i = 0; i < 8; i++) {
    const angle = (i / 8) * Math.PI * 2;
    const rockGeo = new THREE.DodecahedronGeometry(0.5 + Math.random() * 0.5, 0);
    const rockMat = new THREE.MeshStandardMaterial({ color: 0x8a8070, roughness: 0.95 });
    const rock = new THREE.Mesh(rockGeo, rockMat);
    rock.position.set(Math.cos(angle) * (size + 0.5), 0.2, Math.sin(angle) * (size + 0.5));
    rock.rotation.set(Math.random(), Math.random(), Math.random());
    rock.castShadow = true;
    group.add(rock);
  }

  group.userData.isLake = true;
  group.userData.size = size;
  return group;
}

function createHouse() {
  const group = new THREE.Group();

  // 主体
  const bodyGeo = new THREE.BoxGeometry(3, 2.2, 2.5);
  const bodyMat = new THREE.MeshStandardMaterial({
    color: new THREE.Color().setHSL(0.08 + Math.random() * 0.05, 0.3, 0.7 + Math.random() * 0.1),
    roughness: 0.85
  });
  const body = new THREE.Mesh(bodyGeo, bodyMat);
  body.position.y = 1.1;
  body.castShadow = true;
  body.receiveShadow = true;
  group.add(body);

  // 屋顶
  const roofGeo = new THREE.ConeGeometry(2.4, 1.5, 4);
  const roofMat = new THREE.MeshStandardMaterial({ color: 0x8a3a2a, roughness: 0.8 });
  const roof = new THREE.Mesh(roofGeo, roofMat);
  roof.position.y = 2.95;
  roof.rotation.y = Math.PI / 4;
  roof.castShadow = true;
  group.add(roof);

  // 门
  const doorGeo = new THREE.BoxGeometry(0.7, 1.3, 0.1);
  const doorMat = new THREE.MeshStandardMaterial({ color: 0x4a3020, roughness: 0.7 });
  const door = new THREE.Mesh(doorGeo, doorMat);
  door.position.set(0, 0.65, 1.26);
  group.add(door);

  // 窗户
  const windowGeo = new THREE.BoxGeometry(0.5, 0.5, 0.08);
  const windowMat = new THREE.MeshStandardMaterial({ color: 0x88b8d8, roughness: 0.2, metalness: 0.3, emissive: 0x223344, emissiveIntensity: 0.3 });
  const w1 = new THREE.Mesh(windowGeo, windowMat);
  w1.position.set(-0.9, 1.3, 1.26);
  group.add(w1);
  const w2 = new THREE.Mesh(windowGeo, windowMat);
  w2.position.set(0.9, 1.3, 1.26);
  group.add(w2);

  // 烟囱
  const chimneyGeo = new THREE.BoxGeometry(0.4, 1, 0.4);
  const chimneyMat = new THREE.MeshStandardMaterial({ color: 0x6a4a3a, roughness: 0.9 });
  const chimney = new THREE.Mesh(chimneyGeo, chimneyMat);
  chimney.position.set(0.8, 3.3, 0);
  chimney.castShadow = true;
  group.add(chimney);

  return group;
}

function createCloud() {
  const group = new THREE.Group();
  const numPuffs = 4 + Math.floor(Math.random() * 4);
  const cloudMat = new THREE.MeshStandardMaterial({
    color: 0xffffff,
    transparent: true,
    opacity: 0.85,
    roughness: 1.0
  });

  for (let i = 0; i < numPuffs; i++) {
    const puffGeo = new THREE.SphereGeometry(1.5 + Math.random() * 2, 8, 6);
    const puff = new THREE.Mesh(puffGeo, cloudMat);
    puff.position.set(i * 2 - numPuffs, (Math.random() - 0.5) * 1, (Math.random() - 0.5) * 2);
    puff.scale.y = 0.6 + Math.random() * 0.3;
    group.add(puff);
  }

  const s = 2 + Math.random() * 2;
  group.scale.set(s, s * 0.7, s);
  group.userData.speed = 0.5 + Math.random() * 0.5;
  return group;
}

function createBird() {
  const group = new THREE.Group();
  const birdMat = new THREE.MeshBasicMaterial({ color: 0x222222, side: THREE.DoubleSide });

  // 左翅
  const leftWingGeo = new THREE.PlaneGeometry(1.2, 0.4);
  leftWingGeo.translate(-0.6, 0, 0);
  const leftWing = new THREE.Mesh(leftWingGeo, birdMat);
  group.add(leftWing);

  // 右翅
  const rightWingGeo = new THREE.PlaneGeometry(1.2, 0.4);
  rightWingGeo.translate(0.6, 0, 0);
  const rightWing = new THREE.Mesh(rightWingGeo, birdMat);
  group.add(rightWing);

  group.userData.leftWing = leftWing;
  group.userData.rightWing = rightWing;
  group.userData.phase = Math.random() * Math.PI * 2;
  group.userData.speed = 0.8 + Math.random() * 0.5;

  return group;
}

// ==================== 更新环境 ====================
function updateScenery(delta) {
  // 清理远处景物
  for (let i = sceneryObjects.length - 1; i >= 0; i--) {
    const obj = sceneryObjects[i];
    if (obj.position.distanceTo(trainPosition) > 250) {
      scene.remove(obj);
      sceneryObjects.splice(i, 1);
      const ci = clouds.indexOf(obj);
      if (ci >= 0) clouds.splice(ci, 1);
      const bi = birds.indexOf(obj);
      if (bi >= 0) birds.splice(bi, 1);
      const li = lakes.indexOf(obj);
      if (li >= 0) lakes.splice(li, 1);
    }
  }

  // 云朵飘动
  for (const cloud of clouds) {
    cloud.position.x += cloud.userData.speed * delta * 2;
  }

  // 飞鸟扇翅和移动
  for (const bird of birds) {
    bird.userData.phase += delta * 8 * bird.userData.speed;
    const flap = Math.sin(bird.userData.phase) * 0.5;
    bird.userData.leftWing.rotation.z = flap;
    bird.userData.rightWing.rotation.z = -flap;
    bird.position.x += bird.userData.speed * delta * 5;
    bird.position.z += delta * 2;
    bird.position.y += Math.sin(bird.userData.phase * 0.3) * delta * 0.5;
    bird.lookAt(bird.position.x + 1, bird.position.y, bird.position.z + 0.5);
  }

  // 湖泊水面波动
  for (const lake of lakes) {
    lake.rotation.y += delta * 0.02;
  }

  // 定期添加新景物
  if (trainPosition.distanceTo(lastSceneryUpdatePos) > SCENERY_UPDATE_DIST) {
    lastSceneryUpdatePos.copy(trainPosition);
    addScenery();
  }
}

// ==================== 火车运动 ====================
function updateTrain(delta) {
  // 平滑速度
  trainSpeed += (targetSpeed - trainSpeed) * 0.05;

  const track = tracks[currentTrackIndex];
  if (!track) return;

  currentTrackT += (trainSpeed * delta) / track.userData.length;

  let targetDir = track.userData.direction.clone();
  if (currentTrackT > 0.8 && currentTrackIndex < tracks.length - 1) {
    const next = tracks[currentTrackIndex + 1];
    if (next) {
      const blend = (currentTrackT - 0.8) * 5;
      targetDir.lerp(next.userData.direction, blend);
    }
  }

  if (currentTrackT >= 1) {
    currentTrackT = 0;
    currentTrackIndex++;
    if (currentTrackIndex >= tracks.length - 5) addTrackSegment();
    if (currentTrackIndex >= tracks.length) currentTrackIndex = 0;
  }

  const t = tracks[currentTrackIndex];
  trainPosition.lerpVectors(t.userData.startPoint, t.userData.endPoint, currentTrackT);
  trainDirection.lerp(targetDir, 0.1);
}

// ==================== 相机 ====================
function updateCamera() {
  const targetPos = trainPosition.clone();
  targetPos.y += 2.2;
  const backOffset = trainDirection.clone().multiplyScalar(-1.5);
  targetPos.add(backOffset);

  camera.position.lerp(targetPos, 0.1);

  const lookAt = trainPosition.clone().add(trainDirection.clone().multiplyScalar(20));
  lookAt.y = trainPosition.y + 0.8;
  camera.lookAt(lookAt);

  // 速度感FOV
  const targetFov = 68 + (trainSpeed / MAX_SPEED) * 12;
  camera.fov += (targetFov - camera.fov) * 0.05;
  camera.updateProjectionMatrix();
}

// ==================== 时间更新 ====================
function updateTime(delta) {
  if (!paused) {
    timeOfDay += timeFlowSpeed * delta;
    if (timeOfDay >= 1) timeOfDay -= 1;
  }
  updateUI();
}

// ==================== UI ====================
function updateUI() {
  // 速度表
  const speedEl = document.getElementById('speedValue');
  if (speedEl) speedEl.textContent = Math.round(trainSpeed * 10);

  // 时间显示
  const timeEl = document.getElementById('timeValue');
  if (timeEl) {
    const hours = Math.floor(timeOfDay * 24);
    const mins = Math.floor((timeOfDay * 24 - hours) * 60);
    timeEl.textContent = `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`;
  }

  // 时间段标签
  const periodEl = document.getElementById('periodLabel');
  if (periodEl) {
    let period = '';
    if (timeOfDay < 0.2) period = '深夜';
    else if (timeOfDay < 0.3) period = '黎明';
    else if (timeOfDay < 0.45) period = '上午';
    else if (timeOfDay < 0.55) period = '正午';
    else if (timeOfDay < 0.7) period = '下午';
    else if (timeOfDay < 0.8) period = '黄昏';
    else period = '夜晚';
    periodEl.textContent = period;
  }
}

// ==================== 交互 ====================
function onKeyDown(e) {
  if (!started) {
    if (e.code === 'Space' || e.code === 'Enter') {
      startExperience();
    }
    return;
  }

  switch (e.code) {
    case 'ArrowUp':
    case 'KeyW':
      targetSpeed = Math.min(MAX_SPEED, targetSpeed + 5);
      break;
    case 'ArrowDown':
    case 'KeyS':
      targetSpeed = Math.max(MIN_SPEED, targetSpeed - 5);
      break;
    case 'Space':
      paused = !paused;
      document.getElementById('pauseIndicator').style.display = paused ? 'block' : 'none';
      break;
    case 'KeyT':
      // 加速时间
      timeFlowSpeed = timeFlowSpeed > 0.05 ? 0.008 : 0.08;
      break;
    case 'KeyR':
      // 重置时间到上午
      timeOfDay = 0.32;
      break;
  }
}

function startExperience() {
  started = true;
  document.getElementById('startScreen').style.display = 'none';
  document.getElementById('hud').style.display = 'block';
}

// ==================== 事件 ====================
function onResize() {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
}

// ==================== 主循环 ====================
function animate() {
  requestAnimationFrame(animate);
  const delta = Math.min(clock.getDelta(), 0.1);

  if (started) {
    updateTime(delta);
    updateTrain(delta);
    updateScenery(delta);
    updateGround();
    updateCamera();
  }

  updateSky();
  updateCelestialBodies();
  updateStars();

  renderer.render(scene, camera);
}

// 启动
init();
