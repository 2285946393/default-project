# 丁东王 / Hole Is Mine —— IL2CPP metadata v39 突破拆解

拆解时间：2026-10-11 · 前三次尝试失败的补拆
**结论：成了。** 拿到完整升级树结构 + 131 个精确数值。

---

## 零、为什么之前拆不动，怎么解决的

| 障碍 | 原因 | 解法 |
|---|---|---|
| Il2CppDumper 报 `metadata v39 unsupported` | 官方最新版 v6.7.46 停在 2024-07，只支持到 v31 | **不用 Il2CppDumper** |
| 扫 exe 找数值全是噪声 | ❌ 扫错文件了。IL2CPP 的字符串在 `global-metadata.dat`，不在 exe | 改扫 metadata → 1189 个字符串 |
| typetree 读不了 | IL2CPP 包默认不带 typetree | 手写字节解析器 |
| 按 float32 扫找不到数值 | ❌ **数值是 `double`（8 字节）不是 float** | 按 8 字节对齐解析 |
| 数值好像不在资产里 | ❌ 偏移算错 4 字节；且字符串后要 align4 才到 double | 用「前缀长度 = 字符串长度」做锚点定位 |

### 最终有效的方法（可复用到任何 IL2CPP 游戏）

```
1. 扫 global-metadata.dat 的 ASCII 串 → 拿到命名空间/类名/字段名（1189 个 FlyingRocks 串）
2. UnityPy 加载 resources.assets，MonoBehaviour 的 m_Name 在固定偏移 0x1C
   ├─ 0x00-0x0B  PPtr<GameObject>  (int32 fileID + int64 pathID)
   ├─ 0x0C-0x0F  m_Enabled (1字节 + 3 padding)
   ├─ 0x10-0x1B  PPtr<MonoScript>  ← script pathID 在这里
   ├─ 0x1C-0x1F  string 长度 (int32)
   └─ 0x20...    字符串数据，然后 align4
3. 字段名锚定法：
   找到 `\x09\x00\x00\x00_armySize` 这种「长度前缀 + ASCII」
   → 值在 align4(字符串结束) 或 align8(字符串结束) 处的 double
```

> **最关键的一步**：发现 `_` 开头的字段名串，前面 4 字节正好是它的长度。
> 用这个当锚点，就能精确跳到后面的 double 值 —— 不需要 typetree。

---

## 一、游戏内部项目名：FlyingRocks（飞石）

### 1.1 架构：Clean Architecture 三层 + 三个独立 dll

```
FlyingRocks.Domain.dll          领域层（纯逻辑，不依赖 Unity）
FlyingRocks.Application.dll     应用层（用例：BuyUpgrade / ConfigureUpgrades）
FlyingRocks.Infrastucture.dll   基础设施层（Unity 相关：UpgradeView）
```

> ⚠️ 注意 **`Infrastucture` 是拼错的**（少一个 r），前后端一致地错，所以能编译。

命名空间分布（含类/字段名中出现的分支）：
- `Domain.Upgrades` — Upgrade / UpgradeTree / BasicUpgrade / ConditionalUpgrade / MultiUpgrade
- `Domain.Incursion` — 下潜（一局的核心循环）
- `Domain.{Player, Minions, Resource, Town, Save, Persistence, Core, Helpers}`
- `Application.Upgrades` / `Application.Ports.Upgrades`（端口 = 依赖倒置）
- `Infrastructure.{Upgrades, Incursion, UI, Minions, Throwables, GeneralSystems, Main}`

> **对你的意义**：这是"独立游戏也认真分层"的样本。跟咖啡星球工匠（Domain.Rogue vs DoPrestige）同一个路子 ——
> 领域层独立成 dll，方便写单元测试、也方便做多版本（见下面 FG_/PR_）。

### 1.2 三套并行配置（正式版 / 序章 / 测试）

资源路径里的 `FG_` / `PR_` / `PT_` 前缀：

