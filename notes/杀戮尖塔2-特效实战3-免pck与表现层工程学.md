# 杀戮尖塔 2 · 特效实战 3：More Ironclad Animations —— 免 pck 与「表现层工程学」

> 拆解日期：2026-10-08
> 样本：创意工坊 `3798368044`「More Ironclad Animations」v1.9.57-offering.1，作者：page
> （内部 id 仍是 `ShieldOnly`，显示名改过）
> 简介：Ironclad cosmetic animations，Presentation only
> 拆解产物：`projects/ref-shieldonly/`（反编译 ~2.7 万行）

---

## 零、为什么这个样本最贴近我们的处境

前两个样本解决的问题：

| 样本 | 解决什么 | 代价 |
|---|---|---|
| 万象辉星 | 特效**长什么样** | 59MB pck，要完整 Godot 工程 |
| 动作与特效 | 打起来**爽不爽** | 30MB pck + Spine + GDExtension |
| **这个** | **怎么在不背资源包的前提下做出好效果** | **0 pck，584KB dll + 122 个散装文件** |

**关键事实：这个 mod 没有 pck，`has_pck: false`。**
场景、贴图、着色器、粒子方案**全部散装**放在 dll 旁边，运行时从磁盘读。

→ 这正是我们最需要的：
- **不用装 Godot 编辑器、不用导出 pck、不用等构建** —— 改一张 PNG 直接生效
- **改一行 GLSL 直接生效**（着色器是写在 C# 字符串里的）
- **调一个粒子参数只改 JSON**（不用重编译）

**代价**：要自己写资源加载、自己做缓存、自己保证"找不到文件时优雅失败"。
下面把这套机制完整拆开。

---

## 一、★ 核心技术 1：定位自己 + 从磁盘读贴图

### 1.1 怎么知道自己在哪

整份代码里出现了 20+ 次同一个表达式：

```csharp
Path.Combine(Path.GetDirectoryName(typeof(ModEntry).Assembly.Location), "spine")
```

**`typeof(自己的某个类).Assembly.Location`** → 拿到 dll 的完整路径 → 取目录 → 拼子目录。
因为 mod 的 dll 就躺在工坊目录里（`.../workshop/content/2868840/3798368044/`），
所以 `…/3798368044/spine`、`…/3798368044/blood`、`…/3798368044/offering` 全都找得到。

### 1.2 怎么把 PNG 变成 Godot 能用的贴图

```csharp
Image img = Image.LoadFromFile(Path.Combine(directory, part.GetProperty("file").GetString()));
try
{
    if (img == null || img.IsEmpty())
        throw new InvalidDataException("Card weapon texture missing.");

    return new Art((Texture2D)ImageTexture.CreateFromImage(img), transform);
}
finally
{
    ((IDisposable)img)?.Dispose();          // ★ Image 是 IDisposable，用完要 Dispose
}
```

就两步：**`Image.LoadFromFile(path)` → `ImageTexture.CreateFromImage(image)`**。
完全绕过 Godot 的 `res://` 导入系统（`.import` / `.ctex` / remap 那一整套都不需要）。

> ⚠️ `Image` 实现了 `IDisposable`，**必须 Dispose**，否则重复加载会漏显存。
> 作者在 `finally` 里 Dispose，同时把 `ImageTexture`（它不 Dispose）留下来长期用。
> 注意 `ImageTexture.CreateFromImage` 会**复制**数据到 GPU，之后 Image 就可以丢了。

### 1.3 三种资源来源，各有各的用处

| 来源 | 写法 | 适合 |
|---|---|---|
| **散装文件**（首选） | `Assembly.Location` + `Image.LoadFromFile` | 贴图、JSON、二进制曲线 —— **热改** |
| **dll 内嵌资源** | `Assembly.GetManifestResourceStream("名字.json")` | 小数据、不想散落文件 |
| **pck**（本项目没用） | `ResourceLoader.Load<PackedScene>` | 需要 Godot 编辑器做场景时 |

