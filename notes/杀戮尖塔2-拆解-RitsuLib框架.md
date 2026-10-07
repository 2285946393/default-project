# 拆解 · RitsuLib（STS2-RitsuLib）—— 塔2 mod 的地基框架

> **工坊** `3747602295` · 内部 id `STS2-RitsuLib` · **作者** OLC（GitHub `BAKAOLC`） · **版本** 0.6.6
> **协议**：**MIT**（可合法读源码、抄代码、抄架构）· **开源**：https://github.com/BAKAOLC/STS2-RitsuLib
> **官网/文档**：https://sts2-ritsulib.ritsukage.com/ · 仓库 **200 star / 20 fork**，2026-03-15 建，2026-10-06 仍在推
> **形态**：纯 dll + `assets.zip`（**无独立 pck**）· `affects_gameplay: false` · `min_game_version: 0.111.0`
> **依赖**：**零依赖**（它是最底层）
>
> **作者自述**：
> > A shared Slay the Spire 2 mod framework library providing reusable patching, persistence,
> > lifecycle, localization, and utility APIs for other mods.
>
> **一句话**：这不是一个 mod，是**一整套 mod 开发框架**。它把"给塔2写 mod"从"改字节码 +
> 到处 hack"变成了"打标注 + 订阅事件 + 写 JSON"。
>
> ⚠️ **最重要的事实**：它是**全部 119 个 mod 里被依赖最多的（26 个）**，
> 而**你自己 4 个本地 mod 全部依赖它** —— 见 §1。**这不是"可选参考"，这是你的地基。**

---

## 一、为什么必须先拆它（证据）

对本机 `2868840` 工坊 + 本地 `mods/` 全量扫描（119 份清单）后，依赖被引用次数排行：

| 框架 | 被依赖次数 | 说明 |
|---|---|---|
| **`STS2-RitsuLib`** | **26** | 第一 |
| **`BaseLib`** | **25** | 第二（作者 **Alchyr**，StS1「Downfall」作者，v3.4.7） |
| `JmcModLib` | 3 | |
| `MinionLib` | 2 | |
| `aemeath-ww` | 2 | |
| 其他（`voicemod`/`ModConfig`/`MomoLib`/`tune_strain`） | 各 1 | |

**42 个工坊 mod 的清单里出现了框架字样** —— 占全量约 **44%**。
换句话说：**你想读懂的"别人怎么写的"，将近一半是在这套框架之上写的。**

### 你自己的 mod（本地 `Slay the Spire 2/mods/`）

| mod | 版本 | 依赖（含锁版本） |
|---|---|---|
| `DouSpire` | v0.2.0 | **RitsuLib 0.6.6** + BaseLib v3.4.7 |
| `QuestSpire` | v0.2.28 | **RitsuLib 0.6.2** + BaseLib v3.4.7 |
| `FrostSpire` | v0.1.0 | **RitsuLib 0.6.5** + BaseLib v3.4.7 + MinionLib 0.6.3 |
| `BloomlessSpire` | v0.0.0 | **RitsuLib 0.6.2** |

> 四个全中。而且注意版本**不一致**（0.6.2 / 0.6.5 / 0.6.6）—— 这是升级时的隐患点。

### 版式对照：它和另外三个被拆的 mod 的关系

| | 依赖 RitsuLib？ | 怎么写的 |
|---|---|---|
| `CombatSolver`（模拟求解器） | ✅ ≥0.6.0 | 在框架上，把游戏规则复制到影子世界 |
| `wuwancients`（鸣潮先古） | ❌ 用 BaseLib | 在 BaseLib 上做先古内容 |
| `local.action_game`（类幸存者） | ❌ 零依赖 | 裸写 89 个 Harmony 补丁 |
| **`STS2-RitsuLib`** | — | **框架本身** |

> **结论**：`wuwancients` / `action_game` 的写法（裸 Harmony、手写注册、手撸 UI）
> 在框架视角下是"底层手工活"。**学 RitsuLib = 拿到别人已经踩平的捷径。**

---

## 二、★ 多版本包结构：一个 dll 同时喂多个游戏版本

这是整份库第一个值得偷的设计。目录长这样：

```
mods/STS2-RitsuLib/
├── STS2-RitsuLib.dll              36,864 B   ← 薄壳（只有引导逻辑）
├── ritsulib-variants.manifest      629 B     ← 每个程序集的 sha256 清单
├── RitsuLib.References.props      2,476 B    ← MSBuild 集成（给 mod 作者用）
├── mod_manifest.json
├── assets.zip                    522,798 B   ← 主题 + 本地化 + 图片
├── viewer/index.html                         ← 内置 API 浏览器
├── shared/                                   ← 跨版本共享，与游戏版本无关
│   ├── STS2-RitsuLib.Shared.dll    275,456 B + .xml + .pdb
│   ├── STS2-RitsuLib.Ui.dll        650,240 B + .xml + .pdb
│   └── STS2-RitsuLib.Settings.dll  804,864 B + .xml + .pdb
└── compat/
    └── 0.111.0/                              ← 按游戏 API 版本分目录
        ├── STS2-RitsuLib.Runtime.dll  6,476,288 B  ← 真正的身体
        ├── STS2-RitsuLib.Runtime.xml  4,813,451 B  ← API 文档
        ├── STS2-RitsuLib.Runtime.pdb  1,922,600 B
        ├── STS2-RitsuLib.dll             66,048 B
        └── compat-target.txt                 11 B  ← 内容就是 "0.111.0"
```

`ritsulib-variants.manifest`（schema 2）——

```json
{
  "schema": 2,
  "shared": [
    {"assembly": "STS2-RitsuLib.Shared",  "sha256": "F1AFFAD2B107CA26..."},
    {"assembly": "STS2-RitsuLib.Ui",      "sha256": "450628A164483F1F..."},
    {"assembly": "STS2-RitsuLib.Settings","sha256": "9F35FB1FC52ACFBF..."}
  ],
  "variants": [{
    "compatTarget": "0.111.0",
    "files": [
      {"assembly": "STS2-RitsuLib",         "sha256": "D59000B04E5859C9..."},
      {"assembly": "STS2-RitsuLib.Runtime", "sha256": "D0C352EF37D1C00B..."}
    ]
  }]
}
```

**设计要点**

1. **薄壳 + 分版本身体**：`STS2-RitsuLib.dll` 只有 36KB，负责选版本、加载对应 `compat/<ver>/`。
   游戏更新时**只需新增一个 `compat/0.112.0/` 目录**，老版本不动。
2. **切分维度是"变化速度"**，不是"功能模块"：
   - `shared/` = 跟游戏 API 无关的（UI 绘制、设置页、JSON、工具）
   - `compat/<ver>/` = 贴着游戏 API 的（Runtime，6.4MB）
3. **sha256 逐程序集校验**：加载前验证完整性。
4. **`compat-target.txt` 是单行纯文本**（11 字节 = `"0.111.0"`）—— 最笨但最不易解析错的方式。

### `RitsuLib.References.props` —— 让 mod 作者零配置

