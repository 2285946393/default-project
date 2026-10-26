# 第五批：8 款 IL2CPP 游戏批量扫描 + Blue Prince 深挖

时间：2026-10-11 23:00 · 工具：metadata 字符串提取 + 自建 `find_field_values.py`

---

## 〇、结论先行

| 层面 | 结果 |
|---|---|
| **8 款的架构/系统构成** | ✅ 全部拿到（命名空间、关键词、资源路径） |
| **Blue Prince 的抽取系统结构** | ✅ 完整拿到（15 类房间概率字段、稀有度、资源、骰子） |
| **具体数值** | ❌ 拿不到 —— **原因见第四节，这是个重要的方法论修正** |

---

## 一、8 款 IL2CPP 游戏系统构成对比

扫描方式：metadata 字符串去重后统计关键词命中数。

| 游戏（真名） | 体积 | 字符串量 | 突出特征（命中数 top） |
|---|---|---|---|
| **ToM**（金属之泪） | 3.4G | 263K | `Zaia.Economy`(32) / `Zaia.AI`(161) / **Upgrade=1247**、Mult=1353、Stat=5294 |
| **NeonAbyss2**（霓虹深渊2） | 1.6G | 213K | **`NEON.Game` 551 / `NEON.UI` 237 / `NEON.Game.PowerUps` 39**、Room=2879、Spawn=1788 |
| **Everything is Crab**（寄居蟹） | 1.1G | 193K | `World.UI`(45) / `World.POIs`(28) / `World.Items`(16) / `World.Stats`(8)、Modifier=425 |
| **RACCOIN**（推币机） | 552M | 173K | Room=178、**Rate=497**、Resource=690、Stat=2753 |
| **Astro Prospector**（咖啡星球工匠） | 310M | 165K | **`AstroProspector.Runtime.Infrastructure.Upgrades`**、Upgrade=745、Resource=599、Config=572 |
| **BALLxPIT** | 496M | 138K | Upgrade=234、Damage=184、Level=551 |
| **Blue Prince** | 5.0G | 122K | **Room=316 / Deck=108 / Resource=300 / Modifier=119** |
| **Enter the Chronosphere** | 2.0G | — | ❌ 没找到 metadata（可能用了别的打包方式） |

### 值得注意

1. **ToM（金属之泪）有独立的 `Zaia.Economy` 命名空间（32 个）** ——
   把经济系统单独成命名空间的不多见，值得单独拆。
2. **NeonAbyss2 的 `NEON.*` 命名空间最规整**（Game 551 / UI 237 / Behaviors 228 / PowerUps 39），
   是这批里**最容易拆的**（命名统一 = 反编译后结构清晰）。
3. **Astro Prospector（咖啡星球工匠）的 Upgrade 命中最高（745）**，
   且命名空间确认是 `AstroProspector.Runtime.Infrastructure.Upgrades` —— 和丁东王的
   `FlyingRocks.Runtime.Infrastructure.Upgrades` **命名习惯一模一样**（都是
   `<项目>.Runtime.<层>.<模块>`）。疑似同一套模板/同一批开发习惯。

---

## 二、Blue Prince 深挖（你在学的那个）

### 2.1 ★ 房间抽取系统：15 类房间各有独立概率字段

字段名（全部从 metadata 提取）：

```
Bedroom_RoomsChance          Black_RoomsChance
Blue_RoomsChance             Classrooms_RoomsChance
Clockwork_TimeRoomsChance    Duct_RoomsChance
Gear_RoomsChance             Greenhouse_GreenRoomsChance
Hallway_RoomsChance          Library_RareRoomsChance
MajorBump_RoomsChance        MinorBump_RoomsChance
PatioRooms_RoomsChance       Powered_RoomsChance
Shop_RoomsChance
```

**注意命名里的三类前缀，暴露了三种不同的控制维度**：

