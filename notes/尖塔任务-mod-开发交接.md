# 尖塔任务 / Quest Spire — Mod 开发交接文档

> 给后续接手的 AI / 开发者。目标：读这份就能继续改，不必从零考古。
> 最后更新：2026-09-22 夜。当前版本：0.1.0。状态：**编译通过、已部署，未经真人进局实测**。
> 本轮（20:59~22:01）新改：任务进度可存档（`[SavedProperty]`）、修掉「毫发无伤」串场 bug、
> 读档不再重发任务册、任务库 25 → 42（14 个一代移植任务 + 3 个梗任务），
> 奖励从「只有金币」扩到「遗物 / 卡牌 / 随身物」+ 完成提示。设计案见 `notes/尖塔任务-梗任务设计案.md`。

---

## 0. 这是什么

《杀戮尖塔2》(Slay the Spire 2, Steam appid 2868840) 的一个客户端 mod：给游戏加一个**任务系统**。
- 开局和每次进商店，玩家可以**主动点击**接取任务（不自动弹窗）。
- 做任务（累计各类行为）达标后**自动发金币**。
- 右上角常驻**任务面板**：可拖动、可折叠、显示每条任务的简短描述 + 进度条。
- 灵感来自一代《杀戮尖塔》的 SpireQuests mod，但机制全部改成二代能统计的行为重做。

---

## 1. 工程与构建

- 工程目录：`C:\Users\有花无实\Documents\Default Project\projects\quest-spire`
  - `QuestSpire.csproj` + `Directory.Build.props/local.props`（引游戏 dll 路径）
  - mod 清单 `QuestSpire.json`：`has_pck:false` + `has_dll:true`（**纯 dll、无 pck 也能被加载**，已验证）
  - 代码在 `QuestSpireCode/`：
    - `Entry.cs` —— 入口 `[ModInitializer]`，订阅 `RunStarted` 发任务册 + 挂面板 + `ArmRunStart`
    - `Quests/QuestModel.cs` —— `QuestKind` 枚举 / `QuestDef`(record) / `QuestState`(进度)
    - `Quests/QuestCatalog.cs` —— **所有任务定义**（改数值/加任务改这里）
    - `Quests/QuestRunner.cs` —— 本局任务状态机（上限、进度、候选）
    - `Relics/QuestBookRelic.cs` —— 任务册遗物，**所有游戏钩子都在这**（加新目标类型改这里）
    - `Relics/QuestRelicPool.cs` —— 独立遗物池（不挂角色）
    - `UI/QuestPanel.cs` —— 常驻侧栏（可拖动/折叠/描述/进度条 + 「接取任务」按钮）
    - `UI/QuestSelection.cs` —— 选任务浮层（开局 & 商店，点按钮才弹）
    - `UI/UiKit.cs` —— 借游戏字体、找 `SceneTree.Root`、`MetaTag` 标记本 mod 控件
- 依赖：`STS2-RitsuLib`（AutoRegistration 等）。
- 构建：在工程目录 `dotnet build`。**产物 `QuestSpire.dll` 自动(copy)到**
  `D:\software\steam\steamapps\common\Slay the Spire 2\mods\QuestSpire\QuestSpire.dll`。
- **改了 dll 必须重启游戏才生效**（Godot 只在启动时加载 dll）。
- 日志：`%APPDATA%\SlayTheSpire2\logs\godot*.log`，搜 `QuestSpire`。确认是当前会话要看文件 `LastWriteTime`。

---

## 2. 架构要点（改之前先懂这几条）

- **任务册遗物** `[RegisterRelic(typeof(QuestRelicPool))]`，`QuestBookRelic : ModRelicTemplate`。
  它**不挂任何角色**也能注册（会有良性 WARN「relic 不在任何 pool」，不影响 `RelicCmd.Obtain`）。
  每局开始由 `Entry.OnRunStarted` → `RelicCmd.Obtain<QuestBookRelic>(state.Players[0])` 发给主角。
- **钩子即事件源**：`AbstractModel`（遗物基类）暴露大量 `Before/After*` 钩子，遗物重写它们就能被动收到游戏事件，无需 Harmony。所有目标计数都在 `QuestBookRelic` 里累加到 `QuestRunner`。
- **进度模型**（`QuestState`）：只有三种推进方式——
  - `Add(n)` 累加（大多数任务）
  - `SetAbsolute(v)` 取快照、只增不减（金币 `GoldHoard`、房间种类 `RoomTypesSeen`）
  - `Reset()` 每回合清零（单回合类的 `AttacksInOneTurn`）
  达标 `Completed`，`QuestRunner.AddProgress/SetProgress` 返回“本次刚好完成”的队列 → 遗物 `Reward()` 里 `PlayerCmd.GainGold`。
- **候选是随机的**：`QuestCatalog.PickOffers(n, exclude)` 用 `Random.Shared` 打乱、排除进行中/已完成。选任务浮层每次弹只是从候选里挑几个显示，**不是列全部**。
- **接取时机=点击**：`QuestSelection` 用 `PickCtx{None,RunStart,Shop}` 当闸门；
  `Entry` 开局调 `ArmRunStart()`（点亮按钮，不弹）；`QuestBookRelic.AfterRoomEntered` 里
  进商店 `OnShopEntered()`、进非商店 `OnOtherRoom()`（只收回“商店那次”）。面板按钮 `Pressed → QuestSelection.OpenPicker()`。
  候选在进入那一刻锁定在 `_pending`，中途关掉再开是同一批（**防刷新刷任务**）。
- **存档桥（新增）**：`QuestRunner` 还是静态内存，但**不再是唯一的家**。任务册遗物上有一条
  `[SavedProperty] public int[] QuestData { get => QuestRunner.Export(); set => QuestRunner.Import(value); }`。
  这是游戏自带机制：`RelicModel.ToSerializable()` 调 `SavedProperties.From(this)`（读 getter，存进本局存档的
  `props.ints`），`RelicModel.FromSerializable()` 调 `SavedProperties.Fill()`（走 setter 灌回来）。
  ⚠️ **以后往 `QuestRunner` 加任何新的状态字段，必须同步进 `Export/Import`**，否则只活在内存里、读档即丢。
  ⚠️ 属性名会进 `ModelIdSerializationCache`（日志里 `ModelIdSerializationCache initialized` 必须出现在
  mod 注册之后——目前是对的），改名等于换存档格式。
- **新局 vs 读档**：`Entry.OnRunStarted` 用 `state.Players[0].GetRelic<QuestBookRelic>()` 判断——
  拿到 null = 新局（`ResetForRun` + 发册 + `ArmRunStart` 给开局选任务）；拿到实例 = 读档继续
  （不重置、不重发、**不给开局选任务**，否则读档能反复刷任务）。`QuestRunner.RestoredFromSave` 用来兜底
  「老存档里没有 QuestData」的情况。
- **侧栏挂载兜底**：读档进局不一定触发 `RunStarted`，所以另外订阅了 `RunManager.Instance.RoomEntered →
  Entry.EnsureUi()`，进任何房间都会确认侧栏挂着（已挂着就什么都不做）。

---

## 3. 已验证可用的游戏 API（别再猜，直接用）

> 全部经 `ilspycmd` 反编译 + 编译通过验证。若要用没列出的，先反编译确认（见 §5）。

**命名空间**
- 卡牌/枚举：`MegaCrit.Sts2.Core.Entities.Cards`（`CardModel` `CardType` `CardRarity` `CardEnergyCost` `PotionModel` `CardPlay`）
- 生物：`MegaCrit.Sts2.Core.Entities.Creatures`（`Creature`）
- 玩家：`MegaCrit.Sts2.Core.Entities.Players`（`Player`）
- 商店：`MegaCrit.Sts2.Core.Entities.Merchant`（`MerchantEntry`）
- 房间：`MegaCrit.Sts2.Core.Rooms`（`AbstractRoom` `CombatRoom` `RoomType`）
- 数值/伤害：`MegaCrit.Sts2.Core.ValueProps`（`ValueProp`）、伤害结果 `DamageResult`（有 `.TotalDamage`）
- 遗物：`MegaCrit.Sts2.Core.Entities.Relics`（`RelicRarity`）

**对象字段/属性**
- `Player.Gold`(int)、`Player.Deck`(整局卡组 `CardPile`)、`Player.RunState`(IRunState)
  - ⚠️ **2026-09-22 更正**：`Player.Deck` 是**整局卡组**，不是战斗抽牌堆。`PileType` 的注释写明
    `Deck` = 「房间之间卡牌待的地方，开战时会把这里的牌复制进抽牌堆」，战斗抽牌堆是 `PileType.Draw`。
    旧版交接文档写反了。**但这一条还没在游戏里复核过**，用之前先实测确认一次。
- `Creature.CurrentHp`(int)、`Creature.MaxHp`(int)
- `CardModel.Type`(CardType: Attack/Skill/Power…)、`CardModel.Rarity`(CardRarity: Common/Uncommon/Rare/…)、`CardModel.EnergyCost.Canonical`(int 能量费)
- `AbstractRoom.RoomType`(RoomType)：全部成员为 `Unassigned / Monster / Elite / Boss / Treasure / Shop /
  Event / RestSite / Map`——**地图上的 `?` 房就是 `RoomType.Event`**（已确认，不用再猜）。
- 二代本体**有 `MegaCrit.Sts2.Core.Models.Cards.Claw`**（做「打出 Claw N 次」类任务可用）。

**已在本 mod 用过的 AbstractModel 钩子（签名照抄即可）**
- `AfterDamageGiven(PlayerChoiceContext, Creature? dealer, DamageResult, ValueProp, Creature target, CardModel? cardSource)`
- `AfterBlockGained(Creature, decimal amount, ValueProp, CardModel? cardSource)`
- `AfterCardPlayed(PlayerChoiceContext, CardPlay)` （`cardPlay.Player` / `.Card`）
- `AfterCardExhausted(PlayerChoiceContext, CardModel, bool causedByEthereal)`
- `AfterPlayerTurnStart(PlayerChoiceContext, Player)`（**没有 `AfterPlayerTurnEnd`**，回合结束用 `AfterSideTurnEnd` 或在下一次 `AfterPlayerTurnStart` 结算上一回合）
- `AfterHandEmptied(PlayerChoiceContext, Player)`
- `AfterCurrentHpChanged(Creature, decimal delta)`（delta<0 即掉血）
- `AfterPotionUsed(PotionModel, Creature? target)`
- `AfterItemPurchased(Player, MerchantEntry, int goldSpent)`
- `AfterRestSiteHeal(Player, bool isMimicked)` / `AfterRestSiteSmith(Player)`（篝火：休息/打造）
- `AfterGoldGained(Player)`
- `AfterCombatVictory(CombatRoom)` / `AfterCombatEnd(CombatRoom)`
- `AfterRoomEntered(AbstractRoom)`（进任何房间，含无战斗房；判 `room.RoomType`）

**其它存在的、可能有用的钩子（尚未用）**：`AfterEnergySpent(CardModel,int)`、`AfterRewardTaken(Player, Reward)`、`AfterCombatRewardOffered(RewardsSet, CombatRoom)`、`AfterActEntered`、`AfterMapGenerated(ActMap,int)`、`AfterForge(...)`、`AfterModifyingGoldGained(Player, decimal)`、`BeforeSideTurnEnd*`/`AfterSideTurnEnd*(CombatSide,...)`、`AfterFlush(...)`、`AfterOrbChanneled(...)`、`AfterCardDiscarded/Drawn(...)`、`AfterCardChangedPiles(CardModel, PileType, AbstractModel?)`。

**已用（2026-09-22 二批新增）**：
- `AfterOrbEvoked(PlayerChoiceContext, OrbModel orb, IEnumerable<Creature> targets)`（充能球类任务）
- `AfterDamageGiven` 里的 `DamageResult` 字段：`BlockedDamage` / `UnblockedDamage` / `TotalDamage` / `OverkillDamage` / `WasTargetKilled` / `Receiver`（`MegaCrit.Sts2.Core.Entities.Creatures.DamageResult`）
- `IRunState.CurrentActIndex`（一幕=0、二幕=1）、`IRunState.ActFloor`
- `Creature.IsAlive` / `IsDead`
- `CardRarity` 全部成员：`None / Basic / Common / Uncommon / Rare / Ancient / Event / Token / Status / Curse / Quest`
- `CardType` 全部成员：`None / Attack / Skill / Power / Status / Curse / Quest`
- `Player.Deck.Cards`（`CardPile.Cards`），另有 `CardPile.GetCards(player, params PileType[])` 帮手
- `MegaCrit.Sts2.Core.Models.Cards.Claw`（二代本体就有 Claw）

**关键框架用法**
- 发遗物：`await RelicCmd.Obtain<TRelic>(player)`（try/catch，别在初始化阶段发，等 RunStarted）。
- 发金币：`await PlayerCmd.GainGold(int, Player)`。
- 找游戏根节点做浮层：`UiKit.Root` = `SceneTree.Root`；浮层 `ProcessMode=Always`、`ZIndex` 调高、`SetMeta(UiKit.MetaTag,true)` 以便借字体时排除自己。
- 借游戏主题字体：`UiKit.GameFont()` 遍历树找一个**没打 MetaTag** 的 `Label` 取其 `GetThemeFont("font")`。
- **让模型状态进存档**：在 `AbstractModel` / `RelicModel` / `CardModel` 子类的**属性**上打 `[SavedProperty]`
  （`MegaCrit.Sts2.Core.Saves.Runs`）。支持的属性类型：`int` / `int[]` / 枚举 / 枚举[] / `bool` / `string` /
  `ModelId` / `SerializableCard` / `List<SerializableCard>`；其它类型会在存档时抛 `JsonException`。
  读档是按属性名反射 `SetValue`，**setter 会被调用**，所以可以把“读写存档”直接写成属性的 get/set。
  入口：`RelicModel.ToSerializable()` / `RelicModel.FromSerializable(SerializableRelic)`。
- **查玩家身上有没有某个遗物**：`player.GetRelic<T>()`（`Player` 上的泛型方法，没有则返回 null）。
- **判断当前是否在战斗中**：`MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress`。
- **只有 `RunStarted` 这一个局级事件**（`RunManager` 上另有 `RoomEntered / RoomExited / ActEntered`）：
  新局和读档继续都走 `RunManager.Launch()` → `RunStarted`，所以要靠「玩家身上有没有任务册」自己区分两条路。

---

## 4. 当前功能与状态

- 任务上限 `QuestRunner.MaxActive = 5`。
- 任务库 **42 个**（`QuestCatalog.All`）：见文件内注释，每行一个 `new(QuestKind, 中文标题, 简短描述, 目标, _rewardGold[, _rewardLabel])`。
  新增自一代可做的部分，改造为二代可统计行为（例：「加入某费用牌到卡组」改成「**打出**该费用牌 N 张」）。
  - ⚠️ **新增 `QuestKind` 一律追加在枚举末尾**：存档里存的是 `(int)Kind`，插在中间会让老存档串位。
- 奖励：大多数是金币，另有 3 个梗任务是遗物 / 卡牌 / 「随身物」（见 §4.3）。
- 面板：可拖动（按住空白拖，拖后不再自动贴角）、可通过收起按钮折叠、显示描述 + 进度条 + 「接取任务」按钮（不可用时置灰）；
  拿到「随身物」后面板下方会多出「随身物」一栏。
- 选任务：开局与商店均**点击**触发；候选随机、每店/每局锁定不刷新。

### 4.1 本轮修复（2026-09-22 晚）

- **「毫发无伤」串场**：`_dmgThisCombat` 原来只在 `AfterCombatEnd` 清零，事件房掉的血会被算进下一场战斗，
  导致本来无伤的战斗拿不到进度。现在 ① `AfterCurrentHpChanged` 里用 `CombatManager.Instance.IsInProgress`
  判断「只在战斗中」才置位；② `AfterRoomEntered` 里只要是 `CombatRoom` 就把 `_dmgThisCombat / _turnCards /
  _sawFirstTurn` 全部清干净。`AfterCurrentHpChanged` 本身仍然照常累计「硬抗到底」，不受影响。
- **`AfterCardExhausted` 加了 `card.Owner == Owner` 判断**：原来不认人，别人（敌人效果）消耗的牌也会记进度。
- **存档持久化**：见 §2「存档桥」。

### 4.13 详情页那把锁：真凶是「遗物不属于任何池子」（2026-09-22 深夜十二批）

补完 pck 和三张图之后，用户重启游戏实测：**遗物栏图标正常了、奖励发放和提示全对**，
但**打开详情仍然是一把锁**（「这个遗物需要在时间线中解锁才会在未来的游戏中出现」）。
所以 4.12 里「缺大图 → 回退成锁」那条只解释了一半，**详情页的锁另有原因**。

**真凶**：RitsuLib 从第一次启动就在反复警告：

```
relic 'RELIC.QUEST_SPIRE_RELIC_QUEST_DEAD_BRANCH_RELIC' is registered in ModelDb
but is not contained in any relic pool. Skipping it while building energy icon overrides.
```

我们的 `QuestRelicPool` **只是个普通自定义池，没登记成「共享遗物池」**。
而 RitsuLib 的补丁 `AllRelicPoolsPatch` 写得很明白：

```csharp
// Append registered shared relic pools to ModelDb.AllRelicPools
ModelDbContentPatchHelper.Append(ref __result, ModContentRegistry.AppendSharedRelicPools);
```

→ **只有挂 `[RegisterSharedRelicPool]` 的池子才会被追加进 `ModelDb.AllRelicPools`。**
我们的池子不在里面，游戏就认为「这些遗物不属于任何池子」→ 详情页当成未解锁的遗物 → 显示锁。

**修法**：给 `QuestRelicPool` 挂上 `[RegisterSharedRelicPool]`（`STS2RitsuLib.Interop.AutoRegistration`）。

**副作用核查过，不会让遗物变成随机掉落**：
RitsuLib 只改了 `ModelDb.AllRelicPools`；`ModelDb.AllSharedRelicPools` 是游戏的**固定数组**，
源码里没有任何补丁碰它（grep 过 RitsuLib 全仓，0 处引用）→ 随机奖励拿不到我们的遗物。
（对照：`moreRelics` 那些 mod 用的是**另一条路**——`[Pool(typeof(SharedRelicPool))]` 把遗物塞进
**原生**共享池，所以它们的遗物是会随机掉的。两种做法不一样，别混。）

**如果重启后详情页还是锁**，说明游戏那边还要一个正经的 Epoch（时间线解锁节点），
下一步就走 RitsuLib 的正路：

```csharp
[RegisterEpoch] [RegisterStoryEpoch(typeof(某Story))] [AutoTimelineSlotAfterColumn(EpochEra.Act1)]
public sealed class 某Epoch : ModEpochTemplate { ... }
// 再 .BindRelicUnlockEpoch<某Epoch>()
```

（那需要额外写 `epochs.json` 本地化 + 时间线槽位，工作量比这次大，所以先试便宜的。）

### 4.12 图标规格：三张图一个都不能少（2026-09-22 实测反馈）

**用户实测反馈**（pck 上线后）：遗物栏里**能看到图了** ✓，但 ——
① 打开详情**仍然是一把锁、没有文字描述**；② 我们的遗物**比正式遗物明显小一节**。

**原因**（日志 + 图片尺寸对照，两条都查实了）：

1. **只给了小图标，没给详情大图**。遗物详情走的是 `BigIconPath`（原版 256×256），
   缺了它就回退成 `locked_model.png`（锁），**连文字也一起被「锁定」文案盖掉**。
   → **铁律：`IconPath` / `IconOutlinePath` / `BigIconPath` 三个都要给。**
2. **一代的图是 128×128 带透明留白，二代是 85×85 贴边**。放进同一个格子一起缩放，
   有留白的那张就显得小一圈。

**修法**：新增 `tools/normalize_icons.py`（可重复跑）：

- 以小图标的**透明边界**为准，同一件遗物的三张图用**同一个裁剪框和缩放比**（保证轮廓/大图对齐），
  规整成 **小图 85×85、大图 256×256**。
- 一代没画大图的（枯木树枝 / 玩具扑翼飞机 / 金偶像），用规整后的小图**放大补一张**——会有点糊，但比锁头强。
- 改完图要跑 `tools/build_pck.ps1` 重新打包，再 `dotnet build` 部署。**pck 是启动时加载的，必须重启游戏才生效。**

**自查办法（这次靠它抓到漏网）**：改完之后扫一遍 dll 里的路径，确认七件遗物的三张图都在：

```powershell
python -c "d=open(r'...\mods\QuestSpire\QuestSpire.dll','rb').read(); print(d.count('golden_idol_big.png'.encode('utf-16-le')))"
```

> 本次 `QuestGoldenIdolRelic` 的 `BigIconPath` 一开始漏写（新建文件时顺手 copy 了 `null`），
> 就是这个自查扫出来的。**每加一件遗物，三张图逐个数一遍。**

### 4.11 素材授权更正 + 第二批自研遗物（2026-09-22 深夜十一批）

**更正一条**：§4.10 里我把一代原画标成了「版权风险、发布前要换掉」。
用户指出 —— **官方公开表示过一代的素材可以用在二代里**，所以直接用是允许的。
（出处待补，别再把这条当版权问题删图。见 §4.10 的说明框。）

既然能用，就把剩下**接口干净的**一代遗物也做成我们自己的，继续减少对 moreRelics 的依赖。

| 新遗物 | 效果 | 用的钩子 | 图标来源 |
|---|---|---|---|
| `QuestGoldenIdolRelic` 金偶像 | 敌人掉落金币 +25% | `ModifyGoldGained`（基类返回原值 → 乘 1.25） | `goldenIdolRelic.png` |
| `QuestOmamoriRelic` 御守 | 抵消接下来 2 张诅咒 | `ShouldAddToDeck`（基类 true → 返回 false 否决） | `omamori.png` |
| `QuestClockworkRelic` 齿轮工艺品 | 战斗第一个回合获得 1 层人工制品 | `PowerCmd.Apply<ArtifactPower>` | `clockwork.png` |

- 御守的「剩几次」用 `[SavedProperty] public int ChargesLeft { get; set; } = 2;`
  —— 默认值靠属性初始化器，读档时由存档覆盖。挡掉一张诅咒时 `Flash()` 并扣一次。
- 齿轮工艺品有个**时序坑**：`PowerCmd.Apply` 要 `PlayerChoiceContext`，而 `BeforeCombatStart()` 没有，
  所以改成「战斗的第一个回合开始时」给（那边有 context）。体感几乎一样。
- 三件的奖励改用 `GrantOwnRelicOrGold<T>(名字, 兜底金币)`：先发自研的，已有/失败就折现。**不再依赖别的 mod。**

**剩余三件仍走旧路，原因写清楚了**（在 `QuestLegacyRelics2.cs` 末尾的注释里）：

- **机械臂 Inserter**（每 2 回合 +1 充能球栏位）：没找到充能球栏位的接口。
- **套娃 Matryoshka**（之后 2 个宝箱双倍）：要改宝箱奖励生成，`RelicReward` 的构造参数没确认。
- **突变酵素 MutagenicStrength**（+3 力量，第一回合结束失去）：要精确移除自己给的那 3 点
  （`PowerCmd.Remove<T>` 会把别处来的力量一起清掉），得先确认力量带不带来源/时长；而且一代 jar 里没这件的大图。

### 4.10 打上 pck 了：真图标 + 正规本地化（2026-09-22 深夜十批）

**先纠正一条以前写错的话**：交接文档开头写过「纯 dll、无 pck 也能被加载（已验证）」——
能加载是对的，但**没有 pck 就加不了任何素材**（图、文本）。之前所有「随身物」「写 AppData」的将就做法，
根子都在这。现在补上了。

#### 现状

```
mods\QuestSpire\
├─ QuestSpire.dll     102 KB
├─ QuestSpire.json    has_pck: true
└─ QuestSpire.pck     48 KB   ← 图标 + 本地化
```

pck 里的东西（导出日志可查）：

```
res://QuestSpire/images/relics/{dead_branch,torii,coffee_dripper,ornithopter}[_outline|_big].png
res://QuestSpire/localization/zhs/relics.json
res://QuestSpire/localization/eng/relics.json
```

#### 怎么重新打包

```powershell
pwsh projects\quest-spire\tools\build_pck.ps1     # 导入素材 + 导出 pck
dotnet build                                      # csproj 的 CopyMod 会把 pck 一起部署
```

- **改了 `QuestSpire/images/` 或 `QuestSpire/localization/` 就要重新打包**；只改 C# 代码不用。
- 打包靠 `export_presets.cfg` 里的 `QuestSpirePck` 预设（`export_filter="all_files"`，
  排除掉 `QuestSpireCode/`、`tools/`、`*.cs` 等源码）。
- ⚠️ `RunPckExport` 这个属性是**空的**（csproj 里声明了但没有任何 Target 用它），别被它误导。
- 素材源文件放在 `QuestSpire/` 子目录里（对应 `res://QuestSpire/`），这是 godot 工程的普通文件夹。

#### 图标：用一代的官方原画

`tools/extract_sts1_relic_art.py` 从**本机的杀戮尖塔1** 里抠出来的
（`D:\software\steam\steamapps\common\SlayTheSpire\desktop-1.0.jar`，图片是明文 PNG）：

| 我们的遗物 | STS1 里的名字 | 拿到的东西 |
|---|---|---|
| 枯木树枝 | `deadBranch` | 小图标 128² + 轮廓（无大图） |
| 鸟居 | `torii` | 小图标 + 轮廓 + 大图 256² |
| 咖啡滤杯 | `coffeeDripper` | 小图标 + 轮廓 + 大图 256² |
| 玩具扑翼飞机 | `ornithopter` | 小图标 + 轮廓（无大图） |