```xml
<RitsuLibReferenceTarget Condition="... and $([System.IO.Directory]::GetDirectories('$(RitsuLibReferenceRoot)compat').Length) == 1">
  $([System.IO.Path]::GetFileName($([System.IO.Directory]::GetDirectories('$(RitsuLibReferenceRoot)compat').GetValue(0))))
</RitsuLibReferenceTarget>
<RitsuLibReferenceTargetValid>$([System.Text.RegularExpressions.Regex]::IsMatch('$(RitsuLibReferenceTarget)', '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$'))</RitsuLibReferenceTargetValid>
```

- **自动探测** `compat/` 下**唯一**的子目录当版本号（多于一个就必须显式指定 `RitsuLibReferenceTarget`）。
- 用正则 `^(0|[1-9][0-9]*)\.…` 校验版本号格式（拒绝 `01.2.3` 这类）。
- 然后一次性加 5 个 `<Reference>`：`compat/<ver>/STS2-RitsuLib.dll`、
  `.../STS2-RitsuLib.Runtime.dll`、`shared/` 里三个。
- 还有个 `ValidateRitsuLibDirectoryReferences` Target 挂在 `ResolveAssemblyReferences` 之前，
  **缺任何一个引用程序集就 `<Error>` 中断构建**。

> **可偷**：以后我们自己的 mod 仓库也可以做 `compat/<gameVersion>/` 分层，
> 加上一个 `.props` 让引用方零配置。这套"薄壳 + 版本目录 + 自动探测 + 缺件即报错"是成熟做法。

---

## 三、公开 API 的规模与分层

手上拿到的是**手写 XML 文档**（**中英双语**），比反编译干净得多。规模：

| 程序集 | 类型 | 方法 | 属性 | 字段 | 事件 | **合计成员** |
|---|---|---|---|---|---|---|
| `STS2-RitsuLib.Runtime` | **1772** | 3880 | 2174 | 1638 | 46 | **9510** |
| `STS2-RitsuLib.Ui` | 182 | 527 | 266 | 634 | 4 | 1613 |
| `STS2-RitsuLib.Settings` | 166 | 396 | 374 | 454 | 6 | 1396 |
| `STS2-RitsuLib.Shared` | 84 | 165 | 144 | 20 | 4 | 417 |
| **合计** | **约 2204** | 4968 | 2958 | 2746 | 60 | **约 12,936** |

Runtime 的**命名空间 206 个**。规模最大的几个：

| 命名空间 | 类型数 | 是什么 |
|---|---|---|
| `Scaffolding.Content` | 140 | 内容模板与 builder |
| `Scaffolding.Content.Patches` | 106 | 把内容塞进原版列表的补丁 |
| `Models.Capabilities` | 90 | 能力（挂载在模型上的插件）系统 |
| `Combat.SecondaryResources` | 88 | **第二资源**（能量/星星之外的资源，如"血""怒""符"） |
| `Networking.Sidecar` | 80 | 联机旁路 |
| **`Interop.AutoRegistration`** | **76** | **声明式注册特性（★ 最值得学）** |
| `Audio` | 58 | FMOD 音频 |
| `Utils.HarmonyIl` | 35 | **IL 改写工具链** |
| `CardPiles` | 34 | 自定义卡堆 |
| `Lifecycle.Patches` | 30 | 生命周期事件发布 |

### 官方给的"五个入口"（作者亲口说的：大多数 mod 只需要这五个）

| 需求 | 用什么 |
|---|---|
| 注册模型 / 关键词 / Epoch / 卡堆 / 顶栏按钮 | `RitsuLibFramework.CreateContentPack(modId)` |
| Patch 游戏方法 | `RitsuLibFramework.CreatePatcher(modId, patcherName)` |
| 响应游戏时机 | `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)` |
| 存 JSON 数据 | `RitsuLibFramework.BeginModDataRegistration(modId)` + `GetDataStore(modId)` |
| 加设置界面 | `RitsuLibFramework.RegisterModSettings(modId, configure)` |

### 分层（官方说法）

- `Scaffolding.Content` —— 内容模板 / builder
- `Content` `Keywords` `CardTags` `CardPiles` `Timeline` `Unlocks` `TopBar` —— 注册器
- `Data` / `Utils.Persistence` —— 数据
- `Settings.ModSettings` / `Settings.ModSettingsUi` —— 玩家可见设置页
- `Patching` —— Harmony 封装与诊断
- `Audio` / `RuntimeInput` / `Ui` —— 运行时辅助

---

## 四、标准启动模板（官方 getting-started 逐字抄）

这是**每个 mod 的入口**都长这样：

```csharp
[ModInitializer(nameof(Initialize))]
public static class MyModEntry
{
    public const string ModId = "MyMod";
    public static Logger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        Logger = RitsuLibFramework.CreateLogger(ModId);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);   // ★ 注册程序集，否则标注不生效
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);  // 仅当有 .tscn 上的 C# 脚本

        var patcher = RitsuLibFramework.CreatePatcher(ModId, "main");
        patcher.RegisterPatches<MyModPatches>();
        RitsuLibFramework.ApplyRequiredPatcher(patcher, DisableMod);  // ★ 关键补丁失败就关掉自己
    }

    private static void DisableMod() { /* 必要补丁无法应用时，在这里关闭自己的 Mod */ }
}
```

⚠️ **`ModTypeDiscoveryHub.RegisterModAssembly(...)` 是必须的**，否则所有 `[RegisterXxx]` 标注全部不生效。
这是个很容易漏、且**静默失败**的坑。

**安装方式**（两种都给）

```xml
<PackageReference Include="STS2.RitsuLib" />
```
```json
{ "dependencies": [ { "id": "STS2-RitsuLib" } ] }   // 游戏 API 0.105.x+
```
> ⚠️ 文档明确提醒：**旧游戏 API 分支必须用旧字符串写法** `"STS2-RitsuLib"`，
> 因为旧版 manifest 解析器遇到 dependency **对象会直接报错**。
> 我们本地 4 个 mod 用的都是对象写法（版本都在 0.111 附近，没问题）。

---

## 五、补丁系统：`IPatchMethod` + IL 工具链

### 5.1 声明式补丁类（`IPatchMethod`）

```csharp
public sealed class MyCombatPatch : IPatchMethod
{
    public static string PatchId => "my_mod_combat_patch";
    public static string Description => "Adjust combat start behavior";
    public static bool IsCritical => true;          // ★ 失败是否致命

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CombatRoom), "OnEnter"),
    ];

    public static void Postfix(CombatRoom __instance) { /* ... */ }
}
```

**`PatchTarget` 工厂让意图显式**（比 `new ModPatchTarget(...)` 可读得多）：

```csharp
public static ModPatchTarget[] GetTargets() =>
[
    PatchTarget.Method<CombatRoom>("OnEnter"),
    PatchTarget.Method<SaveManager>(nameof(SaveManager.SaveRun), typeof(AbstractRoom), typeof(bool)),  // 消歧重载
    PatchTarget.Getter<Player>(nameof(Player.Piles)),
    PatchTarget.OptionalGetter<PowerModel>("PackedIconPath"),      // ★ 目标缺失也不报错
];
```

### 5.2 `ModPatcher` 的账本式管理