| 前缀 | 含义 | 实例 |
|---|---|---|
| `FG_` | **Full Game**（正式版） | `FG_GameConfigurationData` / `FG_Cave1`~`Cave5` / `FG_ExpertisesData` / `FG_AllAchievementsData` |
| `PR_` | **Prologue**（序章/demo） | `PR_GameConfigurationData` / `PR_Cave1` / `PR_ExpertisesData` / `PR_InfiniteCave` |
| `PT_` | **Playtest**（测试版） | `PT_AllAchievementsData` / `PT_IncursionData` / `PT_GameMode` |

**这是 demo 分发的干净做法**：同一套代码，三份配置数据。不是 `if (isDemo)` 撒在代码里。

同一套配置的尺寸对比（可以看出正式版内容量）：

| 配置 | 正式版 | 序章 |
|---|---|---|
| GameConfigurationData | 1820B | 1700B |
| ExpertisesData | 3416B | 2344B |
| InfiniteCave | 2676B | 220B |

---

## 二、升级系统的结构

### 2.1 13 个升级部位（ScriptableObject 存放路径实录）

```
Flying Rocks/Upgrades/Drill/Stat Modify          + Drill/Perk Unlock
Flying Rocks/Upgrades/Level/Stat Modify          + Level/Perk Unlock
Flying Rocks/Upgrades/Player/Stat Modify         + Player/Perk Unlock
Flying Rocks/Upgrades/Minion/Stat Modify         + Minion/Perk Unlock
Flying Rocks/Upgrades/Minion/Stat ModifyByCondition
Flying Rocks/Upgrades/Totems/Stat Modify         + Totems/Perk Unlock
Flying Rocks/Upgrades/Bag/Stat Modify            + Bag/Perk Unlock
Flying Rocks/Upgrades/Item/Stat Modify           + Item/Perk Unlock
Flying Rocks/Upgrades/Town/Stat Modify           + Town/Perk Unlock
Flying Rocks/Upgrades/Incursion/Stat Modify      + Incursion/Perk Unlock
Flying Rocks/Upgrades/Expertise/Stat Modify      + Expertise/Perk Unlock  ← 注意路径末尾有空格
Flying Rocks/Upgrades/Conversion/Stat Modify
Flying Rocks/Upgrades/Redirector/Stat Modify     + Redirector/Perk Unlock
Flying Rocks/Upgrades/Artifacts/Direction Artifact Stat Modify + Perk Unlock
Flying Rocks/Upgrades/AllUpgrades                ← 总表
Flying Rocks/Upgrades/Multi Upgrade              ← 复合升级
Flying Rocks/Upgrades/Dummy                      ← 占位
```

### 2.2 ★ 每个部位都拆成两半：Stat Modify vs Perk Unlock

| 类型 | 干什么 | 有无数值 | 例子 |
|---|---|---|---|
| **Stat Modify** | 改一个数值字段 | ✅ 有 double | `DrillMaxUses1 _maxUses=2.0` |
| **Perk Unlock** | 解锁一个机制/品级 | ❌ 只有名字+引用 | `BabyGrade1`（解锁 1 星） |

> 这跟 RockBottom「机制升级不惩罚、数值升级才惩罚」是同一个分类思想，
> 但丁东王做得更彻底：**直接从资源路径上分成两类资产**。

### 2.3 每条升级资产的字段结构（实测）

```
{
  m_Name:                 "DrillMaxUses1"                      ← 资产名 = <部位><属性><级数>
  UpgradeNames/DrillMaxUses1                                   ← 名字本地化 key
  UpgradeDescriptions/DrillMaxUses                             ← 描述 key（★ 同级共用，不带级数）
  "General" / "General2" / "General3"                          ← ★ 三条分支
  list<PPtr> (可选)                                             ← 授予的 Expertise
  "_maxUses"                                                   ← ★ 目标字段名
  double 2.0                                                   ← ★ 数值
}
```

**三个可抄的点**：

1. **描述 key 不带级数**（`UpgradeDescriptions/DrillMaxUses`），名字 key 带级数。
   → 同一属性的 4 级共用一段描述文案，只有标题不同。**省本地化工作量**。
2. **`General` / `General2` / `General3` 是分支标号**，不是分类名。
   → 同一属性的不同级数可以落在不同分支上（比如 `MinionDamage1`=General、`2`=General2、`3`=General3）。
