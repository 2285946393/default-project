# 篝火斗地主：赌注平衡 + 角色引用（wb 交付记录）

日期 2026-10-08 15:05 · 产物 md5 `0ea7edbfae5e92ce`（308,224 B）· 0 警告 0 错误 · 已部署

用户两条要求（原话）：

> 「ai应该跟我的钱差不多。不能让我赢太多。也不能让我输太多。如果我是零元就拒绝我斗地主。不能让我白手起家」

> 「角色没有正确引用。我这边还战士。没有变成卡芙卡」

---

## 一、赌注平衡（改完即生效，不用测试就能算出来）

### 改之前的问题（用户上一把的实况）

```
斗地主：篝火赌注开局 —— 我 99 金币（一份 10），对家各 1000 / 1000。
斗地主：篝火金币结算 99 -1280 → 0（倍数 ×64）
```

三个毛病叠在一起：

| 毛病 | 为什么是毛病 |
|---|---|
| 对手固定 1000 | 我 99 本金，却能从一个 1000 的对手身上赢 1000 —— 「赢太多」 |
| 一份 = 本金/10（99→10），但**倍数不封顶** | ×64 时一份变成 640，地主一次赔 1280 —— 「输太多」，一把清零 |
| 0 金币也开桌 | 白手起家 |

### 改之后的三条规则

1. **三家起始筹码都 = 我身上的真实金币**（不再是 99/1000/1000）→ 「AI 跟我的钱差不多」
2. **一份封顶**：`一份 ≤ 本金 × 25% ÷ 2`（地主一个人吃两份，所以一局最多动本金 25%）
3. **0 金币拒绝开桌**：篝火点「斗地主」直接弹提示、不开桌

新增两个可在设置界面拨的旋钮（`DouSpireConfig`）：

| 键 | 默认 | 含义 |
|---|---|---|
| `BetUnitDivisor` | 20 | 一份底注 = 本金 ÷ 这个数（下限 1，上限仍是 `BaseStake`=100） |
| `BetMaxSwingPercent` | 25 | 一局最多输赢本金的百分之几 |

### 99 金币时的手算结果（可以对答案）

| | 改前 | 改后 |
|---|---|---|
| 三家筹码 | 99 / 1000 / 1000 | **99 / 99 / 99** |
| 一份底注 | 10 | 4（=99÷20，整数除） |
| 一份封顶 | 无 | 12（=99×25%÷2） |
| 平局（×1） | 地主 ±20 / 农民 ±10 | 地主 ±8 / 农民 ±4 |
| 满配（×2×2×…=×64） | 一份 640 → 地主 **−1280** | 一份封顶 **12** → 地主 **−24** |
| 最坏情况 | 一把清零 | 一把最多掉 24%（99→75） |

倍数**照常显示**（明牌/加倍/炸弹该怎么翻怎么翻，牌局的乐趣不动），只是**金币按封顶结算**；
真撞上封顶时结算文案会自己说明白，免得看着 ×64 却只拿到一点点以为是 bug：

```
（本局倍数 ×64　金币按封顶 12/份 结算）
```

### 逐文件

| 文件 | 改了什么 |
|---|---|
| `DouSpireConfig.cs` | 新增 `BetUnitDivisor`(20)、`BetMaxSwingPercent`(25) |
| `Table/DouSpireTable.cs` | `_stakeCap` / `_stakeCapped` 两个字段；`ResetMoney()` 重写（三家同钱 + 算封顶 + 0 金币改成不赌的兜底）；`PayOut()` 按封顶结算并在日志里标 `→封顶N`；`ResultWithMultiplier()` 补封顶说明；新增 `MyGoldNow()` / `SafeGold()` 供入口问价；`OpenAtRestSite()` 加拒绝分支 |
| `Table/DouSpireRestSite.cs` | `OnSelect()` 先问 `DouSpireTable.MyGoldNow()`，≤0 就弹 toast + `return false`（不消费篝火、不开桌） |

**入口拦两道**：`OnSelect()`（正常入口）+ `OpenAtRestSite()`（兜底，防别的路径绕过来）。
`MyGoldNow()` 读不到金币时返回 null → **不拦**（宁可多开一桌，也别因为读不到就把人挡在门外）。

