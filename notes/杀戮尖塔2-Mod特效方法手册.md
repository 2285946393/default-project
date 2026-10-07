# 杀戮尖塔 2 · Mod 特效方法手册

> **为什么要写这份**：用户 2026-10-07 原话——"我发现这个还是要熟悉各种特效代码啥的。
> 我想让你们多多学习特效的方法，不然做其他的 mod 会特别乏力。"
>
> 所以这份不是"某个特效怎么做"，而是**做法总表**：下次做任何 mod 要加特效，从这里挑一条抄。
>
> **取证来源**（都实测过，不是"我记得"）：
> - 本机 `sts2.dll`（`D:\software\steam\...\data_sts2_windows_x86_64\sts2.dll`）字符串实测
> - 本机 `SlayTheSpire2.pck` 实测解析（15890 条）→ 清单见 [杀戮尖塔2-本体特效资源清单.md]
> - 官方/社区教程资料包（`projects/sts2-moddev-workspace/`，Steam 工坊 "STS2 Agent ModDev Workspace"，作者 jezxx）
> - 反编译源码基线 0.111.0（同资料包 `02-游戏反编译资料/`）
> - 我们自己的 mod：`projects/dou-spire/`
>
> ⚠️ **版本边界**：资料包的反编译源码是 **0.111.0** 的快照，本机游戏是更新的 build。
> `VfxCmd` 这套 API 已在本机 dll 里核对存在，但**其余结论以本机为准**，别只信快照。

---

## 📍 从这里开始（配套资料地图）

> **找不着文件？** 全仓库笔记总目录在 [`notes/README.md`](README.md)，按主题分区 + 每条一句"什么时候看"。

**六份笔记，按你要解决的问题挑：**

| 你现在想干嘛 | 看这份 |
|---|---|
| **东西坏了，先查病因** | ⭐ `杀戮尖塔2-特效-坑清单（按症状查）.md` —— 按"症状"索引（特效不显示 / 游戏卡死 / 联机串味 / 打包加载不出来 / 观感乏力…），每条带出处 |
| **要动手做特效了** | 就是本文件（往下看 §零 选路）+ `projects/sts2-vfx-template/`（13 个文件的骨架，`README.md` 有速查表） |
| **从零画一套 = 白干，先看看本体有什么** | `杀戮尖塔2-本体特效资源清单.md`（312 场景 / 100 着色器 / 160 材质）+ `projects/sts2-vanilla-vfx/`（可检索的 `索引-特效目录.md`） |
| **想知道"别人怎么做的"（美术）** | `杀戮尖塔2-特效实战-万象辉星拆解.md` —— `StartPos` 约定、注册表、pck 三个坑 |
| **想知道"怎么打起来爽"（演出）** | `杀戮尖塔2-特效实战2-动作与特效-打击感拆解.md` —— 打击感六件套、位移状态机 |
| **想知道"怎么低成本不闯祸"（工程）** | `杀戮尖塔2-特效实战3-免pck与表现层工程学.md` —— 免 pck、着色器写成字符串、表现层不阻塞游戏 |

**三层骨架（`projects/sts2-vfx-template/`）：**

```
美术层   VfxUtil · CardFx · CardVfxPlayer · WorldEnvironmentUtil     ← 万象辉星
演出层   Juice.cs（拉伸/弧线/打击停顿/目标中心/朝向翻转）             ← 动作与特效
工程层   LooseAsset.cs（免 pck）· PresentationFlow.cs（不阻塞游戏）   ← More Ironclad
入口     ModEntry · Setting · Sfx
素材     scenes/ 两个场景模板（带 StartPos / 不带）· manifest.example.json
```

**拆解产物（可搜索、可对答案）：**

| 目录 | 是什么 |
|---|---|
| `projects/sts2-vanilla-vfx/` | **本体**特效库：312 `.tscn` + 100 `.gdshader` + 160 `.tres` + 可检索索引 |
| `projects/ref-regentfx/` | 万象辉星：79 个反编译 `.cs` + 自制资源 |
| `projects/ref-meleeattack/` | 动作与特效：76 个反编译 `.cs` |
| `projects/ref-shieldonly/` | More Ironclad：3 个 dll 反编译（26917 行） |
| `projects/sts2-moddev-workspace/` | 官方+社区中文教程 + 0.111.0 反编译源码 |

**工具：**

| 工具 | 用途 |
|---|---|
| `tools/extract-pck-files.py <pck> <out> [关键词]` | 抽 pck 文件内容（⚠️ offset 要加 `file_base`） |
| `tools/index-vanilla-vfx.py <库目录> <输出md>` | 重建场景目录（⚠️ `id=` 正则要写 `(?:^\|\s)`） |
| `tools/scan-pck-files.py <pck> [关键词]` | 只列目录 |
| `tools/scan-game-strings.py <dll> <关键词…>` | 二进制字符串扫描（**`strings` 命令不在 PATH**） |
| `~/.dotnet/tools/ilspycmd.exe -o <out> <mod>.dll` | **反编译别人的 mod**（唯一能看到代码的路） |

---

## 零、先选路（三选一，成本从低到高）

| 路 | 做法 | 素材需求 | 适合 | 落地难度 |
|---|---|---|---|---|
| **A. 游戏本体特效** | 调 `VfxCmd`，或直接 `ResourceLoader` 载 `res://scenes/vfx/*.tscn` | **零** | 打击、爆炸、治疗、闪光、烟雾……几乎任何常见效果 | ★ 最低 |
| **B. 自己做特效** | 帧动画 / 图集 / 粒子 / Spine | 要图 | 本体没有的专属效果（我们的二次元角色、牌桌专属） | ★★★ |
| **C. Shader** | 写 `.gdshader` 挂到节点材质上 | 零（纯代码） | 改色、发光、描边、扭曲、全屏后处理 | ★★ |

**铁律：先翻 A。** 本体内置 **312 个特效场景 + 100 个着色器**，
"下雨""闪电""碎岩""锁链""全屏大字""气泡对话框"这些**全都有现成的**。
从零画一套 = 白干好几天。

