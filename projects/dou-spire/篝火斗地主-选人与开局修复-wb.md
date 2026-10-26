# 篝火斗地主 · 选人 / 开局 / 角色修复 交棒回执（wb · 2026-10-08 14:50）

> 接手人：**国内台 wb**。上一手是**国际台（国际 WB）**—— 它在 14:37 撞了
> `429 usage exceeds frequency limit`（额度 23:30 重置），用户在 14:41 说
> 「工作我让国内wb接走了。你就别改代码了。先思考怎么做」。
>
> **本轮只动了 `DouSpireCode/Table/DouSpireTable.cs` 一个文件**（+3 处、1 个新方法、2 个 using）。
> 编译 **0 错 0 警**，产物**已部署**（dll md5 与本地一致）。
> **没有启动游戏**（按老规矩，实机验收交给用户）。

---

## 一、用户报的 4 个问题（原话）

14:28：
> 「1没有读取我身上的99金币。还是以一千金币为抵住。
>   2我不应该选角色。我现在使用的是mod角色酒狐。所有应该直接让我操控他」

14:37：
> 「1，我现在使用的角色是mod角色卡芙卡。为什么调用的战士的立绘。人家有自己的立绘，
>   还有自己的攻击和出招动作特效。我要求以后即使加入别的mod角色也能直接应用他的立绘和动作。
>   2没有选人之后。头像不会正常显示。牌桌也没有正常开局」

---

## 二、★ 根因：**一个 bug 解释了三件事**

**`RestoreOrPick()` 的「篝火赌注 · 不弹选人」分支只做了 `ShowPicking(false); Refresh();`，
忘了调 `Deal()`。**

`_game` 于是**从没被创建** ⇒ 连锁出三个症状：

| 症状 | 为什么 |
|---|---|
| 牌桌没有正常开局 | `_game` 是 null，没有任何牌/阶段 |
| 头像不会正常显示 | HUD/立绘的刷新都依赖牌局状态 |
| **筹码还是 1000**（没读我身上的 99 金币） | `ResetMoney()` 是在 **`Deal → BeginShowCards`** 里调的（`:483`）。没开局 ⇒ 永远不执行 ⇒ 筹码停在字段初值 `{1000,1000,1000}`（`:159`） |

**证据**：所有"真正开局"的入口都是同一套 ——
选人界面「开始打牌」（`:2700`）和「再来一局」（`:1863`）都是 `DropRound(); Deal();`。
只有这条新分支漏了。

### 修法
```csharp
if (_stakePlayer is not null && _chars[MeSeat] is { IsEmpty: false })
{
    _picking = false;
    ShowPicking(false);
    Callable.From(() => { if (!GodotObject.IsInstanceValid(this)) return; DropRound(); Deal(); })
            .CallDeferred();   // 延后一帧：这条路径在 _Ready 里跑，等场景树搭完再开局更稳
    return;
}
```

---

## 三、另外改了两处（角色 / 赌注兜底）

### 3.1 我这一席 = **我 run 里的角色**（联机也成立）

`InitCharacters()` 原来是 **联机先 `return`**，于是我这一席的角色被
**主机广播的池子下标**覆盖 → 用户实测看到**铁甲战士**（他要的是卡芙卡）。

改成：**先用 `MyRunCharacter()` 定我这一席，再进联机分支**；联机时**只改我自己这一席**，
另外两家仍用主机广播的下标（保证各端座位表一致，发牌按座位号切、与名字无关 —— 见下）。

```csharp
var mine = (_stakePlayer is not null || _netOn) ? DouSpireCharacters.MyRunCharacter() : null;
if (_netOn) { _chars = BuildNetChars(); if (mine is not null) _chars[MeSeat] = new SeatPick(mine, null); ... }
if (mine is not null) { _chars = Assign(new SeatPick(mine, null), ...); ... }   // 单人篝火
```

> ✅ **安全性已核对**：`GameState.NewGame` 是 `deck.Skip(s * handSize)` **按座位号切牌**，
> `names` 只进 `PlayerState.Name`（显示用，`GameState.cs:83-91`）。
> 所以换我这一席的角色**不会让联机两端发牌不一致**。

> 💡 **好消息**：`DouSpireActor.Spawn(parent, CharacterModel, …)` 本来就是**直接实例化游戏本体那个
> 带 Spine 骨骼的角色**（`NCreatureVisuals.SpineBody` + `RunWhenSpineReady`），动作/音效都来自角色自带。
> ⇒ **"mod 角色用自己的立绘和动作"不需要新写美术系统**，只要把**正确的 CharacterModel**
> 传进去就自动成立。`DouSpireCharacters.MyRunCharacter()` 的注释也早就写明「mod 角色也算」。