`ResetMoney()` 里还有第三道：真走到赌注模式却发现钱是 0，就把这一桌**降级成纯记分板**
（`_stakePlayer = null`）—— 保证「不能白手起家」这条在任何路径下都成立。

---

## 二、角色引用（「我这边还战士，没有变成卡芙卡」）

### 结论：**根因不在我们这半边，我们少调了游戏自己会调的一步。**

先说一件容易看错的事：日志里

```
斗地主（篝火）：我 = 卡芙卡
斗地主：KAFKA-KAFKA_CHARACTER 骨骼就绪，可以动起来了。
斗地主：活角色就位 —— KAFKA-KAFKA_CHARACTER（战斗版（站着）） 目标高=300 缩放=1.079 镜像=否
```

**模型是对的**（就是 KAFKA 的），所以问题不在选人、不在数据，而在**"这具身体是从哪个场景来的"**。

### 证据链

1. **缩放 1.079 和 IRONCLAD 一模一样**（别的角色各不相同：`REGENT 0.896`、`DEFECT 1.154`、
   `NECROBINDER 1.111`、`SILENT 1.23`）。缩放 = 300 ÷ 自然高度，所以两具身体的 `Bounds`
   完全相同 → **同一个场景**。

2. **Kafka 那个 mod（工坊 `3747588119`）根本不带角色场景。** 把它 51 MB 的 `Kafka.pck`
   逐关键字数了一遍：

   | 关键字 | 出现次数 |
   |---|---|
   | `creature_visuals` | **0** |
   | `rest_site` | **0** |
   | `ironclad` | **0** |
   | 自带的场景 | 只有 2 个：`char_select_bg_kafka_character`、`kafka_character_icon` |
   | 自带的骨骼 | 有：`res://Kafka/images/character_anim/spine_anim/kafka_anim.{spine-json,atlas}` |

   → 它的角色**是借本体的壳**（`CharacterModel.VisualsPath` 是 private 非 virtual，改不了，
     所以只能 Harmony 补丁 `get_VisualsPath`）。

3. **它的补丁名单**（游戏日志里 `[Kafka] Patched:` 是权威的）里有关键三条：

   ```
   CharacterModel.get_VisualsPath      ← 让卡芙卡借用某个本体的场景
   CharacterModel.CreateVisuals
   CharacterModel.GenerateAnimator
   NCreature._Ready                    ← ★ 换装在这里
   NRestSiteCharacter._Ready           ← ★ 休息处那套也在
   ```

   DLL 的元数据里有这些方法名，一看就明白它在干什么：

   ```
   CreatureReadyPatch
   ReplaceSpineWithKafka      ← 换成卡芙卡
   HideSpineRecursive         ← 把原来的骨骼藏起来
   CreateKafkaSpineContainer  ← 塞进自己的骨骼容器
   LoadSpineResource / TryCreateSpineNode / KafkaSpineVisuals
   ```

4. **我们牌桌上不建 `NCreature`**（也不需要，我们只是把角色摆上去）。
   于是 `NCreature._Ready` 那个补丁**永远不跑** → 屏幕上留着**没换过的原壳**
   → 看起来就是「还是战士」。

   对照：游戏本体是在 `NCreature.cs:509-513` 里换的 ——
   `_spineAnimator = Entity.Player.Character.GenerateAnimator(Visuals.SpineBody, Entity);`

### 做了什么

| 措施 | 说明 | 风险 |
|---|---|---|
| **补一刀 `GenerateAnimator`** | 照 `NCreature._Ready` 的做法，在「骨骼就绪」之后、播第一个动作之前，调一次 `CharacterModel.GenerateAnimator(骨骼, creature)`。**只借副作用**，返回的 animator 丢掉（骨骼仍由我们自己驱动） | 低。只对 **mod 角色**调（`IsModCharacter` = 类型命名空间不以 `MegaCrit.` 开头），本体角色完全走原来的路；整段 try/catch |
| **`ModCharsPreferSitting` 开关**（默认关） | 打开就改走「休息处那套」（坐姿）—— **那条路 mod 是真的补了 `NRestSiteCharacter._Ready` 的**，所以是保底 | 只影响 mod 角色；本体角色永远站着 |
| **角色自检日志** | 每个角色多打两行：节点类/场景路径/骨骼子节点（带显示隐藏）、骨骼包围盒/动画表 | 零（只写日志） |