---

## 一、路 A：直接用游戏本体特效

> **先看本地素材库** 👉 `projects/sts2-vanilla-vfx/`
> 里面已经放好从本机 pck 抽出来的 **312 个特效场景（完整文本节点树）+ 160 个材质 + 100 个着色器源码**，
> 以及一份可检索目录 `索引-特效目录.md`。
> **想找效果先搜它**，别凭空画。

### A1 战斗内一行调用（`VfxCmd`）

`VfxCmd` 是 `static class`，**全部方法都是静态的**，加 `using MegaCrit.Sts2.Core.Commands;` 直接用：

```csharp
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;   // CombatSide

// 在指定像素位置播放（vfxContainer 传当前战斗的容器，或 null）
VfxCmd.PlayVfx(position, "vfx/vfx_attack_slash", vfxContainer);

// 在生物「中心」播放（会自动跳过已死的目标）
VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_starry_impact");

// 在生物「脚下」播放
VfxCmd.PlayOnCreature(target, "vfx/vfx_bloody_impact");

// 整侧中心（AOE，一边全中）
VfxCmd.PlayOnSide(CombatSide.Enemy, "vfx/vfx_heavy_blunt", combatState);

// 全屏播放（如肾上腺素）
VfxCmd.PlayFullScreenInCombat("vfx/vfx_adrenaline", spawner);

// 批量：每个目标各来一份
VfxCmd.PlayOnCreatureCenters(enemies, "vfx/vfx_scratch");
```

**唯一返回节点的是 `PlayNonCombatVfx`**（非战斗用，见 A5）：

```csharp
public static Node2D? PlayNonCombatVfx(Node container, Vector2 position, string path)
```

其他方法**都返回 void** —— 想事后改这个特效（上色/缩放）就拿不到句柄，得自己写（见 A4）。

### A2 ⚠️ 路径格式：`VfxCmd` 只吃「内部路径」

这是最容易踩的一条。看反编译源码 `MegaCrit.Sts2.Core.Commands/VfxCmd.cs` + `Helpers/SceneHelper.cs`：

```csharp
// SceneHelper.cs（逐字抄的）
public static string GetScenePath(string innerPath) {
    if (innerPath.StartsWith('/'))
        innerPath = innerPath.Substring(1);
    return "res://scenes/" + innerPath + ".tscn";
}
```

所以：

| 你传的 | 实际加载 |
|---|---|
| `"vfx/vfx_attack_slash"` | `res://scenes/vfx/vfx_attack_slash.tscn` ✅ |
| `"/vfx/vfx_attack_slash"` | 同上 ✅（开头 `/` 会被剥掉） |
| `"res://scenes/vfx/vfx_attack_slash.tscn"` | `res://scenes/res://scenes/vfx/...tscn.tscn` ❌ **加载为空** |

**"加载为空"的表现不是报错，是卡牌结算卡死 / 特效静默不出现。**
（资料包 `03-模组制作经验/20-战斗特效与视觉挂载.md` 也专门警告过这条。）

> 反过来：`ResourceLoader.Load<PackedScene>("res://scenes/vfx/xxx.tscn")` 这种用法，
> **必须传完整 `res://` 路径**。两套机制别混。

### A3 `VfxCmd` 内置路径常量（27 个，源码逐字核对）

```csharp
// 攻击类
VfxCmd.slashPath            // vfx/vfx_attack_slash        斩击
VfxCmd.bluntPath            // vfx/vfx_attack_blunt        钝击
VfxCmd.lightningPath        // vfx/vfx_attack_lightning    闪电
VfxCmd.heavyBluntPath       // vfx/vfx_heavy_blunt         重击
VfxCmd.bloodyImpactPath     // vfx/vfx_bloody_impact       血腥冲击
VfxCmd.starryImpactVfx      // vfx/vfx_starry_impact       星辰冲击（带屏幕扭曲）
VfxCmd.giantHorizontalSlashPath // vfx/vfx_giant_horizontal_slash  巨型横斩
VfxCmd.sandyImpactPath      // vfx/vfx_sandy_impact        沙土冲击
VfxCmd.slimeImpactVfxPath   // vfx/vfx_slime_impact        黏液冲击
VfxCmd.dramaticStabPath     // vfx/vfx_dramatic_stab       戏剧性突刺
VfxCmd.thrashPath           // vfx/vfx_thrash              猛击
VfxCmd.hellraiserSwordVfxPath // vfx/hellraiser_attack_vfx

// 投射类
VfxCmd.daggerThrowPath      // vfx/vfx_dagger_throw        飞刀
VfxCmd.daggerSprayPath      // vfx/vfx_dagger_spray        飞刀散射
VfxCmd.flyingSlashPath      // vfx/vfx_flying_slash        飞行斩击
VfxCmd.chainPath            // vfx/vfx_chain               锁链

// 技能类
VfxCmd.blockPath            // vfx/vfx_block               格挡
VfxCmd.healPath             // vfx/vfx_cross_heal          治疗十字
VfxCmd.gazePath             // vfx/vfx_gaze                凝视
VfxCmd.adrenalinePath       // vfx/vfx_adrenaline          肾上腺素（全屏）
VfxCmd.rockShatterPath      // vfx/vfx_rock_shatter        岩石碎裂
VfxCmd.bitePath             // vfx/vfx_bite                撕咬
VfxCmd.scratchPath          // vfx/vfx_scratch             抓挠
VfxCmd.screamVfx            // vfx/vfx_scream              尖叫（区域扭曲）
VfxCmd.spookyScreamVfx      // vfx/vfx_spooky_scream       阴森尖叫

// 金币爆炸
VfxCmd.coinExplosionSmallPath   // vfx/vfx_coin_explosion_small
VfxCmd.coinExplosionRegularPath // vfx/vfx_coin_explosion_regular
VfxCmd.coinExplosionJumboPath   // vfx/vfx_coin_explosion_jumbo

// 全部清单（也可当遍历用）
foreach (var p in VfxCmd.AssetPaths) { /* 已经是 res:// 完整路径 */ }
```