| 前缀 | 含义 | 例子 |
|---|---|---|
| `<房间名>_RoomsChance` | 该房间的**出现概率** | `Bedroom_RoomsChance` |
| `<来源>_<标签>RoomsChance` | 某个来源**影响某类房间的概率** | `Greenhouse_GreenRoomsChance`（温室→绿房）、`Library_RareRoomsChance`（图书馆→稀有房）、`Clockwork_TimeRoomsChance` |
| `Major/MinorBump_RoomsChance` | **概率增减的幅度**，不是概率本身 | `MajorBump` / `MinorBump` |

> ★★ **`MajorBump` / `MinorBump` 这个设计值得抄**：
> 把"概率"和"概率的调整量"分成两类字段。房间（Greenhouse/Library）说的是
> "我要影响哪一类"，Bump 说的是"影响多大"。**两者解耦**，加一个新房间只需要加一条
> `<名字>_RoomsChance`，不用碰 Bump 逻辑。

### 2.2 稀有度系统（Draft / Deck 分层）

```
BaseRarity                        ← 基础稀有度
DraftRankRarity                   ← 抽取位的稀有度（第1张/第2张/第3张权重不同）
DraftSlotRarity                   ← 抽取槽位的稀有度
DeckMixThresholdByRarity_Free     ← ★ 牌库混合阈值（免费档）
DeckMixThresholdByRarity_Gems     ← ★ 牌库混合阈值（宝石档）
DrawRarityShift                   ← 抽到的稀有度偏移
GUIRarityShiftDisplay             ← 稀有度偏移的 UI 显示（★ 居然给玩家看）
FilterRarityOutput                ← 稀有度输出过滤
InitializeRarity
```

**三个可抄的点**：

1. **`DraftRankRarity` vs `DraftSlotRarity` 是两个维度** ——
   一次抽 3 张时，"第几张"（Rank）和"哪个槽位"（Slot）可以有不同的稀有度规则。
   这就是"三选一里必出一个好的"这类保底的实现位置。
2. **`DeckMixThresholdByRarity_Free` / `_Gems`** ——
   按**货币类型**分开设阈值：免费抽和花宝石抽，稀有度分布不同。
   → 这是"付费/免费差异化"的标准做法，且做在配置里不是代码里。
3. **`GUIRarityShiftDisplay`** —— 稀有度偏移**会给玩家看见**。
   一般游戏会藏起来，Blue Prince 选择透明化（符合它"信息全公开、考的是规划"的设计哲学）。

### 2.3 加权抽取的通用件

```
CalculateWeight          GetRandomWeightedIndex
CGWeightedItem           ChanceBias
Draxus_DeadEndsChance    ← 死路概率（Draxus 是宅邸名）
Furnace_RedRoomChance    ← 熔炉→红房概率
```

`CalculateWeight` + `GetRandomWeightedIndex` = 标准加权随机。
**`ChanceBias`** 是个有意思的字段：允许对某类结果加偏置（连续没抽到就提高权重 = 保底）。

### 2.4 资源 / 货币

| 资源 | 字段 | 备注 |
|---|---|---|
| **步数** | `AddStep` / `CanStep` / `StepFsm` / `StepDistance` | 核心限制资源 |
| **金币** | `Gold` / `GoldLeft` / `AddCoins` / `HasCoins` | |
| **宝石** | `Gems` / `GemsLeft` / `GemCost` / `CommonGems` / `Distribution_Gem` | 分普通/稀有 |
| **钥匙** | `GemKeyFSM` / `Laundry_GemsToCoins` / `Laundry_KeysToCoins` | 洗衣房可兑换 |
| **津贴** | `Allowance` / `TotalAllowance` | 每日额度 |
| **骰子** | `IvoryDice_*` / `DiceTradesAccepted` / `IvoryProbability` | ★ 见下 |

### 2.5 ★ 骰子系统（对应你想做的"掷骰子走路"）

```
IvoryDiceGroup          IvoryDiceObject
IvoryDice_Total         IvoryDice_Used        IvoryDice_PercentUsed
IvDicesLeft             IvDicesUsed
IvoryProbability        CheckIvoryDiceTrades  DiceTradesAccepted
```

