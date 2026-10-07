# 拆解 · BaseLib —— 塔2 mod 的"内容层"标准库

> **工坊** `3737335127` · 内部 id `BaseLib` · **作者** **Alchyr**（StS1「Downfall」作者）
> **版本** v3.4.7（仓库 master 同为 v3.4.7）· **协议**：**MIT**
> **仓库**：https://github.com/Alchyr/BaseLib-StS2 —— **451★ / 91 fork**，建 2026-03-05，**2026-10-07 仍在推**
> **官方 Wiki**：https://alchyr.github.io/BaseLib-Wiki/（仓库 `Alchyr/BaseLib-Wiki`，28 篇文档）
> **形态**：dll **1,132,032 B** + pck 132,888 B · `min_game_version 0.107.1` · `affects_gameplay: false` · **零依赖**
>
> ⚠️ **它的目录是嵌套的**：`workshop/.../3737335127/BaseLib/`，
> 清单在 `BaseLib/BaseLib.json` 而不是包根目录 —— 这就是为什么我第一轮扫描把它漏了。
>
> **一句话**：如果说 **RitsuLib 是"框架层"**（改时机、改注册、改 UI、改存档），
> 那 **BaseLib 是"内容层"** —— 它把"往游戏里加东西"这件事标准化了：
> **每个内容类型给你一个 `CustomXxxModel` 基类，继承它，剩下的自动搞定。**
>
> **取证**：`projects/ref-baselib/`（**不反编译**：MIT 开源 + 28 篇官方文档）。
> 源码快照在 `upstream/`（**已 gitignore**），官方文档留在 `wiki/docs/`。

---

## 一、它在生态里的位置

| | 依赖它的 mod 数 | 定位 |
|---|---|---|
| `STS2-RitsuLib` | **26** | 框架层：注册 / 时机 / UI / 存档 / 主题 |
| **`BaseLib`** | **25** | **内容层：`Custom*Model` 基类 + SpireField + 资源系统** |
| 其余（JmcModLib 等） | 3 / 2 / 各 1 | |

两个框架合计覆盖 **42 个 mod（约 44%）**。**两个都拆完了。**

### 和已拆的三个 mod 的实际关系（现在能串起来了）

| mod | 用什么 |
|---|---|
| `wuwancients`（鸣潮先古） | **`CustomAncientModel`** ← **BaseLib 的**。我早先记的 `MakeOptionPools` / `IsValidForAct` / `ShouldForceSpawn` / `AllPossibleOptions` **全是 BaseLib 的 API，不是作者自己发明的**（见 §7） |
| `CombatSolver`（战斗路线求解器） | RitsuLib ≥0.6.0 |
| `local.action_game`（类幸存者） | **零依赖** —— 89 个裸 Harmony 补丁，等于把 BaseLib/RitsuLib 里的活手工干了一遍 |
| 本机 `DouSpire` / `QuestSpire` / `FrostSpire` | **两个框架都依赖** |

> 所以"读别人的 mod"这件事，**先认框架，再读内容** —— 大部分看起来玄乎的写法，
> 其实是在覆盖某个基类的虚方法。

---

## 二、形态与规模

| 指标 | 值 |
|---|---|
| dll | 1,132,032 B |
| pck | 132,888 B（很小 → 只有少量场景/图片） |
| 源码 | **268 个 `.cs` / 40,112 行**（仓库 master） |
| pck 里有什么 | `BaseLib/scenes` 4 个 + `BaseLibScenes/Acts` 3 个（内置章节用） |
| 依赖 | **零**（它自己是最底层） |

### 源码分布（40,112 行）

| 目录 | 行数 | 文件 | 是什么 |
|---|---|---|---|
| `Patches/` | **11,501** | 75 | 往原版各处打补丁（最大的一块） |
| `Utils/` | 9,523 | 53 | SpireField / IL 补丁 / 音频 / 节点工厂 / 存档 |
| `Abstracts/` | 7,183 | 43 | **★ 43 个 `Custom*Model` 基类** |
| `Config/` | 4,474 | 20 | 声明式配置系统 + UI |
| `Hooks/` | 1,568 | 12 | 给外部 mod 实现的钩子接口 |
| `BaseLibScenes/` | 1,549 | 9 | 内置场景脚本 |
| `Extensions/` | 1,457 | 31 | **31 个扩展方法文件（声明式 API 的门面）** |
| `Common/` | 719 | 6 | |
| `Cards/` | 577 | 10 | 自定义动态变量 |
| 其余（Audio/Commands/Monsters/Diagnostics/Console） | ~1,300 | | |

**注意 `Extensions/` 有 31 个文件** —— 这是 BaseLib 的"手感"来源：
大量扩展方法把原版 API 缩短、串起来。`TypeExtensions.cs`(7.9KB) / `CardExtensions.cs`(4.8KB) /
`DynamicVarExtensions.cs` / `HarmonyExtensions.cs` / `TypePrefix.cs` 等。

---

## 三、★ 支柱一：`Custom*Model` 基类（43 个）

`Abstracts/` 里 43 个文件，**每个内容类型一个基类**：

| 基类 | 对应内容 | 大小 |
|---|---|---|
| **`CustomResource.cs`** | **自定义资源（第二资源）** | **55,639 B（全库最大）** |
| `CustomCharacterModel.cs` | 角色 | 24,436 B |
| `CardModifier.cs` | 卡牌修饰 | 20,774 B |
| `ConstructedCardModel.cs` | 程序化构造卡牌 | 16,521 B |
| `CustomActModel.cs` | 章节 | 14,993 B |
| `CustomBadge.cs` | 徽章 | 13,062 B |
| `CustomCardModel.cs` | 卡牌 | 12,388 B |
| `CustomCharacterSelectEntry.cs` | 角色选择项 | 9,666 B |
| **`CustomAncientModel.cs`** | **先古之民** | 7,393 B |
| `CustomEventModel.cs` / `CustomEncounterModel.cs` | 事件 / 遭遇 | ~6.7 KB |
| `CustomMonsterModel.cs` / `CustomModifierModel.cs` / `CustomTemporaryPowerModel.cs` / `CustomMessage.cs` / `CustomReward.cs` / `CustomPile.cs` / `CustomOrbModel.cs` / `CustomPotionModel.cs` / `CustomRelicModel.cs` / `CustomPowerModel.cs` / `CustomPetModel.cs` / `CustomRestSiteOption.cs` / `CustomSingletonModel.cs` / `CustomTargetedMessage.cs` / `CustomEnchantmentModel.cs` / 各 `Custom*PoolModel` / `PlaceholderCharacterModel.cs` | … | 3.4 KB 以下 |

配套**接口**（不是基类，是按需实现的能力）：
`ICustomModel` · `ICustomPower` · `ICustomEnergyIconPool` · `ICustomTypeTextCard` ·
`IHasSecondAmount` · `ITomeCard` · `ITranscendenceCard` · `ITrashHeapCard` / `ITrashHeapRelic` ·
`ILocalizationProvider` · `ISceneConversions` · `IAutoRegisterFormatSpecifier` · `CardModifier`