**注意**：`VfxCmd.AssetPaths` 里的值**已经过 `SceneHelper.GetScenePath` 转换**，
是 `res://scenes/...` 完整路径，可以直接喂 `ResourceLoader.Load`。

### A4 自己想改特效（上色/缩放）—— 照抄 `PlayVfx` 写一份

`VfxCmd` 不返回节点，但它自己就是 8 行。抄过来加个 `return` 就有句柄了：

```csharp
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;

/// <summary>跟 VfxCmd.PlayVfx 同款，但把 Node2D 返回出来，方便事后改。</summary>
public static Node2D? PlayVfxAndGet(Vector2 position, string innerPath, Control? container) {
    string scenePath = SceneHelper.GetScenePath(innerPath);
    var node = PreloadManager.Cache.GetScene(scenePath)
                                 .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
    container?.AddChildSafely(node);
    node.GlobalPosition = position;
    return node;
}

// 用它上色（把整个特效染成青色）
var vfx = PlayVfxAndGet(pos, "vfx/vfx_heavy_blunt", container);
if (vfx != null) {
    foreach (var child in vfx.GetChildren()) {
        if (child is CanvasItem ci) ci.Modulate = new Color(0.4f, 0.9f, 1f);
    }
}
```

**两条硬规矩**（官方教程 3-7 明写，我们踩过）：

1. **改 Material 必须先复制**：`(Material)mat.Duplicate()`。直接改会**污染所有同类特效**——因为场景是共享缓存的。
2. **改 `Scale` 对粒子无效**：粒子发射范围是按场景里的 `ParticleProcessMaterial` 预先算好的，
   缩放节点不会放大发射区域。要改**粒子自己的相对位置参数**，别改节点 Scale。

### A5 非战斗场景（我们的牌桌 = 非战斗）

牌桌不挂在 `NCombatRoom` 下，所以 `NCombatRoom.Instance?.CombatVfxContainer` 是 `null`，
上面那堆 `PlayOnCreature*` 全都不适用。用这个：

```csharp
// 容器传任意 Node（比如你的牌桌根节点），返回节点句柄
Node2D? vfx = VfxCmd.PlayNonCombatVfx(myContainer, globalPosition, "vfx/vfx_bounce_spark");
```

⚠️ **`position` 是 `GlobalPosition`** —— 传全局坐标，不是相对父级的局部坐标。
（源码里写的是 `node2D.GlobalPosition = position`。）

---

## 二、路 B：自己做特效

> 官方教程明确建议：**能载入编辑器手搓就手搓**。
> "用 AI 生成 .tscn 可能会有各种引用错误。" —— 尤其是 `SpriteFrames`、动画库、子节点 `owner`。

### B1 帧动画（序列帧，最通用）

素材：多张**带透明通道的 PNG** 序列图。

流程（Godot/Megadot 编辑器）：
1. 图放 `YourMod/images/effect_x/fn0001.png` 起编号
2. 新建场景，根 `Node2D` → 加 `AnimatedSprite2D`
3. 检查器里给它新建 `SpriteFrames` → "从文件中添加帧" → 按顺序导入
4. 一次性特效绑个自毁脚本：

```csharp
using Godot;

public partial class AutoFreeEffect : AnimatedSprite2D {
    public override void _Ready() => AnimationFinished += QueueFree;
}
```

**优化**：图太大导致卡顿 → 批量选中图片，导入设置里缩分辨率（1080p → 720/540）。

### B2 图集（一张图切 N 帧）

素材是一张大图、按矩形网格排布（本体自带的 `vfx_slash` 就是）。

1. 跟 B1 一样新建 `SpriteFrames`
2. 这一步选 **"从精灵表中添加帧"**
3. 填 **水平数量 / 垂直数量**（比如 3×2），勾"裁切"自动去白边
4. 依次点 6 次加入

优点：省内存、性能好。缺点：不适合超大范围特效。

### B3 粒子（`GPUParticles2D`）

```tscn
[node name="vfx_poof" type="GPUParticles2D"]
rotation = 3.14159
amount = 1
texture = ExtResource("2_62oo3")
lifetime = 0.75
one_shot = true          ; 只播一次
fixed_fps = 60
local_coords = true
emitting = true
process_material = SubResource("ParticleProcessMaterial_xxx")
```

| 属性 | 作用 | 常用值 |
|---|---|---|
| `one_shot` | 播一次就停 | `true`（爆发类） |
| `amount` | 粒子数 | 1~100 |
| `lifetime` | 存活秒数 | 0.1~3.0 |
| `explosiveness` | 爆发度 | `1.0` = 瞬间全发 |
| `fixed_fps` | 固定帧率 | 60 |

`ParticleProcessMaterial` 常用参数：
- `direction` / `spread`：方向与扩散角
- `initial_velocity_min/max`：初速
- `gravity` / `damping_min`：重力与阻力
- `scale_curve` / `alpha_curve`：缩放、淡出曲线
- 环形发射：`emission_shape = 6` + `emission_ring_radius`
- 湍流：`turbulence_enabled = true` + `turbulence_noise_strength`
- 颜色变化：`color_ramp`

> 提示：粒子很吃"素材怎么画"。**简单的圆形/方形/星形透明 PNG 就够用**，
> 让 AI 照着这几个图设计参数即可。

### B4 三套官方模板（本体自带，直接抄）

本体把官方自己用的模板都打包进来了，挑一个改最省事：

```
res://scenes/vfx/templates/vfx_template_particles.tscn        ← 粒子
res://scenes/vfx/templates/vfx_template_spine.tscn            ← Spine 骨骼
res://scenes/vfx/templates/vfx_template_sprite_animation.tscn ← 序列帧
```

---

## 三、路 C：Shader

Shader 是**纯代码**，对 AI 最友好——想要什么效果，把参考的 `.gdshader` 源码喂进来改就行。

### C1 挂一个万能滤镜到任意 Sprite/AnimatedSprite