**关键**：`IvoryDice_Total` / `_Used` / `_PercentUsed` ——
**骰子是消耗品，而且会统计"用了百分之多少"**。

`DiceTradesAccepted` / `CheckIvoryDiceTrades` —— **骰子可以交易**（用别的资源换骰子次数）。

> ★★ 这个设计思路可以直接抄：
> **把"掷骰子的次数"做成可交易的资源**，玩家要用金币/宝石/钥匙去换掷骰机会。
> 这跟海龟潜海"氧气 = 可交易的硬约束"是同一个手法 ——
> **把最硬的约束（步数/骰子次数）变成可以主动花掉的东西**。

---

## 三、★ 方法论修正：一个重要的坑

### 3.1 我以为的（错的）

在丁东王上成功的方法让我误以为：
> "IL2CPP 的资产里存着字段名，用字段名当锚点就能读到后面的数值。"

### 3.2 实际情况

**丁东王是特例，不是通例。**

丁东王的升级资产里真的存了 `"_maxUses"` 这个字符串，因为它的设计是
**用字符串字段名去反射修改目标属性**（从 `FlyingRocks` 的 `StatModify` 命名就能看出来）。

而 **Blue Prince 不是这么做的** —— 我搜遍了所有可能的位置都没找到字段名字符串：

| 搜过的位置 | 大小 | 结果 |
|---|---|---|
| `resources.assets` | 8MB | ❌ |
| `level2` | 103MB | ❌ |
| `sharedassets2.assets` | 407MB | ❌ |
| `GameAssembly.dll` | 52MB | ❌ |
| `rooms_assets_all_*.bundle`（Addressables） | 364MB | ❌ |

### 3.3 正确的决策树：IL2CPP 游戏的数值到底在哪

```
第 1 步：扫 global-metadata.dat 的字符串
         → 拿到命名空间 / 类名 / 字段名（★ 这个永远能拿到，且 IL2CppDumper 版本无关）

第 2 步：判断游戏是不是"字符串字段名"式设计
         判据：在 resources.assets 里搜某个字段名（如 `_maxUses`）
         ├─ 搜得到 → 恭喜，用【字段名锚定法】直接读数值（丁东王模式）
         └─ 搜不到 ↓

第 3 步：数值在资产里，但没有字段名 → 需要【类型树】才能解析
         三条路：
         a) Il2CppDumper 出 dummy dll → 但 v39+ 不支持 ❌
         b) Cpp2IL（支持 Unity 6）→ 待试
         c) 整体扫 float/double 找漂亮序列 → 碰运气，丁东王那批就是这么来的

第 4 步：数值也可能压根不在文件里 —— 在 C# 构造函数的默认值里
         → 编译进代码段，只能靠反汇编（IDA/Ghidra），投入产出比低
```

### 3.4 仍然永远成立的部分

**不管数值拿不拿得到，第 1 步永远有效**，而且价值很大：
- 命名空间 → 看出架构分层（Blue Prince 的 `World.*` / `NEON.*` / `Zaia.Economy`）
- 字段名 → 看出系统构成（`Bedroom_RoomsChance` 直接告诉你"每个房间有独立概率"）
- 资源路径（ScriptableObject 的 CreateAssetMenu 路径）→ 看出数据组织方式

> **字段名本身就是设计文档。** 光看 `MajorBump_RoomsChance` 这个名字，
> 就能读出"概率和调整量是解耦的"这个设计决策 —— 不需要知道它是 0.3 还是 0.5。

---

## 四、待办

- [ ] **试 Cpp2IL**（支持 Unity 6 / metadata v39），如果能出 dummy dll，
      就能用 UnityPy 的 typetree 解析 Blue Prince 全部数值