### ★★ 最有借鉴价值的写法：基类 + 同文件自带的 Harmony 补丁

`CustomAncientModel.cs` 里除了基类，**同文件还有 4 个 `[HarmonyPatch]` 类**：

```csharp
[HarmonyPatch(typeof(EventModel), "BackgroundScenePath", MethodType.Getter)]
class BackgroundScenePath
{
    [HarmonyPrefix]
    static bool Custom(AncientEventModel __instance, ref string? __result)
    {
        if (__instance is not CustomAncientModel custom)
            return true;               // ★ 不是我管的对象 → 原样放行，完全不动原版行为

        __result = custom.CustomScenePath;
        return __result == null;       // ★ 我返回 null 也让原版跑
    }
}
```

**这个 `is not CustomXxxModel → return true` 的守卫是整套 BaseLib 的核心安全策略：**

1. **原版行为零风险** —— 非自定义实例原样走原版逻辑。
2. **补丁贴在基类旁边**，读基类就知道它补了哪些原版 getter（不用去别处找）。
3. **`return __result == null`** 的二次判断让"我没提供自定义值"时继续回落原版 ——
   于是**子类只覆盖需要的部分**，其余全走原版。

> **可偷指数 ★★★**：我们自己写"扩展某个原版行为"时，最怕污染原版。
> 这套"守卫 + 回落"两段式判断值得直接抄成模板。

---

## 四、自动注册：构造函数 + 字典 + `ModelDb.InitIds`

BaseLib 的注册**不写注册清单、不打标注**（那是 RitsuLib 的风格），
而是**在构造函数里自己登记**：

```csharp
public abstract class CustomAncientModel : AncientEventModel, ICustomModel, ILocalizationProvider
{
    public CustomAncientModel(bool autoAdd = true, bool logDialogueLoad = false)
    {
        if (autoAdd) CustomContentDictionary.AddAncient(this);   // ★ 构造即注册
        _logDialogueLoad = logDialogueLoad;
    }
    ...
}
```

接收端 `Patches/Content/ContentPatches.cs` 里的 `CustomContentDictionary`：

```csharp
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.InitIds))]     // ★ 挂在 ModelDb.InitIds
public static class CustomContentDictionary
{
    public static readonly HashSet<Type> RegisteredTypes = [];   // 去重
    public static readonly List<CustomCharacterModel> CustomCharacters = [];
    public static readonly List<CustomAncientModel> CustomAncients = [];
    public static readonly List<CustomEventModel> ActCustomEvents = [];      // 绑定 Act 的事件
    public static readonly List<CustomEventModel> SharedCustomEvents = [];   // 不绑 Act 的事件
    ...
    public static bool RegisterType(Type t) => RegisteredTypes.Add(t);       // 幂等

    public static void AddModel(Type modelType)
    {
        if (!RegisterType(modelType)) return;
        var poolAttribute = modelType.GetCustomAttribute<PoolAttribute>()
            ?? throw new Exception($"Model {modelType.FullName} must be marked with a PoolAttribute...");
        if (!IsValidPool(modelType, poolAttribute.PoolType))
            throw new Exception($"Model ... is assigned to incorrect type of pool ...");
        ModHelper.AddModelToPool(poolAttribute.PoolType, modelType);
    }
}
```

**要点**

| 机制 | 说明 |
|---|---|
| **构造函数自注册** | 零清单。但副作用是：**必须有人构造它**（靠游戏/框架扫描或懒加载） |
| **`RegisteredTypes` 去重** | 同一个类型构造多次也只注册一次（幂等） |
| **`PoolAttribute` 是硬要求** | 卡/遗物/药水必须标 `[PoolAttribute(typeof(某池))]`，缺了**直接抛异常**（不是静默失败） |
| **池类型校验** | `CardPoolModel→CardModel`、`RelicPoolModel→RelicModel`、`PotionPoolModel→PotionModel`，装错池也抛异常 |
| **事件按 `Acts.Length` 分流** | `Acts` 为空 → 共享事件；非空 → 章节事件。**同一个类写不同数组就换归属** |
| **`InsertSorted`** | 各列表都是"插入即排序"，顺序可控 |
| **挂载点 `ModelDb.InitIds`** | 在原版冻结 ID 之前把内容塞进去 |

### 还有一个白送的功能：**自动 ID 前缀**

官方 Features 列表明确写着：
> **Automatic ID prefixing of models that inherit `ICustomModel` (or any `CustomModel` class)**

只要继承 `Custom*Model`，ID 就自动带 mod 前缀 —— **不会和别人撞名**。

### 命名与本地化约定

```
本地化 key：<MODPREFIX>_<CATEGORY>_<TYPENAME>.title / .description
配置标题：  <MODNAME>.mod_title
配置项：    <MODNAME>-<PROPERTY_NAME>.title
静态悬停提示：<MODPREFIX>-HOVERTIP_NAME   →  static_hover_tips.json
关键词：    <MODPREFIX>-KEYWORD_NAME      →  card_keywords.json
```

---

## 五、★ 支柱二：`SpireField` —— 给原版类"加字段"

**这是 BaseLib 最有名的功能**（StS1 时代就有，玩法一模一样）。

文档原话：
> SpireField is a simple way for mods to **effectively add a new field to a preexisting class** in the game.

```csharp
public static readonly SpireField<CardModel, int> MyField = new(() => 0);   // 默认值 0

// 用方法
int val = MyField.Get(cardModel);
MyField.Set(cardModel, 5);

// 或者索引器（更顺）
int val = MyField[cardModel];
MyField[cardModel] = 5;
```

**实现**：`Utils/SpireField.cs` —— 包了一个 `ConditionalWeakTable<TKey, TValue>`：

```csharp
public class SpireField<TKey, TVal> : ICloneableField where TKey : class
{
    private readonly ConditionalWeakTable<TKey, object?> _table = [];
    private readonly Func<TKey, TVal?> _defaultVal;
    ...
}
```

> 文档很诚实地说明：`ConditionalWeakTable` 直接用也行，SpireField 只是**加了默认值等便利**。
> 关键收益是 **`ConditionalWeakTable` 是弱引用表** —— 对象被 GC 时字段自动消失，**不会泄漏**。

### `CopyOnClone` —— 模型复制时带上字段

```csharp
public SpireField<TKey, TVal> CopyOnClone(Action<TKey, TKey, TVal?>? cloneVal = null)
```
限制：**只有挂在 `AbstractModel` 派生类型上的才能开**（否则抛异常）。
浅拷贝（引用类型直接赋过去）。实现方式是 `ICloneableField` 接口里**自带一个补丁**：

```csharp
public interface ICloneableField
{
    private static NotNullSpireField<AbstractModel, HashSet<ICloneableField>> CloneFields = new(() => []);

    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
    private static class CloneSpireFields {
        [HarmonyPostfix]
        static void ModifyResult(AbstractModel __instance, AbstractModel __result) { ... }
    }
}
```
→ 补丁**挂在接口的嵌套类里**，实现这个接口就自动生效。（又一个"声明与实现放一起"的例子。）