资料包 `01-官方与社区教程/.../3-6 Shader/README.md` 里有一份 **450 行的万能 shader**
（包含：透明度 / 发光 / Bloom / 高斯模糊 / 运动模糊 / 锐化 / 波纹扭曲 / 漩涡 / 镜头畸变 /
色差 RGB 分离 / 色相 / 饱和度 / 对比度 / 颜色叠加 / 渐变映射 / 描边 / 像素化 / 反相 / 灰度 / 复古 / 二值化）。

**做法**：存成 `advanced.gdshader` → 目标节点右侧 `CanvasItem → Material → 新建 ShaderMaterial`
→ 快速加载这个 shader → 下方检查器调参数。

**典型用途**：史莱姆一套素材，靠 shader 调出"红/蓝/绿/发光/扭曲"五种变体，不用画五套。

### C2 区域扭曲（本体怎么做的）

看本体两个例子就懂套路：
- `res://scenes/vfx/vfx_starry_impact.tscn`（储君攻击受击）
- `res://scenes/vfx/vfx_scream.tscn`（尖叫）

它们内部都挂了 `vfx_distortion` 系的 shader，表现是**屏幕上一块矩形区域被扭曲**。
照着偷源码改参数即可。

### C3 全屏后处理（亮度/曝光/发光/饱和度）

靠 `NGame.Instance.ActivateWorldEnvironment()` 拿全局 `WorldEnvironment` 节点，
然后改 `Environment` 的属性，配 `Tween` 做淡入淡出。

```csharp
using Godot;
using MegaCrit.Sts2.Core.Nodes;

public static class WorldEnvFx {
    private static WorldEnvironment? _env;

    public static WorldEnvironment? Get() {
        if (_env != null && GodotObject.IsInstanceValid(_env)) return _env;
        _env = NGame.Instance?.ActivateWorldEnvironment();
        return _env;
    }

    public static void SetGlow(float v)     { var e = Get(); if (e != null) e.Environment.GlowIntensity = v; }
    public static void SetExposure(float v) { var e = Get(); if (e != null) e.Environment.TonemapExposure = v; }
    public static void SetSaturation(float v){ var e = Get(); if (e != null) e.Environment.AdjustmentSaturation = v; }
    public static void SetBrightness(float v){ var e = Get(); if (e != null) e.Environment.AdjustmentBrightness = v; }
    public static void SetContrast(float v){ var e = Get(); if (e != null) e.Environment.AdjustmentContrast = v; }

    public static void Reset() {
        SetExposure(1f); SetBrightness(1f); SetContrast(1f); SetSaturation(1f); SetGlow(0.8f);
    }
}
```

⚠️ **慎用**：过度曝光会光污染，有光敏性癫痫风险。做完记得 `Reset()`。

---

## 四、挂载：特效放哪儿（位置学）

### 4.1 `NCombatRoom` 结构（官方教程给的结构图）

```
NCombatRoom (Control)
├── %CombatUi                    UI 层
├── %CombatSceneContainer
│   ├── %AllyContainer           盟友（左）
│   │   └── NCreature (Player)
│   │       ├── Body             视觉/身体
│   │       ├── Visuals          视觉容器
│   │       ├── Hitbox
│   │       ├── IntentContainer
│   │       └── OrbManager
│   ├── %EnemyContainer          敌人（右）
│   └── EncounterSlots
├── %BgContainer                 背景         (ZIndex = -20)
├── %BackCombatVfxContainer      后台特效容器
├── %CombatVfxContainer          前台特效容器 (ZIndex = -9)
└── RadialBlur                   径向模糊
```

**选哪个容器**：

| 容器 | 放什么 |
|---|---|
| `CombatVfxContainer`（前台） | 短暂攻击特效、命中、粒子爆发 —— **要盖在角色上面** |
| `BackCombatVfxContainer`（后台） | 持续状态特效、背景元素 —— **要显示在角色后面**（如黑洞、柱状光） |

```csharp
Node? front = NCombatRoom.Instance?.CombatVfxContainer;
front.AddChildSafely(blade);

Node? back = NCombatRoom.Instance?.BackCombatVfxContainer;
back.AddChildSafely(blackhole);
```

特效之间还要叠，就改 `ZIndex`。

### 4.2 位置与朝向

```csharp
NCreature? node = NCombatRoom.Instance?.GetCreatureNode(creature);

Vector2 center = node.VfxSpawnPosition;   // 生物中心（特效就该放这）
Vector2 foot   = node.GlobalPosition;     // 脚底

// 打得大螃蟹时角色会朝左——放"身前"的特效必须动态判朝向
Node2D? body = NCombatRoom.Instance?.GetCreatureNode(creature)?.Body;
bool facingRight = body?.Scale.X > 0;
int xFac = facingRight ? 1 : -1;
Vector2 pos = center + new Vector2(100f * xFac, 0f);
```

---

## 五、生命周期与缓存（**这条最重要，最容易出"第一次卡顿/第二次就没了"**）

### 5.1 为什么必须自己做缓存

`PreloadManager.Cache` 在**换房间时会 `UnloadAssets()`**，只保留**游戏本体**的资源。
Mod 自己的场景会被清掉 → 表现是"第一次能播，进下一场就没了"。

**解法**：建一个 mod 私有的 `ConcurrentDictionary` 缓存，在 `Entry.cs` 初始化时预加载：

```csharp
using System.Collections.Concurrent;
using Godot;

public static class VFXUtil {
    // Mod 私有场景缓存 —— 不会被 PreloadManager 清掉
    public static readonly ConcurrentDictionary<string, PackedScene> ModSceneCache = new();

    /// <summary>在 Mod 初始化时调用，把所有自己的特效场景预热进缓存（避免首次播放卡顿）。</summary>
    public static void LoadScenes(params string[] resPaths) {
        foreach (var p in resPaths) {
            if (ModSceneCache.ContainsKey(p)) continue;
            var scene = ResourceLoader.Load<PackedScene>(p, null, ResourceLoader.CacheMode.Reuse);
            if (scene != null) ModSceneCache[p] = scene;
        }
    }

    public static Node2D GenVFXNode(string resPath) =>
        ModSceneCache.TryGetValue(resPath, out var s)
            ? s.Instantiate<Node2D>()
            : PreloadManager.Cache.GetScene(resPath).Instantiate<Node2D>();

    public static T GenVFXNode<T>(string resPath) where T : Node2D =>
        ModSceneCache.TryGetValue(resPath, out var s)
            ? s.Instantiate<T>()
            : PreloadManager.Cache.GetScene(resPath).Instantiate<T>();
}
```