- [ ] **ToM（金属之泪）单独拆** —— 有独立的 `Zaia.Economy` 命名空间，经济系统成体系
- [ ] **NeonAbyss2 单独拆** —— `NEON.*` 命名空间最规整，最容易出成果
- [x] ~~**Astro Prospector（咖啡星球工匠）** —— 猜它和丁东王同模板，也用字符串字段名~~
      **❌ 已验证：不是。** 见下「补充验证」

---

## 五、补充验证：咖啡星球工匠也不是「字符串字段名」式设计

我在第三节推测：Astro Prospector 的命名空间
`AstroProspector.Runtime.Infrastructure.Upgrades` 与丁东王的
`FlyingRocks.Runtime.Infrastructure.Upgrades` **命名习惯完全一致**，
所以可能也是同一套设计，锚定法可用。

**实测推翻了这个推测。**

为此写了 `scan_raw_fields.py`（字节级扫描，不依赖 UnityPy，
直接在文件里找 `[int32长度][字段名][对齐][double]`）。

**先做回归测试**（证明脚本本身没问题）：

```
$ python scan_raw_fields.py "东丁王/.../resources.assets"
### resources.assets (122.1MB) — 75 个字段 / 149 个值
   _basicValue            [1.0, 3.0]
   _areaRadius            [0.35, 0.5]
   _damage                [1.0, 2.0, 4.0, 5.0, 10.0, 16.0]
   _damageMultiplier      [0.15, 0.2, 0.25, 0.35, 0.5, 1.0]
   _maxUses               [2.0, 4.0, 5.0, 12.0]
   ...（还顺带扫到了之前漏掉的 _damage=2.0 / _damage=10.0 两个值）
```

✅ 脚本有效，且比之前的 UnityPy 版本**多扫到 18 个值**（149 vs 131）——
因为字节级扫描不受 MonoBehaviour 类型过滤限制。

**然后扫咖啡星球工匠**：

| 位置 | 结果 |
|---|---|
| `Astro Prospector_Data` 整个目录 | **0 个值** |
| `GameAssembly.dll` | **0 个值** |

### 结论

**命名空间命名习惯一样 ≠ 数据组织方式一样。**

`FlyingRocks.Runtime.*` 和 `AstroProspector.Runtime.*` 只是说明两个项目
采用了类似的**分层命名规范**（Runtime / Infrastructure / Application），
这是 Unity 圈的常见约定，不代表同一个作者或同一套数据方案。

**判断游戏是不是「字符串字段名」式设计，唯一可靠的办法就是实测**：
拿一个字段名去资产文件里搜，搜得到才算。

→ 更新后的决策树第 2 步：
```
搜不到字段名 = 需要类型树 → 上 Cpp2IL
（不要再靠"命名习惯像"来猜）
```

---

## 附：本次产出的工具（在 `C:\Temp`）

| 脚本 | 用途 |
|---|---|
| `scan_il2cpp.py` | 批量扫 metadata，输出命名空间 + 关键词统计 → `il2cpp_scan.json` |
| `find_field_values.py` | ★ 在大文件里直接搜字段名，读后面对齐处的 double（mmap，407MB 文件 5 秒扫完） |
| `extract_il2cpp_values.py` | 整体解析 SerializedFile 的 MonoBehaviour，字段名锚定法提取数值 |
| ★ `scan_raw_fields.py` | **字节级扫描**（推荐）—— 不依赖 UnityPy，直接找 `[len][字段名][对齐][double]`。比 UnityPy 版多扫到值，且能扫 dll / bundle 等任意文件 |

`find_field_values.py` 用法：
```bash
python find_field_values.py <assets或dll路径> 字段名1 字段名2 ...
# 例：python find_field_values.py ".../sharedassets2.assets" Bedroom_RoomsChance Shop_RoomsChance
```

`scan_raw_fields.py` 用法（推荐先跑这个）：
```bash
python scan_raw_fields.py <文件或目录> [最小命中次数]
# 例：python scan_raw_fields.py "D:\...\Hole Is Mine_Data" 2
# 输出每个字段名和它出现过的所有值
```
