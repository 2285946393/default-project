# 凛冬尖塔 · 波次系统 API 技术备忘（已反编译核实）

> 2026-10-06 15:55，WorkBuddy 侧（wb）写。
> **所有内容都是 `ilspycmd` 反编译 `sts2.dll` 实测得到，不是推断。** ZCode 可直接照抄。
> 配套：`notes/凛冬尖塔-开发进度.md`（进度）、`notes/凛冬尖塔-单房间模式设计案.md`（玩法）。

---

## 零、一句话结论

**"一波怪" = 一个自定义 `EncounterModel`。**
它的 `GenerateMonsters()` 返回 `List<(MonsterModel, string?)>`，
**第二个元素就是槽位名（站位）**。M1 叠怪就是因为 `CreatureCmd.Add<T>()` 没传这个参数。

**不需要自研坐标系统，不需要缩 UI，不需要 `SetEnemyIndex`。**
本体的遭遇系统本来就是干这个的——我们用它的正规路。

---

## 一、完整调用链（全部实测）

```
EncounterModel.GenerateMonsters()           ← 你重写这个方法
    ↓ 返回
IReadOnlyList<(MonsterModel monster, string? slotName)>
    ↓ 由框架调用
EncounterModel.GenerateMonstersWithSlots(IRunState runState)   ← 框架自动调，别自己调
    ↓ 存进私有字段 _monstersWithSlots
EncounterModel.MonstersWithSlots            ← 只读暴露（生成前访问会抛异常）
    ↓
CombatState.CreateCreature(monster, side, slot)   ← 框架调
    ↓
Creature(monster, side, slotName)   →  creature.SlotName = slotName
    ↓
CombatState.AttachCreature(creature)   ← 挂上但**还没进战斗**（不可被选中、power 不触发）
    ↓
CombatState.AddCreature(creature)      ← 真正进战斗
    ↓
CombatState.SortEnemiesBySlotName()    ← 按槽位名排序（决定敌人显示/行动顺序）
```

---

## 二、确切签名（逐个反编译核实）

### `EncounterModel`（`MegaCrit.Sts2.Core.Models`）

```csharp
public abstract class EncounterModel : AbstractModel
{
    // ★ 你要重写的核心方法。签名实测如下（注意是 protected、无参数）
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters();

    // ★ 槽位名表：决定"这个遭遇有哪些站位、顺序如何"。默认空数组。
    public virtual IReadOnlyList<string> Slots => Array.Empty<string>();

    // 必须实现的抽象属性
    public abstract RoomType RoomType { get; }

    // 可选重写
    public virtual bool HasScene => false;                 // 是否有专属场景/背景
    public virtual bool FullyCenterPlayers => false;       // 是否把玩家居中（"被包围"战斗用）
    public virtual bool ShouldGiveRewards => true;         // 打完是否给战斗奖励（事件遭遇常设 false）
    public virtual string CustomBgm => "";
    public virtual IEnumerable<MonsterModel> AllPossibleMonsters => ...;  // 可能出现的怪（图鉴用）

    // 只读属性
    public bool HaveMonstersBeenGenerated => _monstersWithSlots != null;
    public IReadOnlyList<(MonsterModel, string?)> MonstersWithSlots { get; }  // ★ 不重写
    public IReadOnlyList<MonsterModel> SpawnedEnemies { get; }  // 含战斗中召唤的

    // 钩子
    public void OnCreatureSpawned(Creature creature);  // 战斗中有生物被生成时调
}
```

### `CombatState` / `ICombatState`（`MegaCrit.Sts2.Core.Combat`）

