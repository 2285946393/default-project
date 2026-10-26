# Monster Train 2 与 Wildfrost 拆解笔记

> 拆解时间：2026-10-11 凌晨
> 用户选定：卡牌 roguelike 双壁；学习重点 = **架构 + 数值，两者都要**
> 前置：`notes/增量游戏-五款拆解笔记.md`、`notes/数值提升设计-学习笔记.md`

---

## 零、拆解条件

两个都拿到**完整 C# 源码**（都是 Mono 编译，不是 IL2CPP）：

| 游戏 | dll | 大小 | 反编译结果 |
|---|---|---|---|
| **Monster Train 2** | `MonsterTrain2_Data/Managed/Assembly-CSharp.dll` | 5.5 MB | **2524 个 .cs**（10 秒） |
| **Wildfrost** | `Modded/Wildfrost_Data/Managed/Assembly-CSharp.dll` | 2.15 MB | **1198 个 .cs**（5 秒） |

```bash
ilspycmd.exe -p -o C:\Temp\mt2_src "...\MonsterTrain2_Data\Managed\Assembly-CSharp.dll"
ilspycmd.exe -p -o C:\Temp\wf_src  "...\Modded\Wildfrost_Data\Managed\Assembly-CSharp.dll"
```

⚠️ **Wildfrost 有个坑**：它的**本体是 IL2CPP**（根目录 `GameAssembly.dll` 53MB），
但目录里另有一个 **`Modded/` 子目录是完整 Mono 版**（带 `MonoBleedingEdge`）。
能反编译的正是这个 Modded 版 —— 如果只看根目录会误判成 IL2CPP 而放弃。

> ⚠️ 拆解深度：2524 + 1198 个文件里我只精读了核心的几个（BalanceData、
> CardUpgradeTreeData、CardDraftRarityBucketData 等），拿到的是**骨架与核心参数**，
> 不是逐行通读。

---

## 一、Monster Train 2

### 1.1 ★ 核心规则参数（`BalanceData.cs`，硬编码默认值）

这些数字直接定义了 MT2 的手感：

```csharp
private int startingGold;                       // 初始金币
private int maxHandSize        = 10;            // 手牌上限
private int startOfTurnEnergy  = 3;             // 回合开始能量
private int startOfDeploymentPhaseEnergy = 1;   // 部署阶段能量
private int maxEnergy          = 10;            // 能量上限
private int startOfTurnCards   = 5;             // 每回合抽牌
private int numSpawnPointsPerFloor   = 7;       // ★ 每层 7 个放置点
private int characterCapacityPerFloor = 5;      // ★ 每层最多 5 个单位
private int unitUpgradeSlots      = 2;          // 单位升级槽
private int spellUpgradeSlots     = 2;          // 法术升级槽
private int equipmentUpgradeSlots = 2;          // 装备升级槽
private int maxMutatorCount       = 3;
private int championUpgradesShown = 2;          // 升级时给几个选项
private int initialDragonsHoardCap = 5;
private int maxDragonsHoard       = 10;
private int scoringBonusWaves        = 3;
private int scoringHellforgedBossWaves = 10;
private int alternateChampionUnlockLevel = 5;
private int relentlessIterationsUntilNotification = 10;
private float mainClanXpFactor = 1f;
private float subClanXpFactor  = 1f;
```

**`numSpawnPointsPerFloor = 7` + `characterCapacityPerFloor = 5`** 是 MT2 标志性的
「三层列车」骨架：每层 7 个格子但最多站 5 个单位 ——
**格子永远比单位多 2 个**，所以永远腾挪得开，但又不至于随便堆。
这是"放置类玩法"的关键旋钮，两个数一起定死了战术密度。

`startOfTurnEnergy = 3` + `startOfTurnCards = 5` + `maxHandSize = 10`：
**抽 5 张、能用 3 费** —— 每回合必须取舍，不可能全打出去。

### 1.2 ★★ 升级树：线性链 + 已开始优先（`CardUpgradeTreeData.cs`）

