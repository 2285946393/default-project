# 斗地主尖塔 · 联机同坐一桌（已落地 · wb 2026-10-08 凌晨）

> 用户原话（把之前"做不到"的结论直接推翻了）：
> 「不是。**你得做多人。没人陪我测试但是你不能不写这个代码**。游戏里面多人可以给对方包扎不就和打牌差不多。
>   斗地主是选多人再走联机可能不太好。**而篝火多人代表玩家你已经在联机了**。
>   **你只需要如何让主机客机坐在一起打牌**」
>
> **一句话**：不造房间、不开官方联机界面 —— **复用 run 里那条已经在跑的连接**。
> 篝火是共享节点，大家本来就在同一个房间里，所以在篝火点「斗地主」天然就是"把已经在联机的人拉到一起"。

> ⚠️ **全部未实机验证**（用户定的规矩：测试他自己来，而且"没人陪我测试"）。
> 编译 **0 错 0 警**，dll 已部署到 `mods\DouSpire\DouSpire.dll`。

---

## 一、核心决策（为什么这么设计）

| 问题 | 决定 | 理由 |
|---|---|---|
| 走哪条网络通道 | **RitsuLib Sidecar 总线** | 借游戏自己的 `NetHost/NetClientGameService` 传原始字节，走独立频道，不和原版消息序列化打架。**这条选型已被官方文档证实**（见第十节） |
| 谁当主机 | **run 的主机** | 客机只能 `SendToHost`，只有主机能广播 —— 想同步就得主机当权威 |
| 谁开桌 | 谁点谁请求，**主机实际开** | 客机点「斗地主」→ 发 `InOpenReq` → 主机开桌并广播座位表 |
| 两端怎么看到同一副牌 | **同一个种子 + 全局名字顺序**，再各自旋转 | `GameState.NewGame` 是纯确定性的（`Random(seed)` + Fisher-Yates + 按座位切 17 张） |
| 视角怎么统一 | **本地视角永远把"我"放 0 号位** | 牌桌上其余代码一行不改；换算只在网络边界做 |
| 谁推进牌局 | **只有主机** | 客机的 `_Process` 在动画推进之后直接 `return` |

---

## 二、视角旋转（这是整件事的地基）

`GameState.NewGame(seed, names, landlordSeat, deckCopies)` **按座位号切牌**：
`deck.Skip(seat * 17).Take(17)`。所以：

- **名字顺序必须全局一致** —— 换顺序发牌结果就变了，两端牌就对不上。
- 于是新增 `GameState.ViewFrom(int seat)`：**纯排列旋转**，把全局牌局转成"我坐 0 号位"的本地视角。
  它只重排 `Players` 列表、重算 `Current` / `TrickHolder` / `Winner` 的下标，牌面一张不动。

```csharp
// DouSpireTable
private GameState BuildView(int seed, int globalLandlordSeat)
{
    var global = GameState.NewGame(seed, GlobalNames(), landlordSeat: globalLandlordSeat);
    return _netOn ? global.ViewFrom(DouSpireNet.MySeat) : global;
}
```

> 已确认全工程没有任何地方读 `PlayerState.Seat`（grep 空），所以 `ViewFrom` 里把它设成本地下标是安全的。

网络边界上只有两个换算：

```csharp
public static int ToGlobal(int localView) => ((MySeat + localView) % 3 + 3) % 3;
public static int ToLocal (int globalSeat) => ((globalSeat - MySeat) % 3 + 3) % 3;
```

**规矩：`DouSpireTable` 里的座位号一律是本地视角；`DouSpireNet` 里一律是全局。** 只在收/发消息那一行换。

---

## 三、协议（3 个 opcode）

```csharp
OpIn   = RitsuLibSidecarOpcodes.For("DouSpire", "table.in")    // 客机 → 主机
OpOpen = RitsuLibSidecarOpcodes.For("DouSpire", "table.open")  // 主机 → 所有人：开桌
OpAct  = RitsuLibSidecarOpcodes.For("DouSpire", "table.act")   // 主机 → 所有人：动作
```

投递语义一律 **`StableSync`**（可靠 + 有序）—— 牌局一个动作都不能丢、不能乱序。

### 3.1 客机 → 主机（`OpIn`）

