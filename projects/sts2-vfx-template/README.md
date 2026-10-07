# 尖塔 2 特效骨架模板（从万象辉星反编译提取）

> 来源：反编译 `万象辉星[RegentFX] v0.5.1`（工坊 `3747497501`）的 `RegentFX.dll`
> 用途：往我们自己的 mod 里加卡牌特效时，**直接抄这套骨架，不要再从零想**
> 详细拆解见 `notes/杀戮尖塔2-特效实战-万象辉星拆解.md`
>
> ⚠️ **这是一个参考骨架，没有编译环境、没有验证过能直接 build。**
> 用途是"照着抄结构"，抄的时候按你项目的 namespace / Logger / 配置库改。

---

## 一、这套东西解决什么问题

我们之前做特效「乏力」，缺的不是 shader 知识，是**骨架**：

| 老问题 | 这套骨架的答案 |
|---|---|
| 特效场景怎么摆？坐标从哪算？ | **场景按「目标在原点、起点在 StartPos」画**，代码负责拉伸对准 |
| 怎么让它从出牌者射向目标？ | `FitVfx()` + `GlobalPosition = 目标` |
| 打牌时特效该挂在哪个时机？ | Harmony 挂 4 个函数，见下 |
| 打包成 pck 后为什么加载不出来？ | 见第五节「打包」 |
| 加第 20 张卡的特效要改多少代码？ | **一个类 + 一行特性**，别的都不用动 |

---

## 二、文件清单

```
src/
  ── 美术层 ─────────────────────────────────────────────
  VfxUtil.cs              ★ 工具箱：私有缓存 / 生成节点 / ⟨FitVfx 对准⟩ / 安全等待
  CardFx.cs               ★ 卡牌特效基类 + [CardFx(typeof(卡))] 注册表
  CardVfxPlayer.cs        ★ 一次完整播放流程
  WorldEnvironmentUtil.cs   全屏后处理（闪白/泛光/亮度/对比/饱和）

  ── 演出层 ─────────────────────────────────────────────
  Juice.cs                ★★ 打击感：冲刺拉伸 / 弧线位移 / 打击停顿 / 目标中心 / 朝向翻转

  ── 工程层 ─────────────────────────────────────────────
  LooseAsset.cs           ★★ 免 pck：磁盘读 PNG / magic+Brotli / 内联着色器 / 可选依赖
  PresentationFlow.cs     ★★ 表现层不阻塞游戏：吞异常 / 并行跑 / 代际号预热

  ── 入口与配置 ─────────────────────────────────────────
  ModEntry.cs             入口：装补丁 + 注册脚本 + 预加载
  Setting.cs              开关（含"玩家可关闪屏"）
  Sfx.cs                  音效门面（薄封装，可选）

scenes/
  模板-指向性特效.tscn     带 StartPos，用来做激光/导弹/冲击波
  模板-定点特效.tscn       不带 StartPos，用来做爆点/光环

manifest.example.json     模组清单样板
```

**三个来源（三层互补）**：

| 层 | 来源 mod | 回答什么问题 |
|---|---|---|
| 美术层 | 「万象辉星 RegentFX」 | 特效**长什么样** |
| 演出层 | 「动作与特效 MeleeAttack」 | 打起来**爽不爽** |
| 工程层 | 「More Ironclad Animations」 | 怎么**低成本、可迭代、不闯祸**地做出来 |

---

## 三、三行上手

**① 抄 src/ 进你的项目**，改 `namespace YourMod.Scripts;` → 你的命名空间。

**② 入口调两个方法**（`ModEntry.Init()` 里已经有了）：

```csharp
new Harmony("sts2.你的mod名").PatchAll();
ScriptManagerBridge.LookupScriptsInAssembly(typeof(ModEntry).Assembly);   // ★ 漏了会加载出空壳场景
```

**③ 加一张卡的特效**：

```csharp
[CardFx(typeof(LunarBlast))]                       // ← 游戏里的卡类型
public class LunarBlastFx : CardFX
{
    public override string  VfxScenePath => "res://YourMod/scenes/lunar_laser.tscn";
    public override Vector2 TargetOffset => new(100f, -450f);   // 相对出牌者的偏移
    public override string? HitSfxPath   => "event:/YourMod/sfx/hit";
    public override bool    UseV2Patch   => true;               // 想接管攻击指令就得开
    public override bool    HasExposureEffect => true;          // 要闪屏吗
}
```

