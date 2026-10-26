# Boardlike 拆解笔记（含「尖塔 2 掷骰子走路 mod」方案）

> 拆解时间：2026-10-10
> 游戏目录：`D:\BaiduNetdiskDownload\game\Boardlike.Build.25388032`（743MB / 9月18日构建）
> 反编译产物：`C:\Temp\boardlike_src\`（165 个 .cs，32,333 行）
> 拆解目的：评估「把杀戮尖塔玩法做成掷骰子走路」是否可行、怎么做

---

## 一、先说结论：这次是 **Mono 编译，能读到完整源码**

跟上一份「蓝途王子」笔记对比一下就明白运气有多好：

| | 蓝途王子（Blue Prince） | **Boardlike** |
|---|---|---|
| 编译方式 | **IL2CPP**（`GameAssembly.dll` 是原生机器码） | **Mono**（`MonoBleedingEdge` 目录存在） |
| 能拿到什么 | 只能 dump 出**类结构/字段名**，拿不到方法体 | **完整 C# 源码**，连 `if` 分支和注释都在 |
| 工具 | Il2CppDumper v6.7.46 | 本机已有的 `ilspycmd 8.2.0.7535` |
| 产出 | `dump.cs`（9,673 个类、光秃秃的签名） | 165 个可编译回读的 `.cs` |

**一条命令就搞定：**

```bash
ilspycmd.exe -p -o C:\Temp\boardlike_src \
  "D:\BaiduNetdiskDownload\game\Boardlike.Build.25388032\Boardlike_Data\Managed\Assembly-CSharp.dll"
```

`-p` = 按命名空间建子目录。核心 dll 只有 476KB，但反出来 3 万多行。

> 顺带发现：代码注释是**土耳其语**（`"Oyuncunun tile'i"`、`"Aktif degil, shuffle atlandi."`），
> 是个土耳其小团队的 Unity 游戏。

---

## 二、游戏是什么：小镇 + 棋盘冒险 + 地牢，三层结构

```
┌─ Town（小镇，常驻基地）──────────────────────┐
│  BuildingsManager 建筑 / BreedingManager 养东西 │
│  VisitorManager 访客 / CraftingManager 合成     │
│  EquipmentMerchant、DwarvenBlacksmith 商人      │
└──────────────┬───────────────────────────────┘
               │ 选英雄出门（HeroPathSelect：战士/商人/盗贼…）
               ▼
┌─ Adventure（棋盘冒险，掷骰子走路）────────────┐   ← ★ 用户想抄的就是这一层
│  BoardManager 生成格子                        │
│  PlayerMovement 掷骰子 + 逐格走               │
│  BoardPlayerManager 踩格子触发事件（按 tag）    │
└──────────────┬───────────────────────────────┘
               │ 踩到 Dungeon / Enemy
               ▼
┌─ Dungeon / Battle（地牢与战斗）───────────────┐
│  DungeonManager + DungeonTile 地牢格子         │
│  BattleManager → Battle.cs（4714 行，最大文件） │
└──────────────────────────────────────────────┘
```

`GameManager.cs` 是总控，手里攥着二十几个子系统的引用
（`boardManager` / `adventureManager` / `battleManager` / `playerMovement` …），
还管着 `boardVisuals` vs `townVisuals`、`boardCam` vs `townCam` 两套场景的切换。

---

## 三、★ 核心：掷骰子走路到底怎么做的

这一节是整份笔记的重点。

### 3.1 骰子不是 1–6，是 **2–4 点**

```csharp
// PlayerMovement.cs
public void RollDice()
{
    diceCount = Random.Range(2, 5);   // ★ 整数随机，上限取不到 → 实际是 2/3/4
}
```

### 3.2 步数修正链（照抄价值极高）

```csharp
// 伪代码，还原自 PlayerMovement.cs
int steps = diceCount;

if (MaxDice_Buff)  steps = Mathf.Max(steps, 4);   // 保底提升到 4
if (DiceHalved)    steps /= 2;                    // 减半
if (DiceMin)       steps = Mathf.Max(steps, 1);   // 保底 1

steps += Turn_MovementBuff;      // 本回合移动增益
steps -= Turn_MovementDebuff;    // 本回合移动减益