> ✅ **素材授权（2026-09-22 用户指正）**：一开始我把这批图标标成了「版权风险，发布前要换掉」，
> 用户指出 —— **官方公开表示过一代的素材可以用在二代里**，所以直接用一代原画是允许的，
> 不用自己重画、也不用担心发布问题。
> （出处待补：我只搜到「官方确认 modding 会更方便」这类报道，
> [ResetEra 的采访](https://www.resetera.com/threads/1454593/) 被 Cloudflare 挡了没读到原文。
> 下次谁有空去官方 Discord / 公告里找一句原文补在这里，免得以后又被当成版权问题删图。）
> 「尖塔任务册」是本 mod 自己发明的遗物，一代没有对应原画，暂时**借本体的 `dusty_tome` 图标**。

`RelicAssetProfile` 收的是**原始路径**（不像 `ContentAssetProfiles` 会自动补 `res://images/` 前缀），
所以写全：`IconPath: "res://QuestSpire/images/relics/dead_branch.png"`。

#### 本地化：AppData 后门退居二线

pck 里现在有正规的 `res://QuestSpire/localization/<语言>/relics.json`，
游戏合并顺序是「先覆盖目录、后 mod 表」，所以 **pck 里的文本优先**。
`LocInjector` 加了检测：**发现 pck 里的表存在就直接返回，不再往 AppData 写**。
留着它是给「临时关掉 pck 调试」这种情况兜底的。

### 4.9 遗物「显示成锁头」的真正原因（2026-09-22 深夜九批）

用户截图反馈：我们加的遗物在遗物栏里是**一个锁头**，提示写着
「锁定 —— 这个遗物需要在时间线中解锁才会在未来的游戏中出现」。

**日志给出了确凿答案**（不用猜）：

```text
[WARN] AtlasResourceLoader: Missing sprite 'quest_spire_relic_quest_book_relic' in relic_atlas
       (requested: res://images/atlases/relic_atlas.sprites/quest_spire_relic_quest_book_relic.tres)
[WARN] Asset not cached: res://images/packed/common_ui/locked_model.png
```

也就是说：

1. 游戏按 **遗物条目的全小写**（`QUEST_SPIRE_RELIC_QUEST_BOOK_RELIC` → `quest_spire_relic_quest_book_relic`）
   去图集里找图标；我们没 pck，加不进图集 → 找不到。
2. 找不到就**回退成 `locked_model.png`（那个锁头）**，而且**标题和说明也一起换成「锁定」文案**。
   → **「没有图标」和「效果说明显示不出来」是同一个问题**，这是这次最关键的一条。
3. 顺便排除了 RitsuLib 的嫌疑：它的 `IsUnlocked` 对「没登记 epoch 要求的模型」直接 `return true`
   （见 `ModUnlockRegistry`），我们没登记过，所以锁定状态是游戏本体按缺图标判的。

**修法（不用 pck）：借本体已有的图集条目。**
`ContentAssetProfiles.Relic("<本体条目名小写>")` 会拼出本体图集的路径，直接能用：

| 我们的遗物 | 借的本体遗物 | 为什么 |
|---|---|---|
| 尖塔任务册 | `dusty_tome`（尘封古籍） | 都是书 |
| 枯木树枝 | `driftwood`（浮木） | 都是木头 |
| 鸟居 | `lantern`（灯笼） | 日式小物里最近 |
| 咖啡滤杯 | `venerable_tea_set`（古老茶具） | 都是冲泡器具 |
| 玩具扑翼飞机 | `paels_wing`（佩尔的翅膀） | 都带翅膀 |

本体条目名是**全大写下划线**，从存档/分析文件里核过拼写
（`DUSTY_TOME` 23 次、`DRIFTWOOD` 17 次、`LANTERN` 401 次、`VENERABLE_TEA_SET` 104 次、`PAELS_WING` 9 次）。

**⚠️ 图标只是借来顶位的，画风是错的。** 想要真原画只有两条路：

1. **给 mod 打 pck** —— 只有打了 pck 才能往 `res://` 图集里加自己的贴图
   （现有的一代原画在 STS1 的 `desktop-1.0.jar` 里，或 moreRelics 的 pck 里）。
   顺便还能把本地化从「写 AppData」改回正规的 `res://<ModId>/localization/...`。
2. 用户给图（按他一贯的习惯），我们放进 pck。

**另外**：用户明确说了「这些本来是一代的遗物，你没必要跑（画图）」——
一代遗物的原画是现成的，不要重画。已停手，一张都没生成。

### 4.8 自研遗物 + 本地化注入 + B 站梗（2026-09-22 深夜八批）

任务库 60 → **61**。这一批解决了一个**结构性问题**。

#### ★ 关键突破：纯 dll 的 mod 也能给自己造的遗物起中文名

以前认定「纯 dll 的 mod 造不出带中文名的遗物」，所以奖励里的船/枯枝都只能做成面板里的「随身物」。
**这个限制其实有官方后门**（`LocManager.LoadTablesFromPath` / `TryLoadOverrideFile`）：

```text
%APPDATA%\SlayTheSpire2\localization_override\<语言>\<表名>.json
（即 user://localization_override/...）
```

- 游戏启动时把这里的 JSON **合并**进对应语言的本地化表。
- `TryLoadOverrideFile` **只校验 smart-format 格式串，不要求 key 已存在** —— 可以直接塞新 key。
- **mod 初始化早于本地化加载**，所以 mod 在 `Initialize()` 里写完，当次启动就生效。

→ 新增 `LocInjector.cs`：在 `Entry.Initialize()` 里把本 mod 遗物的 `.title/.description/.flavor`
写进去。**先读再合并**，解析不了就整个跳过（绝不覆盖玩家/译者自己放的文件）。
Entry 名不硬编码：用 `ModelDb.GetId(typeof(某遗物)).Entry` 在运行期问游戏。

#### 于是我们自己造了 4 件遗物（不再依赖别人的 mod）

| 类 | 名字 | 效果 |
|---|---|---|
| `QuestDeadBranchRelic` | 枯木树枝 | 每当你消耗一张牌，将一张随机牌加入手牌 |
| `QuestToriiRelic` | 鸟居 | 战斗开始时获得 8 点格挡 |
| `QuestDripperRelic` | 咖啡滤杯 | 每场战斗的第一个回合 +1 能量 |
| `QuestOrnithopterRelic` | 玩具扑翼飞机 | 每场战斗开始时回复 4 点生命 |

**顺带把「任务册」自己也补上了名字/描述**（它一直显示内部 key）。

奖励优先级改成：**先发我们自研的那件** → 没有自研版的（金偶像/套娃/御守/突变酵素/机械臂/齿轮工艺品）
才去别的 mod 里按 ID 找 → 再不行折现金币。
`GrantOwnRelic<T>(名字, fallback)` 负责这件事：已有则折现 150，发放异常则退回「随身物」效果，绝不静默失败。

⚠️ 缺的只有图标：`RelicAssetProfile.Empty`，遗物栏里会是空白格。**等用户给图**（别自己造美术）。

#### 「莽夫之道」的漏范围 bug（实测反馈）

用户反馈「感觉这个不止对伤害生效」——查证属实：`ModifyBlockMultiplicative` 对**所有**格挡来源生效，
所以锚/鸟居/帆船/药水给的格挡也一起被清零了，太粗暴。改成**只让技能牌的格挡失效**：
加 `cardSource != null && cardSource.Type == CardType.Skill` 判断。
同时把伤害加成收紧到 `cardSource != null && cardPlay != null && props.IsPoweredAttack() && dealer == Owner.Creature && cardSource.Owner == Owner`。

> **通用教训（第二次踩）**：写 `Modify*` 钩子前先反编译看**基类返回什么**，
> 再想清楚**这个钩子会被谁、在什么范围调用**。
> `ModifyDamageCap` 基类返回 `decimal.MaxValue`；`ModifyBlockAdditive` 基类返回 `0m`；
> `ModifyBlockMultiplicative` 基类返回 `1m` —— 不用它的分支必须原样返回这些中性值。

#### B 站梗：新增「宇宙冷漠」任务

**查实了**：《尖塔梗百科》里最火的「宇宙冷漠」= 游戏里真有的牌
`CosmicIndifference`，**摄政王的 1 费普通技能：6 格挡 + 从弃牌堆捞一张放回抽牌堆顶**（升级 9 格挡）。
社区把它当歌合唱（「宇！宙！冷！漠！」），带火它的是主播「菜农来辣（农神）」。

- 任务：「**宇！宙！冷！漠！**」打出 3 张宇宙冷漠（**摄政王专属**，用 `OnlyCharacter`）→
  奖励「冷漠」：每当你获得格挡，额外获得 3 点格挡（`ModifyBlockAdditive`）。
- 完整梗清单见 `notes/杀戮尖塔2-梗清单.md`（含《尖塔梗百科》14 期全名单 + 每条能不能做成任务）。

### 4.7 借《海克斯符文》的两个手法（2026-09-22 深夜六批）

任务库 51 → **53**。拆解笔记：`notes/海克斯符文-拆解.md`（那个 mod 880 个符文类 / 502 条文本，
值得一读）。

| 任务 | 条件 | 奖励 | 类型 |
|---|---|---|---|
| 熟能生巧 | 本局打出 60 张牌 | 随机升级牌组里的 2 张牌 | **升级型**（让你已有的牌变强） |
| 玩命 | 4 次以低于 30% 的血量赢下战斗 | 「莽夫之道」：伤害 +50%，但再也无法获得格挡 | **代价换强度型** |
| 手不释卷 | 本局抽 120 张牌 | 每回合多抽 1 张牌 | `ModifyHandDraw` |
| 砍价高手 | 商店累计花掉 300 金币 | 商店价格 -30% | `ModifyMerchantPrice` |
| 养生 | 篝火休息 4 次 | 篝火回复量 +50% | `ModifyRestSiteHealAmount` |
| 稳如老狗 | 5 场无伤取胜 | 能量上限 +1 | `ModifyMaxEnergy` |
| 厚积薄发 | 打出 15 张能力牌 | 你施加的增益与减益都 +1 层 | `ModifyPowerAmountGivenAdditive` |
| 铁壁 | 本局承受 300 点伤害 | 单次受到的伤害不超过 15 | `ModifyDamageCap` |
| 连环拳 | 打出 40 张攻击牌 | 你的攻击牌多打一段 | `ModifyAttackHitCount` |

> ⚠️ **第一版我照着《海克斯符文》抄了个「二刀流」，被用户要求删掉**——
> 那个 mod 在二代太火、基本人人都玩过，抄它的效果会显得没水平。
> 用户要的是**学到它的手法**，所以真正该学的是它的「本钱」：
> `AbstractModel` 上 **25 个 `Should*`（否决）+ 29 个 `Modify*`（改写）** 钩子。
> 完整菜单见 `notes/海克斯符文-拆解.md` 第三节 —— **做效果先去那张表找旋钮，别硬写**。
> 现在这 8 个随身物每个对着**不同**的旋钮，手感天然不一样，而且全是自己设计的。

- **升级型奖励**：`CardCmd.Upgrade(card)`（void，自带预览）+ `Owner.Deck.Cards.Where(c => c.IsUpgradable)`；
  一张都升不了时折现 150 金币。这是奖励维度的扩展——以前只有「发东西」。
- **莽夫之道**（代价换强度，两条 `[SavedProperty]` 桥 + 面板一栏）：
  - 收益：`ModifyDamageMultiplicative` → 自己打出的、有牌来源的攻击返回 `1.5m`
  - 代价：`ModifyBlockMultiplicative` → 目标是 `Owner.Creature` 时返回 `0m`
- ⚠️ `ModifyDamageCap` 的**基类实现返回 `decimal.MaxValue`**：不用它的场合必须原样返回这个值，
  返回 0 会把所有伤害清零。

**新验证到的接口**：`CardCmd.Upgrade(CardModel, CardPreviewStyle)`、`CardModel.IsUpgradable`、
`AbstractModel.ShouldPlay(CardModel, AutoPlayType)`（拦牌不让打出）、`ModifyCardPlayCount`（重放）、
`ModifyAttackHitCount(AttackCommand, int)`（`AttackCommand` 在 `MegaCrit.Sts2.Core.Commands.Builders`）。

⚠️ **这批全是没实测过的**：`ModifyCardPlayCount` 的「重放」语义在遗物上是否真的生效、
`ModifyDamageMultiplicative` 会不会误伤别的伤害，都得上游戏看。实测时**先接「玩命」**
（用测试按钮一键完成），打一场看攻击牌是不是打了两次、伤害是不是低了。

### 4.6 鸡煲三部曲 + 角色专属任务（2026-09-22 深夜五批）

任务库 48 → **51**。梗来源见 `notes/杀戮尖塔2-梗清单.md`。

| 任务 | 条件 | 奖励 | 限定 |
|---|---|---|---|
| 还在启动 | 前 3 个回合一张牌都不出，还把这场战斗打赢 | 机械臂 | 故障机器人 |
| 回响形态 | 单回合内把同一张牌打出两次 | 齿轮工艺品 | 故障机器人 |
| 严父在上 | 在第一幕赢下一场精英战 | 突变酵素 | 故障机器人 |

**新机制：`QuestDef.OnlyCharacter`**（`Type?`，null = 谁都能抽）。

- `QuestCatalog.PickOffers(n, exclude, preferEarly, character)` 会把
  不匹配当前角色的任务从候选里滤掉；`character == null` 时**不过滤**（失败时放开，不静默清空候选）。
- 判定用 `OnlyCharacter.IsAssignableFrom(characterType)`，子类也算。
- 角色类型从 `Player.Character.GetType()` 拿，两个调用点：
  `Entry.OnRunStarted` 传进 `QuestSelection.ArmRunStart(...)`；
  `QuestBookRelic.AfterRoomEntered`（进商店）传进 `QuestSelection.OnShopEntered(...)`。
- 同批还顺手把 `球球交响`（充能球）和 `爪爪爪`（Claw）也标成了故障机器人专属——
  这两张牌/机制别的角色根本没有，抽给别人就是**永远做不完的任务**。

**顺手调的两个数值**：`挥金如土` 商店购买 10 → 6（10 次基本要跑遍全图商店）。

### 4.5 战斗内生成牌的坑（2026-09-22 深夜，实测反馈）+ 测试按钮

**用户实测反馈：枯枝随机出来的牌，一打出就卡住。**

原因：我原来是用「造一张牌丢进手牌」的粗办法做的——

```csharp
// ❌ 错的做法（会卡住）
var card = Owner.RunState.CreateCard(template, Owner);
await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
// 更早那版甚至是 CardPileCmd.Add(card, PileType.Hand)
```

游戏没把这牌登记成「战斗内生成的牌」，所以它一被打出就卡死。
**正确写法**（照抄 `moreRelics` 里那根真 DeadBranch 的实现）：

```csharp
// ✅ 对的做法
var hand = CardPile.Get(PileType.Hand, Owner);
if (hand == null || hand.Cards.Count >= CardPile.MaxCardsInHand) return;   // 手牌满了就别发

var pool = Owner.Character.CardPool
    .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint);

var cards = CardFactory
    .GetDistinctForCombat(Owner, pool, 1, Owner.RunState.Rng.CombatCardGeneration)
    .ToList();
if (cards.Count == 0) return;

Flash();
await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner, CardPilePosition.Bottom);
```

要点：
- **`CardFactory.GetDistinctForCombat`** 才是「为战斗造一张牌」的正规入口（会带上 Owner、战斗上下文、用共享 RNG）。
- `CardPileCmd.AddGeneratedCardsToCombat`（复数版）会把这张牌写进战斗历史。
- **牌池只取本职业**：`Owner.Character.CardPool.GetUnlockedCards(...)`。跨职业的牌塞手里会出问题，
  玩家也看不懂。这条也适用于「展望未来」给的 X 费牌。
- 手牌满（`CardPile.MaxCardsInHand` = 10）时直接不发，别硬塞。

> ⚠️ 反面教材价值：`RunState.CreateCard` + `CardPileCmd.Add` 这套**只适合往整局卡组塞牌**
> （游戏自己的 `AddCurseToDeck` 就是这么写的），**不要**拿来往战斗中的手牌/抽牌堆塞。

**测试按钮**（用户要求，方便试奖励）：面板底部有一行

```text
【测试】立即完成全部任务
```

点了会把所有进行中的任务直接判完成，并**真的走一遍发奖逻辑**（遗物 / 卡牌 / 随身物都发）。
实现：
- `QuestState.ForceComplete()` —— 不管条件直接置完成。
- `QuestRunner.ForceCompleteAll()` —— 遍历进行中的任务，逐个完成并调用发奖出口。
- 发奖代码在遗物里，所以遗物拿到时会把自己注册成 `QuestRunner.RewardSink`（见 `Entry.GiveQuestBook`
  和读档分支），UI 才能复用同一套发奖。
- ⚠️ 这是个**测试口子**，将来要发布/分享之前记得删掉或藏起来（会让玩家一键刷完所有奖励）。

### 4.4 一代遗物任务（2026-09-22 深夜二批）

任务库 42 → **48**。素材来源：工坊 mod **moreRelics「更多遗物」**（移植了一代剩下的 59 件遗物，
中文名和效果已全部扒出来存进 `notes/一代遗物-更多遗物mod清单.md`）。

| 任务 | 条件 | 给的遗物 |
|---|---|---|
| 枯枝还魂（改奖励） | 本局消耗 10 张牌 | 枯木树枝 |
| 小伤不理 | 本局累计获得 200 点格挡 | 鸟居 |
| 今晚不睡 | 在篝火升级 2 张牌，且一次都没休息过 | 咖啡滤杯 |
| 药罐子 | 本局使用 4 瓶药水 | 玩具扑翼飞机 |
| 见钱眼开 | 本局攒到 150 金币 | 金偶像 |
| 一个都不放过 | 打开本局的第一个宝箱房 | 套娃 |
| 一身清白 | 牌组里一张诅咒都没有，赢下 5 场战斗 | 御守 |

> ⚠️ **这几条的目标值是返工过一次的。** 第一版我配的是「500 金币 → 金偶像」「3 张诅咒 → 御守」
> 「3 个宝箱房 → 套娃」，被用户当场指出是蠢设计：这三件遗物都是**拿到之后才持续生效**的
> （金偶像＝之后金币+25%；御守＝抵消之后的诅咒；套娃＝之后 2 个宝箱双倍），
> 配后置条件等于到手就作废。
>
> **写新任务时先问：这件遗物是「前置型」还是「结算型」？**
> - 前置型（金币加成 / 诅咒防护 / 宝箱加成 / 每回合回能 / 每次消耗触发…）：
>   条件必须**第一幕内能达成**，而且任务要打上 `EarlyBird` 标记。
> - 结算型（战斗结束回血、一次性效果）：条件晚点无所谓。
>
> 另外，光把条件提早还不够——任务库 48 个，**开局只随机给 6 个候选**，
> 所以 `QuestDef` 加了 `EarlyBird` 字段，`QuestCatalog.PickOffers(..., preferEarly: true)`
> 会在**开局选任务**时优先抽这些任务。不然前置型遗物经常到第三幕才露脸，照样白给。

**核心机制：不硬依赖任何 mod。** `QuestBookRelic.GrantLegacyRelic(entryKeyword, cnName, fallback)`：

1. 遍历 `ModelDb.All`，找 `RelicModel` 且 `Id.Entry` 含关键字的（这些移植 mod 的 ID 就是它写的英文，
   例如 moreRelics 的 `[CustomID("DeadBranch")]` → 本地化 key `DeadBranch.title` → 中文名「枯木树枝」）。
2. 找到 → `RelicCmd.Obtain(found.ToMutable(), Owner)` 直接发真货；已经有了就折现 120 金币。
3. 找不到（没装那个 mod / 那件在 mod 配置里被关掉）→ 执行 fallback：
   4 件有自研仿制效果（枯木树枝 / 鸟居 / 咖啡滤杯 / 玩具扑翼飞机，走 `QuestBoons` 的「随身物」），
   其余 3 件折现金币（金偶像 200 / 套娃 150 / 御守 150）。
   Toast 里会明说「没装带『XX』的遗物 mod，给的是仿制品」，不糊弄玩家。
4. 发放失败（异常）也走 fallback，绝不因为别人 mod 的问题崩掉。

**新条件类型**：`BlockGainedBig`（300 格挡）、`SmithNoRest`（升级 5 张 + 本局从未休息，
靠 `EverRested` / `Smiths` 两个 `[SavedProperty]` 属性）、
`PotionsUsedBig`、`GoldHoardBig`、`TreasureRooms`（`RoomType.Treasure` 计数 + `Treasures` 存档属性）、
`CursesInDeck`（卡组快照里数 `CardType.Curse`）。

**教训（下次加计数先想存档）**：`_everRested` / `_smiths` / `_treasures` 一开始写成了普通私有字段，
读档回来会归零——「今晚不睡」会在读档后被误判为满足条件。已经改成带 `[SavedProperty]` 的属性。
⚠️ `_roomTypes`（`HashSet<RoomType>`，采风向导用）仍然是普通字段，没进存档；
因为那条任务用 `SetAbsolute` 只增不减，读档不会让它倒退，所以暂时无害，但要知道这回事。

### 4.3 梗任务三连 + 「随身物」（2026-09-22 夜）

设计案在 `notes/尖塔任务-梗任务设计案.md`。任务库 39 → **42**。

| 名字 | 条件 | 奖励 |
|---|---|---|
| 船长的执念 | 本局同时拥有 锚 + 角夹 + 船长之轮 | 「随身物」帆船 |
| 枯枝还魂 | 本局消耗 20 张牌 | 「随身物」枯枝 |
| 展望未来 | 本局打出 3 张 X 费牌 | 本体遗物**化学X** + 一张随机 X 费牌进卡组 |

**奖励不再只有金币**：`QuestDef` 多了一个可选的 `RewardLabel`（只有非金币奖励才填，UI 里显示它），
真正发什么东西在 `QuestBookRelic.Reward()` 里按 `Kind` 分派。

**⚠️ 为什么「船」和「枯枝」不是遗物，而是「随身物」——这条务必先看懂：**

游戏读 mod 本地化表的路径是写死的（`ModManager` 里）：

```csharp
string path = $"res://{current.manifest.id}/localization/{language}/{file}";
```

`res://<ModId>/` **只有打了 pck 才存在**。本 mod 是 `has_pck:false` 的纯 dll，所以自己新建的遗物
**拿不到 `.title` / `.description`**，界面上会显示内部 key（例如 `QUESTSPIRE_RELIC_XXX.title`）。
（连带问题：现有的「任务册」遗物本身也是没有本地化的。）

于是奖励分两类：

- **本体已有的东西** → 直接发（`RelicCmd.Obtain<ChemicalX>` / 加卡进卡组），名字是游戏自带的。
- **本体没有的东西** → 做成「随身物」：`QuestBoons` 静态开关 + 挂在任务册上的效果 +
  我们自己面板里的「随身物」一栏（中文硬编码，因为那是我们的 UI）。开关用两条
  `[SavedProperty]` 桥接进存档，跟 `QuestData` 一个套路。

**要根治**：给 mod 打 pck（csproj 里 `GodotExe` / `RunPckExport` 的开关都在，现在是关的）。
那会动到目前跑通的构建链，所以单独一步做。

**新用到的接口：**
- 发本体遗物：`await RelicCmd.Obtain<ChemicalX>(Owner)`（发之前先 `GetRelic<T>()` 查重，重复就折现金币）
- 造一张有主的牌：`Owner.RunState.CreateCard(template, Owner)`
- 塞进卡组：`await CardPileCmd.Add(card, PileType.Deck)` + `CardCmd.PreviewCardPileAdd(result, 1.6f)`
- 塞进手牌（枯枝）：`await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner)`
- 加格挡（船的）：`await CreatureCmd.GainBlock(Owner.Creature, 15m, ValueProp.Unpowered, null)`
- 屏幕提示：`RitsuToastService.ShowInfo(正文, 标题)`（`STS2RitsuLib.Ui.Toast`，已包成 `Entry.Toast`，内部 try/catch）
- X 费牌判定：`card.EnergyCost.CostsX`（不要用 `Canonical`，X 费牌的 Canonical 是 0）

**随机牌的池子**（`RandomPool`）：只取 Attack / Skill / Power，排掉 Token / Status / Curse / Quest，
免得随机出一张根本打不出去的牌。用的是 `Random.Shared`（单机够用；联机要换 `RunState.Rng`）。

### 4.2 一代移植的 14 个任务（2026-09-22 晚二批）

任务库 25 → **39**。来源是一代池子里「能做没做」的那批（完整对照见 `notes/一代SpireQuests任务池.md`）。

| 二代名字 | 一代 # | 判定方式 |
|---|---|---|
| 三费齐鸣 | 2 | `AfterCardPlayed` 记本回合打出的费用集合，1/2/3 齐了算一次（每回合清零） |
| 朴实无华 | 4 | 本场标记 `_nonCommonPlayedThisCombat`（Uncommon/Rare/Ancient）+ 精英/首领胜利 |
| 球球交响 | 6 | `AfterOrbEvoked` 按 `orb.GetType()` 去重，单回合 4 种 |
| 整整齐齐 | 16 | 卡组快照：恰好 10 攻击 / 10 技能 / 5 能力 |
| 礼让一手 | 39 | 精英战 + `_combatTurnIndex == 1` + `_turnCards == 0`（回合开始 / 战斗胜利两处都判） |
| 精准收割 | 41 | `AfterDamageGiven` 里 `result.WasTargetKilled && result.OverkillDamage == 0` |
| 问号猎手 | 47 | `AfterRoomEntered` + `room.RoomType == RoomType.Event` |
| 贫下中农 | 52 | 卡组快照：普通 −（罕见+稀有）≥ 7 |
| 只看不买 | 67 | 进商店清零 `_shopSpent`，进下一个非商店房间时结算 |
| 偶数为王 | 70 | 卡组快照：非 Basic 牌里偶数/0 费 ≥ 7 且奇数费 = 0 |
| 闪电战 | 74 | 胜利时 `_combatTurnIndex <= 4` |
| 爪爪爪 | 75 | `cardPlay.Card is Claw` |
| 满血见王 | 76 | 进 `RoomType.Boss` 时 `CurrentHp >= MaxHp` |
| 原装出厂 | 77 | 新存 `BaselineMaxHp`（`[SavedProperty]`）+ `RunState.CurrentActIndex == 1` + 首领胜利 |

**两个没做，原因写在一代池子 md 里**：#7 Triple Dipper（没有正向的「卡牌加入卡组」钩子）、
#11 Ambitious Strike（认不出「起始打击」这张牌）。

**新加的框架要点：**
- `_combatTurnIndex` / `_isEliteCombat` / `_nonCommonPlayedThisCombat` 这些「本场」标记，
  **统一在「进 CombatRoom」时清零**，`AfterCombatEnd` 里**故意一个都不清**。
  原因：`AfterCombatEnd` 和 `AfterCombatVictory` 谁先谁后没实测过，如果 End 先跑，
  Victory 里的判断（毫发无伤 / 朴实无华 / 闪电战 / 礼让一手）就会读到被清空的值。
- 卡组构成类（整整齐齐 / 贫下中农 / 偶数为王）走 `CheckDeckQuests()`，在进房间和战斗胜利时各算一次。
  它们是「条件成立即完成」，所以 `Set(kind, 1)` + target=1（`SetAbsolute` 只增不减，天然就是一次性判定）。
  ⚠️ **依赖「`Player.Deck` 就是整局卡组」**；万一不是，这三个任务会永远停在 0（不会崩），实测时重点看这个。
- `BaselineMaxHp` 是第二条 `[SavedProperty]`，在第一次进房间时记下当时的最大生命。

---

## 5. 环境与工具坑（务必读）

- **PowerShell 5.1 控制台显示 UTF-8 中文必乱码**（GBK 控制台）——只是显示问题，文件正常。别被误导。
  ⚠️ 但**含中文的 `.ps1` 脚本文件必须存成「带 BOM 的 UTF-8」**，否则 PS 5.1 按 ANSI 读，
  中文会碎掉、连字符串引号都会被判成没闭合（报 `The string is missing the terminator`）。
  `tools/build_pck.ps1` 就踩过这个，已加 BOM。新建带中文的 ps1 记得也加。
- 抓游戏 API：`ilspycmd -t <Full.Type> "<game>\data_sts2_windows_x86_64\sts2.dll" -o <临时目录>` → 生成 `<Full.Type>.decompiled.cs`。
  `ilspycmd` 在 `~/.dotnet/tools/`。版本旧(8.2)会提示 update，能忽略。用它**先验证再写代码**，别凭记忆猜签名（本仓一贯规矩）。
- **Playwright 禁 file://**：要测本地 html 才需要起 `python -m http.server`；本 mod 是 Godot，测试靠**重启游戏 + 真人进局**。
- **从 agent 侧启动游戏不稳定**（`steam://rungameid/2868840`，常“未在 150s 内启动”）。能起也是停在主菜单，**桌面 Godot UI 无法自动点**，进局实测必须真人配合。
- **Write/Edit 工具偶发弄乱文件名或往代码里插乱码**（历史出现过 ` Geilei`、`.gitignore` 改名等）。**每次写完 build 一遍**即可暴露；重要 md 也可写完 `read` 回读核对。
- 游戏日志只反映**上次实际运行**的会话；改 dll 后没重启就读日志会读到旧的，先对 `LastWriteTime`。

---

## 6. 已知限制 / 后续 TODO（按优先级）

1. ~~**★★ 存档不持久**~~ **已修（2026-09-22 晚，未经实测）**：见 §2「存档桥」。改法是把进度塞进
   `QuestBookRelic.QuestData`（`[SavedProperty]`），读档时靠 `Fill` 的 setter 灌回 `QuestRunner`。
   → 待真人验证：接任务 → 打出一些进度 → 退回主菜单 → 「继续游戏」→ 侧栏任务与进度条是否原样回来。
2. **★ 开局按钮在“地图界面”是否可见可点，未实测**。面板挂在 `SceneTree.Root`，地图界面若把 HUD 放在独立 CanvasLayer，按钮可能被盖住 → 开局那批就点不到。若属实：改为在合适界面 re-arm，或换挂载点。**这是最该先真人验的一条。**
   （旁证：上一次实测里选任务浮层是点得到的，说明 Root 层浮层至少当时能盖住游戏 UI，但仍未单独验过地图界面。）
3. **奖励只有金币**。想要“完成任务给一张牌/一个遗物/一次性效果”→ 需先反编译 `Reward`/`RewardTaken` 相关类型（`AfterRewardTaken(Player, Reward)` 里的 `Reward` 类型、以及往卡组加卡的命令），目前未定位。
4. **卡组构成类任务**（一代 Organized/Peasant「卡组恰好有 X 张某类牌」）没做：原以为是缺「整局 master deck」访问，
   但 2026-09-22 反编译发现 `Player.Deck` **就是**整局卡组（见 §3 更正一）。**只差游戏内实测确认一次**，
   确认后 7 / 16 / 52 / 70 这几个任务就能加；卡「颜色/无色」属性仍未确认。
5. **数值未平衡**：所有 target / rewardGold 都是拍脑袋，需真人几局手感后再调 `QuestCatalog`。
6. **“上限/禁止/连击”型任务语义缺失**：框架只有“累计到目标”。像“本幕承受未格挡伤害≤25”“连续 N 场不用药水”“两个商店分文不花地离开”需要新增“约束/失败”或“连击计数器”模型。目前用 AfterCombatVictory + AfterCurrentHpChanged 凑了近战斗“不掉血”，真正的全幕上限类没做。
7. ~~**精确伤害击杀**（一代 Calculated Killer）未做~~ **已做（2026-09-22 晚）**：`精准收割`，
   判定 `result.WasTargetKilled && result.OverkillDamage == 0`（不用比受击前 HP，`OverkillDamage` 直接给答案）。
8. 单机假设：任务册只发给 `Players[0]`，多人未处理。
9. 没有 mod 图标 / 详细描述 json 字段 / 英文本地化（标题硬编码中文，内销够用）。

---

## 7. 参照与资源

- 一代任务池（78 个，可继续挑选改造的原文来源）：
  `notes\一代SpireQuests任务池.md`
- 一代原始 jar（任务文案在 `anniv8Resources/localization/eng/<作者>/Queststrings.json`）：
  `D:\software\steam\steamapps\workshop\content\646570\3649417096\SpireQuests.jar`
- 本 mod 进度流水账：`notes\尖塔任务-quest-spire-开发进度.md`
- 一代游戏工坊 mod 目录：`...steamapps\workshop\content\646570\`；二代参考 mod：`...\content\2868840\`

---

## 8. 改完必做（验收清单）

1. `dotnet build` → **0 error**（这一步能验证钩子签名/枚举/字段是否存在）。
2. 确认 `mods\QuestSpire\QuestSpire.dll` 时间戳是刚生成的。
3. **重启游戏**，真人开一局验证：开局按钮点亮/弹出、拖动手感、折叠、描述显示、商店按钮时序、各任务计数与发奖、地图界面按钮可点性。
3.5 **本轮新加 14 个任务，重点验这几个**（其余的等自然抽到即可）：
   - **整整齐齐 / 贫下中农 / 偶数为王** → 验「`Player.Deck` 是不是整局卡组」。这三个只要卡组一动
     （捡牌、打一场后）就该有反应；如果永远是 0，说明 Deck 不是整局卡组，卡组构成类要换访问方式。
   - **问号猎手** → 进一个 `?` 房看进度 +1。
   - **只看不买** → 进商店不买东西直接走，看是否 +1；买一件再看是否**不**加。
   - **闪电战 / 礼让一手 / 朴实无华** → 打完一场战斗看有没有异常计数（这三依赖「战斗胜利时本场标记还在」）。
   - **爪爪爪** → 只有拿到 Claw 这张牌才验得到，验不到也没关系。
   - 侧栏一次显示 5 条任务时，看排版会不会挤爆 / 超出屏幕。
3.6 **梗任务三连（2026-09-22 夜新增，抽到才验得到）**：
   - **船长的执念**：抽到后看面板是不是显示 `0/3`；拿到三件船用遗物（锚 / 角夹 / 船长之轮）后
     应弹出「任务完成」，面板下方出现「随身物：帆船」。**顺便验船的效果**：之后每场战斗开始应该 +15 格挡，
     本场第一次格挡被打光时再 +20（看格挡数字和遗物闪光）。
   - **枯枝还魂**：消耗 20 张牌后弹出完成提示；之后每消耗一张牌，手上应该多出一张随机牌、
     **而且这张牌要能正常打出去**（2026-09-22 实测过一次「打出来卡住」，已按正规写法修好，重点复验）。
     手牌满 10 张时不会发牌，这是故意的。
   - 想省时间就用面板底部的 **【测试】立即完成全部任务** 按钮，一次把所有奖励都看一遍。
   - **展望未来**：打出 3 张 X 费牌后 → 遗物栏应该多出游戏本体的**化学X**，并且卡组里多一张随机 X 费牌
     （会有一个获得动画）。如果你已经自己拿过化学X，会改成给 120 金币。
   - 三个任务完成时都应该在屏幕角落弹提示（`RitsuToastService`）。弹不出来的话日志里会有 `Toast failed`。
4. **读档验证（本轮新加，必须做）**：接 1~2 个任务 → 打出一些进度（打两场战斗就够）→ 退回主菜单 →
   「继续游戏」→ 看侧栏任务、进度条、已完成标记是否原样回来；确认**没有出现第二个任务册遗物**、
   也**没有冒出一次「开局选任务」的按钮**（读档刷任务漏洞）。
   判定依据：日志里应出现 `Run loaded: quest book restored, N active quest(s).`（新局则是
   `Run started: quest book granted.`）。
5. 若崩溃/无反应：看 `godot*.log` 里 `QuestSpire` 行的异常。常见：钩子签名不符（编译会拦）、运行期 null（`Owner` 早期未就绪）、浮层没显示（挂载/ZIndex）。
   存档相关的新异常形态：`SavedProperties` 抛 `JsonException` → 说明 `[SavedProperty]` 打在了不支持的属性类型上
   （只支持 int / int[] / 枚举 / bool / string / ModelId / SerializableCard / List<SerializableCard>）。

---

## 9. 发布到创意工坊（2026-09-22 深夜，**已成功**）

> ## ⚠️ 上传纪律（用户 2026-09-23 明确要求）
> **不要每次改完就立刻上传。** 攒够一批改动，**等用户说「传」再传**。
> 上传 = 对外发布，会直接推给所有订阅者；改一版传一版会刷屏，也让人分不清线上是哪个版本。
> 平时改完只要：`dotnet build`（会自动把 dll 部署进游戏 mod 目录）+ 本地进游戏验。
> 只有用户发话时才跑 `prepare_workshop.py` + `ModUploader.exe upload`。

> ## ⚠️ 工坊的两个硬限制（2026-09-23 实测踩到）
> 1. **说明文上限 4000 字**（不是 8000）。超了会上传失败、报 `k_EResultInvalidParam`，
>    而且日志里只显示到 `CommittingChanges → Invalid`，**不告诉你是哪一栏**。
>    验证过程：4605 字 ✗ → 极简 44 字 ✓ → 收到 2262 字 ✓。
>    （旁证：工坊上别人的说明文也都在 4000 以内——RitsuLib 3777、Draw&Guess 3954、我们 0.1.6 是 3723。）
>    **写说明文时随时数一下字数**，宁可精简。
> 2. **封面必须是横图（16:9）**：竖图（1024×1440）会被拒（同样报 `k_EResultInvalidParam`）；
>    裁成 16:9 之后（1280×720、减色 PNG、615KB）就通过了 → **减色不是问题，比例才是**。
>    现在的封面：`workshop/cover.png`（用户给的那张「任务册 + 遗物」的画裁成 16:9）。
>    `prepare_workshop.py` 已改成：源图 < 950KB 就直接用，否则先试全彩、再试减色。
>    换图时记住这两条：**横图 + < 1MB**。

**线上条目**：id `3806270140` → https://steamcommunity.com/sharedfiles/filedetails/?id=3806270140
（**当前是 `public`**，公开可搜、可订阅、可留言。想改回来就动 `meta.json` 的 `visibility`，或在工坊网页上直接改。）

### 9.1 工具
- 用的是官方上传器（Mega Crit 自己出的）：`github.com/megacrit/sts2-mod-uploader`
- 已解压到 `D:\software\sts2-mod-uploader\`（`ModUploader.exe` v0.2.0，`steam_appid.txt` = 2868840）
- **上传前 Steam 必须开着且已登录**；上传器成功后自己会在 Steam 里打开条目页。
- 工作区形状：`content/`（要传的 dll + json + pck）+ `workshop.json` + `image.png`（**必须 <1MB**）+ 可选 `previews/`
- 命令（先 `cd D:\software\sts2-mod-uploader`）：
  `.\ModUploader.exe upload -w "D:\software\sts2-mod-uploader\QuestSpire"`
  - 第一次成功后目录里出现 `mod_id.txt`；**以后 upload 都是更新同一条**，不会重复建条目。
  - 失败详情看 `mod-uploader.log`。
  - 不带参数直接跑 `ModUploader.exe` 会在当前目录建一个空的 `NewModWorkspace`（已经建出来了，无视即可）。

### 9.2 本工程的落地文件
| 文件 | 作用 |
|---|---|
| `projects\quest-spire\workshop\description.md` | 说明文正文（**BBCode** 排版） |
| `projects\quest-spire\workshop\meta.json` | 标题 / 可见性 / 标签 / 依赖 / 更新说明 |
| `projects\quest-spire\tools\prepare_workshop.py` | 一键生成工作区：拷 mod 文件 + 写 workshop.json + 把封面压到 <1MB |
| `projects\quest-spire\workshop\check_item.py` | 读工坊条目（标题/标签/依赖/说明文）：`python check_item.py <id>` |
| `projects\quest-spire\workshop\survey_descriptions.py` | 调研热门模组说明文写法（**要走代理**） |

发布流程 = ① `dotnet build`（顺带把 dll 部署进游戏 mod 目录）→ ② `python tools\prepare_workshop.py` → ③ `ModUploader.exe upload -w ...`

### 9.3 ⚠️ 最大的坑：`dependencies` 必须写数字
- 写 `"dependencies": ["3747602295"]`（**字符串**）→ 上传器直接报
  `Exception thrown while parsing the workshop config! Double-check that the format is correct.`，**整个上传失败**。
- 写 `"dependencies": [3747602295]`（**数字**）→ 正常。（`3747602295` 就是 RitsuLib 的工坊 id。）
- 当时二分定位：最小配置（标题 + "test"）能传 → 加完整说明文能传 → 加数字依赖能传 → 再加标签也能传 ⇒ 凶手就是字符串依赖。

### 9.4 说明文怎么写（读了工坊趋势榜前 10 个模组）
1. **支持 BBCode**，热门模组全在用：`[h1] [h2] [b] [i] [u] [list][*] [url=][/url] [quote] [code] [hr] [img]`（`[img]` 可直接嵌 steamusercontent 链接）。纯文字不排版会很吃亏。
2. **常用板块顺序**：一句话说清是什么 → Features（逐条列表）→ 前置/依赖 → 多人兼容性 → 已知问题 → **Changelog**（版本号 + 条目）→ Credits 致谢 → 链接（GitHub / B站 / QQ群）。大模组还会加 Localization、FAQ、常见崩溃处理。
3. **字数**：常见 1500~2200 字（最短 200，最长 4000）。
4. **多语言**：支持多语言的模组会把中英**逐条并列**（`More Ironclad Animations` 是范例）。
5. **标签可以自己填**（榜上出现过 `Rixian`、`TekExplorer`、`configurable` 这种个人标签），语言标签写 `schinese` / `english`。
6. 调研脚本：`check_item.py`（⚠️ 私有条目 `result=9` 读不到，公开的才读得到）、`survey_descriptions.py`（**直连被重置，必须走 `127.0.0.1:7892` 代理**；报告写到 `%TEMP%\workshop_survey.txt`）。
7. 本模组当前说明文 2041 字，板块：怎么玩 / 任务 / 奖励 / 测试期 / 注意事项 / 已知问题 / 更新日志 / English / 致谢。

### 9.5 改可见性
`meta.json` 里 `"visibility"` 改成 `public` 再跑一次上传即可；**也可以直接在工坊条目网页上改，不用重新上传**。

---

## 10. 2026-09-23 批：梗任务（61 → 68 条）

### 10.1 新增的 7 条（来源：B 站《尖塔梗百科》，UP：TowerHeart22）
| 任务 | 条件 | 用到的钩子 |
|---|---|---|
| 我说过牌有没有懂的 | 单回合抽 8 张 | `AfterCardDrawn` + 每回合 `ResetProgress` |
| 我说别带 | 一场战斗里一张技能牌都不打还赢 | `AfterCardPlayed` 打标记 + `AfterCombatVictory` 结算 |
| 152 金币 | 在商店一口气花掉 150 金币以上 | `AfterItemPurchased` **自带 goldSpent**，不用自己算 |
| 通电 | 本局生成 8 个充能球 | `AfterOrbChanneled` |
| 蛇花圣经 | 给敌人累计叠 20 层减益 | `BeforePowerAmountChanged` + `PowerType.Debuff` |
| 火焰屏障输出特别高 | 靠火焰屏障的反弹打出 40 点伤害 | `AfterDamageGiven` + `Creature.HasPower<FlameBarrierPower>()` |
| 掉集中 | 本局累计失去 10 点集中 | `BeforePowerAmountChanged` + `power is FocusPower` |

新增随身物「**蛇花圣经**」：你给敌人施加的减益 ×1.5。
用的是 `ModifyPowerAmountGivenMultiplicative`（**乘法**旋钮），和「厚积薄发」的 `...Additive`（加法）是两个独立旋钮，
先加后乘，同时拿到不会打架——这是「每个随身物对着不同旋钮」那条设计原则的延续。

### 10.2 这轮反编译核实到的事实（写代码前别猜，照着抄）
- ⚠️ **只有 `BeforePowerAmountChanged` 带 target**，`AfterPowerAmountChanged` 的签名里**没有 target**
  （只有 power / amount / applier / cardSource）。想分清「减益叠给谁」必须用 Before 版：
  `BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)`
- **`PowerModel.Type`** = `PowerType.Buff` / `PowerType.Debuff`（命名空间 `MegaCrit.Sts2.Core.Entities.Powers`）；
  `power.Owner` 是挂着这层 buff 的生物。判「是不是减益」用 `power.Type == PowerType.Debuff`。
- **查某生物身上有没有某层 buff**：`Creature.HasPower<T>()` / `Creature.GetPower<T>()`（反编译 Creature 确认）。
- **火焰屏障的伤害不是牌打的**：`FlameBarrier`（2 费技能 / 罕见 / 目标自己）只做两件事——给格挡 + 挂一层
  `FlameBarrierPower`；真正弹回去的伤害来自那层能力，标记是 `ValueProp.Unpowered` 且**没有 cardSource**。
  光按牌名抓是永远抓不到的，判定要写「自己造成 + `cardSource == null` + props 含 Unpowered 且不含 Move
  + 身上挂着 `FlameBarrierPower`」。
- **`ValueProp` 是 [Flags]**：`Unblockable = 2`（毒那类直接掉血）、`Unpowered = 4`（遗物 / 药水 / **能力**造成的伤害）、
  `Move = 8`（攻击牌与敌人普攻）、`SkipHurtAnim = 0x10`。判断一律用位运算，**别用 `==` 比**。
- `ModifyPowerAmountGivenMultiplicative` 基类返回 **1m**（它是乘数，不是增量）；`...Additive` 返回 0m。
- 反编译姿势（本工程老规矩：先验证再写）：
  `ilspycmd -t <完整类型名> "<游戏>\data_sts2_windows_x86_64\sts2.dll" -o %TEMP%\sts2dec`
  这轮靠它确认了 `FlameBarrier` / `BiasedCognition` / `FocusPower` / `FlameBarrierPower` / `PowerType` /
  `ValueProp` / `Creature` 全都真实存在，于是写完 `dotnet build` **一次过（0 error 0 warning）**。

### 10.3 这轮没做的梗（及原因，别重复研究）
- **基米精神 / 东尼意思 / onp**：含义在网上**没查到准确定义**（B 站合集里有这几期名，但没有可靠解释）。
- **小蓝人**：查到是 B 站上的一个 **mod 角色**（「杀戮尖塔2小蓝人mod介绍」），不是本体内容 → 做进我们的任务不合适。
- **战个未来**：需要「先亏后赚」模型，现在的框架只有「累计到目标」。
- **假商人 / 假货（.FAKE.）**：要判「打赢某个**具体** Boss」，现在只按 `RoomType.Boss` 记，认不出是哪一个。
- **蛇咬 / Duplicate 防御**：要「指定卡牌」「复制一张牌」的语义。

### 10.4 这一批的验证状态
- `dotnet build` 0 error 0 warning，dll 已部署（`mods\QuestSpire\QuestSpire.dll`，2026-09-23 00:31）。
- 工坊已更新为 **0.1.1**（条目 3806270140），说明文 2457 字。
- ⚠️ **还没进游戏实测**：这 7 条任务的实际计数、以及「蛇花圣经」的 ×1.5 效果，都需要真人开一局验（尤其
  「火焰屏障」和「掉集中」这两条依赖反弹伤害 / 集中变化，最该先试）。

### 10.5 批之二：又 6 条（68 → 74 条），挖到假商人的真相了

| 任务 | 条件 | 用到的钩子 / 判据 |
|---|---|---|
| 蛇咬 | 打出 3 张「蛇咬」 | `card is Snakebite`（反编译：2 费技能 / 普通 / 7 层中毒 / 带保留，确实是真牌） |
| 删内切 | 在商店花钱删掉 2 张牌 | `AfterItemPurchased` + `itemPurchased is MerchantCardRemovalEntry` |
| 复制防御 | 一场战斗内生成 5 张牌 | `AfterCardGeneratedForCombat`（单场计数，进战斗重置） |
| 战个未来 | 先被打到半血以下、最后还赢 | `AfterCurrentHpChanged` 打标记 + `AfterCombatVictory` 结算 |
| 假货我也要 | 同时拥有 2 件假货 | 扫 `Owner.Relics`，Id 里带 `FAKE` 的算一件 |
| 真的假的 | 真锚 + 假锚「锚???」同时在手 | 扫遗物 + `Owner.GetRelic<Anchor>()` |

**这轮挖到的三件事（都反编译确认过）**：

1. **「假商人」卖的是真·独立遗物**。dll 里有一整套 `Fake*` 遗物类型，一共 **9 件**：
   `FakeAnchor`（就是官方中文名的**「锚???」**）、`FakeBloodVial`、`FakeHappyFlower`、`FakeLeesWaffle`、
   `FakeMango`、`FakeOrichalcum`、`FakeSneckoEye`、`FakeStrikeDummy`、`FakeVenerableTeaSet`。
   另外还有 `FakeMerchant` **事件** + `FakeMerchantMonster`（会扔遗物的怪）。
   → 所以「假货」类任务**不用碰事件流程**，扫玩家遗物 Id 里带不带 `FAKE` 就行（我们就是这么做的）。
2. **删牌走的是独立的 `MerchantCardRemovalEntry`，而且它也会触发 `AfterItemPurchased`**
   （普通商品走基类那一份，删牌这个类**自己重写**了 `OnTryPurchaseWrapper` 并照样调 Hook）。
   → 「152 金币删内切」这种任务可以精确判出来，不必去碰 `ShouldAllowMerchantCardRemoval`。
   顺带：删牌基础价 75（高难度词缀下 100），之后每删一次 +25（高难度 +50）——
   **「152 金币」这个梗就是这么来的**（删到第 3~4 次的价格）。
3. **「复制一张牌」的官方写法**：`selection.CreateClone()` + `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner)`
   （反编译 `DualWield` 得到）。所以「战斗内生成牌」这条路能覆盖复制出来的牌 → 用 `AfterCardGeneratedForCombat` 计数。
   另：**数玩家身上的遗物**用 `Player.Relics`（`IReadOnlyList<RelicModel>`）。

⚠️ 顺手更正一条旧笔记：梗清单里原写「假商人是 Boss，要判打赢某个具体 Boss，做不了」——
**不对**。假商人是个**事件**（`FakeMerchant` 事件 + 商店界面 `NFakeMerchantInventory`），副本是卖假货；
所以「假货」能做成任务，只是「打赢假商人」这条确实还没做。

### 10.6 批之三：又 4 条（74 → 78 条），四个角色都有专属任务了

| 任务 | 条件 | 判据 |
|---|---|---|
| 打赢假商人 | 击败假商人 | `room.ModelId.Entry` 含 `FAKE_MERCHANT` |
| 数值轮椅 | 单回合造成 30 点伤害 | `AfterDamageGiven` + 每回合 `ResetProgress` |
| 你玩的鸡煲啊 | 故障机器人本局赢 2 场精英战 | `AfterCombatVictory` + `Owner.Character is Defect` |
| 劣人手速 | 静默猎手单回合打出 7 张牌 | `AfterCardPlayed` + 每回合 `ResetProgress` |

**这轮核实到的**：
- **`CombatRoom.ModelId` 就是 `Encounter.Id`**（反编译 `CombatRoom` 确认），所以**能认出打的是哪一场**。
  假商人那一战是 `FakeMerchantEventEncounter`，⚠️ **它的 `RoomType` 是 `Monster` 不是 `Boss`**
  （固定 300 金币奖励）→ 想判「打赢假商人」**不能靠 `BossWins`**，只能看房间 Id。
- **「劣人」是中文社区给静默猎手起的外号**（工坊上就有个叫「劣人TV之尖塔MOD」的皮肤 mod），
  「你玩的鸡煲啊！」是鸡煲那条梗的完整句式（见于 17173 的报道标题）。
- 奖励也能「抢」：`GrantRandomFakeRelic()` 从 `ModelDb.All` 里筛出所有 Id 带 `FAKE` 的遗物，
  排除玩家已有的，随机发一件；全有则折现 200 金币。**发假货当奖杯**是故意的（假货本身就是梗）。
- 四个角色现在都有专属任务：故障机器人（5 条）、静默猎手（1）、摄政王（1）、其余通用。
- ⚠️ 依旧**没实测**。三批共 **17 条**新任务等着真人验。

### 10.7 批之四：又 3 条（78 → 81 条），角色外号补全

| 任务 | 条件 | 判据 |
|---|---|---|
| 战士鸽 | 一场战斗里自己挨 20 点伤害，还赢 | `AfterCurrentHpChanged` 累计本场自伤 + `AfterCombatVictory` |
| 无限 | 单回合打出 10 张牌 | `AfterCardPlayed` + 每回合 `ResetProgress` |
| 我说有费有没有懂的 | 单回合花掉 6 点能量 | `AfterEnergySpent(card, amount)` |

**新核实到的**：
- **「战士鸽」是社区给铁甲战士起的外号**（B 站有「接下来向我们走来的是战士鸽和他的唐氏无限」这种标题），
  和「鸡煲」（故障机器人）、「劣人」（静默猎手）是同一路数的谐音外号。
  至此 **铁甲战士 / 静默猎手 / 故障机器人 / 摄政王 四个角色都有专属任务**。
- ⚠️ **`AfterEnergySpent(CardModel card, int amount)` 没有 Player 参数**，多人局要靠 `card.Owner` 反推是谁花的。
- ⚠️ 外号类任务要挑**干净的**：网上那条「唐氏无限」里的「唐氏」是拿病名骂人的脏梗，**不要写进 mod**。
  「无限」这个词本身没问题，所以只取了「无限」。

### 10.8 批之五：梗的含义**查清了**（81 → 83 条）+「找梗」的标准流程

| 任务 | 条件 | 判据 |
|---|---|---|
| 基米精神 | 带着 200 金币进商店，一分钱不花就走 | 进店时记 `_goldAtShopEntry`，离店时判「没花钱 + 家底够厚」 |
| 东尼意思 | 卡组里有 8 张升级过的牌 | `CheckDeckQuests()` 扫 `card.IsUpgraded` → 奖励随身物（`ModifyCardRewardUpgradeOdds` ×3） |

#### 找梗的标准做法（下次照这个来，别再瞎搜网页）

**工具：`projects/quest-spire/tools/meme_hunt.py`**

1. **拿 bvid**：B站 搜索接口
   `https://api.bilibili.com/x/web-interface/search/type?search_type=video&page=1&page_size=20&keyword=<词>`