然后在打牌时机里调：

```csharp
var fx = CardFX.FromCard(card);
if (fx != null) await CardVfxPlayer.PlayTargeted(fx, owner, target);
```

---

## 四、核心：`StartPos` 约定（唯一必须记住的规则）

### 画场景

```
DirectionalVfx (Node2D)              ← 根，原点 (0,0) 代表「目标」
├── StartPos (Node2D)  position = (-400, 0)   ← ★ 名字必须叫 StartPos，空节点
└── Beam / AnimatedSprite2D / GPUParticles2D  ← 你的美术
```

**一个场景打任意距离、任意方向** —— 代码会拉伸旋转。

### 代码怎么对准

```csharp
node.FitVfx(startMarker.GlobalPosition, Vector2.Zero,  // 场景内：起点、原点
            startPos, targetPos);                      // 世界上：出招者、目标
node.GlobalPosition = targetPos;
```

内部就两行：

```csharp
Vector2 a = sceneStart - sceneEnd;   // 场景内向量 = 特效"原长"
Vector2 b = worldStart - worldEnd;   // 世界向量   = 实际距离
node.Rotation = b.Angle() - a.Angle();               // 差角
node.Scale    = Vector2.One * (b.Length() / a.Length());   // 缩放比
// 结果：StartPos 落在 worldStart，原点落在 worldEnd（首尾吻合）
```

**只做定点特效（爆点/光环）就不要 StartPos**，直接 `PlaySimple(path, position)`。

---

## 五、时机：Harmony 挂哪几个函数

按需要挑，不用全上：

| 你想干的事 | 挂这里 |
|---|---|
| 开始拖牌时给点反馈 | `NPlayerHand.StartCardPlay` (Postfix) |
| 取消出牌时收掉 | `NCardPlay.CancelPlayCard` (Postfix) |
| 攻击指令执行前播特效 | `AttackCommand.Execute` (Prefix) |
| 一张牌彻底打完做清理 | `CardModel.OnPlayWrapper` (Postfix) |
| 掐掉本体攻击动画 | `NRegentVfx.Attack` (Prefix → return false) |
| 掐掉本体攻击音效 | `SfxCmd.Play` (Prefix → return false) |
| 改层级 | `NCreature._Ready` / `NCombatRoom._Ready` (Postfix) |

### ⚠️ 联机红线

```csharp
if (!LocalContext.IsMe(cardOwner)) return true;   // ★ 只处理「我自己」的出牌
```

本体的同步机制会把出牌动作**广播给所有机器**，每台机器都会跑一遍。
不判 `IsMe` 的话，队友出牌也会在你屏幕上播你的特效，还会污染你的本地状态。

> 这跟斗地主尖塔联机里的 `RestSiteSynchronizer.ChooseLocalOption` 是同一个坑。

### 层级（ZIndex）

```
BgContainer          -20    背景
CombatVfxContainer     0    战斗特效层（特效默认挂这儿）
NCreature             10    角色（压住特效）
BackCombatVfxContainer      垫在角色后面的特效挂这儿
```

---

## 六、打包（踩过的坑都在这儿）

### ① pck 里的 `.cs` 是 1 字节占位

抽别人的 pck 会看到 `Scripts/*.cs` 全是 1 字节（内容是一个 `\n`）。
**这不是解包器坏了** —— Godot 导出 C# 项目时不导出源码（编译进 dll 了），
但为了 `res://Scripts/X.cs` 这个路径还能解析（场景引用 + 类名索引），塞个占位文件。

→ **想知道别人代码怎么写的，必须反编译 dll**：`ilspycmd -o out X.dll`

### ② 贴图是「两份」，不是一份

```
res://YourMod/frames/fn0001.png.import      ← ~200B 文本，里面写着 path= 指向↓
res://.godot/imported/fn0001.png-<md5>.ctex ← 真数据
```