### 5.2 播一条 + 自动销毁

```csharp
/// <summary>实例化一个特效，挂到指定容器，lifetime 秒后自动销毁。</summary>
public static Node2D? PlaySimple(Node container, string resPath, Vector2 position, float lifetime = 2f) {
    try {
        var node = VFXUtil.GenVFXNode(resPath);
        container.AddChildSafely(node);
        node.GlobalPosition = position;

        var timer = node.GetTree().CreateTimer(lifetime);
        timer.Timeout += () => {
            if (GodotObject.IsInstanceValid(node)) node.QueueFree();
        };
        return node;
    } catch (System.Exception e) {
        Entry.Logger.Warn($"特效播放失败 {resPath}: {e.Message}");
        return null;
    }
}
```

**关键点**：
- 一定要 `GodotObject.IsInstanceValid(node)` 再 `QueueFree` —— 换场景后节点可能已释放，直接调会炸。
- 挂 `Timer` 用 `GetTree().CreateTimer()`，别自己 `new Timer()`（要手动 `AddChild` 才生效）。

### 5.3 别把 UI 层和世界层搞混（**我们踩过的真坑，见第七节**）

`Node2D` 挂在 `Control` 下**是合法的**，但：

- `Node2D.Position` 是**父节点局部坐标（像素）**，不是 0~1 的比例。
- 想要"放在屏幕中央 40% 高度处"，要自己算成像素：
  `new Vector2(dir.Px(960), dir.Px(432))`（1920×1080 设计稿口径）。

---

## 六、自查清单（改完特效，逐条过）

资料包 `03-模组制作经验/20-战斗特效与视觉挂载.md` 的「完成判据」，我按我们能查的整理：

- [ ] **路径格式对**：`VfxCmd` 用内部路径（`vfx/xxx`）；`ResourceLoader` 用完整 `res://`
- [ ] **场景真的能加载**：`ResourceLoader.Exists(path)` 为真 + `Load` 不为 null（有日志）
- [ ] **PCK 里有条目**：`tools/scan-pck-files.py <pck> <关键词>` 能搜到
- [ ] **位置对**：在**正确的目标位置**出现（不是屏幕角落、不是 0,0）
- [ ] **坐标口径对**：像素 vs 比例 —— 别把缩放比当像素用
- [ ] **不重复播**：同一张牌不会因为走多个通道播两遍
- [ ] **场景切换后还在**：进下一场战斗/换房间后依然能播（缓存问题）
- [ ] **失败可诊断**：日志里能看出是"哪张牌、哪个通道、哪个路径"失败
- [ ] **没有静默死**：所有 `try/catch`，特效出错只记日志，**绝不把异常抛进 Godot 的信号派发**

> 铁律：**"文件存在" ≠ "能加载" ≠ "能渲染"**。三层都要各验一次。

---

## 七、我们自己的踩坑（斗地主尖塔实测）

### 坑 1：`DouSpireUi.Screen` 是**缩放比**，不是屏幕尺寸 —— 特效被扔到屏幕左上角

**现场**（`DouSpireCode/Table/DouSpireTable.cs` `PopMultiplier`，2026-10-07 写的）：

```csharp
var sw = DouSpireUi.Screen;      // ← 它 ≈ 1.0，不是 1920！
var sh = sw * 0.5625f;           // ← ≈ 0.5625
SpawnGameVfx("res://scenes/vfx/bounce_spark_vfx.tscn",
             new Vector2(sw * 0.5f, sh * 0.44f), 2.2f);
             // → Vector2(0.5, 0.247) —— 基本就是左上角原点
```

**为什么**（`DouSpireCode/Table/DouSpireUi.cs`）：

```csharp
public static float Screen { get; private set; } = 1f;   // ← 这是倍率
// UseScreen(): Screen = Mathf.Clamp(视口宽 / 1920f, 0.6f, 2.2f);
public static float Px(float baseSize) => baseSize * Screen * DouSpireConfig.UiScale;
```

`Screen` 是**"视口宽 ÷ 1920"的倍率**（0.6~2.2）。
所以 `Vector2(0.5, 0.247)` 传到 `node.Position` = **距牌桌左上角 0.5 像素**的地方。

**这就是"粒子特效不知道能不能显示"的答案**：它确实播了，只是在**左上角**，
小到看不见。日志里还会正常打 `特效已播 @(0.5, 0.25)`。

**正解**：位置一律走 `Px()` 换算（跟 `Place()` 的比例锚点是两套，别混）：

```csharp
// 1920×1080 设计稿口径 → 实际像素
var x = DouSpireUi.Px(1920f * 0.5f);    // 屏幕中央
var y = DouSpireUi.Px(1080f * 0.44f);   // 40% 高度
SpawnGameVfx("res://scenes/vfx/bounce_spark_vfx.tscn", new Vector2(x, y), 2.2f);
```

> 反面教材：`DouSpireUi.Place(ctrl, ax, ay, w, h)` 用的是 **0~1 比例锚点**（对 `Control` 有效）。
> 同一个类里两套坐标口径，`Place` 吃比例、`Px` 吃设计稿像素 —— **写特效时最容易串**。

### 坑 2：`SpawnGameVfx` 用的是完整 `res://` 路径（对），但没做缓存（错）

现在实现（`DouSpireTable.cs` `SpawnGameVfx`）：