内嵌资源的例子（在同一份代码里）：
```csharp
using var s = typeof(DemonAccessories).Assembly.GetManifestResourceStream("BodySlamAnimation.AccessoryMeshes.json");
```

→ **我们做卡牌特效，散装文件这条就够。** 卡牌特效不需要 Godot 场景编辑器，
用代码搭 `Node2D` 树反而更可控。

### 1.4 完整性校验（可选但很专业）

包里有 `checksums.manifest`（17KB）：

```json
[
  { "path": "blood/atlas.png", "sha256": "0C1D2426A4344D99…" },
  { "path": "blood/plans.blood", "sha256": "82DE9A10EA36CC4D…" },
  { "path": "mod_manifest.json", "sha256": "EF5D419F19236E1A…" },
  …
]
```

→ 用途：**检测用户手动改过/装坏了文件**，报错时能直接说"你的 blood/atlas.png 损坏了"。
我们自己不一定要做，但**知道别人这么做**，说明这是成熟做法。

---

## 二、★ 核心技术 2：`RuntimeAsset` —— 4 字节 magic 决定要不要解压

这是我认为**最巧的一个设计**，全文只有 20 行：

```csharp
internal static class RuntimeAsset
{
    internal static Stream Open(string path, ReadOnlySpan<byte> marker, out bool compact)
    {
        var fs = File.OpenRead(path);
        try
        {
            Span<byte> head = stackalloc byte[4];
            fs.ReadExactly(head);                              // 读头 4 字节
            compact = head.SequenceEqual(marker);              // 是不是约定的 magic？
            if (compact)
                return new BrotliStream(fs, CompressionMode.Decompress);   // 是 → Brotli 解压
            fs.Position = 0L;                                  // 不是 → 回到开头，当明文读
            return fs;
        }
        catch { fs.Dispose(); throw; }
    }

    internal static JsonDocument ReadJson(string path)
        => JsonDocument.Parse(Open(path, "JMZ1"u8, out _));    // JSON 用 "JMZ1"
}
```

**同一个文件路径，两种形态，代码一行都不用改：**

| 用途 | magic | 内容 |
|---|---|---|
| 二进制曲线（`.motion`） | `CAZ1` | 头 4 字节 + Brotli 压缩的浮点数据 |
| JSON（`.spmeta` / `.json`） | `JMZ1` | 头 4 字节 + Brotli 压缩的 JSON |

**为什么这招好：**
- **开发期**：存明文 JSON —— 可读、可 diff、可手改、可直接在 git 里看变化
- **发布期**：加 4 字节 magic 再 Brotli 压一下 —— 体积小、用户改不动
- 运行时**同一个 `Open()`**，靠 magic 自动分流

> 💡 我们做特效参数表（粒子的速度/曲线/颜色）完全可以照这个来：
> 开发时明文 JSON 随便调，发布时一行命令压成 Brotli。

实测：`blood/plans.blood` 是**明文 JSON**（能直接读），
`spine/barricade-placement.motion` 是 `CAZ1` + Brotli（`file` 命令认不出来）。

---

## 三、★ 核心技术 3：着色器写成 C# 字符串（16 个）

不用 `.gdshader` 文件，运行时建：

```csharp
new ShaderMaterial
{
    Shader = new Shader
    {
        Code = "shader_type canvas_item;\nrender_mode unshaded;\n"
             + "varying vec4 vertex_tint;\n"
             + "void vertex() { vertex_tint = COLOR; }\n"
             + "void fragment() { COLOR = vec4(vec3(1.0), texture(TEXTURE, UV).a * vertex_tint.a); }"
    }
}
```

全文 `grep -c 'Code = "shader_type'` = **16 个**。摘几个有代表性的：