| kind | 载荷 | 含义 |
|---|---|---|
| `InOpenReq` = 1 | `i32 我的房间id` | 我想开桌 |
| `InShow` = 2 | `u8 want` | 明牌表态 |
| `InWant` = 3 | `u8 want` | 抢地主意向 |
| `InThrow` = 4 | `u8 gesture` | 出拳（0石头/1剪刀/2布） |
| `InDouble` = 5 | `u8 want` | 加倍表态 |
| `InPlay` = 6 | `u8 n` + n×`(u8 rank, u8 suit)` | 出牌 |
| `InPass` = 7 | — | 要不起 |
| `InLeave` = 8 | — | 我离桌 |

### 3.2 主机 → 所有人（`OpOpen`）

`i32 Gen` + `i32 Seed` + `i32 RoomKey` + `u8 座位数` + 每席 `(u8 是真人, u64 netId, str 名字, i32 角色池下标)`

- `Gen` = 这一桌的"第几代"，每次主机重开 +1。客机靠它判断"新的一桌"还是"重复收到的老消息"。
- `RoomKey` 只做**一致性校验**（客机本地房间号 vs 主机广播的），不是主键。

### 3.3 主机 → 所有人（`OpAct`）

`ActShow`=1 / `ActWant`=2 / `ActThrow`=3 / `ActDouble`=4 / `ActStart`=5 / `ActPlay`=6 /
`ActPass`=7 / `ActNewRound`=8 / `ActErr`=9 / `ActSeatAi`=10 / `ActClose`=11 / **`ActRpsPhase`=12**

`ActRpsPhase`：`u8 阶段`（0 定格 / 1 平局重出 / 2 定完地主进加倍）+ `i32 地主全局座位`。

---

## 四、唯一落地点（保证两端永远同步）

**每一手牌，在所有机器上都只经过 `ApplyPlay` / `ApplyPass` 这两个函数。**

```
单人：按钮 / AI 直接调 Apply*
联机主机：收到客机上报 → 校验 → Apply* → 广播
联机客机：收到主机广播 → Apply*
```

四个"问话阶段"（明牌 / 抢地主 / 出拳 / 加倍）同理，各自只有一个 `ApplyShow` / `ApplyWant` /
`ApplyThrow` / `ApplyDouble`，而且**每个座位只认第一次**（`_showDone[seat]` 之类的挡着，幂等）。

### ⚠️ 自检抓到的两个真 bug（都已修）

**① 四个问话阶段缺"回声广播"。**
`NetPlay` / `NetPass` 在主机侧会回声广播，但 `NetShow` / `NetWant` / `NetThrow` / `NetDouble` 没有。
而客机这四个表态都是**只上报、不本地落地**（等主机确认），所以它自己那一席的
`_showDone` / `_wantDone` / `_throwDone` / `_doubleDone` 只能靠主机广播回来才置位 ——
不回播的话客机"三家都表态了吗"永远差自己一票，**永久卡在"等你对面表态…"**。

修法：主机收到客机表态后，**先回声广播、再本地落地**（顺序很重要，落地可能顺带触发下一阶段的广播）：

```csharp
internal void NetWant(int localSeat, bool want)
{
    if (!_netOn || !_bidding) return;
    if (_netHost) DouSpireNet.CastWant(DouSpireNet.ToGlobal(localSeat), want);   // ← 回声
    ApplyWant(localSeat, want);
}
```

**② 猜拳的定格 / 重出 / 进加倍不能各端自己数秒。**
原来两端都靠 `_rpsTimer` 自己推进。但客机的计时器**永远慢主机一个网络往返** ——
主机已经进了下一拍、把新的拳发出来了，客机还停在上一拍的定格里，
那几条消息会被 `ApplyThrow` 的 `if (_rpsThrow[seat] >= 0) return;` 当"重复"丢掉 →
**平局重出时必然死锁**（少一个人的拳，"等所有人的拳到齐"那道闸永远开不了）。

修法：**只有主机推进猜拳阶段**，客机照令走：

```csharp
// 客机：定格听主机的令
case RpsStep.Swing:  ... if (_netOn && !_netHost) return; ...
// 客机：下一阶段听主机的令
case RpsStep.Reveal: if (_netOn && !_netHost) return; ...
```

主机到点时 `DouSpireNet.CastRpsPhase(...)` 再自己走；客机在 `NetRpsPhase` 里照做。
（`ActStart` 早就是这个套路：主机发号施令，客机照着开。）

---

## 五、兜底（"绝不卡死"清单）