```csharp
if (!ResourceLoader.Exists(resPath)) { Warn(...); return null; }   // ✅ 有检查
var ps = ResourceLoader.Load<PackedScene>(resPath);                // ✅ 没走 VfxCmd
var node = ps.Instantiate<Node2D>();                               // ⚠️ 没传 GenEditState.Disabled
AddChild(node);                                                    // ⚠️ 没走 ModSceneCache
node.Position = at;                                                // ⚠️ 局部像素坐标
```

改进方向（照第五节）：
1. `Instantiate<Node2D>(PackedScene.GenEditState.Disabled)` —— 本体所有地方都这么写
2. 走 `VFXUtil.ModSceneCache` 预加载 —— 否则换场景后本体缓存一清就没了
3. 用 `AddChildSafely` 代替 `AddChild`

> **✅ 已修（2026-10-08 01:18，国际版 WorkBuddy 接手 dou-spire 代码线）**
> 上面三条**一条都不用自己做** —— 本体早就有现成入口：**`VfxCmd.PlayNonCombatVfx`**。
>
> ```csharp
> // VfxCmd.cs
> public static Node2D? PlayNonCombatVfx(Node container, Vector2 position, string path)
> {
>     string scenePath = SceneHelper.GetScenePath(path);   // "res://scenes/" + path + ".tscn"
>     Node2D node2D = PreloadManager.Cache.GetScene(scenePath)
>                        .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);   // ← ② ③
>     container.AddChildSafely(node2D);                                            // ← ③
>     node2D.GlobalPosition = position;                                            // ← ④
>     return node2D;
> }
> ```
>
> 三点修正我们自己的认知：
> - **不需要自建缓存**。`AssetCache.GetAsset` 是**懒加载**的（命中不了就
>   `ResourceLoader.Load(..., CacheMode.Reuse)` 再存回去），只会打一条
>   `Asset not cached: <path>` 警告，**不会失败**。所以 `VFXUtil.ModSceneCache` 那套是给
>   **mod 自己的场景**用的，播本体场景直接走 `PreloadManager.Cache` 就行。
> - **参数是内部路径**（`"vfx/vfx_coin_explosion_jumbo"`），不是 `res://` 全路径 —— 见坑 A2。
> - **位置是 `GlobalPosition`**，所以传**视口像素**，不用管父级缩放（这条比 `node.Position` 更省心）。
>
> 现在 `DouSpireTable.SpawnGameVfx` 只剩一个薄壳：调 `PlayNonCombatVfx` + 可选缩放 + 记日志
> + 一个 5s 的兜底 `QueueFree`。

### 坑 3：本体 `bounce_spark_vfx.tscn` 不一定适合当"倍数翻倍"特效

它是**破盾火花**，跟"倍数翻倍"语义不搭。路 A 里更适合的备选：
`vfx/vfx_starry_impact`（带屏幕扭曲，最"重"）、`vfx/vfx_coin_explosion_jumbo`（筹码爆炸，跟斗地主最搭）、
`vfx/vfx_power_up`（增益上升感）。

> **✅ 已换（2026-10-08 01:18）**：改成 **`vfx/vfx_coin_explosion_jumbo`**。
>
> 而且这个坑**比"语义不搭"严重得多** —— 反编译 `NBounceSparkVfx` 后确认，那个场景**根本不能这么用**：
>
> ```csharp
> // NBounceSparkVfx._Ready()  —— 我们设的位置和缩放会被它自己覆盖
> _startPosition += new Vector2(Rng.Chaotic.NextFloat(-120f, 120f), Rng.Chaotic.NextFloat(0f, 20f));
> _floorY = _startPosition.Y + Rng.Chaotic.NextFloat(0f, 64f);
> base.GlobalPosition = _startPosition;                 // ← 覆盖我们设的 Position！
> _velocity = new Vector2(Rng.Chaotic.NextFloat(-400f, 400f), Rng.Chaotic.NextFloat(-800f, -300f));
> base.Scale = new Vector2(num, 2f - num) * Rng.Chaotic.NextFloat(0.1f, 0.8f);   // ← 覆盖 Scale！
>
> // 而且它的"起点/地面"是从**战斗房间的怪物**推的：
> // NBounceSparkVfx.Create() → NCombatRoom.Instance.GetCreatureNode(target).GetBottomOfHitbox()
> ```
>
> `_Process` 里还按 `_gravity(0,1500)` 跑抛物线、在 `_floorY` 处弹跳。
> **牌桌上 `NCombatRoom.Instance` 是 null** ⇒ 起点落到原点附近，第一帧就"落地弹跳"飞出去。
> 所以"粒子特效不知道能不能显示"的真正答案不只是坐标算错，**这个场景在非战斗环境里本来就是坏的**。
>
> 对照：`vfx_coin_explosion_jumbo` 是纯 `GPUParticles2D`，脚本 `NVfxParticleSystem._Ready()`
> 只做两件事 —— 递归把子粒子的 `Emitting` 置 `true`、`_lifetime`(=3.0s) 后 `QueueFreeSafely()`。
> **不碰位置、不碰缩放、不依赖战斗房间** ⇒ 才是能塞进 UI 的那类场景。
>
> 📌 **挑特效场景的通用判据**（新增）：打开 `.tscn` 看根节点挂的脚本 ——
> 如果 `_Ready`/`_Process` 里出现 `GlobalPosition =`、`Scale =`、`NCombatRoom`、`GetCreatureNode`，
> 就别往非战斗场景里塞。只做 `Emitting = true` + 自毁的（`NVfxParticleSystem` 系）才安全。
>
> ⚠️ 另外 `SpawnGameVfx` 原来那个 2.6s 的 `QueueFree` 会把 `lifetime=3.0` 的筹码拦腰砍掉，
> 已改成 5s（纯兜底，正常情况下粒子场景自己会自毁）。

---

## 八、实战：一个真做出来的特效 mod 是怎么搭的

> 前面的「路 A/B/C」是**可能性**。这一节是**落地答案** ——
> 拆了一个工坊上真实好用的特效 mod「万象辉星（RegentFX）」，把它的一套骨架完整还原出来了。

**两份产出：**