### `SavedSpireField` —— 字段进存档

```csharp
public class BlahCard : CustomCardModel
{
    public static readonly SavedSpireField<BlahCard, int> SpecialValue =
        new(() => 100, "my_mod_special_value");        // ★ 必须给个 mod 唯一的存档名
}
```

**支持挂载的对象类型（就这 7 种）**：
`CardModel` · `RelicModel` · `PotionModel` · `EnchantmentModel` · `Player` · `Reward` · `IRunState`

**免注册就能存的值类型**：
`int` · `bool` · `string` · `int[]` · `ModelId` · `SerializableCard` · `SerializableCard[]` · `List<SerializableCard>`

**其他类型要手动注册**：

```csharp
// 简单类型
ExtendedSaveTypes.RegisterAdditionalSaveType<MyType>(...);
// 列表 / 字典 / 对象
ExtendedSaveTypes.RegisterListSaveType<...>(...);
ExtendedSaveTypes.RegisterDictionarySaveType<...>(...);
ExtendedSaveTypes.RegisterObjectSaveType<BlahCard.TestSaveType>(
    ExtendedSaveTypes.PropertyFunc<BlahCard.TestSaveType, string>(nameof(BlahCard.TestSaveType.Value)));
```

也可以用 `IPacketSerializable` 自己写 `Serialize` / `Deserialize`。

### 用法建议（官方给的，很有价值）

| 需求 | 用哪个键 |
|---|---|
| **每个玩家、每场战斗**的数据（像机器人的充能球） | **`SpireField<PlayerCombatState, ?>`** |
| 每个玩家**持久**的数据 | `SavedSpireField<Player, ?>` |

> 官方解释为什么不选别的：
> `CharacterModel` 在**同角色多人**时不唯一；`Player` 虽唯一但要**自己在战斗间清数据**；
> 用 `PlayerCombatState` 就**自动随战斗重置**。
> **这个"键选哪个"的判断是踩过坑才有的**，直接照抄。

### `AddedNode` —— SpireField 的场景版

```csharp
public class AddedNode<TParentType, TNode> : ReadonlySpireField<TParentType, TNode>, IAddedNodes<TParentType>
    where TParentType : Node where TNode : Node
```
（`Utils/SpireField.cs` 第 303 行）

**往原版已有的场景里加节点** —— 不用改 `.tscn`，不用 patch 场景加载。
文档：`wiki/docs/scenes/add-nodes.md`（3.5KB）。
配套还有 `GeneratedNodePool`（用代码生成的节点进 NodePool，`docs/utilities/pooling.md`）。

> **对我们的线直接相关**：想往战斗 UI 上加东西（血条预测、额外角标、自己的面板），
> `AddedNode` 是官方路径。

---

## 六、★ `[CustomEnum]` —— 运行时给 enum 加值

C# 里**编译好的 enum 无法在运行时加成员**。BaseLib 的做法是：
**在运行时生成一个新值，赋给一个静态字段。**

```csharp
[CustomEnum]
public static CardTag MyNewTag;               // 生成一个新 CardTag 值

[CustomEnum]
public static StaticHoverTip Mechanic;        // 生成一个新 StaticHoverTip 值
```

**引用方式很反直觉但很重要**：

```csharp
CardTag.MyNewTag      // ❌ 错
MyClassName.MyNewTag  // ✅ 对（用定义它的类名，而不是 enum 类型名）
```

约束：**必须 `public static`**（`readonly` 可能设置失败）；可以定义在任何类里。

### 对特定 enum 有额外支持

| 类型 | 额外能力 |
|---|---|
| `StaticHoverTip` | 本地化：`static_hover_tips.json` 里加 `<MODPREFIX>-HOVERTIP_NAME.title/.description` |
| `CardKeyword` | 本地化放 `card_keywords.json`，key 为 `<MODPREFIX>-KEYWORD_NAME` |
| `CardPile` | 配合 `CustomPile` 基类能做出可用的牌堆（官方标注**尚未完全测试**） |

**关键词有个自动注入的开关**（很实用）：

```csharp
[CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
public static CardKeyword Keyword;
```
> 原版对自带关键词是**自动加进卡牌文本**的（所以卡面描述里不写 `Exhaust`/`Sly`）。
> 加这个 attribute，你的自定义关键词也能自动出现。

`[CustomEnum("别的名字")]` 可以让 ID 与变量名不同。

### 官方给的一个"概念澄清"（避免走错路）

> StS2 的 `CardKeyword` **不等同于** StS1 的关键词。
> StS1 的关键词涵盖所有"会弹提示的高亮文字"；**StS2 的 `CardKeyword` 只用于
> "影响卡牌行为、且没有关联数字"的单个词**。
> 所以 `Sly` 是 `CardKeyword`，但 **`Summon` 不是**。
> 需要"带数字的词" → 去看 **动态变量提示**（`docs/localization/var-loc.md`）。

---

## 七、`CustomAncientModel` 详解 + 与 `wuwancients` 的对应

**先古是 BaseLib 里封装最重的一个**（官方原话："contains more supporting code than most other custom models"）。

### 7.1 三池选项

```csharp
protected abstract OptionPools MakeOptionPools { get; }

// 用自己的 MakePool / AncientOption 组池
protected override OptionPools MakeOptionPools => new(
    MakePool(
        AncientOption<Astrolabe>(),
        AncientOption<Astrolabe>(),
        AncientOption<SeaGlass>(relicPrep: (glass) => { ... })   // 需要预处理的遗物
    ),
    MakePool(
        AncientOption<Astrolabe>(weight: 100),                   // ★ 权重控制出现率
        AncientOption<Astrolabe>(weight: 10),
        AncientOption<TheBoot>(weight: 1)
    ),
    MakePool(
        AncientOption<Astrolabe>(),
        AncientOption<Astrolabe>(),
        AncientOption<Astrolabe>()
    ));
```

**池数语义（官方写得很明确）**：

| 池数 | 行为 |
|---|---|
| **1 个池** | 三个选项**全从这个池随机抽** |
| **2 个池** | 前两个选项用池 1，**第三个**用池 2 |
| **3 个池** | 每个选项**各自一个池** |

> 原版先古大多是"分池"的，**目的是防止某些相似选项同时出现**。

`AncientOption<T>(weight, relicPrep, makeAllVariants)`：
- `weight` —— 出现权重
- `relicPrep: (relic) => relic` —— 生成前预处理（例子里是"给 SeaGlass 随机指定另一个角色"）
- `makeAllVariants` —— 一个遗物生成多个变体

### 7.2 生成条件

```csharp
public virtual bool IsValidForAct(ActModel act) => true;                                  // 默认 2/3 章都行
public virtual bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false;
```

**官方规则 + 警告（逐字值得记）**：