| 着色器特点 | 用途 |
|---|---|
| `render_mode unshaded; ... COLOR = vec4(0.0);` | **纯黑遮罩**（做剪影/挖洞） |
| `uniform sampler2D screen_texture : hint_screen_texture` | **屏幕空间扭曲**（读已渲染的画面） |
| `render_mode unshaded, blend_add;` | **加色混合**（发光、火焰） |
| `uniform float inner_radius=.25; varying vec2 ellipse_point;` | **椭圆遮罩**（法阵/光环） |
| `uniform vec4 foot_rect; uniform vec2 foot_texture_size;` | **脚下阴影 / 立足点对齐** |
| `uniform bool arm; uniform vec4 root_gate;` | **按骨骼部位分档**（手臂 vs 身体） |
| `uniform float heat=0.0;` | **热变形**（可动画的强度参数） |
| `uniform float floor_y=3000.0; uniform vec3 release_plane;` | **地面/释放面裁切**（让特效"落地"） |

**为什么值得学：**
1. **着色器变成热改的**：改字符串 → 重编译 dll → 生效。不用跑 Godot 导入。
2. **参数化程度高**：`heat`、`root_gate`、`floor_y` 这类 uniform 让**同一个着色器覆盖很多场景**。
3. **`hint_screen_texture`** —— 有了它就能做屏幕扭曲而**不需要 `WorldEnvironment`**。
   我们之前手册 §C3 讲的"全屏后处理"是另一条路；这条更轻。

> ⚠️ 反编译出来的字符串里 `\n` 和 `\r\n` 混用（原作者明显是从不同地方粘的），
> 抄的时候统一用 `\n` 就行，GLSL 两种都吃。

---

## 四、★ 核心技术 4：不用后处理的辉光 = 高斯核 + MultiMesh

我们手册 §三-C1 讲"挂一个万能滤镜"，§C3 讲"全屏后处理"。
**这个 mod 走了第三条路：把辉光做成"一堆高斯加权的自身副本叠加"。**

### 4.1 高斯核是算出来的，不是贴图

```csharp
internal static List<(Vector2 Offset, float Alpha)> Kernel(double sigma, double step, double strength)
{
    var pts = new List<(double X, double Y, double W)>();
    int n = (int)Math.Floor(3.0 * sigma / step);        // 半径 = 3σ（覆盖 99.7%）
    for (int i = -n; i <= n; i++)
    for (int j = -n; j <= n; j++)
    {
        double dx = j * step, dy = i * step;
        if (Math.Sqrt(dx * dx + dy * dy) > 3.0 * sigma) continue;
        pts.Add((dx, dy, Math.Exp(-(dx * dx + dy * dy) / (2.0 * sigma * sigma))));   // ★ 高斯
    }
    double sum = pts.Sum(p => p.W);                      // 归一化
    return pts.Select(p => (
        new Vector2((float)(p.X * 2.88), (float)(-p.Y * 2.88)),          // ★ Y 取负（屏幕 Y 向下）
        1f - (float)Math.Exp(-strength * p.W / sum)                      // ★ 饱和 alpha
    )).ToList();
}

// 两套核，一前一后
private static readonly List<(Vector2 Offset, float Alpha)> BackKernel  = Kernel(2.7, 1.5, 3.2);
private static readonly List<(Vector2 Offset, float Alpha)> FrontKernel = Kernel(10.0, 5.0, 0.95);
```

**两个非显而易见的点：**

1. **alpha 用 `1 - exp(-strength · w)` 而不是直接用 `w`。**
   因为要叠几十层半透明副本 —— 如果每层 alpha 都是 `w`（中心可能只有 0.1），
   叠 50 层也就勉强看得见；用 `1-exp(-k·w)` 会让**中心饱和到接近 1**、外围快速衰减，
   叠出来的形状才有"中间亮、边缘化开"的辉光感。

2. **`2.88` 这个系数**：把核的偏移量放大到像素单位（`step=1.5` 时，`1.5 × 2.88 ≈ 4.3px` 一格）。
   作者调的，不用深究，抄就是了。

### 4.2 用 `MultiMeshInstance2D` 一次画完

