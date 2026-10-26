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
/// **德州扑克桌的联机层**（玩法二 · P4，2026-10-09）。
///
/// 和 <see cref="DouSpireNet"/>（斗地主那套）是**两个独立的会话层**，各有各的三个频道：
/// 那边是 <c>table.in / table.open / table.act</c>，这边是 <c>poker.in / poker.open / poker.act</c>。
/// **为什么不复用**：斗地主那张桌 <c>Seats = 3</c> 是写死的、而且已经上架在跑，
/// 改它等于拿线上功能冒险；而且两张桌的阶段机完全不一样（斗地主是"抢地主+出牌"，
/// 德州是"四轮下注+摊牌"），硬套只会两边都别扭。**复制骨架、独立演化**是这里更划算的选择。
///
/// ── 走哪条线 / 谁说了算 ─────────────────────────────────────
/// 完全照抄 <see cref="DouSpireNet"/> 的结论（那边已经实机验过）：
/// 借 RitsuLib 的 Sidecar 总线传原始字节，投递语义 <c>StableSync</c>（可靠 + 有序）；
/// **run 的主机就是这一桌的主机**，主机是唯一权威 —— 它跑 AI、校验每个人的动作、
/// 把结果广播给所有人。客机**不自己推进任何决策**，只做两件事：
/// 把玩家的操作发给主机、把主机广播的动作照做。
///
/// ── ★ 为什么客机"自己不应用、等主机回声" ────────────────────
/// 德州是**完全信息同步**的：所有端用同一个种子、同一串动作，跑出来必然一模一样。
/// 既然如此，最稳的做法就是"客机不许自己走子"：
/// 点了按钮 → 只发消息 → 等主机广播同一条动作 → 才在本地 <c>Apply</c>。
/// 这样**永远不可能分叉**（代价是点完要等一个来回，牌类游戏完全能接受）。
/// 主机自己的动作则是"本地应用 + 广播"一步到位。
///
/// ── ★ 底牌怎么保证公平又不外泄 ──────────────────────────────
/// 用户 2026-10-09 拍板：「**同种子各端本地发牌**」（接受理论上可作弊）。
/// 每手的种子由主机定、跟"开这手"的消息一起广播；各端拿同一个种子各自洗牌发牌
/// → 同一个人在各端拿到的底牌必然一样，而**谁的底牌谁自己知道**，
/// 界面上别人那两张一直画牌背，只有摊牌那一刻才翻开。
///
/// ── 兜底（照斗地主的规矩：**永远不能卡死**）────────────────
/// 对面没装 mod / 掉线 / 挂机 → 主机侧有超时，超时就用"能过牌就过牌、否则弃牌"替他走完。
/// 主动离桌的人，他那一席改成电脑接管并广播。
/// </summary>
public static class DouSpirePokerNet
{
    /// <summary>一桌最多坐 4 个（德州实际用 3 或 4，单挑也支持）。</summary>
    public const int MaxSeats = 4;

    /// <summary>网络 opcode 的命名空间 —— ★ 跟着 <see cref="Entry.ModId"/> 走（同 DouSpireNet）。</summary>
    private const string ModId = Entry.ModId;

    // ══════════════════════════════════════════════════════════════
    //  三个 opcode —— mod 自己的频道（和斗地主那三条完全分开）
    // ══════════════════════════════════════════════════════════════

    /// <summary>客机 → 主机：我要开桌 / 我的动作 / 我要下一手 / 我走了。</summary>
    public static readonly ulong OpIn = RitsuLibSidecarOpcodes.For(ModId, "poker.in");

    /// <summary>主机 → 所有人：开桌（座位表 + 人数）。</summary>
    public static readonly ulong OpOpen = RitsuLibSidecarOpcodes.For(ModId, "poker.open");

    /// <summary>主机 → 所有人：牌局动作（开一手 / 某人走子 / 某席改电脑 / 收桌）。</summary>
    public static readonly ulong OpAct = RitsuLibSidecarOpcodes.For(ModId, "poker.act");

    // ── 客机 → 主机 的消息种类 ──
    public const byte InOpenReq = 1;
    public const byte InAct = 2;
    public const byte InNextHand = 3;
    public const byte InLeave = 4;