2. **拿标题/UP/时长**：`https://api.bilibili.com/x/web-interface/view?bvid=<bvid>`
3. **拿热评（关键一步）**：`https://api.bilibili.com/x/v2/reply?type=1&ps=20&sort=2&oid=<aid>`
   → **梗的准确含义基本都写在最高赞评论里**，比搜网页快得多、也准得多。
4. ⚠️ **站方限流**：同一个 IP 连续请求会先给 `code=0` 然后一路 `HTTP 412`。
   必须**每次请求之间 sleep 5~6 秒**，并且失败要退避重试。走代理 `127.0.0.1:7892`。
5. 灰机 wiki（`sts2.huijiwiki.com/api.php`）**基本被 403 挡死**，别指望它。

#### 这次查实的含义（原话记在 `notes/杀戮尖塔2-梗清单.md` §一·五）

- **基米精神**（`BV1hkYUz9ERr`，17.9 万播放）最高赞（1580 赞）原话：
  「**基米精神是没有开辉眼的强行战未来，宁愿赌极小概率胡局也不愿意削弱未来强度以增强当下战力保证过渡而委屈自己**」
  → 一句话：**不打过渡、死赌未来**。补充（1172 赞）：「你觉得你的操作能让你想喊一声基米，那就是基米精神」。
- **东尼意思**（`BV14QuC6fEX6`）热评：「有些游戏：xxx 很强 → 削成区；有些游戏：xxx 很强 → 增强别的让 xxx 变得合理」
  → **东尼 = 制作人安东尼**，这期讲的是他的平衡哲学（社区还有「安东尼常数」「东尼算法」的说法）。
- **战个未来**（`BV1NmvwzQEvr`，23.2 万）热评：「每次我看到 x 药，耳边就会响起一句『战个未来吧』」
  → 早期吃亏换后期起飞 ✓ 和我们做的「先亏后赚」是同一个意思，这条**没做错**。
- **onp**（`BV1Yhjt6CEjs`）→ ❌ **不做**：源自主播圈的评论区刷屏 + 「偷梗」争议（连白夕Seal 都出现在评论区），
  属于圈子互撕，不是游戏内容。
- **小蓝人** → ❌ **不做**：《以撒的结合》的角色（魂心那个），和尖塔无关。

#### 至此梗库的状态

《尖塔梗百科》14 期里，**能做成任务的都做完了**（剩 `onp`/`小蓝人` 两个是圈外梗、
「删牌没点到会员卡」无法追踪）。任务总数 **83**。

---

## 11. 游戏真实数值表（做平衡必须查这里，2026-09-23 反编译核对）

来源：`ilspycmd` 反编译 `Ironclad` / `EncounterModel` / `AscensionHelper` / `AscensionLevel` /
`MerchantCardRemovalEntry` / `AscendersBane`。

### 11.1 金币

| 项目 | 数值 | 出处 |
|---|---|---|
| **开局自带** | **99** | `Ironclad.StartingGold`（其余角色没逐个核对，估计一致） |
| 普通怪每场 | 10~20 | `EncounterModel.MinGoldReward / MaxGoldReward`（Monster） |
| 精英每场 | 35~45 | 同上（Elite） |
| 首领 | 100 | 同上（Boss） |
| 假商人那一战 | 固定 300 | `FakeMerchantEventEncounter` 自己重写了 Min/Max |
| **A3「贫困」起** | **全部 ×0.75** | `AscensionHelper.PovertyAscensionGoldMultiplier` |
| 商店删牌 | 基础 **75**，每删一次 **+25** | `MerchantCardRemovalEntry.BaseCost / PriceIncrease` |
| 同上，A6「物价上涨」起 | 基础 **100**，每删一次 **+50** | 同上，走 `GetValueIfAscension(Inflation, 100, 75)` |

**推论**：150 金币 ≈ 打三四场普通怪（所以「见钱眼开」原来太容易）；
一局总金币收入大约 800~900（3 幕 ×（10 场普通 ×15 + 精英 40 + 首领 100）），
所以「持有 400 金币」是个真要攒的目标。

### 11.2 进阶 A1~A10（`AscensionLevel` 枚举顺序就是级别顺序）

`SwarmingElites(1) / WearyTraveler(2) / Poverty(3) / TightBelt(4) / AscendersBane(5) /`
`Inflation(6) / Scarcity(7) / ToughEnemies(8) / DeadlyEnemies(9) / DoubleBoss(10)`

- **A3 Poverty**：金币全部 ×0.75。
- **A5 AscendersBane（进阶之灾）**：开局白送一张诅咒，关键词 **`Eternal` + `Unplayable` + `Ethereal`**，
  `MaxUpgradeLevel = 0`、`CanBeGeneratedByModifiers = false`。
  ⚠️ **`Eternal` = 无法移除** → 任何「牌组里没有诅咒」型任务在 A5+ **永远做不完**（我们踩过，见 11.3）。
- **A6 Inflation**：商店删牌涨价。
- **A10 DoubleBoss**：一幕两个首领。

### 11.3 按真实数值做的平衡调整（0.1.6，来自玩家反馈）

| 任务 | 原来 | 改成 | 依据 |
|---|---|---|---|
| 一身清白 | 牌组零诅咒赢 5 场 | **最多 1 张诅咒** | A5 的进阶之灾带 Eternal，移除不掉 |
| 见钱眼开 | 攒 150 金币 | **250** | 开局 99 + 普通怪 10~20 → 150 只要三四场 |
| 一个都不放过 | 开第一个宝箱房（白送） | **第一幕开宝箱房 + 第一幕赢 1 场精英** | 每幕都有宝箱房；要有门槛又不能过了第一幕（套娃是前置型） |
| 我说过牌有没有懂的 | 一回合抽 8 张 | **15 张** | 运转卡组一回合能转好几圈 |
| 无限 | 单回合打出 10 张 | **15 张** | 同上 |
| 劣人手速 | 单回合打出 7 张 | **12 张** | 同上，和「无限」同机制不能一个 7 一个 15 |
| 守财之路 | 持有 300 金币 | **400** | 和「见钱眼开」的 250 拉开距离 |
| 展望未来 | 随机 X 费牌直接塞进卡组 | **卡牌奖励三选一、不限职业** | 玩家要求：X 费牌该是「奖励」不是「发牌」 |

### 11.4 「发一个卡牌奖励让玩家自己挑」的正规写法（以后照抄）

```csharp
var options = CardCreationOptions.ForNonCombatWithDefaultOdds(AllCardPools(), c => c.EnergyCost?.CostsX == true);
var reward  = new CardReward(picks, CardCreationSource.Other, player, options);
await RewardsCmd.OfferCustom(player, new List<Reward> { reward });
```

- `RewardsCmd.OfferCustom(Player, List<Reward>)` 是入口；另有 `OfferForRoomEnd`（走战斗结算那套）。
- `CardCreationSource.Other` = 「来自事件或遗物的奖励」。**别写 `Encounter`**，那会混进战斗奖励流程。
- `CardCreationOptions.ForNonCombatWithDefaultOdds(pools, filter)` 是现成工厂；
  `picks` 直接放 canonical 的 `CardModel` 就行（构造时会包成 `CardCreationResult`，不会改原模型）。
- **「不限职业」** = 自己遍历 `ModelDb.All` 里所有 `CardPoolModel` 取并集，别用 `owner.Character.CardPool`。
- ⚠️ `CardSelectCmd.FromChooseACardScreen(...)` 也能弹选牌界面，但它**要一个 `PlayerChoiceContext`**，
  而那是抽象类、new 不出来；走 `RewardsCmd` 这条路不需要 context。
- 相关类型：`Commands.RewardsCmd`、`Rewards.CardReward`、`Rewards.SpecialCardReward`、`Rewards.Reward`、
  `Runs.CardCreationOptions`（**在 `Runs` 命名空间，不在 `Entities.Cards`**）、`Runs.CardCreationSource`。

---

## 12. 多人局适配 + 全量数值重算（2026-09-23，用户要求「也要为了多人局考虑」）

### 12.1 反编译确认的多人规则

| 项目 | 结论 | 出处 |
|---|---|---|
| 敌人**血量** | 按人数放大，再乘分幕系数（一幕 ×1.1 / 二幕 ×1.2 / 三幕 ×1.2、三幕首领 ×1.3） | `Models.Singleton.MultiplayerScalingModel.GetMultiplayerScaling(encounter, actIndex)` |
| 敌人**格挡** | 同上（`ModifyBlockMultiplicative`）；2 人时直接返回人数 | 同上 |
| 敌人伤害 | **没有**跟着放大（那个模型只管格挡） | 同上 |
| 金币 | **按人各算、不共享** | `PlayerCmd.GainGold` → `player.Gold += amount` |
| 本地玩家 | `RunManager.Instance.NetService.NetId == Player.NetId` | `RunManager`（`LocalContext.NetId = NetService.NetId`） |
| 实测印证 | 三幕首领单人 321~599 血 → 3 人 ×1.3 ≈ **1250~2300**，和玩家截图里的 `2170/2782` 对得上 | `Aeonglass 512 / Queen 599` |

**推论（这决定了哪些目标要按人数放大）**：
- 「本局累计伤害」**不会**因为人多而变多：敌人血量 ×N，但每个玩家的输出份额不变、回合数也差不多
  → **累计类不放大**（放大了联机反而更难）。
- **单回合爆发**会被队友直接顶上去（喂能量、上增益、挂易伤）→ **这一类按人数放大**。
- 格挡 / 自伤没有明确放大依据 → 不放大。

### 12.2 代码上的三个修改（这才是真·多人适配）

1. **任务册原来只发给 `Players[0]`** → 改成发给 **本地玩家**（`Entry.LocalPlayer`，
   按 `NetService.NetId` 匹配）。原来多人局里除了房主，**其他人根本没有任务册**。
2. **静态 `QuestRunner` 的守卫**：本机会模拟所有玩家的遗物，而 `QuestRunner` 是静态单例、只服务本地玩家，
   所以只有本地玩家那本任务册能改状态（`QuestBookRelic.IsLocalOwner`，拦在
   `Grant` / `Set` / 每回合清零 / `QuestData` 存档入口 四处）。不拦的话 2~4 本任务册会往同一份进度里叠。
3. **目标按人数放大**：`QuestDef.ScaleWithPlayers`（单回合爆发那 6 条）+ `QuestRunner.PlayerCount`
   + `QuestState.Target`（**面板显示也必须走它**，别用 `Def.Target`）。

### 12.3 每幕真实数据（这次重算的依据）

| 幕 | 房间数 | 普通怪血量 | 精英 | 首领 |
|---|---|---|---|---|
| 一幕 Overgrowth | 15 | 11~160（均 72） | 78~127（均 95） | 173~252 |
| 二幕 Underdocks | 15 | 17~89（均 51） | 26~140（均 80） | 211~240 |
| 三幕 Hive | 14 | 24~171（均 98） | ~161 | 321~408 |
| 四幕 Glory | 13 | 30~261（均 140） | 234~300（均 270） | 512~599 |

→ **一局（三幕）总敌人血量约 2800**（约 24 场普通战 × 均 80 + 4~6 场精英 + 3 个首领），
金币收入约 800~900。**旧数值普遍只有真实值的 1/10**（「累计造成 150 伤害」一场首领就够了）。