如果每层副本建一个 `Sprite2D`，几十层就是几十个节点 —— draw call 爆炸。
作者的做法是**一个 `MultiMeshInstance2D` 装下所有副本**（MultiMesh = 一次 draw call 画 N 个实例）：

```csharp
internal sealed class Source
{
    internal float[]   Basis   = Array.Empty<float>();
    internal Vector2[] Uvs     = Array.Empty<Vector2>();
    internal int[]     Indices = Array.Empty<int>();
    internal readonly ArrayMesh Mesh = new();
    internal readonly List<MultiMeshInstance2D> Layers = new();
}
```

配合的着色器就是最简单的那个：**只取原图的 alpha，颜色刷白**（辉光的颜色靠 `Modulate` 给）：

```glsl
shader_type canvas_item;
render_mode unshaded;
varying vec4 vertex_tint;
void vertex() { vertex_tint = COLOR; }
void fragment() { COLOR = vec4(vec3(1.0), texture(TEXTURE, UV).a * vertex_tint.a); }
```

→ **这一招完全不需要 `WorldEnvironment`、不需要屏幕纹理、不影响别的 UI。**
代价是"辉光只跟着这个物体的形状走"（这正是它想要的：法阵辉光贴合法阵）。

### 4.3 什么时候该用哪条路

| 想要 | 用哪条 |
|---|---|
| 整个屏幕发光（大招爆发） | `WorldEnvironment` 的 `glow_intensity`（手册 §C3） |
| **某个物体自己发光**（法阵、武器、护盾） | **高斯核 + MultiMesh**（本节） |
| 屏幕扭曲/涟漪 | `hint_screen_texture` 着色器（免 `WorldEnvironment`） |

---

## 五、★ 核心技术 5：「表现层工程学」—— 绝不能让特效拖累游戏

这个 mod 反复强调 "Presentation only"，代码里有个专门的类干这件事：

```csharp
internal static class PresentationFlow
{
    /// 让"表现层"和"原游戏逻辑"并行跑，游戏不被表现层拖住
    internal static async Task AlongsideAsync(Func<Task> visual, Func<Task> original,
                                             Action release, Action<Exception> failed)
    {
        Task presentation = SafelyAsync(visual, failed);   // ★ 先启动，但不 await
        try
        {
            await original();          // ★ 原逻辑先走完（游戏推进不受影响）
            await presentation;        //   再让视觉收尾
        }
        finally
        {
            release();                 // ★ 一定会放行
            await presentation;
        }
    }

    /// ★ 表现层炸了绝不能往上冒 —— 只记日志
    internal static async Task SafelyAsync(Func<Task> visual, Action<Exception> failed)
    {
        try { await visual(); }
        catch (Exception e) { failed(e); }
    }

    internal static async Task AroundAsync(Func<Task> before, Func<Task> original,
                                           Func<Task> after, Action<Exception> failed)
    {
        await SafelyAsync(before, failed);
        await original();
        await SafelyAsync(after, failed);
    }
}
```

**三个必须记住的点：**

1. **`SafelyAsync` 兜住所有异常** —— 特效崩了游戏必须照常。
   我们自己的特效代码经常直接 `await` 一堆 Godot 调用，一旦中间抛异常就会**卡住卡牌结算**。
   这是最该抄的一条。
2. **`AlongsideAsync` 让表现层"并行起步、最后收尾"** —— 不是"先播完再继续"。
   卡牌该结算就结算，特效自己播自己的。
3. **`release()` 在 `finally`** —— 无论成功失败都会放行，不会把游戏锁死。

### 配套：按需预热 + 可取消

```csharp
internal static class PresentationPreloader
{
    private static CancellationTokenSource? _pending;
    private static int _generation;                       // ★ 代际号
    internal static bool Ready { get; private set; }
    internal static Task Completion { get; private set; } = Task.CompletedTask;

    internal static void Prepare(RunState? run)
    {
        // 只在"单人 + 铁甲角色"时才预热 —— 别的角色不浪费内存
        if (PresentationScope.IsSingleplayer && run != null && run.Players.Count == 1
            && run.Players[0].Character.GetType().Name == "Ironclad"
            && !Ready && _pending == null)
        {
            Completion = WarmupAsync(_pending = new CancellationTokenSource(), _generation);
        }
    }
}
```