场景里写的还是 `res://YourMod/frames/fn0001.png`（不带后缀）。
**别只把小 png 塞进 pck** —— 要么让 Godot 自己导出（两份都生成），
要么手工组包时两份都造、`uid` 要对上。

### ③ 本体着色器不能整份照抄 —— `_util` 依赖

本体 `shaders/vfx/common/vfx_flipbook_shader.gdshader` 开头有：

```glsl
#include "res://shaders/vfx/_util/flipbook.gdshaderinc"
#include "res://shaders/vfx/_util/hue_shift.gdshaderinc"
#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"
```

共 7 个 `_util/*.gdshaderinc`。抄进自己 mod 时：

- ✅ **内联**（万象辉星的做法，稳妥）
- ❌ 把自己的 `.gdshaderinc` 也打进 pck —— `#include` 路径会跟本体撞名，风险高

---

## 六点五、打击感（`Juice.cs`）—— 特效漂亮 ≠ 打起来爽

拆自「动作与特效」这个 mod。**特效"乏力"往往不是美术不够，是缺这几样：**

| 想要 | 用哪个 | 一句话原理 |
|---|---|---|
| 冲刺"快"的感觉 | `Juice.EasedProgress` + `StretchFactor` | 前 62.5% 时间走 20% 路程；X 轴像橡皮筋拉长再弹回 |
| 冲过去 | `Juice.DashWithStretch` | 位移 + 形变一起做（**光位移就是瞬移，不是"冲"**） |
| 跳过去的重量感 | `Juice.ArcMove` | 走抛物线，弧高 = 距离 × 0.3 |
| 命中那一下"顿" | `Juice.ApplyHitStop(0.1f)` | 把 `Engine.TimeScale` 冻 0.1 秒 |
| 打准位置 | `Juice.TargetCenterOf` | 用 **`Hitbox`** 中心，不是 `GlobalPosition`（那是脚底） |
| 特效跟着人翻转 | `Juice.FlipVfxNode` | **先给粒子设 `LocalCoords`，再翻转，再重播** |
| 异步动画不卡死 | `Juice.WaitTweenOrTimeout` | tween 超时保护，返回 false 就别再动节点了 |

### ⚠️ 打击停顿（HitStop）的三个坑

```csharp
Engine.TimeScale = 0.0;    // 冻住
```

1. **`TimeScale = 0` 之后普通 `Timer` 也停** —— 所以计时必须用**挂钟** `Time.GetTicksMsec()`。
2. **`ProcessFrame` 信号在 `TimeScale = 0` 时照常发** —— 靠它逐帧轮询退出条件。
3. **还原写在 `finally`，还原后还要复查一次** —— 别的地方（暂停菜单等）可能在这期间动过。

还有一条使用建议：加个"一次攻击只顿一下"的闸，否则多段攻击会连冻三下，观感很糟。

### ⚠️ 朝向翻转的坑

`LocalCoords = false` 的粒子，速度是世界坐标的。你把父节点 `Scale.X` 翻成负的，
**粒子照样往原方向飞** → 看起来就是"反了"。必须：先切局部坐标 → 翻转 → `Restart()` 重播。

---

## 六点八、工程层（`LooseAsset` / `PresentationFlow`）—— 不闯祸才算做完

### 免 pck 装资源（`LooseAsset.cs`）

**可以完全不用 pck** —— 贴图/JSON 散装放在 dll 旁边，运行时从磁盘读：

```csharp
// 1) 定位自己：dll 就躺在 mod 目录里
var dir = Path.GetDirectoryName(typeof(LooseAsset).Assembly.Location);

// 2) 读贴图：绕过 Godot 的 res:// 导入系统（不需要 .import / .ctex / remap）
var tex = LooseAsset.TryLoadTexture("vfx/laser.png");
//   内部就是：Image.LoadFromFile(path) → ImageTexture.CreateFromImage(img)
//   ⚠️ Image 是 IDisposable，必须 Dispose（ImageTexture 不用）
```

好处：**不用装 Godot 编辑器、不用导出 pck、改一张 PNG 直接生效。**

### `RuntimeAsset`：4 字节 magic 决定要不要 Brotli

```csharp
LooseAsset.Open(path, "JMZ1"u8, out bool compact);   // 头 4 字节 == magic → Brotli 解压
                                                     // 否则 → 回退当明文读
```