拆了三个互补的样本 —— **一个教"特效长什么样"，一个教"打起来爽不爽"，一个教"怎么低成本不闯祸地做出来"**：

**样本 ①「万象辉星 RegentFX」—— 美术特效层**

| 路径 | 是什么 |
|---|---|
| `notes/杀戮尖塔2-特效实战-万象辉星拆解.md` | **完整拆解**：装机清单 / pck 里的三个坑 / 代码四层结构 / `StartPos` 约定 + 数学验算 / 播放流程逐行 / 11 个 Harmony 补丁挂在哪 / 全屏后处理 / 对答案（它抄了本体什么） |
| `projects/ref-regentfx/decompiled/` | 反编译出的 **79 个 `.cs`** |

**样本 ②「动作与特效 MeleeAttack」—— 打击感/演出层**

| 路径 | 是什么 |
|---|---|
| `notes/杀戮尖塔2-特效实战2-动作与特效-打击感拆解.md` | **完整拆解**：Godot 完全导出的 pck 形态（二进制 `.scn` + `.remap` + GDExtension + Spine）/ **打击感六件套**（HitStop、敌方慢动作、冲刺拉伸、弧线位移、残影模糊、朝向翻转）/ 位移状态机 / 原值备份表 / 目标中心用 `Hitbox` / 设置页注入 |
| `projects/ref-meleeattack/decompiled/` | 反编译出的 **76 个 `.cs`** |

**样本 ③「More Ironclad Animations」—— 工程层（**零 pck！**）**

| 路径 | 是什么 |
|---|---|
| `notes/杀戮尖塔2-特效实战3-免pck与表现层工程学.md` | **完整拆解**：`Assembly.Location` 定位自己 + `Image.LoadFromFile` 免 pck 读贴图 / `RuntimeAsset`（4 字节 magic 决定 Brotli 解压）/ 着色器写成 C# 字符串（16 个）/ 高斯核 + `MultiMeshInstance2D` 免后处理辉光 / `PresentationFlow` 表现层不阻塞游戏 / 声明式粒子方案 JSON / 可选依赖 dll 运行时加载 / README 里的工程规范 |
| `projects/ref-shieldonly/decompiled/` | 反编译的 3 个 dll（主 dll 26917 行） |

> 这个样本 **`has_pck: false`** —— 贴图/JSON/曲线全是散装文件放在 dll 旁边，运行时从磁盘读。
> **不用 Godot 编辑器、不用导出 pck、改一张 PNG 直接生效。**

**可以直接抄的骨架**

| 路径 | 是什么 |
|---|---|
| `projects/sts2-vfx-template/` | `VfxUtil` / `CardFX` 注册表 / `CardVfxPlayer` / `WorldEnvironmentUtil` / `ModEntry`（来自①）<br>+ **`Juice.cs`**（来自②：`EasedProgress` / `StretchFactor` / `DashWithStretch` / `ArcMove` / `ApplyHitStop` / `TargetCenterOf` / `FlipVfxNode`）<br>+ **`LooseAsset.cs`**（来自③：免 pck 读贴图 / magic+Brotli / 内联着色器 / 可选依赖）<br>+ **`PresentationFlow.cs`**（来自③：`AlongsideAsync` / `SafelyAsync` / 代际号预热）<br>+ 两个场景模板 + `manifest.example.json` + README 速查 |

**四条最值钱的结论**（详细推导在拆解笔记里）：

1. **`StartPos` 约定** —— 特效场景按「目标在原点 (0,0)、起点放在名为 `StartPos` 的空 `Node2D`」来画；
   运行时 `FitVfx()` 一拉伸一旋转，**一个场景就能打任意距离、任意方向**，不用做朝向版本。
   （公式已验算：变换后 `StartPos` 恰好落在世界起点、原点恰好落在世界终点。）

2. **加一张卡的特效 = 一个类 + 一行特性**：
   ```csharp
   [CardFx(typeof(LunarBlast))]
   public class LunarBlastFx : CardFX { override VfxScenePath / TargetOffset / HitSfxPath ... }
   ```
   注册表靠反射自动建，预加载列表也自动收 —— **不用改任何列表**。

3. **每个 Harmony 补丁都必须先判 `LocalContext.IsMe(owner)`**。
   本体同步会把出牌广播给所有机器，不判的话队友出牌也会在你屏幕上播你的特效。
   ⚠️ **和斗地主尖塔联机里 `RestSiteSynchronizer.ChooseLocalOption` 是同一个坑。**

4. **抽别人的 pck 看不到代码**：`Scripts/*.cs` 全是 1 字节占位（Godot 不导出 C# 源码）。
   要看代码必须反编译 dll —— `~/.dotnet/tools/ilspycmd.exe -o out X.dll`。

**打击感这条线（样本②）最值钱的四条**：

5. **光有位移没有形变 = 瞬移，不是"冲"**。冲刺必须配 squash & stretch：
   X 轴按 `StretchFactor()` 拉到峰值再回落（只拉 X，Y 不变），位移用 `EasedProgress()` 分段缓动。
   两个纯数学函数，跟引擎无关，直接抄。

6. **打击停顿（HitStop）= 把 `Engine.TimeScale` 冻 0.1 秒**。
   ⚠️ 三个坑：①`TimeScale=0` 后普通 `Timer` 也停 → 计时必须用挂钟 `Time.GetTicksMsec()`；
   ②靠 `ProcessFrame` 信号轮询退出（它在 `TimeScale=0` 时照常发）；③还原写 `finally` 且要复查。

7. **瞄目标用 `Hitbox`，不用 `GlobalPosition`** —— 后者是脚底，`Hitbox.GlobalPosition + Hitbox.Size*0.5` 才是判定框中心。

8. **翻转特效前必须处理粒子**：`LocalCoords = false` 的粒子速度是世界坐标，
   父节点 `Scale.X` 翻负时粒子照样往原方向飞。要先设 `LocalCoords = true` → 翻转 → `Restart()`。

**工程层这条线（样本③）最值钱的四条**：