`Patching.Core.ModPatcher` 持有 Harmony 实例，把补丁分**静态 / 动态**两本账：

| 成员 | 作用 |
|---|---|
| `RegisteredPatchCount` / `RegisteredDynamicPatchCount` / `AppliedPatchCount` | 三个计数器 |
| `IsApplied` | "已完成且未发生严重失败" |
| `PatchAll()` | 一次性应用全部静态补丁 |
| `UnpatchAll()` | **移除失败的会保留"已应用"标记，以便下次重试**（细节做得很好） |

`ModPatchResult`（每个补丁的结果）带 `Success` / `ErrorMessage` / `Exception` / **`Ignored`**
（目标缺失且设了 `IgnoreIfMissing` 时为 true）。

> **对比**：我们拆的 `local.action_game` 是 89 个手写 Harmony 补丁、无统一账本、无 critical 分级。
> 这套"每个补丁有 ID / 描述 / 是否致命 / 结果对象"的模型**直接可以搬**。

### 5.3 `PrivateAccess` —— 访问私有成员的检查版

```csharp
private static readonly AccessTools.FieldRef<NCombatUi, CombatState> StateRef =
    PrivateAccess.FieldRef<NCombatUi, CombatState>("_state");

private static readonly Func<NCardPlay, CardModel?> GetCard =
    PrivateAccess.DeclaredGetterDelegate<NCardPlay, Func<NCardPlay, CardModel?>>("Card");

private static readonly Action<NCardPlay, bool> Cleanup =
    PrivateAccess.DeclaredMethodDelegate<NCardPlay, Action<NCardPlay, bool>>("Cleanup", typeof(bool));
```

它包了 `AccessTools`，**加了"必需成员检查"** —— 私有成员名变了会在**补丁初始化阶段**
就给出清晰错误，而不是运行时 NRE。还有 `declared` vs `inherited` 两套（对应"确定是这个类型上的"
和"允许来自基类的"）。

### 5.4 ★ `HarmonyIl` / `HarmonyIlPattern` / `HarmonyIlRewriter` —— 带断言的 IL 改写

官方态度很明确：**不要在 patch 里手写长 `CodeInstruction` 链**。改成"模式匹配 + 报告断言"：

```csharp
var rewriter = HarmonyIlRewriter.From(instructions);
var pattern = HarmonyIlPattern.Sequence(
    HarmonyIl.IsLdstr("prefix"),
    HarmonyIl.IsLdloc(),
    HarmonyIl.IsCall(concatMethod),
    HarmonyIl.IsStloc());

var report = rewriter.TryInsertAfterFirst(
    "MyPatch insert override",
    pattern,
    [ HarmonyIl.Ldarg(0), HarmonyIl.Call(overrideMethod) ],
    alreadySatisfied: code => code.Any(i => HarmonyIl.IsCallTo(i, overrideMethod)));  // ★ 别的 mod 已经改过也不报错

report.RequireSucceeded();
if (report.Applied > 0) report.RequireExactly(1);
return rewriter.InstructionsChecked("MyPatch insert override");
```

三个关键 API：

| API | 用途 |
|---|---|
| `report.RequireSucceeded()` / `RequireExactly(n)` | **断言改写确实发生了**，没发生就报错 |
| `rewriter.InstructionsChecked(...)` | 校验结构问题（branch label 缺失、反射 operand 非法） |
| `alreadySatisfied` 谓词 | **幂等**：目标已被别的 mod 改过时视为已完成，不重复插入 |
| `TryFindAfter` / `TryFindBefore` | 锚点搜索，避免"替换过宽指令区间" |

> **这是我们最该偷的一块**。我们做 dou-spire 时改原版方法，一旦游戏更新 IL 变了就是静默失效。
> 有了"断言 + 幂等 + 锚点"，**写错会当场炸**，而不是悄悄不生效。

### 5.5 Async 的专用处理

- **首选**：普通 postfix 替换 `ref Task __result` → `HarmonyAsyncTaskBridge.After(...)`。
- **必须改状态机时**：`HarmonyAsyncIl.RedirectAwaitedCalls`（最窄：只重定向**被直接 await** 的调用，
  要求栈形状与 awaitable 返回类型完全一致）或 `ReplaceAwaitedCalls`（需要多加载状态机字段时）。
- 明确说明：**不会创建新的 async state**，需要额外 await 点就改用 task-wrapper。

**还有不写 transpiler 也能查 IL 的只读层**：

```csharp
var body = onPlayMethod.GetOriginalIl();                 // async 方法默认解析到 MoveNext
var directlyCallsTarget = body.HasCall(IsTargetMethod);

var path = onPlayMethod.FindOriginalIlCallPath(
    isTarget: IsTargetMethod,
    shouldTraverse: called => called.DeclaringType == cardType);  // ★ 下钻必须显式开启，返回最短路径
```
文档诚实说明边界：**不推断虚调用分派 / 委托目标 / 反射调用 / 任意传递行为**，也不含 transpiler 输出。

---

## 六、★ 声明式自动注册：76 个 `[Register…]` 特性

这是整个框架**最省事**的部分 —— **内容写在哪，注册就写在哪**。

```csharp
[RegisterCard(typeof(MyCardPool))]
public sealed class MyStrike
    : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    public override void Use(ICombatContext ctx, ICreatureState user, ICreatureState? target)
    {
        ctx.DealDamage(user, target, Damage);
    }
}
```

配套本地化（key 由框架按规则生成）：

```json
{
  "MY_MOD_CARD_MY_STRIKE.title": "精准打击",
  "MY_MOD_CARD_MY_STRIKE.description": "造成 {Damage} 点伤害。"
}
```

### 常用特性（从 76 个里挑实用的）

| 特性 | 注册什么 |
|---|---|
| `RegisterCard(typeof(pool))` | 卡牌进卡池 |
| `RegisterRelic` / `RegisterPotion(typeof(pool))` | 遗物 / 药水进池 |
| `RegisterCharacter` | 角色模型 |
| `RegisterPower` / `RegisterOrb` | 战斗模型 / 充能球 |
| `RegisterAct` / `RegisterMonster` / `RegisterGlobalEncounter` | 章节 / 怪物 / 全局遭遇 |
| `RegisterActEncounter(typeof(act))` | 指定章节的遭遇 |
| `RegisterSharedEvent` / `RegisterActEvent(typeof(act))` | 事件 |
| **`RegisterSharedAncient` / `RegisterActAncient(typeof(act))`** | **先古之民事件**（对照 `wuwancients`） |
| `RegisterTrashHeapCard` / `RegisterTrashHeapRelic` | 「垃圾堆」事件候选 |
| `RegisterAchievement` / `RegisterEnchantment` / `RegisterAffliction` | 成就 / 附魔 / 苦痛 |
| `RegisterGoodModifier` / `RegisterBadModifier` | 每日特效（`ModifierListSortOrder` 控制列表位置） |
| `RegisterSharedCardPool` / `RegisterSharedRelicPool` / `RegisterSharedPotionPool` | 共享池 |
| `RegisterEpoch` / `RegisterStory` | 时间线纪元 / 故事 |
| `RegisterModelCapability` | 模型能力 |
| `RegisterOwnedKeyword` / `RegisterOwnedCardTag` | 自有 ID |
| `RegisterSmartFormatSource` / `RegisterSmartFormatter` | 本地化格式化扩展 |
| `RegisterArchaicToothTranscendence` | 「古老牙齿」卡牌超越映射 |
| `RegisterTouchOfOrobasRefinement` | 「欧洛巴斯之触」遗物精炼映射 |
| `RegisterDefaultModelCapability` | 给原版模型的实例加默认能力 |