3. **字段名 = 字符串**（`"_maxUses"`），不是直接引用。
   → 说明游戏用**反射/字符串匹配**修改字段。灵活但慢，且改名会静默失效。

---

## 三、★ 完整数值表（131 个，从 resources.assets 直读）

### 3.1 Drill（钻头）28 项

| 升级项 | 字段 | 数值 |
|---|---|---|
| DrillAreaRadius1 / 2 | `_areaRadius` | 0.35 → **0.5** |
| DrillDamage1 / 2 / 3 | `_damage` | 1 → 16 → 4 |
| DrillDamage5 / 6 / 7 | `_damage` | 25 → **900** → 450 |
| DrillDamageMultiplier1 / 2 | `_damageMultiplier` | 0.35 → 0.5 |
| DrillDamageMultiplier_Combo | `_damageMultiplier` | 1.0 |
| DrillDamageReload1 / 2 / 3 | `_damageReload` | 0.5 → 0.8 → 1.5 |
| DrillDamageReload_Combo | `_damageReload` | 0.75 |
| DrillDistance1 / 2 | `_distance` | 2.0 → 2.0 |
| DrillExtraCritChance1 | `_extraCritChance` | 0.05 |
| DrillExtraDamagePerConsecutiveHit1 | `_extraDamagePerConsecutiveHit` | 0.05 |
| DrillExtraDamageToDangers1 | `_extraDamageToDangers` | 0.15 |
| **DrillMaxUses1~4** | `_maxUses` | **2 → 4 → 5 → 12** |
| DrillMaxUses_Combo | `_maxUses` | 12 |
| DrillRechargeAmount1 / _Combo | `_rechargeUses` | 1.0 / 1.0 |
| DrillRechargeReload1 | `_rechargeReload` | 0.2 |

### 3.2 Minion（小弟）43 项

| 升级项 | 字段 | 数值 |
|---|---|---|
| **MinionArmySize1~11** | `_armySize` | **全部 1.0**（★ 见下方解读） |
| MinionDamage1~8 | `_damage` | 1 → 5 → 35 → 2 → 10 → 175 → 50 → 29.9 |
| MinionDamageMultiplier1 / 2 | `_damageMultiplier` | 0.15 → 0.25 |
| MinionDamageMultiplier_Combo | `_damageMultiplier` | 0.2 |
| MinionReload1~4 | `_actionReload` | 0.15 → 0.25 → 0.35 → 0.5 |
| MinionSpeed1 / 2 | `_speed` | 0.2 → 0.2 |
| MinionJobBlocks1 / 2 / 3 | `_minimumBlocksBeforeWander` | 1 → 1 → 2 |
| MinionWanderLess1 / 2 | `_restingTime` | **-1.0** / -1.0 |
| MinionHasRestingTime1 | `_restingTime` | 0.0 |
| MinionChosenDamage1 / 2 | `_chosenDamage` | 20 → 150 |
| MinionChosenCritChance1 | `_chosenCritChance` | 0.1 |
| MinionChosenCritDamage1 | `_chosenCritDamage` | 0.75 |
| MinionChosenCloneLifetime1 | `_chosenCloneLifetime` | 8.0 |
| MinionChosenExtraBlocks1 | `_chosenExtraBlocks` | 10 |
| MinionChosenReload1 | `_chosenReload` | 1.0 |
| MinionChosenThrowDistance1 | `_chosenThrowDistance` | 2.0 |

> ★ **`MinionArmySize1~11` 全部是 1.0** —— 这是重点：
> 「每级 +1 个位置」→ 每级数值都是 1，**等级信息完全由「资产个数」承载**（11 个资产 = 11 级）。
> 和财富磨坊「一个数组长度表达等级」同一个手法，但这里是「一个文件夹里的资产个数表达等级」。
> **好处**：加一级 = 复制一个资产改个名，策划不用碰代码，也不用改数组。

### 3.3 Player（玩家）16 项