| 规则 | 说明 |
|---|---|
| **默认不能在第 1 章出现** | 唯一改法是覆盖 `ShouldForceSpawn` 并**只在该局被选中的是涅奥时**返回 true |
| 大多数先古**只能出现在第 2 或第 3 章之一** | `Darv` 是唯一例外 |
| 想限定章节 | `public override bool IsValidForAct(ActModel act) => act.ActNumber() == 2;` |
| ⚠️ **`ShouldForceSpawn` 别乱用** | 原文："Messing with this can cause mod conflicts, please only use if it is 100% necessary" |
| ⚠️ 用了 `ShouldForceSpawn` | **`IsValidForAct` 就应该返回 false** |

### 7.3 遗物生成条件用 `AddCustomAncientSpawnCondition`

在遗物构造函数里调 `this.AddCustomAncientSpawnCondition(...)`。
官方解释为什么不用原版的 `IsAllowed`：
> `IsAllowed` **收不到任何上下文**，而且**原版自己没用/没实现它**（意图不明）。
> 所有检查应该基于传进来的 ancient 事件；要判断玩家就查事件的 `Owner`。

### 7.4 克隆时要清缓存（细节）

```csharp
protected override void AfterCloned()
{
    base.AfterCloned();
    _optionPools = null;      // ★ 避免共享字段引用
}
```

### 7.5 ★ 这解释了 `wuwancients` 的写法

我在 `notes/杀戮尖塔2-拆解-鸣潮先古wuwancients.md` 里记过 `Augusta` 的代码：
colors / `CustomMapIconPath` / `CustomScenePath` / `MakeOptionPools`（10 个遗物选 3）/
`AllPossibleOptions` / `IsValidForAct → ActNumber(act)==3` / `ShouldForceSpawn`。

**现在可以确认：这些全是 `CustomAncientModel` 提供的虚方法**，
`wuwancients` 只是**继承了它然后逐个覆盖**。它的 9 个角色先古 = 9 个 `CustomAncientModel` 子类。

### 7.6 对话按约定自动加载

```csharp
protected override AncientDialogueSet DefineDialogues()
{
    var baseKey = AncientDialogueUtil.BaseLocKey(Id.Entry, character.Id.Entry);
    characterDialogues[character.Id.Entry] = AncientDialogueUtil.GetDialoguesForKey("ancients", baseKey, log);
    // 另外还有 "ANY" 作为与角色无关的通用对话
}
```

本地化 key 约定：
```
{Entry}.title                                            （必需）
{Entry}.epithet                                          （必需）
{Entry}.talk.firstvisitEver.0-0.ancient                  （必需）
<baseKey>  →  与每个角色的对话
<ANY baseKey>  →  通用对话（官方建议至少有一条可重复的，否则会出现"没有任何对话可选"）
```
**缺 key 不会崩**（BaseLib 会把缺失的本地化记 error 而不是崩溃 —— 见 §11）。

---

## 八、补丁体系

### 8.1 `TryPatchAll` —— 扫描程序集自动全打

`BaseLibMain.Initialize()`：

```csharp
public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, ...);

internal static Harmony MainHarmony
{
    get { field ??= new Harmony(ModId); return field; }
}

public static void Initialize()
{
    Libgcc();                                   // ★ Linux 下手动 dlopen libgcc，否则 Harmony 挂
    IsMainThread = true;
    Godot.OS.AddLogger(new LogListener());
    try { NodeFactory.Init(); } catch (Exception e) { Logger.Error(e.ToString()); }

    var assembly = Assembly.GetExecutingAssembly();
    Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);   // 注册 C# 脚本

    ModConfigRegistry.Register(ModId, new BaseLibConfig());

    try {
        ExtendedSavePatches.Patch(MainHarmony);          // 存档扩展（必须优先）
        TheBigPatchToCardPileCmdAdd.Patch(MainHarmony);  // CardPileCmd.Add（那个"大补丁"）
        CustomBadgesPatch.Patch(MainHarmony);
    } catch (Exception e) { Logger.Error(e.ToString()); }

    MainHarmony.TryPatchAll(assembly);            // ★ 扫整个程序集，把 [HarmonyPatch] 全打上

    CustomLocTableManager.Register("card_modifiers");
    ModCredits.Register(ModId, new ModCredits.Section("TEAM"), new ModCredits.Section("CONTRIBUTORS"));
}
```

**对比**：

| | 怎么打补丁 |
|---|---|
| **BaseLib** | `MainHarmony.TryPatchAll(assembly)` —— **声明式**，写个带 `[HarmonyPatch]` 的类就行，自动扫到 |
| **RitsuLib** | 显式 `patcher.RegisterPatches<MyModPatches>()` + `ApplyRequiredPatcher` |
| `local.action_game` | 手写 89 个 Harmony 补丁，无统一管理 |

**注意几个细节**：
- **三个补丁必须先手动打**（存档扩展 / `CardPileCmd.Add` / 徽章），因为它们**互相依赖或有顺序要求**，不能交给扫描。
- **`Libgcc()` 的 Linux 兜底**：`[DllImport("libdl.so.2")] dlopen("libgcc_s.so.1", 2|256)` —— 注释写"Hopefully temporary fix for linux"。**Harmony 在 Linux 上需要手动加载 libgcc**。这是跨平台发布才会踩到的坑。
- **`Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly`** —— 只有 mod 带挂在 `.tscn` 上的 C# 脚本才需要（对应 RitsuLib 的 `EnsureGodotScriptsRegistered`）。
- **`[ThreadStatic] IsMainThread`** —— 自己维护"是不是主线程"。

### 8.2 `InstructionPatcher` + `IMatcher` —— IL 改写

`Utils/Patching/InstructionPatcher.cs`（26KB）+ `InstructionMatcher.cs`（23.7KB）+ `CallMatcher.cs`：

```csharp
var patcher = new InstructionPatcher(instructions);
patcher.Match(matcherA, matcherB, matcherC)      // 顺序匹配，失败抛异常
       .Insert(...)                              // 链式
       .MatchFromEnd(...)                        // 从方法尾往头匹配
       .GetIndex(out int idx);
```

| API | 行为 |
|---|---|
| `Match(...)` | 顺序匹配，**失败抛异常**（默认 `DefaultMatchFailure`） |
| `Match(onFailMatch, ...)` | 失败时调自定义回调 |
| `TryMatch(...)` | 失败返回 `null` → **可以配合 `?.` 链式短路** |
| `MatchFromEnd(...)` | **从方法尾部**往前匹配（对付尾部模式很关键） |
| `Log` | 累积日志，便于诊断为什么没匹配上 |
| `GetIndex(out int index)` | 拿当前位置，方便继续插指令 |

作者的自我评价（代码注释逐字）：
> `//Placeholder-ish, other existing tools may be easier to use. Will need time to see.`