    // ── 主机 → 所有人 的动作种类 ──
    public const byte ActOpen = 1;
    public const byte ActHand = 2;
    public const byte ActMove = 3;
    public const byte ActSeatAi = 4;
    public const byte ActClose = 5;

    // ══════════════════════════════════════════════════════════════
    //  会话状态（全局只有一桌，所以是静态的）
    // ══════════════════════════════════════════════════════════════

    /// <summary>这一桌跑在联机里吗？（篝火 + 多人房才为 true）</summary>
    public static bool Active { get; private set; }

    /// <summary>我是不是这一桌的主机（= run 的主机）。</summary>
    public static bool IsHost { get; private set; }

    /// <summary>我的全局座位号。</summary>
    public static int MySeat { get; private set; }

    /// <summary>这一桌坐几个人（3 或 4，人不够的座位由电脑补）。</summary>
    public static int SeatCount { get; private set; } = 3;

    /// <summary>开桌时所在的房间 Id。</summary>
    public static int RoomKey { get; private set; } = -1;

    /// <summary>这一桌的"第几代"。每次主机重新开桌 +1，客机靠它分辨"新桌"和"重复收到的老消息"。</summary>
    public static int Gen { get; private set; }

    /// <summary>客机已经请求开桌、还在等主机应答。</summary>
    public static bool OpenPending { get; set; }

    private static readonly bool[] SeatHuman = new bool[MaxSeats];
    private static readonly ulong[] SeatNet = new ulong[MaxSeats];
    private static readonly string[] SeatName = new string[MaxSeats];
    private static readonly int[] SeatChar = new int[MaxSeats];     // 发牌池下标，-1 = 没定

    /// <summary>
    /// 各席的玩家对象（**只有主机那边有值**）。用途：结算时主机把每个人的输赢
    /// 写回他**自己的真实金币**，再靠游戏自己的玩家状态同步发到各端。
    /// </summary>
    private static readonly Player?[] SeatPlayerObj = new Player?[MaxSeats];

    /// <summary>当前开着的牌桌（收桌时置空）。</summary>
    internal static DouSpirePokerTable? Live;

    // ══════════════════════════════════════════════════════════════
    //  对外查询
    // ══════════════════════════════════════════════════════════════

    public static bool IsHumanSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < SeatCount && SeatHuman[globalSeat];

    public static ulong NetIdOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < SeatCount ? SeatNet[globalSeat] : 0UL;

    public static string NameOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < SeatCount ? SeatName[globalSeat] : "？";

    /// <summary>这一席预定的发牌池下标（-1 = 没定，各端自己挑）。</summary>
    public static int CharIndexOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < SeatCount ? SeatChar[globalSeat] : -1;

    public static Player? PlayerOfSeat(int globalSeat)
        => globalSeat >= 0 && globalSeat < SeatCount ? SeatPlayerObj[globalSeat] : null;

    /// <summary>本地视角的第 <paramref name="localView"/> 号位 → 全局座位号。</summary>
    public static int ToGlobal(int localView)
        => Active ? (((MySeat + localView) % SeatCount) + SeatCount) % SeatCount : localView;

    /// <summary>全局座位号 → 本地视角的第几号位。</summary>
    public static int ToLocal(int globalSeat)
        => Active ? (((globalSeat - MySeat) % SeatCount) + SeatCount) % SeatCount : globalSeat;

    /// <summary>本地视角下"我"的那一席的全局座位号。</summary>
    public static int MyGlobalSeat => MySeat;