| 场景 | 兜底 |
|---|---|
| 对面没装 mod / 不在篝火 | 客机开桌请求 12 秒没人应答 → 自己收桌 + 弹提示 |
| 对面挂机不出牌 | 主机侧超时（`TurnTimeLimit + 10` 秒，默认 30 秒）→ **主机替他出**，状态栏显示倒计时 |
| 对面挂机不表态（明牌/抢/加倍） | 主机侧同样超时 → **替他选最保守的那个**（不明牌 / 不抢 / 不加倍），不替他翻倍输钱 |
| 对面挂机不出拳 | 客机自己 12 秒自动出一手（`RpsThrowSay`），走正常的"上报→回声"那条路 |
| 对面中途离桌 | 客机 `_ExitTree` 发 `InLeave` → 主机把那一席改成电脑 + 广播 `ActSeatAi` |
| 有人点了"继续"离开篝火 | `NetRoomGuard` 发现 `RoomKeyNow()` 变了 → 自动收桌 |
| 三家一直都不抢 | `_redealCount >= 6` → 按手牌分强行定地主，不无限换牌 |
| 猜拳定不出地主 | 主机换一副牌重发（`ActNewRound`） |
| 客机上报的牌不合法 | 主机 `CastErr` 打回，客机清掉选择 + 弹提示 |
| 主机收桌 | `CastClose` → 所有人一起收 |

---

## 六、诊断日志（"对面没反应"时唯一能自查的东西）

RitsuLib 的广播在"对端没就绪"时是**静默跳过**的 ——
`TryBroadcastToReadyPeers` 里 `continue`，而且**一条都没发出去也照样返回 `true`**。
所以光看返回值什么也判断不出来。开桌前会打一行对端状态：

```
斗地主（联机）：对端 7656119xxxxxxxx 准备好收广播=True RitsuLib 能发=True
```

三个组合的含义：

| 准备好收广播 | RitsuLib 能发 | 结论 |
|---|---|---|
| False | — | 对端还在加载房间（等一等） |
| True | False | **对面没装斗地主 mod**，或 RitsuLib 版本不匹配（握手没做完） |
| True | True | 通了，还没反应就是逻辑问题，看两边日志对比 |

主机侧 `ConnectedPeers` 为空时会直接 Warn 一行。日志在
`mods\DouSpire\voice-log.txt` 和 `%APPDATA%\SlayTheSpire2\logs\godot.log`。

---

## 七、改了哪些文件

| 文件 | 改了什么 |
|---|---|
| `Table/DouSpireNet.cs` | **整体重写**（原先是"打开官方多人界面"的壳）。三个 opcode、会话状态、座位表、二进制编解码、收发处理、对端诊断 |
| `Table/DouSpireTable.cs` | 加联机分支：`OpenForNet` / `BeginNetSession` / `BuildNetChars` / `GlobalNames` / `BuildView` / `IsHumanLocal` / `PlayLocked` / `ApplyPlay` / `ApplyPass` / `LockRpsButtons` / `NetRoomGuard` / `StepAi` 重写 / `#region 联机收信口`（`NetShow`…`NetClosed`）；四个问话阶段加"主机替 AI 决定并广播" |
| `Table/DouSpireCharacters.cs` | 加 `PoolIndex(CharacterModel?)` 和 `PickAt(int)` —— 让角色用**池子下标**跨端互认 |
| `Table/DouSpireRestSite.cs` | `OnSelect()` 改走 `DouSpireNet.OpenAtRestSite`（单人/多人自动分流）。仍然 `return false`（不限次数） |
| `Table/DouSpireEntry.cs` | 主菜单多人页改成"**在篝火开桌**"的说明 + 一个"打开官方多人界面（去组队）"按钮 |
| `Entry.cs` | `RegisterContentPack()` 之后加 `RegisterNetworking()` → `DouSpireNet.Register()` |
| `dou-spire-rules/Game/GameState.cs` | 加 `ViewFrom(int seat)` |

---

## 八、⚠️ 实机验收清单（游戏**必须重启**）

**单人回归（最重要 —— 联机代码绝不能碰到单人的路）**

| # | 看什么 | 期望 |
|---|---|---|
| 1 | 主菜单按 F9 / 点「斗地主尖塔」→ 单人 | 一切照旧，**不弹选人以外的任何东西** |
| 2 | 篝火点「斗地主」 | 单人桌（我 vs 两个 AI），金币赌注照旧 |
| 3 | 收开桌几次 | 不报错，日志里**不应出现**任何"联机"字样 |