9. **★ 可以完全不用 pck**：`Path.GetDirectoryName(typeof(X).Assembly.Location)` 定位到自己目录，
   然后 `Image.LoadFromFile(path)` → `ImageTexture.CreateFromImage(img)` 直接从磁盘读贴图
   （`Image` 是 `IDisposable`，**必须 Dispose**）。**改一张 PNG 直接生效，不用碰 Godot 编辑器。**

10. **★ 着色器可以写成 C# 字符串**：`new Shader { Code = "shader_type canvas_item; …" }`。
    实测一个 mod 里内联了 **16 个**。**改 GLSL 直接生效，不用 `.gdshader` 文件、不用导入。**
    其中带 `hint_screen_texture` 的能读屏幕做扭曲 —— **不用 `WorldEnvironment`**。

11. **★ 表现层绝不能拖累游戏**：`PresentationFlow.AlongsideAsync(visual, original, release)` ——
    表现层和原逻辑**并行起步**、`finally` 里放行、**异常全部吞掉**。
    ⚠️ 这条最容易被忽略：我们自己的特效代码直接 `await` 一串 Godot 调用，
    中间一抛异常就会**把卡牌结算卡死**，玩家看到的是"游戏卡住了"。

12. **"4 字节 magic 决定要不要解压"**（`RuntimeAsset`）：
    头部是 `JMZ1`/`CAZ1` 就当 Brotli 解压，否则回退当明文读。
    → **开发期存明文 JSON 随便调，发布期压成 Brotli，代码一行都不用改。**

另外三个跨样本共识（**多个独立作者都踩到，说明是真坑**）：
- 本地演出类 Harmony 钩子**必须判 `LocalContext.IsMe(owner)`**（联机红线）。
- **改了属性就要备份原值**，并且要有**兜底的强制还原**（战斗结束 / 回合结束 / 换房间）——
  否则一次异常就让角色永久歪在场景里。
- **一定要有缓存 + 优雅失败**：文件找不到 / 贴图读坏 / 可选依赖没装，
  都要能降级继续跑，不能崩、不能卡。

**另外两个打包坑**（会直接导致"打包后加载不出来"）：
- 贴图在 pck 里是**两份**：`xxx.png.import`（文本，指向）+ `.godot/imported/xxx.png-<md5>.ctex`（真数据）。别只塞 png。
- 本体着色器大量 `#include "res://shaders/vfx/_util/*.gdshaderinc"`（共 7 个），
  抄进自己 mod 时**必须内联**（万象星辉就是这么干的，它的 pck 里一个 `gdshaderinc` 都没有）。

---

## 九、资源清单入口

- ⭐ **本地特效素材库（先看这个）** → `projects/sts2-vanilla-vfx/`
  - `索引-特效目录.md` —— 312 个场景的可检索目录（构成 + shader 链）
  - `scenes/vfx/`（312 个 `.tscn` 完整节点树）· `materials/`（160 个 `.tres`）· `shaders/`（100 `.gdshader` + 7 `.gdshaderinc`）
  - `README.md` —— 怎么用、最值得看的通用件、几个看源码才知道的发现
- **本体特效全表**（312 场景 / 52 Spine / 100 shader / 170 材质）→ `notes/杀戮尖塔2-本体特效资源清单.md`
- **原始全量清单**（15890 条）→ `projects/dou-spire/_work/pck-全部文件清单.txt`
- **重新扫描**：`tools/scan-pck-files.py <pck> [关键词]`（只列目录，解析 GDPC v3）
- **抽取文件内容**：`tools/extract-pck-files.py <pck> <输出目录> [关键词]`
  —— ⚠️ 关键坑：条目里的 offset 是**相对 `file_base`（头部偏移 24 处的 u32，本机 = 112）**的，
  必须 `file_base + offset` 才是真实位置，直接用 offset 会读到上一个文件的尾巴。
- **重建场景目录**：`tools/index-vanilla-vfx.py <库目录> <输出 md>`
  —— ⚠️ 解析 `[ext_resource]` 时，`id="..."` 的正则必须写 `(?:^|\s)id="..."`，
  否则会误匹配 `uid="uid://..."` 里的 `id=`（这个坑害得材质→shader 链只解开 17/153）。
- **二进制字符串扫描**：`tools/scan-game-strings.py <dll> <关键词…>`（ASCII + UTF-16LE，**`strings` 命令不在 PATH，别用**）
- **官方教程原文**：`projects/sts2-moddev-workspace/01-官方与社区教程/SlayTheSpire2ModdingTutorials/Visuals/03 - 特效与动画/`（7 节）
- **反编译源码**：`projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/`
- **社区 mod 实例库** → `projects/ref-regentfx/`
  - `decompiled/` —— 反编译出的 80 个 `.cs`（万象辉星的完整代码）
  - `RegentFX/scenes|materials|shaders/` —— 它自制的 45 场景 / 16 材质 / 16 着色器
  - **反编译别人的 mod**：`~/.dotnet/tools/ilspycmd.exe -o <输出目录> <mod>.dll`
- **社区 mod 实例库 ②** → `projects/ref-meleeattack/`
  - `decompiled/` —— 反编译出的 76 个 `.cs`（「动作与特效」，打击感/演出的完整实现）
  - `pck/` —— `.remap` / `.spatlas` / `.gdextension` / `project.binary` / 14 个二进制 `.scn`
- **社区 mod 实例库 ③** → `projects/ref-shieldonly/`
  - `decompiled/` —— 反编译的 3 个 dll（「More Ironclad Animations」，**零 pck**，免 pck 加载 + 表现层工程学）
  - 原 mod 目录值得直接看：`…/3798368044/`（`spine/` 118 个散装文件 + `blood/plans.blood` 明文粒子方案 + `checksums.manifest`）
- **可复用骨架** → `projects/sts2-vfx-template/`（`README.md` 有速查表）

---

*版本：2026-10-08 首版 · 由 WorkBuddy 接手 ZCode 未完成任务写就*
*2026-10-08 增补：第八节「实战：万象辉星拆解」+ `projects/sts2-vfx-template/` 骨架*
