using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using DouSpireRules;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.Sidecar;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire.Table;

/// <summary>
/// 斗地主桌的**联机层** —— 让主机和客机坐在一起打同一副牌。
///
/// ── 用户的要求（2026-10-08 凌晨）──────────────────────────────
/// 「你得做多人。…游戏里面多人可以给对方包扎不就和打牌差不多。斗地主是选多人再走联机可能不太好。
///   而篝火多人代表玩家你已经在联机了。**你只需要如何让主机客机坐在一起打牌**」
///
/// 所以这一层**不自己造房间、不打开官方联机界面**（想组队的话，主菜单那个入口里有
/// 一个"打开官方多人界面（去组队）"的按钮，走的是游戏自己的 <c>NMultiplayerSubmenu</c>）
/// —— 它**复用 run 里那条已经在跑的连接**：
/// 篝火是共享节点，所有人本来就都在这个房间里，所以"在篝火开一桌"天然就是"把已经在联机的人拉到一起"。
///
/// ── 走哪条线 ─────────────────────────────────────────────────
/// 用 RitsuLib 的 **Sidecar 网络总线**（<see cref="RitsuLibSidecarHighLevelSend"/> /
/// <see cref="RitsuLibSidecarBus"/>）：它借游戏自己的 <c>NetHostGameService</c>/<c>NetClientGameService</c>
/// 传原始字节，走的是**独立频道**（<c>RecommendedChannel</c> 给了 48/49），
/// 不会和原版的消息序列化打架。反编译确认的签名：
/// <code>
/// RitsuLibSidecarBus.RegisterHandler(ulong opcode, Action&lt;RitsuLibSidecarDispatchContext&gt; handler)
/// RitsuLibSidecarOpcodes.For(string modId, string messageKind) -> ulong   // xxhash64(modId\0kind) | 0x10000
/// RitsuLibSidecarHighLevelSend.TrySendAsClient(RunManager?, ulong, ReadOnlySpan&lt;byte&gt;, DeliverySemantics, …)
/// RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(RunManager?, ulong, ReadOnlySpan&lt;byte&gt;, DeliverySemantics, …)
/// </code>
/// 投递语义用 <c>StableSync</c>（可靠 + 有序）—— 牌局一个动作都不能丢、不能乱序。
///
/// ── 谁说了算 ────────────────────────────────────────────────
/// **run 的主机就是这一桌的主机**（因为客机只能 <c>SendToHost</c>，主机才能广播）。
/// 主机是唯一的权威：它跑 AI、它校验每个人的出牌、它把结果广播给所有人。
/// 客机**不自己推进任何决策**，只做两件事：把玩家的操作发给主机、把主机广播的动作照做。
///
/// ── 视角 ────────────────────────────────────────────────────
/// 全局座位（本文件里的 <c>seat</c>）和本地视角（牌桌上的 0/1/2 号位）在**网络边界**上换算：
/// <see cref="ToLocal"/> / <see cref="ToGlobal"/>。牌桌上其余代码一行都不用改 ——
/// 本地视角永远把"我"放在 0 号位（<c>DouSpireTable.MeSeat</c>），
/// 而牌局本身用 <see cref="GameState.ViewFrom"/> 从全局牌局旋转出来。
///
/// ── 兜底 ────────────────────────────────────────────────────
/// 对面没装 mod / 掉线 / 挂机 → 主机侧有超时，超时就用 AI 替他出（<c>DouSpireTable</c> 里的
/// <c>NetTickRemoteTimeout</c>）。所以**永远不会卡死**。
/// </summary>
public static class DouSpireNet
{
    /// <summary>一桌几席 —— **联机固定 4 席**（斗地主 2v2 组队口径）。
    ///
    /// ★★ 2026-10-10：由 3 改成 4。用户原话：
    ///   「如果是多人联机每个[人]看到的应该是 本人 客机名字 客机名字 客机名字 这样的选人视角。
    ///     如果不足四人则往上多加电脑名」。
    ///   ⇒ 联机桌永远是 4 席：本人 + 在场真人（各按名字显示）+ **不足的用电脑补齐到 4**。
    ///   牌局本身走 4 人档（两副牌去 3/4/5 共 84 张、对家 2v2，见 `GameState.NewGame(playerCount:4)`），
    ///   和单人「四人桌（2v2）」是同一套规则 —— 只是这里是真人 + AI 补位。
    ///
    /// ⚠️ 这**只影响斗地主这条线**。德州扑克是**另一套**网络（<see cref="DouSpirePokerNet"/>，
    ///   它自己的 `MaxSeats = 4`，2~4 席可变），一行都不用动。</summary>
    public const int Seats = 4;

    /// <summary>网络 opcode 的命名空间 —— ★ 跟着 <see cref="Entry.ModId"/> 走，
    /// 这样本地开发版（DouSpireDev）和工坊版（DouSpire）用的是**两套互不串的频道**，
    /// 两边同时装着也不会互相收发错包。代价是开发版连不上工坊版玩家（预期）。</summary>
    private const string ModId = Entry.ModId;

    // ══════════════════════════════════════════════════════════════
    //  三个 opcode —— mod 自己的频道
    // ══════════════════════════════════════════════════════════════

    /// <summary>客机 → 主机：我要开桌 / 我的表态 / 我出牌。</summary>
    public static readonly ulong OpIn = RitsuLibSidecarOpcodes.For(ModId, "table.in");

    /// <summary>主机 → 所有人：开桌（座位表 + 牌种子）。</summary>
    public static readonly ulong OpOpen = RitsuLibSidecarOpcodes.For(ModId, "table.open");

    /// <summary>主机 → 所有人：牌局动作（明牌/抢/出拳/加倍/开局/出牌/要不起/收桌…）。</summary>
    public static readonly ulong OpAct = RitsuLibSidecarOpcodes.For(ModId, "table.act");

    // ── 客机 → 主机 的消息种类 ──
    public const byte InOpenReq = 1;
    public const byte InShow = 2;
    public const byte InWant = 3;
    public const byte InThrow = 4;
    public const byte InDouble = 5;
    public const byte InPlay = 6;
    public const byte InPass = 7;
    public const byte InLeave = 8;

    /// <summary>
    /// 客机 → 主机：「我想开新一局」（点了「再来一局」/「开始打牌」）。
    ///
    /// ★★ 2026-10-09 加：以前联机时点「再来一局」是**各端自己 `Deal()`** ——
    /// `_dealSeed = Random.Shared.Next()` 每台机器各随机一次，而且这条链路**一个包都不发**，
    /// 于是两端发到的根本不是同一副牌 ⇒ 一开局就分叉
    /// （用户实测：第二局立刻"主机广播的出牌没落地（手里没有 3）"，接着"还没轮到这一家"）。
    /// 现在改成**主机定种子并广播**，客机只负责请求。
    /// </summary>
    public const byte InNewRound = 9;

    /// <summary>客机 → 主机：「我要重选角色，请大家一起弹选人界面」。</summary>
    public const byte InPickOpen = 10;

    /// <summary>客机 → 主机：「我这一席换成了池子下标 N」（主机校验发信人 = 那一席后广播）。</summary>
    public const byte InPickSet = 11;

    /// <summary>
    /// 客机 → 主机：「我选好了，点「开始打牌」了」。
    ///
    /// ★ 2026-10-09 加：联机选角必须**各选各的、都点了才开始**。原来谁点谁直接开局，
    /// 队友还没翻完箭头就被拽进牌局（用户要求"每个人可以自己选择自己要玩的角色"）。
    /// 主机收齐**所有真人席**的准备票才定种子开局。
    /// </summary>
    public const byte InPickReady = 12;

    /// <summary>客机 → 主机：2v2 扣牌阶段「我扣这一张」（内容 = 一张牌）。
    /// ★ 2026-10-10 加：4 人 2v2 开局后手队每家要扣一张底牌，这条流程以前**只在单人跑**；
    /// 联机扩到 4 席后必须也走主机权威 —— 客机上报、主机校验 + 广播。</summary>
    public const byte InDiscard = 13;

    /// <summary>
    /// 客机 → 主机：「把**电脑那一席**换成池子下标 N」（内容 = 全局座位 + 下标）。
    ///
    /// ★ 2026-10-10 加（用户："2 虽然明确只能选自己那一行，但是如果是电脑我们也应该可以动
    /// 电脑那一行选角才行。不然不好玩。"）。
    ///
    /// 和 <see cref="InPickSet"/> 的区别：那条**只能改自己那一席**（一切以发信人的 netId 为准）；
    /// 这条明确带一个目标座位，主机**必须校验那一席确实是电脑**才认 —— 真人的角色只能他自己挑。
    /// </summary>
    public const byte InPickSetAi = 14;

    /// <summary>
    /// 客机 → 主机：**心跳**（空包，主机收到什么都不做）。
    ///
    /// ★ 2026-10-10 加（修"断线了客机不知道，一直等一个不会来的广播"）：
    /// 客机的 `_game` 只能靠主机广播推进，而"等主机出牌"的那段时间客机**一个包都不发** ——
    /// 于是 `SendIn` 的失败计数永远是 0，断线哨兵（`DouSpireTable.NetLinkGuard`）
    /// 的软信号根本没机会触发，玩家只能干等到天荒地老。
    /// 有了它，客机每 10 秒探一次"主机那头还收不收得到我"，断线最多 30 秒内被发现。
    /// 主机**不需要回包** —— `TrySendAsClient` 返回 false 本身就是"送不到"的判据。
    /// </summary>
    public const byte InPing = 15;

