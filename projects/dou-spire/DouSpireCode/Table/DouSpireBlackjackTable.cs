using Godot;
using System.Threading.Tasks;
using DouSpireRules;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire.Table;

/// <summary>
/// **21 点（Blackjack）牌桌** —— DouSpire 的第三种玩法。
///
/// ★ 设计前提（用户 2026-10-11 拍板）：
/// <list type="bullet">
///   <item>**庄家是电脑荷官** —— 桌上每个人各打各的，**互不干扰**
///     （用户原话「我自己都找不到一起玩的人」→ 单机是主场景）。</item>
///   <item>四个动作：要牌 / 停牌 / 加倍 / 分牌（不做保险、投降）。</item>
///   <item>规则内核在 <see cref="BlackjackGame"/>（纯逻辑，57 条自检 + 2 万局 RTP 99.91%）。</item>
/// </list>
///
/// ★★ 2026-10-11 晚**整桌重做**（用户实测截图：「**这个 21 点模式怎么感觉什么都没有**…
///    **我甚至都不知道哪个是我**」）。
///    病因不是"没素材"—— 素材全都在（立绘 / 头像 / 台面 / 筹码 / 牌背），
///    是**这一版代码根本没画**：只有几个深色文字框，连自己在哪一席都看不出来。
///    这一版补齐四件事：
/// <list type="number">
///   <item>**专属台面** <c>table/table_bj.png</c>（绿绒椭圆 + 三个下注圈；德州那张刻着 5 个公共牌位，不适用）；</item>
///   <item>**三家立绘**（<see cref="DouSpireCharacters.SeatArt"/>）+ **头像信息框**
///     （<see cref="DouSpireCharacters.Icon"/> / 名字 / 筹码 / 本局押注）；</item>
///   <item>**「我」金徽章** —— 挂在我那一席信息框的左上角，外加立绘背后的金色光环
///     （用户："我甚至都不知道哪个是我" ⇒ 这一条是**必须**，不是装饰）；</item>
///   <item>庄家信息框 + 中央横幅（结算 / 提示）压在台面中轴上。</item>
/// </list>
///
/// 摆位全部是**屏幕比例**（1920×1080 基准，见 <c>SeatArtX</c> 那几张表），
/// 换分辨率不跑位；改摆位只动那几张表。
/// </summary>
public sealed partial class DouSpireBlackjackTable : Control
{
    private static DouSpireBlackjackTable? _instance;

    public static bool IsOpen => _instance != null && GodotObject.IsInstanceValid(_instance);

    // ────────────────────────────── 常量 ──────────────────────────────

    /// <summary>桌上几个人（我 + 两个电脑）。21 点里座位之间零交互，加人只影响观感。</summary>
    private const int SeatCount = 3;

    /// <summary>开局每人多少筹码。</summary>
    private const int StartChips = 1000;

    /// <summary>输光之后补多少（德州是"一手一结算"，这里同理：不让任何人真的出局）。</summary>
    private const int RebuyChips = 1000;

    /// <summary>下注档位（点 −/+ 在这些档里跳）。</summary>
    private static readonly int[] BetSteps = { 10, 25, 50, 100, 200, 500, 1000 };

    /// <summary>电脑思考几秒（装作在想）。</summary>
    private const double AiThinkSeconds = 0.9;

    /// <summary>结算画面停几秒再回下注界面。</summary>
    private const double ResultHoldSeconds = 2.6;

    /// <summary>我是 0 号位（和斗地主、德州一致：本地玩家永远坐 0）。</summary>
    private const int MySeat = 0;

    // ══════════════════════════ 摆位表（屏幕比例） ══════════════════════════
    //
    //  座位 0 = **我（下中）**，1 = 右，2 = 左 —— 21 点是"玩家一字排开对着荷官"，
    //  所以我坐在正中、最显眼的位置（用户："我甚至都不知道哪个是我"）。
    //
    //  一席三件套：**立绘**（人物）+ **信息框**（头像/名字/筹码）+ **牌面**（在台面下注圈上）。
    //  三个 x 分别是 0.500 / 0.885 / 0.115 —— 立绘和信息框同列，牌面往桌心收一点。
    //
    //  ⚠️ 台面图 `table_bj.png` 自己带三个下注圈，位置**对不上就改这里**（别去改图）。
    private static readonly float[] SeatArtX = { 0.500f, 0.885f, 0.115f };
    private static readonly float[] SeatArtY = { 0.907f, 0.430f, 0.430f };
    private static readonly float[] SeatHudX = { 0.500f, 0.885f, 0.115f };
    private static readonly float[] SeatHudY = { 0.690f, 0.140f, 0.140f };
    private static readonly float[] SeatCardX = { 0.500f, 0.700f, 0.300f };
    private static readonly float[] SeatCardY = { 0.578f, 0.560f, 0.560f };

    /// <summary>庄家（荷官）信息框与牌面 —— 顶中，正对三家。</summary>
    private const float DealerHudX = 0.500f, DealerHudY = 0.055f;
    private const float DealerCardX = 0.483f, DealerCardY = 0.240f;

    /// <summary>按钮行的高度 / 位置（**别加大** —— 下面是"我"的立绘，加大就压上去）。</summary>
    private const float BtnH = 54f, BtnY = 0.764f;

    /// <summary>
    /// 右上角「身家」那行的**右缘**。
    ///
    /// ★ 2026-10-11：原来 0.980（贴着屏幕右边），为了给**「收桌」圆钮**让位挪到 0.898。
    ///   那颗钮钉在 0.938（62px 宽 ⇒ 左缘 0.922），两者之间还剩 ~46px 空隙。
    ///   想再挪钮的位置，**这条也要跟着动**（不然文字会压到钮上）。
    /// </summary>
    private const float PurseRight = 0.898f;

    /// <summary>
    /// 尺寸（1920 宽基准像素）。
    ///
    /// ⚠️ 这几个数是**算过不打架**的，改之前先算一遍：
    ///   一列从下到上 = 立绘(238) → 按钮行(54) → 信息框(90) → 牌面(牌 101 + 说明 24)，
    ///   再往上就是中央横幅 / 庄家牌。1920×1080 下每条缝只有 8~13px。
    ///   立绘比例 190/238 ≈ 0.80 —— 和 `seats/char_*.png`（0.80~0.86）基本一致，
    ///   所以人物是**满框**的，别把宽高比改歪（改歪了人物会缩在框中间）。
    /// </summary>
    private const float ArtW = 190f, ArtH = 238f;
    private const float HudW = 300f, HudH = 90f;
    private const float CardW = 72f;
    private const float DealerCardW = 92f;

    /// <summary>牌面那一格的容器高度（内容 101+24 ≈ 129，留一点余量）。</summary>
    private const float CardBoxH = 132f;

    // ────────────────────────────── 状态 ──────────────────────────────

    private enum LocalPhase
    {
        /// <summary>下注中，等点「发牌」。</summary>
        Betting,

        /// <summary>已经发牌，桌上在动作（可能轮到我，也可能轮到电脑）。</summary>
        Playing,

        /// <summary>打完了，展示结果。</summary>
        Result,
    }

    /// <summary>
    /// 篝火开桌时挂上来的那个 <see cref="Player"/>（**赌注模式**）。
    /// 有它 = 输赢接金币或血量；没它 = 主菜单那版纯筹码局。
    /// </summary>
    private static Player? _stakePlayer;

    private BlackjackGame? _game;
    private LocalPhase _phase = LocalPhase.Betting;
    private int _seed = 20261011;

    /// <summary>这一桌是不是"拿命赌"（血量模式）。</summary>
    private static bool UseHp => DouSpireConfig.BlackjackUsesHp();

    private readonly int[] _chips = new int[SeatCount];
    private readonly int[] _betOf = new int[SeatCount];
    private readonly string[] _names = new string[SeatCount];
    private readonly Color[] _colors = new Color[SeatCount];

    /// <summary>三家抽到的角色（<see cref="DouSpireCharacters.Assign"/> 固定返回 4 格）。</summary>
    private readonly DouSpireCharacters.SeatPick[] _chars = new DouSpireCharacters.SeatPick[4];

    private int _myBet = 100;
    private int _myTotal;                 // 累计输赢（只做展示，不影响筹码）

    private double _aiWait;
    private double _resultWait;

    // ────────────────────────────── UI 引用 ──────────────────────────────

    private Control _scene = null!;

    private Label _banner = null!;
    private Label _hint = null!;
    private Label _purseLabel = null!;

    private HBoxContainer _dealerCards = null!;
    private Label _dealerTotal = null!;
    private Panel _dealerHud = null!;

    private readonly Panel[] _seatHud = new Panel[SeatCount];
    private readonly Label[] _hudName = new Label[SeatCount];
    private readonly Label[] _hudChips = new Label[SeatCount];
    private readonly VBoxContainer[] _seatCards = new VBoxContainer[SeatCount];
    private readonly TextureRect?[] _seatArt = new TextureRect?[SeatCount];
    private readonly Control?[] _myHalo = new Control?[SeatCount];