| 升级项 | 字段 | 数值 |
|---|---|---|
| PlayerCritChance1 / 2 / 3 | `_critChance` | 0.05 → 0.075 → 0.08 |
| PlayerCritChance_Combo | `_critChance` | 0.05 |
| PlayerCritDamage1 / 2 / 3 | `_critDamage` | 0.15 → 0.25 → 0.3 |
| PlayerMaxDistance1 / 2 | `_maxThrowDistance` | 0.6 → 0.75 |
| PlayerSpeed1 / 2 | `_playerSpeed` | 0.2 → 0.33 |
| PlayerCollectionRadius1 / _Combo | `_collectionRadius` | 0.25 / 0.25 |
| PlayerThrowChargeTime1 / 2 / 3 | `_throwChargeTime` | **-0.24 → -0.18 → -0.2** |

### 3.4 Level / Item / Incursion / Totems / 其他

| 组 | 项 | 字段 | 数值 |
|---|---|---|---|
| Level | LevelBitChance1~5 | `_blockBitChance` | 0.02 → 0.03 → 0.02 → 0.03 → **0.1** |
| Level | LevelBitChance_Combo | `_blockBitChance` | 0.02 |
| Level | LevelBitAmount1 / BitValue1 | `_bitAmount` / `_bitValue` | 1.0 / 1.0 |
| Level | LevelDeepInBits1 / 2 | `_deepInBits` | 2 → 3 |
| Item | ItemAmount1 / _Combo | `_amount` | 1.0 / 1.0 |
| Item | ItemCreateChance1 | `_createItemChance` | **0.015** |
| Incursion | IncursionDuration1~4 | `_duration` | 10 → 8 → 10 → 8 |
| Incursion | IncursionDeepInTime1 / 2 | `_deepInTime` | 10 → 5 |
| Incursion | IncursionTimeByBlock1 | `_timeByBlock` | 2.0 |
| Totems | TotemsBoundExtraArea1 | `_boundExtraArea` | 0.25 |
| Totems | TotemsBoundExtraDamage1 / 2 | `_boundExtraDamage` | 0.5 → 1.0 |
| Totems | TotemsBoundExtraMovement1 | `_boundExtraMovement` | 0.5 |
| Town | TownCave2BySecond1 | `_cave2PerSecond` | 1.0 |
| Town | TownCave3BySecond1 | `_cave3PerSecond` | 1.0 |
| Town | TownTentaclesBySecond1 | `_tentaclesPerSecond` | 1.0 |
| Redirector | RedirectorSpeed1 | `_extraSpeed` | 0.3 |
| Conversion | ConversionBasicDevotion1~3 | `_basicValue` | 1 → 1 → 3 |
| Expertise | ExpertiseCurrentMax1 / 2 | `_currentCopies` | 1.0 / 1.0 |

---

## 四、从数值里读出的 4 条设计规律

### 4.1 负数表示「减少」，不是一个独立字段名

```
PlayerThrowChargeTime : -0.24 / -0.18 / -0.2   → 缩短蓄力时间
MinionWanderLess      : -1.0 / -1.0            → 减少乱逛
```

用**正负号**表达方向，字段名只描述目标属性（`_restingTime`），不用 `_restingTimeReduction`。
→ 省字段名，且正负可以叠加。

### 4.2 数值不单调 = 后缀不是等级，是「场景索引」

```
DrillDamage:  1, 16, 4, 25, 900, 450      ← 完全不递增
MinionDamage: 1, 5, 35, 2, 10, 175, 50, 29.9
```

对比真正按等级走的：
```
DrillMaxUses: 2, 4, 5, 12                 ← 递增
MinionReload: 0.15, 0.25, 0.35, 0.5       ← 递增
PlayerCritDamage: 0.15, 0.25, 0.3         ← 递增
```

**结论**：`DrillDamage1~7` 和 `MinionDamage1~8` 的**数字后缀对应的是洞穴/深度**，不是等级。
（游戏有 `FG_Cave1`~`Cave5` + `CreditsCave` + `InfiniteCave` = 7 个洞穴 → 正好对上 `DrillDamage1~7`）

→ **同一个属性在不同场景给不同的基础值**，而不是一路加下去。

### 4.3 概率值都极小、且刻意不取整