```csharp
public sealed class UpgradeTree {
    private List<CardUpgradeData> cardUpgrades;   // ★ 一条线性升级链
    public bool UpgradeIsMaxedOut(CardUpgradeState s)
        => cardUpgrades[cardUpgrades.Count - 1].GetID() == s.GetCardUpgradeDataId();
}
```

**给选项的逻辑**（`GetRandomChoices`）是整个构筑系统的核心：

```csharp
foreach (UpgradeTree tree in upgradeTrees) {
    if (玩家在这条树上已有升级)  list.Add(这条树的【下一级】);   // 深化已有
    else if (这条树已到顶)       跳过;                          // 满了不给
    else                        list2.Add(这条树的【第一级】);  // 新的起点
}
list.Shuffle(); list2.Shuffle();
result = list.Concat(list2);          // ★ 先已开始的，再全新的
if (result.Count > numOptionsDisplayed) 截断;
```

**三个设计点：**

1. **树是线性的，不能跳级** —— 只能按顺序往下点，避免玩家直奔最强升级。
2. **已经开始的树优先给"下一级"**（`list` 排在 `list2` 前面）。
   这保证玩家的构筑会**持续深化**而不是每次都拿到一堆起点逐渐摊薄。
3. **满级的树直接剔除** —— 后期选项池会自然收窄，避免无效选项。

> **对比尖塔**：尖塔是"卡牌升级一次就到顶"，MT2 是**多条独立的升级链，每条能点好几级**。
> 后者给玩家的长期目标感强得多 —— 你不是在强化一张卡，是在走一条路线。

### 1.3 经济：跳过奖励也是明码标价

`BalanceData.GoldForSkippingRewardsData` —— **不要奖励可以换金币**，每种奖励各有定价：

```csharp
public int cardDraft;               // 跳过一次卡牌选择
public int blessingDraft;           // 跳过一次祝福选择
public int championUpgradeDraft;    // 跳过冠军升级
public int individualBlessing;      // 单张祝福
public int individualCard;          // 单张卡
public int purge;                   // 移除
public int levelUpUnit;             // 单位升级
```

以及 `DraftCost` —— **按「奖励类型 × 稀有度」定价，而且是范围值**：

```csharp
private RewardType      rewardType;
private CollectableRarity rarity;
private Vector2Int      costRange;    // ★ 价格是一个区间，不是定值
```

**`costRange` 用区间不用定值** —— 同一稀有度的卡，实际售价在区间内浮动。
这样既保持了稀有度的价格秩序，又避免了"所有稀有卡都恰好一样贵"的僵硬感。

稀有度本身走 `CardDraftRarityBucketData`（一个 List<CollectableRarity> 的桶），
说明**抽卡是按"稀有度桶"抽取，不是按单张卡权重** —— 先定稀有度，再从该稀有度里选。

### 1.4 效果系统规模（体现内容量）

```
RelicEffect*.cs   260 个    遗物效果
CardEffect*.cs    141 个    卡牌效果
CardTrigger*.cs    15 个    卡牌触发器
```

**260 个遗物效果** —— MT2 的内容厚度主要堆在这里。

---

## 二、Wildfrost

### 2.1 ★ 架构：一切皆 Modifier

`*ModifierSystem.cs` 有 22 个，`StatusEffect*.cs` 有 167 个。看命名就知道它的组织方式：

```
AddCharmSlotModifierSystem        AddFrenzyToBossesModifierSystem
BlockConsumeModifierSystem        BlockForMinibossesModifierSystem
BoostAllEffectsModifierSystem     BoostAllHealthModifierSystem
BoostArea2EnemyDamageModifierSystem
DrawWhenRedrawChargedModifierSystem
DrawExtraWhenRedrawNotChargedSystem
BombskullClunkersModifierSystem
```

**每个规则一条独立的小系统，靠组合叠加出复杂行为。**
这跟尖塔 2 的 `ModifierModel` 是同一个思路 ——
你在尖塔 2 里做自定义模式，用的正是这套模式，Wildfrost 是个现成的参照实现。