### 3.2 赌注玩家兜底解析

`_stakePlayer = player` → `player ?? ResolveMyPlayer()`（篝火 + 联机两条路都改）。
新增私有方法：走 `RunManager.Instance?.DebugOnlyGetState()` → `LocalContext.GetMe(state)`，
和 `MyRunCharacter()` 同一套 API。
> 为什么需要：篝火的 `Owner` 在联机下**可能不是本机玩家**（广播会在每台机器上重放一遍 `OnSelect`），
> 客机也可能拿不到自己的 Player。读不到就是不下注（牌局照开），能读到就一定要读到 ——
> 否则「用我身上的金币当赌注」这条直接废掉。
> 顺带把 `OpenAtRestSite` 里 `player.Gold` 的日志改成空安全（原来 `player` 为 null 会抛）。

---

## 四、编译 / 部署核对

| | 值 |
|---|---|
| 源码 `DouSpireTable.cs` mtime | 2026-10-08 14:47:16 |
| 本地 dll | 2026-10-08 14:47:38 · 302,592 B |
| 部署 dll（`mods/DouSpire/`） | 2026-10-08 14:47:38 · 302,592 B |
| md5（本地 / 部署） | `fe716f61ef19` / `fe716f61ef19` ✅ 一致 |
| 编译结果 | **0 警告 0 错误** |
| 部署清单 | `min_version: 0.6.6` / `v3.4.7` ✅ |

---

## 五、实机验收清单（交给用户）

1. 在**篝火**点「斗地主」：
   - 还弹不弹选人界面？（应该**不弹**，直接进牌局）
   - 牌桌**正常开局**吗（有手牌、能明牌/抢地主）？
   - 我这一席是**卡芙卡**（我 run 里的角色）吗？她有没有自己的立绘 + 出招动作？
   - 我这一席的筹码是不是**我身上的真实金币**（不再是 1000）？两个 AI 仍是 1000（它们没有钱包）。
2. 玩一把、结算，看金币有没有真的增减。
3. 顺手按 F9 / 主菜单开桌，确认**老玩法没被搞坏**（那两条路 `_stakePlayer` 为 null，走抽签/存局）。

### 看日志能确认的三行
```
斗地主（篝火）：我 = 卡芙卡；三家 = …
斗地主：篝火赌注开局 —— 我 99 金币（一份 10），对家各 1000 / 1000。
斗地主：篝火金币结算 99 -10 → 89（倍数 ×1）
```

---

## 六、⚠️ 我发现但**没改**的（留给下一手 / 需要你拍板）

1. **「取小」策略会把声明下限越压越低**（`SyncManifestDependencies`）。
   现在是 `min_version = min(manifest 现值, 实装值)`，"只许降不许抬"。
   风险：换一台**装旧 RitsuLib** 的机器构建一次，下限就被悄悄压低 ⇒ 又回到
   "版本校验能过、运行时 `MissingMethodException`"。
   建议：**只在实装值更小时才降，并且降的时候打 `Warning`**（现在这条路径没有告警）。
2. **联机模式下金币结算可能不同步**：`SettleGold` 直接改本机的 `player.Gold`，
   各端只结自己那一席。单人/单机测试无感，真联机下主机与客机的钱包可能对不上。
3. **`1a′`（`min_game_version` 重议）仍未做** —— 需要先核 `sts2.xml` 的 API 下限，不能拍脑袋。
4. 低优先：`7` BaseLib 改 `PackageReference`、`8` 删死字段 `pck_name`。

---

## 七、边界

- 我**只改了** `projects/dou-spire/DouSpireCode/Table/DouSpireTable.cs`。
  没碰 `DouSpireNet.cs` / `DouSpireCharacters.cs` / `DouSpireActor.cs` / `DouSpireRestSite.cs`。
- **没有**改 manifest / csproj（那是国际台 14:15 做完的，我复核过，是对的，没回滚）。
- **没有**启动游戏。
- `projects/dou-spire/` 在 notes 仓库里是**未跟踪**的（mod 源码目前零版本控制，见
  `notes/斗地主尖塔-交接-20260808` 系列与工作区记忆）—— 所以这次改动**没有 git 提交**，
  只落在磁盘上。

---
*发件：国内台 wb · `C:\Users\有花无实\WorkBuddy\2026-10-08-00-26-18`*