> ⚠️ `GenerateAnimator` **不吃 null**（本体实现里会 `creature.GetHpPercentRemaining()` 直接炸），
> 所以必须给一个真 `Creature` —— 用的是我自己的 `_stakePlayer.Creature`。
> 拿不到就跳过并记一条日志（不影响玩）。

### 怎么验证（打开就能看）

**① 赌注**：进篝火 → 点「斗地主」，日志应该出现

```
斗地主：篝火赌注开局 —— 三家各 99 金币，一份 4，一份封顶 12（一局最多输赢 25% 本金）。
```

三个座位头顶的金币数字应该**都是 99**（改前是 99 / 1000 / 1000）。
打一把输掉，结算应该是 `99 -8 → 91` 这种量级，不再是 `-1280 → 0`。

**② 零元拒绝**：把金币花到 0（或找个 0 金币的存档），点「斗地主」→ 应该只弹一句
「你身上一个金币都没有 —— 先去挣点钱再来斗地主。」，**桌子不开、篝火不消费**。
日志：`斗地主：身上 0 金币，拒绝开桌（没钱不赌）。`

**③ 角色**（重点看这两行新日志）：

```
斗地主：角色自检 —— 模型=Kafka.KafkaCode.Character.KafkaCharacter 节点=… 场景=… 来路=… 骨骼子节点=[…]
斗地主：KAFKA-KAFKA_CHARACTER 骨骼自检 —— 包围盒=… 动画表=…
斗地主：KAFKA-KAFKA_CHARACTER 已按 NCreature 的做法调过 GenerateAnimator（返回 …）
```

- **如果卡芙卡出来了** → 好，收工。
- **如果还是战士** → 把设置里的 `ModCharsPreferSitting` 打开，重开桌子（F9 收 / F9 开）。
  那一席会变成**坐姿**的卡芙卡。要是还不行，把上面那三行日志发我 ——
  `场景=` 和 `骨骼子节点=` 直接告诉我们那具身体到底是谁的，一次就能定位。

---

## 三、没改的 / 边界（说清楚，免得以后当成 bug 查）

1. **多人局的钱是各算各的**：`SettleGold()` 只往**本机**的 `_stakePlayer` 上结。
   联机时每个人的钱包独立，真联机对局下三个人的钱包数字可能对不上。彩蛋玩法，先不动。
2. **封顶只封金币，不封倍数**。倍数是牌局的记分，金币是钱包 —— 我按"赌桌上限"的思路做的
   （赌场是"台面上限"而不是"不许你翻倍"）。用户要是觉得 ×64 只结算一点很别扭，
   改成"倍数也一起封顶"是几行的事，说一声就改。
3. **本金很小时封顶兜不住**：金币 ≤7 时，一份最小是 1、地主一把最多 ±2 ——
   按比例算超过 25%。绝对数额小（2 金币），没专门处理。
4. **只在单人篝火做了"拒绝开桌"**。联机下这条会按"本机玩家没钱"处理；
   没深入测过多人的分支（怕误伤队友）。
5. **没动 `1a′`（`min_game_version` 要不要回调到 0.107.1）** —— 那条要单独验，不能猜。
6. **这个改动没有 git 提交**：`projects/dou-spire/` 是未跟踪目录，只存在于磁盘上。

---

## 四、证据索引（想自己复核的话）

| 想确认的事 | 看哪 |
|---|---|
| 缩放 1.079 和 IRONCLAD 撞车 | `%AppData%\SlayTheSpire2\logs\godot*.log` 里所有 `活角色就位` 行，按角色分组 |
| 卡芙卡不带角色场景 | `grep -a -c creature_visuals "…/workshop/content/2868840/3747588119/Kafka.pck"` → `0` |
| 它靠补丁换装 | `godot.log` 里 `[Kafka] Patched:` 列表；DLL 里的 `ReplaceSpineWithKafka` / `HideSpineRecursive` |
| 游戏本体在哪一步换 | 反编译源码 `MegaCrit.Sts2.Core.Nodes.Combat/NCreature.cs:509-513` |
| 外观从哪个场景来 | `CharacterModel.cs:117` `VisualsPath => "creature_visuals/" + Id.Entry.ToLowerInvariant()` |
| 本体角色的命名空间 | `MegaCrit.Sts2.Core.Models.Characters.*`（mod 角色在它自己的命名空间下） |