**联机（两个人，都要装 mod）**

| # | 看什么 | 期望 |
|---|---|---|
| 4 | 组队进 run → 一起走到篝火 | 两人都在篝火里 |
| 5 | **随便谁**点「斗地主」 | 两边**同时**出现牌桌；座位名是各自选的角色名，0 号位（自己）显示"我" |
| 6 | 看日志有没有"对端 … 准备好收广播=True RitsuLib 能发=True" | 有就是通了；`RitsuLib 能发=False` 就是对面没装 mod |
| 7 | 明牌 → 抢地主 → 出拳 → 加倍 | 两边阶段**同步推进**；两边倍数一样 |
| 8 | 出牌 | 客机点出牌 → 牌从客机手里出去（要等主机确认，会有一下延迟）；**两边看到的牌局完全一样** |
| 9 | 平局重出（故意出同样的拳） | **不卡**，两边都进新一轮 |
| 10 | 一局结束 | 两边各自的**自己的金币**变了（各结各的，不是同一个人） |
| 11 | 客机关掉牌桌 | 主机那一席变成"电脑"，并且**广播给所有人** |
| 12 | 有人点"继续"离开篝火 | 另一边自动收桌 |

**已知最容易出问题的地方**（按风险排序）：

1. **牌桌盖不盖得住休息处界面** —— 牌桌 `ZIndex = 200`，但休息处那层在 Godot 里可能更高。
   单人已经验证过没问题；联机时客机的牌桌是"被主机叫出来的"，层级应该一样。
2. **`readyForBroadcasting` 的时机** —— 如果对端在房间切换的瞬间还没 ready，第一条广播会被静默丢掉。
   现在的兜底是：客机 12 秒超时自己收桌（重新点一次就行）。日志第 6 行能确认。
3. **RitsuLib 握手** —— `DouSpireNet.Register()` 里**第一句**就调
   `RitsuLibSidecarProtocol.EnsureDefaultHandlers()`（幂等、线程安全）。装晚了（等到第一次发送才装）
   会赶不上大厅阶段的握手，`CanSendToPeer` 一直 false → 消息全丢。

---

## 九、参考：反编译确认的 API 签名

```csharp
// STS2RitsuLib.Networking.Sidecar
RitsuLibSidecarBus.RegisterHandler(ulong opcode, Action<RitsuLibSidecarDispatchContext> handler)
RitsuLibSidecarOpcodes.For(string modId, string messageKind) -> ulong   // xxhash64(modId\0kind) | 0x10000
RitsuLibSidecarHighLevelSend.TrySendAsClient(RunManager?, ulong, ReadOnlySpan<byte>, DeliverySemantics, WireFlags, bool gzip, ReadOnlySpan<byte>)
RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(RunManager?, ulong, ReadOnlySpan<byte>, DeliverySemantics, ...)      // readyOnly: true
RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcastToAllConnected(...)                                                    // readyOnly: false
RitsuLibSidecarProtocol.EnsureDefaultHandlers()   // 幂等；装生命周期钩子 + 握手
RitsuLibSidecarSessionManager.CanSendToPeer(ulong peerId) -> bool
RitsuLibSidecarDeliverySemantics { BestEffort=0, StableSync=1, Unspecified=255 }
RitsuLibSidecarDispatchContext { SenderNetId, TransferMode, Channel, IsHostIngest, Envelope, Opcode, Payload, WithOwnedEnvelopeMemory() }

// MegaCrit.Sts2.Core.Multiplayer
NetGameType { Singleplayer, Host, Client }        // 注意：类型在 .Multiplayer.Game，服务类在 .Multiplayer
NetHostGameService  { Type => Host,   ConnectedPeers: List<NetClientData>, NetHost }
NetClientGameService{ Type => Client, NetId, HostNetId, NetClient }
NetSingleplayerGameService { Type => Singleplayer, NetId => 1 }
NetClientData { ulong peerId; bool readyForBroadcasting; PeerVersionInfo versionInfo; }

// MegaCrit.Sts2.Core.Entities.Players
Player.Gold { get; set; }        // setter 内部 GoldChanged?.Invoke() —— 顶部金币 HUD 会自动刷新
Player.NetId, Player.RunState (IRunState), Player.Character
MegaCrit.sts2.Core.Nodes.TopBar.NTopBarGold.Initialize(Player)   // 订阅 GoldChanged

// RunManager
RunManager.State  是 private → 用反射取（工程开了 Krafs.Publicizer，但 State 仍是 private 属性）
RunManager.NetService, RunManager.IsInProgress
```