### 三个容易踩的语义规则

1. **`Order`**：同一注册阶段内值越小越早。初始卡牌/遗物/药水的 `Order` 也写进 starter 条目，
   starter 列表**按 `Order` 排，再按注册顺序排**。
2. **`Inherit = true`**：基类上的标注**只有显式写 `Inherit = true` 才继承**给派生类；
   且**就近声明胜出** —— 派生类上写 `[RegisterCard(typeof(别的池))]` 是**替换**而不是叠加
   （一个逻辑槽位只能声明一次，重复声明 = 错误）。
3. **`RitsuLibOwnedBy("MyMod")`**：标注类在**辅助程序集**里（游戏无法映射到你的 manifest id）时用。

**还有装饰生命周期节点的特性**，例如 `AutoTimelineSlotAfterEpochColumnAttribute`、
`UnlockEpochAfterAscensionWinAttribute`、`RevealAscensionAfterEpochAttribute` ——
**解锁条件也是打标注**。

---

## 七、内容注册：把内容"塞进"原版列表

框架为**每一个原版列表**都准备了补丁（`Content.Patches`，106 个类型）：

| 补丁 | 塞进哪 |
|---|---|
| `AllCharactersPatch` / `AllRelicsPatch` / `AllPowersPatch` / `AllOrbsPatch` | 角色/遗物/能力/球 总表 |
| `AllPotionPoolsPatch` / `AllRelicPoolsPatch` / `AllSharedCardPoolsPatch` | 各种池 |
| **`AllAncientsPatch` / `AllSharedAncientsPatch`** | 先古之民 |
| `AllEventsPatch` / `AllSharedEventsPatch` | 事件 |
| `AllMonstersPatch` / `ActsPatch` / `ActsByIndexPatch` | 怪物 / 章节 |
| `AchievementsPatch` / `DebugEnchantmentsPatch` / `DebugAfflictionsPatch` | 成就 / 附魔 / 苦痛 |
| `GoodModifiersPatch` / `BadModifiersPatch` / `MutuallyExclusiveModifiersPatch` | 每日特效 + **互斥组** |
| `TrashHeapCardsRegistryPatch` / `TrashHeapRelicsRegistryPatch` | 垃圾堆候选 |

### 三条关键机制

1. **注册有生命周期**：`ContentRegistryPhase { Open → Frozen → Resolved }`。
   `Frozen` 之后继续注册**会抛异常**（`ContentRegistrationState` 同理）。
2. **合并策略显式化**：`ContentMergeMode { AppendDistinctById, MergeDistinctById }` ——
   原版在前还是混合、按 ID 去重，是**写死的策略**而不是隐式行为。
3. **★ `InjectDynamicRegisteredModels`**：注释说得很明白 ——
   > 游戏的子类型扫描能够发现静态模组 DLL 中的类型，
   > **但无法发现通过 Reflection.Emit 生成的占位类型**。

   所以框架**额外手动注入一遍**。这就是为什么有 `PlaceholderCardDescriptor` /
   `PlaceholderPotionDescriptor` / `PlaceholderRelicDescriptor` 这一组"占位模型"——
   存档里引用了已被卸载的 mod 的内容时，用占位物兜底而不是崩档。

### `ModelId.Entry` 命名规则（务必按它写）

```
<MODID>_<CATEGORY>_<TYPENAME>           例：MY_MOD_CARD_MY_STRIKE
```

规范化：**全大写下划线**；非字母数字分隔符合并成 `_`；驼峰拆开。
`MyMod → MY_MOD`、`com.example.my-mod → COM_EXAMPLE_MY_MOD`、`StarterRelic → STARTER_RELIC`。

> ⚠️ 文档专门警告：**不要用全大写类名**（如 `TESTCARD`）。
> **不走 RitsuLib 固定 Entry 覆写的原版路径**会把 `TESTCARD` 拆成 `T_ES_TC_AR_D`。
> 写 `TestCard`；带缩写时优先 `UrlParser` 而不是 `URLParser`。

关键词 / 卡牌标签 / 卡堆 / 顶栏按钮用同一规则，中间段固定为
`KEYWORD` / `CARDTAG` / `CARDPILE` / `TOPBARBUTTON`。

---

## 八、★ 生命周期事件总线：74 个强类型事件

顶层命名空间 `STS2RitsuLib` 下有 **74 个 `XxxEvent` 类型**。
**这是"不写 Harmony 补丁也能拿到游戏时机"的入口**：

```csharp
var subscription = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
{
    Logger.Info($"Game ready: {evt.Game.Name}");
});

// 一次性订阅：自己 dispose
RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>((evt, sub) =>
{
    PrepareForCombat(evt.RunState);
    sub.Dispose();
});
```

- **Replayable event**：事件已经发生后才来订阅，**默认立即补发**。
  只要未来事件就传 `replayCurrentState: false`。
- `CardRetainedEvent` 在新 host API 上**已过时**，改用 `CardsFlushedEvent`。

### 完整事件目录（按用途分组，抄自官方表）

| 时机 | 事件 |
|---|---|
| 框架启动 | `FrameworkInitializingEvent` `FrameworkInitializedEvent` |
| 模型设置 | `ContentRegistrationClosedEvent` `ModelRegistryInitializedEvent` `ModelIdsInitializedEvent` `ModelPreloadingCompletedEvent` |
| 游戏节点 | `GameTreeEnteredEvent` `GameReadyEvent` |
| 档位与存档 | `ProfileIdInitializedEvent` `ProfileSwitchingEvent` `ProfileSwitchedEvent` `RunSavingEvent` `RunSavedEvent` `ProgressSavingEvent` `ProgressSavedEvent` `ProfileDeletingEvent` `ProfileDeletedEvent` |
| Run 流程 | `RunStartedEvent` `RunLoadedEvent` `RunEndedEvent` `RoomEnteringEvent` `RoomEnteredEvent` `RoomExitedEvent` `ActEnteringEvent` `ActEnteredEvent` `RewardsScreenContinuingEvent` |
| 战斗 | `CombatStartingEvent` `CombatEndedEvent` `CombatVictoryEvent` `SideTurnStartingEvent` `SideTurnStartedEvent` `SideTurnEndingEvent` `SideTurnEndedEvent` `PlayerTurnStartedEvent` `ExtraTurnTakenEvent` |
| 战斗资源 | `EnergyGainedEvent` `EnergyResetEvent` `EnergySpentEvent` `StarsGainedEvent` `StarsSpentEvent` |
| 卡牌 | `CardPlayingEvent` `CardPlayedEvent` `CardDrawnEvent` `CardDiscardedEvent` `CardExhaustedEvent` `CardMovedBetweenPilesEvent` `BeforeFlushEvent` `CardsFlushedEvent` `CardAutoPlayingEvent` `CardEnteredCombatEvent` `CardGeneratedForCombatEvent` `CardRemovingEvent` `HandDrawingEvent` `HandEmptiedEvent` `ShuffledEvent` |
| 生物 / 伤害 | `AttackStartingEvent` `AttackEndedEvent` `BlockGainingEvent` `BlockGainedEvent` `BlockBrokenEvent` `BlockClearedEvent` `CurrentHpChangedEvent` `CreatureAddedToCombatEvent` `CreatureDyingEvent` `CreatureDiedEvent` `SummonedEvent` |
| 奖励与物品 | `GoldGainedEvent` `GoldLostEvent` `PotionProcuredEvent` `PotionDiscardedEvent` `PotionUsingEvent` `PotionUsedEvent` `RelicObtainedEvent` `RelicRemovedEvent` `RewardTakenEvent` `ItemPurchasedEvent` |
| 地图 / 休息 | `MapGeneratedEvent` `RestSiteHealedEvent` `RestSiteSmithedEvent` |
| 解锁 | `EpochObtainedEvent` `EpochRevealedEvent` `UnlockIncrementedEvent` |
| 其他 | `GameOverScreenCreatedEvent` |