### 2.2 核心参数

```csharp
// CharacterSaveData.cs
public int handSize = 6;            // ★ 手牌 6 张

// BattleWavePoolData.cs
public int maxSize;                 // 波次池上限
public int maxPulls = 999;

// CampaignBattlePool.cs
private int basePoints;             // 战役难度点数（随进度增长）
```

**手牌 6 张**（MT2 是抽 5 上限 10）。Wildfrost 手牌少、没有能量系统，
走的是"每回合出有限的几张"而不是"算费用"。

### 2.3 两个特色系统

- **`BattleUndoSystem.cs`** —— 战斗内**支持撤销**。
  这在卡牌 roguelike 里非常少见，说明它的战斗是**确定性可回放**的
  （`Battle.cs` 里有 `SetSeed(baseSeed, offset)`，用种子驱动）。
- **`Dice.cs`** —— 有骰子机制（`faceValues`）。

### 2.4 战役结构

`CampaignData` / `CampaignNodeSaveData` / `CampaignSaveData` / `BattleWaveData` /
`BattleWavePoolData` / `CampaignBattlePool(basePoints)` ——
**节点地图 + 波次池 + 难度点数随进度增长**，是标准的 roguelike 战役骨架。

---

## 三、横向对比

| | Monster Train 2 | Wildfrost |
|---|---|---|
| 源码规模 | 2524 文件 | 1198 文件 |
| 手牌 | 抽 5 / 上限 10 | 6 |
| 资源 | 能量 3（部署阶段 1，上限 10） | 无能量 |
| 场地 | 三层 × 7 格，每层最多 5 单位 | — |
| 成长 | **多条线性升级树**，逐条深化 | Modifier 组合 |
| 效果量 | 遗物 260 / 卡牌 141 | StatusEffect 167 / Modifier 22 |
| 定价 | `costRange` 区间 × 稀有度 | — |
| 特色 | 跳过奖励换金币 | 战斗可撤销、种子驱动 |

**两者的成长哲学完全不同：**

- **MT2 是"纵深"** —— 给你几条树，让你沿着其中一两条走到底。
  升级树机制保证你越往后越专精，构筑会自然分化。
- **Wildfrost 是"组合"** —— 一堆小 Modifier 自由叠加，靠组合涌现出玩法。

---

## 四、能拿去用的

| 想做的 | 学谁 | 抄什么 |
|---|---|---|
| 卡牌/单位升级系统 | **MT2** | `CardUpgradeTreeData.GetRandomChoices`：已开始的树优先给下一级 |
| 场地格位设计 | **MT2** | 每层 7 格 / 最多 5 单位（格子比单位多 2，腾挪得开又不至于堆） |
| 稀有度定价 | **MT2** | 按稀有度桶抽 + `costRange` 区间定价（不用定值） |
| "不要奖励"的出口 | **MT2** | 每种奖励各有金币定价，给玩家第二个选择 |
| Modifier 架构 | **Wildfrost** | 每个规则一个 `*ModifierSystem`，组合叠加 |
| 确定性战斗 / 撤销 | **Wildfrost** | `SetSeed` + `BattleUndoSystem` |

**对 DouSpire / 尖塔 2 mod 最直接的一条**：
MT2 那个「**已开始的升级树优先给下一级**」的选牌逻辑，
可以直接用在尖塔的卡牌奖励/升级界面 —— 让玩家已有的构筑方向被持续深化，
而不是每次都从一堆不相关的起点里挑。这条改动很小，对手感的影响很大。

---

## 五、一句话结论

两个都是 Mono，源码完整可读，MT2 2524 个文件、Wildfrost 1198 个。
**MT2 的价值在"成长结构"**（升级树 + 场地格位 + 稀有度区间定价），
**Wildfrost 的价值在"架构"**（一切皆 Modifier + 确定性可撤销战斗）。

Wildfrost 有个坑要记住：**本体是 IL2CPP，但 `Modded/` 子目录才是 Mono 版**，
只看根目录会误判。
