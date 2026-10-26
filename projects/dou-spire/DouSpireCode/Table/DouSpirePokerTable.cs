using Godot;
using DouSpireRules;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire.Table;

/// <summary>
/// 游戏里的那一张**德州扑克桌** —— 玩法二（2026-10-09）。
///
/// 和 <see cref="DouSpireTable"/> 是**两张平行的桌**，不是同一张桌的两个模式：
/// 斗地主的阶段机是「明牌 → 抢地主(猜拳) → 加倍 → 扣牌 → 出牌 → 结算」，
/// 德州是「发底牌 → 翻牌前 → 翻牌 → 转牌 → 河牌 → 摊牌」，一步都对不上。
/// 所以这边**另起一套**，但把当初抽出去的那些静态件全拿来复用：
/// <see cref="DouSpireUi"/>（界面）、<see cref="DouSpireAssets"/>（三层贴图回落）、
/// <see cref="DouSpireCharacters"/> + <see cref="DouSpireActor"/>（桌边活角色）、
/// <see cref="DouSpireSfx"/> / <see cref="DouSpireMusic"/> / <see cref="DouSpireVoice"/>（声音）、
/// <see cref="DouSpireLayout"/>（摆位表）。
///
/// ── 规则全在 <see cref="HoldemGame"/> 里 ────────────────────────
/// 这个文件**一行规则都不写**，只做三件事：把状态画出来、把玩家的点击翻译成
/// <see cref="PokerAction"/>、把电脑的思考交给 <see cref="PokerAi"/>。
/// 好处是"钱算错"这种最要命的 bug 只可能出在内核，而内核是能离线自检的（<c>--poker</c>）。
///
/// ── 新手 / 观众友好（用户 2026-10-09 的硬需求）────────────────
/// 用户原话：「**实话说我从来没玩过德州扑克，你自己看着办吧。主要是观众要加的。**」
/// 所以这一桌处处按"看别人玩也能看懂"来做：
/// <list type="number">
///   <item>常驻一行「**你现在是：一对 A**」—— 新手不用背牌型表（<see cref="HintText"/>）。</item>
///   <item>按钮写人话：`跟注 100` / `加注到 300` / `弃牌`，**要掏多少钱直接写在按钮上**。</item>
///   <item>右上角 `?` 一页牌型速查表（<see cref="BuildHelpPanel"/>）。</item>
///   <item>摊牌时把**赢家那 5 张**用金框圈出来（<see cref="BuildBoardRow"/> 的 highlight）。</item>
///   <item>赢的时候牌型名打大（结算面板的大字）。</item>
///   <item>「该不该跟」给建议（<see cref="OddsHint"/>，可在设置页关）。</item>
///   <item>牌局日志说大白话（内核的 <c>Log</c> 本来就是中文的，直接抄）。</item>
/// </list>
///
/// ⚠️ 联机（P4，2026-10-09 晚）由 <see cref="DouSpirePokerNet"/> 接上：
/// 底牌是隐藏信息，方案是"**同种子各端本地发牌**"（用户已拍板，见设计方案 §3.3）。
/// 这一桌为了联机做了一个**视角旋转**：内核里的座位号是全局的，而画面永远把"我"摆在 0 号位
/// —— 见下面 <see cref="MeView"/> / <see cref="G"/> / <see cref="V"/> 的说明。
/// **单人桌两个概念重合，所以这些换算对单人零影响。**
/// </summary>
public sealed partial class DouSpirePokerTable : Control
{
    /// <summary>本地视角的第 0 号位**永远是我**（画面代码的基准：摆位表、HUD、立绘都按它排）。</summary>
    private const int MeView = 0;

    /// <summary>
    /// **我的全局座位号**。单人桌就是 0；联机桌是我在主机那张座位表里的号。
    ///
    /// ★ 为什么要分成"视角位"和"全局位"两个概念：
    /// <c>HoldemGame</c> 里的座位号是**全局**的（各端一模一样），
    /// 而牌桌画面永远把"我"摆在 0 号位。所以：
    /// 画面上循环用的 <c>s</c> 是**视角位**，喂给内核前要过 <see cref="G"/> 换成全局位；
    /// 内核吐出来的座位号（该谁说话 / 庄家位 / 赢家）要过 <see cref="V"/> 换回视角位。
    /// </summary>
    private static int MeSeat => DouSpirePokerNet.Active ? DouSpirePokerNet.MySeat : 0;

    /// <summary>视角位 → 全局座位号（单人恒等）。</summary>
    private static int G(int view) => DouSpirePokerNet.ToGlobal(view);

    /// <summary>全局座位号 → 视角位（单人恒等）。</summary>
    private static int V(int global) => DouSpirePokerNet.ToLocal(global);

    /// <summary>本桌人数。开局前跟设置走（联机由主机定），开局后以牌局为准。</summary>
    private int SeatCount => _game?.Players.Count ?? TableN;

    /// <summary>这一桌坐几个人（联机由主机定；单人跟设置）。</summary>
    private static int TableN => DouSpirePokerNet.Active
        ? DouSpirePokerNet.SeatCount
        : Mathf.Clamp((int)DouSpireConfig.TableSize, 2, 4);

    /// <summary>主机等远端真人走子的上限（秒）。超了就用兜底动作替他走完 —— 绝不卡死。</summary>
    private const double RemoteTurnTimeoutSeconds = 45.0;

    // ════════════════════════════════════════════════════════
    //  摆位常量（想调布局只改这里；单位是"父级尺寸的比例"）
    // ════════════════════════════════════════════════════════

    private const float TitleAy = 0.052f;
    private const float PotAy = 0.352f;      // 底池
    private const float BoardAy = 0.500f;    // 5 张公共牌
    private const float StatusAy = 0.612f;   // "轮到你 / 电脑A 加注到 300"
    private const float HintAy = 0.702f;     // "你现在是：一对 A"
    private const float ActionAy = 0.802f;   // 动作按钮排
    private const float HandAy = 0.952f;     // 我的两张底牌
    // 牌局日志（右下角，避开座位）。★ 2026-10-11 面板加宽 430→560 后，锚点跟着左移，
    //   否则右边缘会顶出屏幕（560 的板子在 2560 宽的屏上是 747 px，原来 0.878 只能放下 573）。
    private const float LogAx = 0.845f, LogAy = 0.688f;

    /// <summary>公共牌的宽（1920 基准像素）。高 = 宽 × 1.40。</summary>
    private const float BoardCardW = 106f;

    /// <summary>我手上那两张底牌的宽。</summary>
    private const float MyCardW = 118f;

    // ════════════════════════════════════════════════════════
    //  ★ 台面刻线实测表（2026-10-09）
    //
    //  用户反馈：「**你这个透明牌型 ui 和这个圆桌中央的牌口没有对上很难受**」。
    //  为什么对不上：**台面凹槽是美术画在图上的，代码里的坐标是我们自己硬编的**，
    //  两套系统。而且每张台面风格刻线位置都不一样 —— 老绿绒的凹槽贴在桌面偏左，
    //  新那四张（冰蓝/黄铜/黑曜石/二次元）才是居中的一排。
    //
    //  所以只能"一张表"。数据是 `_work/measure5.py` 量出来的
    //  （把台面图按游戏里同样的 KeepAspectCentered 算法铺到 1920×1080 画布上，
    //    再用 Sobel 找凹槽描边的边缘 → 换算成屏幕坐标）。
    //
    //  ⚠️ 以后美术**重画台面图就要重新量**：跑
    //     `D:/software/Python/python.exe _work/measure5.py <风格>`。
    // ════════════════════════════════════════════════════════

    /// <summary>公共牌那一排的中心 y（比例）。</summary>
    private float _markBoardAy = BoardAy;

    /// <summary>底池的中心 y（比例）—— 想让它落进台面上画的底池圆里。</summary>
    private float _markPotAy = PotAy;

    /// <summary>一张公共牌的宽（1920 基准像素）。</summary>
    private float _markCardW = BoardCardW;

    /// <summary>公共牌之间的缝（1920 基准像素）。</summary>
    private float _markGap = 12f;

    /// <summary>底池牌匾的宽高（1920 基准像素）。</summary>
    private float _markPotPlateW = 320f, _markPotPlateH = 78f;

    /// <summary>
    /// 按**当前这张台面**的刻线，定出公共牌那一排 / 底池的位置。
    /// 换台面风格要重新调一次（见 <see cref="PlacePotAndBoard"/>）。
    /// </summary>
    private void ApplyMarks()
    {
        // 默认 = 档 0 经典绿绒（老那张 table_poker.png）。**默认值必须填它** ——
        // 因为还没画的那两档（东方漆器 / 蒸汽朋克）取不到自己的图时，
        // DouSpireAssets.PokerTableOf 的候选链会**回落成 table_poker.png**，
        // 那铺在桌上的就是经典绿绒的刻线 → 位置也得跟着经典绿绒。
        //
        // 经典绿绒实测（1920×1080 屏幕坐标）：
        //   5 格共壁，竖壁在 668 / 783.5 / 899.5 / 1017.5 / 1133.5 / 1250（pitch 116.4，整体居中 959）
        //   牌位那一行上下轨 y = 380 / 520 → 中心 y = 450（内高 140）
        //   ⇒ 卡牌取 96×134.4，正好卧进 116.4×140 的格里，上下各留 2.8 缝
        //   底池圆：y 536~624 → 中心 y = 580（直径 ≈ 88）
        _markCardW = 96f;
        _markGap = 20f;
        _markBoardAy = 450f / 1080f;   // ≈ 0.4167
        _markPotAy = 580f / 1080f;     // ≈ 0.5370
        _markPotPlateW = 210f;         // ★ 2026-10-10 起按素材真实比例（971×256 ⇒ 3.79）定尺寸
        _markPotPlateH = 55f;          //   原来 150×56（比例 2.68）是**竖着拉长的**，看着发胖。

        switch (DouSpireAssets.TableKey(DouSpireConfig.PokerTableStyle))
        {
            // 这四张的刻线**完全一致**（同一套模板出的图），所以共用一组数：
            //   5 个凹槽的左/右边线  740/814、831/902、921/993、1012/1083、1102/1176
            //   → 格宽 73、缝 17.7（pitch 90.7，整体居中在 958）
            //   凹槽上沿 322 / 下沿 444 → 中心 y = 383（凹槽内高 107，卡牌 73×102 正好卧进去）
            //   底池圆：外沿 y 470~617 → 中心 y = 542（半径 ≈ 73）
            case "table_poker_frost":
            case "table_poker_brass":
            case "table_poker_obsidian":
            case "table_poker_anime":
                _markCardW = 73f;
                _markGap = 17.7f;
                _markBoardAy = 383f / 1080f;   // ≈ 0.3546
                _markPotAy = 542f / 1080f;     // ≈ 0.5019
                // ★ 2026-10-10：同 210 那档的理由 —— 按素材比例 3.79 定尺寸（原来 176×64 比例 2.75，偏胖）
                _markPotPlateW = 240f;
                _markPotPlateH = 63f;
                break;
        }
    }

    /// <summary>
    /// 把**底池牌匾 / 筹码堆 / 公共牌那一排**按当前刻线重新摆一遍。
    /// <para>搭场景时调一次；之后每次换台面风格（<see cref="CycleTable"/>）再调一次，
    /// 这样"换桌子"会**连凹槽对齐一起换**，不用收桌重开。</para>
    /// <para><c>DouSpireUi.Place</c> 只写 anchors + offsets、不重新挂父节点，
    /// 所以对已经在场景里的节点再调一次是安全的（不会打乱层级）。</para>
    /// </summary>
    private void PlacePotAndBoard()
    {
        ApplyMarks();

        if (_potBg is { } pb && GodotObject.IsInstanceValid(pb))
            DouSpireUi.Place(pb, 0.5f, _markPotAy,
                DouSpireUi.Px(_markPotPlateW), DouSpireUi.Px(_markPotPlateH));

        // 牌匾的图要**跟着新尺寸重切**（210×55 ⇄ 240×63，见 BuildPotPlate 的注释）。
        BuildPotPlate();

        if (_potChips is { } pc && GodotObject.IsInstanceValid(pc))
            DouSpireUi.Place(pc, 0.5f, _markPotAy, DouSpireUi.Px(56), DouSpireUi.Px(104),
                -DouSpireUi.Px(_markPotPlateW / 2f + 40f));

        if (_boardRow is { } br && GodotObject.IsInstanceValid(br))
        {
            br.AddThemeConstantOverride("separation", DouSpireUi.Gap(_markGap));
            DouSpireUi.Place(br, 0.5f, _markBoardAy,
                DouSpireUi.Px(_markCardW * 5 + _markGap * 4 + 8),
                DouSpireUi.Px(_markCardW * 1.4f));
        }

        // —— 公共牌牌匾（`poker/board_plate.png`）——
        //   ★ 只在**没有自带凹槽**的台面上铺（= 经典绿绒 / 界面默认那张）。
        //     另外 4 张台面的图里已经画了 5 个凹槽，再叠一块牌匾就是两条线打架
        //     （这就是它当初"没被引用"的原因）。牌匾按 5 个格子的总宽摆，居中在牌排后面。
        if (_boardPlate is { } bp && GodotObject.IsInstanceValid(bp))
        {
            var plateOn = DouSpireAssets.TableKey(DouSpireConfig.PokerTableStyle) == "table_poker";
            bp.Visible = plateOn;
            if (plateOn)
            {
                // 牌匾比牌排略大一圈（上下各多留一点绿边），正好把 5 个格框住。
                var plateW = _markCardW * 5 + _markGap * 4 + 90f;
                DouSpireUi.Place(bp, 0.5f, _markBoardAy,
                    DouSpireUi.Px(plateW), DouSpireUi.Px(plateW * (317f / 1280f)));
                // 牌匾要垫在牌**后面** —— 交给 RestackBase 统一排（它会把牌匾放在桌面之上、
                // 所有 UI 之下）。★ 这里原来写死 `MoveChild(bp, 0)`，正是黑屏的**第一块多米诺**：
                // 它把索引 0 让给了牌匾，那块整屏兜底色被挤到 1，后面 bg/table 的写死索引就全错位了。
                RestackBase();
            }
        }
    }

    /// <summary>
    /// （重）建底池牌匾的图 —— `poker/pot_plate.png`（971×256 的圆角红牌匾）切三块铺满。
    ///
    /// <para>★ 为什么每次都要重建：牌匾的尺寸**跟着台面风格走**
    /// （经典绿绒 210×55，另外四张 240×63），而 <see cref="DouSpireUi.Plate"/>
    /// 是按"当时的像素尺寸"把三块的位置和宽都算死的 —— 换个尺寸就得重切一遍。</para>
    ///
    /// <para>★ 左右装饰帽各 48 px：这张图的四角是**圆角 + 金铆钉**，
    /// 从轮廓的平直段量出来圆角半径就是 48（原图 971 宽）。</para>
    /// </summary>
    private void BuildPotPlate()
    {
        if (_potPlate is not null && GodotObject.IsInstanceValid(_potPlate))
        {
            if (_potPlate.GetParent() is { } old) old.RemoveChild(_potPlate);
            _potPlate.QueueFree();
        }
        _potPlate = null;

        if (_potBg is null || !GodotObject.IsInstanceValid(_potBg)) return;

        var tex = DouSpireAssets.First(DouSpireAssets.PokerPotPlate);
        if (tex is null)
        {
            // 图取不到：退回原来的深红纯色块（照样能看清「底池 N」）。
            _potBg.AddThemeStyleboxOverride("panel",
                DouSpireUi.Box(new Color(0.12f, 0.05f, 0.05f, 0.85f), 12, DouSpireUi.Gold, 2));
            return;
        }

        // 有图：Panel 自己的底不要了，只留我们切好的三块。
        _potBg.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _potPlate = DouSpireUi.Plate(tex, 48, 48,
            DouSpireUi.Px(_markPotPlateW), DouSpireUi.Px(_markPotPlateH));
        _potBg.AddChild(_potPlate);
        _potBg.MoveChild(_potPlate, 0);        // 垫在「底池 N」那行字下面
    }

    // ════════════════════════════════════════════════════════
    //  静态状态
    // ════════════════════════════════════════════════════════

    private static DouSpirePokerTable? _instance;

    /// <summary>
    /// 非 null = **赌注模式**：这桌是在游戏篝火里开的，输赢要落到玩家的真实金币上。
    /// 普通开桌（主菜单 / 调试）恒为 null。和 <c>DouSpireTable._stakePlayer</c> 是**两个独立的字段** ——
    /// 两张桌不会同时开，各管各的更不容易串。
    /// </summary>
    private static Player? _stakePlayer;

    public static bool IsOpen => _instance != null && GodotObject.IsInstanceValid(_instance);

    // ════════════════════════════════════════════════════════
    //  一局的状态
    // ════════════════════════════════════════════════════════

    private HoldemGame _game = null!;
    private DouSpireCharacters.SeatPick[] _chars = new DouSpireCharacters.SeatPick[4];

    /// <summary>电脑思考用的随机源。**和发牌的种子无关** —— 发牌用 <see cref="HoldemGame"/> 内部的种子。</summary>
    private readonly System.Random _rng = new();

    /// <summary>这一桌用哪个种子（每手 +1，所以每手牌都不一样）。</summary>
    private int _seed;

    /// <summary>庄家位（每手顺时针轮换）。</summary>
    private int _button = -1;

    /// <summary>这一桌打第几手了（标题上显示）。</summary>
    private int _handNo;

    /// <summary>这一桌的累计输赢（我这一席）。</summary>
    private int _myTotal;

    /// <summary>电脑思考的倒计时（轮到电脑时开始走）。</summary>
    private double _aiTimer;

    /// <summary>本轮下注阶段已经报过"轮到谁"了没（防刷屏 / 防重复音效）。</summary>
    private int _lastSfxSeat = -1;