> **可偷**：我们做特效（VFX）想挂"暴击时""格挡时""敌人死亡时"的触发点，
> 过去要自己写 transpiler。**这里已经有一份现成的 74 个时机表。**
> 把 `AttackStartingEvent` / `BlockGainedEvent` / `CreatureDiedEvent` / `CardPlayedEvent` 接到
> 我们的特效播放器上，就不需要碰原版 IL。

---

## 九、能力系统（Capabilities）：给模型挂插件

`Models.Capabilities`（90 个类型）是一套**"能力（Capability）"**机制：
不继承、不改原版类，而是**把能力对象挂到模型实例上**。

基础契约 `IModelCapability`：

| 成员 | 含义 |
|---|---|
| `CapabilityId` | 稳定 ID（用于运行时查找**和持久化**） |
| `Owner` | 当前所属模型 |

### 按用途划分的契约（挑重点）

| 契约 | 干什么 |
|---|---|
| **`IModelCapabilityHookListener`** | **接收所属模型的原版钩子回调**。文档强调：**影响多人同步的游戏逻辑应走这条路**，因为原版钩子会等模型回调完成 |
| `IModelCapabilityJsonState` | 能力状态 JSON 持久化（带 `SchemaVersion`） |
| `IModelCapabilityMergeHandler` / `CloneHandler` / `CloneNotification` | 合并 / 复制语义 |
| `IModelDynamicVarContributor` | 贡献能力**自有**的动态变量，用 `{Capabilities.Scope.Variable}` 访问 |
| `IModelHoverTipContributor` / `IModelAssetPathContributor` | 悬停提示 / 资源路径 |
| `IModelRightClickCapability` | 同步的右键交互（带 `RightClickPriority` 和执行链中断控制） |
| `ICardDescriptionContributor` / `ICardTitleContributor` | **往卡牌描述/标题里插片段** |
| `ICardEnergyCostContributor` / `ICardStarCostContributor` | 费用修正 |
| **`ICardOverlayContributor`** | **给卡牌节点加可视覆盖层**（★ 见下） |
| `ICardGlowContributor` | 手牌发光判定 |
| `ICardPlayResultContributor` / `ICardPlayStateContributor` | 出牌后目标牌堆 / 出牌状态决策 |
| `ICardTransformCarryOverCapability` | 卡牌转化时把自己带过去 |

### ★ `CardOverlayContribution` —— 和我们的特效线直接对上

```csharp
// 应且仅应设置一个创建来源：ScenePath / Scene / Factory
public class CardOverlayContribution {
    public string Id;              // 稳定 ID，用于诊断与内部排序
    public int Order;              // 数值越大越晚加入 → 显示在上层
    public bool FullRect;          // 创建后应用全矩形布局
    public string ScenePath;       // 走游戏预加载缓存的 path
    public PackedScene Scene;      // 已解析的场景
    public Func<...> Factory;      // ★ 工厂：每次必须返回新节点
}
```

文档交代的关键语义：
> 贡献项会在**游戏原版的苦痛（Affliction）及内置覆盖层处理完成后**加入，
> 因此**会叠加显示，而不会替换原版覆盖层槽位**。
> 同一个 `NCard` 可能同时被多个节点显示 → **`Factory` 每次必须返回新节点**。

> **这是"给卡牌加特效层"的官方正门**。我们之前的做法是自己 patch 卡牌节点、
> 自己管层级和生命周期。这里的 `Order` / `FullRect` / `Factory` 已经把坑踩平了。

---

## 十、★ Shell 主题：一套 W3C Design Tokens 设计系统

**对 UI 线（二游风格 UI）来说，这是整份框架里最值钱的部分。**

### 10.1 格式：W3C 设计令牌 + JSON Schema

主题是 `.theme.json` 文件，遵循 **W3C Design Tokens Community Group** 格式：

```json
{
  "$schema": "https://raw.githubusercontent.com/BAKAOLC/STS2-RitsuLib/main/schemas/ui/shell/v1/schema.json",
  "themeFormatVersion": 1,
  "themeVersion": 11,
  "id": "sakura",
  "displayName": "Sakura (Square)",
  "inherits": "default",
  "core": {
    "color": {
      "scheme": {
        "canvas":  { "$value": "#251B25", "$type": "color" },
        "panel":   { "$value": "#332632", "$type": "color" },
        "accent":  { "$value": "#F0ADC9", "$type": "color" }
      }
    }
  }
}
```

- **叶令牌** = `{ "$value", "$type", "$description"? }`
- **`inherits`**（继承）→ `sakura` 只有 142 行，因为只写**差异**，其余从 `default` 叠上来
- **`$schema` 是真的存在的**：`schemas/ui/shell/v1/schema.json`（3,737 B）在仓库里
- **`themeVersion`** 是**内容修订号**：内置主题修订更新时，会**替换磁盘上的旧副本**
  （先尽力备份）—— 也就是"内置主题可以升级，但玩家改过的还是玩家优先"

### 10.2 规模：内置 9 套主题 × 2 变体 = 18 个文件

| 主题 | 普通版 | 圆角版 |
|---|---|---|
| `default` | 2,582 行 | 266 行 |
| `sakura`（樱） | 142 行 | 266 行 |
| `amethyst`（紫晶） | 142 行 | 266 行 |
| `forest`（森） | 142 行 | 266 行 |
| `ocean`（海） | 142 行 | 266 行 |
| `oled` | 226 行 | 266 行 |
| `paper`（纸） | 142 行 | 266 行 |
| `terracotta`（陶） | 142 行 | 266 行 |
| `warm`（暖） | 266 行 | 456 行 |

> 注意结构：**只有 `default` 是完整定义（2,582 行）**，其余 8 套都靠 `inherits: "default"`
> 只写几十~两百行差异。`*-rounded` 是"把圆角换掉"的薄覆盖层。