**`_generation` 代际号**是用来处理"预热到一半，玩家又干别的了" ——
旧的任务醒来发现自己的代际号过期了就直接退出，不污染新状态。
比单纯的 `CancellationToken` 多一层保险。

---

## 六、复用本体自己的 atlas 区域（不用新画角色）

`spine/*.spmeta` 是一份 **JSON**：

```json
{
  "regions": [
    { "name": "r0", "originalAtlas": "res://animations/characters/ironclad/ironclad.atlas",
      "originalRegion": "shadow", "foot": false },
    { "name": "r1", "originalAtlas": "res://animations/characters/ironclad/ironclad.atlas",
      "originalRegion": "bottom upper arm", "foot": false },
    { "name": "r5", "originalAtlas": "res://animations/characters/ironclad/ironclad.atlas",
      "originalRegion": "sword blade", "foot": false },
    …
  ]
}
```

**含义**：把自定义区域名（`r0`、`r1`…）**映射到本体铁甲角色 atlas 里的具名区域**
（`"shadow"`、`"sword blade"`、`"bottom upper arm"`…）。

代码这边读 `.atlas` 文本、解析出区域矩形：

```csharp
internal sealed record Region(string TexturePath, Rect2I Bounds, int Rotation, Vector2I TextureSize);
…
NativeTextureAtlas.Region region = _atlas.Regions["fixed-shadow"];
NativeTextureAtlas atlas = new NativeTextureAtlas("demon-eyes", rootElement);
NativeTextureAtlas.Region region = atlas.Regions["eye glow"];
```

→ **不用画一张新的角色图，就能重新组合本体的角色零件。**
（这就是"给铁甲加武器/配件"的做法：从本体 atlas 里把剑身、手臂那些区域抠出来重新摆。）

> ⚠️ 这招依赖本体内部的 atlas 区域名（`"sword blade"` 这种字符串）——
> **游戏更新改了 atlas 名字就会崩**。作者显然知道，所以写了兼容性检查
> （代码里有 `user://ShieldOnly/compatibility/inspect.spatlas` 这种诊断输出）。
> **我们自己用这招要加 try/catch + 找不到就跳过。**

---

## 七、声明式粒子方案（`.blood`）

`blood/plans.blood` 是**明文 JSON**，把"血滴飞回能量球"这个效果描述成数据：

```json
{
  "schema": 1,
  "speed": 1.25,
  "attackReferenceFlight": 0.25,
  "profiles": {
    "bloodletting": [
      {
        "streams": 2,
        "masses": [
          { "id": 0, "type": "mass", "start": 0, "duration": 0.4379, "route": 0,
            "offset": 17.4359, "lift": -9.1039, "size": 47.3406, "tile": 20,
            "spin": 4.9719, "burst": { "x": 55.6187, "y": 31.6440 }, "phase": 0.6890 },
          { "id": 1, "type": "mass", "start": 0.022, "duration": 0.4488, "route": 1, … }
        ]
      }
    ]
  }
}
```

反编译出的对应记录：

```csharp
internal sealed record Particle(
    int Id, string Type, int Route,
    float Start, float Duration,
    float Offset, float Lift, float Size,
    int Tile, float Spin, Vector2 Burst, float Phase);
```

**逐字段含义：**

| 字段 | 意思 |
|---|---|
| `Route` | 走哪条曲线（`Curve Path(origin, target, route, attack, unit)`） |
| `Start` / `Duration` | 什么时候出发、飞多久 |
| `Offset` / `Lift` | 相对路径的横向偏移 / 垂直抬升 |
| `Size` | 大小 |
| `Tile` | 用 atlas 里的第几格贴图 |
| `Spin` | 自旋 |
| `Burst` | 出发瞬间的初速度（爆发感） |
| `Phase` | 动画相位（让每滴血不是同一个姿势） |