// perk（天赋）修正：perk 1 / 6 / 2 / 12 各有一段
// item33（道具33）、遗物 R4_Relic_01 也各有一段
```

**这套"骰子 → 保底 → 减半 → 加 buff 减 debuff → 天赋 → 道具 → 遗物"的链条，
是可以直接平移到尖塔的修正器/遗物体系里的。**

### 3.3 走路的两种方式

| 方法 | 行为 | 消耗 |
|---|---|---|
| `Move()` | 协程，逐格动画走 `steps` 格 | 一次性走完 |
| `MoveOneTile()` | **精确走一格** | 花 1 行动点（`ActionPointCount`） |

环形路线，走到底会绕回去：

```csharp
routePosition %= currentRoute.movementNodeList.Count;
```

踩到 `StartPoint` 或 `Boss` 时**强制停下**（哪怕骰子还剩步数）。

### 3.4 路线数据：`Route.cs` 极简

路线就是一堆空物体（`Transform` 子节点）按顺序排成的列表：

```csharp
public class Route : MonoBehaviour {
    public List<Transform> movementNodeList = new List<Transform>();
    private void FillNodes() {
        childObjects = GetComponentsInChildren<Transform>();   // 子节点即路径点
        ...
    }
}
```

**这意味着棋盘可以是任意形状** —— 只要摆一排空物体当路径点，直的、拐弯的、闭合的都行。
对纯 C# 生成 UI 棋盘来说，这个抽象非常好抄。

### 3.5 格子怎么触发：靠 Unity **tag**，不是类型判断

```csharp
// BoardPlayerManager.cs（214 行）核心
private void OnTriggerStay(Collider other)
{
    switch (other.tag) {
        case "Chest":     /* 开箱 */           break;
        case "Wood":      /* 采木头 */         break;
        case "Stone":     /* 采石头 */         break;
        case "Card":      /* 抽卡 */           break;
        case "Event":     /* 随机事件 */       break;
        case "Sacrifice": /* 献祭 */           break;
        case "Dig":       /* 挖矿 */           break;
        case "Dungeon":   /* 进地牢，需 ActionPointCount > 1 */ break;
        case "Gambler":   /* 小游戏 */         break;
        case "Shop":      /* 商店 */           break;
        case "Devil":     /* 恶魔交易 */       break;
        case "Merchant":  /* 行商 */           break;
        case "Goblin":    /* 哥布林商人 */     break;
        case "Dwarf":     /* 矮人商人 */       break;
        case "Enemy":     /* 进战斗 */         break;
    }
}
```

**如果用 `HeroPathSelect.ThiefPath`（盗贼路线），所有商店类格子会被关掉。**
—— 这是"路线/职业影响棋盘构成"的例子，尖塔里的角色差异可以照这个思路做。

---

## 四、格子类型（14 种）与生成上限

`TileCreator.TileObjectType` 枚举：

```
Null, Chest, Enemy, Food, Trap, Card, Event, Wood, Stone,
Breakable, Shop, Devil, Sacrifice, Dig
```

`BoardManager.InitializeMaxCounts()` 里给每类格子定了**随机上限**
（`Random.Range(a, b)` 整数版，上限取不到）：

| 类型 | 上限范围 | 类型 | 上限范围 |
|---|---|---|---|
| Breakable（可破坏物） | 4–5 | Trap（陷阱） | 2–4 |
| Chest（宝箱） | 1–3 | Wood（木头） | 3–5 |
| Enemy（敌人） | 3–5 | Stone（石头） | 3–5 |
| Food（食物） | 3–5 | Shop（商店） | 1 |
| Card（卡牌） | 1 | Devil（恶魔） | 1 |
| Event（事件） | 1 | Sacrifice（献祭） | 1 |
| | | Dig（挖矿） | 1 |

生成流程：`BoardManager.NewBoard()` 设 `isResetTiles = true`
→ `Update()` 里先 `ResetCurrentCounts()` 再逐个 `SetTile()`。
**Boss 棋盘激活时（`BossBoardManager.BossBoardActive`）会跳过这套生成。**

---

## 五、区域机制（Region 1–8）：棋盘会换规则

`RagionMachanicSystem.cs`（原文拼写有误，是 Region）—— 每个区域给玩家挂一个
**倒计时式的负面状态条**，走一步掉一格：

| Region | 计数器 | 归零/满值时发生什么 |
|---|---|---|
| 2 | `region_2_Count` 0–3 | 满 3 → `boardPlayer_Poisoned = true`，每回合掉 `ceil(最大血/10)` |
| 3 | `region_3_Count` | 20% 概率刮起 `frostWind`（寒风），持续 3 |
| 4 | `region_4_Count` 0–10 | 归零 → `playerThirst = true`（口渴） |
| 5 | `region_5_Count` 0–20 | 满 20 且非商人路线 → `playerBurn = true`（灼烧） |
| 6 | `region_6_Count` 0–10 | 归零 → `playerBreak = true`（力竭） |
| 8 | — | **Boss 棋盘**，见下节 |

> 这套东西本质上就是**"Act 主题机制"** —— 尖塔里三幕各有各的味道，
> Boardlike 用 8 个区域 + 一个持续消耗的资源条来实现，成本很低，效果明确。
> 想给尖塔加"幕机制"，这是最省事的抄法。

---

## 六、★ Boss 棋盘：不是回合制打架，是「洗牌博弈」

`BossBoardManager.cs`（1,568 行，第二大的文件）—— 这是整个游戏设计上最妙的部分，
也是**最值得抄进尖塔 Boss 战**的部分。

### 6.1 基本设定

```csharp
public const int BossRegionID = 8;
public float bossMaxHP = 1200f;
public int attackTileCount = 6;      // 一次标记 6 格
public float attackDamage = 45f;     // 站标记格里吃 45 固定伤害
```

**Region 8 上只有三种格子：Food / Trap / Null，卡牌会刷在 Null 格上。**

### 6.2 Boss 每回合二选一

```csharp
public enum BossAction { Attack, Shuffle }