**诚实的作者**。对比 RitsuLib 的 `HarmonyIl` / `HarmonyIlPattern` / `HarmonyIlRewriter`
（带 `RequireSucceeded` / `RequireExactly` 报告断言）—— **RitsuLib 那套更成熟**，
BaseLib 这套胜在**链式顺手 + `MatchFromEnd` + `TryMatch` 短路**。

### 8.3 ★ `AsyncMethodSections` —— 切开异步状态机

`Utils/Patching/` 下 9 个文件，把 async 方法的 `MoveNext` **按状态分段**处理：

| 文件 | 作用 |
|---|---|
| `AsyncMethodCall.cs` (17.8KB) | 顶层入口 |
| `BranchSection.cs` (13.9KB) | 分支段 |
| `BranchingStateSection.cs` (9.7KB) | 有分支的状态段 |
| `LoadStateSection.cs` | 读状态 |
| `MoveNextSection.cs` | MoveNext 段 |
| `EndingSection.cs` | 收尾段 |
| `StateInfo.cs` / `StateParamInfo.cs` / `AsyncMethodContext.cs` | 数据模型 |

**为什么需要它** —— `Notes.txt` 里作者亲笔写了状态机结构（见 §11），
核心是：**每个 await 是一个状态，每个状态有 3.5 个 section**。想插一个新 await 点，
就必须在那个位置正确地构造这些 section，否则栈/状态会错。

> **对比 RitsuLib**：RitsuLib 明确说 `HarmonyAsyncIl` **不创建新 async state**，
> 需要额外 await 点就改 task-wrapper。**BaseLib 这里是在尝试真的切开状态机** ——
> 更激进，也更难。这是 BaseLib 最"重"的一块。

### 8.4 补丁目录里的重点

| 文件 | 大小 | 干什么 |
|---|---|---|
| `Content/CustomPilePatches.cs` | 28.3KB | 自定义牌堆 |
| `UI/HealthBarForecastPatch.cs` | **32.2KB** | **血条预测显示**（配合 `Hooks/HealthBarForecastRegistry.cs` 14.9KB + `IHealthBarForecastSource.cs` 10.8KB） |
| `Content/CustomEnums.cs` | 15.2KB | `[CustomEnum]` 的实现 |
| `Saves/ExtendedSaveHandlers.cs` | 25.4KB | 存档扩展 |
| `Features/CustomTargetType.cs` | 24.8KB | 自定义目标类型 |
| `Features/ModInteropPatch.cs` | 17.9KB | ModInterop 实现 |
| `Content/ContentPatches.cs` | 13.8KB | ★ 上面 §4 的 `CustomContentDictionary` |
| `Fixes/AnyPlayerCardTargetingPatches.cs` | 14.0KB | 修复 |
| `Fixes/CardRewardSerializationPatches.cs` | 12.4KB | 卡牌奖励序列化修复 |
| `Hooks/MaxHandSizePatches.cs` | 17.6KB | 最大手牌数 |
| `Hooks/ModifyBaseDamagePatches.cs` | 16.1KB | 基础伤害修改 |
| `Patches/PostModInitPatch.cs` | 9.3KB | **所有 mod 初始化之后的统一挂载点** |

> ⚠️ **`Patches/Fixes/` 里有 3 个"修复"补丁** —— 说明**原版有 bug，需要 mod 层兜**。
> `CardRewardSerializationPatches` / `AnyPlayerCardTargetingPatches` 这两个名字暗示：
> 卡牌奖励序列化、任意玩家目标选择在原版有问题。**自己做这方向时要留意。**

### 8.5 给外部 mod 用的钩子接口（`Hooks/` 只 12 个文件）

| 接口 | 干什么 |
|---|---|
| **`IHealthBarForecastSource`** | **血条预测数据源**（10.8KB，配套 `HealthBarForecastRegistry`） |
| `IMaxHandSizeModifier` | 修改最大手牌数 —— *（！这正是 RitsuLib 也有的东西，见 §13）* |
| `IModifyScryAmount` / `IAfterScryed` | 修改预言数量 / 预言后 |
| `IHealAmountModifier` | 治疗量修改 |
| `ICardTypeTextModifier` | 卡牌**类型文字**（"攻击"/"技能"）显示修改 |
| `IAfterCardDowngraded` | 卡牌降级后 |
| `IPlayCustomPowerSfx` | 自定义能力播放音效 |
| `BaseLibHooks.cs` / `CustomResourceHooks.cs` | 通用钩子 / 资源钩子 |

---

## 九、声明式配置（`Config/`，4,474 行）

**只写静态属性，自动生成 UI + 自动存档。**

```csharp
internal class MyModConfig : SimpleModConfig
{
    public static bool RandomExplosions { get; set; } = true;
    public static int  ExplosionSize    { get; set; } = 80;
    public enum PotionDropRate { Low, Normal, High, Guaranteed }
    public static PotionDropRate EnemyPotionDropRate { get; set; } = PotionDropRate.Normal;
}

// 初始化入口（建议放在最前面，这样配置值先加载好）
public static void Initialize() {
    ModConfigRegistry.Register(ModId, new MyModConfig());
    ...
}
```

**自动 UI 映射（官方表）**：

| 类型 | 生成 | 控件 |
|---|---|---|
| `bool` | 复选框 | `ModSettingsToggleControl` |
| **任意 enum** | **下拉框** | `ModSettingsDropdownChoiceControl` |
| `int` / `float` / `double` | 滑块 | `ModSettingsSliderControl` |
| `string` | 单行输入 | `ModSettingsStringLineControl` |
| `Color` 或带 `[ConfigColorPicker]` 的 string | 颜色选择器 | `ModSettingsColorControl` |
| **方法** | **按钮** | 见 `config-advanced` |

其他类型要标 `[ConfigIgnore]`，否则控制台会打印提示。
**分区**用 `[ConfigSection("SectionName")]`。

本地化：
```json
{
  "RANDOMEXPLOSIONS.mod_title": "Random Explosions",
  "RANDOMEXPLOSIONS-RANDOM_EXPLOSIONS.title": "Enable Random Explosions",
  "RANDOMEXPLOSIONS-EXPLOSION_SIZE.title": "Explosion Size"
}
```

> **注意 `ModConfigRegistry` 是另一套框架 `ModConfig`（3 个 mod 依赖）的入口** ——
> 但 BaseLib 自己注册了这个 registry（`ModConfigRegistry.Register(ModId, new BaseLibConfig())`），
> 说明 **BaseLib 内置/兼容了 ModConfig 的配置机制**。这解释了为什么有的 mod 同时依赖两者。

文档：`wiki/docs/utilities/config.md`(8KB) + `config-advanced.md`(9.2KB，按钮/自定义控件等)。

---

## 十、`ModInterop` —— 免硬依赖调用别人