```csharp
public Creature CreateCreature(MonsterModel monster, CombatSide side, string? slot);
// 文档原文：Creates a new creature from a monster in this combat state.
//          After you create the creature, you should call AddCreature to add it to combat.

public void AttachCreature(Creature creature);
// 文档原文：Attaches an existing creature to this combat state **without adding it to the combat**。
//          不可被选中、power 不触发。for players and almost all monsters，它紧挨着 AddCreature 调用。

public void AddCreature(Creature creature);
// 文档原文：Adds a creature to the combat. 几乎总是紧跟在 CreateCreature 后面。
//          **不到这一步，生物不能被选为目标、power 也不会触发。**
//          它被独立出来的原因：生物可以被移出战斗、之后再加回来。
//          **官方注明：目前只有 Doormaker 战斗这么用。** ← 这就是"动态加怪"的官方先例
public void RemoveCreature(Creature creature, bool ...);
public void CreatureEscaped(Creature creature);   // 逃走（非死亡）时移除

public void SortEnemiesBySlotName();   // ★ 实测实现见下方「坑 2」
public void SetEnemyIndex(Creature creature, int index);  // ★ 有槽位的战斗里会抛异常！见「坑 3」
```

### `Creature`（`MegaCrit.Sts2.Core.Entities.Creatures`）

```csharp
public string? SlotName { get; set; }
public Creature(MonsterModel monster, CombatSide side, string? slotName);
```

### `CreatureCmd.Add<T>` —— M1 用的那个（★ 关键）

```csharp
public static Task<Creature> Add<T>(ICombatState combatState, string? slotName = null)
    where T : MonsterModel, new();
// ★ 它本来就带 slotName 参数！M1 叠怪纯粹是没传。
// 但它内部走的路径与本体的 CreateCreature/AddCreature 是否一致，建议再核一眼；
// 若要完全走本体正规路，用 CreateCreature + AddCreature 两步。
```

---

## 三、★ 三个必须知道的坑（全部实测）

### 坑 1 · 槽位传 `null` 是合法的，官方自己也在传

反编译 `InkletsNormal`（3 只小怪的官方遭遇）：

```csharp
protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
{
    Inklet item  = (Inklet)ModelDb.Monster<Inklet>().ToMutable();
    Inklet inklet = (Inklet)ModelDb.Monster<Inklet>().ToMutable();
    Inklet item2 = (Inklet)ModelDb.Monster<Inklet>().ToMutable();
    inklet.MiddleInklet = true;
    return new (MonsterModel, string)[3]
    {
        (item,   null),      // ← 槽位传 null！
        (inklet, null),
        (item2,  null)
    };
}
```

→ **槽位全传 `null` 也能正常打**。所以"站位"不是必须自己编，本体有兜底排布逻辑。
→ **但也正是这个原因**：M1 的叠怪可能不是"没传槽位"导致的，而是**`CreatureCmd.Add` 走的路径绕过了本体的排布**。
   **这点必须实测确认**（见下方「五、待 ZCode 实测确认的唯一一点」）。

### 坑 2 · `SortEnemiesBySlotName` 对 `null` 的排序是错的（重要！）

实测实现：

```csharp
public void SortEnemiesBySlotName()
{
    if (Encounter != null)
    {
        _enemies.Sort((a, b) =>
            Encounter.Slots.IndexOf(a.SlotName) - Encounter.Slots.IndexOf(b.SlotName));
    }
}
```

`Slots.IndexOf(null)` **返回 -1**。所以：
- 若某敌人 `SlotName == null`，它会被排到 **-1 位，也就是最前面**。
- 若两个都 `null`，则是 `-1 - (-1) = 0`，**顺序不定**（不稳定排序）。

→ **想要可控站位，就老老实实给 `Slots` 起名字、给每个怪传对应的 `SlotName`。**
   `InkletsNormal` 传全 `null` 能work，多半是因为它 `Slots` 也是空的（`SortEnemiesBySlotName` 里 `Encounter != null` 但仍会排）。

### 坑 3 · `SetEnemyIndex` 在有槽位的遭遇里**直接抛异常**

实测实现：