### 12.4 按真实数据重算的数值（0.1.7）

| 任务 | 旧 → 新 | 依据 |
|---|---|---|
| 见血封喉（伤害） | 150 → **1200** | 一局总血量约 2800，取四成 |
| 铁壁防御（格挡） | 150 → **1000** | 同上量级 |
| 刀剑连鸣（攻击牌） | 25 → **90** | 一局出牌约 300 张，攻击牌约四成 |
| 行云流水（出牌） | 40 → **200** | 24 场 × 每场约 12 张 |
| 破釜沉舟（消耗） | 12 → **40** | |
| 稳如磐石（回合） | 15 → **60** | 一局约 100 个回合 |
| 药剂大师（药水） | 3 → **6** | |
| 凯旋之师（胜场） | 6 → **18** | 一局约 24 场普通战 |
| 精英猎手 / 屠龙者 | 1/1 → **3 / 2** | 一局 4~6 精英、3 首领 |
| 空手出招 / 蓄势待发 | 5/5 → **15 / 12** | |
| 硬抗到底（自伤） | 100 → **400** | |
| 毫发无伤 / 命悬一线 | 3/3 → **5 / 5** | |
| 淬火修行 / 篝火夜话 | 3/2 → **4 / 4** | 一局篝火约 6~9 个 |
| 挥金如土（购买） | 6 → **10** | |
| 白刃相见 / 重拳出击 | 8/8 → **25 / 25** | |
| 奇珍异宝 / 十八般武艺 | 6/10 → **10 / 12** | |
| 狂暴连击（单回合攻击） | 5 → **6** | 👥 |
| 问号猎手 / 只看不买 | 3/2 → **5 / 3** | |
| 闪电战 | 3 → **5** | |
| 通电（充能球） | 8 → **30** | 故障机器人一局几十个球 |
| 数值轮椅（单回合伤害） | 30 → **60** | 👥 成型回合 30 点太轻松 |
| 熟能生巧 / 连环拳 | 60/40 → **150 / 100** | |
| 铁壁（自伤） | 300 → **600** | |
| 稳如老狗 / 养生 | 5/4 → **6 / 5** | |
| 玩命（残血赢） | 4 → **5** | |
| 手不释卷（抽牌） | 120 → **150** | |
| 砍价高手（商店花费） | 300 → **600** | 一局收入约 800~900，花掉 600 才算真花 |
| 枯枝还魂（消耗） | 10 → **15** | 前置型，仍要在第一幕内做得到 |
| 小伤不理（格挡） | 200 → **350** | 前置型，第一幕量级 |

**前置型（EarlyBird）的那几条刻意压着没敢调太高**——奖励是「拿到之后才生效」的东西，
条件必须在**第一幕内**能达成，否则到手就废（这条设计原则见 §4.4）。

### 12.5 回滚/排查提示
- 多人适配改动都集中在：`Entry.LocalPlayer` / `Entry.OnRunStarted`、`QuestBookRelic.IsLocalOwner`、
  `QuestRunner.PlayerCount`、`QuestState.Target`、`QuestDef.ScaleWithPlayers`。
- 单人局这些逻辑等价于原来的行为（`PlayerCount = 1`、`IsLocalOwner = true`）。

---

## 13. 批之六：纯「乐子」12 条（83 → 95 条，2026-09-23）

| 任务 | 条件 | 判据 / 钩子 |
|---|---|---|
| 我从地狱蠕动出来了 | 以 1 点生命赢下一场战斗 | `AfterCombatVictory` 看 `CurrentHp == 1` |
| 谁最适合结婚 | 满血赢下一场首领战 | 同上 + `RoomType.Boss` + `CurrentHp >= MaxHp` |
| 咕咕嘎嘎 | 一场战斗一张牌都不打还赢 | 本场出牌计数 `_cardsThisCombat == 0` |
| 一拳超人 | 单次造成 100 以上伤害 | `AfterDamageGiven` → **Set 快照**（进度条显示你最高的一击） |
| 铁布衫 | 单次获得 50 以上格挡 | `AfterBlockGained` → Set 快照 |
| 三杀 | 单回合击杀 3 个敌人 | `AfterDeath` + `creature.IsPrimaryEnemy / IsSecondaryEnemy` |
| 复读机 | 单回合同一张牌打 3 次 | `AfterCardPlayed` 里按 `card.Id.Entry` 记次数（每回合清） |
| 自暴自弃 | 卡组 5 张以上诅咒 | `CheckDeckQuests` 扫 `DeckCurseCount()`（和「一身清白」正好相反） |
| 破防 | 单场格挡被打穿 5 次 | `AfterBlockBroken` |
| 满手牌 | 单回合手牌 10 张 | `AfterCardDrawn` 读 `CardPile.Get(PileType.Hand, Owner).Cards.Count` |
| 雷霆大机煲 | 单场激发 6 个充能球 | `AfterOrbEvoked` 累计（Defect 专属） |
| 噶人焖 | 累计获得 30 点辉星 | `AfterStarsGained(amount, gainer)`（Regent 专属） |

**这批新用到的 API（都编译验证过）**：
- `AfterDeath(PlayerChoiceContext, Creature, bool wasRemovalPrevented, float deathAnimLength)`
- `AfterBlockBroken(PlayerChoiceContext, Creature target, Creature? breaker)`
- `AfterStarsGained(int amount, Player gainer)`
- 判断敌人：`Creature.IsPrimaryEnemy` / `IsSecondaryEnemy`。
  ⚠️ **别用「不等于自己」来判断敌人**——队友、召唤物都会误判。
- 手牌数：`CardPile.Get(PileType.Hand, owner).Cards.Count`，上限是常量 `CardPile.MaxCardsInHand`。
- 「一拳超人 / 铁布衫」用的是 **Set 快照**而不是 Grant：进度条直接显示你的最高一击 / 最高格挡，比 0/1 好看。

**设计小结**：这批是「一次达成的成就型」，所以目标都写 1（阈值写在描述里）；
只有要展示进度的（一拳超人 / 铁布衫 / 满手牌 / 雷霆大机煲 / 自暴自弃 / 噶人焖）才用 Set 快照。

**找新梗的快捷路子**：直接 B站 搜「杀戮尖塔2 + 乐子 / 名场面 / 抽象 / 沙雕 / 笑死」，
**视频标题经常就是现成的梗**。工具 `tools/meme_hunt.py`，⚠️ 站方限流（连发会从 code=0 变成 412）。

---

## 14. 批之七 / 批之八：第二轮搜索（95 → 101 条，2026-09-23）

### 14.1 新任务

| 任务 | 条件 | 判据 |
|---|---|---|
| 肘击 | 铁甲战士打出 10 张「痛击」(Bash) | `AfterCardPlayed` + `card is Bash`（反编译确认有 `Cards.Bash`） |
| 小蓝人 | 用故障机器人赢 2 场首领战 | `AfterCombatVictory` + `RoomType.Boss` + `Owner.Character is Defect` |
| 骨小妹 | 累计召唤 5 次 | `AfterSummon(choiceContext, Player summoner, decimal amount)`（Necrobinder 的 Osty） |
| 我说弃牌有没有懂的 | 累计弃掉 30 张牌 | `AfterCardDiscarded(choiceContext, CardModel card)`（Silent） |
| 太牢了 | 一场战斗打到 10 回合还赢 | `_combatTurnIndex >= 10` |
| 杂耍 | 带着「杂耍」一回合打满 3 张攻击牌，累计 3 次 | `AfterCardPlayed` + `Owner.Creature.HasPower<JugglingPower>()` |

### 14.2 这轮查实的东西

- **「杂耍」= 牌 `Cards.Juggling`**（1 费 **能力** / 罕见）。效果（反编译 `JugglingPower`）：
  **每回合第 3 张攻击牌会被 `CreateClone()` 复制一张进手牌**；升级后额外获得「固有(Innate)」。
  社区把这张牌和这套玩法都叫「杂耍」，B 站有三条相关视频（10.9 万 / 4 万 / 3.6 万播放），
  热评里是「惊天八杂耍」「最尊重杂耍之人」这种说法。
- **「小蓝人」= 故障机器人的外号**（不是《以撒的结合》那个角色，也不是同名 mod）。
  依据是「尖塔梗百科」评论区那句「作者能不能下一次提到机器人的时候提一嘴小蓝人啊」。
  ⚠️ 这条**更正**了梗清单里早先的错判。
- 可玩角色共 5 个：`Ironclad / Silent / Defect / Regent / Necrobinder`（另有 Deprived / RandomCharacter 等）。
- **没做成**的：「粒子墙」(`Cards.ParticleWall`，0 费技能，效果没细看)、
  「看完不笑的是静默猎手」「毒种」「农蛋」「豹哥」「四不可当」（都是话术或外号，没有可统计的行为）。

### 14.3 找梗的两个有效姿势（合起来用）

1. **搜「乐子 / 名场面 / 抽象 / 沙雕 / 笑死」** → 视频标题经常直接就是梗（批之六）。
2. **搜「行话 / 梗科普 / 主播语录」→ 再拉热评** → 评论区经常直接列黑话和出处（批之七）。
3. **拿不准的术语先去游戏里查是不是真东西**：
   `ilspycmd -l c <sts2.dll> | Select-String <关键词>`（能列全部 6643 个类名）。
   「杂耍」就是这么查实的——它**真是一张牌**（`Juggling`），不是纯口嗨；
   反过来「基米精神」查不到任何类，就说明它只是社区说法。

---

## 15. 批之九：梗百科补全（101 → 105 条，2026-09-23）

### 15.1 重要发现：那个合集是 **18 期**，不是 14 期

我们早先只从单次搜索里数出 14 期。这次**翻页搜**（`page=1/2/3`，两个关键词各翻三页）
才扒全：**「尖塔梗百科」+「尖塔梗知道」共 18 期**，横跨 2025-03 到 2026-08。
→ **教训：搜索只取第一页会漏一半，找系列内容一定要翻页 + 多关键词。**

### 15.2 剩下 4 期解码结果（含义同样来自热评）

| 期名 | 含义 | 做成 |
|---|---|---|
| 【农种】 | 「二哥来了都能开刷的种子」= 开局爽到飞起 | 连续 3 场战斗不掉血 |
| 【绝不认输】 | 白夕Seal 直播间的「禁歌」（点歌进小黑屋），死磕不 SL | 以低于 10% 生命赢下 2 场 |
| 【观者？！】 | STS1 的观者（红蓝紫形态 / 无限 / 999 伤害），塔区对观者强度的怨念 | 单回合造成 300 点伤害（👥按人数放大） |
| 【你知道是谁】 | 固定句式描述离谱 feat 再问「你知道是谁吗」；关联「一层两牌无限」 | 只用 2 种牌赢下一场战斗 |

### 15.3 这批的技术点

- 新计数器：`_flawlessStreak`（连续无伤）、`_cardEntriesThisCombat`（本场用过的**不同**牌，用 `HashSet<string>`）。
- 「你知道是谁」要注意**别把 0 张牌的战斗算进去**（`_cardsThisCombat > 0` 才判）。
- 「连续 3 场无伤」需要在掉血时把连击清零（`else { _flawlessStreak = 0; }`），
  不然会攒成「累计 3 场」——那就和已有的「毫发无伤」重了。

### 15.4 梗库现状

18 期里 **16 期已做成任务**；剩 `onp`（主播圈互撕梗）和 `会员卡/删牌没点到`（手滑买错，追踪不到）。
**任务总数 105。**

---

## 16. 批之十：流派线（105 → 110 条，2026-09-23）

梗百科 18 期做完 16 期之后，换方向做**构筑流派**——这是玩家日常讨论里最稳的「梗源」
（毒流 / 力量流 / 无限流 / 杂耍流 / 召唤流……）。

| 任务 | 条件 | 判据 |
|---|---|---|
| 健身教练 | 累计给自己叠 30 层力量 | `BeforePowerAmountChanged` + `power is StrengthPower` |
| 我说叠毒有没有懂的 | 给敌人叠 50 层中毒 | 同上 + `power is PoisonPower` |
| 身法 | 累计给自己叠 20 层敏捷 | 同上 + `power is DexterityPower` |
| 破甲 | 给敌人叠 30 层易伤 | 同上 + `power is VulnerablePower` |
| 龟壳 | 单场战斗获得 200 点格挡 | `AfterBlockGained` 累计（进战斗清零） |

**踩点提醒（写这类任务必看）**：
- `BeforePowerAmountChanged` 里**只记 `amount > 0`**，减层数（掉集中之类）另外处理。
- **分清楚「给自己的」和「给敌人的」**：用 `target == Owner.Creature` 判断。
  不分开的话「叠毒」会把对手给你上毒也算进去。
- 能力类型名先核实：`ilspycmd -l c`，或直接看
  `%TEMP%\sts2full\MegaCrit.Sts2.Core.Models.Powers\` 下的文件名（共 268 个）。
  ⚠️ `TemporaryStrengthPower` 和 `StrengthPower` 是**两个东西**，别混。

**平台小结**：贴吧 / NGA 这轮没挖到能用的玩法梗（那边在吵剧情和舆论），
**干净的玩法类梗基本都在 B 站**。

---

## 17. 玩家报的 bug：buff/debuff 翻倍（2026-09-23，**已修**）

### 现象

工坊留言：「我这里安装这个模组会导致怪物和自己的 buff 和 debuff 翻倍」

### 真凶：`ModifyPowerAmountGivenAdditive` 的**语义**写错了（我写的）

游戏里的用法（反编译 `Hook.cs` 看到的）：

```csharp
decimal num = amount;
foreach (...) {
    decimal num2 = item.ModifyPowerAmountGivenAdditive(power, giver, num, target, cardSource);
    num += num2;            // ← 钩子返回「要加多少」；基类返回 0m
}
foreach (...) {
    num *= item.ModifyPowerAmountGivenMultiplicative(...);   // ← 这个返回「倍数」；基类返回 1m
}
```

我早期写成了 `return amount + 1m;` → `num += num + 1` = **`2×num + 1`**，
所以只要「厚积薄发」这个随身物生效，玩家给出的所有增益/减益都会翻倍（+1）。
改成 `return 1m;` 即可。

### ⚠️ 教训：Modify\* 钩子有三种完全不同的返回语义，**不能靠猜**

| 语义 | 钩子（例子） | 基类返回 | 我们该返回 |
|---|---|---|---|
| **增量**（`num += 结果`） | `ModifyPowerAmountGivenAdditive`、`ModifyBlockAdditive`、`ModifyDamageAdditive` | `0m` | 要加多少就返回多少（如 `3m`） |
| **倍数**（`num *= 结果`） | `ModifyBlockMultiplicative`、`ModifyDamageMultiplicative`、`ModifyPowerAmountGivenMultiplicative` | `1m` | 1.5 就是 ×1.5，0 就是清零 |
| **新值**（`num = 结果`） | `ModifyAttackHitCount`、`ModifyMaxEnergy`、`ModifyHandDraw`、`ModifyMerchantPrice`、`ModifyRestSiteHealAmount`、`ModifyCardRewardUpgradeOdds` | 原值 | 直接把改好的值返回 |

**判断方法**：去 `%TEMP%\sts2full\...\Hook.cs` 里搜钩子名，看调用点写的是
`+=` / `*=` / `=` —— **别凭基类返回值猜**。
（这次把全项目 11 个 Modify 钩子复查了一遍：只有那一处写错，其余都对。）

### 触发条件（回玩家留言时要讲清楚）

只有当**「厚积薄发」随身物生效**时才会翻倍；没拿那个奖励的存档不受影响。
如果玩家在没拿该奖励时也翻倍，那基本不是我们模组的问题，得排查别的模组。

---

## 18. 玩家报的 bug：多人数据不同步（2026-09-23，**已修**）

### 现象

工坊留言（林雷）：
>「多人联机在涅奥处接取任务后进第一个房间导致了**多人数据不同步**，重新进的时候原本接到的任务变成其他的了，而且打几张牌又不同步了」

### 真凶：我上一版的「多人适配」把发册改成了**非确定性**操作

上一版为了让每个玩家都有自己的任务册，改成了「**每个客户端只给自己那位本地玩家发**」。
问题是：主机发的是玩家 A 的任务册、客机发的是玩家 B 的 —— **各客户端的遗物列表不一样**，
一进房间（游戏会做状态校验）就判定不同步 ✗。

### 正确做法：开局类操作要**确定性**，运行时改状态要**同步**

| 场景 | 正确做法 | 反面教材 |
|---|---|---|
| **开局给所有人发东西**（遗物/牌） | 每个客户端**执行同一套操作**（对**所有**玩家都发一遍）。游戏自己的 `RunManager.ApplyAscensionEffects(player)` 就是这么做的 | 各客户端给自己那位发 → 列表不一致 |
| **运行中改动某个玩家的状态**（加金币/发遗物/加牌） | `RewardSynchronizer` 报给其他客户端（见下） | 直接改，别人不知道 → 数值对不上 |
| **运行中只改本地显示/本地数据**（任务进度、面板） | 不用同步（我们本来就只让本地那本任务册改静态状态） | —— |

### `RewardSynchronizer` 的四个同步接口（游戏自己的调用范例）

游戏里 `MerchantRelicEntry` / `CrystalSphereCurse` 是这么写的：

```csharp
await RelicCmd.Obtain(Model, _player);
RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(Model);   // ← 少了这句就会不同步
```
可用的有：`SyncLocalObtainedGold(int)` / `SyncLocalObtainedRelic(RelicModel)` /
`SyncLocalObtainedCard(CardModel)` / `SyncLocalObtainedPotion(PotionModel)`，
另外还有 `SyncLocalGoldLost(int)`。
⚠️ 这四个在**游戏本体里没有调用点**（因为本体的金币/遗物都走「奖励流程」，那套自带同步），
**它们就是给「直接改状态」的场合用的** ✗ 我们正好是这种场合，所以必须自己调。

### 本轮修的三处

1. **发任务册**：`Entry.GiveQuestBookToEveryone` —— 遍历 `state.Players` 全部发一遍（确定性）。
2. **奖励同步**：发遗物 → `SyncRelic`；加牌 → `SyncCard`；12 处加金币 → 统一走 `GainGoldSynced`。
3. **联机时禁掉两类本地改动**：
   - **随身物**（`BoonsEnabled => QuestRunner.PlayerCount <= 1`）：它们是只在本机生效的数值修正
     （多抽一张、商店打折……），主客机各算各的必然不同步 → 联机时不生效。
   - **升级牌组**（熟能生巧的奖励）：升级没有对应的同步接口 → 联机时改成折现 150 金币。

### 还没根治的（下次要做）

**随身物应该是「每本任务册各自持有 + 每个客户端对所有玩家都算一遍」**：
把 `QuestBoons` 的静态标志改成 `QuestBookRelic` 的**实例字段**（存档本来就按实例存 ✓），
修 hooks 里判断 `player == Owner` 而不是静态标志 —— 这样联机时每个客户端都会为每位玩家
正确套用他自己的随身物，既不用禁也能同步。（这次先禁掉，是因为重构要动 10 多个属性，
**先把不同步止住更重要**。）

---

## 19. 随身物重构：从「全局开关」改成「每本任务册各自记账」（2026-09-24）

§18 里那个 TODO 做完了。

### 改了什么

| 之前 | 现在 |
|---|---|
| `QuestBoons` 是**静态**开关（`[SavedProperty]` 转发到静态字段） | 16 个随身物全是 `QuestBookRelic` 的**实例属性**（`[SavedProperty] public bool HasX { get; set; }`） |
| 联机时用 `BoonsEnabled` 把随身物**整体禁掉** | 开关删了，联机正常生效 |
| 静态表被当"真实状态"用 | 静态表**只做本地面板的镜像**（`MirrorBoons()`，只由本地那本任务册写） |

### 为什么这样就对了

游戏在**每个客户端都模拟所有玩家的遗物**，并且修数值的钩子（`ModifyHandDraw` / `ModifyMerchantPrice`…）
会带上是**哪个玩家**在受影响（`player` / `giver` / `target`）。
所以只要「每本任务册记自己的随身物 + 钩子里判断 `player == Owner`」，
每个客户端就会为每位玩家算出**完全一样**的结果 → 确定性 → 不会不同步 ✓

### 顺手修掉的旧 bug

`HasSurge`（厚积薄发）、`HasBulwark`（铁壁）、`HasCombo`（连环拳）、`HasIndifference`（冷漠）
**这四个以前压根没有 `[SavedProperty]` 属性**——也就是说它们的存档转发根本没生效，
**读档回来会丢**。这次改成实例属性时补齐了 ✓

### 现在的多人规则（写代码时按这张表）

| 东西 | 联机怎么处理 |
|---|---|
| 任务进度 / 面板 | 只有本地玩家那本任务册能改静态状态（`IsLocalOwner` 守卫） |
| **随身物** | **实例字段 + 钩子里判 `player == Owner`** → 每个客户端各算各的，天然一致 |
| 发遗物 / 加金币 / 加牌 | 改完必须调 `RewardSynchronizer.SyncLocalObtainedRelic/Card/Gold` |
| 改牌组（升级牌） | ⚠️ 没有同步接口 → 联机时折现金币 |
| 开局给所有人发东西 | 每个客户端执行同一套操作（对**所有**玩家都发一遍） |

---

## 20. 玩家反馈轮（2026-09-24，Discord 群里提的）

玩家（Yukinoshita）在群里列了几条，逐条处理：

| 玩家原话 | 判断 | 改法 |
|---|---|---|
| 「累计收到 600 伤害，**被格挡的伤害还不算**，哪个角色能做到」 | ✅ 对。自伤类原来只算**掉血量**（`result.UnblockedDamage` / HP 下降），满血才 75，600 等于死八次 | 「硬抗到底 / 铁壁」改成算 `result.TotalDamage`（**含被格挡**），目标 800 / 1500 |
| 「x 费牌没几张，运气不好一整把抓不到，这任务根本完不成」 | ✅ 对，纯运气 | 「展望未来」2 张 → **1 张** |
| 「技牌不给格挡 + 50% 加伤，负面太超模」 | ✅ 对，代价 > 收益 | 「莽夫之道」技能牌格挡 **清零 → 减半**（×0.5） |
| 「5 次低于 30% 血量打赢基本是碰运气」 | ✅ 对 | 「玩命」5 次 → **3 次** |
| 「飞机和获取条件没有任何联动，纯纯数值」 | ✅ **最对的一条**。一代原版是「**使用药水时回复 5 点生命**」，我写成了「战斗开始回 4 血」，而拿它的任务是「药罐子」（用药水）——完全没联动 | 真遗物 + 随身物兜底都改成 `AfterPotionUsed` → 回 5 血，本地化文案同步 |
| 「mod 基本就是用来爽的，数值给高点无所谓」 | 记下方向 | 只砍「做不到 / 看运气 / 惩罚过重」的，**不**把有挑战的变送 |

### 教训

**遗物效果要按一代原版核对，别凭印象写**（扑翼飞机这条就是典型：名字一样、效果写错了）。
下次做「移植类」奖励，先查一代原文效果，再写实现。

---

## 21. 多人同步：奖励改走游戏自己的奖励流程（2026-09-24）

### 诊断（玩家反馈「更新后联机还是不同步」）

两个原因，都是设计问题：

1. **奖励是直接改状态**（`PlayerCmd.GainGold` / `RelicCmd.Obtain` / 往卡组加牌）——
   游戏本体发奖励走的是 **`RewardsCmd` + `Reward` 对象**（`GoldReward` / `RelicReward` / `CardReward` /
   `PotionReward` / `CardRemovalReward`），**同步是那套自带**（`RewardsSetSynchronizer`）。
   我早期补的 `SyncLocalObtainedGold/Relic/Card` 在游戏本体里**根本没有调用点**，多半不生效。
2. **任务进度/随身物存在「本机那本任务册」里**，而多人存档是**主机**存的那份 →
   SL 重进来，主机手里那本你的任务册是空的 → 任务就变了。

### 这轮做的（第 1 部分：奖励）

- `GainGoldSynced` → 改成 `RewardsCmd.OfferCustom(Owner, [new GoldReward(...)])`，失败才退回直接加。
- `OfferRelic(RelicModel)` → `RelicReward`，四个发遗物的助手都改成优先走它（失败兜底直接给 + 老同步调用）。
- 化学X 也走奖励流程。
- **战斗中拿到的奖励先排队**（`_pendingRewards`），`AfterCombatVictory` 里 `FlushPendingRewards()` 补发
  —— 奖励界面在战斗里弹不出来，而且战斗中直接改状态就是不同步的源头。

### 还没做（第 2 部分：任务数据）⚠️

任务进度/随身物仍然是**本机写本机的存档**，所以：
- 主机那份存档里**没有别人的进度** → 联机 SL 之后任务会变 ✗
- 各客户端的随身物状态也可能不一致 ✗

**要做对，得改两件事**：
1. **任务状态改成「每个客户端都为所有玩家算一遍」**（跟随身物重构一个思路：判断 `player == Owner`，
   用实例数据而不是静态单例）——这样每台机器算出来一样，主机存的也是对的；
2. **任务选择要同步**：现在是本地 UI 选，别人不知道。要接游戏的
   `PlayerChoiceSynchronizer`（`ReserveChoiceId` / `SyncLocalChoice` / `WaitForRemoteChoice`），
   或者把「选任务」做成一个自定义 `Reward`（走奖励流程，自带同步）。

这两件**必须真人联机实测**才能确认，别盲改。

---

## 23. 拆《海克斯符文》的多人做法（2026-09-25，用户要求）

mod 位置：`steamapps\workshop\content\2868840\3747501308\lib\0.111.0\HextechRunes.dll`（1691 个类）。

### 23.1 它怎么保证多人同步：**Harmony 补丁游戏自己的发放方法**

`HextechRewardsafetyHooks` 里挂了一串补丁（Prefix/Postfix + `__state`）：

| 补丁 | 打在哪 |
|---|---|
| `GainGoldPatch` | `PlayerCmd.GainGold(decimal, Player, bool)` |
| `RelicObtainPatch` | `RelicCmd.Obtain(...)` |
| `PotionProcurePatch` | `PotionCmd.TryToProcure(...)` |
| `CardPileAddPatch` | `CardPileAdd(...)` |
| `CardRewardSelectPatch` / `SpecialCardRewardSelectPatch` / `RelicRewardSelectPatch` | 三种奖励的 `OnSelect` |
| `RewardSelectUnsynchronizedPatch` | `Reward.SelectUnsynchronized()` |

每个补丁都包一层他们自己的「奖励事务」（`DoubleVisionRune.BeginDirectGoldReward` /
`CompleteDirectGoldRewardAsync` 这类）。

**在它的 dll 里搜到的 API 名**：`SyncLocalObtainedGold` / `SyncLocalObtainedRelic` /
`SyncLocalObtainedCard` / `SyncLocalGoldLost`（各 1 处）+ `RewardSynchronizer`（1 处）
+ `PlayerChoiceSynchronizer`（2 处）。

→ **结论 1：我们用的同步 API 是对的**（`SyncLocalObtained*` 就是干这个的）。
差别在于**它在游戏底层方法上打补丁**，所以连别人 mod 的直接发放也能被同步；
我们是在自己的奖励路径里手写调用 —— 对我们自己发的奖励等效，但覆盖面窄。

### 23.2 它怎么存「每位玩家各自的数据」：**NetId 归一化 + 位打包**

类名直接说明了一切：
- `HextechSavedPropertyNetIdCanonicalizer`：把「netId→属性名」的列表**归一化排序**
  （原版属性名在前，mod 属性名按 `StringComparer.Ordinal` 排序），并算 `ComputeNetIdBitSize`；
- `HextechGeneratedRuneDataCodec`：按上面的顺序**位打包**存每位玩家的数据；
- `HextechSavedPropertyBootstrap`：运行期用反射挂存档属性；
- `HextechMultiplayerDiagnostics` / `HextechMultiplayerScalingCompat`：多人诊断 + 缩放适配。

→ **结论 2：per-player 数据必须「顺序确定、编码确定」**，这样每个客户端算出来、
存下去的东西**字节级一致**，主机存的也就是对的 —— 这正是我们「任务进度存在本机、
主机那份是空的」那个 bug 的正解。

### 23.3 我们该怎么改（下一轮，按这个来）

1. 任务进度/随身物**不再用静态单例**，改成**按玩家存放**（NetId 作键、顺序确定）；
2. 每个客户端对**所有玩家**都算一遍（跟随身物重构一个思路：钩子里判 `player == Owner`）；
3. **任务选择**要么接 `PlayerChoiceSynchronizer`（它也用了这个），要么做成自定义 `Reward`
   走奖励流程（自带同步）；
4. 奖励发放：保留现有的 `SyncLocalObtained*` 调用 ✔（和它一致），或者学它在
   `PlayerCmd.GainGold` / `RelicCmd.Obtain` 上打补丁（覆盖面更广，但会和它**双重同步**，别同时做）。

---

## 24. 多人同步 第 2 部分：任务进度改成「一册一份」（2026-09-25 已实现）

按 §23 的结论动手了：

| 文件 | 改动 |
|---|---|
| `Quests/QuestRunState.cs` | **新增**：一本任务册自己的进度（从原静态 `QuestRunner` 搬过来的逻辑） |
| `Quests/QuestRunner.cs` | 从「状态本身」改成**门面**：只指向**本地玩家那一本**（`LocalState`），UI 照旧读它 |
| `Relics/QuestBookRelic.cs` | 持有 `_state`；`Grant/Set` **不再拦「非本地」**（每本推进自己的）；`Reward` 里才拦（**只由本人那台发奖**，否则四倍奖励）；存档桥读写 `_state` |
| `Entry.cs` | 发册/读档时把本地那本注册成 `QuestRunner.LocalState`；**多人局自动分配任务** |
| `Quests/QuestCatalog.cs` | 新增 `PickOffersSeeded`（同一个种子抽出同一批，洗牌用 `System.Random(seed)`） |
| `UI/QuestSelection.cs` | 多人局不弹本地接取界面 |

**（§25 已推翻这条，改成广播了；下面这段保留作为退路说明）多人局为什么改成自动分配任务**：任务挑选是纯本地操作，各客户端挑的不一样 →
主机存档里没有别人的进度 → SL 回来任务变样。
改用「同一颗种子（由玩家 NetId 推出）抽同一批」，每个客户端算出来完全一样 → 天然同步、存档也是全的。
单人局不变，保留手动挑的乐趣。

**奖励仍然只由本人那台客户端发**（`Reward` 里判 `IsLocalOwner`），发的时候走
`RewardsCmd.OfferCustom`（游戏自己的奖励流程，自带同步）✓

### 待实测（必须真人联机）
1. 两人联机：各自的任务是不是**双方都看得到进度**（不该出现"我这边 3/5、他那边 0/5"）；
2. 联机中存档退出再进：任务列表与进度**原样回来**（这次应该不再变样）；
3. 完成任务后：奖励是不是**只发一次**（不是两台各发一份）。



---

## 22. 「展望未来」改版（2026-09-24，玩家建议）

**旧**：打出 1 张 X 费牌 → 奖励化学X + 一张 X 费牌。
问题：X 费牌本来就少，运气不好一整局抓不到，任务完不成（玩家原话）。

**新**：**带着 250 金币进商店 → 进店当场自动扣 250** → 奖励遗物化学X + 一张**任意职业**的 X 费牌（三选一）。

实现要点：
- 枚举成员 `XCardsPlayed` **改名**成 `ShopXDeal`（**只改名、不动位置**：存档存的是 `(int)Kind`，位置一动老存档就串位）。
- 进店逻辑放在 `AfterRoomEntered`（这个方法改成了 `async`）：先判断
  「这条任务**正在进行中**」（`QuestRunner.Active.Any(...)`）**并且** `Owner.Gold >= 250` 才扣钱
  —— 不判断的话，没接任务的人进店也会被白扣 250。
- 扣钱用 `PlayerCmd.LoseGold(250, Owner, GoldLossType.Spent)` + `SyncLocalGoldLost`。






---

## 25. 多人局「自己挑任务」回来了：用游戏自己的玩家选择通道（2026-09-25）

§24 那个"多人局自动分配"被玩家否了（"别人的海克斯就是能开局三选一还能刷新"）。
拆了 `HextechRunes.dll`（`lib\0.111.0`，ilspycmd 反编译，233 万行那个文件里搜
`ReserveChoiceId`）确认：**海克斯就是走 `PlayerChoiceSynchronizer`**，而且它写了一整套兜底
（事件等待 + 超时 + 反射读 `_receivedChoices` 缓存 + `HextechChoiceCodec` 编码）。
我们只用最核心那三步，不抄它那套兜底。

### 现在的做法（`Quests/QuestPickSync.cs`）

1. **开局**：每个客户端都按同一玩家顺序，给**每位玩家**各预留一个选择编号
   （`RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(p)`）→ 各端编号一致；
2. 本机玩家挑完（5 选最多 3）→ `SyncLocalChoice(local, id, PlayerChoiceResult.FromIndexes(任务种类))`
   把结果广播出去；
3. 其他客户端 `await WaitForRemoteChoice(p, id)`，收到后写进**那位玩家自己的任务册**
   （`p.GetRelic<QuestBookRelic>().State.TryAdd(def)`）。

**身份证用 `QuestKind`（int）而不是目录下标**——枚举各端一致，改目录顺序也不会串。
（`QuestCatalog.Find((QuestKind)raw)` 还原。）

拿不到通道（`PlayerChoiceSynchronizer == null`）或出异常 → 返回 false，
调用方退回 `AutoAssignQuestsForMultiplayer`（同种子自动分配）。**宁可没得挑，也不能不同步。**

### ⚠️ 铁律：编号必须"各端同进退"

`ReserveChoiceId` 每调一次，该玩家在这一局的编号就 +1；**各客户端必须调同样多次、同样顺序**。
只在本机多调一次（比如"进商店就预留一个"），各端编号就错位 →
游戏自己后面那些「选哪张牌」`WaitForRemoteChoice` 会永远等不到 → **卡死**。
所以：

- **商店那次挑选，多人局直接关闭**（`QuestSelection.OnShopEntered` 里 `PlayerCount > 1` 就 return）。
  商店是"谁什么时候进都不一样"的时机，进不了这套编号。
- 多人局只在**开局**挑一次（每人 5 选 3）。

### 顺带加的：刷新候选

`QuestSelection` 里加了「刷新候选」按钮（玩家要求，学海克斯）。
刷新是**纯本机**行为（重抽 `QuestRunner.Offers`），最后只广播"挑中了哪几个"，
所以两人局里也能刷，不会不同步。

### 多人局实测清单（还没测）

1. 两人局：两人各自点「接取任务」→ 都能拿到自己的 5 个候选、都能挑 3 个；
2. 挑完在对方机器上：**那位玩家的任务册里有对应任务**（面板只看自己那本，看进度要进战斗验证）；
3. 两人**同时**挑（都开着选单）→ 互不等待、都能成功；
4. 挑完存档退出再进 → 任务列表与进度原样；
5. 挑完后进战斗、用一次"选牌"的牌（比如弃牌类）→ **不卡**（验证编号没错位）。


---

## 26. 2026-09-25 两条硬教训（0.2.4）

### ① 发卡牌奖励：必须先变成「这名玩家名下的卡」

玩家 hxy8241 反馈：展望未来的 X 费牌奖励**界面弹出来了但牌选不中**，连【测试】直接完成都不行。

原因：`new CardReward(cards, source, player, rerollOptions)`（手动指定牌那个重载）里
塞的是从 `ModelDb.All` 卡池里捞出来的**公共卡模型（canonical）**。界面能渲染，
但选中那一步拿到的是公共模型，不是这名玩家名下的可变卡 → 点不中 / 报错。

**正确写法（`CardGift.OfferXCardReward` 已改）：**

```csharp
var owned = picks.Select(m => owner.RunState.CreateCard(m, owner)).ToList();
var reward = new CardReward(owned, CardCreationSource.Other, owner, options);
```

《海克斯符文》的 `CreateCardsToOffer` 也是 `player.RunState.CreateCard(byId, player)` 再发（§23）。

顺带：`QuestRunner.ForceCompleteAll` 改成**每条任务各自 try/catch** ——
以前一条任务的奖励抛异常，后面所有任务就都不发了，看起来就是"测试按钮点了没用"。

### ② Steam 创意工坊简介上限是 **8000 字节**，不是 4000 字

`k_EResultInvalidParam` 又踩了一次：这次简介 3670 个中文字 = **8051 字节** → 提交失败。
（0.2.3 那次 3506 字 ≈ 7667 字节 → 成功。）

**中文一个字 3 字节**，所以简介上限大约只有 **2600 个汉字**。
准备上传前先量字节，别量字数：

```python
print(len(open('workshop/description.md', encoding='utf-8').read().encode('utf-8')))
```

超了就压老更新日志（新版日志留着，陈年条目合并成一行）。现在 7639 字节。


---

## 27. 2026-09-25 第二波（0.2.5）：静态引用 + 异步发册

### ① 游戏日志在哪（省得下次再找）

```
%APPDATA%\SlayTheSpire2\logs\godot.log
```

Godot 会把上一次的归档成 `godot<时间戳>.log`，**读之前先看时间戳**是不是当前这局。
我们自己的里程碑行（grep 这些就够）：

| 日志行 | 含义 |
|---|---|
| `[QuestSpire] Run started: quest book granted to N player(s).` | 新局发册 |
| `[QuestSpire] Run loaded: quest book restored, N active quest(s)` | 读档（**新局里出现这行就是 bug**） |
| `[QuestSpire] QuestSelection confirmed: picked N quests.` | 接取成功 |
| `[QuestSpire] Quest completed: 任务名 -> ...` | 某条任务完成并发了什么奖 |

### ② 玩家实测「放弃一局重开，旧任务还在」的根因

日志证据：新局只有 `Run started`，**没有** `QuestSelection confirmed` —— 接取根本没进新局。

原因：`OnRunStarted` 里发册是 **fire-and-forget**（`_ = GiveQuestBookToEveryone(...)`），
紧接着就 `QuestSelection.ArmRunStart(...)`。发册是 async 的，点亮按钮那一刻
`QuestRunner.LocalState` 还指向**上一局**的账本 → 候选从上一局抽、接取写进上一局。
面板是从门面读的，所以显示的还是上一局那几条任务。

**修法（`Entry.cs`）**：

1. 新增 `GrantThenArmAsync`：`await GiveQuestBookToEveryone(...)` → 再
   `QuestPickSync.StartMultiplayer` / `AutoAssignQuestsForMultiplayer` → 最后才 `ArmRunStart`；
2. `RunEndedEvent` 里把 `QuestRunner.LocalState = null; RewardSink = null;`（局结束就断开引用）；
3. 新局开头也先清一次（双保险）。

**教训**：静态门面（`QuestRunner`）+ 异步初始化 = 时序 bug。
凡是"点亮 UI 让玩家操作"的动作，必须排在**数据准备好之后**。

### ③ 刷新限一次（玩家要求）

`QuestSelection` 加 `_rerollsLeft`，每批候选抽出来时置 1（开局 5 选 3、商店 3 选 N 都一样），
用掉就不再显示「刷新候选（仅 1 次）」按钮。

### ④ 展望未来（待查）

0.2.4 之后玩家报「展望未来好像根本没发奖励」。日志里**没有** `Quest completed: 展望未来`，
说明触发条件没满足（那条任务当时没在手上，或**进商店那一刻金币 < 250**）。
已在商店钩子里加日志：

```
[QuestSpire] 展望未来：进了商店，金币 X/250。
[QuestSpire] 展望未来：已扣 250 金币，准备发奖。
```

下次测试如果还是不发奖，先看日志有没有这两行：
- 完全没有第一行 → 任务当时不在进行中（或 `IsLocalOwner` 为假）；
- 有第一行、X < 250 → 就是条件没到，属于设计如此；
- 有第二行但没有 `Quest completed` → 那才是发奖流程的问题。


---

## 28. 2026-09-25 第三波：账本绑定「哪一局」+ 奖励排队泄漏

### ① 「下一局任务还是有」再修一层：账本必须绑定 RunState

§27 改了"发完册再点亮"，但静态引用晚一步更新这件事**不该靠时序保证**。现在改成显式判断：

```csharp
QuestRunner.BindLocal(book);   // 发册 / 读档时调用：记下 book.State，并记下"当时这一局"
QuestRunner.ClearLocal();      // 局结束 / 新局开头调用
```

`LocalState` 读取时比对 `Entry.CurrentRunRef`（`RunManager.State` 是私有的，只能自己在
`RunStarted` 里记、`RunEnded` 里清）：

```csharp
if (cur != null && _boundRun != null && !ReferenceEquals(cur, _boundRun)) return null;  // 上一局的账本 → 当空
```

这样**即使时序又出岔子**，上一局的账本也绝不会显示在新局的面板里、更不会被写进新局。

顺手加了诊断日志：`QuestRunner: 已绑定本地任务册（本局任务 N 条）。` ——
新局应该是 **0 条**；读档是实际条数。以后玩家说"任务不对"，先看这行。

### ② 「展望未来没发奖励」的真凶：奖励排队泄漏

日志证据（19:16 那局）：

```
1537 [QuestSpire] 战斗中完成「展望未来」，奖励等这场打完再发。
1557 [QuestSpire] QuestPanel unmounted.          ← 玩家放弃这局
```

「展望未来」明明是**商店**触发的，却被 `CombatManager.Instance.IsInProgress` 判成"战斗中"
进了 `_pendingRewards` 队列；玩家一放弃，队列跟着遗物一起没了 → **奖励彻底丢失**。

修法：进新房间时只要不在战斗里就补发。

```csharp
if (_pendingRewards.Count > 0 && !CombatManager.Instance.IsInProgress)
    _ = FlushPendingRewards();