编译：

```bash
cd "C:/Users/有花无实/Documents/Default Project/projects/dou-spire-rules"
export HTTP_PROXY=http://127.0.0.1:7892 HTTPS_PROXY=http://127.0.0.1:7892
bash tools/build-mod.sh ../dou-spire
```

游戏开着 → 拷 dll 会失败（文件被锁）。本次编译时游戏没开，**已自动部署**。

---

## 十、自查与工坊对标（2026-10-08 补充）

用户要求「自己试着反问自己，看看代码的批漏之处。或者工坊看看有没有类似的」。以下是结果。

### 10.1 用户新下的两个 mod 就是答案

| 工坊 ID | 名字 | 为什么重要 |
|---|---|---|
| **3747636338** | **Campfire Trading**（`STS2Trade`） | **同问题域的最直接对标**：「在多人篝火处让玩家之间交换卡牌/药水/遗物」。作者只依赖 BaseLib，没依赖 RitsuLib |
| **3813511203** | STS2 Agent ModDev Workspace | 17MB 的尖塔 mod 开发资料库，含 **RitsuLib 官方教程《14 - 网络与联机》** |

另外本地已装的 `STS2-MultiplayerLimitBreak`（OLC，RitsuLib 作者本人）是「最多 16 人同 run」的实现，`sts_nndx`（按 C 大笑）是最小 Sidecar 范例。

### 10.2 官方文档直接证实了选型（重要）

`RitsuLib/02 - 玩法基底/14 - 网络与联机/README.md` 的选型表：

> | 场景 | 用哪个 |
> | - | - |
> | 判断是否联机、我是谁、玩家数 | 直接读 `INetGameService` 属性（`Type`/`NetId`），不需要通信 |
> | 传任意数据给队友/主机（通知、UI、自定义配置） | **Sidecar** |
> | 跨端执行需要按动作队列同步、可回放的效果（卡牌、药水同类型） | **ManagedNetAction** |

我们的牌桌是「把 UI/状态同步给队友」→ **Sidecar 是对的**。（`ManagedNetAction` 是给"要塞进原版动作队列、可回放"的效果用的，我们不需要。）

而且文档的「皮肤同步」范例**和我们的模式一模一样**，包括最关键的那一步：

```csharp
// 主机收到客户端的消息后，转发广播给其他队友
if (context.IsHostIngest && context.Message.NetId != context.SenderNetId)
    RitsuLibSidecarTypedMessageRegistry.Broadcast(...);
```

→ **这正是我们那个「回声广播」修复**。所以那个修复不是拍脑袋，是官方范例要求的。

文档确认的两条语义（顺便修正本文档早期写反的地方）：

| 语义 | 传输模式 | 通道 |
|---|---|---|
| `BestEffort` | `NetTransferMode.Unreliable`(1) | **49** |
| `StableSync`（默认） | `NetTransferMode.Reliable`(2) | **48** |

（早期笔记写成「49 可靠 / 48 不可靠」，**是反的**。我们用的 `StableSync` = 可靠 + 通道 48，结论不变。）

载荷上限 **64KB**（我们最大的一条是 20 张牌的 `ActPlay`，几十字节，安全）。

### 10.3 ⚠️ P0：原版篝火在多人下会把 `OnSelect()` 调在**每一台机器**上

**这是本轮抓到的唯一一个会让联机直接坏掉的 bug，已修。**

反编译取证 `MegaCrit.Sts2.Core.Multiplayer.Game.RestSiteSynchronizer`：

```csharp
public Task<bool> ChooseLocalOption(int index)
{
    var message = new OptionIndexChosenMessage { type = OptionIndexType.RestSite, optionIndex = (uint)index, ... };
    _netService.SendMessage(message);          // ← 广播（ShouldBroadcast => true，反编译确认）
    return ChooseOption(LocalPlayer, index);   // ← 本地也走一遍
}

private void HandleRestSiteOptionChosenMessage(OptionIndexChosenMessage message, ulong senderId)
{
    Player player = _playerCollection.GetPlayer(senderId);
    TaskHelper.RunSafely(ChooseOption(player, (int)message.optionIndex));   // ← 每台机器都跑
}

private async Task<bool> ChooseOption(Player player, int optionIndex)
{
    RestSiteOption option = _restSites[playerSlotIndex].options[optionIndex];
    bool flag = await option.OnSelect();       // ← 关键：option.Owner 是"选择者"，不是"我"
    ...
}
```