// 权重：attackWeight = 45, shuffleWeight = 35
// 约束：forceShuffleCardThreshold = 2  → 场上可用卡 < 2 张时强制洗牌（防死锁）
//        maxSameActionStreak = 2       → 同一动作最多连着出 2 次
```

**Attack（攻击）**
1. 随机标 6 格（`MarkAttackTiles()`）
2. 出标记 → `attackImpactDelay = 0.5s` **之后才结算伤害**
3. 关键：这 0.5 秒里**骰子是锁死的**（`DiceLocked`），玩家看着砸下来也跑不掉

**Shuffle（洗牌）**
1. 除了玩家脚下那一格，其余全部重排
2. 重新铺 `foodCount=4` / `trapCount=5` / 卡牌 `Random.Range(5, 9)`
3. 逐格 `tileStagger = 0.02s` 波浪式刷新

### 6.3 打 Boss 的唯一手段是「踩卡」

卡是唯一能对 Boss 造成伤害的东西。于是形成博弈：

```
Boss 洗牌 → 场上冒出卡 → 你掷骰子去够那张卡 → 踩到就打 Boss
                     ↘ 但同时可能洗出陷阱，也可能 Boss 下一回合就 AOE 砸你站的格子
```

**这个"棋盘会变、你得追着资源跑，同时躲 AOE"的循环，
比站桩对轰有意思得多，而且尖塔的 Boss 战完全可以套。**

---

## 七、卡组 / 战斗 / 传承（外围系统）

| 系统 | 文件 | 要点 |
|---|---|---|
| 卡组构建 | `DeckBuildManager.cs`（127 行） | `allCardsList`（卡库）/ `allCardsList_Deck`（当前卡组）/ `handCount`（手牌上限） |
| 战斗 | `Battle.cs`（4,714 行） | 全游戏最大的文件，逻辑全堆一起 |
| 战斗调度 | `BattleManager.cs`（619 行） | `StartBattle()` / `SpawnEnemies()` / `BattleWin()` / `LeaveBattle()`；`EnemyDesicion()` 敌人 AI，`SneakAction()` 潜行 |
| 传承天赋树 | `LegacyTreeManager.cs`（86 行） | **`nodeCount = 160`**，存 `legacy.json` 两个 bool 数组：`unlocked[]` / `ready[]` |
| 英雄属性 | `HeroStats.cs`（791 行） | 血/攻/防/暴击/闪避，每一项都拆成 `_Base` + `_Perk` + `_Items` 三段再相加 |
| 地牢 | `DungeonManager.cs`（679 行）+ `DungeonTile.cs`（502 行） | 棋盘之外的另一套格子体系 |

**`HeroStats` 的"基础值 + 天赋值 + 装备值"三段拆分**
是标准的属性管线写法，尖塔里做自定义属性也能这么拆。

---

## 八、★ 移植到尖塔 2：路线判断与方案

### 8.1 先定路线：走 `ModifierModel`，**不是**主菜单按钮

按 `sts2-ritulib-modding` 技能 §3 的判断表：

| 你要的 | 走哪条 | 本次 |
|---|---|---|
| 在尖塔框架里换个玩法（**要**地图/战斗/房间，只改规则） | `ModifierModel` | ✅ **就是这个** |
| 跟尖塔无关的独立小游戏（**不要**地图/怪物/房间） | 主菜单按钮 | ❌ |

掷骰子走路需要：棋盘（=地图）、格子触发的战斗（=怪物/房间）、跨局存档（=run）。
**三样都要 → 必须寄生在一次 run 上 → 走 `ModifierModel`。**

```csharp
[RegisterGoodModifier(ModifierListSortOrder = -100)]
public sealed class DiceWalkModifier : ModifierModel
{
    public override bool ClearsPlayerDeck => false;
    protected override void AfterRunCreated(RunState runState) { /* 新局 */ }
    protected override void AfterRunLoaded(RunState runState)  { /* 读档 */ }
}
```

⚠️ 注意 `AfterRunCreated` 跑在建局半路上，那时房间还没建、场景树可能没就位
→ **别在里面直接开 UI**，只记旗子，等 `RunManager.Instance.RoomEntered` 再真开。

### 8.2 棋盘怎么画：§4 的纯 C# Godot 控制树，**不需要 pck**

照 `QuestPanel.cs` 的写法，直接挂场景树根部：

```csharp
public sealed partial class DiceBoardPanel : Control {   // 必须 partial
    public override void _Ready() {
        ProcessMode = ProcessModeEnum.Always;   // 暂停也照跑
        ZIndex = 200;
        MouseFilter = MouseFilterEnum.Stop;     // 挡住下面的地图
        SetAnchorsPreset(LayoutPreset.FullRect);
    }
}
```

★ **必须"借"游戏的字体**，否则中文全是豆腐块（Godot 默认字体不含 CJK）。

### 8.3 格子 → 尖塔房间的映射表

| Boardlike 格子 | 尖塔 2 对应房间 | 备注 |
|---|---|---|
| `Enemy` | 普通战斗 | 直接映射 |
| `Dungeon` | **精英战斗** | Boardlike 里要求行动点 > 1 |
| `Chest` | 宝箱房 | |
| `Card` | 卡牌奖励 | |
| `Event` | 事件房 | |
| `Shop` / `Merchant` / `Goblin` / `Dwarf` | 商店 | 四种商人可合并成一种 |
| `Food` | 休息（回血） | |
| `Sacrifice` / `Devil` | 恶魔交易 | 尖塔 2 有 Devil 房 |
| `Trap` | 负面格（掉血/上诅咒） | 尖塔原生没有，需自建 |
| `Boss` | Boss 房 | 走完一圈触发 |
| `StartPoint` | 起点 | |
| `Wood` / `Stone` / `Dig`（采集） | ❌ 尖塔没有 | 建议砍掉，或换成金币 |
| `Breakable`（可破坏物） | ❌ 可换成金币堆 | |
| `Gambler`（小游戏） | ❌ 可选 | 想做可以做成"赌博事件" |

**结论：14 种格子里有 10 种能直接对上尖塔原生房间，只有采集类和赌博是尖塔没有的。**
→ 移植阻力很小。

### 8.4 三个方案，按工作量排序

**方案 A（轻，1–2 天）—— 骰子只决定「走几步」，棋盘用尖塔地图**
- 保留尖塔的分支地图，但把"玩家点节点"换成"掷骰子走 N 步"
- 问题：尖塔地图是**分支**的，"走 N 步"没有唯一定义（岔路怎么算？）
- 结论：看着省事，实际会卡在分支逻辑上，**不推荐**

**方案 B（中，3–5 天）—— 棋盘覆盖层，推荐** ⭐
- `ModifierModel` 寄生在 run 上
- 用纯 C# 画一个**环形棋盘**覆盖在地图上（照 `Route.cs` 的路径点抽象）
- 掷骰子 → 走 N 格 → 踩到的格子**翻译成尖塔的房间**再进
- 尖塔的战斗、卡牌、遗物、金币系统**全部原样保留**，只换掉"选房间"这一步
- 骰子修正链直接接尖塔的遗物/修正器：`steps = dice + buff - debuff`

**方案 C（重，1–2 周）—— 真 3D 棋盘 + 棋子动画**
- 需要场景、模型、摄像机、寻路
- 视觉最好但工作量翻几倍
- 建议**等 B 跑通再考虑**

### 8.5 Boss 战：把「洗牌博弈」搬过去（最有价值的一块）

方案 B 跑通后，Boss 战可以直接抄 Boardlike Region 8：

```
Boss 回合开始
  ├─ 45% Attack：标 6 格 → 0.5s 后结算（期间锁骰子）
  └─ 35% Shuffle：重排除玩家脚下外的所有格
       （场上卡 < 2 张时强制 Shuffle，防死锁；同动作最多连出 2 次）