```

**教训**：凡是"先攒着等条件满足再发"的队列，都必须有**多个出口**（战斗胜利、离开战斗、
进新房间、局结束前），只挂一个出口就等于给玩家准备了一个静默丢奖的坑。

### ③ 一个反直觉的结论

日志里从头到尾**没有出现过** `Run loaded: quest book restored`。
也就是说：玩家的"放弃一局再开"确实每次都走**新局分支**（新账本、空进度），
"旧任务还在"纯粹是**静态引用没跟上**造成的显示/写入错位，不是存档残留。
所以这类 bug 不要在存档上找，要在"谁在读静态变量"上找。


---

## 30. 交接：当前状态（2026-09-25 下午；**2026-09-26 深夜已更新**，供下一轮直接接手）

### ⛔⛔ 三条写死的铁律（每一条我都在这一轮违反过，别再犯）

**① 用户跑的是工坊那份，不是你编译的。**
日志实锤：`Skipping loading mod QuestSpire, it is set to disabled in settings`（本机 mods 那份被禁用）
+ `Loading assembly DLL ...\workshop\content\2868840\3806270140\QuestSpire.dll`。
→ **本机 `dotnet build` 的改动他跑不到**，要让他验证 = **必须先传到工坊**。

**② 绝对不要在"战斗结算那一刻"弹我们自己的奖励框。**
`BeforeCombatRewardOffered` / `AfterCombatVictory` 这两个时刻，官方「搜刮」界面正在搭建，
我们再 `RewardsCmd.OfferCustom` 一个盒子就会**打架** —— 玩家早就报过：
「凭空弹界面 / 奖励丢失 / 还没点就弹了界面、遗物还没拿到」。
→ 正确节奏（玩家定的）：**战斗中拿到的奖励先攒着，进了下一个房间再发**
（`AfterRoomEntered` → `FlushPendingRewards`）。我在 0.2.14 违反过一次，玩家当场又报了一次"卡掉"。

**③ 别往游戏的「搜刮」界面里塞任务奖励。** ~~试了三种写法全失败（详见 §34 表格）。~~
⚠️ **这条已被 §36 推翻**：海克斯符文证明**能塞**（`Reward` 直系子类 + `AfterCombatVictory`
+ `CombatRoom.AddExtraReward`），失败的原因是**我们的奖励类写法不对**，不是"塞不进去"。

### 版本现状
- **创意工坊最新：0.2.22**（2026-09-28 18:22 上传，item 3806270140）。
  0.2.22 = **结构改动**：任务奖励改成 `AfterCombatVictory` → `CombatRoom.AddExtraReward`
  （照《海克斯符文》），**直接进官方「搜刮」界面**，不再自己弹框。详见 **§40**。
- ⚠️ **这个改动还没验证过**。用【测试】按钮 1 分钟能验一次（见 §40 的验证步骤）。
  如果 `[AfterCombatVictory] 任务奖励：N 项已塞进本场「搜刮」单子` 这行出现 → 成了；
  如果出现 `[BeforeCombatRewardOffered] …这里再试一次` → 说明前一个时机太晚，留后一个。
- 本地 `mods\QuestSpire` 编译时自动重建（和工坊版**同 id**，随时可切换用哪份）。
- mod 依赖：RitsuLib + **BaseLib**（3737335127）。

### 未修 bug（按优先级）
1. **验证 0.2.22 的结构改动**（见 §40）。成了就把 `OfferCustom` 那条老路彻底删掉。
2. 联机"我接的任务显示成主机的" —— 已加识别日志，等下次联机日志确认。
2. 重复/多框：0.2.18~0.2.20 加了三层（对账 + 幂等 + 串行），**等实测**。
3. ~~「选牌附魔点不动」~~ → 已修（§31.1）。
4. **联机"我接的任务显示成主机的"** —— 已加识别日志，等下次联机日志确认。

### 排查工具（重要，别再从零找）
- `projects\quest-spire\tools\scan_log.py`：把日志里**所有**异常去重列出来。**教训**：只 grep 我们自己的名字会漏掉"游戏侧因我们而抛的异常"（RelicReward.OnSelect 那次就是漏的）。
- **RitsuLib 联机分歧诊断包**：`%APPDATA%\SlayTheSpire2\logs\ritsulib_state_divergence_*.zip` → 里面的 `state-divergence-report.txt`。只看 `xxx: local=… ; remote=…` **两边不一样**的行（7771 行里只有 4 条不同）。上次靠它定位到：**五条悟 mod 的卡费用两边不一致（31 vs 35）** + **双方 mod 数量不同（39 vs 40）** → 不是我们的锅。
- 游戏日志：`%APPDATA%\SlayTheSpire2\logs\godot.log`（会涨到几百 MB；`godot<时间戳>.log` 是上一局的归档）。

### 联机铁律（踩过的坑）
- **双方 mod 列表必须完全一致**（数量+版本）；顺序不同也会被提示。
- **mod 只在开局加载**：改完 dll 必须**重启游戏**（否则跑的还是旧代码）。
- **战斗中禁止同步遗物/卡牌**：`Tried to sync relic event … during combat! This is not allowed` —— 硬发会被判状态分歧、直接踢回主菜单。所以遗物要"先排队、出了战斗再发"。
- 附魔/选牌的现成做法在《更多附魔》mod 里（`CardSelectCmd.FromDeckForEnchantment`、`EnchantInternal`），零件在 BaseLib（`CardUpgradeReward` / `CardTransformReward`）。

### 上传流程 & 限制
- 改 `workshop/description.md` + `meta.json.changeNote` → `python tools\prepare_workshop.py` → `ModUploader.exe upload -w "D:\software\sts2-mod-uploader\QuestSpire"`。
- **简介上限 8000 字节**（不是字数；中文 3 字节/字）——超了就是 `k_EResultInvalidParam`，压老日志。
- 需要 Steam 开着；上传成功后用 `workshop\check_item.py` 核对长度。
- ⚠️ **2026-09-26 实测：简介已经顶到 7958/8000 字节了**，再加东西必须先压缩老日志。
  量法：`prepare_workshop.py` 跑完后直接看 `workshop.json` 里 `description` 的 UTF-8 字节数
  （`len(desc.encode('utf-8'))`），别只看字数。这次就是加了 0.2.12 日志后到 9590 → 上传失败，
  压缩了 0.2.7~0.2.11 的老条目才降到 7958。
- `ModUploader.exe` 结束时**总是返回 exit code 1**（它自带的 breakpad minidump 处理程序导致），
  **别拿退出码判断成败**——看最后一行有没有 `Successfully uploaded`。

### 设计规则（玩家定的，别再犯）
1. **条件必须和奖励相关**；
2. **奖励不能抹掉条件的结果**（升级类条件不能配"变牌"这种清空升级的奖励）；
3. 任务条件要用**没被用过的维度**，而且**必须可行**（按一局能拿到的资源估上限：删牌次数、稀有牌张数、升级次数、宝箱房数量…）；
4. 测试期给新任务 `Weight = 8` 方便抽到；
5. 交付时**不要让玩家"麻烦再试一次"**（他没有义务当测试员）。

---

## 31. 「选牌附魔点不动」真凶抓到了 + 任务上限提到 20（2026-09-25 晚，**已修**）

### 31.1 「选牌附魔」点不动：不是参数写错，是**弹界面的时机**错了

**现象（玩家原话）**：附魔的选牌界面**弹出来了但点不中**，只能按「前进」。
触发：泰拉瑞亚·以盾取胜 / 明日方舟·部署 / 魔兽世界·附魔（三条都用附魔奖励）。

**真凶**：旧写法是任务一完成就自己调 `CardSelectCmd.FromDeckGeneric(Owner, prefs)` 弹选牌界面。
日志（`%APPDATA%\SlayTheSpire2\logs\godot.log`，约 898816 行）把整件事写得很清楚：

```
[ERROR] [QuestSpire] 选牌附魔失败（固有）: System.Threading.Tasks.TaskCanceledException
   at MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardGridSelectionScreen.CardsSelected()
   at MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromDeckGeneric(...)
   at QuestSpire.Relics.QuestBookRelic.EnchantByChoice(CardKeyword kw, String cnName)
   ...
   at MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.NCardGridSelectionScreen._ExitTree()
```

**紧接着下一段日志就是 `TreasureRoom.EnterInternal`（玩家正在进宝箱房）。**
也就是说：任务是在「刚打完 / 正要切房间」的瞬间完成的，房间切换把我们的选牌界面**拆掉了**，
`_ExitTree()` 取消了选牌任务 → 玩家看到的那个界面根本点不动。

> 顺带说明为什么以前一直没查出来：日志里 `AncientWaifus` 这个第三方 mod 每个输入事件都刷
> 几十行 `MissingMethodException`，把我们的那一条 ERROR 埋得很深。**要用 `tools\scan_log.py`
> 或直接按关键字捞，不要靠肉眼翻。**

### 修法：**不自己弹界面了**，改成奖励界面里的一张「奖励」
新增 `QuestSpireCode\Rewards\KeywordEnchantReward.cs`：

- 继承 BaseLib 的 `CustomReward`，`RewardType` 借用 `BaseLib.Common.Rewards.CardUpgradeReward.CardUpgrade`
  （它已经注册好存读档和图标），`IconPath` 直接借游戏自带的「升级一张牌」图标，`RewardsSetIndex = 8`
  和 BaseLib 的升级奖励同序号 → 多人局能正常同步。
- 描述文案走我们自己的本地化表（`localization\zhs|eng\relics.json` 里新增的
  `QUEST_SPIRE_ENCHANT_REWARD.title / .description / .generic`），运行时用 `LocString.Exists`
  顺着 `relics` → `gameplay_ui` 找，取不到就退回游戏自带的词条名（绝不显示内部 key）。
- **只有玩家在奖励界面上点了这张奖励**，才去开选牌界面——那是稳定时机，
  和 BaseLib 的 `CardUpgradeReward` / `CardTransformReward` 完全一样（那两件一直是好的）。
- `QuestBookRelic` 里三处 `_ = EnchantByChoice(...)` 全部改成
  `_batch.Add(new KeywordEnchantReward(Owner, kw, "保留"/"固有"))`，跟着原来那套
  「战后并进官方奖励界面」的队列走；旧的 `EnchantByChoice` / `EnchantRandomCard` 已删除。
- 选牌参数：`new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1) { Cancelable = true,
  RequireManualConfirmation = true }`，并加了 filter「已经挂过这个词条的牌不让再选」。
- **兜底**：选牌万一还是没完成（玩家自己取消 / 又撞上切房间），自动退回「随机一张」，
  奖励绝不打水漂。日志会写 `附魔（随机兜底）：给「…」挂上「…」`。

### 31.2 任务上限 10 → 20 + 联机开局给 8 条（2026-09-25 晚，玩家要求）

**背景（先记住这个结论，别再问"是不是又要改成共用一个册子"）：**
多人局的任务册**本来就是每人各一本、不是共用的**，面板也是**按人分开显示**：
- 每本 = 每位玩家自己的 `QuestBookRelic`，各自 `State.Active`；
- 每台客户端都会为**所有玩家**推进度（钩子只判 `Owner`），所以各端算出来一样、主机存档是全的；
- 面板显示 = `我` + 每个队友那本，队友的行带 `[角色名]` 前缀（`QuestPanel._Process`）。

**改了两处：**

1. **每人上限 10 → 20**：`QuestRunState.MaxActive = 20`。
   ⚠️ 这是**每人**的上限 —— 4 人局同时可能有 **80 条**在跑。进度钩子按人头走，
   都是整数加减，实际开销很小，但**以后加"每条任务都要扫一遍"的逻辑时要先想这件事**。
2. **联机开局：候选 10 个、最多接 8 条**（单人局仍是 5 选 3）。
   原因：商店那次「接取任务」在联机里是**关掉**的（随时可能发生，会打乱游戏的选择编号），
   所以联机**开局这一次就是整局的量**，原来 3 条根本用不到 20 的上限。
   见 `QuestSelection.StartOffers / StartCap`（按 `QuestRunner.PlayerCount` 自动切换）。

**顺带解决的显示问题**：4 人 × 20 条 = 80 行，全铺开糊满屏幕。
现在面板顶部多一行**可以点的名字**：`我 3/20`、`铁甲战士 2/20`、`静默猎手 0/20`…
**点谁就收起谁那批**（收起时变灰、显示 ▸），再点放出来。
单人局这一行整行隐藏，行为和以前完全一样。实现在 `QuestPanel.RebuildOwnerBar()`。
面板标题也改成 `尖塔任务  我 0/20（0/20）` 这种写法，免得和队友的数字混淆。

### 31.3 验证状态（这两件事一起验）
- `dotnet build` 通过（0 错误 0 警告），dll + pck 已部署到
  `D:\software\steam\steamapps\common\Slay the Spire 2\mods\QuestSpire`。
- **还没真人实测**（改完 mod 必须重启游戏）。下一轮请玩家验这几条：
  1. **附魔**：接到这三条任务之一 → 完成后奖励界面里应出现一张「附魔一张牌」奖励 →
     **点它**才弹选牌界面 → 选中一张牌 → 日志出现 `附魔：给「…」挂上「…」`。
  2. **上限 20**：单人局在商店多接几次，标题应显示 `…/20`。
  3. **联机开局**：开一局 3~4 人，候选应是 10 个、最多能勾 8 条。
  4. **按人收起**：多人局面板顶部应有 `我 x/20`、`[角色名] x/20` 这些可点的名字，
     点一下就收起那一批、再点放出来；**单人局这一行不该出现**。

### 反编译工具（这轮新造的，在 `D:\software\quest-spire-本地备份\probe`）
查游戏 / BaseLib 的真实 API 签名用。命令行（不是 mod 代码）：
`probe.exe "<类型名正则>" "<方法名>"` → 把类型成员、调用点、指定方法的 IL 全打出来。
⚠️ 结论：**别再靠猜 `CardSelectorPrefs` 的构造参数** —— 反编译确认只有
`.ctor(LocString prompt, Int32 selectCount)` 和 `.ctor(LocString, Int32 min, Int32 max)` 两种，
`RequireManualConfirmation` / `Cancelable` / `ShouldGlowGold` 都是属性。

---

## 32. 【重大】"任务完成了却拿不到奖励"的真凶：**加奖励的时机晚了**（2026-09-26 晚，**已修**）

### 玩家反馈（工坊留言，2026-08-26）
> `hxy8241`：只狼的升级卡牌任务和之前的枯枝一样，拿不到奖励。

（更早还有一条同类的：「尽做的升级卡牌任务和之前的就是一样，拿不到奖励」。）

### 真凶：我们把奖励并进战后界面的**钩子选错了**
游戏发战后奖励的顺序（反编译 `CombatRoom.OfferRoomEndRewards` 确认）：

```
OfferRoomEndRewards()
  ├─ Hook.BeforeCombatRewardOffered(rewards, runState, room)   ← 加奖励的正确时机
  └─ RewardsCmd.GenerateForRoomEnd() → RewardsSet.WithRewardsFromRoom()
        → 读 CombatRoom.ExtraRewards → 生成奖励界面
```

而我们**从 0.24 那个版本起**一直把 `FlushIntoRoom(room)` 挂在 `AfterCombatVictory` 里。
`AfterCombatVictory` 触发时**奖励界面早就生成完了** —— 这时候再 `room.AddExtraReward(...)`
玩家永远看不到。**奖励就这样静默消失了。**

日志侧证（`godot.log`，中文被 GBK 控制台显示成乱码，内容可辨认）：

```
518218  遗物先排队，打完这场再发（战斗中不能同步遗物）。      ← QuestBookRelic 约 1389 行
518219  Quest completed: 药罐子 -> 获得「玩具扑翼飞机」        ← 已完成、奖励已"排队"
      ……但同一场战斗的 Reward set 里只有 GoldReward/CardReward，没有我们的东西
```

**受影响的奖励**：所有走 `_batch` 的（战斗中途完成的任务）——
**枯枝还魂（遗物）、只狼·弹反 / 宝可梦·进化（升级 / 变牌）、以及三条附魔任务**。
金币类因为战斗外直接到手，所以看起来没事，这也是为什么之前没发现。

### 修法
1. 把"并进战后奖励"从 `AfterCombatVictory` **移到 `BeforeCombatRewardOffered`**（新重写的方法）：
   在里面 `FlushIntoRoom(room)` → `room.AddExtraReward`，紧接着游戏自己的
   `WithRewardsFromRoom` 就会把它读进 `rewards.Rewards`。金币 / 遗物 / 卡牌 / 升级全部原样走官方流程（自带同步）。
2. `AfterCombatVictory` 里**删掉**那次调用，并留了醒目注释**别再挪回去**。
3. 多人局守卫：只在 `IsLocalOwner` 时并（别人的奖励由他自己那台发）。

### ⚠️ 已知残留（下一轮可以考虑）
靠"胜利那一刻"判定的任务（只狼·弹反 `ParryFlawless`、文明6 `OnlyOneCostWin`、
泰拉瑞亚/饥荒/暗黑/原神/明日方舟那几条）是在 `AfterCombatVictory` 里判定的，
**判定发生在 `BeforeCombatRewardOffered` 之后** → 这批奖励赶不上本场的界面，
会走下一个房间的 `FlushPendingRewards` 弹出（晚一个房间，但**不会丢**）。
要修得把判定挪进 `BeforeCombatRewardOffered`（要确认那时本场统计已经齐了）。
**这条还没做，别以为已经完美。**

### 验证
- `dotnet build` 0 错误 0 警告，dll 已部署进游戏，**必须重启游戏**才生效。
- 验证方法：接「枯枝还魂」或「只狼·弹反」→ 战斗中做完 → 打赢 →
  **战后的「搜刮」界面里应该能看到那件遗物 / 升级奖励**（以前是没有的）。
- 日志里可以看到 `任务奖励：N 项已并入本场战斗的官方奖励。`

### 教训（写下来别再犯）
> **给游戏"加东西"的钩子，必须确认触发时机在游戏"读它"之前。**
> 这次是 `AfterCombatVictory` vs `BeforeCombatRewardOffered`；
> 只要拿不准，就去反编译找触发点和读取点的先后（`probe3` 就是干这个的）。
> 光看"方法名像不像对的时机"会被坑 —— `AfterCombatVictory` 名字看着完全合理。

---

## 33. 【0.2.12 没修好】奖励并入的**清单**又选错了（2026-09-26 深夜，0.2.13 修）

### 玩家实测反馈（23:0x 那局，日志实锤）
```
30013-30015  Quest completed: 纯净卡组→变牌 / 只狼→升级 / 饥荒→+200金
41407        任务奖励：3 项已并入本场战斗的官方奖励。      ← 0.2.12 的动作执行了
41427        Player 1 obtained 16 gold from reward        ← 但只有游戏的奖励被兑现
41429        Auto-looted 2 reward(s)
      ……全程没有我们那 3 项
```
**结论：0.2.12 只修对了一半——时机对了，但加错了地方。**

### 真正完整的顺序（这次把 `OfferRoomEndRewards` 整段读完了）
```
OfferRoomEndRewards()
  IL_005C  RewardsCmd.GenerateForRoomEnd()      ← 建奖励**清单**（这里读 CombatRoom.ExtraRewards）
  IL_013C  Hook.BeforeCombatRewardOffered()     ← 我们的钩子（0.2.12 用的位置）
  IL_019F  RewardsSet.Offer()                   ← 才 get_Rewards → get_Count → ShowScreen 建界面