    /// <summary>这一手结算过了没（<see cref="PayOut"/> 只结一次）。</summary>
    private bool _paidOut;

    /// <summary>摊牌了没 —— true 才把各家的底牌翻开。</summary>
    private bool _revealAll;

    /// <summary>赢家那 5 张（摊牌高亮用）。空 = 没有（弃牌赢的）。</summary>
    private readonly HashSet<PokerCard> _bestFive = new();

    /// <summary>搭场景失败 = true，之后 <c>_Process</c> 直接返回（别每帧抛异常）。</summary>
    private bool _broken;

    /// <summary>开局筹码（每人一样）—— 用来算"这手赢了多少"。</summary>
    private int _startStack = 1000;

    private int _smallBlind = 50, _bigBlind = 100;

    // ── 联机（P4）──

    /// <summary>联机桌上"这一手开始了没"。false = 等主机发牌（只显示等待提示，不出按钮）。</summary>
    private bool _handStarted;

    /// <summary>我这一步已经发给主机、在等回声（等的时候锁住按钮，别重复发）。</summary>
    private bool _waitingHost;

    /// <summary>主机侧：等远端真人走子已经等了多久（超时兜底用，见 <see cref="RemoteTurnTimeoutSeconds"/>）。</summary>
    private double _remoteWait;

    // ════════════════════════════════════════════════════════
    //  界面引用
    // ════════════════════════════════════════════════════════

    private Control _scene = null!;

    /// <summary>
    /// 最底下那层**兜底纯色**（背景图万一取不到，至少不是透明的）。
    ///
    /// ★★ 2026-10-11 黑屏事故的当事人 ★★
    ///   它原来只是 <c>BuildScene</c> 里一个局部变量，靠"第一个 AddChild"天然待在索引 0。
    ///   而换牌桌 / 换室内都是"删旧的、加新的"（新节点永远加在末尾），
    ///   写死的 <c>MoveChild(x, 0/1/2)</c> 一旦被这么一挤就全指错 ——
    ///   这块**整屏不透明**的色块一路被顶到最上面，把背景和桌子全盖掉。
    ///   现在交给 <see cref="RestackBase"/> 统一维持层级，谁都别再写死索引。
    /// </summary>
    private Control? _felt;

    private readonly TextureRect?[] _seatArt = new TextureRect?[4];
    private readonly DouSpireActor?[] _actors = new DouSpireActor?[4];

    /// <summary>
    /// **编外角色待机动画的帧时钟**（本体角色走活模型，用不着它）。
    ///
    /// ★ 2026-10-11 加。以前德州只贴 <c>seats/char_&lt;id&gt;.png</c> 一张静态图 ——
    ///   同样四位客人在斗地主里会动、到德州就变成一张死图（用户："为什么预览是这些静态图"）。
    ///   现在和斗地主走同一个 <see cref="DouSpireCharacters.IdleClock"/>。
    /// </summary>
    private readonly DouSpireCharacters.IdleClock _idleClock = new();

    private readonly Control?[] _hud = new Control?[4];
    private readonly Label?[] _hudName = new Label?[4];
    private readonly Label?[] _hudStack = new Label?[4];
    private readonly Label?[] _hudBet = new Label?[4];

    /// <summary>
    /// 庄家按钮（D 字圆牌，`poker/dealer_button.png`）—— 谁是庄家谁头上挂一枚。
    /// ★ 2026-10-09 接素材：这张图 18:51 就入库了，代码里一直没用（用户问过「怎么没用上」）。
    /// </summary>
    private readonly TextureRect?[] _hudDealer = new TextureRect?[4];
    private readonly Label?[] _hudTag = new Label?[4];

    /// <summary>
    /// 盲注标记（`poker/blind_marker.png`，暗红圆牌）—— 挂在小盲/大盲那一席的信息框角上。
    /// ★ 2026-10-10 接素材：这张图一直没被引用；小盲/大盲以前只有一行小字（`_hudTag`），
    /// 挂上这枚牌之后，翻牌前一眼能看出谁"欠着盲注"。
    /// </summary>
    private readonly TextureRect?[] _hudBlind = new TextureRect?[4];

    /// <summary>
    /// 各席**面前**的一小摞筹码（2~3 枚叠起来）—— 表示"这一席这一轮已经下了多少"。
    ///
    /// ★ 2026-10-10 新增（用户："筹码应该是各个角色面前都放两三个。摆位也奇怪"）。
    ///   以前"这一轮已下 N"只是信息框里的一行小绿字，桌上一点实物感都没有 ——
    ///   而牌桌本身才是德州的主舞台。现在每席朝**桌心**的那一侧摆一小摞筹码，
    ///   本轮没下注就藏起来（<c>Visible = CommittedStreet &gt; 0</c>，见 <see cref="RefreshSeats"/>）。
    /// </summary>
    private readonly Control?[] _hudChips = new Control?[4];

    /// <summary>
    /// 摊牌时才出现的一小排牌 —— **对手的两张底牌**（在各自信息框下面）。
    /// 平时是隐藏的：德州里底牌是隐藏信息，只有摊牌那一刻才公开。
    /// 没有这一排，公共牌上圈出来的"赢家那 5 张"就有两张是看不见的，观众看不懂他凭什么赢。
    /// </summary>
    private readonly HBoxContainer?[] _hudHole = new HBoxContainer?[4];

    private HBoxContainer _boardRow = null!;
    private Label _potLabel = null!;

    /// <summary>底池那块牌匾的底板（换台面风格时要跟着挪，所以留成字段）。</summary>
    private Panel? _potBg;

    /// <summary>
    /// 底池牌匾的**图**（`poker/pot_plate.png` 自己切的三块，见 <see cref="DouSpireUi.Plate"/>）。
    /// ★ 换台面风格时牌匾尺寸会变（经典绿绒 210×55 ⇄ 另外四张 240×63），
    ///   而 Plate 是按**当时的像素尺寸**切好三块的 —— 所以只能整个重建，不能只改锚点。
    /// </summary>
    private Control? _potPlate;

    /// <summary>底池旁边那摞筹码（同上）。</summary>
    private TextureRect? _potChips;

    /// <summary>
    /// 公共牌底下垫的**牌匾**（`poker/board_plate.png`，自带 5 个凹槽）。
    /// ★ 2026-10-10 接素材：这张图一直没被引用，理由是"会跟台面自己的刻线打架"。
    ///   现在的做法是**只在经典绿绒（档 0）这张台面上铺** —— 那张台面没有刻槽，
    ///   牌匾正好补上；别的 4 张台面**自己带凹槽**，铺上去就是两条线叠一起，所以不铺。
    /// </summary>
    private TextureRect? _boardPlate;

    /// <summary>
    /// "你现在是什么牌"牌匾左边那枚**牌力图标**（`poker/rank_1`~`rank_10`）。
    /// 1 = 高牌 … 9 = 同花顺、10 = 皇家同花顺（见 <see cref="RankIconIndex"/>）。
    /// </summary>
    private TextureRect? _hintIcon;

    /// <summary>
    /// 结算时套在**赢家**那席立绘上的金环（`fx/winner_halo.png`）。
    /// 每次结算按赢家位置重新摆（见 <see cref="ShowResult"/> / <see cref="HideResult"/>）。
    /// </summary>
    private TextureRect? _winnerHalo;

    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Label _hintLabel = null!;
    private Label _logLabel = null!;
    private HBoxContainer _handRow = null!;

    private HBoxContainer _actions = null!;
    private Button _foldBtn = null!, _checkBtn = null!, _callBtn = null!, _raiseBtn = null!, _allinBtn = null!;
    private Button _nextBtn = null!, _repickBtn = null!;

    /// <summary>
    /// **「鸡煲代打」**（2026-10-11 从斗地主桌搬过来 —— 用户："现在所有模式都加鸡煲代打"）。
    /// 开着 = AI 用**设置里选的那个性格**（<see cref="DouSpireConfig.PokerStyle"/>）替「我」打完整手。
    ///
    /// **故意用 static** —— 跟斗地主那张桌一个道理：换手（下一手）/ 收桌重开也不忘，"一直托管"。
    /// 决策走 <see cref="AutoMove"/>，**不复制 AI 那套**。
    /// </summary>
    private static bool _autoPilot;

    /// <summary>「鸡煲代打」按钮（跟出牌那排同一个样式，随时能点）。</summary>
    private Button _autoBtn = null!;

    private Control _raiseRow = null!;
    private Button _rHalf = null!, _rThreeQ = null!, _rPot = null!, _rAllin = null!, _rCancel = null!;
    private int _raiseTarget;

    private Button _menuBtn = null!, _faceBtn = null!, _helpBtn = null!, _sceneBtn = null!, _tableBtn = null!;

    /// <summary>
    /// 两个圆钮（室内 / 桌面）**下面**那行小字。钮上不写字（用户要的就是"小圆圈"），
    /// 名字放这儿 —— 既是"现在是什么"，也是"刚才那一下点到的是哪个"的回执。
    /// 见 <see cref="UpdateSceneBtn"/> / <see cref="UpdateTableBtn"/>。
    /// </summary>
    private Label _sceneCap = null!, _tableCap = null!;

    /// <summary>当前铺着的整屏背景层（图或暖色渐变）。换场景时整个换掉 —— 见 <see cref="ApplyScene"/>。</summary>
    private Control? _bgLayer;

    /// <summary>当前铺着的那张牌桌。换牌桌风格时整个换掉 —— 见 <see cref="ApplyTable"/>。</summary>
    private Control? _tableLayer;

    private Control _helpPanel = null!;
    private Control _resultPanel = null!;
    private Label _resultTitle = null!, _resultBody = null!;

    // ════════════════════════════════════════════════════════
    //  开关桌
    // ════════════════════════════════════════════════════════

    /// <param name="keepStake">
    /// true = 保留调用方已经挂好的 <see cref="_stakePlayer"/>（篝火那条路要在 Open 之前挂）。
    /// false（默认）= 清成无赌注 —— 普通开桌就是这个语义。
    /// </param>
    public static void Open(Logger logger, bool keepStake = false)
    {
        if (IsOpen) return;
        if (!keepStake) _stakePlayer = null;

        // 两张桌互斥：斗地主桌开着就先收掉（布局表是共用的，两张桌同时开必打架）。
        if (DouSpireTable.IsOpen) DouSpireTable.Close(logger);

        var root = DouSpireUi.Root;
        if (root == null)
        {
            logger.Warn("德州：场景树还没准备好，开不了桌。");
            return;
        }

        // 每次开桌重新读图 / 语音 / 音乐 / 摆位 —— 美术改完直接收开桌就能看到，不用重启游戏。
        DouSpireAssets.ClearCache();
        DouSpireVoice.ClearCache();
        DouSpireMusic.ClearCache();

        var n = TableN;
        // ★★ 2026-10-09：德州有**自己的一份摆位表**（`layout-poker.txt`）——
        //   和斗地主共用一份时，4 人局的"对家"那一席会正好坐在底池 + 5 张公共牌上
        //   （用户实测："如果说四个人怎么重叠了，不在对应位置"）。
        DouSpireLayout.LoadPoker(n);

        _instance = new DouSpirePokerTable();
        root.AddChild(_instance);
        DouSpireSfx.Open();
        logger.Info($"德州：牌桌已打开（{n} 人桌）。");
    }

    public static void Close(Logger logger)
    {
        // ★ 2026-10-13：和 21 点 / 斗地主同款加固 —— **先摘单例、后判有效性**，
        //   无论单例什么状态都把音效/联机状态收干净，绝不让"收不掉"这种情况存在。
        var inst = _instance;
        _instance = null;

        try { DouSpireSfx.Close(); } catch (System.Exception) { }

        // 联机：收桌要通知所有人一起收（客机收桌只是自己不看，主机收桌才是真收）。
        // ⚠️ 这两句**必须无条件跑**，哪怕实例已经不在了 —— 不然会留下"桌面没了、
        //    联机还挂着"的僵局，对面会一直等一个已经收掉的桌。
        try
        {
            DouSpirePokerNet.Live = null;
            DouSpirePokerNet.CloseTable(broadcast: true);
        }
        catch (System.Exception e) { logger.Warn($"德州：收桌广播出错（本地已收干净）: {e.Message}"); }

        if (inst is null || !GodotObject.IsInstanceValid(inst))
        {
            logger.Info("德州：没有开着的牌桌，收桌按空操作处理（音效/联机已一并收掉）。");
            return;
        }

        try { inst.QueueFree(); }
        catch (System.Exception e) { logger.Warn($"德州：牌桌销毁出错（已尽力收干净）: {e.Message}"); }
        logger.Info("德州：牌桌已收起。");
    }

    public static void Toggle(Logger logger)
    {
        if (IsOpen) Close(logger); else Open(logger);
    }

    /// <summary>
    /// **键盘退出专用**（ESC / F9 兜底，见 <see cref="DouSpireHotkey"/>）：
    /// 桌子开着就收掉并返回 true；没开着返回 false。
    /// </summary>
    public static bool CloseByHotkey()
    {
        if (!IsOpen) return false;
        Close(Entry.Logger);
        return true;
    }

    /// <summary>
    /// **篝火开桌**：把牌桌上的输赢接到玩家的真实金币上。
    /// 顺序有讲究 —— 先挂赌注玩家再 <see cref="Open"/>（Open 默认会把赌注清成 null）。
    /// </summary>
    public static void OpenAtRestSite(Player player, Logger logger)
    {
        var mine = player ?? ResolveMyPlayer();
        if (mine is not null && SafeGold(mine) <= 0)
        {
            logger.Info("德州：身上 0 金币，拒绝开桌（没钱不赌）。");
            Entry.Toast("你身上一个金币都没有 —— 先去挣点钱再来德州。", "德州扑克");
            return;
        }

        if (IsOpen) Close(logger);
        _stakePlayer = mine;
        Open(logger, keepStake: true);
        logger.Info($"德州：篝火开桌（赌注模式），本金金币 {_stakePlayer?.Gold.ToString() ?? "（没拿到我的 Player）"}。");
    }