    // ── 主机 → 所有人 的动作种类 ──
    public const byte ActShow = 1;
    public const byte ActWant = 2;
    public const byte ActThrow = 3;
    public const byte ActDouble = 4;
    public const byte ActStart = 5;
    public const byte ActPlay = 6;
    public const byte ActPass = 7;
    public const byte ActNewRound = 8;
    public const byte ActErr = 9;
    public const byte ActSeatAi = 10;
    public const byte ActClose = 11;

    /// <summary>主机下令推进猜拳阶段（0 定格 / 1 平局重出 / 2 定完地主进加倍）。</summary>
    public const byte ActRpsPhase = 12;

    /// <summary>主机 → 所有人：「大家一起弹选人界面」（谁点了「重新选人」都行，主机代发）。</summary>
    public const byte ActPickOpen = 13;

    /// <summary>主机 → 所有人：「某个座位的角色换成了池子下标 N」。</summary>
    public const byte ActPickSet = 14;

    /// <summary>主机 → 所有人：「某个座位点「开始打牌」了」（其他人看到"XX 已准备"）。</summary>
    public const byte ActPickReady = 15;

    /// <summary>主机 → 所有人：2v2 扣牌阶段「某座位扣了某张牌」（内容 = 座位 + 一张牌）。
    /// 全桌照做 → 两队的暗扣牌在所有人屏幕上一致（★ 2026-10-10，配合 <see cref="InDiscard"/>）。</summary>
    public const byte ActDiscard = 16;

    // ══════════════════════════════════════════════════════════════
    //  会话状态（全局只有一桌，所以是静态的）
    // ══════════════════════════════════════════════════════════════

    /// <summary>这一桌跑在联机里吗？（篝火 + 多人房才为 true）</summary>
    public static bool Active { get; private set; }

    /// <summary>我是不是这一桌的主机（= run 的主机）。</summary>
    public static bool IsHost { get; private set; }

    /// <summary>我的全局座位号。</summary>
    public static int MySeat { get; private set; }

    /// <summary>这一局的牌种子（所有人都一样）。</summary>
    public static int Seed { get; private set; }

    /// <summary>开桌时所在的房间 Id —— 换了房间就自动收桌（不然会一直挡着屏幕）。</summary>
    public static int RoomKey { get; private set; } = -1;

    /// <summary>
    /// 这一桌的"第几代"。每次主机重新开桌 +1，跟着开桌消息一起发出去。
    /// 客机靠它判断"这是新的一桌"还是"重复收到的老消息"—— 没有它，
    /// 关掉再重开时客机会因为"已经初始化过"而不重新摆牌。
    /// </summary>
    public static int Gen { get; private set; }

    /// <summary>
    /// 客机已经请求开桌、还在等主机应答。
    /// 等的时候牌桌是开着的（显示"正在等主机开桌…"），12 秒没人应答就自己收桌。
    /// </summary>
    public static bool OpenPending { get; set; }

    private static readonly bool[] SeatHuman = new bool[Seats];
    private static readonly ulong[] SeatNet = new ulong[Seats];
    private static readonly string[] SeatName = new string[Seats];
    private static readonly int[] SeatChar = new int[Seats];     // 角色池下标，-1 = 由各端自己挑

    /// <summary>
    /// 联机选角：这一席点了「开始打牌」没有（**只有主机读它**）。
    /// 主机收齐**所有真人席**才定种子开局 —— 不然先点的人会把还在挑角色的队友拽进牌局。
    /// </summary>
    private static readonly bool[] SeatReady = new bool[Seats];

    /// <summary>当前开着的牌桌（收桌时置空）。</summary>
    internal static DouSpireTable? Live;

    // ══════════════════════════════════════════════════════════════
    //  连接健康度（★ 2026-10-10 加，修"断线后无限刷屏"）
    //
    //  症状（用户实测日志）：主机那头断了，客机这边 `TrySendAsClient` 一直返回 false，
    //  `SendIn` 每次打一行 "发给主机的消息没送出去"，`TickAutoPilotOnline` 每 12 秒
    //  重报一次 —— **永远不放弃**。日志刷了 3000 多行，玩家只看到牌桌"假活"着。
    //
    //  正常行为应该是：报几次 → 认输 → 收桌 + 弹提示。
    //  下面这几个计数器就是给"报几次"提供判据的。
    // ══════════════════════════════════════════════════════════════

    /// <summary>连着几次"发给主机的消息没送出去"（成功一次 / 收到任何一条消息就清零）。</summary>
    public static int SendFailStreak { get; private set; }

    /// <summary>这一串失败是从什么时候开始的（秒）—— 用来把"玩家手快连点三下"和"真断线"分开。</summary>
    public static double SendFailSince { get; private set; }

    /// <summary>连着几次发不出去才算断线。</summary>
    public const int LinkFailLimit = 3;

    /// <summary>光"连着 3 次"还不够 —— 还得**持续**这么久（秒）。
    /// 不然玩家在 0.2 秒里手抖点三下「出牌」就会被判成断线（那是冤案）。</summary>
    public const double LinkFailSpanSeconds = 8.0;

    /// <summary>
    /// 客机：和主机的线是不是**已经断了**。
    /// 判据 = 连着 ≥3 次发不出去 **且** 这一串失败已经持续 ≥8 秒。
    /// （主机没有这个状态 —— 主机发不出去只意味着某个客机掉线，牌局照常。）
    /// </summary>
    public static bool LinkLost
        => Active && !IsHost
           && SendFailStreak >= LinkFailLimit
           && NowSeconds() - SendFailSince >= LinkFailSpanSeconds;

    private static double NowSeconds() => Environment.TickCount64 / 1000.0;

    /// <summary>记一次"发出去了"。</summary>
    internal static void NoteSendOk()
    {
        if (SendFailStreak != 0) SendFailStreak = 0;
    }

    /// <summary>
    /// 记一次"没发出去"。
    ///
    /// ⚠️ **只在桌子已经开着（<see cref="Active"/>）时才算** —— 开桌那一下的
    /// <c>InOpenReq</c> 本来就允许发不出去（RitsuLib 的握手可能还没做完，见
    /// <see cref="Register"/> 里的注释），拿它当"断线"会误判。
    /// </summary>
    internal static void NoteSendFail()
    {
        if (!Active) return;
        if (SendFailStreak == 0) SendFailSince = NowSeconds();
        if (SendFailStreak < 1000) SendFailStreak++;   // 封顶，免得刷爆
    }

    /// <summary>收到对面任何一条消息 = 线还活着，计数清零。</summary>
    internal static void NoteInbound()
    {
        if (SendFailStreak != 0) SendFailStreak = 0;
    }

    // ══════════════════════════════════════════════════════════════
    //  对外查询
    // ══════════════════════════════════════════════════════════════

    /// <summary>这一席是不是真人（false = AI 补位）。</summary>
    public static bool IsHumanSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < Seats && SeatHuman[globalSeat];

    /// <summary>这一席的 netId（AI 席为 0）。</summary>
    public static ulong NetIdOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < Seats ? SeatNet[globalSeat] : 0UL;

    /// <summary>这一席显示用的名字（全局视角）。</summary>
    public static string NameOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < Seats ? SeatName[globalSeat] : "？";

    /// <summary>这一席预定的角色池下标（-1 = 没定，各端自己挑）。</summary>
    public static int CharIndexOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < Seats ? SeatChar[globalSeat] : -1;

    /// <summary>
    /// 这一席的**玩家对象**（只有主机那边有值 —— 开桌时留下的）。
    /// 用途：结算时主机把每个人的输赢写回他**自己的真实金币**，
    /// 然后靠游戏自己的玩家状态同步发到各端。AI 席 / 客机上返回 null。
    /// </summary>
    public static Player? PlayerOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < Seats ? SeatPlayerObj[globalSeat] : null;

    /// <summary>各席的玩家对象（见 <see cref="PlayerOfSeat"/>）。</summary>
    private static readonly Player?[] SeatPlayerObj = new Player?[Seats];

    /// <summary>本地视角的第 <paramref name="localView"/> 号位 → 全局座位号。</summary>
    public static int ToGlobal(int localView) => (((MySeat + localView) % Seats) + Seats) % Seats;

    /// <summary>全局座位号 → 本地视角的第几号位。</summary>
    public static int ToLocal(int globalSeat) => (((globalSeat - MySeat) % Seats) + Seats) % Seats;

    /// <summary>本地视角下"我"的那一席的全局座位号。</summary>
    public static int MyGlobalSeat => MySeat;

    /// <summary>
    /// 现在是不是**真的联机 run**（不是单人、也不是"假多人"）。
    /// 判定依据是 <c>NetService.Type</c> —— 反编译确认：单人 = <c>NetGameType.Singleplayer</c>，
    /// 主机 = <c>Host</c>，客机 = <c>Client</c>。
    /// </summary>
    public static bool InMultiplayerRun()
    {
        try
        {
            var rm = RunManager.Instance;
            if (rm is null || !rm.IsInProgress) return false;
            var t = rm.NetService?.Type;
            return t == NetGameType.Host || t == NetGameType.Client;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：判断是否联机时出错（按单人处理）: {e.Message}");
            return false;
        }
    }

    /// <summary>run 的主机是不是我。</summary>
    public static bool LocalIsRunHost()
    {
        try { return RunManager.Instance?.NetService?.Type == NetGameType.Host; }
        catch { return false; }
    }