```
- 0.2.12 往 `CombatRoom.ExtraRewards` 里加 —— **那一步在 IL_005C 就读完了**，等于丢进废纸篓。
- 但 `Offer()` 是**在钩子之后**才从 `Rewards` 清单建界面的 →
  **直接往 `rewards.Rewards` 里加，就一定会显示。**

### 0.2.13 的做法
`BeforeCombatRewardOffered` 里直接 `rewards.Rewards.Add(r)`（不再碰 `AddExtraReward`），
并加诊断日志：`任务奖励：N 项已并入…[清单：类型名…][界面清单 A → B 项]`。
删掉了废弃的 `FlushIntoRoom`。

### ⚠️⚠️ 排查时踩的一个大坑（下一个我必须记住）
**用户的游戏加载的是「创意工坊那一份」，不是本机 `mods\QuestSpire`！**
日志里写得明明白白：
```
263: Skipping loading mod QuestSpire, it is set to disabled in settings   ← 本机那份被禁用
942: Loading assembly DLL ...\workshop\content\2868840\3806270140\QuestSpire.dll  ← 加载工坊那份
```
所以**我本机 `dotnet build` 改的东西，用户根本跑不到** —— 他一直测的是工坊版本。
要让用户验证一个修复，**必须先上传到工坊**（或者让他在游戏里切换成用本机那份）。
这个坑害我白写了一轮"验证清单"。

### 还没解决的
1. **靠"胜利那一刻"判定的任务**（只狼·弹反、文明6、泰拉瑞亚等）判定在 `AfterCombatVictory`，
   而 `BeforeCombatRewardOffered` 在它**之前**跑 → 这批奖励赶不上本场界面，
   走下一个房间的 `FlushPendingRewards` 弹出（**晚一个房间，不丢**）。
   要根治：把"胜利判定"提前到 `BeforeCombatRewardOffered` 里做（需确认当时的本场统计已经齐了）。
2. 0.2.13 是否真好使，**还没实测**（等玩家/用户测试日志，看那行 `界面清单 A → B 项`）。

---

## 34. 【最终结论】别再往游戏的「搜刮」界面里塞东西了（2026-09-26 深夜，0.2.14）

### 0.2.13 的实测结果（用户截图 + 日志）
截图里「搜刮」框**只有原版那 2 个按钮**（"将一张牌添加到你的牌组"、"升级一张牌"），
我们那 3 项一个都没有。但日志显示我们**确实加进清单了**：

```
94558  任务奖励：3 项已并入本场战斗的官方奖励。
       [清单：CardUpgradeReward、KeywordEnchantReward、GoldReward]
       [界面清单 2 → 5 项]        ← 清单真的从 2 项变成 5 项
```

**清单里有了，界面上没有** —— 界面读的是更早的一份快照，我们加得再"早"也早不过它。

### ⛔ 三次尝试，同一个规律（别再试第四次）
| # | 做法 | 结果 |
|---|---|---|
| ① | `AfterCombatVictory` → `room.AddExtraReward` | 时机最晚，直接丢 |
| ② | `BeforeCombatRewardOffered` → `room.AddExtraReward` | `ExtraRewards` 更早就读完了 |
| ③ | `BeforeCombatRewardOffered` → `rewards.Rewards.Add` | 清单 2→5，界面还是 2 个按钮 |

**结论：游戏总是在我们动手之前就把奖励清单读走了。这条路走不通。**
（反编译顺序看着像有机会：`GenerateForRoomEnd` → `Hook.BeforeCombatRewardOffered` → `Offer`；
但实测三次都证明"看得见的界面"用的是更早的快照。）

### 0.2.14 的做法：走我们自己的奖励流程
`BeforeCombatRewardOffered` 只做一件事 —— 把攒着的那批交给
**`RewardsCmd.OfferCustom`**（商店/事件任务一直在用的那条路，稳）。
- 代价：任务奖励**单独弹一个界面**，不再并进「搜刮」那张单子。
- 好处：**绝不会再丢**；而且这条路是已经验证过的。

### 0.2.14 同时修的：快速 SL 吞奖励
`QuestData`（进度）有 `[SavedProperty]` 会进存档，但 `_batch` / `_pendingRewards` / `_pendingRelics`
都是**纯内存**的 → 战斗中完成任务后排了队，这时快速 SL，队列没了而任务记着"已完成" → **奖励永久消失**。
修法：
- 新增 `[SavedProperty] public int[] PendingRewardKinds`：发奖励**之前先记账**（进存档），真发到手上才销账。
- `RescheduleRestoredRewards()`：进房间/读档时，只要账上还欠着就重新排进待发队列。
- 遗物加了"已经有了就不发第二次"的防重复。

### ⚠️ 残留风险（如实记下）
如果读档补发时，玩家其实已经拿到过（销账没来得及存档），会**多给一次**。
判断：**多给一次远比丢掉好**，所以选了这边。

### 验证清单（0.2.14）
1. 接「枯枝还魂」或「只狼·弹反」→ 战斗中完成 → 打赢 → 应该**单独弹一个奖励框**（不在「搜刮」里）。
2. 完成任务后故意**快速 SL 一次** → 进下一个房间应该把奖励补给你。
3. 日志看这几行：`战斗结束：把 N 项任务奖励交给奖励流程。` / `任务奖励：一次发了 N 项（同一个界面）。`
   / `读档：发现 N 项任务奖励还没发出去…`。

---

## 35. ★ 玩家要的「检测」：进房间自动对账（2026-09-27，0.2.16，**当前方案**）

### 玩家怎么说的
> 「要不你加个检测。如果路上，没发就下个房间发。就是看任务完成了但是玩家没有奖励」

**这个思路是对的，而且比前面所有"找正确时机"的尝试都稳。** 前面几轮我一直在赌
"某个钩子一定会被执行到"，一旦它被跳过（切房间顶掉、读档、和结算界面打架）奖励就没了。
改成**对账**之后不再赌任何触发点：只认「任务完成了」和「奖励发过没」这两个事实。

### 实现（`QuestSpireCode` 里）
- `QuestRunState.CompletedKinds`：这一册里判定完成的任务种类。
- `QuestBookRelic.PaidRewardKinds`（`[SavedProperty]`，**进存档**）：已经发过奖励的任务种类。
- `ReconcileOwedRewards()`：进房间时对账 ——
  `已完成` − `已发过` − `已记在待发` = **欠着的**，全部记入 `_dueKinds` 并补发。
  日志：`对账：发现 N 个任务已完成但奖励没发过，已记入待发：…`
- 销账时机收紧：`GrantRewardNow` 现在**返回 bool**（true = 只是排队了）。
  **只有当场交到玩家手上才销账**；排队那批等 `FlushBatchAsync` 真发出去才把
  `_dueKinds` → `_paidKinds` 划过去。以前是不管三七二十一就销账，等于把账记丢了。
- `FlushPendingRewards` 结尾改成**直接** `FlushBatchAsync()`（原来是 `ScheduleFlush()`，
  而它在"战斗流程还没结束"时会 return，补发的奖励被无限期押着 —— 这是 0.2.15 那个
  "必须进下一个房间"的直接原因）。
- 新增出口 `BeforeRoomEntered`：**切房间的路上**就发，不用等新房间加载完。

### 0.2.16 另外两处
- **奖励框图标修好**：`IconPath` 原来直接给 `"ui/reward_screen/reward_icon_card_upgrade.png"`，
  日志报 `No loader found for resource`（玩家看到的空框就是这个）。
  必须**过一层** `ImageHelperExtensions.GetModImagePath(...)`，BaseLib 的 `CardUpgradeReward` 就是这么写的。
- 上传踩坑：工作区传多次后会 `k_EResultFail`（连字节都传不出去），
  **复制到一个全新的工作区目录再传一次就好**。`k_EResultInvalidParam` 才是"说明超 8000 字节"。

### 验证
- 打赢后**切房间的路上**就该弹奖励框；就算那一步被跳过，**进房间时对账**也会补上。
- 日志关键行：`对账：发现 N 个任务已完成但奖励没发过…` / `任务奖励：一次发了 N 项（同一个界面）。`

### ⚠️ 0.2.16 的教训：对账 + 待发队列 = 发两次（0.2.17 修）
玩家截图实锤：搜刮界面里出现了**两个一模一样的"添加一张牌到牌组"**、**两个"附魔「保留」"**。

原因：**同一笔奖励走了两条路**。
```
① 奖励进了 `_batch`（待发队列），`_dueKinds` 同时记着"欠着"
② 进房间 → 对账看到 `_dueKinds` 里有 → 按账**重建成一份新奖励**并发了
③ 紧接着 `FlushPendingRewards` 又把 `_batch` 里**原来那份**发了一遍  → 重复
```

修法（0.2.17）：`RescheduleRestoredRewards()` 里**按账重建之前先把 `_batch` 清空**
（日志会写 `补发：丢掉待发队列里重复的 N 项`）。**一份奖励只能走一条路。**

> 教训：加了"兜底重建"之后，必须保证**兜底路径和原路径不会同时命中**。
> 否则兜底就成了重复发放的来源。

### ⚠️ 0.2.17 还是重叠 → 0.2.18 改成"入口幂等闸"
玩家又发截图：搜刮界面里**两个「升级一张牌」+ 两个「变化并升级一张牌」**。
说明 0.2.17 只堵住了「对账重建 + 待发队列」这一条路径，**没堵住根子**（同一条任务的奖励被发了两次）。

0.2.18 的做法：**不追触发点了**，直接在发奖入口 `Reward()` 加幂等闸 ——
`_rewardedKinds`（`[SavedProperty]` 进存档）记住"已经开始发过奖励的任务种类"，
同一条任务的第二次发放直接跳过并写日志 `任务「X」的奖励已经发过了，跳过（防重复）`。
`ClearPendingRewards()` 里也要清这个闸（否则新局同一条任务会被当成"已发过"）。

---

## 36. ★★ 终于去看了《海克斯符文》（2026-09-27，玩家点名"你究竟看了吗"）

**结论：我之前 §32/§33/§34 的判断是错的，必须更正。**

### 它怎么发奖励（反编译 `workshop\...\3747501308\lib\0.111.0\HextechRunes.dll`）
- 它的奖励类 `HextechForgeChoiceReward` **直接继承 `MegaCrit.Sts2.Core.Rewards.Reward`**（不是 BaseLib 的 `CustomReward`）。
- 发放路径：`AfterCombatVictory` → `ApplySharedCombatVictoryRunes(room)` → `HextechForgeGrantHelper.AddRandomForgeReward(...)`
  → **`CombatRoom.AddExtraReward(player, reward)`**。
- 它就这么**正常显示**在「搜刮」界面里。

### 所以 §32/§34 的结论要更正
- ❌ 我写的「`AfterCombatVictory` 时机太晚，所以加进去看不到」——**不对**。海克斯用的就是这个时机，显示正常。
- ❌ 我写的「别往「搜刮」界面里塞奖励」——**片面**。能塞，只是我们的奖励类/数据有问题。
- ✅ **真正的差别在奖励对象本身**：它是 `Reward` 的直系子类；我们用的是 BaseLib 的 `CustomReward`。
  另外它的 `Populate()` 里会调 `Reward.MarkContentAsSeen()`，**我们的 `Populate()` 是空实现** —— 这个差异还没验证是不是关键。

### 下一步该做的（下一轮优先）
1. 把 `KeywordEnchantReward` 从 `CustomReward` 改成**直接继承 `Reward`**（照海克斯的样子），
   然后**试回 `AfterCombatVictory` + `AddExtraReward`** —— 那才是和它完全一致的路径。
2. 对照它的 `Populate()` / `MarkContentAsSeen()` / `ToSerializable()` 逐个补齐。
3. **不要再自己发明路径** —— 有现成能用的参照物（海克斯），照抄就行。

> **教训（比技术细节更重要）：玩家早就说过"看看海克斯"，我一直没去看。**
> 这个项目里凡是"别的 mod 做过同样的事"，**先去看它怎么做的**，比反编译游戏本体猜时机快得多。

---

## 37. 「为什么不是同一个框，而是多个框 / 会被挤掉吗」（2026-09-27，玩家提问，0.2.19 修）

### 玩家的问题
> 「我比较好奇为什么不是同一个框而是多个框。会出现那种被挤掉的情况吗」

**会，而且"多个框"和"被挤掉"是同一个 bug 的两面。**

### 机制
`FlushBatchAsync()` 可以被**三个地方**各自触发：
`BeforeRoomEntered`（切房间路上）、`AfterRoomEntered`（进房间）、战斗外完成时的 `ScheduleFlush()`。

**每一次调用都会弹一个新框**（`RewardsCmd.OfferCustom`）。两次调用挨得近时：
后一个框会把前一个**顶掉**（覆盖层被替换）→ 前一个框里的奖励玩家就看不到了。
日志里会看到**两条** `任务奖励：一次发了 N 项`。

所以：
- 「多个框」= 多次 `OfferCustom`；
- 「被挤掉」= 后一次把前一次顶掉；
- 「同一批奖励发两次」= 幂等闸没拦住（0.2.18 修的）。

### 0.2.19 的修法：发放串行化
`FlushBatchAsync` 加 `_offering` 闸：
- 已经在发放中 → **不并行弹框**，把请求记成 `_needsAnotherFlush` 并返回；
- 当前这次发完后，循环再发一次（把等待期间攒下的**合并进同一次**）。
- 日志：`发放中：N 项先攒着，等这一轮发完一起给你（避免弹多个框互相顶掉）。`

**效果**：同一时间只可能有一个奖励框；同一批的多个任务奖励合并成一次发。

### ⚠️⚠️ 上传流程的坑（这轮踩了，很隐蔽）
`prepare_workshop.py` 是从**游戏的 mods 目录**拷 dll 的。而：
- 游戏**开着**时，`dotnet build` 的**拷贝步骤会静默失败**（只是 warning，编译仍报"成功"）；
- 增量构建还会把这个拷贝目标**整个跳过**。
→ 结果：mods 目录里还是旧 dll，`prepare_workshop.py` 照旧拷、照旧上传，
**传上去是新说明 + 旧代码**（玩家看到的"没更新"就是这个）。

**以后上传前必须验证包里的东西**（别再只看"生成成功"）：
```powershell
# 1) 强制全量构建，确保拷贝目标真的跑
dotnet build QuestSpire.csproj -c Debug --no-incremental
# 2) 确认 mods 目录里的 dll 时间 = 刚构建的时间
# 3) 打包后查包内 dll 里的新日志字符串（比查字段名可靠，字段名不一定进元数据）
python -c "raw=open(r'...\QuestSpire.dll','rb').read(); print('发放中' in raw.decode('utf-16-le'))"
```

---

## 38. 幂等闸放错了层 + 两个新踩的坑（2026-09-27 01:1x，0.2.20）

### ① 幂等闸放在 `Reward()` 里，被补发路径绕过了
玩家 0.2.19 的日志实锤：
```
34156   进房间前：把 2 项任务奖励发掉
34449   补发：3 项欠着的任务奖励进了待发队列     ← ×2
34505-   Quest completed: 三条任务…（又完成了一遍） ← ×2
```
补发路径是 `RescheduleRestoredRewards` → `_pendingRewards` → **直接调 `GrantRewardNow`**，
**不经过 `Reward()`**，所以放在 `Reward()` 里的闸拦不住。
→ 修法：闸挪到 **`GrantRewardNow` 开头**（真正产生奖励副作用的唯一入口）。
> 教训：**闸要放在"副作用发生的那个函数"里**，不能放在"某个调用方"里。

### ② ⚠️ PowerShell 写 json 会加 UTF-8 BOM，把 mod 清单弄坏
我用 `Set-Content -Encoding UTF8` 改 `QuestSpire.json` 的版本号 → 写进了 BOM（`EF BB BF`），
三份（源文件 / mods 目录 / 上传包）全被污染。Python 的 `json.load` 会直接报
`Unexpected UTF-8 BOM`。
→ **改 json 一律用 `edit` 工具或 Python `open(..., encoding='utf-8', newline='')` 写，别用 PowerShell 重定向。**
→ 已经用 `probe\fixbom.py` 把源文件和本地化 json 都清了一遍（校验时用 `utf-8-sig` 读）。

### ③ 玩家问：为什么奖励不和官方的一起放「搜刮」里？
日志显示：战斗结束时我们先**攒着**（`战斗结束：N 项先攒着，进下一个房间再发`），
进房间时才用 `OfferCustom` 弹**我们自己的框** —— 所以和「搜刮」是两个框、两批东西。

**这是 §36 那个错误结论的直接后果**（我误判"塞不进搜刮界面"于是改走自己的流程）。
海克斯已经证明能塞。**下一轮第一优先级：把奖励类改成 `Reward` 直系子类，
试回 `AfterCombatVictory` + `CombatRoom.AddExtraReward`。**

---

## 39. 2026-09-28：玩家 Carson 报的两件事（0.2.21）

### ① 「任务奖励被搜刮界面盖掉」
原话：「泰拉瑞亚：以盾取胜 打完后弹出选择一张卡牌保留，但是随后马上被打完后的搜刮物
（药水金币卡牌）覆盖了，且拿完搜刮物后被覆盖的任务奖励没有了」

**根因**：战斗刚结束时 `CombatManager.IsInProgress` 已经变 false，`ScheduleFlush()` 立刻弹了我们的框；
官方搜刮界面是**紧接着**才搭起来的 → 把我们的盖掉了（这正是 §37 那个"被挤掉"，只是方向反了）。

**修法**：加 `_lootPending` 硬闸 ——
- `BeforeCombatRewardOffered` 置位（搜刮界面开始搭）；
- 这期间 `FlushBatchAsync` 直接 return，`_batch` 原样留着；
- `BeforeRoomEntered`（玩家点离开房间）清除标记并发放；`AfterRoomEntered` 也清一遍兜底。

### ② 「任务栏显示奖励」+ 开关
原话：「可以在任务栏那里显示任务完成后的奖励吗，有时候容易忘记。
如果担心占屏幕的话，或许可以考虑加个按钮设置是否显示」

**做法**（`QuestPanel`）：
- 每条任务行里加一行金色的 `奖励：…`（取 `QuestDef.RewardText`）；
- 标题栏加 `奖励 ◉ / ○` 按钮，点一下全局显示/隐藏（默认**开**，因为他本来就想看到）。

---

## 40. ★★★ 2026-09-28：结构性重做（0.2.22，**当前方案**）

### 为什么要重做（用户的原话很准）
> 「但是我们一直在修复这一类问题。好像从来没修成共过」

数一下：**0.2.12 → 0.2.21，十个版本全在修同一类问题**（奖励发不出 / 重复 / 被盖掉）。
根因不是时机没找对，而是**架构选错了**：

**我们自己造了一条发奖励的路**（`RewardsCmd.OfferCustom` 单独弹框），而不是用游戏那张奖励单。
所有症状都是这一个选择的后果：

| 症状 | 为什么 |
|---|---|
| 两个框 | 游戏一个、我们一个 |
| 被盖掉 | 两个框抢同一个位置 |
| 重复发 | 我们这条路有多个入口 |
| 拿不到 / 要等下一个房间 | 两个时机之间有空隙 |

**打补丁只是让症状换个地方冒出来。**

### 改成了什么
`AfterCombatVictory`（和《海克斯符文》同一个时机）：
```csharp
room.AddExtraReward(Owner, reward)   // 直接进官方「搜刮」单子
```
→ **只有一个框**，盖掉 / 重复 / 空隙这一整类问题**在构造上消失**。

**同时保留两层兜底**（不回归）：
1. `BeforeCombatRewardOffered` 里如果 `_batch` 还没清空，**再塞一次**并写 WARN；
2. 离开房间时 `FlushBatchAsync` 用老办法（`OfferCustom`）接住剩下的。
两者都有明确日志，**下一个版本就只留成功的那条路**。

### ✅ 验证步骤（**一分钟**，不用打一整局）
1. 开一局，进任意一场战斗（打起来就行，不用赢）；
2. 点任务面板底部的 **【测试】立即完成全部任务**；
3. 打赢这场战斗 → 看官方的「搜刮」界面。

**看日志判断走通了哪条路**（`%APPDATA%\SlayTheSpire2\logs\godot.log`）：
| 日志 | 含义 |
|---|---|
| `[AfterCombatVictory] 任务奖励：N 项已塞进本场「搜刮」单子` | ✅ 成了，奖励就在搜刮界面里 |
| `任务奖励：AfterCombatVictory 没塞进去…这里再试一次` | 前一个时机太晚，看下一个时机 |
| `[BeforeCombatRewardOffered] …已塞进本场「搜刮」单子` | 用这个时机就行 |
| `离开房间：把 N 项任务奖励发掉` | 两个时机都没成，走兜底（旧的单独弹框） |

> **这一轮之后，验证再也不用"打一整局"了** —— 这是能跳出"盲发-玩家报错-再修"循环的关键。

### 教训（写给下一轮）
1. **有能用的参照 mod 就先去抄**，别自己发明路径。海克斯就在本机，用户提醒过两次我才去看。
2. **先找"能一分钟验证"的办法**再动手，否则每轮都是一天，而且永远在盲发。
3. 症状反复换个样子出现时，**别继续补，先怀疑架构**。

---

## 41. 2026-10-01：补上 §32 的残留 —— 胜利那一刻判定的奖励，现在能进**本场**搜刮界面了

### 发现（读代码 + 反编译核对）
`AfterCombatVictory`（`QuestBookRelic.cs`）里，**塞进搜刮单子**那一步原来在**方法开头**：

```csharp
if (IsLocalOwner) PushBatchIntoRoom(room, "AfterCombatVictory");   // ← 原来在这（批次还没装齐）
_ = Grant(QuestKind.CombatsWon, 1);
if (!_dmgThisCombat && _blockBrokenThisCombat == 0) _ = Set(QuestKind.ParryFlawless, 1);  // ← 判定在它下面
…（还有 文明6 / 泰拉瑞亚 / 饥荒 / 暗黑 / 原神 / 明日方舟 那几条）
```

所以「胜利那一刻才判定」的任务，**奖励是在塞完之后才进的 `_batch`** → 赶不上本场搜刮界面 →
只能走下一个房间的 `FlushPendingRewards`（就是 §32 记的那个"晚一个房间、不丢"的残留）。

### 反编译确认的时机（这次没猜）
`sts2.dll`：
- `CombatManager.EndCombatInternal()` → `await Hook.AfterCombatVictory(...)`（约 999 行）——胜利钩子。
- `CombatRoom.StartPreFinishedCombat()` → `await OfferRoomEndRewards()`（约 232 行）——
  这里才 `RewardsCmd.GenerateForRoomEnd()` 读 `ExtraRewards`、建奖励单。
- 结论：**整段 `AfterCombatVictory` 都在 `GenerateForRoomEnd` 之前**，所以塞在方法哪一行都赶得上，
  塞在**最后一行**就能把"刚判定出来"的那批一起带上。

### 改法（一处，`QuestBookRelic.cs`）
`PushBatchIntoRoom(room, "AfterCombatVictory")` 从方法**开头挪到末尾**（在 `CheckDeckQuests/CheckBoatRelics/
CheckFakeRelics` 之后、`return` 之前）。其余一律不动。

- **为什么是"只挪一行"而不是"把 Grant 都改成 await"**：本方法里这些 Grant 走的是同步完成的路径
  （`Set/Grant` → `AddProgress` → `Reward` → `GrantRewardNow` 的 `_batch.Add`，中间没有真 await），
  挪到末尾时 `_batch` 已经装齐；不动 `_ = Grant(...)` 的 fire-and-forget 写法，**风险最小**。
- **为什么不会回归**：这批奖励"本来就在 `_batch` 里"，只是**发得更早**（本场而不是下个房间）。
  万一某个 Grant 真的异步晚到，它仍在 `_batch` 里 → 走原来的下个房间补发，和今天行为一样，
  **不会丢、不会重**（对账 + 幂等闸都在）。

### ✅ 验证（沿用 §40 的一分钟法）
1. 开一局 → 进任意战斗 → 任务面板点【测试】立即完成全部任务 → **打赢**；
2. 看官方「搜刮」界面里是否多了任务奖励（升级 / 附魔 / 金币那几张），和金币卡牌**同一张单子**；
3. 日志关键字应出现 `[AfterCombatVictory] 任务奖励：N 项已塞进本场「搜刮」单子`，且 N 比 0.2.22 大
   （把只狼 / 文明6 / 泰拉瑞亚那几条也算进去了）。

> 仍未验证（如实记下）：`AddExtraReward` 里放 **BaseLib 的 `CustomReward` 子类**
> （`KeywordEnchantReward` / `CardUpgradeReward`）在搜刮界面里到底显不显示 —— §36 怀疑它和
> 海克斯那种"`Reward` 直系子类"有差别。**下一轮若实测这几张不显示**，就照 §36 的办法把
> `KeywordEnchantReward` 改成直系 `Reward`（复用内置 `RewardType`，照抄 `HextechForgeChoiceReward`）。

---

## 42. 2026-10-01 第二轮：奖励类型独立 + 任务英文本地化（0.2.23 候选，未上传）

### ① KeywordEnchantReward 借了别人的奖励类型 → 读档会把「附魔」还原成「升级」

**改前**：`KeywordEnchantReward` 的 `RewardType => CardUpgradeReward.CardUpgrade`，并且
`DeserializeMethod => null!`。看着能跑，其实有个隐蔽 bug：

`CombatRoom.ToSerializable()` 会把本场**还没领**的 `ExtraRewards` 一起写进存档
（反编译确认：`serializableRoom.ExtraRewards[netId] = source.Select(r => r.ToSerializable())`）。
而 BaseLib 的注册机制只认**自己声明了 `static RewardType` 字段**的 CustomReward
（`CustomRewardPatches` 按字段扫描 → 建实例 → 读 `ToSerializable().RewardType`）。
我们原来没有这个字段 → 我们的 enchant 奖励压根没注册成自己的类型 → 存档里写的是 `CardUpgrade`。
于是玩家**打赢未领就退出/读档**，`Reward.FromSerializable` 按 CardUpgrade 把它还原成
BaseLib 的「升级一张牌」——**附魔奖励被换成别的奖励**。

**改后**（照 BaseLib 自己的 `CardUpgradeReward` 写法）：
- 自带类型：`[CustomEnum(null)] public static RewardType QuestEnchant;` + `RewardType => QuestEnchant`；
- 自带存档：`ToSerializable()` 把词条存进 `GoldAmount`，`DeserializeMethod => CreateFromSave` 还原
  （词条中文名由 `DisplayNameOf` 重建）。

> 结论修正：§36 猜「BaseLib 的 `CustomReward` 不如 `Reward` 直系子类」是**错的** ——
> BaseLib 的 `CardUpgradeReward` 本身就是 `CustomReward` 且一直好用。真问题是**借类型 + 没注册**。

### ② 任务文案英文本地化（123 条）

- 新增 `QuestSpireCode/Quests/QuestText.cs`：取词口 —— 先查 pck 里 relics 表的
  `QUEST_SPIRE_QUEST_<Kind>.title/.description/.reward`，查不到回退到 `QuestCatalog` 里的**硬编码中文**。
  利用 `LocString` **按当前语言解析**（`LocString.Exists` 查的就是当前语言那张表）：
  中文环境没有英文 key → 自动用中文兜底，**不需要我们判断语言**。
- `QuestSpire/localization/eng/relics.json` 补齐：123 条任务 ×（title + description）+
  38 条带奖励的 quest 的 `.reward` + 通用键（`REWARD_PREFIX` / `REWARD_GOLD` / `STATUS_COMPLETED`）。
  校验脚本确认：0 缺失、0 多余。
- 面板/选任务浮层的 4 处取词改走 `QuestText`（`QuestPanel` 3 处、`QuestSelection` 3 处）。
- **中文不用搬进表**：zhs 表没有这些 key，自动走 C# 里的中文兜底。
- **必须重打 pck**（`tools/build_pck.ps1`）英文才生效（pck 里的 `localization/eng/relics.json`）。

### ③ 顺手修：奖励文案的「奖励：奖励：」

`QuestCatalog` 的 `RewardLabel` 自带「奖励：」前缀（如「奖励：遗物「鸟居」」），而面板还会再加一次前缀
→ 显示成「奖励：奖励：遗物「鸟居」」。改成：`QuestText.Reward()` 只返回**内容**（剥掉自带前缀），
前缀由 `RewardPrefix`（中文「奖励：」/ 英文「Reward: 」）统一加。

### 状态 & 验证
- `dotnet build` 0 错误 0 警告；dll + 重打的 pck 已部署进 `mods\QuestSpire`（时间戳已核对）。
- **未上传、未真人实测**。验证：
  1. 把游戏语言切成 English → 任务面板/选任务浮层的标题、描述、奖励应全部是英文；
     切回中文 → 全部中文（且不再有「奖励：奖励：」）。
  2. 完成「魔兽世界：附魔」拿到附魔奖励后**先别领**，退出到主菜单再「继续游戏」→
     奖励应还是「附魔」，不再是「升级一张牌」。
- 多语言加载路径：`res://QuestSpire/localization/<语言>/relics.json`（只认打了 pck 的）。

---

## 43. 2026-10-01 第三轮：设置页（BaseLib 配置框架）+ 多人「随身物」可关（0.2.23 候选，未上传）

### 玩家提的三件事
1. 不知道在哪切语言；
2. 「有的奖励是战斗中发的」→ 多人容易对接错误 / 数据不同步；
3. 应该像别的 mod 那样有**多种可切换的设置**（玩家自己决定）；并且想搞清楚多语言怎么应对不同玩家。

### ① 怎么加设置：用 **BaseLib 的配置框架**（不用额外 mod）
BaseLib 自带 `BaseLib.Config.ModConfig` / `SimpleModConfig`：**public static 属性 = 一个设置项**，
自动存/读 `user://mod_configs/<命名空间>.cfg`，并挂进游戏的「设置 → 模组配置 (BaseLib)」。
参照物：本机 `HowlFromBeyondBgm.dll` 的 `HowlConfig : SimpleModConfig`（照抄即可）。
- 新增 `QuestSpireCode/QuestSpireConfig.cs`；`Entry.Initialize` 里 `ModConfigRegistry.Register(ModId, new QuestSpireConfig())`。
- 设置项（3 个）：
  - `ShowQuestPanel` 显示任务面板
  - `ShowRewardLines` 任务栏显示奖励（面板上的「奖励 ◉」按钮与它同步，改完落盘）
  - `SafeMultiplayerBoons` 多人局禁用随身物效果（防不同步）