    /// <summary>我身上现在有多少金币（读不到返回 null = 不确定，调用方别拦）。</summary>
    public static int? MyGoldNow()
    {
        try
        {
            var p = _stakePlayer ?? ResolveMyPlayer();
            return p is null ? null : SafeGold(p);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"德州：读身上金币出错（当成有钱，不拦）: {e.Message}");
            return null;
        }
    }

    private static int SafeGold(Player p)
    {
        try { return p.Gold; }
        catch { return int.MaxValue; }
    }

    /// <summary>兜底解析"我自己"的 Player（篝火那条路在联机下可能拿不到）。</summary>
    private static Player? ResolveMyPlayer()
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            return state is null ? null : LocalContext.GetMe(state);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"德州：兜底解析自己的 Player 失败（这一局不下注）: {e.Message}");
            return null;
        }
    }

    /// <summary>给"第三方 mod 角色的换装补丁"一个真 Creature（本体角色用不着）。</summary>
    private static Creature? StakeCreature()
    {
        try { return _stakePlayer?.Creature; }
        catch (System.Exception) { return null; }
    }

    // ════════════════════════════════════════════════════════
    //  生命周期
    // ════════════════════════════════════════════════════════

    public override void _Ready()
    {
        SetMeta(DouSpireUi.MetaTag, true);
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 200;                       // 和斗地主桌同层（两张桌互斥，不会同时出现）
        MouseFilter = MouseFilterEnum.Stop;

        FitToViewport();
        DouSpireUi.UseScreen(this);

        try
        {
            InitCharacters();
            BuildScene();
            DouSpireUi.StyleFont(this);
            DouSpireAssets.ReportOnce();

            if (DouSpirePokerNet.Active)
            {
                DouSpirePokerNet.Live = this;
                // ★ 联机桌：座位表已经有了，但**主机还没说"开这手"**（种子没来）——
                //   先把桌子摆好等着，**绝不自己发牌**（自己发的牌跟别人不一样，一开局就分叉）。
                _handStarted = false;
                ShowWaitingForHost();
            }
            else
            {
                StartHand();
            }
        }
        catch (System.Exception e)
        {
            _broken = true;
            Entry.Logger.Error($"德州：牌桌搭建失败，已自动收桌: {e}");
            Entry.Toast($"德州牌桌起不来：{e.Message}", "德州扑克");
            CallDeferred(MethodName.QueueFree);
        }
    }

    /// <summary>整层贴住可见视口 —— 根 Control 的锚点不可信，所以直接设位置和尺寸。</summary>
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

    /// <summary>
    /// **编外角色的待机动画** —— 本体角色有游戏自己的活模型，四位客人没有，
    /// 只能靠她们自己那 96 帧循环（<c>assets/&lt;id&gt;/idle/fNN.png</c>，12fps）。
    ///
    /// <para>★ 2026-10-11 加：以前德州这一格直接贴 `seats/char_&lt;id&gt;.png` 一张死图，
    /// 而同样这四位在斗地主里是会动的 —— 用户一眼看出来（"为什么预览是这些静态图"）。
    /// 现在三张桌子共用 <see cref="DouSpireCharacters.IdleClock"/>。</para>
    ///
    /// <para>**异常只吞不抛**：贴图失败最多是这一帧不动，绝不能让牌桌停摆
    /// （2026-10-08 那次"进去卡死"就是 SetTexture 抛出去把整张桌子打停的）。</para>
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

    public override void _Process(double delta)
    {
        // 高潮曲到点自己退场 —— 必须在所有 return 之前（挂机不动时 Refresh 不跑）。
        DouSpireMusic.Tick();

        // 待机动画也必须在所有 return 之前 —— 下注阶段 / 等对手 / 摊牌展示都要一直动。
        TickGuestIdle(delta);

        if (_broken) return;
        if (_game is null) return;          // 联机桌：主机还没说"开这手"，什么都不推进

        try
        {
            var seat = _game.CurrentSeat;
            var isMine = seat == MeSeat;
            // 远端真人 = 联机桌 + 不是我的席 + 是真人席（不是电脑补位）
            var remoteHuman = DouSpirePokerNet.Active && !isMine && DouSpirePokerNet.IsHumanSeat(seat);

            if (!_game.HandOver && isMine && _autoPilot)
            {
                // ★ 鸡煲代打：轮到我时 AI 替我做决定（走 MyMove —— 联机客机会自动转成"发给主机"）。
                //   这一支必须排在下面"等按钮"的前面，否则轮到我这一帧就被 else 吞掉了。
                _aiTimer -= delta;
                if (_aiTimer <= 0)
                {
                    _aiTimer = Mathf.Max(0.4f, DouSpireConfig.AiDelay);
                    AutoMove();
                }
            }
            else if (!_game.HandOver && !isMine)
            {
                if (remoteHuman)
                {
                    // ★ 远端真人的回合：**主机不许替他走**，等他的消息。
                    //   但绝不能因为一个人挂机就把整桌卡死 —— 等够久就用兜底动作替他走完。
                    if (DouSpirePokerNet.IsHost)
                    {
                        _remoteWait += delta;
                        if (_remoteWait >= RemoteTurnTimeoutSeconds)
                        {
                            Entry.Logger.Warn($"德州：{seat} 号位 {RemoteTurnTimeoutSeconds:0} 秒没动，"
                                              + "替他走一步兜底（能过牌就过牌，否则弃牌）。");
                            _remoteWait = 0;
                            AiStep(forceFallback: true);
                        }
                    }
                }
                else if (!DouSpirePokerNet.Active || DouSpirePokerNet.IsHost)
                {
                    // 电脑席（单人的电脑 / 联机桌补位的电脑）—— **只有主机（或单人）才跑 AI**，
                    // 客机跑 AI 会跟主机算出不同的结果，直接把牌局带沟里。
                    _aiTimer -= delta;
                    if (_aiTimer <= 0) AiStep();
                }
            }
            else
            {
                _remoteWait = 0;
            }

            // ── 轮到我 / 轮到谁：播一声"该你了"（换人才响）──
            if (!_game.HandOver && isMine && _lastSfxSeat != MeSeat)
            {
                _lastSfxSeat = MeSeat;
                DouSpireSfx.MyTurn();
                Speak(MeView, DouSpireCharacters.Mood.MyTurn);   // 语音和音效同一时机："该我了"
            }
            else if (!_game.HandOver && !isMine)
            {
                _lastSfxSeat = -2;
            }
        }
        catch (System.Exception e)
        {
            _broken = true;
            Entry.Logger.Error($"德州：牌局推进出错，已自动收桌: {e}");
            Entry.Toast("德州牌局出错了，看日志。", "德州扑克");
            CallDeferred(MethodName.QueueFree);
        }
    }

    /// <summary>联机桌刚摆好、主机还没说"开这手"时的样子：一句等待提示 + 不出任何按钮。</summary>
    private void ShowWaitingForHost()
    {
        try
        {
            _titleLabel.Text = "德州扑克尖塔 · 联机桌";
            _statusLabel.Text = "正在等主机发牌…";
            _hintLabel.Text = "";
            _logLabel.Text = "";
            RefreshButtons();
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"德州：显示等待提示失败（不影响开手）: {e.Message}");
        }
    }

    // ════════════════════════════════════════════════════════
    //  开局
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 定三家角色。赌注模式（篝火）用**我 run 里的角色**坐 0 号位，其余按种子分配；
    /// 普通开桌则整个随机。
    /// </summary>
    private void InitCharacters()
    {
        var n = TableN;

        // ★ 联机桌：角色由**主机**定好、跟开桌消息一起发过来了
        //   （DouSpirePokerNet.CharIndexOfSeat 给的是全局座位 → 发牌池下标）。
        //   各端按同一份表摆，才不会"我这边他是铁甲战士、你那边是静默猎手"。
        if (DouSpirePokerNet.Active)
        {
            _chars = new DouSpireCharacters.SeatPick[4];
            for (var g = 0; g < n; g++)
                _chars[V(g)] = DouSpireCharacters.PickAt(DouSpirePokerNet.CharIndexOfSeat(g));
            _seed = System.Random.Shared.Next();     // 联机每手的种子由主机广播，这里只是占位
            Entry.Logger.Info("德州（联机）：座位 = "
                + string.Join("、", Enumerable.Range(0, n).Select(s => SeatName(G(s)))));
            return;
        }

        // 赌注模式（篝火）用**我 run 里的角色**坐 0 号位；普通开桌整个随机。
        // 写法照 DouSpireTable：SeatPick(CharacterModel?, GuestId?)。
        var mine = default(DouSpireCharacters.SeatPick);
        if (_stakePlayer is not null)
        {
            var me = DouSpireCharacters.MyRunCharacter();
            if (me is not null) mine = new DouSpireCharacters.SeatPick(me, null);
        }

        _seed = System.Random.Shared.Next();
        _chars = DouSpireCharacters.Assign(mine, _seed, n);

        Entry.Logger.Info("德州：三家 = "
            + string.Join("、", Enumerable.Range(0, n).Select(s => SeatName(s))));
    }

    /// <summary>某一席显示的名字。<paramref name="globalSeat"/> 是**全局**座位号。</summary>
    private string SeatName(int globalSeat)
    {
        if (globalSeat == MeSeat) return "我";
        // 联机桌优先用主机广播过来的名字（离桌改电脑接管后这个名字会变）
        if (DouSpirePokerNet.Active)
        {
            var nn = DouSpirePokerNet.NameOfSeat(globalSeat);
            if (!string.IsNullOrWhiteSpace(nn)) return nn;
        }
        var nm = DouSpireCharacters.Name(_chars[V(globalSeat)]);
        return string.IsNullOrWhiteSpace(nm) ? $"电脑{(char)('A' + V(globalSeat) - 1)}" : nm;
    }

    /// <summary>单人 / 主机开一手（种子和庄家位自己定）。</summary>
    private void StartHand() => StartHand(0, -1, -1, -1, -1);

    /// <summary>
    /// 开一手新牌。
    ///
    /// ★ 联机的关键是**这一手的种子、庄家位、筹码、盲注必须各端完全一样** ——
    /// 所以由**主机算一次、广播给所有人**（<see cref="DouSpirePokerNet.CastHand"/>），
    /// 客机拿到什么用什么，**绝不自己派生**。四样里只要有一样不同，从这一手开始就分叉了。
    /// </summary>
    private void StartHand(int seed, int button, int stack, int sb, int bb)
    {
        var n = TableN;

        // ── 盲注 / 筹码 ──
        int startStack, bigBlind, smallBlind;
        var fromHost = DouSpirePokerNet.Active && !DouSpirePokerNet.IsHost;

        if (fromHost)
        {
            // 客机：一律用主机广播的四个数
            _seed = seed;
            _button = button;
            startStack = stack;
            smallBlind = sb;
            bigBlind = bb;
        }
        else
        {
            ComputeStakes(out startStack, out bigBlind, out smallBlind);
            _button = _button < 0 ? _rng.Next(n) : (_button + 1) % n;
            _seed = unchecked(_seed * 31 + 17);      // 每手换一个种子 → 每手牌都不一样
        }

        // ★ 兜底（2026-10-11）：HoldemGame 对盲注是**硬约束** ——
        //   `smallBlind <= 0 || bigBlind <= smallBlind` 直接抛 ArgumentException，
        //   抛出来会被 `_Ready` 的 catch 接住、整桌收掉（用户看到的就是
        //   「德州牌桌起不来：盲注不合法」）。两条路径（本地算 / 主机广播）都在
        //   这里过一道，保证送进内核的数**一定合法**。
        //   ⚠️ 必须是**纯函数式**的 clamp —— 各端输入相同则输出相同，否则联机会分叉。
        if (smallBlind < 1) smallBlind = 1;
        if (bigBlind <= smallBlind) bigBlind = smallBlind + 1;
        if (startStack < bigBlind * 2) startStack = bigBlind * 2;   // 至少 2 个大盲，不然发不出牌

        _startStack = startStack;
        _bigBlind = bigBlind;
        _smallBlind = smallBlind;

        // ★ names 必须是**全局座位顺序** —— 内核按这个顺序发牌，
        //   各端顺序不一致的话同一个人拿到的底牌就不一样了。
        var names = Enumerable.Range(0, n).Select(SeatName).ToArray();
        var stacks = Enumerable.Repeat(startStack, n).ToArray();

        _handNo++;

        _game = HoldemGame.NewGame(_seed, names, startStack: startStack,
                                   smallBlind: smallBlind, bigBlind: bigBlind,
                                   button: _button, stacks: stacks);

        _paidOut = false;
        _revealAll = false;
        _bestFive.Clear();
        // 开局先停一拍再让电脑说话 —— 否则电脑在发牌动画还没看清时就抢着行动，节奏很怪。
        _aiTimer = Mathf.Max(0.5f, DouSpireConfig.AiDelay * 0.7f);
        _lastSfxSeat = -1;
        _handStarted = true;
        _waitingHost = false;
        _remoteWait = 0;
        HideResult();

        // ★ 主机：把这一手的四个数广播出去（客机就靠这条消息开手）
        if (DouSpirePokerNet.Active && DouSpirePokerNet.IsHost)
            DouSpirePokerNet.CastHand(_seed, _button, startStack, smallBlind, bigBlind);

        Entry.Logger.Info($"德州：第 {_handNo} 手开局 —— 盲注 {smallBlind}/{bigBlind}，"
                          + $"每人 {startStack} 筹码，庄家位 {SeatName(_button)}（{_button} 号）"
                          + (DouSpirePokerNet.Active ? $"【联机 第 {DouSpirePokerNet.Gen} 代】" : "") + "。");

        DouSpireSfx.Deal();
        DouSpireSfx.MusicCalm();
        // ★ 语音：发牌 —— **只让我这一席开口**。三家同时说会糊成一团（斗地主那边
        //   `Mood.Deal` 也只给 MeSeat，见 DouSpireTable 的三处 WhaleReact(MeSeat, Deal)）。
        Speak(MeView, DouSpireCharacters.Mood.Deal);
        Refresh();
    }

    /// <summary>
    /// 算这一桌的筹码和盲注。
    ///
    /// **普通开桌**（主菜单 / 调试）：每人 1000，大盲 = 设置里的底注。
    /// **赌注模式**（篝火）：每人筹码 = 我身上金币的 <see cref="DouSpireConfig.BetMaxSwingPercent"/>%
    /// —— 这是那条"不能让我赢太多、也不能让我输太多"的安全阀（照斗地主的规矩）。
    /// 盲注取筹码的 1/50（= 50 个大盲的深度，正常牌局手感）。
    /// </summary>
    private void ComputeStakes(out int stack, out int bigBlind, out int smallBlind)
    {
        if (_stakePlayer is null)
        {
            stack = 1000;
            bigBlind = Mathf.Max(2, DouSpireConfig.BaseStake);
        }
        else
        {
            var gold = Mathf.Max(0, SafeGold(_stakePlayer));
            var pct = Mathf.Clamp(DouSpireConfig.BetMaxSwingPercent, 1, 100);
            // ★★ 2026-10-11 修：**本金和大盲都要有下限**。
            //
            //   原来照 25% 算：99 金币 ⇒ 本金 24 ⇒ 大盲 `Max(1, 24/50)` 落到 **1**，
            //   小盲 `Max(1, 1/2)` 也落到 **1** ⇒ 撞上内核 HoldemGame 的
            //   `if (smallBlind <= 0 || bigBlind <= smallBlind) throw new ArgumentException("盲注不合法")`
            //   ⇒ 整桌起不来、当场收掉。用户报的「火堆我进不去德州扑克」就是这个。
            //   （金币 ≤ 0 那条路在 OpenStaked 里已经先拦掉了，所以这里 gold 一定 > 0，
            //     真正会踩坑的是 1~99 这一档。）
            //
            //   本金下限 20 ⇒ 至少 10 个大盲的深度，牌局铺得开；
            //   大盲下限 2  ⇒ 保证**严格大于**小盲（小盲下限是 1）。
            stack = Mathf.Max(20, gold * pct / 100);
            bigBlind = Mathf.Max(2, stack / 50);
        }
        smallBlind = Mathf.Max(1, bigBlind / 2);
    }

    /// <summary>
    /// 再来一手。
    ///
    /// ★ **每一手都重新发同样多的筹码**（不把上一手的筹码带过来）——
    /// 这是刻意的：德州只要允许"某人筹码归零"，就要处理"他没钱了怎么算"（踢出去 / 只能全下旁观），
    /// 那是另一整套逻辑。现在的口径是**一手一结算**，输赢记在 <see cref="_myTotal"/> 上，
    /// 桌上随时人人都有筹码，永远不会出现"某席 0 筹码发不出牌"。
    /// 赌注模式下每一手都重新读一遍我现在的金币，所以**赢了的下一手会玩得更大**（渐进感）。
    /// </summary>
    private void NextHand()
    {
        // ★ 联机客机：**不能自己发牌**（种子在主机那儿）—— 只请求主机开下一手。
        if (DouSpirePokerNet.Active && !DouSpirePokerNet.IsHost)
        {
            DouSpirePokerNet.SendNextHand();
            return;
        }
        StartHand();
    }

    // ════════════════════════════════════════════════════════
    //  电脑走子
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 电脑（或超时兜底）替当前这一席走一步。
    ///
    /// ★ 联机时**只有主机能调**，而且走完必须广播 —— 否则客机那边的牌局会停在这一步。
    /// </summary>
    private void AiStep(bool forceFallback = false)
    {
        var seat = _game.CurrentSeat;

        PokerAction action;
        int amount;
        if (forceFallback) (action, amount) = SafeFallback();
        else (action, amount) = PokerAi.Choose(_game, DouSpireConfig.PokerStyle(), _rng);

        if (!_game.Apply(action, amount, out var err))
        {
            // 电脑算出来的招不合法（理论上不该发生）→ 退回"能不花钱就不花钱"，绝不卡死牌局。
            Entry.Logger.Warn($"德州：{SeatName(seat)} 想走 {action}({amount}) 被拒（{err}），退回兜底。");
            (action, amount) = SafeFallback();
            _game.Apply(action, amount, out _);
        }

        // ★ 主机：本地走完立刻广播（客机靠这条消息照做）
        if (DouSpirePokerNet.Active && DouSpirePokerNet.IsHost)
            DouSpirePokerNet.CastMove(seat, action, amount);

        DouSpireSfx.Select();
        ReactActor(V(seat), action);
        AfterMove();
    }

    /// <summary>兜底动作：能过牌就过牌，否则弃牌（**绝不卡死牌局**）。</summary>
    private (PokerAction, int) SafeFallback()
    {
        var legal = _game.LegalActions();
        if (legal.Contains(PokerAction.Check)) return (PokerAction.Check, 0);
        if (legal.Contains(PokerAction.Fold)) return (PokerAction.Fold, 0);
        return (legal.Count > 0 ? legal[0] : PokerAction.Check, 0);
    }

    // ════════════════════════════════════════════════════════
    //  鸡煲代打（2026-10-11：用户"所有模式都加鸡煲代打"）
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 「鸡煲代打」开关：点一下切换。开着时 <see cref="AutoMove"/> 会在我的每个回合
    /// 按 <see cref="DouSpireConfig.AiDelay"/> 的节奏替我做决定。
    /// </summary>
    private void ToggleAutoPilot()
    {
        _autoPilot = !_autoPilot;
        UpdateAutoBtn();
        // 刚打开时别立刻出手 —— 先让计时器走满一个 AiDelay，观感上"AI 接手了"。
        if (_autoPilot) _aiTimer = Mathf.Max(0.4f, DouSpireConfig.AiDelay);
        Entry.Logger.Info($"德州：鸡煲代打 {(_autoPilot ? "开" : "关")}。");
        Refresh();
    }

    /// <summary>把按钮上的字刷成当前状态（按钮可能还没建出来，所以判空）。</summary>
    private void UpdateAutoBtn()
    {
        if (_autoBtn is null || !GodotObject.IsInstanceValid(_autoBtn)) return;
        _autoBtn.Text = _autoPilot ? "✓ 鸡煲代打中" : "鸡煲代打";
    }

    /// <summary>
    /// 代打替「我」选一步并走掉。
    ///
    /// ★ 走 <see cref="MyMove"/> 而**不是** <see cref="AiStep"/> —— MyMove 内部已经把
    ///   "主机本地落地 + 广播"和"客机只发给主机"两条路分好了（见它的注释），
    ///   代打不用再判一遍联机，也不会像 AiStep 那样在客机上本地乱推。
    ///
    /// 算出来的招不合法就退回兜底（能过牌就过牌，否则弃牌），**绝不卡死牌局**。
    /// </summary>
    private void AutoMove()
    {
        if (_broken || _game is null || _game.HandOver) return;
        if (_game.CurrentSeat != MeSeat) return;

        var (action, amount) = PokerAi.Choose(_game, DouSpireConfig.PokerStyle(), _rng);
        if (!_game.LegalActions().Contains(action)) (action, amount) = SafeFallback();
        MyMove(action, amount);
    }

    /// <summary>一步走完之后：换人的计时、结算、重绘。</summary>
    private void AfterMove()
    {
        if (_game.HandOver) OnHandOver();
        else _aiTimer = Mathf.Max(0.25f, DouSpireConfig.AiDelay);

        Refresh();
    }

    /// <summary>我按了某个动作。</summary>
    private void MyMove(PokerAction action, int amount)
    {
        if (_broken || _game is null || _game.HandOver) return;
        if (_game.CurrentSeat != MeSeat) return;
        if (_waitingHost) return;        // 上一步还没等到主机回声，别重复发

        // ★ 联机客机：**自己不应用**，只把动作发给主机，等主机广播回来才走
        //   （见 DouSpirePokerNet 顶部"为什么客机不自己应用"的说明）。
        if (DouSpirePokerNet.Active && !DouSpirePokerNet.IsHost)
        {
            HideRaiseRow();
            _waitingHost = true;
            DouSpirePokerNet.SendAct(action, amount);
            Refresh();
            return;
        }

        if (!_game.Apply(action, amount, out var err))
        {
            Entry.Toast($"这步走不了：{err}", "德州扑克");
            Entry.Logger.Info($"德州：我的 {action}({amount}) 被拒 —— {err}");
            return;
        }

        // ★ 主机：本地走完立刻广播
        if (DouSpirePokerNet.Active && DouSpirePokerNet.IsHost)
            DouSpirePokerNet.CastMove(MeSeat, action, amount);

        DouSpireSfx.Select();
        HideRaiseRow();
        ReactActor(MeView, action);
        AfterMove();
    }

    // ════════════════════════════════════════════════════════
    //  联机入口（只有 DouSpirePokerNet 会调）
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 联机开桌。座位表 / 角色由 <see cref="DouSpirePokerNet"/> 先摆好，这里只负责把桌子搭起来。
    /// </summary>
    public static void OpenForNet(Player? me, Logger logger, bool stake, int seats)
    {
        _stakePlayer = stake ? (me ?? ResolveMyPlayer()) : null;
        Open(logger, keepStake: true);
        DouSpirePokerNet.Live = IsOpen ? _instance : null;
    }

    /// <summary>
    /// 主机说"开这一手"（客机走这里）。参数**全部来自主机** —— 客机绝不自己派生种子。
    /// </summary>
    public void NetStartHand(int seed, int button, int stack, int smallBlind, int bigBlind)
    {
        if (_broken) return;
        if (_game is not null && !_game.HandOver)
        {
            Entry.Logger.Warn("德州：收到开手消息，但这一手还在打，忽略。");
            return;
        }
        StartHand(seed, button, stack, smallBlind, bigBlind);
    }

    /// <summary>
    /// 某一席走了这一步。**主机本地应用**和**客机照做主机广播**都走这里。
    /// 返回 true = 真的应用成功了（主机据此决定要不要广播）。
    /// </summary>
    public bool NetMove(int globalSeat, PokerAction action, int amount)
    {
        if (_broken || _game is null) return false;
        if (_game.HandOver) return false;

        if (_game.CurrentSeat != globalSeat)
        {
            Entry.Logger.Warn($"德州：{globalSeat} 号位想走子，但现在轮到 {_game.CurrentSeat} 号 —— 忽略。");
            return false;
        }

        if (!_game.Apply(action, amount, out var err))
        {
            Entry.Logger.Warn($"德州：{globalSeat} 号位的 {action}({amount}) 不合法（{err}）—— 忽略。");
            return false;
        }

        if (globalSeat == MeSeat)
        {
            _waitingHost = false;
            HideRaiseRow();
        }

        DouSpireSfx.Select();
        ReactActor(V(globalSeat), action);
        AfterMove();
        return true;
    }

    /// <summary>某一席改成电脑接管了（离桌 / 超时）—— 重画一遍让名字和标记跟上。</summary>
    public void NetSeatAi(int globalSeat)
    {
        if (_broken || _game is null) return;
        Refresh();
    }

    /// <summary>主机收桌了。</summary>
    public void NetClosed()
    {
        Entry.Logger.Info("德州：主机收桌，跟着收。");
        Close(Entry.Logger);
    }

    /// <summary>有人按了"下一手"（主机侧入口）。多人同时按也只开一次。</summary>
    internal void HostNextHand(int globalSeat)
    {
        if (_broken || _game is null) return;
        if (!_game.HandOver)
        {
            Entry.Logger.Info($"德州：{globalSeat} 号位按了下一手，但这手还没结束 —— 忽略。");
            return;
        }
        Entry.Logger.Info($"德州：{globalSeat} 号位按了下一手，开牌。");
        StartHand();
    }

    // ════════════════════════════════════════════════════════
    //  结算
    // ════════════════════════════════════════════════════════

    private void OnHandOver()
    {
        _myTotal += _game.Players[MeSeat].Delta;

        if (_game.WinKind == "showdown")
        {
            _revealAll = true;
            // 赢家那 5 张圈出来 —— 观众一眼看懂"为什么他赢"（这是德州最容易让人懵的地方）。
            var w = _game.Winners.FirstOrDefault(-1);
            if (w >= 0)
            {
                var seven = new List<PokerCard>(7);
                seven.AddRange(_game.Players[w].Hole);
                seven.AddRange(_game.Board);
                if (seven.Count >= 5)
                {
                    HandEval.Best(seven, out var five);
                    foreach (var c in five) _bestFive.Add(c);
                }
            }
        }

        PayOut();

        if (_game.Players[MeSeat].Delta > 0) DouSpireSfx.Win();
        else if (_game.Players[MeSeat].Delta < 0) DouSpireSfx.Lose();

        // _game.Winners 是**全局**座位号，画面上要用视角位
        // ★ 语音：**每一席都开口**（赢的喊赢、其余喊输）。但**只有赢家演赢家动作** ——
        //   输家的"输"若也走 ReactActor，它会跟着播 PlayWin 的动画，那就错了。
        //   所以赢家走 ReactActor（动作 + 赢语音），其余席单独 Speak 一句"输"。
        foreach (var s in _game.Winners) ReactActor(V(s), PokerAction.Check, win: true);
        for (var s = 0; s < SeatCount; s++)
            if (!_game.Winners.Contains(G(s)))
                Speak(s, DouSpireCharacters.Mood.Lose);   // s 是视角位，Speak 收的就是视角位

        ShowResult();
        Entry.Logger.Info($"德州：第 {_handNo} 手结束 —— {_game.ResultText}"
                          + $"（我 {(Delta(MeSeat) >= 0 ? "+" : "")}{Delta(MeSeat)}，累计 {(_myTotal >= 0 ? "+" : "")}{_myTotal}）");
    }

    private int Delta(int seat) => _game.Players[seat].Delta;

    /// <summary>
    /// 赌注模式：把这一手**我这一席的输赢**落到玩家的真实金币上（1 筹码 = 1 金币）。
    ///
    /// 直接改 <c>Player.Gold</c>，不走 <c>PlayerCmd</c> —— 和斗地主那边同一个理由：
    /// 那两个是 async 且要过战斗钩子，篝火处 <c>CombatState</c> 可能是 null，容易在钩子里炸。
    /// 输了不会变负数（钳到 0）。
    /// </summary>
    private void PayOut()
    {
        if (_paidOut) return;
        _paidOut = true;

        // ★ 联机：**只有主机结算金币**（主机是权威），而且**给每一席都结**（用主机手上的
        //   各席 Player 对象写回他们自己的金币，再靠游戏自己的玩家状态同步发到各端）。
        //   客机一律不动 —— 否则主客两边都写同一份金币，或者客机刚写完就被主机同步覆盖。
        if (DouSpirePokerNet.Active)
        {
            if (!DouSpirePokerNet.IsHost) return;
            for (var g = 0; g < SeatCount; g++)
            {
                var p = DouSpirePokerNet.PlayerOfSeat(g);
                if (p is null) continue;          // 电脑补位的席没有真玩家，不结
                var dg = Delta(g);
                if (dg == 0) continue;
                try
                {
                    var before = p.Gold;
                    p.Gold = Mathf.Max(0, before + dg);
                    Entry.Logger.Info($"德州（联机结算）：{g} 号位 {(dg >= 0 ? "+" : "")}{dg}"
                                      + $" → 金币 {before} → {p.Gold}");
                }
                catch (System.Exception e)
                {
                    Entry.Logger.Warn($"德州：写 {g} 号位金币失败（只影响这一手）: {e.Message}");
                }
            }
            return;
        }

        // 单人：只结算我自己。
        var player = _stakePlayer;
        if (player is null) return;

        var d = Delta(MeSeat);
        if (d == 0) return;

        try
        {
            var before = player.Gold;
            player.Gold = Mathf.Max(0, before + d);
            Entry.Logger.Info($"德州：赌注结算 {(d >= 0 ? "+" : "")}{d} → 金币 {before} → {player.Gold}");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"德州：写金币失败（只影响这一手）: {e.Message}");
        }
    }

    // ════════════════════════════════════════════════════════
    //  搭场景
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 背景图还没画出来时的垫底：**一层暖色渐变**（上方胡桃木墙 → 下方台呢绿地面）。
    ///
    /// ★ 为什么不退到斗地主那张酒馆：用户看到的就是"酒馆 + 暗角" =「旁边发暗不好看」。
    ///   渐变至少和绿绒赌桌是同一套味道，而且**四周是亮的**。
    ///   美术把 <c>assets/bg/poker_hall.png</c> 放进去之后，这一层会自动被顶掉。
    /// </summary>
    private static TextureRect PokerRoomFallback()
    {
        var grad = new Gradient
        {
            // 上：胡桃褐（#5A4230 提亮一点）→ 中：牛血红压暗 → 下：深台呢绿
            Colors = new[]
            {
                new Color(0.35f, 0.25f, 0.17f),
                new Color(0.28f, 0.15f, 0.13f),
                new Color(0.12f, 0.22f, 0.18f),
                new Color(0.09f, 0.17f, 0.14f),
            },
            Offsets = new[] { 0f, 0.42f, 0.70f, 1f },
        };
        var tex = new GradientTexture2D
        {
            Gradient = grad,
            Width = 4,
            Height = 256,
            FillFrom = new Vector2(0.5f, 0f),
            FillTo = new Vector2(0.5f, 1f),
        };
        var tr = new TextureRect
        {
            Texture = tex,
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };
        return DouSpireUi.Fill(tr);
    }

    private void BuildScene()
    {
        _scene = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(DouSpireUi.Fill(_scene));

        // ① 兜底纯色 → ② **德州自己的**整屏背景（没有就垫一层暖色渐变）
        // ③ 暗角：**不画了**（理由见下）
        // ④ 桌子（铺得更大，理由见下）
        //
        // ★★ 2026-10-09 用户：「**这个桌牌没铺满，旁边也是暗的不好看**」——
        //    现场是两件事叠在一起：
        //    ① 背景借的是 DouSpireAssets.Bg，而那条链的第一张 bg/hall.png 是
        //       **斗地主那间酒馆**（圆桌烤在画里的那种暗调酒馆）→ 德州桌"摆在酒馆里"；
        //    ② 上面还压了一层暗角（fx/vignette.png 取不到就退游戏自带的 vignette_2.png）
        //       → 四角更黑。
        //    现在：背景走 DouSpireAssets.PokerBgOf(PokerScene)（**一个场景一张图**，
        //    玩家能切，见 ApplyScene），**暗角不画了** ——
        //    用户嫌的就是暗，再压一层等于白画；真需要暗角的话背景图自己带。
        _felt = new ColorRect { Color = DouSpireUi.Deep, MouseFilter = MouseFilterEnum.Ignore };
        _scene.AddChild(DouSpireUi.Fill(_felt));
        ApplyScene();

        // ④ 桌子 —— **铺得更大**（原来 1180×660：屏幕 1920 宽 → 左右各空 370px，
        //    这就是用户说的"没铺满"）。现在 1580×846，正好是原图比例（1654×886）等比放大。
        //    ★ 抽成 ApplyTable()，是为了牌桌上那个「牌桌：…」按钮能**当场换一张**（见 CycleTable）。
        ApplyTable();

        // ⑤ 三家立绘 + HUD
        BuildSeats();

        // ⑥ 底池 —— **牌匾 + 一摞筹码 + 数字**（2026-10-09 接素材）
        //   ★ 以前这里只有一行黄字。用户画的 `poker/pot_plate.png`（底池牌匾）和
        //     `poker/chip_stack.png`（一摞筹码）18:51 就入库了，但代码一条都没引用 ——
        //     他问「**要我画的那些筹码和那些 ui 怎么没用上？**」。现在接上。
        //   ★ 牌匾取不到就退回纯色块；筹码取不到就不摆 —— 底池照样有字，不会白块。
        //   ★★ 2026-10-11 牌匾改画法 ★★
        //    以前用 StyleBoxTexture（九宫格）。Godot 的九宫格**边角是 1:1 画的、不跟着缩放**
        //    （见 DouSpireUi.Plate 的注释），而这张图 971×256、控件只有 210×55 ——
        //    于是四个角的金铆钉保持原始大小、显得又粗又大，中间的红底被挤成一条缝。
        //    现在改成自己切三块（见 BuildPotPlate）。
        var potBg = _potBg = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        _scene.AddChild(potBg);

        _potLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        DouSpireUi.StyleFont(_potLabel);
        _potLabel.SetMeta("douspire_big", true);
        _potLabel.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.78f));
        _potLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(24));
        potBg.AddChild(DouSpireUi.Fill(_potLabel));

        // 牌匾左边摆一摞筹码（★ 2026-10-10：44×80 → 56×104。原来太小，跟牌匾一比像根小牙签）
        _potChips = DouSpireUi.Sprite(DouSpireAssets.PokerChipStack,
            0.5f, PotAy, DouSpireUi.Px(56), DouSpireUi.Px(104));
        if (_potChips != null) _scene.AddChild(_potChips);

        // ⑦ 公共牌区（5 张）
        //   ★ 2026-10-10 接素材：底下垫一块 `poker/board_plate.png`（自带 5 个凹槽的绿呢牌匾）。
        //     以前这张图没被引用是因为"会跟台面刻线打架" —— 解法：**只在经典绿绒那张台面上铺**
        //     （那张台面没有刻槽，牌匾正好补上）；另外 4 张台面自己带凹槽，铺上去是两条线，
        //     所以 <see cref="PlacePotAndBoard"/> 里按当前台面决定它的显隐。
        //     ⚠️ 先加牌匾、再加牌排 —— 后加的画在上面，牌才不会被牌匾盖住。
        _boardPlate = DouSpireUi.Sprite(DouSpireAssets.PokerBoardPlate,
            0.5f, BoardAy, DouSpireUi.Px(760), DouSpireUi.Px(190));
        if (_boardPlate != null) _scene.AddChild(_boardPlate);

        _boardRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _scene.AddChild(_boardRow);

        // ⑥⑦ 一起摆 —— 位置**跟着台面刻线走**（见 ApplyMarks 那张实测表）
        PlacePotAndBoard();

        // ⑧ 标题
        _titleLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, Text = "德州扑克尖塔" };
        DouSpireUi.StyleFont(_titleLabel);
        _titleLabel.SetMeta("douspire_big", true);
        _titleLabel.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        _titleLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(30));
        _scene.AddChild(DouSpireUi.Place(_titleLabel, 0.5f, TitleAy, DouSpireUi.Px(900), DouSpireUi.Px(44)));

        // ⑨ 中央状态条（"轮到你" / "电脑A 加注到 300"）
        _statusLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        DouSpireUi.StyleFont(_statusLabel);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.93f, 0.84f));
        _statusLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(22));
        _scene.AddChild(DouSpireUi.Place(_statusLabel, 0.5f, StatusAy, DouSpireUi.Px(900), DouSpireUi.Px(40)));

        // ⑩ 「你现在是什么牌」+ 建议 —— 新手友好第 1 条，最值钱的那条
        //   ★ 2026-10-09 接素材：底下垫 `poker/handrank_plate.png`（牌型牌匾）。
        //     以前只有一行黄字浮在桌上 —— 现在有块真的牌子。
        //     ⚠️ 牌匾是**横牌**：两端装饰帽保住、只拉中间那段（见 DouSpireUi.Plate）。
        //   ★ 2026-10-10 再加一枚**牌力图标**（`poker/rank_1`~`rank_10`）：牌匾左端留一格
        //     专门摆它，文字整体右偏一点给它让位（图标见 RefreshHintIcon）。
        var hintBg = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        //   ★★ 2026-10-11 用户："提示你牌型咋样的这个板子……感觉被拉伸过或者挤压过" ★★
        //     原图 1024×193（比例 5.31），控件 900×58（比例 15.5）—— 横向要拉 2.9 倍。
        //     以前用 StyleBoxTexture，而 Godot 的九宫格**边角 1:1 画、不缩放**：
        //     左右各保护 204 px（= 图宽 1/5）却只画在 1200 宽的框里，
        //     上下各保护 38 px 而框只有 77 高 —— 中间那段被压成一条缝、金边粗得离谱。
        //     现在自己切三块：左右装饰帽按高度等比缩（形状和原图一样），只拉中间。
        if (DouSpireAssets.First(DouSpireAssets.PokerHandRankPlate) is { } rankTex)
        {
            hintBg.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            // 左帽 104 px：这张图**左端有一枚圆形徽章**（直径约 100 px），
            //   切少了会把徽章劈成两半、一半被拉走；右帽 66 px 是右侧的圆角。
            //   两个数都是从原图轮廓的平直段量出来的。
            hintBg.AddChild(DouSpireUi.Plate(rankTex, 104, 66, DouSpireUi.Px(900), DouSpireUi.Px(58)));
        }
        else
        {
            hintBg.AddThemeStyleboxOverride("panel",
                DouSpireUi.Box(new Color(0.10f, 0.05f, 0.05f, 0.80f), 12, DouSpireUi.Gold, 2));
        }
        _scene.AddChild(DouSpireUi.Place(hintBg, 0.5f, HintAy, DouSpireUi.Px(900), DouSpireUi.Px(58)));

        // 牌力图标（左端一格）；取不到图就不占位
        //   ★ 2026-10-11 往右挪了 39 设计 px（-430 → -391）：牌匾改画法之后，
        //     左端那枚**圆形徽章**（原图里就在最左边，直径约 100 px）第一次真正显示出来了，
        //     而图标原来正好压在上面。现在图标落在徽章右边，两个都看得见。
        _hintIcon = DouSpireUi.Sprite(DouSpireAssets.PokerRankIcon(1),
            0.5f, HintAy, DouSpireUi.Px(46), DouSpireUi.Px(46),
            -DouSpireUi.Px(391));
        if (_hintIcon != null) _scene.AddChild(_hintIcon);

        _hintLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        DouSpireUi.StyleFont(_hintLabel);
        _hintLabel.SetMeta("douspire_big", true);
        _hintLabel.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.78f));
        _hintLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(22));
        // 给左端「徽章 + 牌力图标」留出地方，文字往右偏一点（图标取不到时它照旧居中，看不出来）。
        // ★ 2026-10-11 56 → 90：要让开的不再只是那枚图标，还有牌匾自带的圆形徽章（约 42 px）。
        var hintPad = _hintIcon != null ? DouSpireUi.Px(90) : 0f;
        hintBg.AddChild(DouSpireUi.Place(_hintLabel, 0.5f, 0.5f, DouSpireUi.Px(860) - hintPad, DouSpireUi.Px(44),
            hintPad / 2f));

        // ⑪ 我的两张底牌
        _handRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _handRow.AddThemeConstantOverride("separation", DouSpireUi.Gap(14));
        _scene.AddChild(DouSpireUi.Place(_handRow, 0.5f, HandAy,
            DouSpireUi.Px(MyCardW * 2 + 40), DouSpireUi.Px(MyCardW * 1.4f)));

        // ⑫ 动作按钮排（按 LegalActions 动态显隐，按钮上直接写要掏多少钱）
        _actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _actions.AddThemeConstantOverride("separation", DouSpireUi.Gap(12));
        // ★ 2026-10-09：5 个主要动作挂上美术画的图标（poker/act_*.png，18:51 入库但一直没接）
        _foldBtn = MakeActionButton("弃牌", () => MyMove(PokerAction.Fold, 0), DouSpireAssets.PokerActFold);
        _checkBtn = MakeActionButton("过牌", () => MyMove(PokerAction.Check, 0), DouSpireAssets.PokerActCheck);
        _callBtn = MakeActionButton("跟注", () => MyMove(PokerAction.Call, 0), DouSpireAssets.PokerActCall);
        _raiseBtn = MakeActionButton("加注", ShowRaiseRow, DouSpireAssets.PokerActRaise);
        _allinBtn = MakeActionButton("全下", () => MyMove(PokerAction.AllIn, 0), DouSpireAssets.PokerActAllIn);
        // 「鸡煲代打」（2026-10-11）：点一下 = AI 用当前性格替我打完这手；再点一下收回。
        // 跟出牌那几个按钮同一套底图、同一个尺寸（跟斗地主那张桌保持一致）。
        _autoBtn = MakeActionButton(_autoPilot ? "✓ 鸡煲代打中" : "鸡煲代打", ToggleAutoPilot);
        _nextBtn = MakeActionButton("下一手", NextHand);
        _repickBtn = MakeActionButton("换一批角色", Repick);
        foreach (var b in new[] { _foldBtn, _checkBtn, _callBtn, _raiseBtn, _allinBtn, _autoBtn, _nextBtn, _repickBtn })
            _actions.AddChild(b);
        // 宽度 900 → 1020：多了一个「鸡煲代打」按钮，最宽的一帧（我的回合 5 个动作全亮 + 代打）会顶出去。
        _scene.AddChild(DouSpireUi.Place(_actions, 0.5f, ActionAy, DouSpireUi.Px(1020), DouSpireUi.Px(60)));

        // ⑬ 加注预设（½池 / ¾池 / 满池 / 全下 / 取消）—— 比滑条好点也好做
        _raiseRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, Visible = false };
        _raiseRow.AddThemeConstantOverride("separation", DouSpireUi.Gap(10));
        _rHalf = MakeActionButton("½ 池", () => MyMove(RaiseKind(), _game.PotSizedRaiseTo(MeSeat, 0.5)));
        _rThreeQ = MakeActionButton("¾ 池", () => MyMove(RaiseKind(), _game.PotSizedRaiseTo(MeSeat, 0.75)));
        _rPot = MakeActionButton("满池", () => MyMove(RaiseKind(), _game.PotSizedRaiseTo(MeSeat, 1.0)));
        _rAllin = MakeActionButton("全下", () => MyMove(PokerAction.AllIn, 0));
        _rCancel = MakeActionButton("取消", HideRaiseRow);
        foreach (var b in new[] { _rHalf, _rThreeQ, _rPot, _rAllin, _rCancel })
            _raiseRow.AddChild(b);
        _scene.AddChild(DouSpireUi.Place(_raiseRow, 0.5f, HintAy, DouSpireUi.Px(760), DouSpireUi.Px(56)));

        // ⑭ 牌局日志（右下角，大白话）
        _logLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        DouSpireUi.StyleFont(_logLabel);
        _logLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.84f, 0.88f, 0.92f));
        _logLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(15));
        var logBg = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        // ★ 2026-10-10 接素材：`poker/plate_wood.png`（金边木牌匾）以前一直闲置 ——
        //   本地道本来就是"垫在一块木牌上"的观感。取不到图就退回原来的深色半透明块。
        //   ★ 2026-10-11 改画法 + 加宽：这张图 1024×198（比例 5.17）塞进 430×180（比例 2.39）时，
        //     两端那对**卷草纹装饰**（各 114 px）被 Godot 的九宫格按 1:1 画出来，
        //     占了整块牌子的一大半、中间木纹竖着拉 —— 现在自己切三块（帽子按高度等比缩）。
        //     同时把面板从 430 加宽到 560：帽子占掉的比例小了，中间才有地方放日志文字。
        if (DouSpireAssets.First(DouSpireAssets.PokerPlateWood) is { } woodTex)
        {
            logBg.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            logBg.AddChild(DouSpireUi.Plate(woodTex, 114, 114, DouSpireUi.Px(560), DouSpireUi.Px(180)));
        }
        else
        {
            logBg.AddThemeStyleboxOverride("panel",
                DouSpireUi.Box(new Color(0.06f, 0.07f, 0.09f, 0.62f), 10, new Color(1f, 1f, 1f, 0.10f), 1));
        }
        //   木牌匾本身的木色偏亮，纯灰字压上去会飘 —— 压一层暗底把字托住（牌匾在下面当前景）。
        var logShade = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        logShade.AddThemeStyleboxOverride("panel",
            DouSpireUi.Box(new Color(0.05f, 0.045f, 0.035f, 0.55f), 8));
        logShade.AddChild(DouSpireUi.Place(_logLabel, 0.5f, 0.5f, DouSpireUi.Px(440), DouSpireUi.Px(168)));
        logBg.AddChild(DouSpireUi.Fill(logShade));
        _scene.AddChild(DouSpireUi.Place(logBg, LogAx, LogAy, DouSpireUi.Px(560), DouSpireUi.Px(180)));

        // ⑮ 右上角：收桌 / 牌面 / 牌型速查（**室内 / 桌面那两颗已经挪到台面上了**，
        //     见下面 ⑮.5 —— 这段注释原来还把它们列在这一排里，是 2026-10-09 挪走之后没删的旧话，
        //     2026-10-11 改过来了：照旧注释算 ox 会算出四个重叠的按钮）
        //
        // ★ 摆法（**2026-10-09 修**）：锚点都在右边（1.0），ox 必须写成
        //   `-(右边距 + 宽度/2)`，一个挨一个往左排 —— 斗地主那张桌就是这么算的。
        //   ⚠️ 德州这里原来「收桌」和「牌面」的右边距**都写成了 -17** → 两个按钮**完全重叠**，
        //      「收桌」被压在下面点不到（玩家只能靠 F9 收桌，所以一直没人报）。
        //   ⚠️ 加按钮时照这个式子往下算就行：
        //      下一个 ox = -(上一个的右边距 + 上一个的宽度 + 8 + 自己的宽度/2)。
        //      当前排（从右往左）：收桌 17/110 → 牌面 135/220 → 牌型速查 363/180。
        _menuBtn = MakeActionButton("收桌", () => Close(Entry.Logger));
        _menuBtn.CustomMinimumSize = new Vector2(DouSpireUi.Px(110), DouSpireUi.Px(50));
        _scene.AddChild(DouSpireUi.Place(_menuBtn, 1.0f, 0.0f, DouSpireUi.Px(110), DouSpireUi.Px(50),
            -DouSpireUi.Px(72), DouSpireUi.Px(46)));

        _faceBtn = MakeActionButton("", ToggleSkin);
        _faceBtn.CustomMinimumSize = new Vector2(DouSpireUi.Px(220), DouSpireUi.Px(50));
        _scene.AddChild(DouSpireUi.Place(_faceBtn, 1.0f, 0.0f, DouSpireUi.Px(220), DouSpireUi.Px(50),
            -DouSpireUi.Px(245), DouSpireUi.Px(46)));

        _helpBtn = MakeActionButton("牌型速查", ToggleHelp);
        _helpBtn.CustomMinimumSize = new Vector2(DouSpireUi.Px(180), DouSpireUi.Px(50));
        _scene.AddChild(DouSpireUi.Place(_helpBtn, 1.0f, 0.0f, DouSpireUi.Px(180), DouSpireUi.Px(50),
            -DouSpireUi.Px(453), DouSpireUi.Px(46)));

        // ⑮.5 ★★ 两个**小圆钮**：室内 / 桌面（用户 2026-10-09）
        //
        //   用户原话（第一次）：「**通过小按钮可以桌面和室内**」+「**加两个不一样的，切换不同的东西**」
        //   ⇒ 一个切**室内**（= 牌室场景，`DouSpireConfig.PokerScene`，4 档），
        //      一个切**桌面**（= 牌桌台面，`DouSpireConfig.PokerTableStyle`，7 档）。
        //
        //   ★★ 用户原话（第二次，把上面那版**文字小条**否掉了）：
        //      「**不是这个按钮，我们画面挺小的。没有空间放这些，做两个小圆圈型的就行了，
        //        还可以镶嵌在桌子上，做成互动，画风方面与每个桌子配合都不会奇怪就行了**」
        //      ⇒ ① 形状：文字小条 → **圆钮**；② 位置：右上角 → **牌桌上**（左右对称）；
        //        ③ 排布：右上角那排（收桌 / 牌面 / 牌型速查）**不动**，
        //           被挤走的只有这两个（原来占掉 929px 里的 156px）。
        //
        //   ★ 摆位是怎么算出来的（1920×1080 基准，把已经占了地方的东西全列一遍）：
        //       公共牌  x 671~1249 / y 466~614
        //       底池    x 660~1260 / y 356~404
        //       状态行  x 510~1410 / y 641~681
        //       牌局日志 x 1470~1900 / y 653~833
        //       我的立绘 x 100~340 / y 822+，我的 HUD x 50~390 / y 686~828
        //       左席立绘 x 100~340 / y 336~636，右席立绘 x 1580~1820 / y 336~636
        //       桌子图   x 170~1750 / y 63~909（1580×846 铺在 0.5 / 0.450）
        //     圆钮直径 86 ⇒ 左 415~501、右 1419~1505，y 都在 0.505×1080=545 上下：
        //     **一个都不碰**，左右对称，而且正落在桌面上（不是浮在屏幕角上）。
        //     ⚠️ 想挪位置就改下面两行 `Place(...)` 的 ax / ay；改一个记得改另一个（对称才好看）。
        //   ★ 为什么圆钮的配色**不吃任何一套桌面配色**：见 <see cref="MakeRoundKnob"/> 上面的注释。
        _sceneBtn = MakeRoundKnob(DouSpireKnobIcon.Kind.Room, CycleScene, out _sceneCap);
        _scene.AddChild(DouSpireUi.Place(_sceneBtn, 0.238f, 0.505f,
            DouSpireUi.Px(KnobSize), DouSpireUi.Px(KnobSize)));

        _tableBtn = MakeRoundKnob(DouSpireKnobIcon.Kind.Desk, CycleTable, out _tableCap);
        _scene.AddChild(DouSpireUi.Place(_tableBtn, 0.762f, 0.505f,
            DouSpireUi.Px(KnobSize), DouSpireUi.Px(KnobSize)));

        // 先建结算面板、后建帮助面板 —— 后加的画在上面，
        // 这样"牌型速查"（玩家主动点开的）永远压得住结算面板。
        BuildResultPanel();
        BuildHelpPanel();
        UpdateFaceBtn();
        UpdateSceneBtn();
        UpdateTableBtn();
    }

    /// <summary>三家：立绘（优先活角色，失败退静态图）+ 一个信息框（名字 / 筹码 / 这一轮下了多少 / 身份）。</summary>
    private void BuildSeats()
    {
        var parentW = Size.X > 8f ? Size.X : 1920f;
        var parentH = Size.Y > 8f ? Size.Y : 1080f;

        for (var s = 0; s < SeatCount; s++)
        {
            var lay = DouSpireLayout.PokerSeats[s];

            // —— 座位立绘（客人=待机动画，本体角色=活模型；活角色请不上来时它顶着）——
            // ⚠️ 座位立绘不是"候选链"而是**已经取好的 Texture2D**（DouSpireCharacters.SeatArt），
            //    所以不能走 DouSpireUi.Sprite（那个吃 string[]），得自己搭 TextureRect。
            // ★ 2026-10-11：**节点无条件建出来**，图可能先给 null ——
            //   `seats/char_*.png` 删掉之后本体角色这一席取不到图，但节点得在，
            //   `_seatArt[s]` 才不是 null（`Repick()` 靠它认节点、待机动画也靠它贴帧）。
            var art = new TextureRect
            {
                Texture = DouSpireCharacters.SeatArt(_chars[s]),
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            DouSpireUi.Place(art, lay.ArtX, lay.ArtY, DouSpireUi.Px(lay.ArtW), DouSpireUi.Px(lay.ArtH));
            _scene.AddChild(art);
            _seatArt[s] = art;

            // —— 活角色（游戏本体带骨骼的那个）——
            if (DouSpireConfig.LiveActors && _chars[s].Model is not null)
            {
                try
                {
                    var footX = lay.ArtX * parentW;
                    var footY = lay.ArtY * parentH
                                + DouSpireUi.Px(lay.ArtH) * 0.5f
                                + DouSpireConfig.ActorYOffset * DouSpireUi.Screen;

                    // targetH 只给"想要多高"，ActorScale 由 Spawn 内部的 FitHeight 自己乘 —— 别在这里再乘一次。
                    _actors[s] = DouSpireActor.Spawn(
                        _scene, _chars[s].Model!, new Vector2(footX, footY),
                        DouSpireUi.Px(lay.ArtH),
                        DouSpireConfig.ActorFaceCenter && lay.ArtX > 0.5f,
                        StakeCreature());
                    if (_actors[s] is not null && _seatArt[s] is not null) _seatArt[s]!.Visible = false;
                }
                catch (System.Exception e)
                {
                    Entry.Logger.Warn($"德州：第 {s} 席的活角色没请上来，用静态立绘顶着: {e.Message}");
                    _actors[s] = null;
                }
            }

            // —— 信息框 ——
            var hud = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            hud.AddThemeStyleboxOverride("panel",
                DouSpireUi.Box(new Color(0.05f, 0.06f, 0.08f, 0.72f), 12, DouSpireUi.Gold, 2));

            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", DouSpireUi.Gap(2));
            hud.AddChild(DouSpireUi.Place(box, 0.5f, 0.5f, DouSpireUi.Px(300), DouSpireUi.Px(112)));

            _hudName[s] = MakeHudLabel(box, DouSpireUi.Scaled(19), DouSpireUi.Gold);
            _hudStack[s] = MakeHudLabel(box, DouSpireUi.Scaled(17), new Color(0.96f, 0.93f, 0.84f));
            _hudBet[s] = MakeHudLabel(box, DouSpireUi.Scaled(16), new Color(0.72f, 0.86f, 0.72f));
            _hudTag[s] = MakeHudLabel(box, DouSpireUi.Scaled(15), new Color(0.98f, 0.72f, 0.45f));

            _scene.AddChild(DouSpireUi.Place(hud, lay.HudX, lay.HudY,
                DouSpireUi.Px(DouSpireLayout.HudW), DouSpireUi.Px(DouSpireLayout.HudH)));
            _hud[s] = hud;

            // —— 庄家按钮（D 字圆牌）：挂在信息框右上角**内侧** ——
            //   ★ 2026-10-09 接素材：`poker/dealer_button.png` 一直没被引用（用户问过）。
            //   信息框 340×142 → 半宽 170 / 半高 71；图标 46 见方（半宽 23）。
            //   横向偏移取 170-30 = 140 → 右边缘 163 < 170，正好留在框里。
            var dealer = DouSpireUi.Sprite(DouSpireAssets.PokerDealer, lay.HudX, lay.HudY,
                DouSpireUi.Px(46), DouSpireUi.Px(46),
                DouSpireUi.Px(DouSpireLayout.HudW / 2f - 30f),
                -DouSpireUi.Px(DouSpireLayout.HudH / 2f - 26f));
            if (dealer is not null)
            {
                dealer.Visible = false;      // 只在庄家那一席露出来（见 RefreshSeats）
                _scene.AddChild(dealer);
            }
            _hudDealer[s] = dealer;

            // —— 盲注标记（`poker/blind_marker.png`，暗红圆牌）——
            //   ★ 2026-10-10 接素材：挂在信息框**左上角内侧**（庄家钮在右上角，一左一右不打架）。
            //     只有翻牌前的小盲/大盲那一席露出来（见 RefreshSeats）。
            var blind = DouSpireUi.Sprite(DouSpireAssets.PokerBlind, lay.HudX, lay.HudY,
                DouSpireUi.Px(40), DouSpireUi.Px(40),
                -DouSpireUi.Px(DouSpireLayout.HudW / 2f - 26f),
                -DouSpireUi.Px(DouSpireLayout.HudH / 2f - 24f));
            if (blind is not null)
            {
                blind.Visible = false;
                _scene.AddChild(blind);
            }
            _hudBlind[s] = blind;

            // —— 各席**面前**的一小摞筹码（★ 2026-10-10 新增）——
            //   用户："筹码应该是各个角色面前都放两三个。"
            //   位置：从该席立绘朝**桌心**的方向走 38% —— 落在"面前"而不是"脚下"。
            //   做法：VBoxContainer + **负间距**把 3 枚 `chip_single.png` 叠成一摞
            //   （比直接放一张 chip_stack.png 更贴合"两三个"的说法，也更有实物感）。
            //   显隐交给 RefreshSeats：本轮没下注就藏着。
            var chips = new VBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.Begin,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
            };
            chips.AddThemeConstantOverride("separation", DouSpireUi.Gap(-20));
            var chipTex = DouSpireAssets.First(DouSpireAssets.PokerChipSingle);
            if (chipTex is not null)
            {
                for (var i = 0; i < 3; i++)
                {
                    chips.AddChild(new TextureRect
                    {
                        Texture = chipTex,
                        CustomMinimumSize = new Vector2(DouSpireUi.Px(38), DouSpireUi.Px(38)),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                        MouseFilter = MouseFilterEnum.Ignore,
                    });
                }

                var chipX = lay.ArtX + (0.5f - lay.ArtX) * 0.38f;
                var chipY = lay.ArtY + (0.5f - lay.ArtY) * 0.38f;
                _scene.AddChild(DouSpireUi.Place(chips, chipX, chipY,
                    DouSpireUi.Px(44), DouSpireUi.Px(78)));
            }
            _hudChips[s] = chips;

            // —— 摊牌时才露出来的那两张底牌（挂在信息框正下方，平时藏着）——
            // 只有别人需要这一排；我自己那两张一直在屏幕底下摆着。
            if (s != MeSeat)
            {
                var hole = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, Visible = false };
                hole.AddThemeConstantOverride("separation", DouSpireUi.Gap(6));
                // 信息框高 142（1920 基准像素）→ 换算成屏幕比例是 142/1080；
                // 往下挪"半个信息框 + 半张牌 + 一点缝"。
                const float holeCardW = 56f;
                var holeH = holeCardW * 1.40f;
                var dy = DouSpireLayout.HudH / 1080f * 0.5f + holeH / 1080f * 0.5f + 0.014f;
                _scene.AddChild(DouSpireUi.Place(hole, lay.HudX, lay.HudY + dy,
                    DouSpireUi.Px(holeCardW * 2 + 24), DouSpireUi.Px(holeH)));
                _hudHole[s] = hole;
            }
        }

        // 角色是后加进 _scene 的，会把先建好的信息框压到下面 —— 请完角色顺手把信息框提到最前面。
        foreach (var h in _hud)
            if (h is not null && GodotObject.IsInstanceValid(h)) h.MoveToFront();
        // ★ 庄家按钮也得提上来 —— 不然刚被 MoveToFront 的信息框会把它压住。
        foreach (var d in _hudDealer)
            if (d is not null && GodotObject.IsInstanceValid(d)) d.MoveToFront();
        // ★ 盲注标记同理。
        foreach (var b in _hudBlind)
            if (b is not null && GodotObject.IsInstanceValid(b)) b.MoveToFront();
    }

    private static Label MakeHudLabel(Node parent, int size, Color color)
    {
        var l = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        DouSpireUi.StyleFont(l);
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeFontSizeOverride("font_size", size);
        parent.AddChild(l);
        return l;
    }

    // ════════════════════════════════════════════════════════
    //  重绘
    // ════════════════════════════════════════════════════════

    private void Refresh()
    {
        if (_broken) return;
        // 联机桌：主机还没发这一手（_game 还没建）→ 没什么可画的，等 StartHand 来了再刷。
        if (_game is null) return;
        RefreshSeats();
        RefreshBoard();
        RefreshHand();
        RefreshButtons();
        RefreshText();
    }

    private void RefreshSeats()
    {
        // ★ s = **视角位**（画面上第几席，0 永远是"我"），g = **全局位**（内核里的座位号）。
        //   `_hud/_chars/_actors/_hudHole` 按视角位存；`_game.*`（Players/Button/CurrentSeat…）
        //   一律要全局位。单人时两者相同；联机时我可能坐 1/2/3 号 —— 混用就会把别人的
        //   筹码/身份画到我这一席上（P4 联机时才发现的老假设）。
        for (var s = 0; s < SeatCount; s++)
        {
            var g = G(s);
            var p = _game.Players[g];
            var tag = g == _game.Button ? "庄家" : "";
            var sbSeat = _game.Players.Count == 2 ? _game.Button : (_game.Button + 1) % _game.Players.Count;
            var bbSeat = _game.Players.Count == 2 ? (_game.Button + 1) % _game.Players.Count : (_game.Button + 2) % _game.Players.Count;
            if (_game.Street == Street.Preflop)
            {
                if (g == sbSeat) tag = string.IsNullOrEmpty(tag) ? "小盲" : tag + " · 小盲";
                if (g == bbSeat) tag = string.IsNullOrEmpty(tag) ? "大盲" : tag + " · 大盲";
            }
            if (p.Folded) tag = "已弃牌";
            else if (p.AllIn) tag = "全下";

            if (_hudName[s] is not null)
            {
                // 我这一席把角色名也带上（"我 · 铁甲战士"）—— 赌注模式下这一席就是我 run 里的角色。
                var nm = p.Name;
                if (g == MeSeat)
                {
                    var cn = DouSpireCharacters.Name(_chars[s]);
                    if (!string.IsNullOrWhiteSpace(cn)) nm = $"我 · {cn}";
                }
                _hudName[s]!.Text = nm;
            }
            if (_hudStack[s] is not null) _hudStack[s]!.Text = $"筹码 {p.Stack}";
            if (_hudBet[s] is not null)
                _hudBet[s]!.Text = p.CommittedStreet > 0 ? $"这一轮已下 {p.CommittedStreet}" : "";
            if (_hudTag[s] is not null) _hudTag[s]!.Text = tag;

            // 庄家按钮：坐在庄家位的那一席挂一枚（素材 poker/dealer_button.png）
            if (_hudDealer[s] is { } dealerIcon) dealerIcon.Visible = g == _game.Button;

            // 盲注标记：翻牌前，小盲/大盲那一席挂一枚暗红圆牌（素材 poker/blind_marker.png）。
            // ★ 这副牌 18:51 就入库了，代码里一直没接 —— 现在补上（小盲/大盲以前只有一行小字）。
            if (_hudBlind[s] is { } blindIcon)
                blindIcon.Visible = !_game.HandOver
                    && _game.Street == Street.Preflop
                    && !p.Folded
                    && (g == sbSeat || g == bbSeat);

            // 面前的筹码：本轮**下过注**才露出来（= 信息框里"这一轮已下 N"那行字的实物版）
            if (_hudChips[s] is { } chipPile)
                chipPile.Visible = p.CommittedStreet > 0;

            // 当前该谁说话 → 信息框高亮
            var active = !_game.HandOver && _game.CurrentSeat == g;
            if (_hud[s] is not null)
                _hud[s]!.AddThemeStyleboxOverride("panel", DouSpireUi.Box(
                    active ? new Color(0.16f, 0.13f, 0.06f, 0.85f) : new Color(0.05f, 0.06f, 0.08f, 0.72f),
                    12, active ? DouSpireUi.Gold : new Color(0.5f, 0.45f, 0.35f, 0.6f), active ? 3 : 2));

            if (_actors[s] is { } act && act.Alive)
                act.SetMirror(DouSpireConfig.ActorFaceCenter && DouSpireLayout.PokerSeats[s].ArtX > 0.5f);

            // 摊牌：把没弃牌的人的两张底牌翻出来（这一刻才公开）。
            if (_hudHole[s] is { } hole)
            {
                var show = _revealAll && g != MeSeat && !p.Folded;
                hole.Visible = show;
                if (show)
                {
                    ClearChildren(hole);
                    foreach (var c in p.Hole)
                        hole.AddChild(MakeCardFace(c, 56f, _bestFive.Contains(c)));
                }
            }
        }
    }

    private void RefreshBoard()
    {
        // ★★ 2026-10-10 **事故第七条（德州版）**：`_game` 为 null 时**一个字段都不许碰**。
        //
        //   德州桌的 `Refresh()` 有 `if (_game is null) return;` 守卫，但
        //   `CycleTable()`（点右上角那个「桌面」钮换台面）**绕过了 Refresh、直接调本函数**
        //   ⇒ 开桌后还没开局（以及联机等主机广播期间）点一次「桌面」= 一个 NRE。
        //   昨晚朋友的日志里连点 6 种台面，就是 6 次 `RefreshBoard ← CycleTable` 的
        //   `NullReferenceException`。换「室内」不炸（那条路不碰 `_game`），换「桌面」必炸 ——
        //   日志里这个区别正好对得上。
        //
        //   现在：没开局就照旧摆 5 个空牌位（牌宽还得跟着台面刻线走），底池写「—」。
        if (_game is null)
        {
            ClearChildren(_boardRow);
            for (var i = 0; i < 5; i++) _boardRow.AddChild(MakeCardSlot(_markCardW));
            _potLabel.Text = "底池 —";
            return;
        }

        ClearChildren(_boardRow);

        // 5 个位置：还没发的画空位
        for (var i = 0; i < 5; i++)
        {
            if (i < _game.Board.Count)
                _boardRow.AddChild(MakeCardFace(_game.Board[i], _markCardW, _bestFive.Contains(_game.Board[i])));
            else
                _boardRow.AddChild(MakeCardSlot(_markCardW));
        }

        _potLabel.Text = _game.FinalPot > 0 && _game.HandOver
            ? $"底池 {_game.FinalPot}"
            : $"底池 {_game.Pot}";
    }

    private void RefreshHand()
    {
        ClearChildren(_handRow);
        var hole = _game.Players[MeSeat].Hole;
        for (var i = 0; i < 2; i++)
            _handRow.AddChild(MakeCardFace(hole[i], MyCardW, _bestFive.Contains(hole[i])));
    }

    private void RefreshText()
    {
        _titleLabel.Text = $"德州扑克尖塔 · 第 {_handNo} 手 · {HoldemGame.StreetName(_game.Street)}"
                           + $" · 我 {(_myTotal >= 0 ? "+" : "")}{_myTotal}";

        _statusLabel.Text = StatusText();
        _hintLabel.Text = HintText();
        _hintLabel.Visible = !_raiseRow.Visible;
        RefreshHintIcon();
        _logLabel.Text = LogText();
    }

    /// <summary>
    /// 把"你现在是什么牌"左边那枚图标换成当前牌力的档位（`poker/rank_1`~`rank_10`）。
    ///
    /// <para>档位映射见 <see cref="RankIconIndex"/>；牌没成型的阶段（翻牌前 2 张、或已经弃牌）
    /// 就**不摆图标**（留着反而是误导）。图取不到时整枚不显示。</para>
    /// </summary>
    private void RefreshHintIcon()
    {
        if (_hintIcon is not { } icon || !GodotObject.IsInstanceValid(icon)) return;

        var me = _game.Players[MeSeat];
        if (me.Folded || _game.HandOver)
        {
            icon.Visible = false;
            return;
        }

        // 我手上的 + 公共牌，至少 5 张才评得出牌型（翻牌前是 2 张 → 不摆图标）。
        var known = new List<PokerCard>(7);
        known.AddRange(me.Hole);
        known.AddRange(_game.Board);
        if (known.Count < 5)
        {
            icon.Visible = false;
            return;
        }

        icon.Visible = !_raiseRow.Visible;
        icon.Texture = DouSpireAssets.First(DouSpireAssets.PokerRankIcon(RankIconIndex(HandEval.Best(known, out _))));
    }

    /// <summary>
    /// 把 <see cref="HandEval"/> 的牌型档位翻成 `rank_N` 图标的序号（1~10）。
    ///
    /// <para>`HandEval` 只有 **9 档**（高牌…同花顺），图却有 **10 张** —— 第 10 张是
    /// "皇家同花顺"（A 高同花顺），比法上和第 9 档一样，但观赏性值得单列。</para>
    /// </summary>
    private static int RankIconIndex(HandValue v) => v.Category switch
    {
        HandCategory.HighCard => 1,
        HandCategory.Pair => 2,
        HandCategory.TwoPair => 3,
        HandCategory.Trips => 4,
        HandCategory.Straight => 5,
        HandCategory.Flush => 6,
        HandCategory.FullHouse => 7,
        HandCategory.Quads => 8,
        // 同花顺：A 高 = 皇家同花顺（第 10 张），其余 = 第 9 张。
        HandCategory.StraightFlush => v.K1 == 14 ? 10 : 9,
        _ => 1,
    };

    private string StatusText()
    {
        if (_game.HandOver) return _game.ResultText;

        // 客机：动作已发给主机，正等主机广播回来（这一步没确认前按钮都收起来）。
        if (_waitingHost) return "已发出 —— 等主机确认…";

        var me = _game.Players[_game.CurrentSeat];
        var toCall = _game.CallCost(_game.CurrentSeat);

        if (_game.CurrentSeat == MeSeat)
            return toCall > 0 ? $"轮到你 —— 要跟 {toCall} 才跟得上" : "轮到你 —— 可以过牌，也可以下注";

        var what = toCall > 0 ? $"面对 {_game.CurrentBet}（要补 {toCall}）" : "没人下注";
        return $"{me.Name} 在想…（{what}）";
    }

    /// <summary>
    /// 「你现在是：一对 A」+ 一句建议。**新手友好第 1 条**，也是最值钱的一条 ——
    /// 新手不用背牌型表，看这一行就知道自己手里是什么。
    /// 公共牌不满 5 张时，牌力还是"当前最好的 5 张"，会跟着翻牌变强。
    /// </summary>
    private string HintText()
    {
        try
        {
            var hole = _game.Players[MeSeat].Hole;
            if (hole.Length < 2 || hole[0].Rank == 0) return "";

            var known = new List<PokerCard>(hole);
            known.AddRange(_game.Board);

            string what;
            if (known.Count < 5)
            {
                // 还凑不满 5 张 —— 报"你现在手里是什么"，比如"一对 A" / "两张黑桃"
                what = PreflopName(hole);
            }
            else
            {
                what = HandEval.Name(HandEval.Best(known, out _));
            }

            var advice = DouSpireConfig.PokerOddsHint && !_game.HandOver ? Advice() : "";
            return advice.Length == 0 ? $"你现在是：{what}" : $"你现在是：{what}　　{advice}";
        }
        catch (System.Exception)
        {
            return "";
        }
    }

    /// <summary>翻牌前（只有 2 张底牌）怎么用中文说。</summary>
    private static string PreflopName(IReadOnlyList<PokerCard> hole)
    {
        var a = hole[0];
        var b = hole[1];
        var ra = Mathf.Max(a.Rank, b.Rank);
        var rb = Mathf.Min(a.Rank, b.Rank);
        var na = PokerCard.RankName(ra);
        var nb = PokerCard.RankName(rb);

        if (ra == rb) return $"{na} 一对（口袋对）";
        var suited = a.Suit == b.Suit ? "同花" : "不同花";
        return $"{na} {nb} {suited}";
    }

    /// <summary>
    /// 「该不该跟」的建议 —— 新手友好第 6 条。
    /// 用 <see cref="PokerAi.Equity"/> 算胜率（**不看别人底牌**），再和底池赔率比一下。
    /// 结论只有三档：建议跟 / 看情况 / 建议弃。
    /// </summary>
    private string Advice()
    {
        if (_game.CurrentSeat != MeSeat) return "";

        var toCall = _game.CallCost(MeSeat);
        var legal = _game.LegalActions();

        // 不用花钱（能过牌）：值不值得主动下注
        if (toCall <= 0)
        {
            var eq0 = PokerAi.Equity(_game, MeSeat, 120, _rng);
            if (eq0 >= 0.62) return $"（胜率约 {eq0:P0} —— 可以下注）";
            if (eq0 >= 0.45) return $"（胜率约 {eq0:P0} —— 过牌看看）";
            return $"（胜率约 {eq0:P0} —— 过牌就行）";
        }

        var eq = PokerAi.Equity(_game, MeSeat, 160, _rng);
        var potOdds = (double)toCall / (_game.Pot + toCall);

        if (eq >= potOdds + 0.10) return $"（胜率约 {eq:P0}，要花 {toCall} —— 建议跟）";
        if (eq >= potOdds + 0.01) return $"（胜率约 {eq:P0}，要花 {toCall} —— 勉强能跟）";
        if (legal.Contains(PokerAction.Check)) return $"（胜率约 {eq:P0} —— 建议过牌）";
        return $"（胜率约 {eq:P0}，要花 {toCall} —— 建议弃牌）";
    }

    private string LogText()
    {
        if (!DouSpireConfig.ShowLog) return "";
        var log = _game.Log;
        var take = Mathf.Min(8, log.Count);
        return string.Join("\n", log.Skip(log.Count - take));
    }

    // ════════════════════════════════════════════════════════
    //  按钮
    // ════════════════════════════════════════════════════════

    private void RefreshButtons()
    {
        // 联机桌：主机还没发这一手（_game 还没建）→ 操作按钮全收起来，只留上面那句"等主机发牌"。
        if (_game is null)
        {
            _nextBtn.Visible = false;
            _repickBtn.Visible = false;
            _foldBtn.Visible = false;
            _checkBtn.Visible = false;
            _callBtn.Visible = false;
            _raiseBtn.Visible = false;
            _allinBtn.Visible = false;
            _autoBtn.Visible = false;
            HideRaiseRow();
            return;
        }

        var over = _game.HandOver;
        // 客机把动作发出去、还没等到主机回声（_waitingHost）之前，不给他再点一遍。
        var myTurn = !over && _game.CurrentSeat == MeSeat && !_waitingHost;
        var legal = myTurn ? _game.LegalActions() : new List<PokerAction>();

        // 「鸡煲代打」**整手都挂着**（不是我的回合也露着）—— 可以提前开着托管下一手。
        UpdateAutoBtn();
        _autoBtn.Visible = true;
        _nextBtn.Visible = over;
        // 「换一批角色」是本地重开（自己换种子）—— 联机时不能点（会跟别人分叉），联机桌里藏起来。
        _repickBtn.Visible = over && !DouSpirePokerNet.Active;
        _foldBtn.Visible = myTurn && legal.Contains(PokerAction.Fold);
        _checkBtn.Visible = myTurn && legal.Contains(PokerAction.Check);
        _callBtn.Visible = myTurn && legal.Contains(PokerAction.Call);
        _raiseBtn.Visible = myTurn && (legal.Contains(PokerAction.Raise) || legal.Contains(PokerAction.Bet));
        _allinBtn.Visible = myTurn && legal.Contains(PokerAction.AllIn);

        // 按钮上直接写"要掏多少钱" —— 新手友好第 2 条
        if (_callBtn.Visible)
        {
            var cost = _game.CallCost(MeSeat);
            var me = _game.Players[MeSeat];
            _callBtn.Text = cost >= me.Stack ? $"跟注 {cost}（全下）" : $"跟注 {cost}";
        }
        if (_raiseBtn.Visible)
        {
            var min = _game.MinRaiseToFor(MeSeat);
            _raiseBtn.Text = _game.CurrentBet > 0 ? $"加注（至少到 {min}）" : $"下注（至少 {min}）";
        }
        if (_allinBtn.Visible)
            _allinBtn.Text = $"全下 {_game.Players[MeSeat].Stack}";

        // 过牌/跟注/加注都不可用（理论上不会），至少留一个弃牌，别让玩家卡死
        if (myTurn && !_foldBtn.Visible && !_checkBtn.Visible && !_callBtn.Visible
            && !_raiseBtn.Visible && !_allinBtn.Visible)
            _foldBtn.Visible = true;

        if (!myTurn) HideRaiseRow();

        // 加注预设的金额标签（跟池子走，池子一变就跟着变）
        if (_raiseRow.Visible && myTurn)
        {
            _rHalf.Text = $"½ 池（{_game.PotSizedRaiseTo(MeSeat, 0.5)}）";
            _rThreeQ.Text = $"¾ 池（{_game.PotSizedRaiseTo(MeSeat, 0.75)}）";
            _rPot.Text = $"满池（{_game.PotSizedRaiseTo(MeSeat, 1.0)}）";
        }
    }

    /// <summary>本轮该用"下注"还是"加注" —— 看本轮有没有人下过注（和内核同一套判断）。</summary>
    private PokerAction RaiseKind() => _game.CurrentBet > 0 ? PokerAction.Raise : PokerAction.Bet;

    private void ShowRaiseRow()
    {
        _raiseRow.Visible = true;
        _hintLabel.Visible = false;
        RefreshButtons();
    }

    private void HideRaiseRow()
    {
        if (!_raiseRow.Visible) return;
        _raiseRow.Visible = false;
        _hintLabel.Visible = true;
    }

    /// <summary>
    /// 铺**当前风格**的那张牌桌（<see cref="DouSpireConfig.PokerTableStyle"/>）。
    ///
    /// 单独抽成一个方法，是为了**牌桌上那个「牌桌：…」按钮能当场换** ——
    /// 不抽的话换张桌得收桌重开，试七种风格要开关七次。
    ///
    /// 图不存在就按 `目标风格 → 经典绿绒德州桌 → 斗地主圆桌` 顺序回落——
    /// 不会白屏，也不会因少画一张就突然出现斗地主的刻痕。
    /// </summary>
    private void ApplyTable()
    {
        // 先撤掉旧的（换风格 / 重复调用都要安全）
        if (_tableLayer != null && GodotObject.IsInstanceValid(_tableLayer))
        {
            if (_tableLayer.GetParent() == _scene) _scene.RemoveChild(_tableLayer);
            _tableLayer.QueueFree();
        }
        _tableLayer = null;
        if (_scene is null) return;

        // ★★ 2026-10-10 **尺寸按图自己的比例算**（以前写死 1580×846）。
        //   1580×846 的比值是 **1.8675**，只有 `table_poker.png`（1654×886 = 1.867）正好是这个比；
        //   另外 4 张台面（anime / brass / frost / obsidian）都是 **≈1.78**。
        //   写死尺寸不会把图拉变形（`KeepAspectCentered` 会自己留边），但**换风格后桌子会小一圈**
        //   —— 用户看到的就是"换张台面，桌面缩进去、两边露底"。
        //   现在：**高度钉死 846**（保证台面刻线的纵向位置不动，见 ApplyMarks 那张实测表），
        //   宽度 = 846 × 图自己的宽高比。这样 5 张台面**都是同一个高度**，宽度各按各的。
        var chain = DouSpireAssets.PokerTableOf(DouSpireConfig.PokerTableStyle)
                        .Concat(DouSpireAssets.Table).ToArray();
        var tex = DouSpireAssets.First(chain);
        const float tableH = 846f;
        var tableW = tex is not null && tex.GetHeight() > 0
            ? tableH * tex.GetWidth() / tex.GetHeight()
            : 1580f;

        _tableLayer = DouSpireUi.Sprite(chain, 0.5f, 0.450f, DouSpireUi.Px(tableW), DouSpireUi.Px(tableH));
        if (_tableLayer is null) return;

        _scene.AddChild(_tableLayer);
        // 底座那几层重新叠一遍（背景 → 桌面 → …）。★ 见 RestackBase 的注释：
        //   这里以前写死 `MoveChild(_tableLayer, 2)`，而"换牌桌"是删了重加、
        //   新节点落在末尾 —— 写死的索引会把整屏兜底色一路顶上最前面，于是"切桌子黑屏"。
        RestackBase();
    }

    /// <summary>
    /// 把「兜底纯色 → 背景 → 桌面 → 公共牌牌匾 → 赢家金环」这几层按**固定顺序**重新叠一遍，
    /// 底座永远在最下面、UI 永远在上面。
    ///
    /// ★★ 2026-10-11 修黑屏 ★★
    ///   这三处原来是**写死的绝对索引**：<c>MoveChild(bg, 1)</c> / <c>MoveChild(table, 2)</c> /
    ///   <c>MoveChild(bp, 0)</c>。可是「换牌桌 / 换室内」都是**先删旧的、再加新的**（新节点永远加在末尾），
    ///   删掉一个节点之后，它后面所有兄弟的索引都会往前挪一格 —— 写死的索引当场指错。
    ///
    ///   实测连点「换牌桌 → 换室内 → 换牌桌」三步，那块**整屏不透明的兜底纯色**
    ///   就被顶到了所有东西上面：背景和桌子全没了，只剩底池/卡牌/按钮浮在一片底色上
    ///   （= 用户报的"切桌子切室内会黑屏"，截图 20261011192357_1.jpg 实锤）。
    ///
    ///   改成**相对叠放**之后，中间删过谁、加过谁都不影响顺序。
    /// </summary>
    private void RestackBase()
    {
        if (_scene is null || !GodotObject.IsInstanceValid(_scene)) return;

        // 顺序 = 从下往上。依次 MoveChild 到 0、1、2…，所以前面的永远压在后面的下面。
        var layers = new Node?[] { _felt, _bgLayer, _tableLayer, _boardPlate, _winnerHalo };
        var at = 0;
        foreach (var n in layers)
        {
            if (n is null || !GodotObject.IsInstanceValid(n)) continue;
            if (n.GetParent() != _scene) continue;      // 还没挂上去 / 已经摘掉的，跳过
            _scene.MoveChild(n, at);
            at++;
        }
    }

    /// <summary>轮换牌桌风格（牌桌右上角那个按钮）。换完**当场生效**。</summary>
    private void CycleTable()
    {
        var all = DouSpireTableStyles.All;
        var i = System.Array.IndexOf(all, DouSpireConfig.PokerTableStyle);
        DouSpireConfig.PokerTableStyle = all[(i + 1 + all.Length) % all.Length];
        Entry.Logger.Info($"德州：桌面（牌桌台面）换成「{DouSpireAssets.TableName(DouSpireConfig.PokerTableStyle)}」"
                          + $"（图 = assets/table/{DouSpireAssets.TableKey(DouSpireConfig.PokerTableStyle)}.png）。");
        ApplyTable();
        // ★ 换台面要**连凹槽对齐一起换** —— 每张台面的刻线位置不一样（见 ApplyMarks 那张实测表）。
        //   顺手把公共牌重画一遍：牌的宽度也跟着 _markCardW 走。
        PlacePotAndBoard();
        RefreshBoard();
        UpdateTableBtn();
    }

    private void UpdateTableBtn()
    {
        if (_tableBtn is null || !GodotObject.IsInstanceValid(_tableBtn)) return;
        // 圆钮上**不写字**（用户要的就是"小圆圈"）——
        // 名字放在钮下面那行小字里：既是"现在是什么"，也是"刚才那一点换成了什么"的回执。
        _tableBtn.TooltipText = "换牌桌台面 —— 现在：" + DouSpireAssets.TableName(DouSpireConfig.PokerTableStyle);
        if (_tableCap is null || !GodotObject.IsInstanceValid(_tableCap)) return;
        _tableCap.Text = DouSpireAssets.TableShortName(DouSpireConfig.PokerTableStyle);
    }

    /// <summary>
    /// 铺**当前场景**的整屏背景（<see cref="DouSpireConfig.PokerScene"/>）。
    ///
    /// 单独抽成一个方法，是为了**牌桌上那个「场景：…」按钮能当场换** ——
    /// 不抽的话换场景得收桌重开，试四个场景要开关四次。
    ///
    /// 图不存在就垫 <see cref="PokerRoomFallback"/>（暖色渐变），
    /// **绝不掉回斗地主那间酒馆**（那正是用户说"旁边发暗"的根源）。
    /// </summary>
    private void ApplyScene()
    {
        // 先撤掉旧的（换场景 / 重复调用都要安全）
        if (_bgLayer != null && GodotObject.IsInstanceValid(_bgLayer))
        {
            if (_bgLayer.GetParent() == _scene) _scene.RemoveChild(_bgLayer);
            _bgLayer.QueueFree();
        }
        _bgLayer = null;
        if (_scene is null) return;

        var tex = DouSpireUi.Backdrop(DouSpireAssets.PokerBgOf(DouSpireConfig.PokerScene));
        _bgLayer = tex != null ? tex : PokerRoomFallback();
        _scene.AddChild(_bgLayer);
        // 底座那几层重新叠一遍。★ 以前写死 `MoveChild(_bgLayer, 1)` —— 理由见 RestackBase。
        RestackBase();
    }

    /// <summary>轮换场景（牌桌右上角那个按钮）。换完**当场生效**。</summary>
    private void CycleScene()
    {
        var all = DouSpireScenes.All;
        var i = System.Array.IndexOf(all, DouSpireConfig.PokerScene);
        DouSpireConfig.PokerScene = all[(i + 1 + all.Length) % all.Length];
        Entry.Logger.Info($"德州：室内（牌室场景）换成「{DouSpireAssets.SceneName(DouSpireConfig.PokerScene)}」"
                          + $"（图 = assets/bg/{DouSpireAssets.SceneKey(DouSpireConfig.PokerScene)}.png）。");
        ApplyScene();
        UpdateSceneBtn();
    }

    private void UpdateSceneBtn()
    {
        if (_sceneBtn is null || !GodotObject.IsInstanceValid(_sceneBtn)) return;
        // 同 UpdateTableBtn：钮上不写字，名字在钮下面那行小字里。
        _sceneBtn.TooltipText = "换牌室场景 —— 现在：" + DouSpireAssets.SceneName(DouSpireConfig.PokerScene);
        if (_sceneCap is null || !GodotObject.IsInstanceValid(_sceneCap)) return;
        _sceneCap.Text = DouSpireAssets.SceneShortName(DouSpireConfig.PokerScene);
    }

    private void ToggleSkin()
    {
        DouSpireConfig.CardSkin = DouSpireConfig.CardSkin switch
        {
            DouSpireCardSkin.经典斗地主 => DouSpireCardSkin.尖塔牌面,
            DouSpireCardSkin.尖塔牌面 => DouSpireCardSkin.烫金赌桌,
            _ => DouSpireCardSkin.经典斗地主,
        };
        Entry.Logger.Info($"德州：牌面切成「{DouSpireConfig.CardSkin}」。");
        UpdateFaceBtn();
        Refresh();
    }

    private void UpdateFaceBtn()
    {
        if (_faceBtn is null || !GodotObject.IsInstanceValid(_faceBtn)) return;
        _faceBtn.Text = DouSpireConfig.CardSkin switch
        {
            DouSpireCardSkin.尖塔牌面 => "牌面：尖塔",
            DouSpireCardSkin.烫金赌桌 => "牌面：烫金",
            _ => "牌面：经典",
        };
    }

    /// <summary>换一批角色重开（为了试各个角色的语音 / 立绘）。</summary>
    private void Repick()
    {
        // 联机桌：这是"自己换种子重开"，会让本地跟别人分叉 —— 联机里直接不认（按钮也已藏起来）。
        if (DouSpirePokerNet.Active)
        {
            Entry.Logger.Info("德州：联机桌不支持「换一批角色」（各端会分叉），已忽略。");
            return;
        }

        _button = -1;
        _handNo = 0;
        _myTotal = 0;
        foreach (var a in _actors) a?.Despawn();
        for (var s = 0; s < _actors.Length; s++) _actors[s] = null;

        foreach (var n in _scene.GetChildren())
        {
            if (n is TextureRect tr && _seatArt.Contains(tr)) tr.QueueFree();
            if (n is TextureRect dl && _hudDealer.Contains(dl)) dl.QueueFree();
            if (n is Panel p && _hud.Contains(p)) p.QueueFree();
            if (n is HBoxContainer hb && _hudHole.Contains(hb)) hb.QueueFree();
        }
        for (var s = 0; s < 4; s++) { _seatArt[s] = null; _hud[s] = null; _hudHole[s] = null; _hudDealer[s] = null; }

        InitCharacters();
        BuildSeats();
        StartHand();
    }

    // ════════════════════════════════════════════════════════
    //  结算 / 帮助 面板
    // ════════════════════════════════════════════════════════

    private void BuildResultPanel()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop, Visible = false };
        panel.AddThemeStyleboxOverride("panel",
            DouSpireUi.BoxGlow(new Color(0.09f, 0.07f, 0.05f, 0.96f), 18, DouSpireUi.Gold, 3));
        AddChild(DouSpireUi.Place(panel, 0.5f, 0.44f, DouSpireUi.Px(760), DouSpireUi.Px(340)));

        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        col.AddThemeConstantOverride("separation", DouSpireUi.Gap(14));
        var pad = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            pad.AddThemeConstantOverride(side, DouSpireUi.Scaled(26));
        pad.AddChild(col);
        panel.AddChild(pad);

        // ★ 牌型名打大 —— 观众要有情绪点（"三条 A！" / "同花顺！"）
        _resultTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        DouSpireUi.StyleFont(_resultTitle);
        _resultTitle.SetMeta("douspire_big", true);
        _resultTitle.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        _resultTitle.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(44));
        col.AddChild(_resultTitle);

        _resultBody = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        DouSpireUi.StyleFont(_resultBody);
        _resultBody.AddThemeColorOverride("font_color", new Color(0.94f, 0.92f, 0.86f));
        _resultBody.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(19));
        col.AddChild(_resultBody);

        _resultPanel = panel;

        // —— 赢家金环（`fx/winner_halo.png`）——
        //   ★ 2026-10-10 接素材：这枚环一直没用上。摆法在 ShowResult 里按赢家**那一席的立绘**
        //     重算（每手赢家不同，位置每手都变）。平时藏着。
        _winnerHalo = DouSpireUi.Sprite(DouSpireAssets.PokerWinnerHalo, 0.5f, 0.5f,
            DouSpireUi.Px(240), DouSpireUi.Px(240));
        if (_winnerHalo is not null)
        {
            _winnerHalo.Visible = false;
            _scene.AddChild(_winnerHalo);
        }
    }

    private void ShowResult()
    {
        if (_resultPanel is null || !GodotObject.IsInstanceValid(_resultPanel)) return;
        _resultPanel.Visible = true;

        var w = _game.Winners.FirstOrDefault(-1);
        var iWon = _game.Winners.Contains(MeSeat);

        _resultTitle.Text = iWon ? "你赢了！" : w >= 0 ? $"{_game.Players[w].Name} 赢了" : "这手结束";

        var lines = new List<string>();
        if (w >= 0 && _game.WinKind == "showdown")
        {
            var seven = new List<PokerCard>(7);
            seven.AddRange(_game.Players[w].Hole);
            seven.AddRange(_game.Board);
            if (seven.Count >= 5) lines.Add($"赢家牌型：{HandEval.Name(HandEval.Best(seven, out _))}");
        }
        lines.Add(_game.ResultText);
        lines.Add("");
        for (var s = 0; s < SeatCount; s++)
        {
            var d = Delta(s);
            lines.Add($"{(s == MeSeat ? "我" : _game.Players[s].Name)}　{(d >= 0 ? "+" : "")}{d}　（剩 {_game.Players[s].Stack}）");
        }
        lines.Add("");
        lines.Add($"本桌累计：{(_myTotal >= 0 ? "+" : "")}{_myTotal}");
        _resultBody.Text = string.Join("\n", lines);

        ShowWinnerHalo(w);
    }

    /// <summary>
    /// 把金环套在赢家那一席的立绘上（`fx/winner_halo.png`）。
    /// <paramref name="w"/> 是**全局座位号**；环要挂在**视角位**那一席的坐标上（联机时两者不同）。
    /// 没有人赢（比如全员弃牌走到这里）就把环收起来。
    /// </summary>
    private void ShowWinnerHalo(int w)
    {
        if (_winnerHalo is not { } halo || !GodotObject.IsInstanceValid(halo)) return;
        if (w < 0 || w >= _game.Players.Count)
        {
            halo.Visible = false;
            return;
        }

        var s = V(w);                       // 全局位 → 视角位
        var lay = DouSpireLayout.PokerSeats[s];
        // 环的直径取"立绘宽 + 一点余量"，套在立绘**上半身**（立绘锚点在脚底，环抬高一点才罩得住）。
        var d = lay.ArtW + 0.06f;
        DouSpireUi.Place(halo, lay.ArtX, lay.ArtY - 0.13f, DouSpireUi.Px(d * 1920f), DouSpireUi.Px(d * 1920f));
        halo.Visible = true;
        // 环垫在立绘**后面**（只是"背后发光"，不挡脸）—— 交给 RestackBase 排到"桌面之上、
        // 座位立绘之下"（原来的写死索引 0 会把它塞到整屏兜底色背后，根本看不见）。
        RestackBase();
        if (_seatArt[s] is { } art && GodotObject.IsInstanceValid(art)) art.MoveToFront();
    }

    private void HideResult()
    {
        if (_resultPanel is not null && GodotObject.IsInstanceValid(_resultPanel))
            _resultPanel.Visible = false;
        if (_winnerHalo is not null && GodotObject.IsInstanceValid(_winnerHalo))
            _winnerHalo.Visible = false;
    }

    private void ToggleHelp()
    {
        if (_helpPanel is null || !GodotObject.IsInstanceValid(_helpPanel)) return;
        _helpPanel.Visible = !_helpPanel.Visible;
    }

    /// <summary>牌型速查表 —— 一页看完 10 档，随时能开（新手友好第 3 条）。</summary>
    private void BuildHelpPanel()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop, Visible = false };
        panel.AddThemeStyleboxOverride("panel",
            DouSpireUi.BoxGlow(new Color(0.07f, 0.08f, 0.10f, 0.97f), 18, DouSpireUi.Gold, 3));
        AddChild(DouSpireUi.Place(panel, 0.5f, 0.46f, DouSpireUi.Px(880), DouSpireUi.Px(600)));

        var pad = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            pad.AddThemeConstantOverride(side, DouSpireUi.Scaled(26));
        panel.AddChild(pad);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", DouSpireUi.Gap(8));
        pad.AddChild(col);

        var title = new Label { Text = "牌型速查（从大到小）", HorizontalAlignment = HorizontalAlignment.Center };
        DouSpireUi.StyleFont(title);
        title.SetMeta("douspire_big", true);
        title.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        title.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(28));
        col.AddChild(title);

        (string Name, string How)[] rows =
        {
            ("皇家同花顺", "A K Q J 10，而且同一个花色 —— 最大，几乎见不到"),
            ("同花顺",     "五张连着，而且同一个花色"),
            ("四条",       "四张一样的点数"),
            ("葫芦",       "三张一样的 + 一对（比如 888 + KK）"),
            ("同花",       "五张同一个花色，但点数不连着"),
            ("顺子",       "五张点数连着，花色随便（A 也能当 1 用）"),
            ("三条",       "三张一样的点数"),
            ("两对",       "两组一对（比如 AA + KK）"),
            ("一对",       "两张一样的点数"),
            ("高牌",       "啥都没凑成，比最大的那张"),
        };

        for (var i = 0; i < rows.Length; i++)
        {
            var line = new Label
            {
                Text = $"{i + 1,2}. {rows[i].Name}　—— {rows[i].How}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            DouSpireUi.StyleFont(line);
            line.AddThemeColorOverride("font_color",
                i < 2 ? new Color(1f, 0.82f, 0.35f) : new Color(0.92f, 0.92f, 0.90f));
            line.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(17));
            col.AddChild(line);
        }

        var tip = new Label
        {
            Text = "小提示：每人手上 2 张底牌 + 桌上 5 张公共牌，一共 7 张里挑最好的 5 张比大小。\n"
                   + "牌型一样就比点数；点数也一样就平分底池。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        DouSpireUi.StyleFont(tip);
        tip.AddThemeColorOverride("font_color", new Color(0.72f, 0.80f, 0.92f));
        tip.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(16));
        col.AddChild(tip);

        col.AddChild(MakeActionButton("知道了", ToggleHelp));

        _helpPanel = panel;
    }

    // ════════════════════════════════════════════════════════
    //  画牌
    // ════════════════════════════════════════════════════════

    /// <summary>一张明牌：牌底 + 左上角点数 + 正中央花色（和斗地主桌同一套排版）。</summary>
    private static Control MakeCardFace(PokerCard card, float w, bool highlight)
    {
        var h = w * 1.40f;
        var panel = new Panel
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(w), DouSpireUi.Px(h)),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };

        var body = DouSpireAssets.First(BodyChain(false));
        panel.AddThemeStyleboxOverride("panel", body != null
            ? DouSpireUi.TextureBox(body)
            : DouSpireUi.Box(DouSpireUi.CardFace, 8, new Color(0.25f, 0.22f, 0.20f), 2));

        var W = DouSpireUi.Px(w);
        var H = DouSpireUi.Px(h);

        // 左上角点数
        var pip = DouSpireAssets.First(PipChain(card.Rank));
        if (pip != null)
        {
            var r = new TextureRect
            {
                Texture = pip,
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            r.AnchorLeft = r.AnchorRight = 0f;
            r.AnchorTop = r.AnchorBottom = 0f;
            r.OffsetLeft = W * 0.06f;
            r.OffsetTop = H * 0.030f;
            r.OffsetRight = W * 0.46f;
            r.OffsetBottom = H * 0.245f;
            panel.AddChild(r);
        }

        // 正中央花色
        var suit = DouSpireAssets.First(SuitChain(card.Suit));
        if (suit != null)
        {
            var s = new TextureRect
            {
                Texture = suit,
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            s.AnchorLeft = s.AnchorRight = 0.5f;
            s.AnchorTop = s.AnchorBottom = 0.5f;
            s.OffsetLeft = -W * 0.27f;
            s.OffsetTop = -H * 0.19f;
            s.OffsetRight = W * 0.27f;
            s.OffsetBottom = H * 0.19f;
            panel.AddChild(s);
        }

        // 摊牌高亮：赢家那 5 张圈一圈金边
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

    /// <summary>一张牌背（对手的底牌 / 还没翻的公共牌）。</summary>
    private static Control MakeCardBack(float w)
    {
        var h = w * 1.40f;
        var panel = new Panel
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(w), DouSpireUi.Px(h)),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        var back = DouSpireAssets.First(BackChain());
        panel.AddThemeStyleboxOverride("panel", back != null
            ? DouSpireUi.TextureBox(back)
            : DouSpireUi.Box(new Color(0.20f, 0.08f, 0.10f), 8, DouSpireUi.Gold, 2));
        return panel;
    }

    /// <summary>一个空牌位（公共牌还没发到这一张）。</summary>
    private static Control MakeCardSlot(float w)
    {
        var h = w * 1.40f;
        var panel = new Panel
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(w), DouSpireUi.Px(h)),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        panel.AddThemeStyleboxOverride("panel",
            DouSpireUi.Box(new Color(0f, 0f, 0f, 0.18f), 8, new Color(1f, 1f, 1f, 0.16f), 2));
        return panel;
    }

    // ── 牌面取图：跟着设置里那一档走（经典 / 烫金）──

    private static string[] BodyChain(bool selected) => DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
        ? DouSpireAssets.GildedBody(selected)
        : selected ? DouSpireAssets.CardBodyOn : DouSpireAssets.CardBody;

    private static string[] PipChain(int rank) => DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
        ? DouSpireAssets.GildedPip(rank)
        : new[] { $"cards/pip_{PokerCard.RankName(rank)}.png" };

    private static string[] SuitChain(PokerSuit s)
    {
        var d = MapSuit(s);
        return DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
            ? DouSpireAssets.GildedSuit(d)
            : DouSpireAssets.SuitOf(d);
    }

    private static string[] BackChain() => DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌
        ? DouSpireAssets.GildedBack
        : DouSpireAssets.CardBack;

    /// <summary>德州的花色 → 斗地主的花色（两套枚举名一样，只是分属两个类型）。</summary>
    private static DouSpireRules.Suit MapSuit(PokerSuit s) => s switch
    {
        PokerSuit.Spade => DouSpireRules.Suit.Spade,
        PokerSuit.Heart => DouSpireRules.Suit.Heart,
        PokerSuit.Club => DouSpireRules.Suit.Club,
        _ => DouSpireRules.Suit.Diamond,
    };

    // ════════════════════════════════════════════════════════
    //  小工具
    // ════════════════════════════════════════════════════════

    // ★ 2026-10-11 删掉了 PlateBox（用 StyleBoxTexture 铺横牌匾的那套）。
    //   原因：Godot 的九宫格**边角是 1:1 画的、不跟着缩放**
    //   （见 servers/rendering/renderer_rd/shaders/canvas.glsl 的 map_ninepatch_axis），
    //   而我们的牌匾控件普遍比原图小 3~6 倍 —— 边角显得硕大、中间被挤成一条缝。
    //   现在统一走 DouSpireUi.Plate（自己切三块，装饰帽按高度等比缩）。

    /// <summary>
    /// 动作按钮。「收桌 / 牌面 / 牌型速查」和「弃牌 / 过牌 / 跟注 / 加注 / 全下」都走这里。
    ///
    /// ★ 2026-10-09：加 <paramref name="icon"/> —— 用户问「**要我画的那些筹码和那些 ui
    ///   怎么没用上？**」。`poker/act_*.png` 那 5 张金色浮雕图标 18:51 就在库里了，
    ///   但代码里**一条都没引用**（按钮上一直只有文字）。现在把图挂到 <see cref="Button.Icon"/> 上。
    ///   图不存在就**只留文字**，跟以前一样。
    /// </summary>
    private static Button MakeActionButton(string text, System.Action onPressed, string[]? icon = null)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(150), DouSpireUi.Px(54)),
        };
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        btn.MouseEntered += DouSpireSfx.Hover;   // 划过一声很轻的官方 hover 音
        if (!DouSpireUi.ImageButton(btn, DouSpireAssets.BtnSecondary, 12, 22, 10)
            && !DouSpireUi.ApplyGradButton(btn, 10))
        {
            btn.AddThemeStyleboxOverride("normal", DouSpireUi.Box(DouSpireUi.Panel, 10, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("hover", DouSpireUi.Box(new Color(0.18f, 0.15f, 0.10f), 10, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("pressed", DouSpireUi.Box(DouSpireUi.Panel, 10, DouSpireUi.Gold, 3));
        }
        btn.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        btn.SetMeta("douspire_big", true);
        btn.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(19));

        // 图标（可选）：挂在文字左边。按钮本身不高（54），图标压到 34 免得把按钮撑高。
        if (icon != null && DouSpireAssets.First(icon) is { } iconTex)
        {
            btn.Icon = iconTex;
            btn.ExpandIcon = true;
            // ⚠️ 这个 Godot 版本没有 `Button.IconMaxWidth` 属性 —— 得走主题常量 `icon_max_width`。
            btn.AddThemeConstantOverride("icon_max_width", DouSpireUi.Scaled(34));
            // 图和字之间留一点缝，不然贴在一起。
            btn.AddThemeConstantOverride("h_separation", DouSpireUi.Gap(8));
        }
        return btn;
    }

    // ── 桌面两个圆钮（用户 2026-10-09：「两个小圆圈…镶嵌在桌子上」）──
    //
    //   ★★ 颜色为什么**跟桌面风格和 UI 皮肤都不挂钩**（写死一套中性香槟金）：
    //      别人问"换个台面会不会变得奇怪"——会。这两颗钮要压在**七张桌子**上
    //      （绿绒 / 黄铜 / 黑曜石 / 冰蓝水晶 / 二次元 / 漆器 / 蒸汽朋克），
    //      还要在五套 UI 皮肤下都好看：跟风染一次就得试 35 种组合。
    //      中性金 + 半透明深底是这七张桌子里唯一共同的色（每张桌子都有金属件），
    //      所以写死一套最省事，也最不容易翻车。
    //   ★ 顺带一个技术好处：**不走 `DouSpireUi.Box`**（那个会转发给 DouSpireTheme.Panel，
    //      按皮肤把圆角乘个 0.8~1.5 的系数）—— 圆角一乘，正方形按钮就不是正圆了。
    //      这里自己拼 StyleBoxFlat，半径直接给死"一半边长"，任何皮肤下都是正圆。

    /// <summary>圆钮的圆环色（香槟金）。</summary>
    private static Color KnobRing => DouSpireKnob.Ring;

    /// <summary>圆钮直径（1920 基准像素）。★ 2026-10-10 移交给 <see cref="DouSpireKnob.Size"/>。</summary>
    private static float KnobSize => DouSpireKnob.Size;

    /// <summary>
    /// 桌面上的**小圆钮**（「室内」/「桌面」）。用户 2026-10-09：
    /// 「**做两个小圆圈型的就行了，还可以镶嵌在桌子上，做成互动，画风方面与每个桌子
    /// 配合都不会奇怪就行了**」。
    ///
    /// ★★ 2026-10-10：**实现搬到 <see cref="DouSpireKnob"/> 了** ——
    ///   斗地主桌要角上同一排钮（用户：「给斗地主加，德州的那两个按钮」），
    ///   两份各写一遍必然漂移（改了这边忘了那边）。这里只留一层薄壳转发，
    ///   省得改一堆调用点。
    /// </summary>
    /// <param name="caption">出参：钮下面那行小字（由 UpdateSceneBtn / UpdateTableBtn 填内容）。</param>
    private static Button MakeRoundKnob(DouSpireKnobIcon.Kind kind, System.Action onPressed,
                                        out Label caption)
        => DouSpireKnob.Make(kind, onPressed, out caption!);

    private static void ClearChildren(Node n)
    {
        foreach (var c in n.GetChildren())
        {
            n.RemoveChild(c);
            c.QueueFree();
        }
    }

    /// <summary>让某一席的活角色做个动作 / 表情。失败只记日志。</summary>
    // ════════════════════════════════════════════════════════
    //  语音（借斗地主那套素材，德州不另造一套）
    // ════════════════════════════════════════════════════════
    //
    //  ── 用户 2026-10-10："德州扑克一句语音没有。你看看斗地主然后也配置一下。" ──
    //
    //  素材是**按斗地主事件编好号**的（1_deal / 10_pass / 9_bomb …，见 DouSpireVoice），
    //  德州不另造一套，直接把动作对到语义最接近的槽位 —— 好处是六个语音包
    //  （编外四套 + 本体四角色 + 储君）**一行都不用改**，用户以后往
    //  `assets/voice/<角色>/` 里丢新文件也自动轮播（见 DouSpireVoice.Pick）。
    //
    //  真要说"德州专属台词"，那是**素材**的事（丢新 ogg 即可），不是代码的事。

    /// <summary>德州的一步动作 → 借哪一句斗地主槽位的语音。</summary>
    private static DouSpireCharacters.Mood MoodFor(PokerAction a) => a switch
    {
        PokerAction.Fold => DouSpireCharacters.Mood.Pass,      // 弃牌 ≈ "要不起，我不跟了"
        PokerAction.Check => DouSpireCharacters.Mood.Single,   // 过牌 = 最小动作
        PokerAction.Call => DouSpireCharacters.Mood.Single,    // 跟注 = 小动作
        PokerAction.Bet => DouSpireCharacters.Mood.Beat,       // 下注 = 主动压一手
        PokerAction.Raise => DouSpireCharacters.Mood.Beat,     // 加注 = 压得更狠
        PokerAction.AllIn => DouSpireCharacters.Mood.Bomb,     // 全下 = 大招（"全体起立"）
        _ => DouSpireCharacters.Mood.Idle,
    };

    /// <summary>
    /// 让第 <paramref name="seat"/> 席（**视角位**，和 <c>_chars</c> / <c>_actors</c> 同一套下标）
    /// 为某件事开口。
    ///
    /// 认不出角色 / 空座 / 玩家没装语音包都只是安静 —— 绝不因为语音把牌局搞崩
    /// （<see cref="DouSpireVoice"/> 内部已把每一步异常都吞掉，这里再兜一层）。
    /// </summary>
    private void Speak(int seat, DouSpireCharacters.Mood mood)
    {
        if (seat < 0 || seat >= _chars.Length) return;
        try { DouSpireVoice.ReactAs(_chars[seat], mood, seat); }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"德州：第 {seat} 席语音没播成（不影响玩）: {e.Message}");
        }
    }

    /// <summary>一步动作 / 一次输赢 → 借斗地主槽位开口。</summary>
    private void Speak(int seat, PokerAction action, bool win = false)
        => Speak(seat, win ? DouSpireCharacters.Mood.Win : MoodFor(action));

    private void ReactActor(int seat, PokerAction action, bool win = false)
    {
        // ★ 语音先播 —— **不能放在"有没有活角色"的判断后面**：
        //   编外角色 / 关掉活角色的桌没有骨骼模型，但照样该开口
        //   （斗地主 WhaleReact 踩过同一个坑，见它的注释）。
        Speak(seat, action, win);

        try
        {
            var a = seat >= 0 && seat < _actors.Length ? _actors[seat] : null;
            if (a is null || !a.Alive) return;

            if (win) { a.PlayWin(); return; }
            switch (action)
            {
                case PokerAction.Fold: a.PlayHit(); break;
                case PokerAction.AllIn: a.PlayAttack(); break;
                default: a.PlayAttack(); break;
            }
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"德州：第 {seat} 席的角色动作失败（只影响好看程度）: {e.Message}");
        }
    }
}