求值：
```csharp
BloodFlowMotion.Position(item, age, in _paths[item.Route], Attack, SpatialScale);
```

→ **这是"手写一份粒子方案"，不是程序随机。**
好处：**效果可控、可复现、可逐帧调**。
坏处：**要手调参数**（那些 0.437928975536488 一看就是工具调出来的）。

> 💡 **对我们的启发**：卡牌特效"飞过去打中"这件事，
> 与其在代码里写死 `position = lerp(a, b, t)`，不如做成 JSON 方案表。
> 3 条血流 vs 2 条血流就是**数据差异**，不是代码分支。

---

## 八、可选依赖：运行时加载同级 dll

这个 mod 把设置界面拆成可选的独立 dll（`ShieldOnly.BaseLib.dll` / `ShieldOnly.Settings.dll`），
运行时按需加载：

```csharp
Assembly assembly = typeof(ModEntry).Assembly;
string path = Path.Combine(Path.GetDirectoryName(assembly.Location),
                           text == "BaseLib" ? "ShieldOnly.BaseLib.dll" : "ShieldOnly.Settings.dll");

AssemblyLoadContext.GetLoadContext(assembly)
    .LoadFromAssemblyPath(path)
    .GetType("ShieldOnly.BaseLibSettingsMenu", throwOnError: true)
    .GetMethod("Install", BindingFlags.Static | BindingFlags.Public)
    .Invoke(null, null);
```

三个要点：
1. **`AssemblyLoadContext.GetLoadContext(自己的assembly).LoadFromAssemblyPath(...)`** —— 加载同级 dll 的标准写法。
2. **版本守卫**：加载前检查自己的 `mod.manifest.version` 够不够（`new Version(3,4,7)` / `(1,9,0)`）。
3. **整段包 try/catch** —— 加载失败就 `Console.Error.WriteLine` 并保留原设置，**不崩**。
   （设置界面加载不出来，但动画效果照常 —— 这个降级策略很对。）

→ 我们如果做"可选依赖别的 mod（比如 RitsuLib 设置库）"，就该这么写。

---

## 九、README 里的工程规范（这才是最值钱的部分）

作者的 README（7.3KB）写得不像是 mod 说明，像是**发布说明 + 验收报告**。
摘几条：

- **逐像素回归**：「新旧两版游戏的必要回归通过，**各 162 对原生画面逐像素一致**」
- **性能优化有对标的基线**：「与 blood.1 的原生渲染**逐像素一致**，保留全部粒子、路径、透明度、层级和时间」
- **参数一律写明来源**：`sigil.json` 里带 `"source": "offering/compare-v1 revision 05; user accepted parameters 2026-10-04"`
- **版本继承关系写清楚**：「基于 v1.9.50-multiplayer.1 保留已确认的多人范围与原有动作规则」
- **明确划出"只在单人生效"的范围**：列出了哪些效果联机可见、哪些只有自己看得见
- **明确列出"未验证项"**：
  > 「受控原生场景与握手检查**不能替代真人 Steam 双端实战**；两账号实际联网与断线重连**尚未验证**。Android 兼容性**尚未实测**。」
- **明确"不改玩法"**：「原人物动作、卡牌数值、等待、目标、费用、抽牌与消耗均不改；**不新增网络消息**」

### 值得我们对齐的 5 条

1. **做"纯表现层"就要能证明不改玩法** —— 明确列出"不改什么"，比说"只改视觉"可信。
2. **参数改动要留出处** —— 谁在什么时候确认的，写进文件里（`sigil.json` 的 `source` 字段）。
3. **"我验证了什么"和"我没验证什么"分开写** —— 主动写"未验证项"。
4. **性能优化要有可对标的基线**（逐像素对比）—— 不然"优化了"没法证明。
5. **多人的可见性范围要逐条列** —— 哪些联机可见、哪些只有本机。