- 设置页里的开关文字：`SetupConfigUI` 里按当前语言（`LocManager.Instance.Language`）写中文/英文。
- ⚠️ `QuestSpire.json` 补上 **BaseLib 依赖**（id `BaseLib` / version `v3.4.7`）——代码一直在用 BaseLib
  （`CustomReward` / `CardUpgradeReward` / 现在还有 `BaseLib.Config`），清单里却只有 RitsuLib。

### ② 多人不同步的真凶：**随身物只在本人那台点亮**
随身物（帆船 / 手不释卷 / 厚积薄发…）是**改写战斗数值**的钩子（`Modify*` / `Should*`）。
它们挂在 `QuestBookRelic` 上、**每台客户端都会为每位玩家各跑一遍**；但发奖励走 `IsLocalOwner` 守卫
→ **只有本人那台**才会把 `HasX` 打开 → 各端算出来的数值分叉 = 「数据不同步」。
修法：加 `QuestSpireConfig.BoonsActive`（`!(SafeMultiplayerBoons && PlayerCount>1)`），
**17 处随身物钩子**统一先判它，不生效就返回中性值。单人局永远生效；多人局默认关（玩家可在设置里打开）。

> 金币 / 遗物 / 卡牌不受影响：它们走游戏自带的同步接口（`PlayerCmd.GainGold` + `SyncLocalObtained*`）。

### ③ 多语言怎么应对不同玩家（回答玩家的问题）
- **游戏自己已经按地区选过一次语言**：首次启动时 `LocManager` 用 `PlatformUtil.GetThreeLetterLanguageCode()`
  取系统语言，之后把选择存进 `SettingsSave.Language`；玩家可以在 **设置 → 游戏设置** 里改。
- **mod 只要跟着游戏语言走就行**：`LocString` 是**按当前语言**解析的
  （`LocString.Exists` 查的就是当前语言那张表），所以我们的 `QuestText` 天然按玩家自己的语言显示。
- **联机里每台客户端各显示各的语言**——这是对的，也是主流做法（客户端本地文本不同步、也不需要同步）。
- 所以**不需要自己识别地区**。若以后想「不受游戏语言影响、强制某语言」，再加一个下拉设置即可。

### 状态
`dotnet build` 0 错误 0 警告；dll + json 已部署（清单无 BOM、依赖 = RitsuLib + BaseLib）。
**未上传、未真人实测**。验证：设置里应出现「QuestSpire」；多人局把「禁用随身物」开着打一局看是否还报不同步。

---

## 44. 2026-10-01 第四轮：概率平均化 + 4 个跨游戏新任务（0.2.23 候选，未上传）

> ⚠️ 任务数现在是 **127**（`quest-spire-任务总表.md` 那 111 行已过期，别再照它数）。

### ① 概率平均化（玩家要求）
`QuestCatalog` 里 12 条跨界任务原来带 `{ Weight = 8 }`（测试期临时调高，方便抽到）。
玩家要求「所有任务概率平均」→ **12 处 `Weight = 8` 全部删掉**，现在所有任务 `Weight = 1`，
`PickOffers` 均匀抽取。（`QuestDef.Weight` 属性保留，默认 1。）
注意：**开局那次仍会优先抽 EarlyBird（前置型）**，那是刻意的设计（前置遗物必须早到手），不是概率失衡。

### ② 4 个新任务类型（跨游戏「收集 / 成长」维度）
先反编译核对 `Player` 能读到哪些**没用过的**数据，再挑维度（不重复已有的）：
`Player.Relics`(遗物列表) / `Player.Potions`(药水) / `Player.Deck.Cards`(整局卡组) / `Creature.MaxHp`。

| 新 QuestKind | 标题 | 条件 | 维度 |
|---|---|---|---|
| `RelicsHeld`  | 以撒的结合：道具大亨 | 同时持有 12 件遗物 | 遗物**数量**（之前只按具体遗物判过） |
| `PotionsHeld` | 星露谷物语：背包     | 把药水栏装满（2 瓶） | 药水**存量**（之前只算「用过几瓶」） |
| `DeckSizeBig` | 俄罗斯方块：堆高     | 卡组攒到 40 张       | 卡组**张数**（之前只判构成/稀有度） |
| `MaxHpGained` | 黑神话：悟空·修炼   | 生命上限比开局多 25  | **成长量**（之前只判「没改过上限」） |

- 全部**追加在枚举末尾**（存档存 `(int)Kind`，插中间会串位）。
- 统计走新增的 `QuestBookRelic.CheckCollectionQuests()`（读持有量、`Set` 记快照、只增不减），
  在 `AfterCombatVictory` 和 `AfterRoomEntered` 里各调一次。
- 英文本地化已补齐（eng/relics.json），校验脚本：127 条 0 缺失。
- ⚠️ **踩到一条**：`PotionsHeld` 一开始写「3 瓶」，玩家指出**进阶 10 只有 2 个药水栏** →
  改成 **2**（否则高进阶永远做不完，违反设计规则③「条件必须可行」）。以后设计「存量类」条件，
  **要按高进阶的上限估**（药水栏、手牌上限这类会被进阶削减）。

### 状态
`dotnet build` 0 错误 0 警告；dll + 重打的 pck 已部署（新 Kind 已在 dll 里核对到）。
**未上传、未真人实测**。

---

## 45. 2026-10-01 第五轮：「血之契约」契约任务（0.2.23 候选，未上传）

> 任务数现在 **128**。

### 玩家点子
「下几场战斗怪物获得随机 buff，然后给一件强力随机遗物」→ 用户选了变体：**敌人随机增益（挑战）**。
另一个点子「直接挑战本幕 boss」**没做**：要跳过地图节点，游戏没给安全接口，硬改会踩联机同步。

### 实现（`QuestBookRelic.cs`）
- 新 `QuestKind.ContractChallenge`（**追加在枚举末尾**）：接下后，**每场战斗敌人开场随机带一个增益**；
  打赢 3 场 → 一件强力随机遗物 + 150 金币。
- **效果写法照抄本体《弹珠袋》`BagOfMarbles`**：
  ```csharp
  public override async Task BeforeSideTurnStart(PlayerChoiceContext ctx, CombatSide side,
      IReadOnlyList<Creature> participants, ICombatState combatState)
  {
      if (participants.Contains(Owner.Creature) && Owner.PlayerCombatState.TurnNumber <= 1)
          await PowerCmd.Apply<T>(ctx, combatState.HittableEnemies, amount, Owner.Creature, null);
  }
  ```
  **关键**：`BeforeSideTurnStart` **带 `PlayerChoiceContext`** —— 这才是 `PowerCmd.Apply` 能用的时机
  （`BeforeCombatStart` 没有 context，就是 §4「齿轮工艺品」那个坑，别再试）。
- 增益池（都用 `Models.Powers` 里真实存在的类型）：力量 / 荆棘 / 敏捷 / 人工制品 / 缓冲 / 仪式，各 +1~2。
- **随机用 `ActFloor` 当种子**（不是 `Random.Shared`）：同一次战斗各端算出来一致。
- **多人不生效**（`BoonsActive` 闸）：给敌人挂能力会改战斗数值、各端必须一致 → 联机干脆不开（和随身物同一套政策）。
- 计数：`AfterCombatVictory` 里 `Grant(ContractChallenge, 1)`（只对已接的任务生效）。
- 奖励：`GrantRandomQuestRelic()`（我们自己的遗物池）+ 150 金币。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署（`ContractChallenge` / `HittableEnemies` 已在 dll 里核对到）。
英文本地化补齐（128 条 0 缺失）。**未上传、未真人实测**。

---

## 46. 2026-10-01 第六轮：盛碗虫任务 + 4 个新任务（0.2.23 候选，未上传）

> 任务数现在 **133**。

### ① 「盛碗虫泛滥」（玩家点名的）
**加怪的口子**（反编译确认）：`CreatureCmd.Add<T>(ICombatState combatState, string? slotName)`
—— `T` 是 `MonsterModel` 子类。放在 `BeforeSideTurnStart` 里（那里有 `combatState`）。
- 盛碗虫 4 个变体：`BowlbugEgg / BowlbugNectar / BowlbugSilk / BowlbugRock`（二幕巢穴的普通怪），随机召一个。
- 奖励**与这怪相关**：随身物「**盛碗虫常数**」——每当你获得格挡**额外 +1**。
  梗来源：社区「盛碗虫常数」——石虫头槌 15（9+ 难度 16），一堆牌恰好 15 格挡挡不住，
  玩家吐槽安东尼为这怪把牌削到「恰好 15」。+1 正好把 15 补成 16，一卡挡住 → 触发它的眩晕。
  实现落在 `ModifyBlockAdditive`（和「冷漠」共用一个钩子，**改成两个 boon 相加返回**）。
- 侧栏「随身物」里也加了一行（`QuestPanel._bowlbugLabel`）。

### ② 另外 4 个新任务（玩家要求「多点任务」）
| QuestKind | 标题 | 条件 | 新维度 |
|---|---|---|---|
| `BlockInOneTurn`   | 我说叠甲有没有懂的 | 单回合获得 40 格挡 | **单回合**格挡（之前只有单场总量） |
| `SkillsInOneTurn`  | 花拳绣腿           | 单回合打出 8 张技能牌 | **单回合**技能牌 |
| `GoldZeroWin`      | 两袖清风           | 身上 0 金币时赢一场战斗 | **金币为 0** |
| `NoDamageDealtWin` | 我说别打有没有懂的 | 一场战斗自己没造成任何伤害还打赢 | **零输出** |

- 新计数：`_blockThisTurn` / `_skillsThisTurn`（进回合清零）、`_dealtDamageThisCombat`（进战斗房间清零）。
- **`CreatureCmd.Add` 也走 `BoonsActive` 闸**：加怪会改战斗（多一只敌人），联机里各端必须一致 →
  单人局才生效（和随身物 / 血之契约同一套政策）。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署（新符号全部核对到）。英文本地化 133 条 0 缺失。
**未上传、未真人实测**。

---

## 47. 2026-10-01 第七轮：修正「零输出」任务 + 新增「堆叠宝石」（0.2.23 候选，未上传）

> 任务数现在 **134**。

### ① 玩家质疑「零输出还赢」站不住脚 —— 确实
玩家原话：「怎么做到零输出还能赢？是骨头人叠灾厄吗，还是这是多人？」
**两者都对**，所以这条设计是坏的：
- **灾厄（`DoomPower`）**：反编译确认它是 **`CreatureCmd.Kill` 直接杀**，**不走伤害结算** →
  骨头人叠灾厄清场 = 自己零伤害。
- **多人**：队友把怪全杀了，你也是零输出躺赢 → **白送**。
- 所以「零输出」只等于"你没亲自造成伤害"，还依赖伤害归属（灾厄/毒谁算谁的）。**删掉。**

**改法**：换成直接用灾厄机制本身的 `QuestKind.DoomKills`（`NoDamageDealtWin` 原地改名，**位置不变**，
老存档不串位）：「骨头人：灾厄」——**用灾厄击杀 3 个敌人**，`OnlyCharacter = Necrobinder`。
钩子是本体专门留的 `AbstractModel.AfterDiedToDoom(PlayerChoiceContext, IReadOnlyList<Creature>)`。
（顺带删掉只为那条服务的 `_dealtDamageThisCombat`。）

### ② 新增「堆叠宝石」（玩家点名）
玩家要：「单回合把同一张牌打出 N 次 → 获得一张**堆叠宝石**（最强卡），所以难度得高点」。
- 先在游戏本地化里查实：「宝石」相关的**卡**只有一张 —— `HIDDEN_GEM` = **「未掘宝石」**
  （稀有；效果 = 给一张牌叠「**重放**」层，让同一张牌多打几遍）。这跟"同一张牌打很多次"完全对口，
  判定玩家说的「堆叠宝石」就是它。
- `QuestKind.SameCardManyTimes`：**单回合把同一张牌打出 8 次**（难度刻意拉高），奖励 = 发一张「未掘宝石」。
- 计数复用已有的 `_cardPlayCountThisTurn`（本回合每张牌打了几次），`Set` 取本回合最大值；
  进回合 `_state.ResetProgress(SameCardManyTimes)` 清零（和 `AttacksInOneTurn` 一样）。
- 发卡照抄「地狱狂徒」那条：`ModelDb.Card<HiddenGem>()` + `RunState.CreateCard` + `CardPileCmd.Add(Deck)`
  + `SyncLocalObtainedCard`。
- ⚠️ 8 次是否偏高，等实测手感再调（在 `QuestCatalog` 那一行）。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署（`DoomKills`/`AfterDiedToDoom`/`SameCardManyTimes`/`HiddenGem`
全部核对到）。英文本地化 134 条 0 缺失。**未上传、未真人实测**。

---

## 48. 2026-10-01 ★ 0.2.23 已上传创意工坊 ✅

- **工坊 id 3806270140**，`Successfully uploaded`；`check_item.py` 复核：result=1、说明 3470 字（新版）、
  file_size=2859847 与上传包一致、time_updated 已变、可见性 = public。
- 本次打包内容：dll（含 DoomKills / BowlbugSwarm / ContractChallenge / SameCardManyTimes / QuestSpireConfig
  + 中文串核对到）+ pck（含新 eng 键）+ json（version 0.2.23，deps = RitsuLib + BaseLib）。
- 说明文按 8000 字节上限**压过老日志**（0.2.12 的 [list] 收成一行、0.2.13~0.2.15 合并），现 7582 字节。
- ⚠️ 上传流程里那条 `k_EItemUpdateStatusInvalid` 之后仍出现 `Successfully uploaded`，**以最后一行 + check_item 为准**。
- 待办：玩家 Carson 报的「只狼·弹反奖励被覆盖」正由本次修好，可在留言区回他一句（**尚未回，等用户确认**）。

---

## 49. 2026-10-01 第八轮：设置页加「语言 / 难度 / 无限刷新」（本地，未上传；0.2.23 已传过，这批算 0.2.24 候选）

玩家要求：① 本地是英文尖塔、想玩中文；② 设置页要给更多**玩法参数**。

### ① 语言（关键：让"强制中文"在英文游戏里也能用）
设置页新增 `Language` 枚举（自动变成下拉）：**跟随游戏 / 强制中文 / 强制英文**。
- `QuestText` 取词改成三级：强制中文 → `QuestCatalog` 里硬编码的中文；强制英文 → **`QuestTextEn`**；
  跟随游戏 → pck 里 `relics` 表（查不到再回退中文）。
- ⚠️ **英文表生成进了 C#**（`QuestSpireCode/Quests/QuestTextEn.cs`，由 `tools/gen_questtexten.py`
  从 `localization/eng/relics.json` 生成）。原因：`LocString` 只按**当前游戏语言**取表，
  游戏是中文时**取不到英文**。塞进 C# 后，任意"游戏语言 × 设置语言"的组合都能正确显示。
  改英文文案 → 改 json → 跑 `python tools/gen_questtexten.py`。

### ② 玩法参数
- `Difficulty` 枚举：**休闲 0.6× / 正常 1× / 硬核 1.5×**，乘在任务目标上（`QuestState.Target`）。
- `UnlimitedRerolls` 开关：开了就没有"每批候选只准刷一次"的限制（`QuestSelection.RerollBudget`）。
- 设置页现有 6 项：显示任务面板 / 任务栏显示奖励 / **任务文案语言** / **任务难度** / **无限刷新候选** / 多人禁用随身物。
- 均为 public static 属性，BaseLib 自动落盘 `user://mod_configs/QuestSpire.cfg`；枚举→下拉、bool→开关。

### 状态
`dotnet build` 0 错误 0 警告；dll 已部署（`QuestTextEn`/`TextLanguage`/`QuestDifficulty`/`UnlimitedRerolls`/
`TargetMultiplier`/`RerollBudget` 全部核对到）。**未上传**（要传得先升版本号到 0.2.24）。

---

## 50. 2026-10-01 第九轮：加怪 / 加精英任务（本地，未上传）

> 任务数现在 **136**。

玩家：奖励清一色金币没意思，多来点「下场战斗加几只怪 / 加个精英」那种；参考一代 Spire Quests。

### 参考（`notes/一代SpireQuests任务池.md`）
- **#55 Reality Twist**：接下来几场普通战换成危险怪组合。
- **#56 Superior Elite**：下一个精英更强。
- **#71 Zilliax**：随机模块怪混进战斗。
- **#63 Evil Sentries**：下一个精英变成三只哨兵。
- **#60 #Blessed**：每场战斗随机祝福一个敌人。

### 加了什么（奖励是**遗物**，不是金币）
| QuestKind | 标题 | 效果 | 条件 | 奖励 |
|---|---|---|---|---|
| `MonsterRush` | 怪潮 | 每场战斗额外多 **2 只普通怪** | 打赢 3 场 | 随机遗物 |
| `EliteAmbush` | 精英加餐 | 每场战斗额外多 **1 只精英** | 打赢 2 场 | 随机遗物 + 150 金币 |

- 召怪还是 `CreatureCmd.Add<T>(combatState)`（`BeforeSideTurnStart` 里、BoonsActive 闸内，和盛碗虫同一处）。
- 普通怪池：`LeafSlimeM / Flyconid / Inklet / GlobeHead / FuzzyWurmCrawler / CorpseSlug`。
- 精英池：`Byrdonis / Entomancer / InfestedPrism / BygoneEffigy`（都是单体精英，不选多段/整幕怪）。
- 随机用 `ActFloor` 种子（各端一致）。
- ⚠️ 加精英会显著变难，**已让玩家先实测手感**（数值/只数都在 `BeforeSideTurnStart` 里，好调）。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署（`MonsterRush`/`EliteAmbush`/`Byrdonis`/`FuzzyWurmCrawler` 核对到）。
英文本地化 136 条 0 缺失。**未上传**。

---

## 51. 2026-10-01 第十轮：奖励多样化 + 效果在多人局生效（本地，未上传）

### ① 奖励多样化（玩家：奖励清一色金币没意思）
新增 `QuestDef.Reward`（`RewardKind` 枚举）：`Gold / Relic / CardAny / CardSkill / CardPower / CardRare /
CardOtherClass / Upgrade / Transform`。默认 `Gold`（`RewardGold` 那条路不变）。
- `GrantRewardNow` 的 `default` 改为 `GrantByRewardKind(q)`（金币、遗物、卡牌三选一、升级、变牌）。
- 卡牌奖励复用 `CardGift.BuildCardReward(owner, filter, count=3)`（从 `BuildXCardReward` 泛化而来；
  仍是"不限职业 + 转成玩家名下的卡"，否则界面点不中）。它塞进 `_batch` → 和金币/遗物**同一个搜刮界面**。
- **24 条凑数金币任务**换成了不同奖励（脚本一次性改的，见下）：累计类 → 技能/能力/稀有牌或遗物；
  卡组/商店类 → 「其他职业的牌」（`IsOwnerClassCard` 用 `VisualCardPool.Id.Entry` 比）。
  **保留了相当一批金币**（小目标、金币主题的「守财之路」，以及所有梗/乐子/跨界任务）。
- 面板文案：非金币奖励走 `QuestText.KindLabel`（中/英）。

### ② 下调"做不到"的条件（玩家点名「叠 20 敏捷根本做不到」）
`力量 30 → 15`、`敏捷 20 → 8`、`易伤 30 → 15`、`中毒 50 → 25`（中英文案 + QuestTextEn 一起改了）。

### ③ ★ 效果在多人局生效（玩家：这游戏本质是多人游戏）
关键一步：**随身物改成"所有客户端统一点亮"**。
- 之前 `Reward()` 第一行就 `if (!IsLocalOwner) return;` → 随身物只在本人那台开 →
  各端战斗数值分叉（这就是当初联机不同步的根）。现在 `Reward()` 先调
  `ApplySharedBoon(kind)`（**不加 IsLocalOwner 守卫**），把"奖励就是这个随身物本身"的 13 个 unify 点亮；
  **发金币/遗物/卡牌那部分仍然只由本人那台做**（否则重复发）。
- ⚠️ **兜底型**随身物（遗物发不出去才启用的 HasDeadBranch/HasTorii/HasDripper/HasOrnithopter/HasGoldenIdol）
  **不能**放进去 —— 那取决于本地发放结果，各端不一定一样。
- 加怪（盛碗虫/怪潮/精英）、敌人随机增益（血之契约）本来就**用 `ActFloor` 当种子**（各端一致），
  所以多人局同样生效。
- `SafeMultiplayerBoons` 默认改成 **false**（= 多人也生效），降级成"万一又不同步"的应急开关。
- ⚠️ **没法在本机验证联机**，要玩家实测：开一局多人，确认不再被踢回主菜单 / 不再报数据不同步。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署（`RewardKind`/`GrantByRewardKind`/`ApplySharedBoon`/
`BuildCardReward`/`CardOtherClass` 核对到）。**未上传**（要传升 0.2.24）。

---

## 52. 2026-10-01 第十一轮：遗物奖励走原版 + 奖励指定核心牌（本地，未上传）

> 任务数 136。

### ① 遗物奖励改成走游戏原版（玩家指出「别老写池子空了」）
- 旧写法 `GrantRandomQuestRelic()`：从**我们自己那 7 件**遗物里抽，抽完就"池子空了，折现 150 金币"。
  玩家指出：原版遗物**不会发重**，真发完了给**「头环」(Circlet)**，而且除非打无尽 mod 很难清空。
- 新写法 `AddRandomRelicReward(q)`：直接 `_batch.Add(new RelicReward(Owner))` ——
  走游戏的 `RelicFactory.PullNextRelicFromFront`，**自带防重复 + 耗尽给头环**（`FallbackRelicPool`）。
- 6 处调用全部换掉（我的世界挖矿 / 暗黑刷装备 / 怪潮 / 精英加餐 / 血之契约 / `RewardKind.Relic`）。
  顺带删了没人用的 `GrantRandomQuestRelic` 和 `GrantRelicDirect`。

### ② 奖励指定核心牌（玩家：「把任务和各种特色的牌组合起来，奖励特定的牌」）
- 新增 `RewardKind.CardNamed` + `QuestDef.RewardCard`（存牌的**模型 Id**，如 `"DEMON_FORM"`）。
- `GrantNamedCard`：在 `ModelDb.All` 里按 Id 找牌 → `CreateCard` + `CardPileCmd.Add(Deck)` +
  `SyncLocalObtainedCard`（和「地狱狂徒」同一条路）。找不到就折现。
- 换了 8 条（都带 `RewardLabel` 写清中文名 + eng json 的 `.reward`）：
  | 任务 | 奖励核心牌 |
  |---|---|
  | 见血封喉 (伤害) | 恶魔形态 `DEMON_FORM` |
  | 铁壁防御 (格挡) | 壁垒 `BARRICADE` |
  | 行云流水 (出牌) | 机器学习 `MACHINE_LEARNING` |
  | 破釜沉舟 (消耗) | 腐化 `CORRUPTION` |
  | 硬抗到底 (自伤) | 主宰 `JUGGERNAUT` |
  | 奇珍异宝 (稀有牌) | 未掘宝石 `HIDDEN_GEM` |
  | 十八般武艺 (能力牌) | 创造AI `CREATIVE_AI` |
  | 我说叠毒有没有懂的 (毒) | 剧毒烟雾 `NOXIOUS_FUMES` |
- ⚠️ 这些卡 Id 是**从游戏本地化里核对存在的**（不是我编的）；手感/强度等实测再调。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署（`GrantNamedCard`/`RewardCard`/`AddRandomRelicReward`/`CardNamed` 核对到）。
校验：136 条 0 缺失。**未上传**。

---

## 53. 2026-10-01 第十二轮：两个改牌组任务 + 附魔奖励（本地，未上传）

> 任务数 138。

### ① 「诅咒交易」`CursedBargain`
接取时**随机塞 5 张诅咒**（排除 `CardKeyword.Eternal` —— 那类移除不掉）；打赢 3 场 →
**500 金币 + 清除牌组所有诅咒**。
- 塞哪 5 张用**确定性种子**（NetId + ActFloor）→ 各端一样。
- 「清除所有诅咒」放在 `ApplySharedEffect`（各端都做）→ 联机不会只清一个人的。

### ② 「大改造」`DeckOverhaul`
接取时把牌组里**所有「打击」→ 升级版「飞溅」**（`SPLASH`）、**「防御」→ 升级版「发现」**（`DISCOVERY`）；
每场战斗**多一只精英**；打赢 3 场 → 随机遗物。
- 识别打击/防御用本体的 `CardTag.Strike` / `CardTag.Defend`。
- 加牌：`Owner.RunState.CreateCard` + `CardCmd.Upgrade`（**升级版**，玩家点名）+ `CardPileCmd.Add(Deck)`；
  删牌：`CardPileCmd.RemoveFromDeck`。

### ③ 接取时改动的时机（关键）
新增 `EnsureQuestSetups()`（在 `AfterRoomEntered` 里调）：扫描进行中的这两个任务，
没做过就做一次，并记进 `[SavedProperty] QuestSetupDone`（**必须存**，否则读档会再塞一次）。
**各端都会跑**（牌组是各端复制的，同样改动结果一致 → 联机不同步的问题在构造上就没有）。

### ④ 新奖励维度：**附魔**（借鉴本体 + PengoTarot）
本体自带附魔系统（`EnchantmentModel`，22 种：Sharp/Swift/Adroit/Nimble/Corrupted…），我之前只用了「词条」。
- `RewardKind.Enchant` + `EnchantReward`（照 `KeywordEnchantReward` 写的 CustomReward）：
  点了才开选牌界面 → `CardCmd.Enchant(enchantModel.ToMutable(), card, 1)`。
- 挂了 4 条任务：三费齐鸣 / 整整齐齐 / 精准收割 / 问号猎手。

### 借鉴清单（本机装了 83 个 mod，值得抄的）
- **PengoTarot**：44 张塔罗 + **40 个新附魔** → 已经借了附魔这个维度。
- **TuneStrain（集谐）**：新的资源系统库（偏移/干涉/响应）→ 可做「新资源」类任务。
- **ShopEnhancement**：商店卖牌/刷新 → 商店类任务。
- **五条悟**：新充能球（苍/赫/茈）+ 第四幕 → 新球类任务。
- **Body Burst**：生物死亡连锁 → 死亡类效果。
- **Multiplayer Limit Break**：16 人 → 上限相关。
- ⚠️ 借鉴时注意：**别人 mod 的卡/遗物要装了才有**，别硬依赖；能复用**本体系统**的优先（像附魔）。

### 状态
`dotnet build` 0 错误 0 警告；dll（+pck）已部署。**未上传**。

---

## 54. ⛔ 2026-10-01 【重大事故】EnchantReward 让游戏**启动就崩**（已修）

### 症状
玩家「进不去游戏」。日志实锤（`godot.log`，NGame.GameStartup 阶段）：
```
[ERROR] System.NullReferenceException
   at BaseLib.Abstracts.CustomReward.Initialize()
   at BaseLib.Patches.Content.GenEnumValues.FindAndGenerate()
   at MegaCrit.Sts2.Core.Models.ModelDb.Init_Patch10(Type[] injectedModelTypes)
   at MegaCrit.Sts2.Core.Nodes.NGame.GameStartup()
```

### 真凶
BaseLib 在**启动时**（`GenEnumValues.FindAndGenerate` → `ModelDb.Init`）会自动实例化**所有
"带 static `RewardType` 字段的 `CustomReward` 子类"**，并对每个调 `Initialize()`。
`CustomReward.Initialize()` 第一行就是 `DeserializeMethod.Target` ——
我新加的 `EnchantReward` 写了 `DeserializeMethod => null!` → **空引用 → 启动崩溃**。

### 修法
照 `KeywordEnchantReward` 补齐：`DeserializeMethod => CreateFromSave`（**必须是静态方法**）、
`ToSerializable()`（把附魔 Id 存进 `PredeterminedModelId`）、`CreateFromSave`（`ModelDb.GetById<EnchantmentModel>`）。
⚠️ `ToSerializable()` 会在**空壳实例**上被调用（字段还是 null）→ 里面必须防空（`_enchant?.Id ?? ModelId.none`）。

### 铁律（下次别再犯）
1. **`CustomReward` 子类只要声明了 `static RewardType` 字段，就会被 BaseLib 在启动时 `Initialize()`。**
   → `DeserializeMethod` **绝不能返回 null**，否则**整个游戏起不来**。
2. 改完**先看日志确认没在 `GameStartup` 阶段抛异常**再交付（`grep -aA3 NullReference|GameStartup godot.log`）。

### 顺带澄清（排查时走过的弯路）
- 把 `mods\QuestSpire` **改名没用**：游戏是"扫描 mods 目录里任何带 manifest 的文件夹"，不看名字 →
  改名后照样加载（日志：`Loading assembly DLL ...mods\QuestSpire_off\QuestSpire.dll`）。
  真要停用，得把文件夹**移出 mods 目录**，或在 `settings.save` 的 `mod_settings.mod_list` 里 `is_enabled=false`。
- `settings.save` 的位置：`%APPDATA%\SlayTheSpire2\steam\<SteamID>\settings.save`，
  结构：`mod_settings.mod_list = [{ id, is_enabled, source }]`（改这个 = 设置里取消勾选）。
- 本机还装了 **AncientWaifus**（6 月的 dll，对 v0.111.0 每输入事件抛 `MissingMethodException`）——**另一码事**，
  已顺手在 `settings.save` 里禁用（备份 `settings.save.bak-before-disable-AncientWaifus`）。

---

## 55. 2026-10-01 第十三轮：实测反馈三连修（本地，未上传）

玩家实测报的三条：

### ① 「接取时卡组没变动」→ 其实是**改了，只是等到进下一个房间才改**
`EnsureQuestSetups()` 原来只挂在 `AfterRoomEntered`。玩家在**地图/商店**接完任务**当场看牌组**
当然没变（日志里是"进战斗那一刻"才打 `已生效`）。
**修**：`QuestRunner` 记住 `LocalBook`，新增 `ApplyAcceptSetups()`；`QuestSelection` 确认接取的**当场**就调一次。
（房间那次保留当兜底。）