```csharp
[ModInterop("OtherModId", "OtherMod.Namespace.Here")]
public static class Interop {
    public static void Test(int num) { }                       // → OtherMod.Namespace.Here.Test

    [InteropTarget("MethodName")]
    public static void Annotated(object obj) { }               // object 参数 = 通配符

    public static int Number { get; set; }                     // 访问属性/字段 Number

    [InteropTarget("OtherMod.OtherNamespace", "SecretId")]
    public static string Id { get; set; }                      // 换个类型 + 换个名字

    [InteropTarget("OtherMod.OtherNamespace.SomeClass")]
    public class SomeClass : InteropClassWrapper {
        public class SomeClass(string id, int val) { }         // 伪造构造函数
        public int SpecialValue() { return 0; }
        public static int SpecialValue(object instance) { return 0; }   // 实例方法的静态版
    }
}
```

**官方定性（很重要）**：
> 这**本质上是 reflection 的封装**，用于"想用别的 mod 的复杂类但不想硬依赖"的**边缘场景**。
> 也可以用来**访问私有字段/属性/类**。

**生成时机**：`ModInterop generation occurs after all mod initialization and before the ModelDb is initialized.`

三大能力：**调方法 / 读写字面段和属性 / 模仿类（`InteropClassWrapper`）**。
实例化对象的真身存在 `this.Value`（类型是 `object`）。

> **对比 RitsuLib**：RitsuLib 的 `ModInterop` 是**生成 Harmony 转译补丁**；
> BaseLib 这套**明确说自己是 reflection 封装**。目的相同，实现不同。
> 想"一次写好两边都能用"的话，**RitsuLib 的版本性能更好**，BaseLib 的更好懂。

---

## 十一、★★ `Notes.txt` —— 作者的逆向笔记本（**最值钱的东西之一**）

`BaseLib-StS2/Notes.txt`（11.5KB，已拷到 `projects/ref-baselib/Notes.txt`）是
**作者 Alchyr 自己拆原版时留下的笔记**，涵盖：

### 11.1 异步状态机的结构（他为什么要写 `AsyncMethodSections`）

> 状态机 ——
> 整数 `state`，从 **-1** 开始
> 每个方法参数一个字段
> 每个 await 的结果一个字段（如果存在局部变量里）
> **每个 awaited 调用是另一个状态**，每个 await 一个 `TaskAwaiter<T>` 字段
>
> `MoveNext` 方法 —— **基本包含整个方法的实际内容**
> 开头检查 state 并跳转
>
> **每个状态是一个分支，有 3.5 个 section：**
> 1. **初始到达该状态时**（从上一个状态的结尾来）：调用该状态的 async 方法（返回 task）并取 awaiter。如果 awaiter 已完成，跳到 section 4；否则继续 section 2
> 2. 设置 `AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted`
> 3. **await 完成后 MoveNext 再次被调用时的分支目标**：设置好让 section 4 能像"从 section 1 来"一样工作
> 4. **该状态的收尾 + 下一个状态的起始**：调 `TaskAwaiter.GetResult`；中间那些非 await 的代码也在这里
>
> 只在需要等待时才设置 state，此时方法返回并等待下次执行。

另有关于 await 本身的说明：
> **"await" 只是"等待"**。真正的异步启动是**方法调用**做的。
> 同一个 task 被 await 多次也只会执行一次。

### 11.2 ★ 出牌流水线（完整链路）

```
NPlayerHand → NHandCardHolder → NCardPlay
  → card.TryManualPlay(目标)                       // null 或 target，看卡牌的目标类型
    → CardModel.EnqueueManualPlay
      → new PlayCardAction → RunManager.ActionQueueSynchronizer.RequestEnqueue
        → 合法性检查
          → CardModel.OnPlayWrapper                // ★ 真正干活的都在这
```
- 非自动打出：`CardPileCmd.AddDuringManualCardPlay` —— 保证视觉 NCard 存在、从原牌堆移除、加入 Play 牌堆、移到位
- 自动打出：直接加进 Play 牌堆（含定位）
- 钩子：`Hook.ModifyCardPlayResultPileTypeAndPosition`

### 11.3 ★ `CardPileCmd.Add` 的完整控制流（IL 级）

笔记里连**跳转标签**（`IL_0611`、`069b`）和**局部标志位**（`flag2`…`flag8`）都写下来了。摘要：

```
基本检查（是否还在战斗、卡牌归属）
对每张要移动的卡：
  目标是手牌 → 检查手牌是否满；满了改成弃牌堆
  flag = 本地玩家 || 目标是 Play 牌堆 || 原牌堆是 Play 牌堆
  若 flag，找 NCard：
    flag6 = 没有 NCard 且原牌堆是 抽牌/弃牌/消耗/牌库
      → 加入 cardsWithoutNodesChangingPiles
    否则若（NCard 为 null 且（目标是/曾是手牌 || 原牌堆非 null））
      → 创建节点！（重要）
069b:
  从原牌堆移除
  若原牌堆为 null 且目标是牌库 → 修改"加入牌库"钩子
  加入目标牌堆（位置按参数）
  若原牌堆为 null 且目标是战斗牌堆 → 触发 AfterCardEnteredCombat
  满手牌且是本地玩家 → 显示"手牌已满"
  若原牌堆 null 或其类型不是 play，或移动到（可见的）手牌，或重复 → 更新 NCard 视觉
```

然后 `create tween under NCombatRoom.Instance` → `MoveCardToNewPileBeforeTween` → 按牌堆类型分派补间 →
无节点换堆的卡走 `NCardFlyShuffleVfx` 飞行动画 → 全部完成后触发 `AfterCardChangedPiles` 钩子。

### 11.4 其他重要结论

| 结论 | 内容 |
|---|---|
| **伤害变量的三层值** | `DamageVar`：**`BaseValue`**（基础）/ **`EnchantedValue`**（附魔后的"基础"，升级后 +3 伤害会表现在这层）/ **`PreviewValue`**（应用所有修正后的最终值）。文本变色 = 比较 `PreviewValue` 与 `EnchantedValue`。见 `CardModel.ToHighlightedString`，由 `HighlightDifferencesFormatter` / `HighlightDifferencesInverseFormatter` 在卡面文本用 `.diff` / `.inverseDiff` 时调用 |
| **卡牌必须在某个牌堆里** | 不在牌堆 → 视为不在战斗中（**不计算伤害**） |
| **⚠️ 性能警告** | "卡牌在哪个牌堆"的判断**是遍历玩家所有牌堆返回第一个包含的结果** —— **大量遍历**。"如果谁有上百万张卡，可能是问题" |
| **伤害计算/文本** | 文本走 **SmartFormat** 库，扩展见 `LocManager.LoadLocFormatters` |
| **卡面描述来源** | `CardModel.GetDescriptionForPile`，被 `NCard` 调用并设为其 label |
| **关键类** | `CombatManager` / `RunManager` / `CombatState.IterateHookListeners` / `NThing` 是视觉版 / `CardModel→NCard` |
| **自定义资源要补的地方** | `NCard.UpdateEnergyCostVisuals`（检查 `_pretendCardCanBePlayed`）/ `PlayerCombatState.HasEnoughResourcesFor` / `CardModel.FinalizeUpgradeInternal`、`EndOfTurnCleanup`、`DowngradeInternal`、`CostsEnergyOrStars`；`CardEnergyCost.AfterCardPlayedCleanup`；`GetResourceCostColor`（见 `CardCostHelper`）；钩子 `TryModifyResourceCostWithHooks` |
| **费用修改的坑** | 费用有 `bool IncludeCombinedModifications`（是否被"木乃伊之手"这类"所有费用变 0"的效果影响），默认 true；还要处理 `CardModel.SetToFreeThisCombat` / `SetToFreeThisTurn` |
| **原版的坑** | "**瓦库的低语耳环**"调了 `SpendResources` **但不看返回值**。作者注："这些资源*应该*传给 CardPlay。瓦库没有正确设置。**也许可以算个 bug？**" |
| **`il-guide`** | 笔记第一行就推荐 https://github.com/pawslee/il-guide 作为 IL 入门 |