⇒ **客机一点「斗地主」，主机也会为"客机那一项"跑一遍 `OnSelect()`，而 `Owner` 是客机。**

我原来的 `OpenAtRestSite` 只判断了 `LocalIsRunHost()`，于是主机会 `HostOpenTable(me = 客机的 Player)`：
把客机排成 0 号位、`MySeat = 0`、座位表里 0 号位是客机的名字、主机拿到的是**客机那 17 张牌**；
而客机也从 `HandleOpen` 里认到自己坐 0 号位 —— 两端 `MySeat` 都是 0，主机的名字根本不上桌。整桌全错。

**修法**（两处，都加了防御）：

```csharp
// 1) DouSpireNet.OpenAtRestSite —— 只认"我自己那一项"
if (mp && !LocalContext.IsMe(player)) { logger.Info("…别人的篝火选项（本机只是重放那条广播），不动作。"); return; }

// 2) DouSpireNet.HostOpenTable —— 万一把别人传进来，换成本机玩家（认不出来就原样继续，不引入新失败点）
if (!LocalContext.IsMe(me)) { var self = LocalMe(); if (self is not null && self.NetId != me.NetId) me = self; }
```

修完之后的正确链路：
- **主机点**：主机自己 `IsMe` 为真 → `HostOpenTable` 开桌 + 广播；客机收到那条 `OptionIndexChosenMessage` 时 `IsMe` 为假 → 什么都不做，只等 `ActOpen`。✅
- **客机点**：客机 `IsMe` 为真 → 亮占位桌 + 发 `InOpenReq`；主机收到请求 → `HostOnOpenReq` → `HostOpenTable(LocalMe())`。✅
  （主机那条"重放"分支因为 `IsMe` 为假直接返回，不会抢着开桌。）

### 10.4 顺带确认：`OnSelect()` 返回 `false` 是对的，不用打 `ShouldDisableRemainingRestSiteOptions` 补丁

Campfire Trading 打了这个补丁：

```csharp
[HarmonyPatch(typeof(Hook), "ShouldDisableRemainingRestSiteOptions")]
public static bool Prefix(IRunState runState, Player player, ref bool __result) { ... __result = false; return false; }
```

反编译 `RestSiteSynchronizer.ChooseOption` 看清楚为什么**我们不需要**：

```csharp
bool flag = await option.OnSelect();
...
if (!flag) return false;                        // ← 我们走这条：后面全部跳过
player.RunState...RestSiteChoices.Add(...);
if (Hook.ShouldDisableRemainingRestSiteOptions(player.RunState, player))
    restSite.options.Clear();                   // 清空所有剩余选项 → 篝火结束
else
    restSite.options.RemoveAt(optionIndex);     // 只移除用掉的那个
```

他们返回 `true`（一次交易真的消耗掉一次休息），所以必须补丁去阻止"清空其余选项"；
**我们返回 `false`，压根走不到那一行** —— 选项不移除、篝火不结束、其余选项也完好。这就是「不限次数」的全部机制。

### 10.5 ⚠️ 第二个 bug（已修）：客机中途离桌会让全桌死锁

`HostGotLeave` 原来只做了「把那一席改成 AI + 广播 `ActSeatAi`」。但如果此刻正卡在**等他表态**（明牌/抢/加倍）或**等他出拳**：

- `AllDone(_showDone/_wantDone/_doubleDone)` 和 `AllThrowDone()` 永远等不到他那一票；
- 而 `NetTickAskTimeout` 只盯**真人席**（`IsHumanLocal`），座位改成 AI 之后就不再盯它了；
- → **全桌永久卡死**。

修法：`DouSpireTable.NetSeatAi` 里补一句 —— 改完 AI 立刻重跑当前阶段的 `HostDecideAi*()`。
四个 `HostDecideAi*` 都是幂等的（内部只看 `IsHumanLocal` + `Apply*` 的 `*Done` 标志），重跑不会重复决定别人。

### 10.6 自查过的其他可疑点（结论：没问题，但值得记下来）