### ② 「二层精英来一层了，一手打击防御怎么打」→ 精英按幕配的，不能跨幕塞
`EliteAmbush` / `DeckOverhaul` 原来**任何一幕**都加精英，而池子里的 `Byrdonis/Entomancer/InfestedPrism/
BygoneEffigy` 是二幕级别的 → 一幕直接打不过。
**修**：加幕数闸 —— `(Owner.RunState?.CurrentActIndex ?? 0) >= 1`（**只从第二幕起**才加精英）。

### ③ 「假商人打赢本来就给所有假货」→ 奖励重复
本体里打赢假商人**本身就给全部假货遗物**，我们那条「从它那儿抢一件假货」等于没给。
**修**：改成 `AddRandomRelicReward`（一件随机遗物），目录文案 + eng json 同步改；
顺手删掉没人用的 `GrantRandomFakeRelic`。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署。**未上传**。

---

## 56. 2026-10-01 第十四轮：「打赢假商人」改成必定可达（本地，待部署）

玩家：假商人本来就**很难遇到**，还得靠投「污浊药水」触发 → 这任务基本做不完。
**改法（按玩家的方案）**：
1. **接取时**给一瓶「污浊药水」`FOUL_POTION`（`PotionCmd.TryToProcure<FoulPotion>`；加进 `EnsureQuestSetups`）。
2. **接下来必定遇到**假商人：`BeforeSideTurnStart` 里直接 `CreatureCmd.Add<FakeMerchantMonster>(combatState)`
   （它是 165/175 血的事件怪，本体要二幕事件才碰得到）。
3. 判定改成**打死那只怪**就算（`AfterDeath` 里 `creature.Monster is FakeMerchantMonster`），
   因为它是我们召进普通战斗的，房间 Id 不是 FAKE_MERCHANT。原来的房间 Id 判定保留。
- ⚠️ 把事件怪召进普通战斗有未知风险（它的行为可能依赖事件状态），让玩家实测；不行就换成"只发药水 + 保留原事件判定"。

⚠️ 本轮**编译通过但没部署成功**——玩家当时游戏开着，dll 被锁（`SlayTheSpire2.exe`）。关掉游戏后重新 `dotnet build` 即可。

---

## 57. 2026-10-01 第十五轮：精英**每一幕都加**（纠正 §56 的幕数限制）+ 全部部署

玩家澄清：要的是「**接下来所有战斗都加精英**」，第一幕也加 —— 因为大改造把整副牌换成
「飞溅 / 发现」，够强、扛得住。所以 §56 里加的「只从第二幕起」那条**撤掉**。

- `EliteAmbush` / `DeckOverhaul`：`BeforeSideTurnStart` 里**每一幕**都额外召一只精英
  （`Byrdonis / Entomancer / InfestedPrism / BygoneEffigy` 随机）。
- ✅ 本轮连同上一轮的**假商人必遇 + 污浊药水**一起**成功部署**（玩家关掉游戏后）。

### 本地（0.2.24 候选）目前累计未上传的改动
接取当场改牌组 / 每场加精英 / 假商人必遇+药水 / 奖励多样化(24 条) / 多人局随身物生效 /
遗物走原版 RelicReward / 8 条指定核心牌 / 附魔奖励 / 诅咒交易 + 大改造 / 开局启动崩溃修复。
**要发就升 0.2.24。**

---

## 58. 2026-10-01 第十六轮：精英改成「按当前幕挑」+ 已部署

玩家点出：**本体官方每日挑战有"全精英"特效**（修正器 `BigGameHunter`）。
研究结论：`BigGameHunter.ModifyGeneratedMap` 是**地图层**的 —— 进每一幕时重生成地图、精英节点 ×2.5，
**当前这幕改不了**。所以我们**不照搬**，继续用"每场战斗额外召一只精英"（立刻生效）——
但把它改好：**按当前幕挑精英**。

- `PickActElite(seed)`：用 `Owner.RunState.Act.AllEliteEncounters`（本幕精英遭遇）
  → `AllPossibleMonsters`；**只挑单体精英**（`Count()==1`，避开蜈蚣段那种多段怪）；
  用 `seed` 保证各端一致；取不到就退回原来的固定池（Byrdonis/Entomancer/InfestedPrism/BygoneEffigy）。
- 效果：**每一幕、每场战斗**都额外一只**本幕**精英 —— 一幕出一幕的，不会跨幕过强。
- ✅ 已成功部署。

### 顺带挖到
`new ThrowingPlayerChoiceContext()` 可以在**没有 context 的钩子**里调 `PowerCmd.Apply`
（见 `Modifiers.Murderous`）—— §4 记的"齿轮工艺品 PowerCmd.Apply 要 context"的坑可以用它解开。

---

## 59. 2026-10-01 ★ 0.2.24 已上传创意工坊 ✅（未经过真人实测）

玩家：「上传，懒得测了」→ 直接把本地这堆改动打包发了。
- **工坊 id 3806270140**，`Successfully uploaded`；`check_item.py` 复核 result=1、说明 2920 字（含 0.2.24 / 138）、public。
- 说明文压缩老日志后 **6107/8000 字节**；包内 dll/pck/json 全部核对为新版（version 0.2.24，deps 含 BaseLib）。
- 本版含：任务 111→138；奖励多样化（遗物 / 三选一卡牌 / 指定核心牌 / 附魔 / 升级 / 变牌）；
  多人局效果全部生效；设置页扩充（文案语言 / 难度 / 无限刷新 / 显示面板 / 显示奖励）；
  修只狼等奖励被盖；概率平均化；接取当场改牌组；每场加本幕精英；假商人必遇 + 送污浊药水；
  开局启动崩溃（EnchantReward 的 DeserializeMethod=null）修复。
- ⚠️ **没测就发了**：重点观察 —— ① 游戏能否正常启动（上次崩过一次）；
  ② 假商人被召进普通战斗行为是否正常；③ 大改造换成飞升/发现后每场精英是否扛得住。
  出问题就回滚：工坊条目可以在网页上回退版本，或让玩家退订。

---

## 60. 2026-10-01 【bug 修复】问号房 SL 后奖励消失 —— 幂等闸吞掉欠账（本地已修）

玩家：在 `?` 房间 SL，奖励没了。**日志实锤**：
```
读档：发现 3 项任务奖励还没发出去，等进下一个房间补发。
补发：3 项欠着的任务奖励进了待发队列。
任务「大改造」的奖励已经发过了，跳过（防重复）。      ← 被闸挡回去
（只狼·弹反 / 淬火修行 同样）
```
**根因**：「对账」把欠着的账重排进了待发队列，但 `GrantRewardNow` 开头就是
`if (!_rewardedKinds.Add(kind)) 跳过` —— **「开始发过」≠「送到」**。
奖励排进了 `_batch`（**内存、不进存档**），玩家一 SL，`_batch` 没了，
可 `_rewardedKinds` 是 `[SavedProperty]`（进存档）还记着 → 闸把重发拦住 = 奖励**永久丢**。

### 修法（三处，`QuestBookRelic`）
1. **闸对"欠账"放行**：`bool owed = _dueKinds.Contains(kind)`，`owed` 时跳过幂等闸（该重发就重发）。
2. **`GrantRewardNow` 的返回值改成"是否真排进了 `_batch`"**（`_batch.Count` 前后比），
   原来用 `_dueKinds.Contains` —— 补发时那个**永远为真**，会把"当场到手"的也当成排队、账销不掉。
3. **当场到手的记「已送到」并销账**：`Reward()` 的即时分支和 `FlushPendingRewards` 的补发循环
   都补 `ClearDue + _paidKinds.Add`。这样对账：`完成 − 已送到 − 欠着` 就干净了。

> 教训：**"开始发" 和 "送到了" 是两回事**，用前者当幂等闸，一定会在"发到一半被打断"（SL / 崩溃）时吞奖励。


### ⚠️ 上传状态
**0.2.24 已经带着这个 bug 上线了**（玩家刚上传）。本地已修，**建议尽快升 0.2.25 补上**。

---

## 61. 2026-10-01 【bug 修复】奖励被**重建两遍**（本地已修）

§60 修完后再看日志：**"已经发过了，跳过"没了**（放行生效 ✓），但暴露新问题：
```
补发：2 项欠着的任务奖励进了待发队列。       ← 同一批，出现 2 次
Quest completed: 蓄势待发 -> 奖励已加入：…   ← 每个任务建了 2 次
发放中：4 项先攒着…                          ← 4 项（2 种 × 2）
```
**根因**：`RescheduleRestoredRewards()` 被调了**两次** ——
`AfterRoomEntered` 里调一次（旧代码 1224 行），`FlushPendingRewards()` 里又调一次（2203 行）
→ 同一批欠账**重建两遍**，`_batch` 里每样变两份。
**修**：删掉 `AfterRoomEntered` 里那次，只保留 `FlushPendingRewards` 里的。

> 另外日志反复出现 `任务奖励：此刻还在战斗流程里，4 项留着下次发` ——
> 那是**设计行为**（避免和战斗结算界面打架，等出战斗再发）；叠加上面的重复重建，
> 就变成"每次进房间都重建 4 项、又被战斗押着"的循环。去掉重复后应能收敛。

本地已修 + 部署。⚠️ 工坊 0.2.24 里这两个 bug（§60 吞奖励 / §61 重建两遍）都还在 → 要尽快升 0.2.25。

---

## 62. 2026-10-01 【bug 修复】奖励"点不动、只能跳过" —— 旧 Player 实例（本地已修）

玩家：奖励列表能出，但**点了没反应，只能跳过**。**日志实锤**：
```
[ERROR] System.InvalidOperationException: SelectLocalReward called for reward
   ...RelicReward / ...CardReward with non-local player 1! This is not allowed
   at ...RewardsSetSynchronizer.SelectLocalReward(Reward reward)
   at ...NRewardButton.GetReward()
```

### 根因
游戏 `RewardsSetSynchronizer.SelectLocalReward` 第一行是：
```csharp
if (reward.Player != LocalPlayer)  // ← **实例引用**比较，不是比 NetId！
    throw ...
```
**快速 SL** 会把这一局重建 → 本地玩家变成**新实例**；而我们塞给奖励界面的那些奖励对象
还攥着**旧的 Player 实例** → `!=` → 被判"非本地玩家" → **点不动、只能跳过**。
（错误信息里的 `player 1` 是 NetId —— 编号一样、实例不一样，所以极具迷惑性。）

### 修法
`QuestBookRelic.ClearInMemoryRewardObjects()`：读档 / 快速 SL 后（`Entry.OnRunStarted` 的读档分支）
把内存里的奖励对象**全丢掉**（`_batch` / `_pendingRewards` / `_pendingRelics`）。
存档账本（`_dueKinds` / `_paidKinds`）**不动** → 进下一个房间会按**当前玩家**重建成新对象，实例就对得上。
> 注意：SL 之后**已经弹出来的那个**奖励框里的旧对象清不掉了 —— 它可能仍然点不动，
> 但**进下一个房间**会重发可领的新对象。

### 累计教训（奖励链路的三种"看起来一样"的坏法）
1. §60 幂等闸吞欠账（"开始发"≠"送到了"）；
2. §61 重复重建（同一批账建两遍）；
3. §62 旧 Player 实例（实例比较，SL 后失配）。
—— 全都是"SL / 中断"把**内存对象**和**存档账本**之间的时序差放大出来的。

---

## 63. 2026-10-01 ★ 0.2.25 已上传创意工坊 ✅

修 §60/§61/§62 三个奖励链路 bug 后的补丁版。
- **工坊 id 3806270140**，`Successfully uploaded`；`check_item.py` 复核 result=1、说明 3090 字、含 0.2.25。
- 说明文 6503/8000 字节；包内 dll/pck/json 核对为新版（version 0.2.25，deps 含 BaseLib，
  dll 含 `ClearInMemoryRewardObjects` / `PickActElite` / `GiveFoulPotion` / `ApplyAcceptSetups`）。
- 本版修：① 欠账被幂等闸吞（SL 后奖励丢）；② 同一批奖励重建两遍；③ SL 后奖励**点不动只能跳过**（旧 Player 实例）。
- ⚠️ 这三条**都还没经过真人实测**（玩家自己测到的，我照日志修的）——留意是否还有残留。

---

## 64. 2026-10-01 第十七轮：用没用过的钩子做了 4 条新任务（本地，未上传）

玩家：「自己继续找点乐子吧」→ 翻 `AbstractModel` 的钩子表，挑**没用过**的做了 4 条（不是凑数）。

| QuestKind | 标题 | 条件 | 钩子 | 奖励 |
|---|---|---|---|---|
| `FlushCount`      | 轮回             | 本局弃牌堆洗回抽牌堆 8 次 | `AfterFlush` | 一张能力牌三选一 |
| `PotionDiscarded` | 我说丢药有没有懂的 | 累计丢弃 6 瓶药水         | `AfterPotionDiscarded` | 一张牌三选一 |
| `EnergyTotal`     | 能量黑洞         | 本局累计花掉 120 点能量    | `AfterEnergySpent` | 一张稀有牌三选一 |
| `OstyRevive`      | 骨小妹·不死      | Osty 复活 3 次（亡灵绑定者）| `AfterOstyRevived` | 随机一件遗物 |

- ⚠️ `AfterEnergySpent` **已经被"单回合花能量"那条占了** → 这次是**并进同一个重写**里
  （同一个钩子不能重写两次，编译会报 CS0111）。以后加同类先 grep 一下有没有重写过了。
- `AfterPotionDiscarded` / `AfterOstyRevived` 都**没有 Player 参数**（靠"只有自己的"来近似）。
- 任务数 **138 → 142**；英文本地化补齐；dll + pck 已部署。**未上传**（要发就是 0.2.26）。

---

## 65. 2026-10-01 第十八轮：又 3 条（批之六，本地未上传）

玩家：「继续」。接着挖没用过的钩子：

| QuestKind | 标题 | 条件 | 钩子 | 奖励 |
|---|---|---|---|---|
| `PileMoved`      | 牌堆搬运工         | 牌在牌堆之间移动 300 次 | `AfterCardChangedPiles` | 一张牌三选一 |
| `PotionProcured` | 我说给药有没有懂的 | 累计获得 8 瓶药水       | `AfterPotionProcured` | 一张牌三选一 |
| `CardReplay`     | 我说重放有没有懂的 | 让牌重放 5 次           | `AfterModifyingCardPlayCount` | 一张稀有牌三选一 |

- ⚠️ `BeforeCardAutoPlayed` 那条**放弃了**：它的参数类型 `AutoPlayType` 在反编译里**找不到公开类型**
  （可能内部/嵌套），怕编译炸就没做。以后要用先确认类型可见。
- 任务数 **142 → 145**；英文补齐；dll + pck 已部署。**未上传**（要发 = 0.2.26）。

### 还能继续挖的（下次）
`AfterCardEnteredCombat` / `AfterActEntered` / `AfterModifyingGoldGained` / `AfterModifyingHandDraw` /
`AfterPreventingBlockClear` / `BeforeDamageReceived` / `AfterDamageReceivedLate` / `AfterCardPlayedLate` …

---

## 66. 2026-10-02 第十九轮：又 4 条（批之七，本地未上传）

玩家：「自己找点乐子」→ 继续挖没用过的钩子（先 grep 确认没重写过，避免 CS0111）：

| QuestKind | 标题 | 条件 | 钩子 | 奖励 |
|---|---|---|---|---|
| `ActThree`            | 一步之遥           | 走到第三幕                 | `AfterActEntered`（看 CurrentActIndex≥2） | 随机遗物 |
| `BlockClearPrevented` | 永固               | 阻止格挡被清空 3 次        | `AfterPreventingBlockClear`（壁垒/卡尺那类） | 一张能力牌三选一 |
| `DamageTakenCount`    | 皮糙肉厚           | 累计**挨打次数** 30 次     | `BeforeDamageReceived` | 一张牌三选一 |
| `DeckAddPrevented`    | 我说别塞有没有懂的 | 阻止 3 张牌进入牌组        | `AfterAddToDeckPrevented`（比如御守挡诅咒） | 一张牌三选一 |

- 「皮糙肉厚」是**次数**维度（之前只有"挨了多少血"）；「我说别塞」是**阻止**维度（之前没有）。
- 任务数 **145 → 149**；英文补齐；dll + pck 已部署。**未上传**（要发 = 0.2.26）。

---

## 67. 2026-10-02 第二十轮：把「梗化」那批梗做成任务（批之九，本地未上传）

玩家：「你这次不是做了很多梗嘛，可以把有些梗做成任务写进去」→ 第一版做了 6 条**计数型**，
玩家反馈「**这些任务没意思**」——复盘发现它们其实是已有任务的换皮
（干饭人≈药剂大师、钞能力≈挥金如土、内卷≈花拳绣腿、退退退≈龟壳）。**已删掉那 3 条换皮的**，
改成玩家要的方向：**「改玩法的挑战型」**（接下后改变接下来几场战斗的打法）。

| QuestKind | 标题 | 类型 | 条件 | 实现 | 奖励 |
|---|---|---|---|---|---|
| `WorkerCards`   | 打工人   | 计数 | 本局打出 60 张「打击/防御」起始牌 | `AfterCardPlayed` 里原有的 `STRIKE_/DEFEND_` 判断块 | 金币 |
| `MokugyoSkills` | 电子木鱼 | 计数 | 本局打出 50 张技能牌 | `AfterCardPlayed` 技能分支（新字段 `_skillsThisRun`） | 任意牌三选一 |
| `RetreatBlock`  | 退退退   | **挑战** | 接下后 2 场**不能打攻击牌**；赢 2 场 | 打攻击牌 → 本场标记作废；`AfterCombatVictory` 里没违规才 +1 | 稀有牌三选一 |
| `SlackOff`      | 摸鱼     | **挑战** | 接下后 2 场**前 2 回合不能出牌**；赢 2 场 | `_combatTurnIndex<=2` 时出牌 → 作废 | 能力牌三选一 |
| `NakedRun`      | 裸装     | **挑战** | 接下后 3 场**不能打能力牌**；赢 3 场 | 打能力牌 → 作废 | 随机遗物 |
| `SanheGod`      | 三和大神 | **叙事** | 一局里在篝火休息 3 次，且**一次铁都没打** | 新增 `[SavedProperty] RestsForSanhe`，在 `AfterRestSiteHeal` 里判 `RestsForSanhe>=3 && Smiths==0` | 随机遗物 |
| `FamilyBucket`  | 全家桶   | **叙事** | 牌组里同时有 6 张攻击 / 6 张技能 / 6 张能力 | `CheckDeckQuests()` 里加一行（它本来就数了 attacks/skills/powers） | 能力牌三选一 |

**挑战型的写法（跨文件，七步）**：
1. `QuestModel.cs` **枚举末尾追加**（铁律①，存档存 int，插中间串档）；
2. `QuestBookRelic.cs`：新增"本场违规"标记字段 + 在 `AfterRoomEntered` 里和别的 `_xxxThisCombat` 一起**清零**；
3. 在 `AfterCardPlayed` 的分类分支里置标记；
4. 在 `AfterCombatVictory` 的契约类计数那一段加 `if (!标记) Grant(...)`（**违规只是本场不计，不算失败**）；
5. `QuestCatalog.cs` 加条目；6. `eng/relics.json` 补英文；7. 跑 `gen_questtexten.py`。

- 任务数 **152 → 159**；编译 0 错 0 警告；pck 重打；dll + pck 已部署。**未上传**（要和 0.2.26 一起发）。
- ⚠️ 删枚举成员时**必须同时删掉所有引用**（这次漏删 `FoodiePotions`/`CashPower`/`InvolutionTurn` 三处调用，编译报 CS0117）。
- ⚠️ 这些标记和 `_turnCards`/`_blockThisCombat` 一样**没有 `[SavedProperty]`**，SL 后当回合重算（与「花拳绣腿」「龟壳」同口径）。
- ⚠️ **还没真人实测**。上线前重点看：挑战任务能不能抽到、违规判定对不对、赢 2/3 场后奖励发不发。

---

## 67. 2026-10-02 UI 可缩放（玩家：面板太大 / 设置里能调大）

- 设置页新增 **`UiScale`**（`[ConfigSlider(0.7, 1.4, 0.05)]`，默认 **0.85**，玩家反馈原来太大）。
- 作用面：`QuestPanel.Width = 330 * scale`；`UiKit.ApplyFont` 的基准 18 与面板里的 17/14/13 全部走
  `UiKit.Scaled(base)`（下限 8，防缩到看不见）。
- **改完立刻生效**：`QuestPanel._Process` 里比对 `_scaleBuilt` 与配置，变了就 `HidePanel + ShowPanel` 按新尺度重建。
- ⚠️ 面板宽度变成了 **`static` 属性**（原来是 `const`）——以后别在 `const` 上下文里用它。

---

## 68. 2026-10-02 ★ 0.2.26 已上传 ✅

- 工坊 id 3806270140，`Successfully uploaded`；check_item 复核 result=1、说明 3726 字、含 0.2.26/159。
- 本版：任务 138 → **159**（7 条梗任务 + 我全都要·攻/技/能）；**首次引入 Harmony 补丁**
  （`Patches/CardAddedToDeckPatch`，补"卡牌加入卡组"钩子）；面板 **UI 缩放**（默认 0.85×，0.7~1.4 可调，改完即生效）。
- 说明文 7689/8000 字节。
- ⚠️ 仍**未真人实测**（Harmony 补丁 + 新任务 + UI 缩放）——上线后留意启动与奖励领取。


---

## 69. 2026-10-03 第二十轮：批之十（速度 / 牌组极致 / 新奖励「删牌」）

玩家：「自己找点乐子」→ 按"新形状"挑了一批（不是又堆计数），任务数 159 → **167**。

| QuestKind | 名称 | 条件 | 奖励 |
|---|---|---|---|
| FirstTurnKill   | 一发入魂   | 第一个回合就把这场战斗打完 | 稀有牌三选一 |
| SpeedContract   | 限时特惠   | 接下后，接下来 3 场都得在 3 回合内打赢 | 随机遗物 |
| EliteSpeedKill  | 落地成盒   | 3 回合内打赢一场精英战 | 金币 +250 |
| MinimalDeck     | 极简主义   | 牌组 ≤10 张时赢下一场 | **删一张牌** |
| ZeroCostDeck    | 斗地主附体 | 牌组里有 8 张 0 费牌 | 稀有牌三选一 |
| DiscardInCombat | 羊关通关   | 单场战斗弃掉 25 张牌 | 一张牌三选一 |
| RainbowDeck     | 国际纵队   | 牌组里有 4 张别的职业的牌 | 技能牌三选一 |
| PurgeMaster     | 清仓大甩卖 | 本局一共删掉 6 张牌 | **删一张牌** |

### ① 新奖励维度：删牌（`RewardKind.RemoveCard`）
用本体自带的 `CardRemovalReward`（和商店花钱删牌**同一个界面**）。
联机不发 → 折现 150 金币：删牌走的是 `DoUnsyncedCardRemoval`，没有同步手段，
各端删的牌不一样就会被判数据不同步（和「升级牌组」类奖励同一条政策）。

### ② 第二个 Harmony 补丁：卡牌从卡组里被删掉
`Patches/CardRemovedFromDeckPatch` 补 `CardPileCmd.RemoveFromDeck(CardModel, bool)` 的 Postfix。
**反编译核实过**：商店花钱删牌（`OneOffSynchronizer.DoMerchantCardRemoval`）、
删牌奖励（`RewardSynchronizer.DoUnsyncedCardRemoval`）、事件/卡牌删牌**全都走这个命令** →
一处补丁覆盖所有来源，不漏也不重复计。

### ③ 顺手加固：Harmony 补丁改成**逐个打**
原来 `PatchAll(assembly)` 是"一个失败整批失败"——某个补丁目标签名对不上，另一个好的也会被连累。
现在用 `CreateClassProcessor(类型).Patch()` 一个一个打、各自记日志，坏的只影响它自己。

### ④ 新增工具
`tools/check_i18n_coverage.py`：一条命令校验"枚举 / 目录 / 英文表 / C# 英文兜底"四方对齐。

### 状态
`dotnet build` 0 错误 0 警告；dll + pck 已部署本地；文案校验 167 条 0 缺失。
**未上传**（本地版本号还是 0.2.26，和工坊同号但内容更多；要发就升 **0.2.27**）。
⚠️ 未实测：重点看 ① 启动（新增了补丁）② 「删牌」奖励点了能不能正常选牌。
---

## 70. ⛔ 2026-10-03 【严重事故】战斗卡死 = 我们往普通房间塞了"会登场的精英"（已修）

### 玩家描述
「杀戮尖塔卡住了」——战斗打到一半整个卡住，只能退游戏。

### 日志实锤（`godot2026-10-03T23.40.36.log`）
```
[INFO] Monster BYGONE_EFFIGY performing move WAKE_MOVE
[ERROR] Combat #1 turn loop died while its combat is in progress;
        the combat is stuck until the room is restarted:
  System.InvalidOperationException: You can only trigger the elite transition in an elite room
    at NRunMusicController.TriggerEliteSecondPhase()
    at BygoneEffigy.WakeMove(...)
    at MonsterMoveStateMachine.MoveState.PerformMove(...)
    at Creature.TakeTurn()
    at CombatManager.ExecuteEnemyTurn(...)
```
`Combat #1 turn loop died ... the combat is stuck until the room is restarted`
= **回合循环崩了 → 战斗永久卡住**（游戏自己的错误信息）。

### 根因（我们的锅）
「精英加餐 / 大改造」会把一只**精英怪塞进普通战斗**。而游戏的
`NRunMusicController.TriggerEliteSecondPhase()` 第一行就抛错：**它要求当前房间是精英房**。
我们塞进去的精英一旦走到那一步 → 抛异常 → **敌人的回合直接崩 → 战斗卡死**。

**反编译核实：全游戏只有 2 处**会调到 `TriggerEliteSecondPhase`，正好都在我们池子里的两只怪身上：
| 怪 | 触发点 | 危险程度 |
|---|---|---|
| `BygoneEffigy`（往生遗像） | `WakeMove()` —— 开局"苏醒"那一下 | 进场就炸 |
| `InfestedPrism`（寄生棱晶） | `InfestedPower.RevealWrigglersAfterDeathAnim()` —— **死亡时**才触发 | 更阴，前面几回合看不出来 |

（Byrdonis / Entomancer 只是自己换阶段，**不碰**这个机制，安全。）

### 修法
1. `PickActElite()` 里加 `.Where(m => !IsUnsafeElite(m))`。
2. 新增 `IsUnsafeElite()`：按 Id 排除 `BYGONE_EFFIGY` / `INFESTED_PRISM`，**注释里写明原因和证据**。
3. 兜底固定池里**删掉这两只**（原来 `case 2/3` 就是它们），只留 Byrdonis / Entomancer。

### 铁律（新增）
> **往普通战斗里塞怪之前，先确认那只怪不会依赖"它是精英房"这个前提。**
> 检查方法：反编译搜 `TriggerEliteSecondPhase`（或其它 `RoomType.Elite` 断言）的调用点。
> 一旦在敌人回合里抛异常，**战斗会永久卡死**，玩家只能强退。

### 状态
`dotnet build` 0 错误。⚠️ **还没部署**（排查时游戏正开着，dll 被锁）。
⚠️ **工坊 0.2.26 带着这个 bug**，玩家会卡死 —— 修好后应尽快升 **0.2.27** 上传。

### 附带发现（不是这个 bug）
同一晚另一次会话「进主菜单 471 秒」（`Time to main menu: 471119ms`），那次**没有**这条报错，
更像是"回主菜单时资源重载慢"，另一回事。正常值约 25~43 秒。

---

## 2026-10-04（下午）：中文玩家看到英文界面 —— 已修

**玩家反馈**：「作者你好，请问为什么接取任务界面显示的是英文，是我的 mod 顺序有问题吗」

### 根因（不是 mod 顺序）

`QuestText.Choose` 在「跟随游戏」模式下会去查本地化表：

```csharp
default: return Resolve(locKey, cn);   // ← 旧代码
```

而我们只给 `QuestSpire/localization/zhs/relics.json` 写了**遗物那 21 条**，
任务文案（`QUEST_SPIRE_QUEST_*.title` 之类，约 400 条）**一条都没写**。

问题在于：**游戏本地化系统在当前语言查不到 key 时，会回退到英文表**。
mod 的 `eng/relics.json` 有 419 条（任务文案全在里面），于是：

> 中文游戏 → zhs 表没有 → 回退到 eng 表 → 查到英文 → `LocString.Exists` 返回 **true** → 显示英文

所以中文玩家看到的是英文，英文玩家反而正常。遗物名显示中文是对的（那 21 条在 zhs 表里）。

### 修法

「跟随游戏」时**按游戏语言直接选文案，不再查表**：

```csharp
default: return QuestSpireConfig.GameIsEnglish() ? en : cn;
```

- `QuestText.Choose` 和 `QuestText.Gold` 两处都改了。
- `QuestSpireConfig.GameIsEnglish()` 从 `private` 改成 `public` 供上面调用。
- 中文文案本来就在 `QuestCatalog` 里硬编码着（`cn` 参数），不需要额外补表。
- `QuestTextEn`（英文 C# 表）不受影响，英文游戏照旧显示英文。

### 状态

`dotnet build` **0 警告 0 错误**，dll 已自动部署到游戏 mods 目录（2026-10-04 14:50）。
⚠️ **工坊上还是旧版**，要玩家拿到修复得重新上传。

### 下次注意

**别把"给中文玩家看的 key"只写进 eng 表** —— 只要有 fallback，中文环境就会捞到英文。
mod 自己渲染的文本，要么在 C# 里按语言直接选，要么 **zhs / eng 两张表都写全**。

---

## 2026-10-04：**0.2.28 已上传**（工坊 id 3806270140）

- 版本号、`workshop/meta.json` 的 changeNote、`workshop/description.md` 的更新日志都改了。
- 上传流程照旧：`python tools/prepare_workshop.py` → 在 `D:\software\sts2-mod-uploader`
  跑 `.\ModUploader.exe upload -w .\QuestSpire`。

### 顺带踩到的坑：工坊说明文的 8000 **字节**上限

第一次上传被拒，只回一句 `k_EResultInvalidParam`，看不出是哪里错。
排查发现 `description.md` 是 **3953 字 / 8184 字节** —— **超了 8000 字节**。
（Steam 这里卡的是字节数，中文一个字 3 字节，所以看着才 3900 字就已经爆了。）

把最老的几条更新日志（0.2.8~0.2.21）合并成一条后降到 7961 字节，上传立刻成功。

**`prepare_workshop.py` 现在会打印字节数并在超限时警告**，别再靠猜。