`default` 主题里共 **526 个叶令牌**：

| 分组 | 数量 |
|---|---|
| `components.*` | **433** |
| `semantic.*` | 59 |
| `core.color.*`（`scheme` 32 + `fixed` 2） | 34 |

### 10.3 令牌命名空间（55 个 token 类型）

按维度分得极细，这套命名值得直接抄：

- **颜色**：`ColorTokens` `TextTokens` `InsetSurfaceTokens` `FramedSurfaceTokens` `EntrySurfaceTokens`
  `OverlayPanelTokens` `SidebarCardTokens` `SidebarRailTokens` `SidebarBtnTokens` `PillTokens`
  `ToggleTokens` `SliderTokens` `StepperTokens` `DropdownTokens` `DragHandleTokens`
  `ListShellTokens` `ListItemTokens` `ListEditorTokens` `PageToolbarTrayTokens`
  `TextButtonTokens` `TextButtonToneTokens` `CollapsibleTokens` `ChromeMenuTokens`
  `ChoiceCenterTokens` `StringValidationTokens`
- **度量**：`BorderWidthMetrics` `RadiusMetrics` `FontSizeMetrics` `EntryMetrics` `OverlayMetrics`
  `SidebarMetrics` `SliderMetrics` `KeybindingMetrics` `ColorRowMetrics` `StringEntryMetrics` `ChoiceMetrics`
- **原子**：`LeafToken` `BgBorder` `BoxCorners` `BoxEdges` `ShadowTokens`
- **引擎**：`RitsuShellTheme` `RitsuShellThemeBuilder` `RitsuShellThemeDocument` `RitsuShellThemeCatalog`
  `RitsuShellThemeMerger` `RitsuShellThemeRuntime` `RitsuShellThemeValueCoerce`
  `RitsuShellThemeReferenceResolver` `RitsuShellThemeLayoutResolver` `RitsuShellThemeModRegistration`
  `RitsuShellStyleCache` `RitsuShellChromeStyles` `RitsuShellPanelStyles`

### 10.4 引擎能力（从 XML 文档抽出）

| 类型 | 干什么 |
|---|---|
| `RitsuShellThemeDocument` | 文档模型：`Inherits` + `Core` + `Semantic` + `Components` + **`Scopes`** + `Extensions` |
| `RitsuShellThemeCatalog` | 加载**内置 + 磁盘 `.theme.json`**，解析继承/作用域覆盖/令牌引用 |
| `RitsuShellThemeMerger` | 主题合并（继承链叠加） |
| `RitsuShellThemeReferenceResolver` | **令牌引用解析**（令牌可以引用令牌，像 CSS 变量） |
| `RitsuShellThemeValueCoerce` | 值类型转换 |
| `RitsuShellThemeLayoutResolver` | 布局解析 |
| `RitsuShellStyleCache` | **缓存无参样式框**；每份缓存绑定一个不可变主题快照 → 主题换了旧缓存**可回收** |
| `RitsuShellChromeStyles` | 工厂方法：`CreateSurfaceStyle`（圆角面板+边框+柔和阴影）、`CreateInsetSurfaceStyle`（凹陷面板）、`CreatePageToolbarTrayStyle`、`CreateListShellStyle`、`CreateListEditorSurfaceStyle`、`CreateTooltipPanelStyle`、`CreateColorPickerSwatchFrameStyle` |

### 10.5 ★ `Scopes` —— 按作用域覆盖（这是最聪明的设计）

```
scopes: {
  "shell":        { ... },          // 全局壳层
  "modSettings":  { ... },          // 模组设置页
  "mod:<modId>":  { ... }           // ★ 单个 mod 的私有覆盖
}
```

**每个 mod 可以给自己作用域内换皮，而不影响别人。** 配合 `Extensions`（按 mod id 索引的自由数据）。

`RitsuShellThemeCatalog.EnsureLoaded()` 的注释还交代了分发策略：
> 内置主题**缺失时会提取到用户主题目录**；若内置修订较新，则**在尽力备份后替换磁盘上的旧副本**。

### 10.6 官方给的用法与态度

```csharp
RitsuToastService.ShowInfo("设置已保存。", "My Mod");
RitsuToastService.ShowWarning("可选 bank 加载失败。", "My Mod");
RitsuToastService.ShowError("必要初始化失败。", "My Mod");

// 需要后续更新/关闭同一个 toast
var toast = RitsuToastService.ShowTracked(
    RitsuToastRequest.Info("加载中...", "My Mod").Persistent());
toast.UpdateBody("加载完成。");
toast.ResetDuration(2.0d);
toast.Close();
```
> 非持久 toast 显示剩余时间进度条；持久 toast 不显示。

官方建议（值得听）：
> 大多数 Mod 直接使用当前主题即可。只有当你的设置 UI 有**多个自定义控件、需要统一视觉身份**时，才添加自定义主题。
> **Token 名称应表达用途，不要只描述颜色。**

### 10.7 还有现成的运行时 UI 能力

| 能力 | API |
|---|---|
| 运行时快捷键 | `RuntimeHotkeyService.Register("Ctrl+Shift+M", callback, id, title)` |
| 设置页里暴露快捷键 | `AddKeyBinding` / `AddMultiKeyBinding` / `AddRuntimeHotkeySummary` |
| 顶栏按钮 | `CreateContentPack("MyMod").TopBarButtonOwned("my_panel", new ModTopBarButtonSpec(IconPath, Handler)).Apply()` |
| 浮窗 / 文件对话框 / 覆盖层 | `RitsuFloatingWindow` `RitsuFileDialog` `RitsuOverlayHost` `RitsuDebugToolsDock` |

**顶栏按钮 ID 规则**：`MY_MOD_TOPBARBUTTON_MY_PANEL`，hover 文本读 `static_hover_tips`。

### 10.8 ★ 自定义卡堆：`ExtraHand` 直接复用原版交互

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .CardPileOwned("archive", new ModCardPileSpec
    {
        Style = ModCardPileUiStyle.ExtraHand,
        Anchor = ModCardPileAnchor.AtCenter(new Vector2(260f, 520f)),
        CardShouldBeVisible = true,
        ExtraHand = new ModCardPileExtraHandSpec
        {
            Direction = ModExtraHandLayoutDirection.Vertical,
            Spacing = 86f,
            CardScale = Vector2.One * 0.55f,
            HoverScale = Vector2.One,
            ShowPlayableGlow = true,
            AllowCardPlay = true,
        },
    })
    .Apply();