> **可偷指数 ★★★**：这份笔记等于**别人替我们把原版逆向了一遍**。
> 要做"改出牌流程 / 改卡牌视觉 / 改伤害计算 / 加资源"的活，**先看这里**再动手。

### 11.5 作者自己的 TODO（透露了 BaseLib 的边界）

```
auto-scale compendium options for custom pools
pools not linked to a character are added to the misc pool thing
Adjust transpiler patch implementations to make them more general
base damage/block modifier support for card model
WhatMod
CommonActions for Damage (general)
PlayCardAction : allow _card.SpendResources to spend custom resources，
                 并把该值传给 CustomCard 的 OnPlayerWrapper（最好对非 CustomCard 也生效）
NMouseCardPlay handles dragging before card is actually played
```

**两个可直接用的信号**：
- `base damage/block modifier support for card model` 还没做 → **想加基础伤害/格挡修正，得自己来**
- `WhatMod` 存在（`Utils/WhatMod.cs`，4.6KB）→ "这张卡/这个遗物来自哪个 mod"的查询工具

---

## 十二、官方文档与 Wiki（28 篇）

**Wiki 站**：https://alchyr.github.io/BaseLib-Wiki/ · **仓库**：`Alchyr/BaseLib-Wiki`（MIT）
**已拷到仓库**：`projects/ref-baselib/wiki/docs/`（186KB，纯文本）