**同一个路径两种形态，代码一行都不用改**：开发期存明文 JSON（可读可 diff），
发布期加 4 字节 magic 再 Brotli 压一下。用 `LooseAsset.Compact()` 转换。

### 着色器写成 C# 字符串

```csharp
var mat = new ShaderMaterial { Shader = LooseAsset.InlineShader(LooseAsset.ShaderWhiteFromAlpha) };
```

`LooseAsset` 里预置了三个常用的：全白取 alpha（辉光/闪白）、纯黑剪影、屏幕扭曲
（后者用 `hint_screen_texture`，**不需要 `WorldEnvironment`**）。

### 表现层绝不能拖累游戏（`PresentationFlow.cs`）

```csharp
await PresentationFlow.AlongsideAsync(
    visual:   async () => await PlayMyFancyVfx(),   // 特效
    original: async () => await card.Resolve(),     // 原逻辑
    release:  () => ReleaseLock());                 // 一定会执行
```

三条规矩：
1. **`SafelyAsync` 吞掉表现层的所有异常** —— 特效崩了游戏必须照常。这是最关键的一条。
2. **并行起步、最后收尾** —— 不是"先播完再继续"，游戏该结算就结算。
3. **`release()` 写在 `finally`** —— 无论成败都放行，不会把游戏锁死。

`PresentationPreloader` 用**代际号 + CancellationToken** 做"按需预热"：
预热跑到一半玩家换角色了，旧任务醒来发现代际号对不上就自己退出。

---

## 七、能直接抄的本体素材

`projects/sts2-vanilla-vfx/` 里有本体全套（312 场景 / 100 着色器 / 160 材质）。
**通用件直接用**（本体自己也天天在用，万象辉星就抄了）：

| 件 | 用途 |
|---|---|
| `vfx_flipbook_shader` | 图集逐帧播放（本体最主要的特效手法） |
| `vfx_grayscale_particle_shader` | 灰度贴图 + LUT 上色 → 一张图出多种颜色 |
| `vfx_poof_shader` | 消散 |
| `vfx_ring_polar_shader` | 环形/极坐标 |
| `dissolve.gdshader` / `hsv.gdshader` | 溶解 / 变色 |

真正要自己花时间的只有"这张卡长什么样"的美术。

---

## 八、速查

```
取节点      VfxUtil.GenVfx(path)        走私有缓存，别用本体缓存
对准        node.FitVfx(StartPos局部, Zero, 世界起点, 世界终点) + GlobalPosition = 世界终点
挂载        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(node)
销毁        VfxUtil.ClearAfter(node, 秒)  ★ 必须，否则堆场上
等          VfxUtil.Wait(秒)             自带快进模式/战斗结束保护
注册        [CardFx(typeof(卡))] + CardFX.FromCard(card)
预加载      ModEntry.LoadScenes() 反射自动收，加卡不用改列表
闪屏        WorldEnvironmentUtil.TweenExposure(...)  用 try/finally 弹回 1f
联机        if (!LocalContext.IsMe(owner)) return true;

打击感      Juice.StretchFactor / EasedProgress       冲刺的"快"
            Juice.ApplyHitStop(0.1f)                  命中那一下"顿"
            Juice.DashWithStretch / ArcMove           冲过去
            Juice.TargetCenterOf(node)                瞄 Hitbox 中心，不是脚底
            Juice.FlipVfxNode(node)                   翻转（先处理粒子！）
            Juice.WaitTweenOrTimeout(t, node, 秒)     tween 超时保护

免 pck      LooseAsset.TryLoadTexture("a.png")        磁盘读贴图
            LooseAsset.TryReadJson("plan.json")       magic+Brotli 自适应
            LooseAsset.InlineShader(glsl)             着色器写字符串里
            LooseAsset.TryLoadOptional(dll, type, …)  可选依赖，失败降级

不闯祸      PresentationFlow.AlongsideAsync(v, orig, release)   特效不拖游戏
            PresentationFlow.SafelyAsync(视觉)                 吞掉异常
            PresentationPreloader.Prepare(cond, warmup)        代际号预热
```