    // ══════════════════════════════════════════════════════════════
    //  注册（Entry.Initialize 调一次）
    // ══════════════════════════════════════════════════════════════

    private static bool _registered;

    /// <summary>把三个 opcode 的处理器挂到 Sidecar 总线上。幂等。</summary>
    public static void Register()
    {
        if (_registered) return;
        try
        {
            // ⚠️ **必须先叫这一句**：它才会把 RitsuLib 自己的网络生命周期钩子（握手、
            // 会话绑定、就绪标记）装上去。装晚了（等到第一次发送才装）就赶不上
            // 大厅阶段的握手，`CanSendToPeer` 会一直返回 false —— 消息全部静默丢弃。
            // 它自己保证可重复调用，所以这里早叫、后面发消息时再叫都没问题。
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();

            RitsuLibSidecarBus.RegisterHandler(OpIn, HandleIn);
            RitsuLibSidecarBus.RegisterHandler(OpOpen, HandleOpen);
            RitsuLibSidecarBus.RegisterHandler(OpAct, HandleAct);
            _registered = true;
            Entry.Logger.Info("斗地主：联机频道已挂上（客机→主机 / 开桌 / 动作 三条）。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：挂联机频道失败（只能单人玩）: {e.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  开桌 / 收桌
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// **在篝火点「斗地主」** —— 单人就直接开桌，多人就联机开桌。
    /// 由 <see cref="DouSpireRestSiteOption.OnSelect"/> 调。
    ///
    /// ⚠️⚠️ **这个函数在多人下会被每一台机器调用，不只是"点的那个人"那台**。
    /// 原版 <c>RestSiteSynchronizer.ChooseLocalOption(index)</c> 会
    /// <c>_netService.SendMessage(new OptionIndexChosenMessage{...})</c> 把"谁选了第几项"广播出去
    /// （该消息 <c>ShouldBroadcast =&gt; true</c>，反编译确认），
    /// 所有机器的 <c>HandleRestSiteOptionChosenMessage</c> 都会为**那个玩家**跑一遍
    /// <c>ChooseOption(player, index)</c> → 于是 <c>OnSelect()</c> 在每台机器上都被调一次，
    /// 而且 <c>Owner</c> 是**选择者**、不是"我"。
    ///
    /// 所以这里必须先认"这是我自己的那一项"（2026-10-08 自查抓到的 P0）：
    /// 少了这一句，客机一点「斗地主」，**主机会拿客机的 Player 当自己开桌**
    /// —— <c>HostOpenTable(me = 客机)</c> 会把客机排成 0 号位，
    /// 主机于是"我坐 0 号位"却坐着客机的名字、发到客机那 17 张牌；两端 <c>MySeat</c> 都是 0，
    /// 主机的名字根本不出现在桌上。整个座位表全错。
    /// </summary>
    public static void OpenAtRestSite(Player player, Logger logger)
    {
        var mp = InMultiplayerRun();

        // 单人：<c>IsMe</c> 判断只在多人下有意义，单人一律照旧走（不引入回归）
        if (mp && !IsMyPlayer(player))
        {
            logger.Info("斗地主：这条是别人的篝火选项（本机只是重放那条广播），不动作。");
            return;
        }

        if (!mp)
        {
            DouSpireTable.OpenAtRestSite(player, logger);
            return;
        }

        if (Active)
        {
            // 已经有一桌在跑了 —— 直接把自己的桌子重新显示出来（比如刚才手滑收起了）
            logger.Info("斗地主：联机桌已经在跑，直接重新显示。");
            DouSpireTable.OpenForNet(player, logger);
            return;
        }

        if (LocalIsRunHost())
        {
            // ★ 用户 2026-10-08 的规矩：「**三个人或四个人都点了斗地主那个图标才能开始斗地主**」。
            //   所以主机点了自己这张也不立刻开 —— 先**收集**大家的开桌意愿，
            //   等齐了（或超时兜底）才真正开桌。就我一个人的房，点了直接开。
            var expected = ExpectedClickers(player);
            if (expected <= 1)
            {
                HostOpenTable(player, logger);
                return;
            }
            StartGather(player, logger, mineClicked: true);
            return;
        }

        // 客机：**先把自己这桌亮出来**（显示"正在等主机开桌…"），再请求主机开。
        // 不先亮桌的话，对面没装 mod / 不在篝火时玩家完全看不出发生了什么。
        logger.Info("斗地主：我是客机，向主机请求开桌。");
        LogPeerState();
        OpenPending = true;
        DouSpireTable.OpenForNet(player, logger);
        SendIn(new W().U8(InOpenReq).I32(RoomKeyNow()).Done());
    }

    /// <summary>
    /// 开桌前把"对面到底连上没有 / RitsuLib 的握手做完了没有"写进日志。
    ///
    /// 为什么必须有：RitsuLib 的广播在"对端没就绪"时是**静默跳过**的
    ///（`TryBroadcastToReadyPeers` 里 `continue`，而且没发出去也照样返回 true）。
    /// 没有这行日志的话，玩家只会看到"对面没反应"，完全不知道是没连上、没装 mod、
    /// 还是 RitsuLib 握手没做完。有了它，看一眼日志就能定位。
    /// </summary>
    private static void LogPeerState()
    {
        try
        {
            if (RunManager.Instance?.NetService is NetHostGameService host)
            {
                var peers = host.ConnectedPeers;
                if (peers is null || peers.Count == 0)
                {
                    Entry.Logger.Warn("斗地主（联机）：主机侧一个客户端都没连上 —— 对面收不到牌桌。");
                    return;
                }
                foreach (var p in peers)
                    Entry.Logger.Info($"斗地主（联机）：对端 {p.peerId}"
                                      + $" 准备好收广播={p.readyForBroadcasting}"
                                      + $" RitsuLib 能发={RitsuLibSidecarSessionManager.CanSendToPeer(p.peerId)}");
            }
            else if (RunManager.Instance?.NetService is NetClientGameService client)
            {
                // ⚠️ 客机这里 NetService.NetId 是**我自己**的 id，主机的 id 在 HostNetId 上
                var hostId = client.HostNetId;
                Entry.Logger.Info($"斗地主（联机）：我是客机（我的 id {client.NetId}），主机 {hostId}"
                                  + $" RitsuLib 能发={RitsuLibSidecarSessionManager.CanSendToPeer(hostId)}");
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：查对端状态出错（不影响开桌）: {e.Message}");
        }
    }

    /// <summary>
    /// **主机开桌**：定座位表（主机 = 0 号位，其余按 netId 从小到大），挑牌种子，广播出去。
    ///
    /// ⚠️ 顺序很重要：**先广播开桌消息，再把自己的桌子摆起来**。
    /// 反过来的话，主机摆牌时广播的 AI 决定会**跑在开桌消息前面**，客机收到时还没有会话，
    /// 那几条消息就被丢掉了 —— 客机就会永远卡在"等 AI 表态"上。
    /// </summary>
    public static void HostOpenTable(Player me, Logger logger)
    {
        try
        {
            // 防御：万一把"别人的 Player"传进来了（比如原版那条"别人选了这一项"的广播重放），
            // 一律换成本机玩家 —— 否则座位表会把别人排成 0 号位（见 OpenAtRestSite 的注释）。
            // 只在**确实认出了本机玩家且不是同一个人**时才换；认不出来就原样继续（不引入新失败点）。
            if (!MegaCrit.Sts2.Core.Context.LocalContext.IsMe(me))
            {
                var self = LocalMe();
                if (self is not null && self.NetId != me.NetId)
                {
                    logger.Warn("斗地主：开桌时传进来的不是我（像是别人的篝火选项重放），改用本机玩家。");
                    me = self;
                }
            }

            var players = me.RunState.Players;
            if (players is null || players.Count == 0)
            {
                logger.Warn("斗地主：拿不到 run 里的玩家列表，退回单人开桌。");
                OpenPending = false;
                DouSpireTable.OpenAtRestSite(me, logger);
                return;
            }

            // 主机自己排第一，其余按 netId 从小到大（所有端算出来都一样）
            var ordered = new List<Player>(players);
            ordered.Sort((a, b) => a.NetId.CompareTo(b.NetId));
            var myIdx = ordered.FindIndex(p => p.NetId == me.NetId);
            if (myIdx > 0)
            {
                (ordered[0], ordered[myIdx]) = (ordered[myIdx], ordered[0]);
            }

            var seatCount = Math.Min(Seats, ordered.Count);
            var seed = System.Random.Shared.Next();
            var room = RoomKeyNow();

            for (var s = 0; s < Seats; s++)
            {
                if (s < seatCount)
                {
                    var p = ordered[s];
                    SeatHuman[s] = true;
                    SeatNet[s] = p.NetId;
                    SeatName[s] = PlayerName(p);
                    SeatChar[s] = DouSpireCharacters.PoolIndex(p.Character);
                    SeatPlayerObj[s] = p;          // 主机留着：结算金币时要把输赢写回每个人的真实金币
                }
                else
                {
                    // 人不够 → AI 补位（2 人房就是 2 真人 + 1 电脑）
                    SeatHuman[s] = false;
                    SeatNet[s] = 0;
                    SeatName[s] = "电脑";
                    SeatChar[s] = -1;
                    SeatPlayerObj[s] = null;
                }
            }
            // AI 席的角色：主机定，广播出去（保证三家立绘在所有人屏幕上一致）
            var pool = DouSpireCharacters.Pool;
            if (pool.Count > 0)
                for (var s = 0; s < Seats; s++)
                    if (!SeatHuman[s]) SeatChar[s] = System.Random.Shared.Next(pool.Count);

            MySeat = 0;
            IsHost = true;
            Seed = seed;
            RoomKey = room;
            Gen++;
            OpenPending = false;
            Active = true;

            if (ordered.Count > Seats)
                logger.Warn($"斗地主：这局有 {ordered.Count} 个人，一桌只坐得下 {Seats} 个，多出来的看不了牌。");

            logger.Info("斗地主：主机开桌 —— "
                        + string.Join("、", Enumerable.Range(0, Seats)
                            .Select(s => $"{s}:{SeatName[s]}{(SeatHuman[s] ? "" : "(AI)")}"))
                        + $"，种子 {seed}，第 {Gen} 代。");

            LogPeerState();
            CastOpen();
            DouSpireTable.OpenForNet(me, logger);
        }
        catch (Exception e)
        {
            logger.Warn($"斗地主：主机开桌失败，退回单人: {e}");
            Active = false;
            OpenPending = false;
            DouSpireTable.OpenAtRestSite(me, logger);
        }
    }

    /// <summary>
    /// 主机收到"有人想在篝火开桌"。
    /// **即使当前没开着桌也要处理** —— 这正是开桌的入口。
    ///
    /// ★ 2026-10-08 改：现在是**收集阶段**的一份子（见 <see cref="StartGather"/>），
    ///   不再"来一个就立刻开"。人齐了才开；超时了也开（没点的由电脑补）。
    /// </summary>
    private static void HostOnOpenReq(ulong senderNetId, int theirRoom)
    {
        if (!LocalIsRunHost()) return;

        // ★★ 2026-10-10 改（用户实测："我们俩点了斗地主…有人没进去"）：
        //   原来这里是 `if (Active) return;` —— 桌一开起来，**离桌的人就再也回不来了**。
        //   他重进篝火再点「斗地主」，请求打到主机这边被静默丢掉，只能干等。
        //   现在：开着了也看一眼 —— 他不在桌上就给他补一席；他本来就在，就重发一次座位表。
        if (Active)
        {
            HostAdmitLatecomer(senderNetId, theirRoom);
            return;
        }

        if (OpenPending) return;                  // 自己也在等（理论上不会）

        var mine = RoomKeyNow();
        if (theirRoom >= 0 && mine >= 0 && theirRoom != mine)
        {
            Entry.Logger.Warn($"斗地主：{senderNetId} 在别的房间（{theirRoom}）请求开桌，"
                              + $"我现在在 {mine} —— 不算数。");
            return;
        }

        // 收集阶段：记下"这个人也点了"，人齐就开
        if (_gathering)
        {
            GatherNote(senderNetId);
            return;
        }

        var me = LocalMe();
        if (me is null)
        {
            Entry.Logger.Warn("斗地主：收到开桌请求，但拿不到自己的玩家对象，开不了。");
            return;
        }

        // 还没收集（我是被第一个点的人）：先开始收集
        if (ExpectedClickers(me) <= 1)
        {
            Entry.Logger.Info($"斗地主：{senderNetId} 请求开桌，主机应允。");
            HostOpenTable(me, Entry.Logger);
            return;
        }
        // ★ 2026-10-11 修：客机先点时**不能白送主机一票**（`mineClicked: false`），
        //   否则 2 人房里客机一点就"人齐了"直接开桌，主机根本没点
        //   —— 违反用户「几个玩家都点了斗地主那个图标才能开始」的规矩。
        //   而且这里必须走 GatherNote（原来只 Add 不检查 ⇒ 人齐了也不开，只能干等 25 秒）。
        StartGather(me, Entry.Logger, mineClicked: false);
        GatherNote(senderNetId);
    }

    /// <summary>
    /// ★ 2026-10-10 加：**桌子已经开着，又有人来点「斗地主」** —— 分三种情况处理。
    ///
    /// 为什么必须有：用户实测「我们俩点了斗地主，但是有人没进去」。
    /// 那位的桌子一旦关掉（他自己收了 / 掉线重进 / 等了 40 秒自动收），
    /// 他再点「斗地主」时主机这边是 `Active == true`，原来的代码**直接 return** ——
    /// 请求被静默丢掉，他只能干等，永远回不来。
    ///
    /// 三种情况：
    ///  ① **他已经在桌上**（只是重发请求）→ 把座位表重发一遍，让他的客户端清掉"等主机开桌"。
    ///  ② **他不在桌上，而且牌局还没开始**（还在选人界面）→ 给他找一席电脑坐下，
    ///     `Gen + 1` 换代 + 重发座位表 + 全桌回选人界面重新点「开始打牌」。
    ///  ③ **他不在桌上，但牌已经发了** → 不硬塞（半路换座位会把这一局搅烂），
    ///     只记日志 + 提示他等这一局打完。
    /// </summary>
    private static void HostAdmitLatecomer(ulong netId, int theirRoom)
    {
        if (netId == 0) return;

        var mine = RoomKeyNow();
        if (theirRoom >= 0 && mine >= 0 && theirRoom != mine)
        {
            Entry.Logger.Warn($"斗地主：{netId} 在别的房间（{theirRoom}）请求开桌，我现在在 {mine} —— 不算数。");
            return;
        }

        // ① 本来就在桌上 —— 重发一次座位表（他那边还停在"等主机开桌"）
        if (SeatOfNet(netId) >= 0)
        {
            Entry.Logger.Info($"斗地主：{netId} 又请求了一次开桌（他本来就在桌上）—— 重发座位表。");
            CastOpen();
            return;
        }

        // ③ 牌已经发了 —— 半路塞人会把这一局搅烂，不硬来
        if (Live is null || !Live.IsPicking)
        {
            Entry.Logger.Warn($"斗地主：{netId} 想补进来，但这一局已经在打了 —— 让他等这一局结束。");
            return;
        }

        // ② 还在选人界面 —— 给他找一席电脑
        var free = -1;
        for (var s = 0; s < Seats; s++)
            if (!SeatHuman[s]) { free = s; break; }
        if (free < 0)
        {
            Entry.Logger.Warn($"斗地主：{netId} 想补进来，但 {Seats} 席已经坐满了。");
            return;
        }

        var p = RunPlayers()?.FirstOrDefault(x => x.NetId == netId);
        if (p is null)
        {
            Entry.Logger.Warn($"斗地主：{netId} 想补进来，但 run 里找不到这个人（他可能已经不在这一局了）。");
            return;
        }

        SeatHuman[free] = true;
        SeatNet[free] = netId;
        SeatName[free] = PlayerName(p);
        SeatChar[free] = DouSpireCharacters.PoolIndex(p.Character);
        SeatPlayerObj[free] = p;

        // 换代 → 各端的 `BeginNetSession` 才会认"这是一桌新的"，重新摆牌 + 重新进选人界面
        Gen++;
        Entry.Logger.Info($"斗地主：{netId} 补进来了，坐 {free} 号位（第 {Gen} 代）—— 全桌回选人界面。");

        CastOpen();                        // 座位表重发（他会照这份重新认自己的座位）
        Live?.NetPickOpen("斗地主（联机）：有人补进来了 —— 全桌回选人界面重新点「开始打牌」。");
    }

    // ══════════════════════ 开桌意愿收集（"都点了才开始"）══════════════════════

    /// <summary>等多少个人点才算齐 —— = run 里的玩家人数，封顶一桌的座位数。</summary>
    private static int ExpectedClickers(Player me)
    {
        try
        {
            var n = me.RunState?.Players?.Count ?? 1;
            return Math.Max(1, Math.Min(Seats, n));
        }
        catch { return 1; }
    }

    /// <summary>收集到的人（netId）。主机自己的那次点击也算一份。</summary>
    private static readonly HashSet<ulong> _gatherSeen = new();

    /// <summary>收集阶段的"第几代" —— 防旧的倒计时到点了把新状态误开桌。</summary>
    private static int _gatherGen;

    private static int _gatherExpected = 1;
    private static bool _gathering;

    /// <summary>等多久。超过就先开（没点的座位由电脑补）—— 永远不能因为一个人挂机就开不了桌。</summary>
    private const double GatherTimeoutSeconds = 25.0;

    /// <summary>
    /// 收集阶段记一票：**人齐了立刻开桌**。
    ///
    /// ★★ 2026-10-11 加（修用户实测："必须主机点斗地主、客机再点；客机先点我就进不去"）：
    ///   原来「记票 + 人齐就开」这套逻辑**只写在 `HostOnOpenReq` 的 `_gathering` 分支里**，
    ///   另外两条路都漏了：
    ///     · 客机先点（`HostOnOpenReq` 的"我来起头"分支）→ 只 `Add` 不检查，人齐了也不开；
    ///     · 主机自己后点（`OpenAtRestSite` → `StartGather`）→ 撞上 `if (_gathering) return;`
    ///       **直接返回，什么都不做** ⇒ 主机看到的是"点了没反应、进不去"。
    ///   现在三处统一走这一个函数，谁点都是「记票 → 人齐就开」。
    /// </summary>
    private static void GatherNote(ulong netId)
    {
        if (!_gathering) return;
        if (!_gatherSeen.Add(netId)) return;      // 同一个人重复点，不重复计数

        Entry.Logger.Info($"斗地主：又有一人点了「斗地主」（{_gatherSeen.Count}/{_gatherExpected}）。");
        Entry.Toast($"等大家一起点「斗地主」…（{_gatherSeen.Count}/{_gatherExpected}）", "斗地主");
        if (_gatherSeen.Count >= _gatherExpected)
        {
            Entry.Logger.Info("斗地主：人齐了，开桌。");
            FinishGather();
        }
    }

    /// <summary>
    /// **开始收集"大家都点了没"**（主机）。人齐立刻开；<see cref="GatherTimeoutSeconds"/> 秒没齐也开。
    ///
    /// 用户 2026-10-08：「**四个人或三个人都点了斗地主那个图标才能开始斗地主。为三个或四个玩家
    /// （查询当时是几个人联机）**」—— 以前是"谁点谁开"，其他人被动被拉进来；
    /// 现在先数人头，齐了才发牌。倒计时是兜底：有人没装 mod / 掉线 / 挂机，桌照样能开。
    /// </summary>
    /// <param name="mineClicked">
    /// 这一票是不是**主机自己点的**。
    /// ★ 2026-10-11：必须区分 —— 客机先点时（`HostOnOpenReq` 那条路）**不能白送主机一票**，
    ///   否则 2 人房里客机一点就"人齐了"直接开桌，主机根本没点（违反用户"都点了才能开"的规矩）。
    /// </param>
    private static void StartGather(Player me, Logger logger, bool mineClicked)
    {
        if (_gathering)
        {
            // ★ 已经在收集了 —— 主机这一票补上（原来这里直接 return = "点了没反应"，用户以为进不去）
            if (mineClicked) GatherNote(MyNetIdForSeat());
            return;
        }

        _gatherSeen.Clear();
        if (mineClicked) _gatherSeen.Add(MyNetIdForSeat());   // 主机自己这次点击算一份
        _gatherExpected = ExpectedClickers(me);
        _gathering = true;
        var gen = ++_gatherGen;

        logger.Info($"斗地主：开始收集开桌意愿 —— {_gatherSeen.Count}/{_gatherExpected}"
                    + $"（{GatherTimeoutSeconds:0} 秒后强制开，没点的由电脑补）。");
        Entry.Toast($"等大家一起点「斗地主」…（{_gatherSeen.Count}/{_gatherExpected}）", "斗地主");

        // 倒计时：用 Godot 的 SceneTreeTimer（回调在主线程上，不用自己加锁）
        try
        {
            if (Godot.Engine.GetMainLoop() is Godot.SceneTree tree)
            {
                var t = tree.CreateTimer(GatherTimeoutSeconds);
                t.Timeout += () => GatherTimeout(gen);
            }
        }
        catch (Exception e)
        {
            logger.Warn($"斗地主：开桌倒计时没起来（不影响，人齐了照样开）: {e.Message}");
        }
    }

    private static void GatherTimeout(int gen)
    {
        if (gen != _gatherGen || !_gathering) return;
        Entry.Logger.Info($"斗地主：{GatherTimeoutSeconds:0} 秒到了（收到 {_gatherSeen.Count}/{_gatherExpected}）"
                          + "—— 先开，没点的座位由电脑补。");
        FinishGather();
    }

    private static void FinishGather()
    {
        _gathering = false;
        var me = LocalMe();
        if (me is null)
        {
            Entry.Logger.Warn("斗地主：人齐了却拿不到自己的玩家对象，开不了桌。");
            return;
        }
        HostOpenTable(me, Entry.Logger);
    }

    /// <summary>收桌。主机收桌会通知所有人一起收。</summary>
    public static void CloseTable(bool broadcast)
    {
        if (Active && broadcast && IsHost) CastClose();
        Active = false;
        IsHost = false;
        MySeat = 0;
        Seed = 0;
        RoomKey = -1;
        OpenPending = false;
        SendFailStreak = 0;      // 收桌 = 这张桌的"线"不再有意义，计数清掉
        SendFailSince = 0;
        Array.Clear(SeatHuman);
        Array.Clear(SeatNet);
        Array.Clear(SeatChar);
        Array.Clear(SeatReady);
        for (var s = 0; s < Seats; s++) SeatPlayerObj[s] = null;
    }

    /// <summary>当前所在房间的 Id（用来判断"离开篝火了没有"）。</summary>
    public static int RoomKeyNow()
    {
        try
        {
            var me = LocalMe();
            return me?.RunState?.CurrentRoom?.Id ?? -1;
        }
        catch { return -1; }
    }

    /// <summary>本地玩家对象。</summary>
    public static Player? LocalMe()
    {
        try
        {
            var rm = RunManager.Instance;
            if (rm is null || !rm.IsInProgress) return null;
            var players = RunPlayers();
            if (players is null) return null;
            return MegaCrit.Sts2.Core.Context.LocalContext.GetMe(players);
        }
        catch { return null; }
    }

    /// <summary>
    /// 我自己的 netId —— **必须和主机填座位表时用的是同一个来源**。
    ///
    /// 主机 <c>HostOpenTable</c> 写的是 `SeatNet[s] = p.NetId`（<c>p</c> 来自 <see cref="RunPlayers"/>），
    /// 那是**联机层**的 id（SteamID）。所以客机也得从联机层拿 —— 两端才一定对得上。
    ///
    /// ★★ 2026-10-11 修 P0（用户实测："我出不来牌"）：
    ///   原来 <c>HandleOpen</c> 直接调 <see cref="LocalNetId"/>，而那个函数**第一优先是
    ///   `LocalContext.NetId`** —— 那是**另一套 id**（本地上下文的口径），未必等于联机的 SteamID。
    ///   一旦它返回一个「非 0 但不是 SteamID」的值，就会**一个座位都匹配不上** →
    ///   客机认不出自己 → 按 0 号位处理 → `MySeat` 错成 0 → `ViewFrom(0)` 不旋转 →
    ///   2v2 的 `PendingDiscards` 还是全球口径 `{1,3}` → 客机 `Contains(MeSeat=0)` 恒 false →
    ///   **扣牌按钮不亮** → 主机永远等不到他扣牌 → 整桌卡死。
    ///   主机不受影响（`MySeat` 恒 0），所以现象就是"主机正常、客机动不了"。
    ///
    /// 取值顺序（都试，取第一个非 0）：联机层 NetService.NetId → 我自己的 Player.NetId → LocalNetId 兜底。
    /// </summary>
    private static ulong MyNetIdForSeat()
    {
        // ① 联机层：座位表就是按这一套 id 排的（和 HostOpenTable 的 p.NetId 同源）
        try
        {
            var id = RunManager.Instance?.NetService?.NetId ?? 0UL;
            if (id != 0) return id;
        }
        catch { /* 拿不到就走下一档 */ }
        // ② 我自己的 Player 对象（同一套来源，换条路再试一次）
        try
        {
            var me = LocalMe();
            if (me is not null && me.NetId != 0) return me.NetId;
        }
        catch { /* 拿不到就走下一档 */ }
        // ③ 最后才退回 LocalNetId（它可能给出另一套 id —— 只当救兵，不做首选）
        return LocalNetId();
    }

    /// <summary>
    /// 这个 <see cref="Player"/> 是不是**本机玩家**。
    ///
    /// ⚠️ 为什么不直接用 <c>LocalContext.IsMe()</c>：篝火选项在多人下会被"广播 + 重放"
    /// （见 <see cref="OpenAtRestSite"/> 的注释），实测同一个人点同一次，
    /// `IsMe()` **一会儿 true 一会儿 false** —— 表现就是"点了没反应，得反复点"，
    /// 德州那边更惨：点了没反应，人就卡在篝火里干等（2026-10-09 用户实测：
    /// 日志里连着好几条"这条是别人的篝火选项"，可那就是他自己点的）。
    /// 这里改成**先按 NetId 比**（稳定），拿不到才退回 `IsMe()`。
    /// </summary>
    internal static bool IsMyPlayer(Player? player)
    {
        if (player is null) return false;
        try
        {
            var self = LocalMe();
            if (self is not null) return self.NetId == player.NetId;
        }
        catch { /* 拿不到就走下面的兜底 */ }
        try { return MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player); }
        catch { return false; }
    }

    private static IReadOnlyList<Player>? RunPlayers()
    {
        try
        {
            // 从任何一个 Player 反查 RunState（Player.RunState 是公开属性）
            var rm = RunManager.Instance;
            if (rm?.NetService is null) return null;
            // RunManager 上没有公开的 State 访问器（是 private），
            // 但工程开了 Krafs.Publicizer，所以这里用反射取一次并缓存，最稳。
            return _stateProp?.GetValue(rm) is IRunState st ? st.Players : null;
        }
        catch { return null; }
    }

    private static readonly System.Reflection.PropertyInfo? _stateProp =
        typeof(RunManager).GetProperty("State",
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance);

    /// <summary>玩家在桌上显示的名字 —— 用他这一局选的角色名（游戏自带本地化）。</summary>
    private static string PlayerName(Player p)
    {
        try
        {
            var n = DouSpireCharacters.Name(p.Character);
            if (!string.IsNullOrWhiteSpace(n) && n != "？") return n;
        }
        catch { }
        return $"玩家{p.NetId}";
    }

    // ══════════════════════════════════════════════════════════════
    //  客机 → 主机
    // ══════════════════════════════════════════════════════════════

    public static void SendShow(bool want) => SendIn(new W().U8(InShow).U8(want ? (byte)1 : (byte)0).Done());
    public static void SendWant(bool want) => SendIn(new W().U8(InWant).U8(want ? (byte)1 : (byte)0).Done());
    public static void SendThrow(int gesture) => SendIn(new W().U8(InThrow).U8((byte)gesture).Done());
    public static void SendDouble(bool want) => SendIn(new W().U8(InDouble).U8(want ? (byte)1 : (byte)0).Done());
    public static void SendPass() => SendIn(new W().U8(InPass).Done());
    public static void SendLeave() => SendIn(new W().U8(InLeave).Done());

    /// <summary>
    /// 客机 → 主机的**心跳**（空包）。见 <see cref="InPing"/> 的注释：
    /// 目的只是"持续探一下对面还收不收得到我"，好让断线哨兵有据可依。
    /// 主机收到什么都不做（`HandleIn` 里那个空 case）。
    /// </summary>
    public static void SendPing() => SendIn(new W().U8(InPing).Done());

    /// <summary>客机 → 主机：请开新一局（种子由主机定，见 <see cref="InNewRound"/>）。</summary>
    public static void SendNewRound() => SendIn(new W().U8(InNewRound).Done());

    /// <summary>客机 → 主机：我要重选角色（请全桌一起弹选人界面）。</summary>
    public static void SendPickOpen() => SendIn(new W().U8(InPickOpen).Done());

    /// <summary>客机 → 主机：我这一席换成池子下标 <paramref name="poolIndex"/>。</summary>
    public static void SendPickSet(int poolIndex)
        => SendIn(new W().U8(InPickSet).I32(poolIndex).Done());

    /// <summary>客机 → 主机：我这一席选好了（点了「开始打牌」）。</summary>
    public static void SendPickReady() => SendIn(new W().U8(InPickReady).Done());

    /// <summary>客机 → 主机：把**电脑那一席**换成池子下标 N（主机校验"那一席确实是电脑"再广播）。</summary>
    public static void SendPickSetAi(int globalSeat, int poolIndex)
        => SendIn(new W().U8(InPickSetAi).U8((byte)globalSeat).I32(poolIndex).Done());

    /// <summary>客机 → 主机：2v2 扣牌阶段「我扣这一张」（一张牌）。</summary>
    public static void SendDiscard(Card card)
    {
        var w = new W().U8(InDiscard).U8((byte)card.Rank).U8((byte)card.Suit);
        SendIn(w.Done());
    }

    /// <summary>主机 → 所有人：大家一起弹选人界面。</summary>
    public static void CastPickOpen()
    {
        ClearReady();        // 重新选人 = 之前的"已准备"全作废，大家都得重新点
        CastAct(new W().U8(ActPickOpen).Done());
    }

    /// <summary>主机 → 所有人：某个座位的角色换了（<paramref name="globalSeat"/> + 池子下标）。</summary>
    public static void CastPickSet(int globalSeat, int poolIndex)
        => CastAct(new W().U8(ActPickSet).U8((byte)globalSeat).I32(poolIndex).Done());

    /// <summary>主机 → 所有人：某个座位点了「开始打牌」。</summary>
    public static void CastPickReady(int globalSeat)
        => CastAct(new W().U8(ActPickReady).U8((byte)globalSeat).Done());

    /// <summary>主机 → 所有人：2v2 扣牌阶段「某座位扣了某张牌」（座位 + 一张牌）。</summary>
    public static void CastDiscard(int globalSeat, Card card)
        => CastAct(new W().U8(ActDiscard).U8((byte)globalSeat).U8((byte)card.Rank).U8((byte)card.Suit).Done());

    // ══════════════════════════════════════════════════════════════
    //  联机选角 —— 给上层（DouSpireTable 的选人界面）用的统一入口
    // ══════════════════════════════════════════════════════════════

    /// <summary>我这一席选了角色 <paramref name="poolIndex"/> —— 主机客机都走这条。</summary>
    public static void PickSet(int poolIndex)
    {
        if (!Active) return;
        SetCharIndex(MySeat, poolIndex);              // 本机也记上（不然重开选人时自己那席是旧的）
        if (!IsHost) { SendPickSet(poolIndex); return; }
        CastPickSet(MySeat, poolIndex);
        Live?.NetPickSet(MySeat, poolIndex);
    }

    /// <summary>我点了「开始打牌」—— 主机客机都走这条。</summary>
    public static void PickReady()
    {
        if (!Active) return;
        if (!IsHost) { SendPickReady(); return; }
        HostPickReady(MySeat);
    }

    /// <summary>
    /// 给**电脑那一席**挑角色（★ 2026-10-10 加）—— 主机客机都走这条。
    /// 真人席会被 <see cref="HostPickSetAi"/> 拒掉。
    /// </summary>
    public static void PickSetAi(int globalSeat, int poolIndex)
    {
        if (!Active) return;
        if (!IsHost) { SendPickSetAi(globalSeat, poolIndex); return; }
        HostPickSetAi(globalSeat, poolIndex);
    }

    /// <summary>
    /// 主机侧：替电脑席定角色 —— **校验"那一席确实不是真人"** 再落地 + 广播。
    ///
    /// 为什么要校验：消息里带的座位号是客机说了算的，不校验的话任何一个客机都能把
    /// 别人的角色改掉（那就不是"帮电脑选"，是"抢队友的角色"了）。
    /// </summary>
    public static void HostPickSetAi(int globalSeat, int poolIndex)
    {
        if (!IsHost) return;
        if (globalSeat < 0 || globalSeat >= Seats)
        {
            Entry.Logger.Warn($"斗地主：代选电脑角色时报了个不存在的座位 {globalSeat}，忽略。");
            return;
        }
        if (IsHumanSeat(globalSeat))
        {
            Entry.Logger.Warn($"斗地主：{globalSeat} 号位是真人，不接受代选角色。");
            return;
        }
        if (poolIndex < -1 || poolIndex >= DouSpireCharacters.Pool.Count)
        {
            Entry.Logger.Warn($"斗地主：代选电脑角色时报了个不存在的下标 {poolIndex}，忽略。");
            return;
        }

        SetCharIndex(globalSeat, poolIndex);
        CastPickSet(globalSeat, poolIndex);
        Live?.NetPickSet(globalSeat, poolIndex);
    }

    /// <summary>谁点了「重新选人」都走这条 —— 主机代发"全桌一起弹选人界面"。</summary>
    public static void PickOpenAll()
    {
        if (!Active) return;
        if (!IsHost) { SendPickOpen(); return; }
        CastPickOpen();
        Live?.NetPickOpen();
    }

    /// <summary>清掉所有"已准备"票（重开选人 / 换角色时用）。</summary>
    public static void ClearReady() => Array.Clear(SeatReady);

    /// <summary>
    /// 主机：某一席点了「开始打牌」—— 记下来、广播出去，**所有真人席都齐了才开局**。
    /// 少一个人就继续等（那一席的按钮会变成"✓ 已准备，等队友…"）。
    /// </summary>
    public static void HostPickReady(int globalSeat)
    {
        if (!Active || !IsHost) return;
        if (globalSeat >= 0 && globalSeat < Seats) SeatReady[globalSeat] = true;

        var left = 0;
        for (var s = 0; s < Seats; s++)
            if (SeatHuman[s] && !SeatReady[s]) left++;
        Entry.Logger.Info($"斗地主：{globalSeat} 号位已准备开局 —— 还差 {left} 个真人。");

        CastPickReady(globalSeat);
        Live?.NetPickReady(globalSeat);
        if (left <= 0) Live?.NetHostStartNewRound();
    }

    /// <summary>把某一席的角色改成池子下标（<c>-1</c> = 交回"各端自己挑"）。越界一律忽略。</summary>
    public static void SetCharIndex(int globalSeat, int poolIndex)
    {
        if (globalSeat < 0 || globalSeat >= Seats) return;
        if (poolIndex < -1 || poolIndex >= DouSpireCharacters.Pool.Count) return;
        SeatChar[globalSeat] = poolIndex;
    }

    public static void SendPlay(IReadOnlyList<Card> cards)
    {
        var w = new W().U8(InPlay);
        PutCards(w, cards);
        SendIn(w.Done());
    }

    private static bool SendIn(byte[] payload)
    {
        try
        {
            var ok = RitsuLibSidecarHighLevelSend.TrySendAsClient(
                RunManager.Instance, OpIn, payload, RitsuLibSidecarDeliverySemantics.StableSync);
            if (ok)
            {
                NoteSendOk();
            }
            else
            {
                // ★ 2026-10-10：不再"每次都刷一行" —— 只在**第一次**失败和**判定断线**时说话。
                //   中间那几次静默累计（`SendFailStreak`），交给 `DouSpireTable.NetLinkGuard` 收桌。
                NoteSendFail();
                if (SendFailStreak <= 1)
                    Entry.Logger.Warn("斗地主：发给主机的消息没送出去（连接或握手还没就绪）—— 开始累计失败次数。");
                else if (LinkLost)
                    Entry.Logger.Warn($"斗地主：连着 {SendFailStreak} 次发不出去，"
                                      + $"持续 {NowSeconds() - SendFailSince:0.0} 秒 —— 判定和主机断线。");
            }
            return ok;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：发消息给主机出错: {e.Message}");
            NoteSendFail();
            return false;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  主机 → 所有人
    // ══════════════════════════════════════════════════════════════

    private static void CastOpen()
    {
        var w = new W().I32(Gen).I32(Seed).I32(RoomKey).U8((byte)Seats);
        for (var s = 0; s < Seats; s++)
        {
            w.U8(SeatHuman[s] ? (byte)1 : (byte)0);
            w.U64(SeatNet[s]);
            w.Str(SeatName[s] ?? "？");
            w.I32(SeatChar[s]);
        }
        SendAll(OpOpen, w.Done());
    }

    public static void CastShow(int globalSeat, bool want)
        => CastAct(new W().U8(ActShow).U8((byte)globalSeat).U8(want ? (byte)1 : (byte)0).Done());

    public static void CastWant(int globalSeat, bool want)
        => CastAct(new W().U8(ActWant).U8((byte)globalSeat).U8(want ? (byte)1 : (byte)0).Done());

    public static void CastThrow(int globalSeat, int gesture)
        => CastAct(new W().U8(ActThrow).U8((byte)globalSeat).U8((byte)gesture).Done());

    public static void CastDouble(int globalSeat, bool want)
        => CastAct(new W().U8(ActDouble).U8((byte)globalSeat).U8(want ? (byte)1 : (byte)0).Done());

    public static void CastStart(int globalLandlordSeat)
        => CastAct(new W().U8(ActStart).U8((byte)globalLandlordSeat).Done());

    public static void CastPass(int globalSeat)
        => CastAct(new W().U8(ActPass).U8((byte)globalSeat).Done());

    public static void CastNewRound(int seed)
        => CastAct(new W().U8(ActNewRound).I32(seed).Done());

    public static void CastErr(int globalSeat, string msg)
        => CastAct(new W().U8(ActErr).U8((byte)globalSeat).Str(msg ?? "").Done());

    public static void CastSeatAi(int globalSeat)
        => CastAct(new W().U8(ActSeatAi).U8((byte)globalSeat).Done());

    /// <summary>主机下令推进猜拳阶段 —— <paramref name="globalSeat"/> 只在 phase=2 时有意义（地主全局座位）。</summary>
    public static void CastRpsPhase(int phase, int globalSeat)
        => CastAct(new W().U8(ActRpsPhase).U8((byte)phase).I32(globalSeat).Done());

    public static void CastClose()
        => CastAct(new W().U8(ActClose).Done());

    public static void CastPlay(int globalSeat, IReadOnlyList<Card> cards)
    {
        var w = new W().U8(ActPlay).U8((byte)globalSeat);
        PutCards(w, cards);
        CastAct(w.Done());
    }

    private static bool CastAct(byte[] payload)
    {
        try
        {
            var ok = RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                RunManager.Instance, OpAct, payload, RitsuLibSidecarDeliverySemantics.StableSync);
            if (!ok) Entry.Logger.Warn("斗地主：广播牌局动作没送出去（对面可能没装 mod 或还没就绪）。");
            return ok;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：广播出错: {e.Message}");
            return false;
        }
    }

    private static void SendAll(ulong opcode, byte[] payload)
    {
        try
        {
            RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                RunManager.Instance, opcode, payload, RitsuLibSidecarDeliverySemantics.StableSync);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：广播 {opcode} 出错: {e.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  收消息
    // ══════════════════════════════════════════════════════════════

    private static void HandleIn(RitsuLibSidecarDispatchContext ctx)
    {
        NoteInbound();      // 收到客机任何一条消息 = 线活着（清断线计数）
        try
        {
            var r = new R(ctx.Payload);
            var kind = r.U8();

            // ⚠️ 开桌请求要在"会话还没开"的时候也能处理 —— 它本来就是开桌的入口
            if (kind == InOpenReq)
            {
                HostOnOpenReq(ctx.SenderNetId, r.I32());
                return;
            }

            if (!Active || !IsHost) return;
            if (LocalNetId() != 0 && ctx.SenderNetId == LocalNetId()) return;   // 自己发的（不该发生）

            // 谁发的？按 netId 找他的座位（防冒名：一切以 netId 为准，不看消息里写的座位号）
            var seat = SeatOfNet(ctx.SenderNetId);
            if (seat < 0) { Entry.Logger.Warn($"斗地主：收到一个不认识的人（netId {ctx.SenderNetId}）的消息，忽略。"); return; }

            var t = Live;
            switch (kind)
            {
                case InShow: if (t != null) t.NetShow(ToLocal(seat), r.U8() != 0); break;
                case InWant: if (t != null) t.NetWant(ToLocal(seat), r.U8() != 0); break;
                case InThrow: if (t != null) t.NetThrow(ToLocal(seat), r.U8()); break;
                case InDouble: if (t != null) t.NetDouble(ToLocal(seat), r.U8() != 0); break;
                case InPlay: if (t != null) t.NetPlay(ToLocal(seat), ReadCards(ref r)); break;
                case InPass: if (t != null) t.NetPass(ToLocal(seat)); break;
                // 2v2 扣牌：客机上报「我扣这张」→ 主机校验 + 广播（全桌照做）
                case InDiscard:
                {
                    var card = new Card(r.U8(), (Suit)r.U8());
                    if (t != null) t.NetDiscard(ToLocal(seat), card);
                    break;
                }
                case InLeave: HostGotLeave(seat); break;
                case InNewRound: HostGotNewRound(seat); break;
                // 联机选角：谁点了「重新选人」都行 → 主机代发"大家一起弹"（顺带清掉准备票）
                case InPickOpen: CastPickOpen(); Live?.NetPickOpen(); break;
                // 「我选好了」—— 主机记一票，真人席全齐才开局
                case InPickReady: HostPickReady(seat); break;
                // 只能改**自己那一席**（一切以 netId 为准，不看消息里写的座位号）
                case InPickSet:
                {
                    var idx = r.I32();
                    if (idx < -1 || idx >= DouSpireCharacters.Pool.Count)
                    {
                        Entry.Logger.Warn($"斗地主：{seat} 号位报了个不存在的角色下标 {idx}，忽略。");
                        break;
                    }
                    SeatChar[seat] = idx;
                    CastPickSet(seat, idx);
                    Live?.NetPickSet(seat, idx);
                    break;
                }
                // 「把**电脑那一席**换成 N」—— 座位号是客机报上来的，必须校验那一席确实是电脑
                case InPickSetAi:
                {
                    var g = r.U8();
                    var aiIdx = r.I32();
                    HostPickSetAi(g, aiIdx);
                    break;
                }
                default: Entry.Logger.Warn($"斗地主：收到不认识的客机消息 kind={kind}。"); break;
                // 心跳：**故意什么都不做**。它的价值在"发"不在"收" ——
                // 客机靠 `TrySendAsClient` 的返回值判断对面还在不在；
                // 主机这边 `NoteInbound()`（函数开头）顺手把"这个客机还活着"记下了。
                case InPing: break;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：处理客机消息出错（不影响别人）: {e}");
        }
    }

    private static void HandleOpen(RitsuLibSidecarDispatchContext ctx)
    {
        NoteInbound();      // 收到主机开桌消息 = 线活着
        try
        {
            if (IsHost) return;   // 主机自己不看这个
            var r = new R(ctx.Payload);
            var gen = r.I32();
            var seed = r.I32();
            var room = r.I32();
            var n = r.U8();
            if (n != Seats) { Entry.Logger.Warn($"斗地主：开桌消息的座位数不对（{n}），忽略。"); return; }

            for (var s = 0; s < Seats; s++)
            {
                SeatHuman[s] = r.U8() != 0;
                SeatNet[s] = r.U64();
                SeatName[s] = r.Str();
                SeatChar[s] = r.I32();
            }

            // 我在第几号位？按 netId 找（找不到就按 0 处理，至少不崩）
            // ★★ 2026-10-11：改用 MyNetIdForSeat() —— 和主机填 SeatNet[] 同源（都是 Player.NetId），
            //   两端才一定匹配得上。原来用 LocalNetId()（LocalContext.NetId），客机实测拿到 0，
            //   认不出座位 → MySeat 错成 0 → 2v2 扣牌按钮不亮 → 整桌卡死（用户实测"我出不来牌"）。
            var mine = -1;
            var myId = MyNetIdForSeat();
            for (var s = 0; s < Seats; s++)
                if (SeatHuman[s] && SeatNet[s] == myId) { mine = s; break; }

            if (mine < 0)
            {
                // 兜底②：按"主机是唯一确定的那个"排除 —— 座位表里去掉主机，剩下的真人席如果**只有一个**，
                //        那必定是我（2 真人 + 2 电脑就是这种局面）。3 个真人以上不猜，宁可按 0 也别乱认。
                try
                {
                    if (RunManager.Instance?.NetService is NetClientGameService cc && cc.HostNetId != 0)
                    {
                        var others = new List<int>();
                        for (var s = 0; s < Seats; s++)
                            if (SeatHuman[s] && SeatNet[s] != cc.HostNetId) others.Add(s);
                        if (others.Count == 1)
                        {
                            mine = others[0];
                            Entry.Logger.Warn($"斗地主：认不出自己的 netId（{myId}）—— "
                                              + $"按「排除主机」推出来我坐 {mine} 号位（主机 {cc.HostNetId}）。");
                        }
                    }
                }
                catch { /* 推不出来就走下面的兜底 */ }
            }

            if (mine < 0)
            {
                // 认不出自己 = 座位表会整圈错位（扣牌按钮、视角、金币全不对）—— 日志必须带够证据
                Entry.Logger.Warn($"斗地主：开桌消息里没有我（我的 netId {myId}，"
                                  + $"LocalNetId={LocalNetId()}）—— 按 0 号位处理，座位表会错。"
                                  + "座位表 = " + string.Join("、", Enumerable.Range(0, Seats)
                                      .Select(s => $"{s}:{SeatNet[s]}{(SeatHuman[s] ? "" : "(AI)")}")));
                mine = 0;
            }

            Gen = gen;
            Seed = seed;
            RoomKey = room;
            MySeat = mine;
            IsHost = false;
            OpenPending = false;
            Active = true;

            Entry.Logger.Info($"斗地主：主机开桌了（第 {gen} 代）—— 我坐 {mine} 号位，种子 {seed}，"
                              + string.Join("、", Enumerable.Range(0, Seats)
                                  .Select(s => $"{s}:{SeatName[s]}{(SeatHuman[s] ? "" : "(AI)")}")));

            var me = LocalMe();
            if (me is null) Entry.Logger.Warn("斗地主：客机拿不到自己的玩家对象，只能看牌不能下注。");
            DouSpireTable.OpenForNet(me, Entry.Logger);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：处理开桌消息出错: {e}");
        }
    }

    private static void HandleAct(RitsuLibSidecarDispatchContext ctx)
    {
        NoteInbound();      // ★ 收到主机任何一条广播 = 线活着 —— 客机的断线计数在这里清零
        try
        {
            if (!Active || IsHost) return;    // 主机自己发的，不用收
            var t = Live;
            var r = new R(ctx.Payload);
            var act = r.U8();
            switch (act)
            {
                case ActShow: { var s = ToLocal(r.U8()); if (t != null) t.NetShow(s, r.U8() != 0); break; }
                case ActWant: { var s = ToLocal(r.U8()); if (t != null) t.NetWant(s, r.U8() != 0); break; }
                case ActThrow: { var s = ToLocal(r.U8()); if (t != null) t.NetThrow(s, r.U8()); break; }
                case ActDouble: { var s = ToLocal(r.U8()); if (t != null) t.NetDouble(s, r.U8() != 0); break; }
                case ActStart: { var s = ToLocal(r.U8()); if (t != null) t.NetStart(s); break; }
                case ActPlay: { var s = ToLocal(r.U8()); if (t != null) t.NetPlay(s, ReadCards(ref r)); break; }
                case ActPass: { var s = ToLocal(r.U8()); if (t != null) t.NetPass(s); break; }
                // 2v2 扣牌：主机广播「某座位扣了某张」→ 全桌照做
                case ActDiscard:
                {
                    var s = ToLocal(r.U8());
                    var card = new Card(r.U8(), (Suit)r.U8());
                    if (t != null) t.NetDiscard(s, card);
                    break;
                }
                case ActNewRound: { var seed = r.I32(); if (t != null) t.NetNewRound(seed); break; }
                case ActErr: { var s = ToLocal(r.U8()); var msg = r.Str(); if (t != null) t.NetErr(s, msg); break; }
                case ActSeatAi:
                {
                    // ★ 2026-10-10：**各端都要更新自己的座位表** —— 只调 `t.NetSeatAi` 的话，
                    //   客机的 `SeatHuman[]` 还留着"那个人是真人"，选人界面就会一直显示他的名字、
                    //   `PickUnreadyHumans()` 也一直把他算进"还差几个真人"（倒计时永远不消失）。
                    var g = r.U8();
                    NoteSeatBecameAi(g);
                    if (t != null) t.NetSeatAi(ToLocal(g));
                    break;
                }
                case ActRpsPhase:
                {
                    var ph = r.U8();
                    var seat = r.I32();
                    if (t != null) t.NetRpsPhase(ph, seat < 0 ? -1 : ToLocal(seat));
                    break;
                }
                case ActClose: if (t != null) t.NetClosed(); break;
                case ActPickOpen: if (t != null) t.NetPickOpen(); break;
                case ActPickSet:
                {
                    var gSeat = r.U8();
                    var idx = r.I32();
                    SetCharIndex(gSeat, idx);
                    if (t != null) t.NetPickSet(gSeat, idx);
                    break;
                }
                case ActPickReady:
                {
                    var gSeat = r.U8();
                    if (t != null) t.NetPickReady(gSeat);
                    break;
                }
                default: Entry.Logger.Warn($"斗地主：收到不认识的动作 act={act}。"); break;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：处理动作消息出错: {e}");
        }
    }

    /// <summary>
    /// 主机收到"有人要开新一局" —— **种子由主机定**，定完广播给全桌（含自己）。
    ///
    /// ⚠️ 不许"谁按谁定种子"：`Deal()` 里的种子是**本地随机**的，两端各随一次
    /// 就会发到两副不同的牌 ⇒ 一开局就分叉（见 <see cref="InNewRound"/>）。
    /// </summary>
    private static void HostGotNewRound(int seat)
    {
        Entry.Logger.Info($"斗地主：{seat} 号位要求开新一局 —— 主机来定种子。");
        Live?.NetHostStartNewRound();
    }

    /// <summary>主机收到"某人离桌" —— 把他那一席改成 AI 接管，并广播（不然会卡在他那儿）。</summary>
    private static void HostGotLeave(int seat)
    {
        if (!SeatHuman[seat]) return;
        NoteSeatBecameAi(seat);
        Entry.Logger.Info($"斗地主：{seat} 号位离桌，改由电脑接管。");
        CastSeatAi(seat);
        Live?.NetSeatAi(seat);
    }

    /// <summary>
    /// 把某一席标成"电脑接管"。
    ///
    /// ★ 2026-10-10 抽出来共用 —— 原来只有主机那边改，**客机的 `SeatHuman[]` 一直是旧的**
    ///   ⇒ 客机的选人界面会把已经走掉的人还当成真人显示（名字还在、还会算进"还差几个真人"）。
    ///   现在主机（`HostGotLeave`）和收到广播的客机（`HandleAct` 的 `ActSeatAi`）都走这一条。
    /// </summary>
    private static void NoteSeatBecameAi(int globalSeat)
    {
        if (globalSeat < 0 || globalSeat >= Seats) return;
        SeatHuman[globalSeat] = false;
        SeatNet[globalSeat] = 0;
        SeatName[globalSeat] = "电脑";
        SeatChar[globalSeat] = -1;
    }

    private static int SeatOfNet(ulong netId)
    {
        for (var s = 0; s < Seats; s++)
            if (SeatHuman[s] && SeatNet[s] == netId) return s;
        return -1;
    }

    /// <summary>我自己的 netId（优先问游戏，问不到就自己找）。</summary>
    public static ulong LocalNetId()
    {
        try
        {
            var ctx = MegaCrit.Sts2.Core.Context.LocalContext.NetId;
            if (ctx is ulong id && id != 0) return id;
        }
        catch { }
        try { return RunManager.Instance?.NetService?.NetId ?? 0UL; }
        catch { return 0UL; }
    }

    // ══════════════════════════════════════════════════════════════
    //  二进制编解码（自己写，不引任何序列化器 —— 少一处能踩崩的地方）
    //
    //  牌就是 (byte Rank, byte Suit)：Card 是 readonly record struct Card(int Rank, Suit Suit)，
    //  Rank 3..17、Suit 0..4，一个字节塞得下。
    //  字节序统一小端（BinaryPrimitives），跟机器架构无关。
    // ══════════════════════════════════════════════════════════════

    private sealed class W
    {
        private readonly List<byte> _b = new(64);
        public W U8(byte v) { _b.Add(v); return this; }

        public W I32(int v)
        {
            Span<byte> tmp = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(tmp, v);
            _b.AddRange(tmp.ToArray());
            return this;
        }

        public W U64(ulong v)
        {
            Span<byte> tmp = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(tmp, v);
            _b.AddRange(tmp.ToArray());
            return this;
        }

        public W Str(string s)
        {
            var by = Encoding.UTF8.GetBytes(s ?? "");
            I32(by.Length);
            _b.AddRange(by);
            return this;
        }

        public byte[] Done() => _b.ToArray();
    }

    private struct R
    {
        private readonly byte[] _b;
        private int _i;

        public R(ReadOnlyMemory<byte> m) { _b = m.ToArray(); _i = 0; }

        public byte U8()
        {
            if (_i >= _b.Length) throw new InvalidOperationException("斗地主：网络消息读越界");
            return _b[_i++];
        }

        public int I32()
        {
            if (_i + 4 > _b.Length) throw new InvalidOperationException("斗地主：网络消息读越界");
            var v = BinaryPrimitives.ReadInt32LittleEndian(_b.AsSpan(_i, 4));
            _i += 4;
            return v;
        }

        public ulong U64()
        {
            if (_i + 8 > _b.Length) throw new InvalidOperationException("斗地主：网络消息读越界");
            var v = BinaryPrimitives.ReadUInt64LittleEndian(_b.AsSpan(_i, 8));
            _i += 8;
            return v;
        }

        public string Str()
        {
            var n = I32();
            if (n < 0 || _i + n > _b.Length) throw new InvalidOperationException("斗地主：网络消息读越界");
            var s = Encoding.UTF8.GetString(_b, _i, n);
            _i += n;
            return s;
        }
    }

    private static void PutCards(W w, IReadOnlyList<Card> cards)
    {
        // 一次出牌的牌数上限 —— 单副牌手牌 17~20 张，两副牌（4 人 2v2）手牌正好 20 张。
        // 取 24 留点余量，别因为"两手牌一次甩出"这种极端把包截断。
        var n = Math.Min(cards.Count, 24);
        w.U8((byte)n);
        for (var i = 0; i < n; i++)
        {
            w.U8((byte)cards[i].Rank);
            w.U8((byte)cards[i].Suit);
        }
    }

    private static List<Card> ReadCards(ref R r)
    {
        var n = r.U8();
        var list = new List<Card>(n);
        for (var i = 0; i < n; i++)
        {
            var rank = r.U8();
            var suit = r.U8();
            list.Add(new Card(rank, (Suit)suit));
        }
        return list;
    }
}