---

## 十、我们能直接抄什么

| 优先级 | 抄什么 | 在哪 |
|---|---|---|
| ⭐⭐⭐ | **免 pck 读贴图**：`Image.LoadFromFile` + `ImageTexture.CreateFromImage`（记得 Dispose） | `ShieldOnly.cs:5272` |
| ⭐⭐⭐ | **`RuntimeAsset`**：4 字节 magic 决定 Brotli 解压 | `ShieldOnly.cs:19798` |
| ⭐⭐⭐ | **`PresentationFlow`**：`SafelyAsync` + `AlongsideAsync` + `finally release()` | `ShieldOnly.cs` 内 `PresentationFlow` |
| ⭐⭐⭐ | **着色器写成 C# 字符串** | 全文 16 处 `Code = "shader_type…"` |
| ⭐⭐ | **高斯核辉光**：`Kernel()` + `MultiMeshInstance2D` | `SecondWindGlow` |
| ⭐⭐ | **声明式粒子方案**（JSON 描述 route/start/duration/offset…） | `BloodFlowMotion` + `plans.blood` |
| ⭐⭐ | **可选依赖 dll 运行时加载** + 版本守卫 + 降级 | `ShieldOnly.cs:19380` |
| ⭐ | **`checksums.manifest`** 完整性自检 | 包根目录 |
| ⭐ | **`Assembly.Location`** 定位自己目录 | 全文 20+ 处 |
| ⚠️ | 复用本体 spine atlas 区域 | 依赖本体内部位名，**要加 try/catch** |

---

## 十一、三个样本拼起来 = 完整的三层

```
  ① 万象辉星（美术层）        ② 动作与特效（演出层）        ③ More Ironclad（工程层）
  ├ StartPos + FitVfx        ├ HitStop 打击停顿             ├ 免 pck：磁盘读 PNG
  ├ [CardFx] 注册表          ├ 冲刺拉伸形变                 ├ RuntimeAsset：magic+Brotli
  ├ 私有场景缓存             ├ 弧线位移                     ├ 着色器写成 C# 字符串
  ├ 全屏后处理               ├ 残影/模糊/推镜/黑边           ├ 高斯核辉光（免后处理）
  └ 帧动画 + 着色器          └ 朝向翻转 + 粒子修正           └ PresentationFlow 不阻塞游戏
                                                            └ 声明式粒子方案 JSON
```

**我们现在缺的就是 ③** —— 前两个样本讲了"怎么做好看"，
③ 讲的是**"怎么低成本、可迭代、不闯祸地把效果做出来"**。

---

## 十二、待验证 / 存疑

| 项 | 说明 |
|---|---|
| `.motion` 的 `CAZ1` 内部结构 | 确认是 magic + Brotli，但解压后的浮点布局没解 |
| `NativeTextureAtlas` 完整实现 | 只确认了 `Region(TexturePath, Bounds, Rotation, TextureSize)` 和 `.Regions[name]` |
| `2.88` 系数的含义 | 经验值，推测是核偏移到像素的缩放 |
| `MultiMeshInstance2D` 的组装细节 | `Source` 类的 Basis/Uvs/Indices 填充逻辑没逐行读 |
| 全部 16 个内联着色器源码 | 只看了前缀和 uniform 名，没读全文 |
| `README` 说的"162 对逐像素一致"怎么做的 | 应该是截图对比工具，未见 |

---

## 附：拆解产物

| 路径 | 内容 |
|---|---|
| `projects/ref-shieldonly/decompiled/ShieldOnly/ShieldOnly.decompiled.cs` | 反编译主 dll（26917 行） |
| `projects/ref-shieldonly/decompiled/ShieldOnly.BaseLib/` | 可选设置菜单 dll |
| `projects/ref-shieldonly/decompiled/ShieldOnly.Settings/` | 可选设置 dll |
| 原 mod | `D:\software\steam\steamapps\workshop\content\2868840\3798368044\`（无 pck，122 个散装文件） |