```

文档交代的实现细节很有价值：
> `ExtraHand` 牌堆使用**可交互的原版卡牌 holder**，支持悬停放大、关键词提示、手柄焦点、可打出发光，
> 并可选择通过原版目标选择、资源支付、hook、播放队列和结果牌堆流程手动打出。
> 默认 `VanillaHand` 布局使用与玩家手牌相同的**动态扇形、缩放、旋转、悬停抬升和邻牌让位**规则。

以及一个**关键的 hack 说明**：
> 手动打出额外手牌卡牌时，RitsuLib 会**临时把它桥接到后端原版手牌**，
> 使封闭的原版 `PlayCardAction` 接受该卡牌。取消目标选择或已排队动作时，卡牌会恢复到来源牌堆。

> **可偷**：做"额外手牌/存档区/配方区"这类 UI，不用自己写卡牌交互 —— 这套已经接好了
> `LayoutResolver`（逐卡变换）、`OnCardVisualCreated` / `OnCardArrived`（自定义展示动画）、
> `FlightTargetPositionResolver` / `FlightStartPositionResolver`（自定义飞行动画端点）。

---

## 十一、跨 mod 互操作：不编译期引用也能调别人

`Interop` 命名空间最实用的两块：

### 11.1 `ModInteropAttribute` —— 免引用调用另一个 mod

```csharp
[ModInterop(ModId = "SomeOtherMod", Type = "SomeOtherMod.SpecialApi")]
public static class OtherModBridge
{
    // 这里声明"存根"，运行时被重写成对那个 mod 的真实调用
}
```

**实现方式**（`Interop.Internal.ModInteropEmitter`）：
> 生成 **Harmony 转译补丁**，使带有互操作标记的 CLR 存根转发到另一个模组或程序集。

配套：
- `InteropTargetAttribute` —— 覆盖目标类型/成员名，支持 `GenericTypes`（最多 32 个，需带 CLR 元数如 `Namespace.Box\`1`）
- `InteropAnyParamAttribute` —— 参数作通配符匹配
- `InteropIgnoreAttribute` —— 排除成员
- `InteropClassWrapper` —— 实例成员转发到运行时对象（字段 `Value` 持有实例）

> **可偷**：我们和 BaseLib / 其他 mod 集成的代码，现在应该是硬引用。
> 用这个模式可以**把"可选依赖"做成真的可选** —— 对方不在也不崩。

### 11.2 `ModTypeDiscoveryHub` —— 加载后类型发现管线

在本地化初始化早期调用的可扩展管线。文档特别说明：
> 它与 **BaseLib 的扫描时机保持一致**，但不会将发现流程绑定到单一功能。

`LogDiagnostics()` 可以把贡献器列表和 mod→程序集映射写进日志（**排查注册不生效时先看它**）。

---

## 十二、时间线与解锁

| 类型 | 作用 |
|---|---|
| `ModTimelineRegistry` | 把模组自定义的 Epoch / Story 加进游戏时间线字典。**一个纪元 = 一个解锁槽位；一个故事 = 有序纪元归入同一进度列** |
| `ModTimelineLayoutRegistry` | 注册各 Epoch 在时间线中的**列**和列内 `EraPosition`。**游戏本体先占格位**，避免模组槽位静默重叠 |
| `ModTimelineEraIconRegistry` | 按时代注册坐标轴图标策略（**没有纹理时默认隐藏图标**） |
| `ModEpochGatedContentRegistry` | Epoch ID → 受其限制的卡牌/遗物 CLR 类型 |
| `ModStoryEpochBindings` | 每个具体 Epoch 类型收集有序的纪元类型 |

**解锁条件用特性声明**（见 §6），或继承模板：

| 模板 | 解锁什么 |
|---|---|
| `ModEpochTemplate` | 基类（带 `Era` `EraPosition` `AssetProfile` `CustomPackedPortraitPath` `CustomBigPortraitPath`） |
| `CardUnlockEpochTemplate` / `RelicUnlockEpochTemplate` / `PotionUnlockEpochTemplate` | 按 CLR 类型声明并解锁卡/遗物/药水 |
| `PackDeclaredCardUnlockEpochTemplate` / `PackDeclaredRelicUnlockEpochTemplate` | 通过内容包声明（一次注册同时给解锁操作+文本+纪元要求） |
| `ModStoryTemplate` | 故事基类（纪元顺序来自绑定，不用子类重写类型列表） |

时间线相关的补丁有个细节值得记：`NEraColumnHideEmptyIconPatch`、
`NUnlockTimelineScreenExpansionSlotSortPatch`（**先按时代再按位置排序，避免不同时代同位置冲突**）、
`NeowEpochQueueUnlocksCoExpansionScopePatch`（**把共同扩展限制在特定流程内，避免无关调用解锁/播放所有动画**）。

> 这一块对 `wuwancients` 那种"往先古加内容"是**替代方案** —— 用框架的话不用自己 patch
> `NTimelineScreen`。

---

## 十三、持久化与设置页

### 13.1 持久化（`Utils.Persistence`）

```csharp
// 强类型 JSON 封装，支持迁移 / 备份回退 / 变更通知
public class PersistentDataEntry<T> {
    public T Data;                 // 通过它修改会触发 Changed 事件
    public string FilePath;        // 解析后的 Godot 用户数据路径
    public SaveScope Scope;
    public event Action Changed;
    public void Load();            // 读 JSON（带备份回退）、应用迁移、更新 Data
    public void Save();
}
```

| 概念 | 值 |
|---|---|
| `SaveScope` | `Global`（所有档案共享）/ `Profile`（专属一个档案）/ `InMemory`（不落盘） |
| `DataLifecycleState` | `WaitingForProfile` → `Ready` |
| `ProfileManager` | 跟踪活动档案 ID，解析 `user://` 存储路径；事件 `ProfileChanged` / `ProfileDeleted` |
| `IMigration` | `FromVersion` → `ToVersion` |
| `ModDataMigrationConfig` | `CurrentDataVersion` / `MinimumSupportedDataVersion` / `SchemaVersionProperty` |
| `MigrationResult<T>` | `Success` / `Data` / `WasMigrated` / `FinalVersion` / **`RequiresRecovery`**（JSON 坏了或版本太旧 → 应隔离或重置） |
| 云同步 | `ModCloudSyncPathRegistry` / `ModCloudSyncScope` / `StorageSyncPathEnumerator` |

**四个生命周期事件**：`ProfileDataReadyEvent`（含 `IsInitialReady` / `IsProfileSwitch` / `DataReloaded`）、
`ProfileDataChangedEvent`、`ProfileDataInvalidatedEvent`。

> **要点**：**"档案（Profile）"是一等公民**。存档数据必须区分 Global / Profile，
> 而且要处理"档案被删了"的情况。这套模型比自己写 `user://` 读写可靠得多。

### 13.2 设置页

`STS2RitsuLib.Settings`（166 类型 / 1396 成员）提供完整控件库：

| 控件 | |
|---|---|
| `ModSettingsToggleControl` | 开关 |
| `ModSettingsSliderControl` / `ModSettingsFloatSliderControl` | 滑块 |
| `ModSettingsDropdownChoiceControl<T>` / `ModSettingsChoiceControl<T>` | 下拉 / 选择 |
| `ModSettingsKeyBindingControl` / `ModSettingsMultiKeyBindingControl` | 按键绑定（**多键**） |
| `ModSettingsColorControl` | 颜色选择 |
| `ModSettingsStringLineControl` / `ModSettingsStringMultilineControl` | 单行 / 多行文本（带校验） |
| `ModSettingsListControl<T>` / `ModSettingsListDropSlot<T>` / `ModSettingsListItemCard<T>` | 可拖拽列表 |
| `ModSettingsCollapsibleSection` / `ModSettingsCollapsibleHeaderButton` | 折叠区块 |
| `ModSettingsSidebarButton` / `ModSettingsGamepadCompatibleButton` | 侧栏按钮 / **手柄兼容按钮** |