```csharp
public void SetEnemyIndex(Creature creature, int index)
{
    if (Encounter.Slots.Any())
    {
        throw new InvalidOperationException(
            "Cannot modify turn order of a combat with pre-set slots");
    }
    ...
}
```

→ **`Encounter.Slots.Any()` 为真时，`SetEnemyIndex` 直接抛异常。**
→ ZCode 之前笔记里提到的 `SetEnemyIndex` 方案**在自定义遭遇里不能用了**——
   **改用 `Slots` + `SlotName`**。这是本轮最重要的一条修正。

---

## 四、给凛冬波次系统的推荐写法（照抄即可）

```csharp
// 一个"波次遭遇" = 一个 EncounterModel 子类。
// 用 RitsuLib 注册：可能用 [RegisterActEncounter] 或类似特性（ZCode 之前提过，需核实确切名字）。

public sealed class WinterWave01 : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;

    // ★ 站位表：名字随便起，顺序决定显示/行动顺序。
    // 建议 5 个槽位上限（对应本体的战斗布局）。
    public override IReadOnlyList<string> Slots => new[] { "l2", "l1", "c", "r1", "r2" };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // 每个怪指定一个槽位名，就按这个位置站，不叠。
        return new (MonsterModel, string?)[]
        {
            (ModelDb.Monster<LeafSlimeM>().ToMutable(), "l1"),
            (ModelDb.Monster<Inklet>().ToMutable(),     "r1"),
        };
    }

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        new[] { ModelDb.Monster<LeafSlimeM>(), ModelDb.Monster<Inklet>() };
}
```

**波次表**（幕数/波数/阵容）用自己的数据类 + 确定性种子实现，
`EncounterModel` 自带的 `Rng` 已由框架按 `runState.Rng.Seed + runState.TotalFloor + 遭遇ID哈希` 播好种
（实测 `GenerateMonstersWithSlots` 源码），**联机天然一致，不用自己搞种子**。

---

## 五、待 ZCode 实测确认的「唯一一点」

**M1 叠怪的真实原因，到底是不是"没传 slotName"？**

有两条互相矛盾的线索：
- ZCode 的结论：`CreatureCmd.Add<T>(combatState, slotName)` 带参数，没传所以叠。
- 我实测的 `InkletsNormal`：**官方遭遇里槽位也传 `null`**，而它并不叠。

**可能性 A**：`CreatureCmd.Add` 内部完全复刻 `CreateCreature + AddCreature`，那么传 `null` 时
本体自有排布逻辑（可能按 `_enemies.Count` 算位置），叠是因为别的原因。
**可能性 B**：`CreatureCmd.Add` 走的是一条不触发排布的捷径。

→ **建议的验证方式（不需要真人测试，写个日志就行）**：
在 `CreatureCmd.Add` 前后打印 `combatState.Enemies.Select(c => c.SlotName)`，
看新加的怪 `SlotName` 是不是 `null`、以及 `Enemies` 顺序怎样。
**或者更彻底**：直接改用 `CreateCreature(monster, CombatSide.Enemy, "l1") + combatState.AddCreature(c)` 两步走，
跳过 `CreatureCmd.Add`——这是本体自己的正规路径，最稳。

**我的倾向：直接走 `CreateCreature + AddCreature`，别在 `CreatureCmd.Add` 上纠结。**
理由：这是本体 `EncounterModel` 用的同一条路，且 `AddCreature` 的文档明确说"只被 Doormaker 用过"，
说明它是官方支持的动态加怪入口。

---

## 六、复现命令（给下一个接手的人）

```bash
ILSPY="C:/Users/有花无实/.dotnet/tools/ilspycmd.exe"
STS2DLL="D:/software/steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.dll"
XML="D:/software/steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.xml"

# 反编译单个类型（最常用）
"$ILSPY" -t "MegaCrit.Sts2.Core.Models.EncounterModel" "$STS2DLL"
"$ILSPY" -t "MegaCrit.Sts2.Core.Combat.CombatState" "$STS2DLL"
"$ILSPY" -t "MegaCrit.Sts2.Core.Models.Encounters.InkletsNormal" "$STS2DLL"   # 多怪遭遇样板

# 列类型（类），找名字
"$ILSPY" -l c "$STS2DLL" | grep -i "encounter"

# xml 查官方文档注释（比反编译更快，但覆盖不全）
grep -n -A 14 "EncounterModel.MonstersWithSlots" "$XML"
```