    /// <summary>
    /// **各家请上来的活角色**（游戏本体带骨骼的那个）。拿不到就是 null → 退回立绘。
    ///
    /// <para>★★ 2026-10-11 加 ★★ —— 用户："为什么预览是这些静态图。把这些静态图给我删了。
    /// 我用的是本家和那四个二次元人物。"</para>
    ///
    /// <para>21 点这张桌子原来是**唯一一张不请活角色的**：斗地主和德州都让本家角色
    /// 站在桌边（还会攻击 / 受击 / 待机），21 点却只贴一张 <c>seats/char_*.png</c> 静态图，
    /// 于是同一局游戏里 21 点的人物是死的。现在补齐：**本家 5 位请活模型，
    /// 四位客人播待机动画**（<see cref="TickGuestIdle"/>），和另外两张桌子一致。</para>
    /// </summary>
    private readonly DouSpireActor?[] _actors = new DouSpireActor?[SeatCount];

    /// <summary>编外角色待机动画的帧时钟（本体角色走活模型，用不着它）。</summary>
    private readonly DouSpireCharacters.IdleClock _idleClock = new();

    private Button _btnHit = null!;
    private Button _btnStand = null!;
    private Button _btnDouble = null!;
    private Button _btnSplit = null!;
    private Button _btnHint = null!;
    private Button _btnDeal = null!;
    private Button _btnBetDown = null!;
    private Button _btnBetUp = null!;
    private Label _betLabel = null!;

    /// <summary>
    /// **「鸡煲代打」**（2026-10-11 加 —— 用户："现在所有模式都加鸡煲代打"）。
    /// 开着 = AI 按**基本策略**（<see cref="BlackjackAi"/>）替「我」打：
    /// 下注阶段自动按「发牌」，打牌阶段自动要牌 / 停牌 / 加倍 / 分牌。
    ///
    /// **故意用 static** —— 跟斗地主、德州两张桌一个道理：收桌重开也不忘，"一直托管"。
    /// </summary>
    private static bool _autoPilot;

    /// <summary>「鸡煲代打」按钮（跟其它动作按钮同一套底图）。</summary>
    private Button _autoBtn = null!;

    // ── 三颗系统圆钮（★ 2026-10-11 加，见 BuildKnobs 的注释）─────────────

    /// <summary>**右上角那颗「收桌」圆钮** —— 退出这张桌子。</summary>
    private Button _closeBtn = null!;

    /// <summary>带字的「收桌 (ESC)」钮（★ 2026-10-13）—— 圆钮之外的**第二条可见退路**。</summary>
    private Button? _exitBtn;

    /// <summary>牌桌上那两颗圆钮：换牌室场景 / 换牌桌台面。</summary>
    private Button _sceneBtn = null!;
    private Button _tableBtn = null!;

    /// <summary>上面两颗钮下面那行小字（圆钮上不写字，名字放这儿）。</summary>
    private Label? _sceneCap;
    private Label? _tableCap;

    /// <summary>
    /// 整屏背景那一层（<see cref="DouSpireBlackjackTable"/> 的直接子节点、不在 <c>_scene</c> 里）。
    ///
    /// ★ 2026-10-11：存下来是为了「场景」钮能**换它的 <c>Texture</c>**——
    ///   换 Texture 不重建节点，所以**不可能**打乱层级。
    ///   （德州那边是"删了重建 + RestackBase"，那是因为它还要换兜底色；
    ///     21 点背景层结构简单，能省就省 —— 少一次层级操作少一次踩坑机会。）
    /// </summary>
    private TextureRect? _bgLayer;

    /// <summary>台面那一层（永远是 <c>_scene</c> 的最底层，见 <see cref="ApplyTable"/>）。</summary>
    private TextureRect? _felt;

    // ══════════════════════════════ 开 / 关 ══════════════════════════════

    public static void Toggle(Logger logger)
    {
        if (IsOpen) Close(logger);
        else Open(logger);
    }

    public static void Open(Logger logger)
    {
        if (IsOpen) return;
        var root = DouSpireUi.Root;
        if (root == null)
        {
            logger.Warn("21 点：场景树还没准备好，牌桌开不了（稍后再试）。");
            return;
        }
        // ★ 2026-10-13：开桌先清缓存 —— 和斗地主 / 德州对齐（那两张桌一开桌就清）。
        //   21 点原来**一次都不清**，于是两件事全都坏掉：
        //     ① 收桌时登记的音乐压制期（见 DouSpireMusic.Stop）会**一直留着**，
        //        再开桌头两秒没有音乐，看着像"音乐坏了"；
        //     ② 往 music\ 里新丢的歌，不重启游戏永远认不出来。
        DouSpireAssets.ClearCache();
        DouSpireVoice.ClearCache();
        DouSpireMusic.ClearCache();

        _instance = new DouSpireBlackjackTable();
        root.AddChild(_instance);
        DouSpireSfx.Open();
        DouSpireSfx.MusicCalm();
        logger.Info("21 点：牌桌已打开（单人筹码局）。");
    }

    /// <summary>
    /// 收起牌桌（退出 21 点）。
    ///
    /// ★★ 2026-10-13 加固（用户：「**一定要保证能退出房间**」）：
    ///   这一版把"退出"当成**硬需求**来做，不再依赖"那颗圆钮一定能点到"：
    ///   <list type="number">
    ///     <item>**幂等**：`_instance` 已经是 null（或已被销毁）时**照样把音乐/压制收干净**，
    ///       不再像以前那样在 `!IsOpen` 时直接 return —— 那一版如果单例状态错乱，
    ///       就会出现"桌面没了、音乐还在放、再点也收不掉"的死角。</item>
    ///     <item>**先摘单例、再 QueueFree**：单例字段立刻置空，
    ///       于是同一帧内再点一次（或按键）**不会重复走一遍收桌流程**、
    ///       也不会因为 `QueueFree` 是延迟生效而被 `IsOpen` 又判成"还开着"。</item>
    ///     <item>**音乐无条件停**：不管走哪条路进来，音乐与压制都收干净。</item>
    ///   </list>
    /// </summary>
    public static void Close(Logger logger)
    {
        // ① 先把单例摘下来 —— 这一步必须**无条件**做，哪怕下面提前返回。
        var inst = _instance;
        _instance = null;

        // ② 音乐 / 音效无条件收干净（幂等，重复调没有副作用）。
        try { DouSpireSfx.Close(); } catch (System.Exception) { }
        try { DouSpireSfx.MusicStop(); } catch (System.Exception) { }

        // ③ 篝火那条路挂上来的赌注玩家也要松开，不然下次从主菜单开桌会带着旧引用。
        _stakePlayer = null;

        if (inst is null || !GodotObject.IsInstanceValid(inst))
        {
            logger.Info("21 点：没有开着的牌桌，收桌按空操作处理（音乐已一并停掉）。");
            return;
        }

        try { inst.QueueFree(); }
        catch (System.Exception e) { logger.Warn($"21 点：牌桌销毁出错（已尽力收干净）: {e.Message}"); }

        logger.Info("21 点：牌桌已收起。");
    }

    /// <summary>
    /// **键盘退出专用**（ESC / F9 兜底，见 <see cref="DouSpireHotkey"/>）：
    /// 桌子开着就收掉并返回 true；没开着返回 false（让调用方把按键让回给游戏）。
    ///
    /// 单独开一个方法而不是让热键直接调 <see cref="Close"/>，是为了**收得动但不出声**：
    /// 收桌音效留在这条路上照常响，但**日志不会因为"按了一下 ESC 但没桌子"而刷屏**。
    /// </summary>
    public static bool CloseByHotkey()
    {
        if (!IsOpen) return false;
        Close(Entry.Logger);
        return true;
    }

    /// <summary>
    /// **篝火开桌**：把桌上的输赢接到玩家的金币（或血量）上。
    /// </summary>
    public static void OpenAtRestSite(Player? player, Logger logger)
    {
        _stakePlayer = player;
        if (IsOpen) Close(logger);
        Open(logger);
        var what = UseHp ? "血量" : "金币";
        logger.Info($"21 点：篝火开桌（{what}模式），本金"
                    + $" {(_stakePlayer is null ? "（没拿到 Player，退回筹码局）" : what == "血量" ? MyHpNow()?.ToString() ?? "?" : MyGoldNow()?.ToString() ?? "?")}。");
    }