| 可疑点 | 结论 |
|---|---|
| 联机下用 `System.Random` 会不会两端不一致？ | **不会**。资料库《10-机制与时序取证》警告「联机别用 `System.Random`」，但我们的设计让**所有随机只在主机跑**（AI 意向/出拳/加倍、重发种子、AI 角色），结果一律 `Cast*` 广播；客机 `_Process` 在 `if (_netOn && !_netHost) return;` 就停了，从不自己跑 AI。**座位号、牌局、倍数因此天然一致** |
| `GameState.ViewFrom` 改了 `PlayerState.Seat` 会不会有副作用？ | 全工程 grep `\.Seat` **无引用**，安全 |
| `ViewFrom` 只拷了 `LastPlay/LastCards` 引用，要不要也旋转？ | 不用 —— 它们不含座位号（牌就是 `(Rank, Suit)`） |
| `_game.Players[s].Name` 在座位离桌转 AI 后还是旧名字？ | 是。`SeatNames()` 读的是 `DouSpireNet.NameOfSeat`（已变「电脑」），所以 HUD 显示正确；只有 `StatusText()` 那句"等着 X 出牌……"仍是旧名。**纯文字瑕疵，不影响逻辑** |
| `TrySendAsHostBroadcast` 返回 `true` 但一条都没发出去？ | 是（`num <= 0` 也 `return true`）。所以不能靠返回值判断，靠开桌前的对端诊断日志 |
| 收信会不会在后台线程（Godot 节点操作非法）？ | **我们走的这条不会**。Sidecar 的「路由端点」用的是 `RitsuLibSidecarEndpointDispatchMode.ReceiveThread`（反编译确认是**同步直接在接收线程调回调**），而 `RitsuLibSidecarTypedMessageRegistry` 会自动挂上那个端点；我们用的是它**下面一层**的 `RitsuLibSidecarBus.RegisterHandler` + `RitsuLibSidecarHighLevelSend`，收信走游戏自己的消息分发（主线程）。**代价是没有重发队列**，但 `StableSync` 本身已是可靠传输 |
| `min_game_version` 写 0.106.0 但游戏是 0.111.0？ | 无害（表示"0.106 及以上都能用"）。RitsuLib 的 compat 目录是 `compat/0.111.0`，对得上 |

### 10.7 还没做、但值得做的（按性价比排序）

1. **换成 `RitsuLibSidecarTypedMessageRegistry`（官方文档 API）**。收益：删掉 ~200 行手搓的 opcode + 二进制编解码，改用 `JsonSerializer` + 描述符，还能拿到 `context.IsHostIngest`（比我们自己判 `_netHost` 干净）和**路由端点的重发队列**。
   **风险**：路由端点用 `ReceiveThread`，收信回调会落在接收线程上 —— 而我们的回调要改 Godot 节点（`Refresh()`）。**要先实测确认这个线程问题**，或者只在"能保证主线程"的前提下迁移。**所以现在不动。**
2. 用游戏自己的 RNG（`RunManager` 上的随机上下文）替代 `System.Random` —— 目前不需要（见 10.6），但如果以后加"客机也要自己算随机"的功能就必须换。
3. 牌局加个**校验和/版本号**：每局开始时两端比对一次状态摘要，不一致就立刻提示"重开一桌"，而不是等到牌局歪掉才发现。

### 10.8 参考实现清单（照抄对象）

| 参考 | 位置 | 抄什么 |
|---|---|---|
| **Campfire Trading** | 工坊 `3747636338` | 篝火选项的多人交互流程（`PlayerChoiceSynchronizer.ReserveChoiceId` / `SyncLocalChoice` / `WaitForRemoteChoice`、`LocalContext.IsMe(Owner)` 分流、`NTargetManager` 选目标）；原生 `INetMessage` + `SendMessage<T>` 写法 |
| **RitsuLib 官方教程《14 - 网络与联机》** | 资料库 `01-官方与社区教程/.../RitsuLib/02 - 玩法基底/14 - 网络与联机/README.md` | Sidecar vs ManagedNetAction 的**选型表**；皮肤同步范例（= 主机转发模式） |
| **Multiplayer Limit Break** | 工坊 `3747606832` | 多人大厅协议、`RitsuNetMessageTailExtensions` 给原版消息加尾巴 |
| **sts_nndx** | 工坊 `3754231537` | 最小 Sidecar 范例（`RitsuLibSidecarTypedMessageRegistry` 的 `Register/Subscribe/Broadcast/SendToHost`） |
| 原版 `RestSiteSynchronizer` | `sts2.dll` | 篝火的多人流程（**必读**，10.3 那个 P0 就藏在这儿） |