    /// <summary>现在是不是**真的联机 run**（单人 / 假多人都不算）。</summary>
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
            Entry.Logger.Warn($"德州：判断是否联机时出错（按单人处理）: {e.Message}");
            return false;
        }
    }

    /// <summary>run 的主机是不是我。</summary>
    public static bool LocalIsRunHost()
    {
        try { return RunManager.Instance?.NetService?.Type == NetGameType.Host; }
        catch { return false; }
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
    //  注册（Entry.Initialize 调一次）
    // ══════════════════════════════════════════════════════════════

    private static bool _registered;

    /// <summary>把三个 opcode 的处理器挂到 Sidecar 总线上。幂等。</summary>
    public static void Register()
    {
        if (_registered) return;
        try
        {
            // ⚠️ **必须先叫这一句**（和斗地主那边同一个理由）：它才会把 RitsuLib 自己的
            // 网络生命周期钩子（握手、会话绑定、就绪标记）装上去。装晚了就赶不上大厅阶段的握手，
            // `CanSendToPeer` 会一直返回 false —— 消息全部静默丢弃。它保证可重复调用。
            RitsuLibSidecarProtocol.EnsureDefaultHandlers();

            RitsuLibSidecarBus.RegisterHandler(OpIn, HandleIn);
            RitsuLibSidecarBus.RegisterHandler(OpOpen, HandleOpen);
            RitsuLibSidecarBus.RegisterHandler(OpAct, HandleAct);
            _registered = true;
            Entry.Logger.Info("德州：联机频道已挂上（poker.in / poker.open / poker.act 三条）。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：挂联机频道失败（只能单人玩）: {e.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  开桌 / 收桌
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// **在篝火点「德州扑克」** —— 单人就直接开桌，多人就联机开桌。
    /// 由 <see cref="DouSpirePokerRestSiteOption.OnSelect"/> 调。
    ///
    /// ⚠️⚠️ **这个函数在多人下会被每一台机器调用，不只是"点的那个人"那台** ——
    /// 原版 <c>RestSiteSynchronizer.ChooseLocalOption(index)</c> 会把"谁选了第几项"
    /// 广播出去（该消息 <c>ShouldBroadcast => true</c>），所有机器的
    /// <c>HandleRestSiteOptionChosenMessage</c> 都会为**那个玩家**跑一遍 <c>ChooseOption</c>
    /// → 于是 <c>OnSelect()</c> 在每台机器上都被调一次，而且 <c>Owner</c> 是**选择者**、不是"我"。
    /// 少了下面这句 <c>IsMe</c> 判断，客机一点「德州扑克」，
    /// **主机会拿客机的 Player 当自己开桌** —— 座位表全错（斗地主那边踩过这个 P0）。
    /// </summary>
    public static void OpenAtRestSite(Player player, Logger logger)
    {
        var mp = InMultiplayerRun();

        // 单人：IsMe 判断只在多人下有意义，单人一律照旧走（不引入回归）
        // ⚠️ 判据走 DouSpireNet.IsMyPlayer（先比 NetId）—— `LocalContext.IsMe()` 在
        //    "篝火选项广播重放"这条路上实测时灵时不灵，玩家点了德州没反应、只能干等
        //    （2026-10-09 用户实测）。
        if (mp && !DouSpireNet.IsMyPlayer(player))
        {
            logger.Info("德州：这条是别人的篝火选项（本机只是重放那条广播），不动作。");
            return;
        }

        if (!mp)
        {
            DouSpirePokerTable.OpenAtRestSite(player, logger);
            return;
        }

        if (Active)
        {
            logger.Info("德州：联机桌已经在跑，直接重新显示。");
            DouSpirePokerTable.OpenForNet(player, logger, stake: true, seats: SeatCount);
            return;
        }

        if (LocalIsRunHost())
        {
            // ★ 和斗地主同一条规矩：**大家都点了才开**。主机点了自己这张先不立刻开，
            //   先收集大家的意愿，等齐了（或超时兜底）才真正发牌。就我一个人的房，点了直接开。
            var expected = ExpectedClickers(player);
            if (expected <= 1)
            {
                HostOpenTable(player, logger);
                return;
            }
            StartGather(player, logger);
            return;
        }

        // 客机：请求主机开桌。**先不摆桌**（桌等主机广播开桌消息时才出现），
        // 但给一个 toast，否则玩家点了完全看不出发生了什么。
        logger.Info("德州：我是客机，向主机请求开桌。");
        LogPeerState();
        OpenPending = true;
        Entry.Toast("已请求开一桌德州，等主机发牌…", "德州扑克");
        SendIn(new W().U8(InOpenReq).I32(RoomKeyNow()).Done());
    }

    /// <summary>
    /// 开桌前把"对面到底连上没有 / RitsuLib 的握手做完了没有"写进日志。
    /// RitsuLib 的广播在"对端没就绪"时是**静默跳过**的，没有这行日志就完全没法定位。
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
                    Entry.Logger.Warn("德州（联机）：主机侧一个客户端都没连上 —— 对面收不到牌桌。");
                    return;
                }
                foreach (var p in peers)
                    Entry.Logger.Info($"德州（联机）：对端 {p.peerId}"
                                      + $" 准备好收广播={p.readyForBroadcasting}"
                                      + $" RitsuLib 能发={RitsuLibSidecarSessionManager.CanSendToPeer(p.peerId)}");
            }
            else if (RunManager.Instance?.NetService is NetClientGameService client)
            {
                var hostId = client.HostNetId;
                Entry.Logger.Info($"德州（联机）：我是客机（我的 id {client.NetId}），主机 {hostId}"
                                  + $" RitsuLib 能发={RitsuLibSidecarSessionManager.CanSendToPeer(hostId)}");
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：查对端状态出错（不影响开桌）: {e.Message}");
        }
    }

    /// <summary>
    /// **主机开桌**：定座位表（主机 = 0 号位，其余按 netId 从小到大）、挑角色、广播出去。
    ///
    /// ⚠️ 顺序很重要：**先广播开桌消息，再把自己的桌子摆起来**。
    /// 反过来的话，主机摆牌时广播的"开这手"会跑在开桌消息前面，客机收到时还没有会话，被丢掉。
    /// </summary>
    public static void HostOpenTable(Player me, Logger logger)
    {
        try
        {
            // 防御：万一把"别人的 Player"传进来了（原版那条"别人选了这一项"的广播重放），
            // 一律换成本机玩家 —— 否则座位表会把别人排成 0 号位。
            if (!DouSpireNet.IsMyPlayer(me))
            {
                var self = LocalMe();
                if (self is not null && self.NetId != me.NetId)
                {
                    logger.Warn("德州：开桌时传进来的不是我（像是别人的篝火选项重放），改用本机玩家。");
                    me = self;
                }
            }

            var players = me.RunState.Players;
            if (players is null || players.Count == 0)
            {
                logger.Warn("德州：拿不到 run 里的玩家列表，退回单人开桌。");
                OpenPending = false;
                DouSpirePokerTable.OpenAtRestSite(me, logger);
                return;
            }

            // 主机自己排第一，其余按 netId 从小到大（所有端算出来都一样）
            var ordered = new List<Player>(players);
            ordered.Sort((a, b) => a.NetId.CompareTo(b.NetId));
            var myIdx = ordered.FindIndex(p => p.NetId == me.NetId);
            if (myIdx > 0) (ordered[0], ordered[myIdx]) = (ordered[myIdx], ordered[0]);

            // ★ 坐几个人：**取"人多"的那个** —— 既要够坐得下 run 里所有人，
            //   也不能比设置里那档（3 人 / 4 人）小。这样"4 个人联机但设置是 3 人桌"不会有人被挤掉。
            var seats = Math.Clamp(Math.Max(ordered.Count, (int)DouSpireConfig.TableSize), 2, MaxSeats);
            SeatCount = seats;

            var room = RoomKeyNow();

            for (var s = 0; s < MaxSeats; s++)
            {
                if (s < Math.Min(seats, ordered.Count))
                {
                    var p = ordered[s];
                    SeatHuman[s] = true;
                    SeatNet[s] = p.NetId;
                    SeatName[s] = PlayerName(p);
                    SeatChar[s] = DouSpireCharacters.PoolIndex(p.Character);
                    SeatPlayerObj[s] = p;      // 主机留着：结算金币时要把输赢写回每个人的真实金币
                }
                else
                {
                    // 人不够 → 电脑补位（3 人桌 + 2 个真人 = 2 真人 + 1 电脑）
                    SeatHuman[s] = false;
                    SeatNet[s] = 0;
                    SeatName[s] = "电脑";
                    SeatChar[s] = -1;
                    SeatPlayerObj[s] = null;
                }
            }

            // 电脑席的角色：主机定，广播出去（保证各端立绘一致）
            var pool = DouSpireCharacters.Pool;
            if (pool.Count > 0)
                for (var s = 0; s < seats; s++)
                    if (!SeatHuman[s]) SeatChar[s] = System.Random.Shared.Next(pool.Count);

            MySeat = 0;
            IsHost = true;
            RoomKey = room;
            Gen++;
            OpenPending = false;
            Active = true;

            if (ordered.Count > seats)
                logger.Warn($"德州：这局有 {ordered.Count} 个人，一桌只坐得下 {seats} 个，多出来的看不了牌。");

            logger.Info("德州：主机开桌 —— "
                        + string.Join("、", Enumerable.Range(0, seats)
                            .Select(s => $"{s}:{SeatName[s]}{(SeatHuman[s] ? "" : "(AI)")}"))
                        + $"，第 {Gen} 代。");

            LogPeerState();
            CastOpen();
            DouSpirePokerTable.OpenForNet(me, logger, stake: true, seats: seats);
        }
        catch (Exception e)
        {
            logger.Warn($"德州：主机开桌失败，退回单人: {e}");
            Active = false;
            OpenPending = false;
            DouSpirePokerTable.OpenAtRestSite(me, logger);
        }
    }

    // ══════════════════════ 开桌意愿收集（"都点了才开始"）══════════════════════

    /// <summary>等多少个人点才算齐 —— = run 里的玩家人数，封顶一桌的座位数。</summary>
    private static int ExpectedClickers(Player me)
    {
        try
        {
            var n = me.RunState?.Players?.Count ?? 1;
            return Math.Max(1, Math.Min(MaxSeats, n));
        }
        catch { return 1; }
    }

    private static readonly HashSet<ulong> _gatherSeen = new();
    private static int _gatherGen;
    private static int _gatherExpected = 1;
    private static bool _gathering;

    /// <summary>等多久。超过就先开（没点的座位由电脑补）—— 永远不能因为一个人挂机就开不了桌。</summary>
    private const double GatherTimeoutSeconds = 25.0;

    /// <summary>**开始收集"大家都点了没"**（主机）。人齐立刻开；超时没齐也开。</summary>
    private static void StartGather(Player me, Logger logger)
    {
        if (_gathering) return;

        _gatherSeen.Clear();
        _gatherSeen.Add(LocalNetId());            // 主机自己这次点击算一份
        _gatherExpected = ExpectedClickers(me);
        _gathering = true;
        var gen = ++_gatherGen;

        logger.Info($"德州：开始收集开桌意愿 —— {_gatherSeen.Count}/{_gatherExpected}"
                    + $"（{GatherTimeoutSeconds:0} 秒后强制开，没点的由电脑补）。");
        Entry.Toast($"等大家一起点「德州扑克」…（{_gatherSeen.Count}/{_gatherExpected}）", "德州扑克");

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
            logger.Warn($"德州：开桌倒计时没起来（不影响，人齐了照样开）: {e.Message}");
        }
    }

    private static void GatherTimeout(int gen)
    {
        if (gen != _gatherGen || !_gathering) return;
        Entry.Logger.Info($"德州：{GatherTimeoutSeconds:0} 秒到了（收到 {_gatherSeen.Count}/{_gatherExpected}）"
                          + "—— 先开，没点的座位由电脑补。");
        FinishGather();
    }

    private static void FinishGather()
    {
        _gathering = false;
        var me = LocalMe();
        if (me is null)
        {
            Entry.Logger.Warn("德州：人齐了却拿不到自己的玩家对象，开不了桌。");
            return;
        }
        HostOpenTable(me, Entry.Logger);
    }

    /// <summary>主机收到"有人想在篝火开桌"。**即使当前没开着桌也要处理** —— 这正是开桌的入口。</summary>
    private static void HostOnOpenReq(ulong senderNetId, int theirRoom)
    {
        if (!LocalIsRunHost()) return;
        if (Active) return;
        if (OpenPending) return;

        var mine = RoomKeyNow();
        if (theirRoom >= 0 && mine >= 0 && theirRoom != mine)
        {
            Entry.Logger.Warn($"德州：{senderNetId} 在别的房间（{theirRoom}）请求开桌，"
                              + $"我现在在 {mine} —— 不算数。");
            return;
        }

        if (_gathering)
        {
            if (_gatherSeen.Add(senderNetId))
            {
                Entry.Logger.Info($"德州：又有一人点了「德州扑克」（{_gatherSeen.Count}/{_gatherExpected}）。");
                Entry.Toast($"等大家一起点「德州扑克」…（{_gatherSeen.Count}/{_gatherExpected}）", "德州扑克");
                if (_gatherSeen.Count >= _gatherExpected)
                {
                    Entry.Logger.Info("德州：人齐了，开桌。");
                    FinishGather();
                }
            }
            return;
        }

        var me = LocalMe();
        if (me is null)
        {
            Entry.Logger.Warn("德州：收到开桌请求，但拿不到自己的玩家对象，开不了。");
            return;
        }

        if (ExpectedClickers(me) <= 1)
        {
            Entry.Logger.Info($"德州：{senderNetId} 请求开桌，主机应允。");
            HostOpenTable(me, Entry.Logger);
            return;
        }
        StartGather(me, Entry.Logger);
        if (_gatherSeen.Add(senderNetId))
            Entry.Toast($"等大家一起点「德州扑克」…（{_gatherSeen.Count}/{_gatherExpected}）", "德州扑克");
    }

    /// <summary>收桌。主机收桌会通知所有人一起收。</summary>
    public static void CloseTable(bool broadcast)
    {
        if (Active && broadcast && IsHost) CastClose();
        Active = false;
        IsHost = false;
        MySeat = 0;
        SeatCount = 3;
        RoomKey = -1;
        OpenPending = false;
        _gathering = false;
        Array.Clear(SeatHuman);
        Array.Clear(SeatNet);
        Array.Clear(SeatChar);
        for (var s = 0; s < MaxSeats; s++) SeatPlayerObj[s] = null;
    }

    /// <summary>当前所在房间的 Id。</summary>
    public static int RoomKeyNow()
    {
        try { return LocalMe()?.RunState?.CurrentRoom?.Id ?? -1; }
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

    private static IReadOnlyList<Player>? RunPlayers()
    {
        try
        {
            var rm = RunManager.Instance;
            if (rm?.NetService is null) return null;
            // RunManager 上没有公开的 State 访问器（是 private），
            // 但工程开了 Krafs.Publicizer，所以这里反射取一次并缓存，最稳。
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

    /// <summary>把我的动作发给主机。**客机自己不应用** —— 等主机的回声。</summary>
    public static void SendAct(PokerAction action, int amount)
        => SendIn(new W().U8(InAct).U8((byte)action).I32(amount).Done());

    /// <summary>我按了"下一手"。</summary>
    public static void SendNextHand() => SendIn(new W().U8(InNextHand).Done());

    /// <summary>我不玩了。</summary>
    public static void SendLeave() => SendIn(new W().U8(InLeave).Done());

    private static bool SendIn(byte[] payload)
    {
        try
        {
            var ok = RitsuLibSidecarHighLevelSend.TrySendAsClient(
                RunManager.Instance, OpIn, payload, RitsuLibSidecarDeliverySemantics.StableSync);
            if (!ok) Entry.Logger.Warn("德州：发给主机的消息没送出去（连接或握手还没就绪）。");
            return ok;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：发消息给主机出错: {e.Message}");
            return false;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  主机 → 所有人
    // ══════════════════════════════════════════════════════════════

    private static void CastOpen()
    {
        var w = new W().I32(Gen).I32(RoomKey).U8((byte)SeatCount);
        for (var s = 0; s < SeatCount; s++)
        {
            w.U8(SeatHuman[s] ? (byte)1 : (byte)0);
            w.U64(SeatNet[s]);
            w.Str(SeatName[s] ?? "？");
            w.I32(SeatChar[s]);
        }
        SendAll(OpOpen, w.Done());
    }

    /// <summary>
    /// **开一手**：把种子、庄家位、筹码、盲注一起广播出去。
    ///
    /// ★ 为什么要连筹码一起发：赌注模式下每人的本金是**按自己身上金币**算的，
    /// 各端算出来的数不一样 —— 而 <c>HoldemGame</c> 要求所有端开局筹码完全相同，
    /// 否则从第一手开始就分叉。所以**由主机算一次、广播给所有人**。
    /// </summary>
    public static void CastHand(int seed, int button, int stack, int smallBlind, int bigBlind)
        => CastAct(new W().U8(ActHand).I32(Gen).I32(seed).U8((byte)button)
                          .I32(stack).I32(smallBlind).I32(bigBlind).Done());

    /// <summary>广播"某人走了这一步"（主机的本地应用和广播是配套的）。</summary>
    public static void CastMove(int globalSeat, PokerAction action, int amount)
        => CastAct(new W().U8(ActMove).U8((byte)globalSeat).U8((byte)action).I32(amount).Done());

    public static void CastSeatAi(int globalSeat)
        => CastAct(new W().U8(ActSeatAi).U8((byte)globalSeat).Done());

    public static void CastClose()
        => CastAct(new W().U8(ActClose).Done());

    private static bool CastAct(byte[] payload)
    {
        try
        {
            var ok = RitsuLibSidecarHighLevelSend.TrySendAsHostBroadcast(
                RunManager.Instance, OpAct, payload, RitsuLibSidecarDeliverySemantics.StableSync);
            if (!ok) Entry.Logger.Warn("德州：广播牌局动作没送出去（对面可能没装 mod 或还没就绪）。");
            return ok;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：广播出错: {e.Message}");
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
            Entry.Logger.Warn($"德州：广播 {opcode} 出错: {e.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  主机侧：接受一步棋
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// 主机接受"某一席走了这一步"：**先在本地应用，成功了再广播**。
    /// 本地应用失败（不合法 / 不是他的回合）就什么都不做 —— 客机会一直等，所以这里必须打日志。
    /// </summary>
    internal static void HostAcceptMove(int globalSeat, PokerAction action, int amount)
    {
        if (!IsHost) return;
        var t = Live;
        if (t is null) { Entry.Logger.Warn("德州：收到动作但桌子不在了，忽略。"); return; }

        if (t.NetMove(globalSeat, action, amount)) CastMove(globalSeat, action, amount);
        else Entry.Logger.Warn($"德州：{globalSeat} 号位的 {action}({amount}) 没被接受，已忽略。");
    }

    /// <summary>主机：把某一席改成电脑接管（离桌 / 超时兜底），并广播。</summary>
    internal static void HostSeatToAi(int globalSeat)
    {
        if (!IsHost) return;
        if (globalSeat < 0 || globalSeat >= SeatCount || !SeatHuman[globalSeat]) return;
        SeatHuman[globalSeat] = false;
        SeatNet[globalSeat] = 0;
        SeatName[globalSeat] = "电脑";
        SeatChar[globalSeat] = -1;
        Entry.Logger.Info($"德州：{globalSeat} 号位离桌，改由电脑接管。");
        CastSeatAi(globalSeat);
        Live?.NetSeatAi(globalSeat);
    }

    // ══════════════════════════════════════════════════════════════
    //  收消息
    // ══════════════════════════════════════════════════════════════

    private static void HandleIn(RitsuLibSidecarDispatchContext ctx)
    {
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

            // 谁发的？按 netId 找他的座位（防冒名：一切以 netId 为准，不看消息里写的座位号）
            var seat = SeatOfNet(ctx.SenderNetId);
            if (seat < 0)
            {
                Entry.Logger.Warn($"德州：收到一个不认识的人（netId {ctx.SenderNetId}）的消息，忽略。");
                return;
            }

            switch (kind)
            {
                case InAct:
                {
                    var action = (PokerAction)r.U8();
                    var amount = r.I32();
                    HostAcceptMove(seat, action, amount);
                    break;
                }
                case InNextHand:
                    Live?.HostNextHand(seat);
                    break;
                case InLeave:
                    HostSeatToAi(seat);
                    break;
                default:
                    Entry.Logger.Warn($"德州：收到不认识的客机消息 kind={kind}。");
                    break;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：处理客机消息出错（不影响别人）: {e}");
        }
    }

    private static void HandleOpen(RitsuLibSidecarDispatchContext ctx)
    {
        try
        {
            if (IsHost) return;   // 主机自己不看这个
            var r = new R(ctx.Payload);
            var gen = r.I32();
            var room = r.I32();
            var n = r.U8();
            if (n < 2 || n > MaxSeats)
            {
                Entry.Logger.Warn($"德州：开桌消息的座位数不对（{n}），忽略。");
                return;
            }

            for (var s = 0; s < MaxSeats; s++)
            {
                SeatHuman[s] = false;
                SeatNet[s] = 0;
                SeatName[s] = "电脑";
                SeatChar[s] = -1;
                SeatPlayerObj[s] = null;
            }
            for (var s = 0; s < n; s++)
            {
                SeatHuman[s] = r.U8() != 0;
                SeatNet[s] = r.U64();
                SeatName[s] = r.Str();
                SeatChar[s] = r.I32();
            }

            // 我在第几号位？按 netId 找（找不到就按 0 处理，至少不崩）
            var mine = -1;
            var myId = LocalNetId();
            for (var s = 0; s < n; s++)
                if (SeatHuman[s] && SeatNet[s] == myId) { mine = s; break; }
            if (mine < 0)
            {
                Entry.Logger.Warn($"德州：开桌消息里没有我（我的 netId {myId}），按 0 号位处理。");
                mine = 0;
            }

            Gen = gen;
            RoomKey = room;
            SeatCount = n;
            MySeat = mine;
            IsHost = false;
            OpenPending = false;
            Active = true;

            Entry.Logger.Info($"德州：主机开桌了（第 {gen} 代）—— 我坐 {mine} 号位，"
                              + string.Join("、", Enumerable.Range(0, n)
                                  .Select(s => $"{s}:{SeatName[s]}{(SeatHuman[s] ? "" : "(AI)")}")));

            var me = LocalMe();
            if (me is null) Entry.Logger.Warn("德州：客机拿不到自己的玩家对象，只能看牌不能下注。");
            DouSpirePokerTable.OpenForNet(me, Entry.Logger, stake: true, seats: n);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：处理开桌消息出错: {e}");
        }
    }

    private static void HandleAct(RitsuLibSidecarDispatchContext ctx)
    {
        try
        {
            if (!Active || IsHost) return;    // 主机自己发的，不用收
            var t = Live;
            var r = new R(ctx.Payload);
            var act = r.U8();
            switch (act)
            {
                case ActHand:
                {
                    var gen = r.I32();
                    var seed = r.I32();
                    var button = r.U8();
                    var stack = r.I32();
                    var sb = r.I32();
                    var bb = r.I32();
                    // 代次不对 = 老消息（换过一桌了），丢掉
                    if (gen != Gen) { Entry.Logger.Info($"德州：收到第 {gen} 代的开手消息（我在第 {Gen} 代），忽略。"); break; }
                    if (t is null) { Entry.Logger.Warn("德州：收到开手消息但桌子还没建起来，忽略。"); break; }
                    t.NetStartHand(seed, button, stack, sb, bb);
                    break;
                }
                case ActMove:
                {
                    // ⚠️ 主机广播的座位号**本来就是全局位**（CastMove/CastSeatAi 都直接发全局位），
                    //    这里**不能再换算一次** —— 早先写成 ToGlobal(ToLocal(g))，对 Move 是恒等
                    //    （看不出来），对 SeatAi 就变成"把全局位当视角位"，座位会错。
                    var seat = r.U8();
                    var action = (PokerAction)r.U8();
                    var amount = r.I32();
                    t?.NetMove(seat, action, amount);
                    break;
                }
                case ActSeatAi:
                {
                    var seat = r.U8();          // 同上：全局位，直接用
                    t?.NetSeatAi(seat);
                    break;
                }
                case ActClose:
                    t?.NetClosed();
                    break;
                default:
                    Entry.Logger.Warn($"德州：收到不认识的动作 act={act}。");
                    break;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：处理动作消息出错: {e}");
        }
    }

    private static int SeatOfNet(ulong netId)
    {
        for (var s = 0; s < SeatCount; s++)
            if (SeatHuman[s] && SeatNet[s] == netId) return s;
        return -1;
    }

    // ══════════════════════════════════════════════════════════════
    //  二进制编解码（自己写，不引任何序列化器 —— 少一处能踩崩的地方）
    // ══════════════════════════════════════════════════════════════

    private sealed class W
    {
        private readonly List<byte> _b = new(32);
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
            if (_i >= _b.Length) throw new InvalidOperationException("德州：网络消息读越界");
            return _b[_i++];
        }

        public int I32()
        {
            if (_i + 4 > _b.Length) throw new InvalidOperationException("德州：网络消息读越界");
            var v = BinaryPrimitives.ReadInt32LittleEndian(_b.AsSpan(_i, 4));
            _i += 4;
            return v;
        }

        public ulong U64()
        {
            if (_i + 8 > _b.Length) throw new InvalidOperationException("德州：网络消息读越界");
            var v = BinaryPrimitives.ReadUInt64LittleEndian(_b.AsSpan(_i, 8));
            _i += 8;
            return v;
        }

        public string Str()
        {
            var n = I32();
            if (n < 0 || _i + n > _b.Length) throw new InvalidOperationException("德州：网络消息读越界");
            var s = Encoding.UTF8.GetString(_b, _i, n);
            _i += n;
            return s;
        }
    }
}