**有 `ModSettingsGamepadCompatibleButton`** —— 手柄支持是内建的，这对塔2（有主机版）很重要。

入口：`RitsuLibFramework.RegisterModSettings(modId, configure)`，
或用反射提供器 `RegisterModSettingsReflectionProvider<T>()`（**属性驱动**的设置页）。
JSON Schema 在仓库 `schemas/mod-settings/runtime-interop/v1/schema.json`（20,320 B）。

---

## 十四、我们能偷什么（按优先级）

| # | 偷什么 | 用在哪 | 成本 |
|---|---|---|---|
| 1 | **74 个生命周期事件表** | **特效触发点**：`AttackStartingEvent` / `BlockGainedEvent` / `CreatureDiedEvent` / `CardPlayedEvent` → 接特效播放器，**不用碰 IL** | 极低 |
| 2 | **`HarmonyIl` 模式 + 报告断言 + `alreadySatisfied`** | dou-spire 改原版方法时**写错当场炸**、游戏更新后不静默失效 | 低 |
| 3 | **Shell 主题 token 体系**（W3C Design Tokens + `inherits` + `scopes`） | 二游风 UI：**只写差异的薄主题 + 按 mod 作用域覆盖** | 低（抄 JSON） |
| 4 | **`ICardOverlayContributor` / `CardOverlayContribution`** | 给卡牌加特效层：`Order` 排序、`FullRect`、`Factory` 每节点新实例**都是现成约定** | 低 |
| 5 | **`[Register…]` 声明式注册** | 新内容不用写注册清单，注册点贴着类 | 低 |
| 6 | **`ModInterop` 免引用调用** | 与 BaseLib/其他 mod 集成做成**真可选依赖** | 中 |
| 7 | **`compat/<gameVersion>/` 分层 + `.props` 自动探测** | 我们自己的 mod 仓库结构 | 低 |
| 8 | **`IsCritical` + `ModPatchResult` 账本** | 补丁失败可控降级、可诊断 | 低 |
| 9 | **能力（Capability）改装模型** | 不改原版类地挂行为；`IModelCapabilityHookListener` **是多人同步的正确路径** | 中 |
| 10 | **`ExtraHand` 卡堆 + `LayoutResolver` / `OnCardVisualCreated`** | 额外手牌/配方区/存档区 UI，**复用原版卡牌交互与飞行动画** | 中 |
| 11 | **持久化 Profile 模型 + `RequiresRecovery`** | 存档数据分 Global/Profile，坏档可隔离 | 中 |
| 12 | **`Placeholder*Descriptor` 占位模型** | 卸载 mod 后旧档不崩 | 低 |
| 13 | **`RitsuToastService` / `RuntimeHotkeyService` / 顶栏按钮** | 运行时反馈与入口，现成的 | 极低 |
| 14 | **`ModelId.Entry` 命名规则 + 全大写类名的坑** | **命名踩坑预警**（`TESTCARD` → `T_ES_TC_AR_D`） | 零 |

### ⚠️ 两个"必须马上检查"的点

1. **`ModTypeDiscoveryHub.RegisterModAssembly` 漏了 = 所有标注静默失效。**
   我们 4 个 mod 都在用 RitsuLib，值得逐个确认入口里有这一行。
2. **版本不一致**：`DouSpire 0.6.6` / `FrostSpire 0.6.5` / `QuestSpire 0.6.2` / `BloomlessSpire 0.6.2`。
   框架还在活跃更新（2026-10-06 还在推），**统一到最新版**能少吃跨版本的坑。

---

## 十五、取证方法与复现

### 本地取证路径

```bash
M="D:/software/steam/steamapps/common/Slay the Spire 2/mods/STS2-RitsuLib"
# mods/ 与 工坊 3747602295 内容一致
```

| 产物 | 位置 |
|---|---|
| API 表面导出（12 个命名空间） | `projects/ref-ritsulib/dumps/*.txt` |
| 全类型清单（1772 行） | `projects/ref-ritsulib/dumps/_alltypes.txt` |
| 解压后的内置主题 + 本地化 | `projects/ref-ritsulib/assets/` |
| 上游源码快照（19MB，**不进仓库**） | `projects/ref-ritsulib/upstream/STS2-RitsuLib-main/` |
| 解析脚本 | `projects/ref-ritsulib/tools/parse_xml_api.py` · `dump_ns.py` |

### 复现步骤

```bash
# 1. API 表面（比反编译干净，且有中英双语说明）
python projects/ref-ritsulib/tools/parse_xml_api.py "$M/shared/STS2-RitsuLib.Shared.xml" \
  "$M/shared/STS2-RitsuLib.Ui.xml" "$M/shared/STS2-RitsuLib.Settings.xml" \
  "$M/compat/0.111.0/STS2-RitsuLib.Runtime.xml"

# 2. 按命名空间导出成员+文档
python projects/ref-ritsulib/tools/dump_ns.py "$M/compat/0.111.0/STS2-RitsuLib.Runtime.xml" "STS2RitsuLib.Patching"

# 3. 内置主题与本地化
python -c "import zipfile; zipfile.ZipFile('$M/assets.zip').extractall('projects/ref-ritsulib/assets')"

# 4. 上游源码（MIT，可读可抄）
curl -sSL -o ritsulib.tar.gz https://codeload.github.com/BAKAOLC/STS2-RitsuLib/tar.gz/refs/heads/main
```

### 官方文档目录（29 篇，双语文档站）

`getting-started` · `framework-design` · `terminology` · `content-authoring-toolkit` ·
`content-packs-and-registries` · `character-and-unlock-scaffolding` · `card-dynamic-var-toolkit` ·
`patching-guide` · `persistence-guide` · **`secondary-resources`**（27KB，最长）·
`localization-and-keywords` · `loc-string-placeholder-resolution` · `custom-events` ·
`timeline-and-unlocks` · `fmod-and-audio` · `mod-settings` · `shell-theme` ·
**`creature-visuals-and-animation`** · `godot-scene-authoring` · `asset-profiles-and-fallbacks` ·
`diagnostics-and-compatibility` · `debug-log-viewer` · `telemetry-backend` · `update-checks` ·
`lifecycle-events` · `index`

> **还没细读但和特效线相关的三篇**：`creature-visuals-and-animation.md`、
> `card-dynamic-var-toolkit.md`、`secondary-resources.md`（第二资源——「怒」「符」「血」这类，
> 88 个类型的子系统）。以后要往特效/机制线深挖，从这三篇开始。

---

## 十六、一句话总结

**RitsuLib 把"塔2 mod 开发"这件事标准化了**：
注册变标注、时机变事件、改字节码变带断言的模式匹配、UI 变设计令牌、
存档变 Profile 作用域、跨 mod 调用变免引用存根。

**它是全工坊第一依赖（26 个），也是你自己 4 个 mod 的地基。**
拆完它以后再看其他 mod，就真的**是降维了** —— 大部分"他们怎么做到的"，
答案都会变成"调了框架的哪个 API"。