    /// <summary>我身上多少金币（读不到返回 null = 不确定，调用方别拦）。</summary>
    public static int? MyGoldNow()
    {
        try { return _stakePlayer?.Gold; }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"21 点：读金币出错（当成有钱，不拦）: {e.Message}");
            return null;
        }
    }

    /// <summary>我当前多少血（读不到返回 null）。血量模式下这就是"我能押多少"的上限。</summary>
    public static int? MyHpNow()
    {
        try
        {
            var c = _stakePlayer?.Creature;
            return c is null ? null : System.Convert.ToInt32(c.CurrentHp);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"21 点：读血量出错（当成还有血，不拦）: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// **血量结算**：输掉血、赢回血。
    ///
    /// 三条保命线（用户 2026-10-11 拍板时定下的，缺一条都能把自己玩死）：
    /// <list type="number">
    ///   <item>**单次最多掉当前血量的 <see cref="DouSpireConfig.BlackjackHpCapPercent"/>%** —— 再大的注也只掉这么多；</item>
    ///   <item>**永远不会掉到 1 血以下** —— 最低留一口气；</item>
    ///   <item>**回血不超过最大血量** —— 不会溢出。</item>
    /// </list>
    ///
    /// ⚠️ 走 <c>CreatureCmd</c> 而不是直改 <c>CurrentHp</c>：直改会绕过护盾、遗物、
    /// 各种「受到伤害时」的钩子，别的 mod 也感知不到。
    /// 掉血用 <c>ValueProp.Unblockable</c>（= 生命流失，像中毒那样）——
    /// 21 点的输赢不该被护盾挡掉，也不该触发"被攻击"那一类反应。
    /// </summary>
    private static async Task ApplyHpAsync(int net)
    {
        var c = _stakePlayer?.Creature;
        if (c is null || net == 0) return;

        try
        {
            var cur = System.Convert.ToInt32(c.CurrentHp);
            var max = System.Convert.ToInt32(c.MaxHp);

            if (net < 0)
            {
                var want = -net;
                var byPercent = Math.Max(1, cur * DouSpireConfig.BlackjackHpCapPercent / 100);
                var byLife = Math.Max(0, cur - 1);           // 至少留 1 血
                var lose = Math.Min(want, Math.Min(byPercent, byLife));
                if (lose <= 0) return;

                // ★ 用 7 参那个重载：它才允许 dealer 传 null（5 参版的 dealer 是非空 Creature）。
                await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), c, lose,
                    ValueProp.Unblockable, null, null, null);
                Entry.Logger.Info($"21 点（血量）：掉 {lose} 血（本该掉 {want}，被保命线压到 {lose}）"
                                  + $" —— {cur} → {System.Convert.ToInt32(c.CurrentHp)}");
            }
            else
            {
                var gain = Math.Min(net, Math.Max(0, max - cur));
                if (gain <= 0) return;

                await CreatureCmd.Heal(c, gain);
                Entry.Logger.Info($"21 点（血量）：回 {gain} 血 —— {cur} → {System.Convert.ToInt32(c.CurrentHp)}");
            }
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"21 点：血量改动失败（这局的钱照算，血没动）: {e.Message}");
        }
    }

    // ══════════════════════════════ 建界面 ══════════════════════════════

    public override void _Ready()
    {
        SetMeta(DouSpireUi.MetaTag, true);
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 200;
        MouseFilter = MouseFilterEnum.Stop;

        FitToViewport();
        DouSpireUi.UseScreen(this);

        // —— 背景：复用德州那几间房 ——
        //   ★ 2026-10-11：存下这一层，「场景」钮换的是它的 Texture（不重建节点）。
        _bgLayer = DouSpireUi.Backdrop(DouSpireAssets.PokerBgOf(DouSpireConfig.PokerScene));
        if (_bgLayer != null) AddChild(_bgLayer);
        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.30f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(DouSpireUi.Fill(dim));

        // —— 绝对摆位层：以下所有东西都按屏幕比例钉在这上面 ——
        _scene = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(DouSpireUi.Fill(_scene));

        for (var s = 0; s < SeatCount; s++) _chips[s] = StartChips;
        PickCharacters();

        ApplyTable();
        BuildDealer();
        BuildSeats();
        BuildHeader();
        BuildFooter();
        BuildKnobs();       // ★ 2026-10-11：21 点原来**一颗系统钮都没有**（用户："没有退出的按钮啊"）

        Refresh();
    }

    /// <summary>
    /// 铺**当前牌桌风格**的台面（<see cref="DouSpireConfig.PokerTableStyle"/>，和德州共用同一个值）。
    ///
    /// <para>★ 2026-10-11 从 <c>BuildFelt()</c> 改名 —— 因为牌桌上多了颗「牌桌」钮要**当场换**
    /// （见 <see cref="CycleTable"/>），"搭场景时铺一次"这个名字就不对了。</para>
    ///
    /// <para>⚠️ **必须 <c>MoveChild(_felt, 0)</c>** —— 台面是 <c>_scene</c> 的最底层，
    /// 而"换台面"是删了重加、新节点落在末尾；不挪回去就会盖住整桌 UI。
    /// （同一类坑：德州"切桌子黑屏"就是写死绝对索引 —— 这里用**相对 0**，安全。）</para>
    /// </summary>
    private void ApplyTable()
    {
        if (_felt is not null && GodotObject.IsInstanceValid(_felt))
        {
            if (_felt.GetParent() == _scene) _scene.RemoveChild(_felt);
            _felt.QueueFree();
        }
        _felt = null;
        if (_scene is null) return;

        var chain = DouSpireAssets.BlackjackTableOf(DouSpireConfig.PokerTableStyle);
        var tex = DouSpireAssets.First(chain);

        // ★ 高度钉死 846，宽度按图自己的宽高比 —— 换图不会把台面拉变形。
        const float tableH = 846f;
        var tableW = tex is not null && tex.GetHeight() > 0
            ? tableH * tex.GetWidth() / tex.GetHeight()
            : 1580f;

        _felt = DouSpireUi.Sprite(chain, 0.5f, 0.450f, DouSpireUi.Px(tableW), DouSpireUi.Px(tableH));
        if (_felt is null) return;
        _scene.AddChild(_felt);
        _scene.MoveChild(_felt, 0);     // 垫到最底下（立绘 / 信息框 / 按钮全在它上面）
    }

    /// <summary>庄家（荷官）：顶中一个信息框 + 牌面。</summary>
    private void BuildDealer()
    {
        _dealerHud = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        _dealerHud.AddThemeStyleboxOverride("panel",
            DouSpireUi.Box(new Color(0.05f, 0.06f, 0.08f, 0.78f), 12, DouSpireUi.Gold, 2));
        DouSpireUi.Place(_dealerHud, DealerHudX, DealerHudY, DouSpireUi.Px(320f), DouSpireUi.Px(HudH));

        var box = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        box.AddThemeConstantOverride("separation", DouSpireUi.Gap(2));
        _dealerHud.AddChild(DouSpireUi.Fill(Pad(box, 8f)));

        var who = new Label
        {
            Text = "庄 家",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        DouSpireUi.StyleFont(who);
        who.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        who.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(21));
        box.AddChild(who);

        _dealerTotal = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        DouSpireUi.StyleFont(_dealerTotal);
        _dealerTotal.AddThemeColorOverride("font_color", new Color(0.96f, 0.93f, 0.84f));
        _dealerTotal.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(22));
        box.AddChild(_dealerTotal);

        _scene.AddChild(_dealerHud);

        // —— 庄家牌（在台面顶上那个小圈上）——
        _dealerCards = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        _dealerCards.AddThemeConstantOverride("separation", DouSpireUi.Gap(8));
        DouSpireUi.Place(_dealerCards, DealerCardX, DealerCardY,
            DouSpireUi.Px(700f), DouSpireUi.Px(DealerCardW * 1.4f + 20f));
        _scene.AddChild(_dealerCards);
    }

    /// <summary>三家：立绘 + 活角色 + 信息框（头像/名字/筹码/我徽章）+ 牌面。</summary>
    private void BuildSeats()
    {
        var parentW = Size.X > 8f ? Size.X : 1920f;
        var parentH = Size.Y > 8f ? Size.Y : 1080f;

        for (var s = 0; s < SeatCount; s++)
        {
            // ── ① 立绘（我在正中，另外两家左右）——
            //   ★ 2026-10-11：节点**无条件建出来**，图可能先给 null ——
            //     `seats/char_*.png` 删掉之后本家角色这一格取不到图，但节点得在，
            //     待机动画靠它贴帧、金色光环靠它算层级。
            var art = new TextureRect
            {
                Texture = DouSpireCharacters.SeatArt(_chars[s]),
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            DouSpireUi.Place(art, SeatArtX[s], SeatArtY[s], DouSpireUi.Px(ArtW), DouSpireUi.Px(ArtH));
            _scene.AddChild(art);
            _seatArt[s] = art;

            // ── ② 活角色（游戏本体带骨骼的那个）——
            //   ★★ 2026-10-11 加 ★★ 21 点原来是**三张桌子里唯一一张不请活角色的**：
            //     斗地主 / 德州都让本家角色站在桌边（待机 / 受击 / 亮牌），21 点却只贴一张
            //     `seats/char_*.png` 静态图 —— 用户看到的"静态图"就是它。
            //     现在本家 5 位请活模型，四位客人走待机动画（见 TickGuestIdle）。
            if (DouSpireConfig.LiveActors && _chars[s].Model is not null)
            {
                try
                {
                    var footX = SeatArtX[s] * parentW;
                    var footY = SeatArtY[s] * parentH
                                + DouSpireUi.Px(ArtH) * 0.5f
                                + DouSpireConfig.ActorYOffset * DouSpireUi.Screen;

                    // targetH 只给"想要多高"，ActorScale 由 Spawn 内部的 FitHeight 自己乘。
                    _actors[s] = DouSpireActor.Spawn(
                        _scene, _chars[s].Model!, new Vector2(footX, footY),
                        DouSpireUi.Px(ArtH),
                        DouSpireConfig.ActorFaceCenter && SeatArtX[s] > 0.5f);
                    if (_actors[s] is not null) art.Visible = false;   // 活人在 → 立绘让位
                }
                catch (System.Exception e)
                {
                    Entry.Logger.Warn($"21点：第 {s} 席的活角色没请上来，用立绘顶着: {e.Message}");
                    _actors[s] = null;
                }
            }

            // ── ③ 立绘背后的金色光环（**只有我这一席有** —— 一眼认出自己在哪）——
            if (s == MySeat)
            {
                var halo = new Panel { MouseFilter = MouseFilterEnum.Ignore };
                halo.AddThemeStyleboxOverride("panel",
                    DouSpireUi.BoxGlow(new Color(1f, 0.82f, 0.30f, 0.10f), 200, new Color(1f, 0.82f, 0.30f, 0.85f), 3));
                DouSpireUi.Place(halo, SeatArtX[s], SeatArtY[s],
                    DouSpireUi.Px(ArtW + 26f), DouSpireUi.Px(ArtH + 26f));
                _scene.AddChild(halo);
                _myHalo[s] = halo;
                // 垫到立绘下面。★ 用 art 自己的索引而不是写死 `GetChildCount() - 2` ——
                //   中间多插了活角色节点之后，写死的偏移会指错（同一类坑见 RestackBase）。
                _scene.MoveChild(halo, art.GetIndex());
            }

            // ── ③ 信息框：头像 + 名字 + 筹码/押注 ——
            var hud = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            hud.AddThemeStyleboxOverride("panel",
                DouSpireUi.Box(new Color(0.05f, 0.06f, 0.08f, 0.78f), 12,
                    s == MySeat ? new Color(1f, 0.84f, 0.30f) : new Color(0.55f, 0.48f, 0.32f),
                    s == MySeat ? 3 : 2));
            DouSpireUi.Place(hud, SeatHudX[s], SeatHudY[s], DouSpireUi.Px(HudW), DouSpireUi.Px(HudH));
            _seatHud[s] = hud;

            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", DouSpireUi.Gap(8));
            hud.AddChild(DouSpireUi.Fill(Pad(row, 9f)));

            var avatar = DouSpireCharacters.Icon(_chars[s]);
            if (avatar != null)
            {
                var av = new TextureRect
                {
                    Texture = avatar,
                    MouseFilter = MouseFilterEnum.Ignore,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                    CustomMinimumSize = new Vector2(DouSpireUi.Px(64f), DouSpireUi.Px(64f)),
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                };
                row.AddChild(av);
            }

            var col = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Center,
            };
            col.AddThemeConstantOverride("separation", DouSpireUi.Gap(2));
            row.AddChild(col);

            _hudName[s] = HudLabel(col, 19, DouSpireUi.Gold);
            _hudChips[s] = HudLabel(col, 16, new Color(0.72f, 0.86f, 0.78f));

            // 「我」金徽章 —— 挂信息框左上角外侧，**只有我这一席有**
            if (s == MySeat)
            {
                var bw = DouSpireUi.Px(52f);
                var bh = DouSpireUi.Px(52f);
                var badge = new Panel { MouseFilter = MouseFilterEnum.Ignore };
                badge.AnchorLeft = badge.AnchorRight = 0f;
                badge.AnchorTop = badge.AnchorBottom = 0f;
                badge.OffsetLeft = -bw * 0.30f;
                badge.OffsetTop = -bh * 0.30f;
                badge.OffsetRight = badge.OffsetLeft + bw;
                badge.OffsetBottom = badge.OffsetTop + bh;
                badge.AddThemeStyleboxOverride("panel",
                    DouSpireUi.Box(new Color(1f, 0.84f, 0.30f), 200, new Color(0.30f, 0.20f, 0.03f), 3));

                var bl = new Label
                {
                    Text = "我",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                DouSpireUi.StyleFont(bl);
                bl.AddThemeColorOverride("font_color", new Color(0.14f, 0.09f, 0.02f));
                bl.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(26));
                badge.AddChild(DouSpireUi.Fill(bl));
                hud.AddChild(badge);
            }

            _scene.AddChild(hud);

            // ── ④ 牌面（落在台面的下注圈上）——
            _seatCards[s] = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _seatCards[s].AddThemeConstantOverride("separation", DouSpireUi.Gap(4));
            DouSpireUi.Place(_seatCards[s], SeatCardX[s], SeatCardY[s],
                DouSpireUi.Px(680f), DouSpireUi.Px(CardBoxH));
            _scene.AddChild(_seatCards[s]);
        }
    }

    /// <summary>标题（左上）+ 身家（右上）。</summary>
    private void BuildHeader()
    {
        var title = new Label { Text = "21 点 · BLACKJACK" };
        DouSpireUi.StyleFont(title);
        title.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        title.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(30));
        PlaceTopLeft(title, 0.020f, 0.028f, 520f, 46f);
        _scene.AddChild(title);

        _purseLabel = new Label { HorizontalAlignment = HorizontalAlignment.Right };
        DouSpireUi.StyleFont(_purseLabel);
        _purseLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.93f, 0.84f));
        _purseLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(19));
        // ★ 2026-10-11：右缘从 0.980 挪到 0.898 —— 给右上角那颗「收桌」圆钮让位
        //   （那颗钮钉在 0.938，62px 宽 ⇒ 左缘 0.922；0.898 与它之间还有 ~46px 空隙）。
        PlaceTopRight(_purseLabel, PurseRight, 0.032f, 520f, 40f);
        _scene.AddChild(_purseLabel);
    }

    /// <summary>
    /// 桌上那三颗系统圆钮（★ 2026-10-11 加）。
    ///
    /// <para>★★ 为什么加 ★★ 用户：「**你这个没有退出的按钮啊。黑jk**」
    ///   21 点原来是**三张桌子里唯一一张一颗系统钮都没有的**：
    ///   斗地主左上角有一排（室内 / 桌面 / 牌面 / **收桌**），德州右上角有「收桌」文字钮、
    ///   台面上还有「场景 / 牌桌」两颗圆钮 —— 21 点什么都没有，进来就只能靠关游戏出来。</para>
    ///
    /// <para>顺手把德州那两颗也补上：21 点本来就和德州**共用**同一套
    ///   <see cref="DouSpireConfig.PokerScene"/> / <see cref="DouSpireConfig.PokerTableStyle"/>
    ///   （背景走 <c>PokerBgOf(PokerScene)</c>、台面走 <c>BlackjackTableOf(PokerTableStyle)</c>），
    ///   却没有任何地方能改，很不合理。</para>
    ///
    /// <para>**摆位是算过不打架的**（1920×1080 基准，把这一桌已经占了地方的全列一遍）：
    ///   左席立绘 x 126~316 / 右席立绘 x 1604~1794（y 345~583）；
    ///   左席信息框 x 37~193 / 右席 x 1699~1851（y 98~182）；
    ///   左席牌面容器中心 576、右席 1004（容器 680 宽，实际牌只有 ~150 宽）；
    ///   按钮行 x 380~1540（y 769~823）；我的立绘 x 865~1055（y 861+）。
    ///   两颗钮直径 62 ⇒ 左 426~488、右 1432~1494，y 514~576 —— **一个都不碰**，
    ///   左右对称，而且正落在台面上（不是浮在屏幕角上）。</para>
    ///
    /// <para>⚠️ 想挪位置就改下面两行 <c>Place(...)</c> 的 ax / ay；改一个记得改另一个（对称才好看）。
    ///   预览脚本：<c>_work/preview-bj-layout.py</c>（改完坐标记得同步改那边）。</para>
    /// </summary>
    private void BuildKnobs()
    {
        var knob = DouSpireUi.Px(DouSpireKnob.Size);

        // —— ① 换牌室场景 / ② 换牌桌台面：左右对称，落在台面上 ——
        //   长相直接抄 DouSpireKnob（= 斗地主 / 德州那套小圆钮，中性香槟金，
        //   "和每张桌子配都不奇怪"）。钮上不写字，名字在钮下面那行小字里。
        _sceneBtn = DouSpireKnob.Make(DouSpireKnobIcon.Kind.Room, CycleScene, out _sceneCap,
                                      tooltip: "换牌室场景");
        _scene.AddChild(DouSpireUi.Place(_sceneBtn, 0.238f, 0.505f, knob, knob));

        _tableBtn = DouSpireKnob.Make(DouSpireKnobIcon.Kind.Desk, CycleTable, out _tableCap,
                                      tooltip: "换牌桌台面");
        _scene.AddChild(DouSpireUi.Place(_tableBtn, 0.762f, 0.505f, knob, knob));

        // —— ③ 收桌（退出）：单独放**右上角** ——
        //   退出类操作不该埋在台面上跟玩法按钮混在一起（误点一下这局就没了）。
        //   ⚠️ 提示语**别写"回到篝火"** —— 21 点从主菜单也能开（`Open`），
        //      那种情况下点了只是收桌，没地方可回。
        _closeBtn = DouSpireKnob.Make(DouSpireKnobIcon.Kind.Close, () => Close(Entry.Logger),
                                      out var closeCap, tooltip: "收起桌面（退出 21 点）");
        _scene.AddChild(DouSpireUi.Place(_closeBtn, 0.938f, 0.035f, knob, knob));
        closeCap?.SetText("收桌");

        BuildExitTextButton();

        UpdateSceneBtn();
        UpdateTableBtn();
    }

    /// <summary>
    /// **带字的「收桌」钮** —— 圆钮之外的**第二条可见退路**（★ 2026-10-13 加）。
    ///
    /// <para>★★ 为什么在已经有一颗圆钮的情况下还要再加一个 ★★
    ///   用户 2026-10-13：「**一定要保证能退出房间**」。
    ///   那颗圆钮有一个致命弱点：**它是个图标钮，上面的字只有悬停提示才看得到**。
    ///   万一它被别的层挡住、或者玩家根本没认出来那是"退出"，人就出不去 ——
    ///   三张桌子里这是唯一"出不去就等于关游戏"的地方，代价太大。</para>
    ///
    /// <para>所以补三样：**带"收桌"两个字的钮**（一眼看得见）、**写着 ESC 的提示**
    ///   （键盘也能退，见 <see cref="DouSpireHotkey"/>）、以及**压在屏幕右上角最外层**
    ///   （层级上不可能被台面 / 立绘 / 横幅盖住）。</para>
    ///
    /// <para>⚠️ **不要把它挪进 <c>_scene</c>** —— 它现在是 <c>this</c> 的直接子节点、
    ///   排在 <c>_scene</c> 之后，取的就是"永远在最上面"这个效果。</para>
    /// </summary>
    private void BuildExitTextButton()
    {
        var btn = new Button
        {
            Text = "收桌  (ESC)",
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(150f), DouSpireUi.Px(42f)),
            TooltipText = "收起桌面，退出 21 点（按 ESC 也一样）",
        };
        btn.Pressed += () => { DouSpireSfx.Select(); Close(Entry.Logger); };
        btn.MouseEntered += DouSpireSfx.Hover;

        if (!DouSpireUi.ImageButton(btn, DouSpireAssets.BtnPrimary, 12, 24, 10)
            && !DouSpireUi.ApplyGradButton(btn, 10))
        {
            btn.AddThemeStyleboxOverride("normal", DouSpireUi.Box(DouSpireUi.Panel, 10, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("hover", DouSpireUi.Box(new Color(0.20f, 0.15f, 0.09f, 0.96f), 10, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("pressed", DouSpireUi.Box(DouSpireUi.Panel, 10, DouSpireUi.Gold, 3));
        }
        btn.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        btn.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(17));

        // 钉在右上角：圆钮在 (0.938, 0.035)、直径 62 ⇒ 这颗摆在它**正下方**一点，
        // 两者留 10px 缝，避免叠在一起互相挡。
        DouSpireUi.Place(btn, 0.938f, 0.100f, DouSpireUi.Px(150f), DouSpireUi.Px(42f));
        AddChild(btn);         // ★ 直接挂在自己身上（不在 _scene 里）⇒ 永远压在最上层
        _exitBtn = btn;
    }

    /// <summary>轮换牌室场景（台面右上那颗「场景」钮）。换完**当场生效**，不用收桌重开。</summary>
    private void CycleScene()
    {
        var all = DouSpireScenes.All;
        var i = System.Array.IndexOf(all, DouSpireConfig.PokerScene);
        DouSpireConfig.PokerScene = all[(i + 1 + all.Length) % all.Length];
        Entry.Logger.Info($"21 点：室内（牌室场景）换成「{DouSpireAssets.SceneName(DouSpireConfig.PokerScene)}」"
                          + $"（图 = assets/bg/{DouSpireAssets.SceneKey(DouSpireConfig.PokerScene)}.png）。");
        ApplyScene();
        UpdateSceneBtn();
    }

    /// <summary>
    /// 把背景换成当前场景那张图。
    ///
    /// ★ 只改 <c>Texture</c>、**不重建节点** —— 背景是这张桌子的直接子节点（不在 <c>_scene</c> 里），
    ///   重建要重新排层级；换贴图则一行搞定，而且**不可能**把 UI 盖掉。
    /// </summary>
    private void ApplyScene()
    {
        if (_bgLayer is null || !GodotObject.IsInstanceValid(_bgLayer)) return;
        var tex = DouSpireAssets.First(DouSpireAssets.PokerBgOf(DouSpireConfig.PokerScene));
        if (tex is null) return;         // 这张图没有 → 留着旧的那张，别把背景搞成空白
        _bgLayer.Texture = tex;
    }

    private void UpdateSceneBtn()
    {
        if (_sceneBtn is null || !GodotObject.IsInstanceValid(_sceneBtn)) return;
        _sceneBtn.TooltipText = "换牌室场景 —— 现在：" + DouSpireAssets.SceneName(DouSpireConfig.PokerScene);
        if (_sceneCap is null || !GodotObject.IsInstanceValid(_sceneCap)) return;
        _sceneCap.Text = DouSpireAssets.SceneShortName(DouSpireConfig.PokerScene);
    }

    /// <summary>轮换牌桌台面（台面右上那颗「牌桌」钮）。换完**当场生效**。</summary>
    private void CycleTable()
    {
        var all = DouSpireTableStyles.All;
        var i = System.Array.IndexOf(all, DouSpireConfig.PokerTableStyle);
        DouSpireConfig.PokerTableStyle = all[(i + 1 + all.Length) % all.Length];
        Entry.Logger.Info($"21 点：牌桌台面换成「{DouSpireAssets.TableName(DouSpireConfig.PokerTableStyle)}」"
                          + $"（图 = assets/table/{DouSpireAssets.TableKey(DouSpireConfig.PokerTableStyle)}.png）。");
        ApplyTable();
        UpdateTableBtn();
    }

    private void UpdateTableBtn()
    {
        if (_tableBtn is null || !GodotObject.IsInstanceValid(_tableBtn)) return;
        _tableBtn.TooltipText = "换牌桌台面 —— 现在：" + DouSpireAssets.TableName(DouSpireConfig.PokerTableStyle);
        if (_tableCap is null || !GodotObject.IsInstanceValid(_tableCap)) return;
        _tableCap.Text = DouSpireAssets.TableShortName(DouSpireConfig.PokerTableStyle);
    }

    /// <summary>中央横幅 + 提示 + 按钮行。</summary>
    private void BuildFooter()
    {
        _banner = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        DouSpireUi.StyleFont(_banner);
        _banner.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.35f));
        _banner.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(26));
        DouSpireUi.Place(_banner, 0.500f, 0.360f, DouSpireUi.Px(1000f), DouSpireUi.Px(64f));
        _scene.AddChild(_banner);

        _hint = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        DouSpireUi.StyleFont(_hint);
        _hint.AddThemeColorOverride("font_color", new Color(0.82f, 0.84f, 0.88f));
        _hint.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(17));
        DouSpireUi.Place(_hint, 0.500f, 0.432f, DouSpireUi.Px(1200f), DouSpireUi.Px(56f));
        _scene.AddChild(_hint);

        // —— 按钮行：**一行动态换内容**（下注时露发牌/−/+，打牌时露要牌/停牌/…）——
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        row.AddThemeConstantOverride("separation", DouSpireUi.Gap(10));
        DouSpireUi.Place(row, 0.500f, BtnY, DouSpireUi.Px(1160f), DouSpireUi.Px(BtnH));
        _scene.AddChild(row);

        _btnBetDown = ActButton(row, "−", OnBetDown, small: true);
        _betLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        DouSpireUi.StyleFont(_betLabel);
        _betLabel.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        _betLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(22));
        _betLabel.CustomMinimumSize = new Vector2(DouSpireUi.Px(220f), DouSpireUi.Px(0f));
        row.AddChild(_betLabel);
        _btnBetUp = ActButton(row, "+", OnBetUp, small: true);

        _btnDeal = ActButton(row, "发 牌", OnDeal);
        _btnHit = ActButton(row, "要牌", OnHit);
        _btnStand = ActButton(row, "停牌", OnStand);
        _btnDouble = ActButton(row, "加倍", OnDouble);
        _btnSplit = ActButton(row, "分牌", OnSplit);
        _btnHint = ActButton(row, "提示", OnHint);
        // 「鸡煲代打」（2026-10-11）：点一下 = AI 按基本策略替我把这一局（含发牌）打完；再点一下收回。
        _autoBtn = ActButton(row, _autoPilot ? "✓ 代打中" : "鸡煲代打", ToggleAutoPilot);
    }

    private static Label HudLabel(Container parent, int size, Color color)
    {
        var l = new Label { HorizontalAlignment = HorizontalAlignment.Left, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        DouSpireUi.StyleFont(l);
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(size));
        parent.AddChild(l);
        return l;
    }

    private static Button ActButton(Container parent, string text, System.Action onPressed, bool small = false)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(small ? 60 : 150), DouSpireUi.Px(BtnH)),
        };
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        btn.MouseEntered += DouSpireSfx.Hover;
        if (!DouSpireUi.ImageButton(btn, DouSpireAssets.BtnPrimary, 14, 30, 12)
            && !DouSpireUi.ApplyGradButton(btn, 12))
        {
            btn.AddThemeStyleboxOverride("normal", DouSpireUi.Box(DouSpireUi.Panel, 12, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("hover", DouSpireUi.Box(new Color(0.16f, 0.14f, 0.10f, 0.95f), 12, DouSpireUi.Gold, 2));
        }
        btn.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        btn.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(small ? 24 : 21));
        parent.AddChild(btn);
        return btn;
    }

    /// <summary>把内容塞进一个四边留 <paramref name="m"/> 像素边距的盒子（信息框里别贴着边）。</summary>
    private static MarginContainer Pad(Control inner, float m)
    {
        var mc = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            mc.AddThemeConstantOverride(side, DouSpireUi.Gap(m));
        mc.AddChild(inner);
        return mc;
    }

    /// <summary>以 (ax, ay) 为**左上角**摆一个固定尺寸的控件（比例锚点）。</summary>
    private static T PlaceTopLeft<T>(T c, float ax, float ay, float w, float h) where T : Control
    {
        c.AnchorLeft = c.AnchorRight = ax;
        c.AnchorTop = c.AnchorBottom = ay;
        c.OffsetLeft = 0f;
        c.OffsetTop = 0f;
        c.OffsetRight = DouSpireUi.Px(w);
        c.OffsetBottom = DouSpireUi.Px(h);
        return c;
    }

    /// <summary>以 (ax, ay) 为**右上角**摆一个固定尺寸的控件（比例锚点）。</summary>
    private static T PlaceTopRight<T>(T c, float ax, float ay, float w, float h) where T : Control
    {
        c.AnchorLeft = c.AnchorRight = ax;
        c.AnchorTop = c.AnchorBottom = ay;
        c.OffsetLeft = -DouSpireUi.Px(w);
        c.OffsetTop = 0f;
        c.OffsetRight = 0f;
        c.OffsetBottom = DouSpireUi.Px(h);
        return c;
    }

    // ══════════════════════════════ 角色 ══════════════════════════════

    /// <summary>
    /// 给三个座位挑角色。拿得到本体角色就用本体（含 mod 角色），拿不到就退成"你 / 电脑甲 / 电脑乙"。
    /// 21 点里座位之间不打照面，所以角色只影响**名字、颜色、立绘和头像**，不影响玩法。
    /// </summary>
    private void PickCharacters()
    {
        var picks = DouSpireCharacters.Assign(default, _seed, SeatCount);
        for (var s = 0; s < _chars.Length; s++) _chars[s] = picks[s];

        for (var s = 0; s < SeatCount; s++)
        {
            if (!_chars[s].IsEmpty)
            {
                _names[s] = DouSpireCharacters.Name(_chars[s]);
                _colors[s] = DouSpireCharacters.ColorOf(_chars[s]);
            }
            else
            {
                _names[s] = s == MySeat ? "你" : $"电脑{s}";
                _colors[s] = new Color(0.75f, 0.70f, 0.55f);
            }
        }
    }

    // ══════════════════════════════ 一局的流程 ══════════════════════════════

    /// <summary>开新的一局：收注 → 发牌。</summary>
    private void StartRound()
    {
        // 血量模式：我这一席的"筹码"就是**我现在的血量**（押注上限 = 血量，且永远留 1 血）
        if (UseHp)
        {
            var hp = MyHpNow();
            if (hp is > 0) _chips[MySeat] = hp.Value;
        }

        for (var s = 0; s < SeatCount; s++)
        {
            if (_chips[s] <= 0) _chips[s] = RebuyChips;        // 输光了自动补，不让谁出局

            if (s == MySeat)
            {
                var want = UseHp ? DouSpireConfig.BlackjackBaseStake : _myBet;
                var room = UseHp ? Math.Max(1, _chips[s] - 1) : _chips[s];   // 血量模式至少留 1 血
                _betOf[s] = Math.Min(want, room);
            }
            else
            {
                _betOf[s] = Math.Min(AiBetAmount(s), _chips[s]);
            }
        }

        _game = new BlackjackGame(SeatCount, _seed++);
        for (var s = 0; s < SeatCount; s++)
        {
            _game.Seats[s].IsAi = s != MySeat;
            _game.PlaceBet(s, Math.Max(1, _betOf[s]));
        }

        _game.Deal();
        DouSpireSfx.Deal();

        _phase = _game.IsOver ? LocalPhase.Result : LocalPhase.Playing;
        _aiWait = AiThinkSeconds;
        _resultWait = ResultHoldSeconds;
        Entry.Logger.Info($"21 点：第 {_seed} 局发牌（我押 {_betOf[MySeat]}）。");
        Refresh();
    }

    /// <summary>电脑下多少注：底注上下浮动，看着像有人性。</summary>
    private int AiBetAmount(int seat)
    {
        var roll = (_seed * 7 + seat * 31 + _chips[seat]) % 100;
        return roll < 55 ? 100 : roll < 85 ? 200 : 500;
    }

    /// <summary>结算并收钱。</summary>
    private void SettleRound()
    {
        if (_game is null) return;
        for (var s = 0; s < SeatCount; s++)
        {
            var net = _game.Seats[s].Net;
            _chips[s] = Math.Max(0, _chips[s] + net);
            if (s != MySeat) continue;

            _myTotal += net;

            // 只有篝火开桌（有 _stakePlayer）才真的动金币 / 血量；主菜单那版是纯筹码局
            if (_stakePlayer is null) continue;

            if (UseHp)
            {
                // 拿命赌：走 CreatureCmd（不直改 CurrentHp），三条保命线在 ApplyHpAsync 里
                _ = ApplyHpAsync(net);
            }
            else
            {
                try { _stakePlayer.Gold = Math.Max(0, _stakePlayer.Gold + net); }
                catch (System.Exception e)
                {
                    Entry.Logger.Warn($"21 点：金币结算失败（这局的输赢记在桌上了）: {e.Message}");
                }
            }
        }

        var mine = _game.Seats[MySeat];
        if (mine.Net > 0) { DouSpireSfx.Win(); DouSpireSfx.MusicTense(); }
        else if (mine.Net < 0) { DouSpireSfx.Lose(); }

        _phase = LocalPhase.Result;
        _resultWait = ResultHoldSeconds;
        Refresh();

        var gotBj = false;
        foreach (var h in mine.Hands) if (h.IsBlackjack) gotBj = true;
        Entry.Logger.Info($"21 点：本局我净 {mine.Net}（{(gotBj ? "含黑杰克" : "普通")}），筹码 {_chips[MySeat]}。");
    }

    // ══════════════════════════════ 每帧 ══════════════════════════════

    /// <summary>
    /// **编外角色的待机动画** —— 本体角色走游戏自己的活模型（<see cref="BuildSeats"/> 里请），
    /// 四位客人没有模型，只能靠她们自己那 96 帧循环
    /// （<c>assets/&lt;id&gt;/idle/fNN.png</c>，12fps）。
    ///
    /// <para>★ 2026-10-11 加：21 点原来整桌都是静态立绘，现在和斗地主 / 德州一致。</para>
    ///
    /// <para>**异常只吞不抛**：贴图失败最多是这一帧不动，绝不能让牌桌停摆。</para>
    /// </summary>
    private void TickGuestIdle(double delta)
    {
        var frame = _idleClock.Tick(delta);

        for (var s = 0; s < _chars.Length; s++)
        {
            if (!_chars[s].IsGuest) continue;                  // 本体角色走活模型
            if (_actors[s] is { Alive: true }) continue;       // 活角色在 → 立绘让位
            if (_seatArt[s] is not { } art) continue;

            try
            {
                if (!GodotObject.IsInstanceValid(art)) continue;
                var tex = DouSpireCharacters.IdleFrame(_chars[s].GuestId, frame);
                if (tex is not null) art.Texture = tex;
            }
            catch (System.Exception) { /* 这一帧贴不上就算了，下一帧再试 */ }
        }
    }

    /// <summary>收桌时把请来的活角色逐个送走（它们挂在 <c>_scene</c> 上，跟着一起销毁）。</summary>
    public override void _ExitTree()
    {
        for (var s = 0; s < _actors.Length; s++)
        {
            try { _actors[s]?.Despawn(); }
            catch (System.Exception) { /* 收桌出岔子不影响任何事 */ }
            _actors[s] = null;
        }
    }

    public override void _Process(double delta)
    {
        // 待机动画必须排在所有 return 之前 —— 下注 / 打牌 / 结算三个阶段都要一直动。
        TickGuestIdle(delta);

        // 鸡煲代打：下注阶段自己按「发牌」（不然每局还得手动点一下，代打就断了）。
        // 这一支必须在 `_game is null` 兜底**之前** —— 第一局开桌时 `_game` 还是 null。
        if (_autoPilot && _phase == LocalPhase.Betting)
        {
            _aiWait -= delta;
            if (_aiWait <= 0)
            {
                _aiWait = AiThinkSeconds;
                OnDeal();
            }
            return;
        }

        if (_game is null) return;

        if (_phase == LocalPhase.Playing)
        {
            // 电脑那一席：装模作样想一会儿再动手
            if (_game.TurnSeat >= 0 && _game.TurnSeat != MySeat)
            {
                _aiWait -= delta;
                if (_aiWait <= 0)
                {
                    _aiWait = AiThinkSeconds;
                    AiAct(_game.TurnSeat);
                }
            }
            // 鸡煲代打：轮到我，AI 替我要牌 / 停牌 / 加倍 / 分牌（同一套基本策略、同一个节奏）
            else if (_autoPilot && _game.TurnSeat == MySeat)
            {
                _aiWait -= delta;
                if (_aiWait <= 0)
                {
                    _aiWait = AiThinkSeconds;
                    AiAct(MySeat);
                }
            }
            return;
        }

        if (_phase == LocalPhase.Result)
        {
            _resultWait -= delta;
            if (_resultWait <= 0)
            {
                _phase = LocalPhase.Betting;
                _aiWait = AiThinkSeconds;   // 代打开着的话，下一局也先"想一会儿"再发牌
                Refresh();
            }
        }
    }

    /// <summary>
    /// 「鸡煲代打」开关：点一下切换。开着时 <see cref="AiAct"/> 会替「我」打完每一局
    /// （含自动按「发牌」），节奏跟电脑席一样是 <see cref="AiThinkSeconds"/>。
    /// </summary>
    private void ToggleAutoPilot()
    {
        _autoPilot = !_autoPilot;
        // 刚打开时别立刻出手 —— 先让计时器走满一个思考时间。
        if (_autoPilot) _aiWait = AiThinkSeconds;
        UpdateAutoBtn();
        Entry.Logger.Info($"21 点：鸡煲代打 {(_autoPilot ? "开" : "关")}。");
        Refresh();
    }

    /// <summary>把按钮上的字刷成当前状态（按钮可能还没建出来，所以判空）。</summary>
    private void UpdateAutoBtn()
    {
        if (_autoBtn is null || !GodotObject.IsInstanceValid(_autoBtn)) return;
        _autoBtn.Text = _autoPilot ? "✓ 代打中" : "鸡煲代打";
    }

    /// <summary>电脑按**基本策略**打一手。</summary>
    private void AiAct(int seat)
    {
        if (_game is null) return;
        var hand = _game.Seats[seat].Current;
        var move = BlackjackAi.Decide(hand, _game.DealerUpCard,
            _game.CanSplit(_chips[seat]), _game.CanDouble(_chips[seat]));

        if (!_game.Apply(move, _chips[seat]))
            _game.Apply(BlackjackGame.BjMove.Stand, _chips[seat]);   // 做不了就停，绝不卡住

        DouSpireSfx.Deal();
        if (_game.IsOver) SettleRound();
        else Refresh();
    }

    // ══════════════════════════════ 按钮 ══════════════════════════════

    private void OnDeal()
    {
        if (_phase != LocalPhase.Betting) return;
        StartRound();
    }

    private void OnHit()
    {
        if (_game is null || _phase != LocalPhase.Playing) return;
        if (_game.TurnSeat != MySeat) return;
        if (!_game.Apply(BlackjackGame.BjMove.Hit, _chips[MySeat])) return;

        DouSpireSfx.Deal();
        if (_game.IsOver) { SettleRound(); return; }
        if (_game.TurnSeat != MySeat) _aiWait = AiThinkSeconds;
        Refresh();
    }

    private void OnStand()
    {
        if (_game is null || _phase != LocalPhase.Playing) return;
        if (_game.TurnSeat != MySeat) return;
        if (!_game.Apply(BlackjackGame.BjMove.Stand, _chips[MySeat])) return;

        _aiWait = AiThinkSeconds;
        if (_game.IsOver) { SettleRound(); return; }
        Refresh();
    }

    private void OnDouble()
    {
        if (_game is null || _phase != LocalPhase.Playing) return;
        if (_game.TurnSeat != MySeat) return;
        if (!_game.Apply(BlackjackGame.BjMove.Double, _chips[MySeat]))
        {
            Entry.Toast("这手不能加倍（只有前两张、且钱要够再加一份）。", "21 点");
            return;
        }

        DouSpireSfx.Deal();
        _aiWait = AiThinkSeconds;
        if (_game.IsOver) { SettleRound(); return; }
        Refresh();
    }

    private void OnSplit()
    {
        if (_game is null || _phase != LocalPhase.Playing) return;
        if (_game.TurnSeat != MySeat) return;
        if (!_game.Apply(BlackjackGame.BjMove.Split, _chips[MySeat]))
        {
            Entry.Toast("这手不能分牌（要两张点数一样、且钱够再加一份）。", "21 点");
            return;
        }

        DouSpireSfx.Deal();
        if (_game.IsOver) { SettleRound(); return; }
        Refresh();
    }

    private void OnHint()
    {
        if (_game is null || _phase != LocalPhase.Playing) return;
        if (_game.TurnSeat != MySeat) return;

        var hand = _game.Seats[MySeat].Current;
        var move = BlackjackAi.Decide(hand, _game.DealerUpCard,
            _game.CanSplit(_chips[MySeat]), _game.CanDouble(_chips[MySeat]));
        _hint.Text = BlackjackAi.AdviceText(move);
    }

    private void OnBetUp()
    {
        var idx = System.Array.FindIndex(BetSteps, b => b > _myBet);
        if (idx >= 0) _myBet = BetSteps[idx];
        else _myBet = Math.Min(_chips[MySeat], BetSteps[^1] * 2);
        Refresh();
    }

    private void OnBetDown()
    {
        var idx = System.Array.FindLastIndex(BetSteps, b => b < _myBet);
        if (idx >= 0) _myBet = BetSteps[idx];
        Refresh();
    }

    // ══════════════════════════════ 刷新界面 ══════════════════════════════

    private void Refresh()
    {
        if (!GodotObject.IsInstanceValid(this)) return;

        var sign = _myTotal >= 0 ? "+" : "";
        var unit = UseHp ? " 血" : "";
        _purseLabel.Text = UseHp
            ? $"血量 {_chips[MySeat]}　·　本桌累计 {sign}{_myTotal}{unit}"
            : $"筹码 {_chips[MySeat]}　·　本桌累计 {sign}{_myTotal}";

        var shownBet = _betOf[MySeat] > 0
            ? _betOf[MySeat]
            : UseHp ? DouSpireConfig.BlackjackBaseStake : _myBet;
        _betLabel.Text = $"押注  {shownBet}{unit}";

        var betting = _phase == LocalPhase.Betting;
        _btnDeal.Visible = betting;
        // 血量模式：注码由设置项 <c>BlackjackBaseStake</c> 定，桌上的 −/+ 没意义 —— 收起来
        _btnBetUp.Visible = betting && !UseHp;
        _btnBetDown.Visible = betting && !UseHp;
        _btnHit.Visible = _btnStand.Visible = _btnDouble.Visible = _btnSplit.Visible = _btnHint.Visible = !betting;

        // 「鸡煲代打」**两个阶段都露着**（下注时也能开 —— 开了就自动发牌、自动打）。
        UpdateAutoBtn();
        _autoBtn.Visible = true;

        // 轮到我才能动手
        var myTurn = _game != null && _phase == LocalPhase.Playing && _game.TurnSeat == MySeat;
        _btnHit.Disabled = !myTurn;
        _btnStand.Disabled = !myTurn;
        _btnHint.Disabled = !myTurn;
        _btnDouble.Disabled = !myTurn || (_game != null && !_game.CanDouble(_chips[MySeat]));
        _btnSplit.Disabled = !myTurn || (_game != null && !_game.CanSplit(_chips[MySeat]));

        RefreshDealer();
        for (var s = 0; s < SeatCount; s++) RefreshSeat(s);
        RefreshBanner();
    }

    private void RefreshDealer()
    {
        foreach (var c in _dealerCards.GetChildren()) { _dealerCards.RemoveChild(c); c.QueueFree(); }

        if (_game is null || _game.Dealer.Cards.Count == 0)
        {
            _dealerTotal.Text = "等发牌";
            _dealerTotal.AddThemeColorOverride("font_color", new Color(0.66f, 0.68f, 0.72f));
            return;
        }

        var reveal = _game.Phase is BlackjackGame.BjPhase.DealerTurn or BlackjackGame.BjPhase.Settled;

        if (reveal)
        {
            foreach (var c in _game.Dealer.Cards) _dealerCards.AddChild(CardFace(c, DealerCardW, false));
            _dealerTotal.Text = _game.Dealer.Busted
                ? $"爆了（{_game.Dealer.Best}）"
                : $"{_game.Dealer.Best} 点";
            _dealerTotal.AddThemeColorOverride("font_color",
                _game.Dealer.Busted ? new Color(0.95f, 0.45f, 0.40f) : new Color(0.96f, 0.93f, 0.84f));
        }
        else
        {
            _dealerCards.AddChild(CardFace(_game.DealerUpCard, DealerCardW, false));
            _dealerCards.AddChild(CardBack(DealerCardW));
            _dealerTotal.Text = $"{_game.DealerShownTotal()} 点 + 暗牌";
            _dealerTotal.AddThemeColorOverride("font_color", new Color(0.96f, 0.93f, 0.84f));
        }
    }

    private void RefreshSeat(int seat)
    {
        _hudName[seat].Text = seat == MySeat ? $"我 · {_names[seat]}" : _names[seat];
        _hudName[seat].AddThemeColorOverride("font_color", _colors[seat]);

        var bet = _betOf[seat];
        _hudChips[seat].Text = UseHp
            ? seat == MySeat ? $"血量 {_chips[seat]}　押 {bet}" : $"押 {bet} 血"
            : $"筹码 {_chips[seat]}　押 {bet}";

        // 当前在动作的那一席，信息框框一圈亮金边
        var active = _game != null && _phase == LocalPhase.Playing && _game.TurnSeat == seat;
        var mine = seat == MySeat;
        _seatHud[seat].AddThemeStyleboxOverride("panel",
            DouSpireUi.Box(new Color(0.05f, 0.06f, 0.08f, 0.78f), 12,
                active ? new Color(1f, 0.95f, 0.55f)
                       : mine ? new Color(1f, 0.84f, 0.30f)
                              : new Color(0.55f, 0.48f, 0.32f),
                active ? 4 : mine ? 3 : 2));

        var host = _seatCards[seat];
        foreach (var c in host.GetChildren()) { host.RemoveChild(c); c.QueueFree(); }

        if (_game is null || _game.Seats[seat].Hands.Count == 0 || _game.Seats[seat].Hands[0].Cards.Count == 0)
        {
            var wait = new Label { Text = "等发牌", HorizontalAlignment = HorizontalAlignment.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            DouSpireUi.StyleFont(wait);
            wait.AddThemeColorOverride("font_color", new Color(0.62f, 0.64f, 0.68f));
            wait.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(17));
            host.AddChild(wait);
            return;
        }

        var hands = _game.Seats[seat].Hands;

        // 分牌之后一手变两手 —— **并排放**（竖着摞会顶到下面的信息框），
        // 同时把牌缩一档，否则两手的牌会横向铺开压到邻席。
        var handRow = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        handRow.AddThemeConstantOverride("separation", DouSpireUi.Gap(14));
        var cardW = hands.Count > 1 ? CardW * 0.70f : CardW;

        for (var i = 0; i < hands.Count; i++)
        {
            var hand = hands[i];
            var isCurrent = active && i == _game.Seats[seat].ActiveHand;

            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", DouSpireUi.Gap(3));

            var cards = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            cards.AddThemeConstantOverride("separation", DouSpireUi.Gap(6));
            foreach (var c in hand.Cards) cards.AddChild(CardFace(c, cardW, isCurrent));
            box.AddChild(cards);

            var info = new Label { HorizontalAlignment = HorizontalAlignment.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            DouSpireUi.StyleFont(info);
            info.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(18));
            info.Text = HandText(hand, seat, i);
            info.AddThemeColorOverride("font_color",
                hand.Busted ? new Color(0.90f, 0.42f, 0.36f)
                : hand.IsBlackjack ? new Color(1f, 0.86f, 0.35f)
                : new Color(0.96f, 0.94f, 0.88f));
            box.AddChild(info);

            handRow.AddChild(box);
        }

        host.AddChild(handRow);
    }

    /// <summary>一手下面那行小字：点数 + 押注 + 结果。</summary>
    private string HandText(BlackjackGame.BjHand hand, int seat, int index)
    {
        var tag = _game!.Seats[seat].Hands.Count > 1 ? $"第{index + 1}手 " : "";
        var pts = hand.Busted ? $"爆了（{hand.Best}）"
            : hand.IsBlackjack ? "黑杰克 21"
            : hand.IsSoft ? $"{hand.Best} 点（软）"
            : $"{hand.Best} 点";
        var bet = $"押 {hand.Bet}";

        if (_game.Phase == BlackjackGame.BjPhase.Settled)
        {
            var oc = _game.OutcomeOf(hand);
            var net = _game.SettleHand(hand);
            var word = oc switch
            {
                BlackjackGame.BjOutcome.Blackjack => "黑杰克！",
                BlackjackGame.BjOutcome.Win => "赢",
                BlackjackGame.BjOutcome.Push => "平",
                _ => "输",
            };
            return $"{tag}{pts} · {bet} · {word} {(net >= 0 ? "+" : "")}{net}";
        }
        return $"{tag}{pts} · {bet}";
    }

    private void RefreshBanner()
    {
        if (_game is null) { _banner.Text = ""; _hint.Text = ""; return; }

        if (_phase == LocalPhase.Betting)
        {
            _banner.Text = "";
            _hint.Text = UseHp
                ? $"拿命赌：押 {DouSpireConfig.BlackjackBaseStake} 血。爆牌掉血、黑杰克回血 "
                  + $"（单次最多掉当前血量的 {DouSpireConfig.BlackjackHpCapPercent}%，永远给你留 1 血）。"
                : "离 21 点越近越好，超过 21 就直接输。庄家不到 17 必须一直要牌。";
            return;
        }

        if (_phase == LocalPhase.Playing)
        {
            if (_game.TurnSeat == MySeat)
            {
                _banner.Text = "";
                _hint.Text = "轮到你了。要不起就停牌，牌好就加倍。";
            }
            else if (_game.TurnSeat >= 0)
            {
                _banner.Text = "";
                _hint.Text = $"{_names[_game.TurnSeat]} 在想……";
            }
            return;
        }

        // 结算
        var mine = _game.Seats[MySeat];
        var hand = mine.Hands[0];
        var oc = _game.OutcomeOf(hand);
        _banner.Text = oc switch
        {
            BlackjackGame.BjOutcome.Blackjack => $"★ 黑杰克！ +{mine.Net}",
            BlackjackGame.BjOutcome.Win => $"你赢了 +{mine.Net}",
            BlackjackGame.BjOutcome.Push => "平局（退注）",
            _ => hand.Busted ? $"爆牌 −{Math.Abs(mine.Net)}" : $"你输了 {mine.Net}",
        };
        _banner.AddThemeColorOverride("font_color",
            mine.Net > 0 ? new Color(0.55f, 0.95f, 0.55f)
            : mine.Net < 0 ? new Color(0.95f, 0.45f, 0.40f)
            : new Color(0.90f, 0.88f, 0.80f));

        _hint.Text = $"庄家 {(_game.Dealer.Busted ? "爆了" : _game.Dealer.Best + " 点")}";
    }

    // ══════════════════════════════ 画牌（复用德州那套贴图） ══════════════════════════════

    /// <summary>
    /// 一张牌面。**和德州 `MakeCardFace` 一模一样的拼法**：牌身 + 左上点数 + 中央花色三层贴图。
    /// 贴图链按 <see cref="DouSpireConfig.CardSkin"/> 在「经典 / 烫金」两套之间切。
    /// </summary>
    private static Control CardFace(PokerCard card, float w, bool highlight)
    {
        var panel = NewCardPanel(w, highlight);
        var h = w * 1.40f;
        var W = DouSpireUi.Px(w);
        var H = DouSpireUi.Px(h);

        var body = DouSpireAssets.First(BodyChain(highlight));
        panel.AddThemeStyleboxOverride("panel", body != null
            ? DouSpireUi.TextureBox(body)
            : DouSpireUi.Box(DouSpireUi.CardFace, 8, new Color(0.25f, 0.22f, 0.20f), 2));

        var pip = DouSpireAssets.First(PipChain(card.Rank));
        if (pip != null)
            panel.AddChild(Artless(pip, W * 0.06f, H * 0.030f, W * 0.46f, H * 0.245f));

        var suit = DouSpireAssets.First(SuitChain(card.Suit));
        if (suit != null)
        {
            var r = Artless(suit, 0f, 0f, W * 0.27f, H * 0.19f);
            r.AnchorLeft = r.AnchorRight = 0.5f;
            r.AnchorTop = r.AnchorBottom = 0.5f;
            panel.AddChild(r);
        }
        return panel;
    }

    /// <summary>一张牌背（庄家的暗牌）。</summary>
    private static Control CardBack(float w)
    {
        var panel = NewCardPanel(w, false);
        var back = DouSpireAssets.First(BackChain());
        panel.AddThemeStyleboxOverride("panel", back != null
            ? DouSpireUi.TextureBox(back)
            : DouSpireUi.Box(new Color(0.20f, 0.08f, 0.10f), 8, DouSpireUi.Gold, 2));
        return panel;
    }

    private static Panel NewCardPanel(float w, bool highlight)
    {
        var panel = new Panel
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(w), DouSpireUi.Px(w * 1.40f)),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        if (highlight)
        {
            var ring = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            ring.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            ring.AddThemeStyleboxOverride("panel",
                DouSpireUi.Box(new Color(1f, 0.82f, 0.25f, 0.10f), 10, new Color(1f, 0.82f, 0.25f), 4));
            panel.AddChild(ring);
        }
        return panel;
    }

    private static TextureRect Artless(Texture2D tex, float left, float top, float right, float bottom)
    {
        var r = new TextureRect
        {
            Texture = tex,
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        r.AnchorLeft = r.AnchorRight = 0f;
        r.AnchorTop = r.AnchorBottom = 0f;
        r.OffsetLeft = left;
        r.OffsetTop = top;
        r.OffsetRight = right;
        r.OffsetBottom = bottom;
        return r;
    }

    private static string[] BodyChain(bool selected) => DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
        ? DouSpireAssets.GildedBody(selected)
        : selected ? DouSpireAssets.CardBodyOn : DouSpireAssets.CardBody;

    private static string[] PipChain(int rank) => DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
        ? DouSpireAssets.GildedPip(rank)
        : new[] { $"cards/pip_{PokerCard.RankName(rank)}.png" };

    private static string[] SuitChain(PokerSuit s)
    {
        var d = s switch
        {
            PokerSuit.Spade => DouSpireRules.Suit.Spade,
            PokerSuit.Heart => DouSpireRules.Suit.Heart,
            PokerSuit.Club => DouSpireRules.Suit.Club,
            _ => DouSpireRules.Suit.Diamond,
        };
        return DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
            ? DouSpireAssets.GildedSuit(d)
            : DouSpireAssets.SuitOf(d);
    }

    private static string[] BackChain() => DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
        ? DouSpireAssets.GildedBack
        : DouSpireAssets.CardBack;

    // ══════════════════════════════ 视口 ══════════════════════════════

    private void FitToViewport()
    {
        var r = GetViewport()?.GetVisibleRect() ?? new Rect2(0, 0, 1920, 1080);
        if (r.Size.X < 8f || r.Size.Y < 8f) return;
        AnchorLeft = AnchorTop = AnchorRight = AnchorBottom = 0f;
        Position = r.Position;
        Size = r.Size;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == (int)NotificationResized || what == (int)NotificationWMSizeChanged) FitToViewport();
    }
}