**注意**：`sts2.xml` 覆盖不全（`SortEnemiesBySlotName`、`Doormaker` 都搜不到，只在 dll 里）。
**xml 查不到就去反编译 dll，别下"不存在"的结论。**

---

## 七、顺带核实的两条（M2 会用）

### ① 遭遇注册特性

ZCode 之前笔记提过 `[RegisterActEncounter]`。**本轮未核实其确切名字**，
用之前先 `ilspycmd -l c` 搜 RitsuLib 里的 `*Encounter*Attribute`。

### ② 自定义遭遇怎么被选中

`Encounter.Stats`、`EncounterStats` 存在，另有 `ModifyUnknownMapPointRoomTypes`
（`IReadOnlySet<RoomType>` → 可改"未知点"能解析成哪些房间类型）。
**这个钩子可以用来把普通战斗点改造成波次遭遇点**——M2 再核。

---

## 八、★★ 两个通用坑（M2 审查新增，写 mod 时反复会踩）

### 坑1：写在 `BeforeCombatStart` 里的状态初始化，**读档后都会丢**

**实证**（sts2.dll 第 600 行）：

```csharp
// CombatManager.StartCombatInternal(CombatTurnState turnState) 里
await Hook.BeforeCombatStart(turnState.State.RunState, turnState.State);   // ← 全游戏仅此一处调用
```

- `BeforeCombatStart` **只在 `StartCombatInternal()` 里调**。
- **读档恢复战斗不走 `StartCombatInternal`**（从存档直接重建 `CombatTurnState`）。
- ⇒ 钩子不重跑 ⇒ 钩子里赋的字段保持存档旧值；若该字段从未被赋过值，就是 `int` 默认 **0**。

**踩法实例**（M2 v1 的 `SubWavesPlanned`）：进房间前存档 → 读档 → `Planned=0` → 锁1/锁2 判定 `0<0=false` → 双双失效。

**规矩**：
1. **编译期常量别进 `[SavedProperty]`** —— 用计算属性（`=> 表.Length`）代替，读档天然正确。
2. 必须在运行时算的状态，**在每回合都跑的钩子（如 `BeforeSideTurnStart`）里做惰性兜底**：
   ```csharp
   if (SubWavesPlanned <= 0) SubWavesPlanned = WaveTable.All.Length;
   ```
3. **回归用例固定套路**：*进房间/开战**之前**存档 → 读档 → 看状态还在不在*。

### 坑2：官方 `EncounterModel.GetNextSlot` 有**连调挑重**陷阱

```csharp
public string GetNextSlot(ICombatState combatState)
    => Slots.FirstOrDefault(s => combatState.Enemies.All(c => c.SlotName != s), string.Empty);
```

- **一次只挑一个**，靠读 `combatState.Enemies` 判断占用。
- **连调 N 次刷 N 只怪时会挑到同一个槽位** —— 因为怪还没实际落位，`Enemies` 没更新。
- ⇒ 一批怪里要挑 N 个空位，**必须自己先做 `taken` 快照**（ZCode 的 `PickFreeSlots` 就是这么干的，写对了）。

**注意返回值**：找不到空位时官方返回 **`string.Empty`（不是 null）**；两者在 `SortEnemiesBySlotName`
里都排最前（`Slots.IndexOf` 返回 -1），都会导致位置错乱。真"满员"时要另外决定策略。

**规矩**：**推荐官方 API 前，先看它的调用约束（能不能连调、返回空值是什么），别只看签名。**