```

映射到尖塔：格子内容换成「能量 / 药水 / 卡牌 / 陷阱 / 空格」，
踩到卡牌格才能对 Boss 出牌。**尖塔 Boss 战的流程感会完全不一样。**

### 8.6 区域机制 → 尖塔「幕机制」

抄 `RagionMachanicSystem`：每幕挂一个持续消耗的资源条，
走一步掉一格，归零/满值触发负面。成本极低，但能让三幕手感明显不同。

---

## 九、可以直接抄的代码片段清单

| 想做什么 | 抄哪个文件 | 关键行 |
|---|---|---|
| 掷骰子 + 步数修正链 | `PlayerMovement.cs` | `RollDice()`（`Random.Range(2,5)`）；`MaxDice_Buff` / `DiceHalved` / `DiceMin` 修正链 |
| 环形路径 + 逐格走 | `PlayerMovement.cs` | `Move()` / `MoveOneTile()`；`routePosition %= movementNodeList.Count` |
| 路径点抽象 | `Route.cs` | `movementNodeList` = 一堆空物体子节点 |
| 踩格子触发 | `BoardPlayerManager.cs` | `OnTriggerStay` 按 tag 分派（14 个 case） |
| 格子类型 + 生成上限 | `TileCreator.cs` / `BoardManager.cs` | `enum TileObjectType`（14 种）；`MaxCounts` 字典 |
| Boss 洗牌博弈 | `BossBoardManager.cs` | `BossTurn()` / `ShuffleBoard()` / `MarkAttackTiles()` |
| 区域/幕机制 | `RagionMachanicSystem.cs` | `CheckRegions()` / `RegionalActions()` |
| 属性三段拆分 | `HeroStats.cs` | `xxx_Base` + `xxx_Perk` + `xxx_Items` |
| 传承天赋树存档 | `LegacyTreeManager.cs` | `nodeCount = 160`，`legacy.json` 两个 bool 数组 |

---

## 十、一句话结论

**Boardlike 是 Mono 编译，源码是完整可读的 —— 这次不用靠猜。**
它的「掷骰子走路」核心只有四个组件：骰子（2–4 点）、环形路径点、按 tag 分派的格子触发、
每类格子的生成上限。**14 种格子里 10 种能直接对上尖塔原生房间**，移植阻力很小。

做尖塔 2 mod 的话：走 **`ModifierModel`**（要地图/怪物/房间 → 必须寄生在 run 上），
棋盘用**纯 C# 的 Godot 控制树**画覆盖层（不需要 pck），
先做方案 B，跑通了再把 Boss 的**洗牌博弈**搬过去 —— 那才是这个游戏设计上真正值钱的部分。