| 分类 | 文档 |
|---|---|
| 总览 | `Features.md`（2.0KB）· `mechanics.md`（2.7KB） |
| **models/** | `custom-act` · **`custom-ancient`**(5.1KB) · `custom-card`(4.7KB) · `custom-character` · `custom-encounter` · `custom-event` · **`custom-orbs`(7.7KB)** · `custom-relic` · `custom-singleton` · `custom-temporary-power` · `index` |
| **localization/** | **`ancient-dialogue`**(4.3KB) · `code-loc` · `display-var` · `simplified-loc` · `var-loc` · `index` |
| **scenes/** | **`add-nodes`**(3.5KB) · **`creature-visuals`**(4.8KB) · `energy-counter`(4.8KB) · `merchant-character` · `index` |
| **utilities/** | `ancient-upgrades` · **`config`**(8.0KB) · **`config-advanced`**(9.2KB) · `custom-calc-vars` · **`enums`**(3.8KB) · `mod-audio` · `mod-interop`(2.8KB) · `pooling` · **`spirefield`**(4.2KB) · `index` |

### `Features.md` 里两个"不算功能但要说"的条目（很实用）

> - 标了 **`[SavedProperty]`** 的属性会**自动注册进游戏的 `SavedPropertiesTypeCache`**
> - **本地化缺失会记 error 而不是崩溃**
> - 自定义角色的卡池**会自动在图鉴里加一个筛选选项**
> - 玩家回合内自我施加的、涉及 `CustomModel` 类的 debuff **不会跳过第一次持续时间结算**
> - 一堆**防止用自定义角色时崩溃的兼容补丁**
> - `CommonActions` 类提供常用指令的简写

### 两个"小东西"（直接可用的 API）

```csharp
DescriptionOverrides.CustomizeDescription += delegate { ... };   // 全局描述修改（对所有 mod 生效）

RelicImageOverridePatch.AddOverride<BurningBlood>(
    new("relic.png".BigRelicImagePath(),
        "relic.png".RelicImagePath(),
        "relic_outline.png".RelicImagePath()));                   // 遗物图片覆盖，可加条件
```

---

## 十三、横向对照：BaseLib vs RitsuLib

**两个都 25+ 依赖、都 MIT、都是活跃维护的底座。选哪个？答案是——内容用 BaseLib、框架用 RitsuLib，而事实上很多 mod 两个都用。**

| 维度 | **BaseLib**（v3.4.7，451★） | **RitsuLib**（0.6.6，200★） |
|---|---|---|
| 定位 | **内容层** | **框架层** |
| 加内容 | **继承 `Custom*Model` 基类** | **打 `[RegisterXxx]` 标注** 或 content pack 链 |
| 注册时机 | 构造函数自注册 → `ModelDb.InitIds` | `ModTypeDiscoveryHub` 扫描程序集 |
| 给原版加字段 | **`SpireField` / `SavedSpireField`**（招牌） | **能力（Capability）系统**挂插件 |
| 扩展 enum | **`[CustomEnum]`**（运行时生成值） | 无对应（走 Capability + 自有 ID） |
| 打补丁 | `TryPatchAll(assembly)` **自动扫** | 显式 `RegisterPatches<T>` + 必需补丁降级 |
| IL 工具 | `InstructionPatcher` + `IMatcher`（链式，作者自称 placeholder-ish） | **`HarmonyIl` + 断言报告**（更成熟） |
| 异步状态机 | **`AsyncMethodSections` 真的切开状态机** | `HarmonyAsyncIl` **明确不创建新 state** |
| 时机/事件 | 少量 `Hooks/I*` 接口（12 个文件） | **74 个强类型生命周期事件 + 订阅** |
| UI 主题 | 无（有 Config 自动 UI） | **W3C 设计令牌主题系统**（9 主题） |
| 存档 | `SavedSpireField` + `ExtendedSaveTypes` | `PersistentDataEntry<T>` + Profile 作用域 |
| 配置 | **声明式静态属性 → 自动 UI + 存档** | `RegisterModSettings` + 完整控件库 |
| 跨 mod 调用 | `[ModInterop]`（reflection 封装） | `[ModInterop]`（**生成 Harmony 转译补丁**） |
| 文档 | **Wiki 站 28 篇 + `Notes.txt` 逆向笔记** | 文档站 29 篇（双语） |
| 多版本支持 | 单包（`min_game_version`） | **`compat/<gameVersion>/` 分层 + sha256 清单** |
| 自带资源 | pck 133KB（少量内置场景） | `assets.zip` 523KB（主题+本地化） |
| 招牌功能 | **`SpireField` · `[CustomEnum]` · `Custom*Model`** | **74 个事件 · 主题系统 · IL 断言 · 多版本包** |
| 一个共同的东西 | `IMaxHandSizeModifier` | 最大手牌数（两边都做，说明**原版手牌上限是硬编码的**，都要 patch） |

> **实操结论**：
> - **加卡/遗物/角色/先古/事件** → 用 BaseLib 的 `Custom*Model`，最省事
> - **要事件挂钩、要主题、要存档作用域、要跨游戏版本** → 用 RitsuLib
> - **要改出牌/伤害/视觉管线** → 两边都只是工具，**真正的知识在 `Notes.txt`**（§11）

---

## 十四、我们能偷什么（按优先级）

| # | 偷什么 | 用在哪 | 成本 |
|---|---|---|---|
| 1 | **`Notes.txt` 的逆向结论**（出牌流水线 / `CardPileCmd.Add` 控制流 / `DamageVar` 三层值 / 状态机 3.5 段结构） | **任何改原版流程的活**。省掉自己摸一遍 | 零（读） |
| 2 | **`SpireField` / `SavedSpireField` 的"键选哪个"判断** | 给原版对象挂数据：**每战斗用 `PlayerCombatState`，持久用 `SavedSpireField<Player,?>`** | 极低 |
| 3 | **`is not CustomXxxModel → return true` 的守卫写法** | 我们扩展原版行为时**保证不污染原版** —— 加上"我没提供值就回落原版"的二次判断 | 极低 |
| 4 | **`[CustomEnum]` 的思路** | C# enum 不能运行期加值 → **生成值赋给静态字段**。想扩展原版枚举集时直接用 | 低 |
| 5 | **`CustomAncientModel` 的三池语义 + `IsValidForAct`/`ShouldForceSpawn` 规则** | 先古线（我们已经拆过 `wuwancients`，现在知道底是谁的了） | 低 |
| 6 | **`AddedNode<TParent,TNode>`** | 往原版场景加节点，**不改 `.tscn` 不 patch 场景加载**。做战斗 UI 扩展用 | 低 |
| 7 | **`[SavedProperty]` 自动进存档缓存 + `ExtendedSaveTypes`** | 持久化自定义类型 | 低 |
| 8 | **声明式配置（静态属性 → 自动 UI/存档）** | 给自己 mod 加设置页，**不用写一个控件** | 低 |
| 9 | **`TryPatchAll(assembly)` + 需先打的三个补丁** | 补丁管理；**注意哪些必须手动先打**（有顺序依赖） | 低 |
| 10 | **`InstructionPatcher.MatchFromEnd` / `TryMatch` + `?.` 短路** | IL 匹配的顺手写法（补齐 RitsuLib 那套没有的能力） | 低 |
| 11 | **`Sts2PathDiscovery.props`** | **自动从注册表/Steam 探测游戏路径**（`HKEY_LOCAL_MACHINE\...\Steam App 2868840` → `InstallLocation`，回退 `HKCU\Software\Valve\Steam@SteamPath`）→ 我们自己的构建脚本可以直接用，**少一个手工配置** | 极低 |
| 12 | **`Libgcc()` 的 Linux 兜底** | 跨平台发布时 Harmony 在 Linux 需要 `dlopen("libgcc_s.so.1")` | 零 |
| 13 | **⚠️ 性能预警：卡牌"在哪个牌堆"是遍历所有牌堆** | 别写会放大这个遍历的代码；"有上百万张卡可能是问题" | 零（避坑） |
| 14 | **⚠️ `Fixes/` 三个修复补丁暗示原版有 bug** | 卡牌奖励序列化 / 任意玩家目标选择；做这方向时先看 | 零（避坑） |
| 15 | **`WhatMod`（这张卡/遗物来自哪个 mod）** | 排查 mod 冲突时有用 | 低 |

---

## 十五、取证与复现

### 目录

```
projects/ref-baselib/
├── README.md                    说明 + 复现步骤
├── BaseLib.json                 工坊清单（id/version/min_game_version）
├── LICENSE.txt                  MIT
├── Notes.txt                    ★★ 作者的逆向笔记（11.5KB）
├── Sts2PathDiscovery.props      ★ 自动探测游戏路径的 MSBuild 片段
├── wiki/
│   ├── index.md
│   └── docs/                    ★ 28 篇官方文档（186KB，按 models/localization/scenes/utilities 分类）
└── upstream/                    ⚠️ 不进仓库（2.9MB 源码 + 文档源，可一键重下）
    ├── BaseLib-StS2-master/     268 个 .cs / 40,112 行
    └── BaseLib-Wiki-main/
```

### 复现

```bash
B="D:/software/steam/steamapps/workshop/content/2868840/3737335127/BaseLib"
ls "$B"                                    # BaseLib.dll / BaseLib.json / BaseLib.pck
cat "$B/BaseLib.json"                      # ★ 清单在嵌套目录里，不在包根

# 源码（MIT）
mkdir -p upstream && cd upstream && \
  curl -sSL https://codeload.github.com/Alchyr/BaseLib-StS2/tar.gz/refs/heads/master | tar xz && \
  curl -sSL https://codeload.github.com/Alchyr/BaseLib-Wiki/tar.gz/refs/heads/main | tar xz

# 行数统计
find BaseLib-StS2-master -name '*.cs' | xargs wc -l | tail -1
```

### 相关的其它 Alchyr 仓库（都是 MIT，都能直接看）

| 仓库 | star | 用途 |
|---|---|---|
| **`ModTemplate-StS2`** | **299★** | **塔2 mod 项目模板** —— 起新 mod 时直接用它 |
| **`StS2ModAnalyzers`** | — | **给塔2 modding 用的 Roslyn 分析器/修复器** —— 能自动提示常见错误 |
| `Oddmelt` | 13★ | WIP 角色 mod（用 BaseLib 写的完整例子） |
| `BaseLib-Wiki` | 2★ | 本文档源 |

> ⚠️ **`ModTemplate-StS2`（299★）值得单独拆一次** —— 它定义了"一个塔2 mod 工程应该长什么样"
> （csproj / 打包 / 本地化 / 部署）。我们自己的 mod 工程可以对照它对齐。

---

## 十六、一句话总结

**BaseLib 把"往塔2里加内容"变成了"继承一个基类"。**

- **43 个 `Custom*Model`** 覆盖每一种内容类型；**构造函数自注册**，ID 自动加前缀。
- **同文件自带 Harmony 补丁 + `is not CustomXxx → return true` 守卫** —— 扩展原版而**不污染原版**。
- **`SpireField` / `SavedSpireField`** 给原版类挂字段（含进存档），**`[CustomEnum]`** 运行期扩枚举。
- **`Notes.txt` 是别人替我们做的逆向** —— 出牌流水线、`CardPileCmd.Add` 的 IL 控制流、
  `DamageVar` 三层值、异步状态机的 3.5 段结构，全在里面。

**它和 RitsuLib 是互补而非竞争**：BaseLib 管**内容**，RitsuLib 管**框架**。
两个都拆完之后，工坊里 **44% 的 mod** 的"底层语法"我们都能读懂了。