```
ItemCreateChance1      = 0.015   (1.5%)
LevelBitChance1~5      = 0.02 / 0.03 / 0.02 / 0.03 / 0.1
LevelBitChance_Combo   = 0.02
DrillExtraCritChance1  = 0.05
```

- 概率全在 **1.5% ~ 10%** 区间 —— 不玩"5%/10%/15%"整齐档位。
- `LevelBitChance` 五级是 **0.02, 0.03, 0.02, 0.03, 0.1** —— 前四级在两档之间反复横跳，第五级才跳到 10%。
  → 这是**"前四级是量变、第五级是质变"**的曲线设计。

### 4.4 `_Combo` 是独立的一档，不是加成倍率

有 `_Combo` 后缀的项：`DrillDamageMultiplier_Combo`(1.0) / `DrillDamageReload_Combo`(0.75) /
`DrillMaxUses_Combo`(12) / `MinionDamageMultiplier_Combo`(0.2) / `PlayerCritChance_Combo`(0.05) /
`PlayerCollectionRadius_Combo`(0.25) / `ItemAmount_Combo`(1.0) / `LevelBitChance_Combo`(0.02)

观察：`_Combo` 的值往往**直接取该系列的最好一档或更优**：
- `DrillDamageMultiplier`: 0.35 → 0.5，而 `_Combo` = **1.0**（比满级还高）
- `DrillMaxUses`: 2 → 4 → 5 → 12，而 `_Combo` = **12**（等于满级）
- `DrillDamageReload`: 0.5 → 0.8 → 1.5，而 `_Combo` = **0.75**（夹在中间）

→ **`_Combo` 不是"再乘一个倍率"，而是"独立的一档数值"**，由连击/组合系统授予。

---

## 五、其他资产结构（顺带拿到）

### 5.1 洞穴系统

```
FG_Cave1 (1348B) / Cave2 (2032) / Cave3 (2164) / Cave4 (2176) / Cave5 (1780)
FG_CreditsCave (1412B)      ← 制作人员关
FG_InfiniteCave (2676B)     ← 无限模式（最大！）
PR_Cave1 (1324) / PR_InfiniteCave (220)
FG_IncursionData (192B) / PR_IncursionData (144) / PT_IncursionData (180)
```

### 5.2 成就：41 个（正式版）+ 15 个（序章）+ 序章重制版

规律：`<条件>_Achievement`，含 `Blocks1~3` / `Items1~3` / `Resources1~3` / `Throws1~3` /
`Depth2~5` / `MinionCount1~3` / `Shop2~3` / `Cave1~4Finished` / `First*`（11 个首次类）

### 5.3 三种 GameMode

`00_GameModeData` / `FG_GameMode` / `PR_GameMode` / `PT_GameMode` —— 序章、正式、测试各一份。

---

## 六、这批拆解产生的 3 条方法论（写进工具手册）

1. **IL2CPP 游戏不用 Il2CppDumper 也能拆** —— 前提是你能拿到：
   ① `global-metadata.dat` 的字符串表（命名空间/类名/字段名全在里面）
   ② UnityPy 读 `resources.assets` 的 MonoBehaviour（m_Name 在 0x1C）
   ③ 字段名锚定法跳转到 double 值
2. **数值是 double 不是 float** —— 这是本次最大的坑。IL2CPP + Unity 序列化对 double 会 align8，
   按 float32 扫必然找不到。
3. **ScriptableObject 的"名字"就是元数据** ——
   `DrillMaxUses1` 这个名字本身就编码了 [部位=Drill][属性=MaxUses][级数=1]。
   读名字就能重建整个升级树，不需要解析任何字段。

---

## 附：产出文件

- `C:\Temp\hole_fr.txt` — 1189 个 FlyingRocks 命名空间字符串
- `C:\Temp\hole_paths.txt` — 41 个 ScriptableObject 资源路径
- `C:\Temp\hole_mono.txt` — 1497 个 MonoBehaviour 的 pathID/名字/长度（818 个有名）
- `C:\Temp\hole_values.json` — ★ **123 个升级对象的字段名 + 数值（131 个）**
