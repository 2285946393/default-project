using Godot;
using DouSpireRules;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire.Table;

/// <summary>
/// 游戏里的那一张斗地主桌 —— **整屏一幅场景**，不是浮框。
///
/// **它是一个独立窗口，不跑尖塔。** 没有地图、没有怪物、没有房间 ——
/// 从主菜单点「斗地主尖塔」进来（或按 F9），看到的就是一张桌子。
/// 用户原话：「**不应该出现怪物，就一个桌子然后你选的角色，和ai选的角色出现，
/// 说到底只是在游戏里面开个斗地主的窗口**」。
///
/// ── 图层（自下而上）────────────────────────────────────────────
/// <code>
///  felt 纯色兜底 → bg 背景图 → 暗角
///  → 圆桌 → 星盘 → 牌堆 → 回合光环 → 三家立绘
///  → 皇冠 / 地主徽章 → 底牌 3 张 → 场上最后一手 → 中央文字
///  → 三家 HUD（头像框/血条/宝石/剩牌）→ 标题
///  → 我的手牌 → 操作按钮 → 圆形菜单按钮
///  → 选人覆盖层
/// </code>
///
/// 每一层都是「**有贴图就用贴图，没有就退回纯色块**」——
/// 美术可以一张一张往里丢（`mods\DouSpire\assets\`），丢一张立刻生效，
/// <b>不用重编 dll</b>。槽位表和缺失清单见 <see cref="DouSpireAssets"/> 与
/// `assets/素材清单.md`。
///
/// 位置全部用**相对坐标**（<see cref="DouSpireUi.Place"/>），尺寸用
/// <see cref="DouSpireUi.Scaled"/> 按屏幕宽等比放大 —— 换分辨率不跑位。
/// 想微调摆位只改下面那一组 <c>*_AX / *_AY</c> 常量。
/// </summary>
public sealed partial class DouSpireTable : Control
{
    /// <summary>座位 0 永远是"我"，1 = 下家（右），2 = 上家（左）；4 人时 2 = 对家（上）、3 = 上家（左）。</summary>
    private const int MeSeat = 0;

    /// <summary>
    /// 本桌座位数。开局前跟设置走，开局后以牌局为准（联机协商结果也覆盖它）。
    /// 所有座位循环一律用它，**不许再写死 3**。
    /// </summary>
    // ★★ 2026-10-09 修：`_game` 没建好时**不能**一律回退到设置里的「人数」。
    //   两个真实后果（用户联机实测：①"四人不是四个人是三个人" ②"还有旧的静态贴图"）：
    //     · `BuildScene` 跑在 `_game` 建好**之前**（联机尤其明显：`_game` 要等主机广播摆牌），
    //       于是它按设置里的 4 去建了 4 份光环 / 立绘 / 选人行；
    //     · 之后所有每帧循环都用真值 3 ⇒ **第 4 席的静态立绘永远没人回收**，
    //       建出来时 `Visible` 就是 true ⇒ 那张图就那么永久挂在屏幕上。
    //   联机桌的席位数是**常量** `DouSpireNet.Seats`（=3，人和 AI 补位都算在里面）。
    /// <summary>开桌时定的席位数（`Open` 传进来的）—— `_game` 建好之前唯一准的人数来源。</summary>
    private int _openSeats;

    private int SeatCount => _game?.Players.Count
                             ?? (_openSeats > 0 ? _openSeats
                                 : _netOn ? DouSpireNet.Seats : (int)DouSpireConfig.TableSize);

    /// <summary>
    /// **选人界面**要摆几行：联机固定按联机的席位数，单人看设置里的人数。
    /// 刻意**不读 `_game`** —— 切换人数那一瞬间 `_game` 还是上一局的，读了就会刷错行数
    /// （见 <see cref="RefreshPicker"/> 的注释）。
    /// </summary>
    private int PickerSeats => _netOn ? DouSpireNet.Seats : (int)DouSpireConfig.TableSize;

    /// <summary>选中牌那圈高亮框的节点名 —— 反复选中/取消时靠它判断"已经加过了"，避免叠框。</summary>
    private const string SelFrameName = "DouSpireSelFrame";

    // ════════════════════════════════════════════════════════
    //  摆位常量（想调布局只改这里；单位是"父级尺寸的比例"）
    //
    //  ⚠️ 三家**坐哪**不在这里 —— 在 DouSpireLayout（可改文件、不用重编）。
    //  这里只管牌桌上的物件。
    // ════════════════════════════════════════════════════════

    private const float TableAx = 0.50f, TableAy = 0.455f;
    private const float CompassAx = 0.50f, CompassAy = 0.395f;
    private const float DeckLAx = 0.315f, DeckLAy = 0.372f;
    private const float DeckRAx = 0.685f, DeckRAy = 0.372f;

    // 底牌 3 张：**贴到屏幕最顶上、缩小**（用户 2026-10-06："按道理来说太大了，应该在最顶上"）
    // 底牌：对齐桌图上自带的那三个空牌位（中心 964,290，间距 116，牌位 99×153）
    private const float BottomCardsAx = 0.502f, BottomCardsAy = 0.269f;
    private const float TrickAx = 0.502f, TrickAy = 0.518f;             // 场上最后一手

    // 杀戮尖塔 2 主题水印（桌面中央那个 logo）——**单独一组坐标**。
    // ★ 2026-10-10（用户："斗地主的这个杀戮尖塔标题ui换成那种渲染过的版本。并且要往上移动，现在不好看"）：
    //   原来它和 `TrickAy`（0.518）共用坐标，正压在"场上最后一手"那一行上 ——
    //   一出牌就和牌面糊在一起。现在往上抬到 0.335，落在底牌(0.269)和最后一手(0.518)中间的空档里。
    //   横向只占中间一段（0.5 ± 0.115），不会碰两侧的底牌位（x≈0.1 / 0.9）。
    //
    // ★ 2026-10-10 晚（用户："杀戮尖塔ui太大了…我更建议缩小点"）：
    //   换彩色素材那一次按真实比例放大到了 600×266 —— 那是**1920 宽的 31%**，
    //   比出牌区（520 = 27%）还宽，所以看着压场。现在缩到 **440×195**（1920 的 22.9%），
    //   比出牌区窄一圈，退成"桌面底纹"该有的分量。
    //   比例保持素材原始 1192×528（2.258）：440 / 2.258 ≈ 195。
    //   ⚠️ 还想再调就只动这两个数（改 W 后记得 H = W / 2.258，别把 logo 拉扁）。
    private const float MarkAx = 0.50f, MarkAy = 0.335f;
    private const float MarkW = 440f, MarkH = 195f;

    // 手牌：**必须落在操作按钮下面**。牌高 168px 且是从格子顶部开始摆的，
    // 原来 0.900 让牌顶伸到 0.805，正好被"出牌/要不起/提示"（0.765~0.825）压住。
    //（用户 2026-10-07："我的牌全被出牌提示挡住了"）
    private const float HandAy = 0.945f;
    private const float StatusAy = 0.726f;                               // "轮到你出牌"条幅（醒目）
    // 操作按钮：往上一格，给下面 168px 高的手牌腾地方
    //（用户 2026-10-07："我的牌全被出牌提示挡住了"）
    private const float ActionAy = 0.770f;
    // 出牌历史：**左下角、但往右让开我的立绘**。
    // 用户 2026-10-09 拍板："日志到左下，第四个信息和人物到右下"；
    // 同一天又报："我的立绘在下面太往下了…往上抬，然后我又发现自己的立绘被明牌挡住了"。
    // 立绘从 0.950 抬到 0.855 之后**整个人都进画面了**，面板再钉在 x0.115 就是一整片压在他胸口
    //（面板 360px 宽 → 半宽 0.0703@2560；立绘 240px 宽 → 中心 0.115、右缘 0.1775）。
    // 挪到 0.285：左缘 0.215（离立绘右缘还剩 ~96px），右缘 0.355（离「出牌/要不起/提示」
    // 那排按钮的左缘 0.37 还剩 ~38px）。y 不动 —— 还是左下那一块，只是不压人了。
    private const float LogAx = 0.285f, LogAy = 0.775f;

    // 倍数翻倍（明牌 / 加倍 / 炸弹）在牌堆中央炸的那个筹码特效的缩放。
    // 本体自己播这条时不缩放（`VfxCmd.PlayVfx` 只设 GlobalPosition），
    // 所以 1.0 = 本体原始大小。觉得太小/太大就调这一个数。
    private const float MultiplierVfxScale = 1.0f;

    // ════════════════════════════════════════════════════════
    //  静态状态：收桌再开时接着来
    // ════════════════════════════════════════════════════════

    private static DouSpireTable? _instance;

    /// <summary>
    /// 「按人数重开牌桌」专用的**残骸标记**。
    ///
    /// 为什么需要：<c>ReopenForNewTableSize()</c> 是在**同一个延迟回调里"先 Close 再 Open"**，
    /// 而 Godot 的 <c>QueueFree()</c> 要等**帧末**才真正回收 ⇒ 旧桌子的 <see cref="_ExitTree"/>
    /// 会排在**新桌子 <c>_Ready</c> 之后**才跑。那时全局联机状态早已是新桌子的了，
    /// 旧桌子若还当自己是当前桌，就会朝对面广播**假的"收桌"**、并把新桌子的
    /// <c>DouSpireNet.Live</c> 一起抹掉（2026-10-09 联机实测就是这么炸的）。
    /// </summary>
    private bool _silentExit;

    /// <summary>
    /// 「切人数重开」时给**下一张桌子**的留言：**停在选人界面，别直接开局**。
    ///
    /// 为什么需要：切人数走的是 <c>Close() + Open()</c>（新桌子新实例），
    /// 而新桌子的 <see cref="RestoreOrPick"/> 会顺着"存档恢复 / 篝火直接开局"那条路
    /// **直接进牌桌** —— 玩家看到的就是"一切人数就直接进去了、没让我选人"（用户实测原话）。
    /// 这是个一次性留言：新桌子读到就清掉。
    /// </summary>
    private static bool _reopenInPicker;

    private static GameState? _savedGame;
    private static string[] _savedActions = new string[4];
    private static int _savedMultiplier = 1;   // 存档那局的倍数（恢复时补回，不然结算按 ×1 算）
    private static DouSpireCharacters.SeatPick[]? _savedChars;

    public static bool IsOpen => _instance != null && GodotObject.IsInstanceValid(_instance);

    // ════════════════════════════════════════════════════════
    //  一局的状态
    // ════════════════════════════════════════════════════════

    private GameState _game = null!;
    private DouSpireCharacters.SeatPick[] _chars = new DouSpireCharacters.SeatPick[4];
    private readonly HashSet<Card> _selected = new();
    private string[] _lastAction = new string[4];
    private double _aiTimer;
    private bool _picking;
    private bool _broken;

    // 音效的两个"只播一声"闸门：轮到谁（变了才响）、本局结束（只响一次）
    private int _lastCurrentSeat = -1;
    private bool _endSfxDone;
    /// <summary>定地主那一下的"称王"动作只做一次；换局（<see cref="Deal"/>）时复位。</summary>
    private int _lordAnimSeat = -1;

    // 编外角色（鲸鱼娘）的临时表情：哪一席、贴哪张、到什么时候恢复常态
    private readonly string?[] _whaleFace = new string?[4];
    private readonly double[] _whaleFaceUntil = new double[4];
    private readonly bool[] _watchPass = new bool[4];
    private DouSpireRules.Combo? _watchLastPlay;
    private bool _winFaceDone;

    /// <summary>本局结算的时刻（秒）。终局庆祝窗口起点 —— 「再来一局/重新选人」晚 2.5s 再亮，
    /// 不然金币雨还没落完按钮就出来了，庆祝整个被埋掉（用户 2026-10-08 晚）。</summary>
    private double _roundOverAt;

    // 待机动画的播放头（12fps 循环，让编外角色"一直动"）
    private int _idleFrame;
    private double _idleClock;

    // 「会动的表情」：这一席正在播哪个表情、播到第几帧（null = 没在用动画表情）
    private readonly string?[] _faceAnim = new string?[4];
    private int _faceAnimFrame;

    // 场面检测的"上一次"快照：轮到谁、各家剩几张、选了几张
    private int _watchCurrent = -1;

    // 出牌倒计时：轮到我那一刻的秒数（0 = 没在等我）、"5 秒催过没有"
    private double _myTurnSince;
    private bool _myTurnNagged;
    private readonly int[] _watchHand = new int[4];
    private int _watchSelected = -1;

    // ════════════════════════════════════════════════════════
    //  界面引用
    // ════════════════════════════════════════════════════════

    private Control _scene = null!;      // 桌面场景整层
    private Control _picker = null!;     // 选人覆盖层

    // 三家
    private readonly TextureRect[] _seatArt = new TextureRect[4];
    private string _seatArtKey = "";             // 当前立绘对应的三家 id，变了才重贴
    private readonly TextureRect?[] _seatGlow = new TextureRect?[4];
    /// <summary>
    /// 三席的**活角色**（游戏本体带骨骼的那个，见 <see cref="DouSpireActor"/>）。
    /// 为 null = 这一席没能请到活人（编外角色 / 加载失败），就用 <see cref="_seatArt"/> 那张静态立绘。
    /// </summary>
    private readonly DouSpireActor?[] _actors = new DouSpireActor?[4];
    private readonly Control[] _hud = new Control[4];
    private readonly TextureRect?[] _hudAvatar = new TextureRect?[4];
    private readonly TextureRect?[] _hudCrown = new TextureRect?[4];   // 地主小皇冠（贴在头像旁，用户："放在头像旁边就行了"）
    private readonly TextureRect?[] _hudBadge = new TextureRect?[4];   // 地主小勋章
    private readonly Label[] _hudName = new Label[4];

    /// <summary>
    /// 「轮到谁出牌」的那个三角（2026-10-09 用户："到谁出牌了那个三角形太小了，加大一点"）。
    ///
    /// 原来是往名字里塞一个字符 —— <c>_hudName[seat].Text = (turn ? "▶ " : "") + p.Name;</c> ——
    /// 字号跟着名字走（只有 19），摆在 112px 那一列，小得根本没人在意。
    /// 现在换成**独立的大三角**：挂在信息框**左外侧**（框里 112px 左边那条竖带已经被头像占满，
    /// 塞进去只会压头像），金色 + 描边 + 呼吸缩放，轮到谁谁亮。
    /// </summary>
    private readonly Label?[] _hudTurn = new Label?[4];

    /// <summary>三角的呼吸相位（<see cref="TickTurnMark"/> 用）。</summary>
    private double _turnMarkPhase;

    // ══════════════ 出牌倒计时环（2026-10-10 加）══════════════
    //  用户："加个显示时间的 ui，每个人出牌限制 20 秒。（记得高级点。）"
    //  原来只有**第 5 秒的一句台词** + 到点自动过 —— 玩家根本不知道还剩几秒。
    //  现在每家 HUD 的头像外圈套一枚金环（见 DouSpireTurnTimer），轮到谁谁亮、读秒。
    /// <summary>三席各一枚倒计时环（套在自己头像外圈）。</summary>
    private readonly DouSpireTurnTimer?[] _turnTimer = new DouSpireTurnTimer?[4];

    /// <summary>
    /// 倒计时环**现在挂在哪个座位**（-1 = 没挂）。
    /// 存在的意义：只在"换人"那一帧重新挂环，**别每帧重置** ——
    /// 每帧 Arm 一次等于秒数永远停在满格。
    /// </summary>
    private int _timerSeat = -1;

    // ── 选人倒计时（2026-10-10 加，用户："然后选人也要限制时间。"）──
    /// <summary>选人界面上那行「还有 XX 秒自动开始」（队友一直不点「开始打牌」时才亮）。</summary>
    private Label? _pickCountdown;

    // ── 钱（筹码）：进场时按「我这边的真实情况」发，胜负按倍数往两边走 ──
    //   用户 2026-10-07："进房间你应该给每个玩家发钱 1000，然后三个地方显示三个人的钱的 ui（尖塔自带的钱的样子）"
    //   用户 2026-10-08 更正："没有读取我身上的 99 金币，还是以一千金币为底注"
    //     ⇒ 篝火赌注模式下，**三家起始筹码都 = 我身上的真实金币**（1000 只在普通开桌/无赌注时用）。
    //   用户 2026-10-08 追加："ai应该跟我的钱差不多。不能让我赢太多。也不能让我输太多。"
    //     ⇒ ① 对手不再固定 1000（那样我 99 本金却能从 1000 的对手身上赢 1000）；
    //        ② 一局输赢按 BetMaxSwingPercent 封顶（地主一把 ×64 会把人直接打回零元）。
    //   ⚠️ 原来是 `static readonly` —— 意味着**第二局不会重置**（接着上一局的数走）。现在改成实例字段、
    //      每局由 ResetMoney() 重新发牌，顺手修掉那个隐藏 bug。
    private readonly int[] _money = { 1000, 1000, 1000, 1000 };   // 4 格：三人只用前 3（2026-10-09：4 人局第 3 席发筹码越界）

    /// <summary>
    /// 记分板模式的筹码**这个会话发过没有**。发过之后「再来一局」就接着算，不再重发 1000
    /// （用户 2026-10-08："打完一局又变1000了，我本来是负数的。应该继续计算"）。
    /// 只在记分板分支（<see cref="ResetMoney"/> ①）置位；收桌重开是新的桌子实例，自然归位。
    /// </summary>
    private bool _moneySeeded;
    private readonly Label[] _hudMoney = new Label[4];
    private readonly TextureRect?[] _hudGoldIcon = new TextureRect?[4];
    private bool _moneyPaidOut;           // 这一局的钱结算过了没（只结一次）
    private int _stakeUnit = DouSpireConfig.BaseStake;   // 这一局「一份」多少筹码（见 ResetMoney）
    private int _stakeCap;               // 这一局「一份」的上限（0 = 不封顶，普通开桌就是 0）
    private bool _stakeCapped;           // 这一局真的撞上封顶了没（结算文案要说明一下）

    /// <summary>
    /// 非 null = **赌注模式**：这桌是在游戏篝火里开的，筹码要落到玩家的真实金币上。
    /// 由 <see cref="OpenAtRestSite"/> 挂上，普通开桌（F9 / 主菜单）恒为 null。
    /// 联机时**每个端挂自己的那个 Player** —— 输赢各算各的（牌局是同一副，所以结果天然一致）。
    /// </summary>
    private static Player? _stakePlayer;

    // ════════════════════════════════════════════════════════
    //  联机（主机 / 客机同桌）
    //
    //  设计：**主机是唯一权威**。它跑 AI、它校验每个人的出牌、它把结果广播给所有人。
    //  客机不推进任何决策，只做两件事：把玩家操作发给主机、把主机的广播照做。
    //  座位号：网络消息里一律用**全局座位号**；牌桌上（本文件）一律用**本地视角**
    //  （0 号位永远是我），换算只在 <see cref="DouSpireNet.ToGlobal"/>/<see cref="DouSpireNet.ToLocal"/> 上做。
    //  详见 DouSpireNet.cs。
    // ════════════════════════════════════════════════════════

    /// <summary>这桌跑在联机里吗？（篝火 + 多人房才 true）</summary>
    private bool _netOn;
    /// <summary>我是不是这一桌的主机。</summary>
    private bool _netHost;
    /// <summary>联机会话已经开起来了没（防止重复开）。</summary>
    private bool _netStarted;
    /// <summary>已经按哪一代开过桌了（主机重开桌时会 +1，见 DouSpireNet.Gen）。</summary>
    private int _netGen = -1;
    /// <summary>客机等主机开桌已经等了多久（40 秒没人应答才自己收桌 —— 主机要先收集"大家都点了没"，最长 25 秒）。</summary>
    private double _netOpenWait;

    /// <summary>
    /// 客机：**"联机 run 没了"这个硬信号持续了多久**（秒）。
    /// 单独一帧为 false 不算 —— 换房间 / 重连的一瞬间也会假 false，
    /// 得连续 2.5 秒都是 false 才认（见 <see cref="NetLinkGuard"/>）。
    /// </summary>
    private double _netDeadWait;

    /// <summary>客机往主机发心跳的计时器（每 10 秒一个，见 <see cref="DouSpireNet.SendPing"/>）。</summary>
    private double _netPingWait;

    /// <summary>这一局正式开局了没（防止 ActStart 重复触发）。</summary>
    private bool _roundStarted;

    /// <summary>「等别人…」提示 —— 客机在等主机回话时，中央横幅显示这个（RefreshRpsUi 优先显示它）。</summary>
    private string _netWait = "";

    /// <summary>四个"问话阶段"里，各席表态了没（本地视角）。每进一个阶段清零。</summary>
    private readonly bool[] _showDone = new bool[4];
    private readonly bool[] _wantDone = new bool[4];
    private readonly bool[] _throwDone = new bool[4];
    private readonly bool[] _doubleDone = new bool[4];

    /// <summary>主机侧：远程人类回合已经等了多久（超时就替他出，绝不卡死）。</summary>
    private double _remoteWait;
    /// <summary>主机侧：上一次看到的"轮到谁"（换人就重置超时计时）。</summary>
    private int _lastNetTurn = -1;

    /// <summary>主机侧：2v2 扣牌阶段，远程真人这一席已经等了多少秒（超时替他扣）。
    /// ★ 2026-10-11：这个计时**必须每帧累加**（见 <see cref="StepDiscard"/> 里的血泪注释）——
    ///   原来被 `_aiTimer` 门控住，30 秒的兜底实际要等半个多小时，等于没有兜底。</summary>
    private double _discardWait;
    /// <summary>同上：每 5 秒往日志里报一次"还在等谁扣牌"（下次卡住看日志就知道卡在谁身上）。</summary>
    private double _discardLogWait;
    /// <summary>2v2 扣牌最多等多少秒就替远程真人扣（和出牌那条同款兜底思路）。
    /// ★ 2026-10-11：30 → 15。出牌限时是 20 秒，扣牌只是一张牌，不该比出牌还等得久；
    ///   用户实测"队友不扣牌 → 我这桌一直出不了牌"，兜底越快越好。</summary>
    private const double DiscardWaitLimit = 15.0;

    /// <summary>主机侧：远程真人在"问话阶段"（明牌/抢/加倍）已经等了多久。</summary>
    private double _netAskWait;
    /// <summary>主机侧：上一次盯的是哪一席（换人就重置上面的计时）。</summary>
    private int _netAskSeat = -1;

    /// <summary>三家都不抢 → 换牌重发的次数（太多就强行定地主，防死循环）。</summary>
    private int _redealCount;

    private readonly Label[] _hudCount = new Label[4];
    private readonly Label[] _hudPlay = new Label[4];
    private readonly Label[] _hudTag = new Label[4];

    // 中央
    private HBoxContainer _bottomRow = null!;
    private Label _bottomTag = null!;
    private HBoxContainer _trickRow = null!;
    private Label _centerLabel = null!;
    private Control _statusPanel = null!;
    private Label _statusLabel = null!;
    private Label _titleLabel = null!;
    private Label _logLabel = null!;

    // 倍数：左上角常驻一个小的（当前几倍），翻倍时在牌堆中央弹一个大字
    //（用户 2026-10-07："倍数也得出个 ui。放在一个位置……倍数会翻倍用一个动画出现在牌堆中央"）
    private Label _multiplierLabel = null!;
    private Label _multiplierPop = null!;

    // 手牌与按钮 —— 手牌不用容器，自己摆（同类牌要叠起来，见 RebuildHand）
    private Control _handRow = null!;
    private Button _playBtn = null!, _passBtn = null!, _hintBtn = null!, _nextBtn = null!;
    /// <summary>「重新选人」—— 一局打完除了「再来一局」之外，还能回到选人界面换一套角色再开
    /// （用户 2026-10-08："希望能够直接重开对局选人"，为了试各个角色的语音）。</summary>
    private Button? _repickBtn;
    // ⚠️ 2026-10-10 起这四颗（含 `_menuBtn`/`_faceBtn`）统一**可空** ——
    //   它们现在和 `_bgBtn`/`_tableBtn` 一起由 `AddKnob` 造出来，
    //   而造钮的那一刻可能在 `BuildScene` 之外被跳过（比如皮肤/资源异常），
    //   可空能让"用之前先判空"变成编译期强制，跟 `BringKnobsToFront` 的判空口径一致。
    private Button? _menuBtn;
    private Button? _faceBtn;      // 牌面切换（经典 / 尖塔 / 烫金）

    // ── 左上角那排小圆钮（2026-10-10）──────────────────────────
    // ★ 用户：「给斗地主加，德州的那两个按钮。然后这些按钮都移动到左上角在第一个角色
    //   面板信息的还有上面，适当缩小」。
    //   四颗：室内（换背景）/ 桌面（换台面）/ 牌面（换卡图）/ 收桌。
    //   全是德州同款圆钮（共用 DouSpireKnob），**不再是右上角那两颗大方按钮**。
    /// <summary>「室内」钮 —— 换整屏背景（套用德州那几个房间画面）。</summary>
    private Button? _bgBtn;
    /// <summary>「桌面」钮 —— 换桌台面材质。</summary>
    private Button? _tableBtn;
    /// <summary>「室内」钮下面那行小字（显示当前房间短名）。</summary>
    private Label? _bgCap;
    /// <summary>「桌面」钮下面那行小字（显示当前台面短名）。</summary>
    private Label? _tableCap;
    /// <summary>「牌面」钮下面那行小字。</summary>
    private Label? _faceCap;
    /// <summary>「收桌」钮下面那行小字。</summary>
    private Label? _closeCap;
    private Control? _bgLayer;            // 整屏背景层（换房间时整层换掉）
    private Control? _tableLayer;         // 桌面层（换台面时整层换掉）

    // 选人
    private readonly Label[] _pickName = new Label[4];
    private readonly TextureRect[] _pickPortrait = new TextureRect[4];
    private readonly HBoxContainer[] _pickRow = new HBoxContainer[4];   // 每个座位的选人行（按人数显隐）
    private readonly Label[] _pickTag = new Label[4];                   // 每行的方位标签（3/4 人文案不同）
    private Button? _pickSize3;                                         // 「三人桌」按钮
    private Button? _pickSize4;                                         // 「四人桌（2v2）」按钮
    private readonly Button?[] _pickLeft = new Button?[4];               // 选人行的 ◀（联机时要按席位显隐）
    private readonly Button?[] _pickRight = new Button?[4];              // 选人行的 ▶
    private readonly Label?[] _pickWho = new Label?[4];                  // 「我」/「队友」+ 他控的角色（联机才用）
    private Button? _pickStartBtn;                                       // 「开始打牌」（联机时兼作"我准备好了"）

    /// <summary>
    /// 联机选角：这一席点了「开始打牌」没有（**本地视角**）。
    /// 只是界面提示用的 —— 真正决定"开不开局"的是主机那边的 <c>DouSpireNet.SeatReady</c>。
    /// </summary>
    private readonly bool[] _pickReady = new bool[4];

    /// <summary>主机等队友点「开始打牌」等了多少秒（超时兜底用，见 <see cref="TickPickWait"/>）。</summary>
    private double _pickWait;

    /// <summary>上一次弹"和主机不同步"提示的时间（秒）—— 每分钟最多弹一次，别刷屏。</summary>
    private double _desyncToastAt;

    // ══════════════════════════ 开关桌 ══════════════════════════

    /// <param name="keepStake">
    /// true = **保留**调用方已经挂好的 <see cref="_stakePlayer"/>（篝火那条路要在 Open 之前挂）。
    /// false（默认）= 清成无赌注 —— 普通开桌（F9 / 主菜单）就是这个语义。
    /// </param>
    /// <param name="seats">
    /// 这一桌几席（用来读摆位表）。0 = 按设置里的 <see cref="DouSpireConfig.TableSize"/> 推。
    /// ★ 2026-10-10：联机桌恒 4 席（<see cref="DouSpireNet.Seats"/>），和设置里那档无关 ——
    ///   所以 <see cref="OpenForNet"/> 会显式传 4，别让联机桌按"设置里还是 3 人"去读 3 人摆位表
    ///   （那样对家的信息板会和上家叠住，2026-10-09 踩过一次）。
    /// </param>
    public static void Open(Logger logger, bool keepStake = false, int seats = 0)
    {
        if (IsOpen) return;
        // 普通开桌（F9 / 主菜单）恒为**无赌注**：赌注模式只能由篝火那条路挂上（见 OpenAtRestSite）。
        if (!keepStake) _stakePlayer = null;
        if (seats <= 0) seats = (int)DouSpireConfig.TableSize;
        var root = DouSpireUi.Root;
        if (root == null)
        {
            logger.Warn("DouSpire: 场景树还没准备好，开不了桌（稍后按 F9 再试）。");
            return;
        }
        // 每次开桌重新读图：美术改完图直接 F9 收/开就能看到，不用重启游戏
        DouSpireAssets.ClearCache();
        DouSpireVoice.ClearCache();   // 语音同理（新丢进来的 wav/ogg 收开桌就生效）
        DouSpireMusic.ClearCache();   // 音乐同理（新丢进来的歌，收开桌就生效，不用重启游戏）
        // 摆位也一样：改完 mods\DouSpire\assets\layout.txt，F9 收/开就生效
        // ⚠️ 必须传**当前人数** —— 2026-10-09 实测：四人局按默认 3 人读表，
        //    对家（座位2）被按"左边"摆，跟左上角的上家完全叠住（用户："信息面板还是重合"）。
        DouSpireLayout.Load(seats);
        _instance = new DouSpireTable();
        _instance._openSeats = seats;    // 建场景/算人数用它（`_game` 还没建好时也准）
        root.AddChild(_instance);
        DouSpireSfx.Open();
        DouSpireMusic.ReportOnce();   // 清点一遍各槽位找到了几首歌（看日志就知道丢的歌认没认出来）
        logger.Info($"DouSpire: 桌面已打开（{seats} 席）。");
    }

    /// <param name="silent">
    /// true = **"我这张桌子是被顶替的残骸"**（「按人数重开牌桌」专用）。
    /// 被回收时**不广播收桌、不动全局联机状态** —— 理由见 <see cref="_silentExit"/>。
    /// </param>
    public static void Close(Logger logger, bool silent = false)
    {
        // ★ 2026-10-13：**先摘单例、后判有效性**（和 21 点 / 德州同款加固）。
        //   原来开头是 `if (!IsOpen) return;` —— 单例状态一旦错乱（比如已被 QueueFree
        //   但字段还留着旧引用），这一句就会把收桌整个挡掉，人只能关游戏。
        //   现在：无论单例是什么状态，**音乐/音效一律收干净**，再尽力销毁实例。
        var inst = _instance;
        _instance = null;

        try { DouSpireSfx.Close(); } catch (System.Exception) { }

        if (inst is null || !GodotObject.IsInstanceValid(inst))
        {
            logger.Info("DouSpire: 没有开着的桌面，收桌按空操作处理（音效已一并停掉）。");
            return;
        }

        if (silent) inst._silentExit = true;
        try { inst.QueueFree(); }
        catch (System.Exception e) { logger.Warn($"DouSpire: 桌面销毁出错（已尽力收干净）: {e.Message}"); }
        logger.Info("DouSpire: 桌面已收起。");
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
    /// **篝火开桌**（游戏内的休息处点「斗地主」走这条）：把牌桌上的输赢接到玩家的真实金币上。
    ///
    /// 顺序有讲究：先 <see cref="Open"/>（它会把赌注模式清成 null），再挂上玩家 —— 反了就被清掉了。
    /// </summary>
    public static void OpenAtRestSite(Player player, Logger logger)
    {
        // ⚠️ **没钱就别开桌**（用户 2026-10-08：「如果我是零元就拒绝我斗地主」「不能让我白手起家」）。
        //    入口 DouSpireRestSiteOption.OnSelect 已经拦过一道，这里是兜底 ——
        //    别的路径（联机广播重放、调试入口）绕过来也走不进去。
        var mine = player ?? ResolveMyPlayer();
        if (mine is not null && SafeGold(mine) <= 0)
        {
            logger.Info("斗地主：身上 0 金币，拒绝开桌（没钱不赌）。");
            Entry.Toast("你身上一个金币都没有 —— 先去挣点钱再来斗地主。", "斗地主");
            return;
        }

        if (IsOpen) Close(logger);
        // ⚠️ **必须先挂赌注玩家再 Open** —— Open() 里会走 _Ready → InitCharacters()，
        //    那一步要靠 _stakePlayer 判断"用我 run 里的角色、不弹选人界面"。
        //    原来是 Open 完才挂，结果 InitCharacters 看到的永远是 null（真 bug，2026-10-08 修）。
        // ⚠️ 再兜一层：篝火那条路有时拿不到自己的 Player（联机客机 / 广播重放时 Owner 不是本机），
        //    读不到就**从 run 里自己解析**一遍 —— 否则赌注模式进不去、金币照旧按 1000。
        _stakePlayer = mine;
        Open(logger, keepStake: true);
        logger.Info($"斗地主：篝火开桌（赌注模式），本金金币 {_stakePlayer?.Gold.ToString() ?? "（没拿到我的 Player）"}。");
        WriteDiag($"篝火开桌：本金金币 {_stakePlayer?.Gold.ToString() ?? "（无）"}");
    }

    /// <summary>
    /// **我身上现在有多少金币**（读不到返回 null = 不确定，调用方别拦）。
    /// 供篝火入口在**开桌之前**问一句「这人还玩得起吗」。
    /// </summary>
    public static int? MyGoldNow()
    {
        try
        {
            var p = _stakePlayer ?? ResolveMyPlayer();
            return p is null ? null : SafeGold(p);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：读身上金币出错（当成有钱，不拦）: {e.Message}");
            return null;
        }
    }

    private static int SafeGold(Player p)
    {
        try { return p.Gold; }
        catch { return int.MaxValue; }   // 读不出来就当成有钱，宁可不拦也别误拦
    }

    /// <summary>
    /// 我自己的 <see cref="Creature"/> —— 只在赌注模式（篝火那局）才有。
    ///
    /// 用途：**第三方 mod 的角色靠哪一步换成自己的外观**。
    /// 那种 mod 的换装补丁挂在 <c>NCreature._Ready</c> 里，我们牌桌没有 <c>NCreature</c>，
    /// 于是只能照它的样子调一次 <c>CharacterModel.GenerateAnimator(骨骼, creature)</c> ——
    /// 而那个方法**不接受 null**（本体实现里会 <c>creature.GetHpPercentRemaining()</c> 直接炸），
    /// 所以必须给一个真的。本体角色用不到这个参数。
    /// 详见 <c>DouSpireActor.TryModVisualsHook</c>（private，注释在那边）。
    /// </summary>
    private static Creature? StakeCreature()
    {
        try { return _stakePlayer?.Creature; }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：拿不到自己的 Creature（mod 角色的换装补丁会跳过）: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 兜底解析「我自己」的 <see cref="Player"/>。
    ///
    /// 篝火那条路的 <c>Owner</c> 在联机下可能**不是本机玩家**（广播会在每台机器上重放一遍 OnSelect），
    /// 客机也可能拿不到自己的 Player。读不到就是不下注（牌局照开），但能读到就一定要读到 ——
    /// 否则「用我身上的金币当赌注」这条就废了。
    /// 和 <see cref="DouSpireCharacters.MyRunCharacter"/> 走同一套 API。
    /// </summary>
    private static Player? ResolveMyPlayer()
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            return state is null ? null : LocalContext.GetMe(state);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：兜底解析自己的 Player 失败（这一局不下注）: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// **联机开桌**（篝火 + 多人房走这条）：把桌子打开并挂上**我自己**的赌注玩家。
    ///
    /// 和 <see cref="OpenAtRestSite"/> 的区别：
    /// <list type="bullet">
    ///   <item>赌注玩家可能是 null（客机拿不到自己的 Player 时只能看牌不下注）。</item>
    ///   <item>不接着上一局（<c>_savedGame</c> 清掉）—— 联机永远从主机广播的种子开新局。</item>
    ///   <item>跳过选人界面 —— 三家是谁由 run 里的角色决定（主机广播）。</item>
    /// </list>
    /// 会在 <c>_Ready</c> 之后调 <see cref="BeginNetSession"/> 把牌局搭起来。
    /// </summary>
    public static void OpenForNet(Player? player, Logger logger)
    {
        // 同样先挂赌注玩家再开桌（InitCharacters 会读它；联机时它可能为 null，没关系）
        // 兜底解析同上：客机拿不到自己的 Player 时，从 run 里自己找一遍 —— 这样金币赌注也能生效。
        _stakePlayer = player ?? ResolveMyPlayer();
        // ★★ 2026-10-10：联机桌**恒 4 席**（`DouSpireNet.Seats`），摆位/建场景都按 4 来
        //   —— 别让设置里还停在"三人桌"时按 3 席读摆位表（对家会和上家叠住）。
        Open(logger, keepStake: true, seats: DouSpireNet.Seats);
        _savedGame = null;
        _savedChars = null;
        _savedActions = new string[4];
        try { _instance?.BeginNetSession(); }
        catch (System.Exception e)
        {
            logger.Warn($"斗地主：联机开桌失败（收桌）: {e}");
            Entry.Toast("联机开桌失败，看日志。", "DouSpire");
            Close(logger);
        }
    }

    /// <summary>
    /// 联机会话开起来 —— 定三家、摆牌、进"明牌"阶段。
    ///
    /// **可以重复调**：主机每重新开一次桌，<c>Gen</c> 就 +1，这里靠它判断"是新的一桌"，
    /// 于是关掉再重开会重新摆牌（不然客机会因为"已经初始化过"而停在旧牌局上）。
    /// </summary>
    private void BeginNetSession()
    {
        if (_netStarted && _netGen == DouSpireNet.Gen) return;
        _netStarted = true;
        _netGen = DouSpireNet.Gen;
        _netOn = true;
        _netHost = DouSpireNet.IsHost;
        DouSpireNet.Live = this;

        _chars = BuildNetChars();
        _picking = false;
        ShowPicking(false);
        _broken = false;

        // 客机还在等主机应答：先摆一副占位牌把桌子显示出来，牌局等 OpOpen 到了再重开
        if (DouSpireNet.OpenPending)
        {
            _netOpenWait = 0;
            Deal(System.Random.Shared.Next());
            ShowShowCards(false);
            _netWait = "已通知主机 —— 等大家都点了「斗地主」才发牌…";
            Refresh();
            _titleLabel.Text = "斗地主 —— 正在等主机开桌…";   // Refresh 之后设，不然会被标题冲掉
            Entry.Logger.Info("斗地主（联机）：已请求主机开桌，等应答。");
            return;
        }

        Entry.Logger.Info($"斗地主（联机）：我是 {(DouSpireNet.IsHost ? "主机" : "客机")}，"
                          + $"坐 {DouSpireNet.MySeat} 号位；三家 = "
                          + string.Join("、", _chars.Select(c => DouSpireCharacters.Name(c))));

        // ★★ 2026-10-09：**联机进桌先让每个人自己挑角色**（用户反复强调：
        //   「联机下，每个人可以自己选择自己要玩的角色」）。
        //   原来这里直接 `Deal(DouSpireNet.Seed)` 就开局了 —— 角色全按"你在 run 里是谁"钉死，
        //   玩家根本没有选的余地（用户原话："开始进入斗地主我们无法选人，是直接按我们的身份"）。
        //   现在：两端都进选人界面 → 各翻各的箭头（实时广播给对面看）→ 都点「开始打牌」
        //   主机才定种子发牌（`DouSpireNet.HostPickReady`）。
        ReturnToPickerForNet();
    }

    /// <summary>
    /// 联机：进 / 回选人界面 —— 各家自己挑自己要玩的角色。
    /// 「重新选人」按钮和进桌都走这里（全桌一起弹，见 <see cref="DouSpireNet.PickOpenAll"/>）。
    /// </summary>
    private void ReturnToPickerForNet()
    {
        DropRound();
        for (var s = 0; s < _pickReady.Length; s++) _pickReady[s] = false;
        DouSpireNet.ClearReady();
        _pickWait = 0;

        // 各席的默认角色：主机广播的池子下标（谁都没挑过就退回"你 run 里正在玩的那位"）
        _chars = BuildNetChars();
        if (DouSpireNet.CharIndexOfSeat(DouSpireNet.MySeat) < 0)
        {
            var mine = DouSpireCharacters.MyRunCharacter();
            if (mine is not null) _chars[MeSeat] = new DouSpireCharacters.SeatPick(mine, null);
        }

        _picking = true;
        ApplyPickerSize();
        ShowPicking(true);
        Entry.Logger.Info("斗地主（联机）：进选人界面 —— 各席 = "
                          + string.Join("、", _chars.Select(c => DouSpireCharacters.Name(c))));
    }

    /// <summary>联机：谁点了「重新选人」—— 主机广播过来，全桌一起弹选人界面。</summary>
    /// <param name="logLine">
    /// 写日志用；传 null = 用默认那句"队友点了「重新选人」"。
    /// ★ 2026-10-10 加这个参数：<see cref="DouSpireNet.HostAdmitLatecomer"/>（有人中途补进来）
    ///   也要把全桌拉回选人界面，但原因不是"重新选人" —— 日志里得说清楚。
    /// </param>
    internal void NetPickOpen(string? logLine = null)
    {
        if (!_netOn) return;
        Entry.Logger.Info(logLine ?? "斗地主（联机）：队友点了「重新选人」—— 全桌回选人界面。");
        ReturnToPickerForNet();
    }

    /// <summary>
    /// 现在是不是停在**选人界面**（牌还没发）。
    /// ★ 2026-10-10 加：给 <see cref="DouSpireNet.HostAdmitLatecomer"/> 判断
    ///   "现在补一个人进来安不安全" —— 只在选人阶段允许，牌发了就不硬塞。
    /// </summary>
    internal bool IsPicking => _picking;

    /// <summary>
    /// 主机在选人界面等队友点「开始打牌」。等太久（对面挂机 / 掉线 / 卡在别的界面）就**自己开**
    /// —— 绝不能因为一个人不点就把全桌永久卡在选人界面里。
    /// </summary>
    private void TickPickWait(double delta)
    {
        var need = PickUnreadyHumans();
        if (need == 0)
        {
            _pickWait = 0;
            UpdatePickCountdown();

            // ★★ 2026-10-10 修（用户实测："我们俩点了斗地主…结果是两个电脑，一直等队友"）：
            //   队友在选人界面**离桌**（或掉线被改判成电脑）以后，真人席就全齐了
            //   —— 但这里原来只是"把计时器清零再 return"，**没人去开局**，
            //   主机于是永久停在选人界面上（屏幕上是"我 + 两个电脑 + ✓已准备，等队友…"）。
            //   现在：真人全齐（need == 0）**且我自己已经点过「开始打牌」**→ 直接开局。
            //
            //   为什么这个条件不会误开：
            //     · 进选人界面时 `_pickReady` 全是 false ⇒ 只要我是真人，need ≥ 1，
            //       所以 `need == 0` 必然意味着"我已经点过了"。
            //     · 开局后 `BeginShowCards` 会把 `_picking` 置 false（见那条 `_picking = false;`），
            //       本函数跟着不再被调，不会重复开局。
            if (_picking && _pickReady[MeSeat])
            {
                Entry.Logger.Info("斗地主（联机）：真人席全齐了（有人离桌由电脑补），主机开局。");
                NetHostStartNewRound();
            }
            return;
        }

        _pickWait += delta;
        UpdatePickCountdown();
        if (_pickWait < PickWaitLimit()) return;
        _pickWait = 0;

        Entry.Logger.Warn($"斗地主（联机）：还差 {need} 个人没点「开始打牌」，等超时了 —— 主机直接开局。");
        Entry.Toast("队友一直没点「开始打牌」—— 先开了。", "斗地主");
        NetHostStartNewRound();
    }

    /// <summary>
    /// 客机侧：**只读数、不裁定** —— 真正"到点自动开局"是主机干的（见 <see cref="TickPickWait"/>）。
    /// 客机照同一个上限数一遍，屏幕上那行倒计时才和主机对得上。
    /// </summary>
    private void TickPickCountdown(double delta)
    {
        if (PickUnreadyHumans() == 0) { _pickWait = 0; UpdatePickCountdown(); return; }
        _pickWait += delta;
        UpdatePickCountdown();
    }

    /// <summary>还差几个**真人**没点「开始打牌」（电脑席不算 —— 它不用点）。</summary>
    private int PickUnreadyHumans()
    {
        var need = 0;
        for (var s = 0; s < SeatCount; s++)
            if (DouSpireNet.IsHumanSeat(DouSpireNet.ToGlobal(s)) && !_pickReady[s]) need++;
        return need;
    }

    /// <summary>
    /// 选人阶段等队友的上限（秒）。**和「出牌时间上限」共用同一个设置** ——
    /// 用户要的就是"选人也要限制时间"，一个滑条管两处，不用再多记一个数字。
    /// 设 0（不限时）时退回 60 秒兜底 —— 绝不能因为一个人不点就把全桌永久卡在选人界面里。
    /// </summary>
    private static double PickWaitLimit()
        => DouSpireConfig.TurnTimeLimit > 0 ? DouSpireConfig.TurnTimeLimit : 60.0;

    /// <summary>刷选人界面那行倒计时（队友没准备时才亮，见 <see cref="UpdatePickCountdown"/>）。</summary>
    private void UpdatePickCountdown()
    {
        if (_pickCountdown is null) return;

        var need = PickUnreadyHumans();
        if (!_netOn || need == 0)
        {
            _pickCountdown.Visible = false;
            return;
        }

        var left = Mathf.Max(0, Mathf.CeilToInt((float)(PickWaitLimit() - _pickWait)));
        _pickCountdown.Visible = true;

        // ★ 2026-10-10：**把"等的是谁"点出来**。
        //   用户实测的困惑就是这一条：屏幕上另外两行都写着「电脑」，
        //   这行只说"还差 1 个人没点「开始打牌」" —— 看着像在等一个不存在的人。
        var names = new List<string>();
        for (var s = 0; s < SeatCount; s++)
        {
            var g = DouSpireNet.ToGlobal(s);
            if (DouSpireNet.IsHumanSeat(g) && !_pickReady[s]) names.Add(DouSpireNet.NameOfSeat(g));
        }
        var who = names.Count > 0 ? "（" + string.Join("、", names) + "）" : "";
        _pickCountdown.Text = $"还差 {need} 个人没点「开始打牌」{who} · {left} 秒后自动开始";
    }

    /// <summary>联机：某个座位的角色换了（主机广播；<paramref name="globalSeat"/> 是全局座位号）。</summary>
    internal void NetPickSet(int globalSeat, int poolIndex)
    {
        if (!_netOn) return;
        var v = DouSpireNet.ToLocal(globalSeat);
        if (v < 0 || v >= _chars.Length) return;
        _chars[v] = DouSpireCharacters.PickAt(poolIndex);
        if (_picking) RefreshPicker();
        Entry.Logger.Info($"斗地主（联机）：{globalSeat} 号位选的角色 → "
                          + DouSpireCharacters.Name(_chars[v]));
    }

    /// <summary>联机：某个座位点了「开始打牌」（主机广播；其他人看到"XX ✓已选"）。</summary>
    internal void NetPickReady(int globalSeat)
    {
        if (!_netOn) return;
        var v = DouSpireNet.ToLocal(globalSeat);
        if (v < 0 || v >= _pickReady.Length) return;
        _pickReady[v] = true;

        if (v == MeSeat && _pickStartBtn is not null)
        {
            _pickStartBtn.Text = "✓ 已准备，等队友…";
            _pickStartBtn.Disabled = true;
        }
        if (_picking) RefreshPicker();

        if (v != MeSeat)
            Entry.Toast($"{DouSpireNet.NameOfSeat(globalSeat)} 已经选好了 —— 轮到你了。", "斗地主");
    }

    /// <summary>联机时的三家角色：按主机广播的池子下标取（各端池子一样，所以下标互认）。</summary>
    private DouSpireCharacters.SeatPick[] BuildNetChars()
    {
        var arr = new DouSpireCharacters.SeatPick[4];
        var pool = DouSpireCharacters.Pool;
        for (var v = 0; v < SeatCount; v++)
        {
            var g = DouSpireNet.ToGlobal(v);
            var pick = DouSpireCharacters.PickAt(DouSpireNet.CharIndexOfSeat(g));
            // 没定（编外角色 / 拿不到）→ 按全局座位号从池子里挑一个 —— 各端算出来一样
            if (pick.IsEmpty && pool.Count > 0) pick = pool[((g % pool.Count) + pool.Count) % pool.Count];
            arr[v] = pick;
        }
        return arr;
    }

    public static void Toggle(Logger logger)
    {
        if (IsOpen) Close(logger); else Open(logger);
    }

    /// <summary>
    /// 调试用：设了环境变量 <c>DOUSPIRE_DEBUG_OPEN=1</c> 时，主菜单一就绪就自动开桌。
    /// 这样不用手按 F9 也能拿到布局自检日志（自动化验证用）；不设变量时什么也不做。
    /// </summary>
    public static void DebugAutoOpen()
    {
        try
        {
            if (System.Environment.GetEnvironmentVariable("DOUSPIRE_DEBUG_OPEN") != "1") return;
            Callable.From(() => { if (!IsOpen) Open(Entry.Logger); }).CallDeferred();
        }
        catch (System.Exception) { }
    }

    // ══════════════════════════ 开局 ══════════════════════════

    /// <summary>三家名字（抢地主和正式开局都要用）—— **本地视角**：0 号位是我。</summary>
    private string[] SeatNames()
    {
        if (_netOn)
        {
            var a = new string[SeatCount];
            for (var v = 0; v < SeatCount; v++)
            {
                var g = DouSpireNet.ToGlobal(v);
                a[v] = g == DouSpireNet.MySeat ? "我" : DouSpireNet.NameOfSeat(g);
            }
            return a;
        }
        return SeatNameList();
    }

    /// <summary>座位名兜底：没分配到角色时显示方位。4 人 = 我/下家/对家/上家。</summary>
    private string[] SeatNameList()
    {
        var fallback = new[] { "我", "下家", "对家", "上家" };
        var a = new string[SeatCount];
        for (var s = 0; s < SeatCount; s++)
        {
            if (s == 0) { a[0] = "我"; continue; }
            a[s] = _chars[s].IsEmpty ? fallback[s] : DouSpireCharacters.Name(_chars[s]);
        }
        return a;
    }

    /// <summary>
    /// 三家名字 —— **全局视角**（只有建牌局时用）。
    ///
    /// ⚠️ 必须用全局顺序喂给 <see cref="GameState.NewGame"/>：它是**按座位号切牌**的，
    /// 换个顺序发牌结果就变了 —— 各端拿到的牌就对不上了。视角靠
    /// <see cref="GameState.ViewFrom"/> 旋转，不靠换名字顺序。
    /// </summary>
    private string[] GlobalNames()
    {
        if (!_netOn) return SeatNames();
        var a = new string[SeatCount];
        for (var g = 0; g < SeatCount; g++)
            a[g] = g == DouSpireNet.MySeat ? "我" : DouSpireNet.NameOfSeat(g);
        return a;
    }

    /// <summary>
    /// 建一副牌局 —— **所有人都是同一副**（同一个种子 + 同一个全局名字顺序），
    /// 然后按我的全局座位号旋转成"我坐 0 号位"的本地视角。
    /// 4 人（2v2）：<paramref name="globalLandlordSeat"/> 复用为**先手座位**
    /// （没有地主，猜拳赢者所在队先手、赢者先出）。
    /// </summary>
    private GameState BuildView(int seed, int globalLandlordSeat)
    {
        // ★★ 2026-10-10：4 人档现在**联机也走**（联机 = 4 席，见 DouSpireNet.Seats 的注释）。
        //   用户：「多人联机每[人]看到的应该是 本人 / 客机名 ×N，不足四人用电脑补齐」。
        //   判断只看"这张桌子几个人"，不再排掉联机。
        var fourP = LocalSeats == 4;
        var global = fourP
            ? GameState.NewGame(seed, GlobalNames(), firstSeat: globalLandlordSeat, playerCount: 4)
            : GameState.NewGame(seed, GlobalNames(), landlordSeat: globalLandlordSeat);
        return _netOn ? global.ViewFrom(DouSpireNet.MySeat) : global;
    }

    /// <summary>
    /// 这一桌**一共几席** —— 联机 = `DouSpireNet.Seats`（4，含电脑补位），单人 = 设置里那档。
    /// 和 <see cref="SeatCount"/> 的区别：这个**不读 `_game`**，建牌局那一刻就准。
    /// </summary>
    private int LocalSeats => _openSeats > 0
        ? _openSeats
        : _netOn ? DouSpireNet.Seats : (int)DouSpireConfig.TableSize;

    /// <summary>本地视角的第几号位是不是真人（false = AI 补位）。单人恒 true。</summary>
    private bool IsHumanLocal(int localSeat)
        => !_netOn || DouSpireNet.IsHumanSeat(DouSpireNet.ToGlobal(localSeat));

    /// <summary>
    /// 开新一局 —— **先进抢地主**，抢完才真正开局（用户 2026-10-07："加入抢地主玩法"）。
    /// </summary>
    /// <summary>
    /// 开新一局 —— **先问明牌**（底注翻倍），再进抢地主。
    /// 用户 2026-10-07："明牌在最前面。若选择明牌底注会翻倍。然后是抢地主，抢完问你加不加倍。"
    /// </summary>
    private void Deal(int? seed = null)
    {
        _dealSeed = seed ?? System.Random.Shared.Next();
        _redealCount = 0;
        BeginShowCards();
    }

    /// <summary>第一步：问我要不要明牌（得先看见自己的牌）。</summary>
    private void BeginShowCards()
    {
        _askingShow = true;
        _showCards = false;
        _bidding = false;
        _askingDouble = false;
        _discarding = false;      // 新一局：扣牌状态清掉（2v2 会在 StartRound 里重新进）
        ShowDiscardBtn(false);
        _multiplier = 1;
        _moneyPaidOut = false;
        ResetMoney();          // 每局重新发筹码（赌注模式下按我身上的真实金币发）
        _roundStarted = false;
        _netWait = "";
        _netAskWait = 0;
        _netAskSeat = -1;
        // 清**全部 4 格** —— 不能用 SeatCount：它此刻还在看**上一局**的 _game，
        // 换人数重开（3↔4）时会把没清到的格子留成脏状态
        for (var s = 0; s < _showDone.Length; s++) { _showDone[s] = false; _showCardsSeat[s] = false; }

        // ⚠️ 单人 AI 明牌决策挪到了下面 BuildView **之后**（2026-10-09 升级）：
        //    要看真手牌才能决定明不明 —— 原来这里的"无脑全明"删掉了。

        _lastAction = new string[4];
        _selected.Clear();
        _picking = false;
        ShowPicking(false);

        // 先把这一副牌摆上（我要看着自己的手牌决定明不明）
        try
        {
            _game = BuildView(_dealSeed, _netOn ? 0 : 1);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：明牌前摆牌失败，直接进猜拳: {e.Message}");
            _askingShow = false;
            BeginRps(_dealSeed);
            return;
        }

        // ★ 单人 AI 明牌决策（2026-10-09 升级）—— **看手牌定**，不再无脑全明：
        //    牌分 ≥5（有炸弹/王炸/大牌成堆）才明（翻倍补偿），差牌 12% 概率诈一炮。
        //    ⚠️ `_showDone` 一格都不能少 —— 漏了 AllDone 永远为假、明牌阶段死锁（2026-10-08 的坑）。
        if (!_netOn)
        {
            var shown = 0;
            for (var s = 1; s < (int)DouSpireConfig.TableSize; s++)
            {
                var want = AiWantsShow(s);
                _showCardsSeat[s] = want;
                _showDone[s] = true;
                if (want) { _multiplier *= 2; shown++; }
            }
            if (shown > 0) Entry.Logger.Info($"斗地主：AI 明牌 {shown} 家，倍数 → {_multiplier}");
        }

        _lastCurrentSeat = -1;
        _endSfxDone = false;
        _lordAnimSeat = -1;
        _watchCurrent = -1;
        _watchSelected = -1;
        _watchLastPlay = null;
        _lastNetTurn = -1;
        _remoteWait = 0;
        for (var i = 0; i < SeatCount; i++) _watchHand[i] = 0;

        WhaleReact(MeSeat, DouSpireCharacters.Mood.Deal, 2.0);
        DouSpireSfx.Deal();
        ShowShowCards(true);
        Refresh();

        // AI 的明牌决定：只有主机广播（放最后 —— 万一 AI 全明就凑齐了也不会把刚摆好的按钮冲掉）
        if (_netOn && _netHost) HostDecideAiShow();
    }

    /// <summary>我表态：明牌（底注 ×2）/ 不明牌。然后进抢地主。</summary>
    private void ShowCardsSay(bool want)
    {
        if (!_askingShow) return;

        if (_netOn && !_netHost)
        {
            DouSpireNet.SendShow(want);
            ShowShowCards(false);
            _netWait = "等你对面表态…";
            Refresh();
            return;
        }
        if (_netOn) DouSpireNet.CastShow(DouSpireNet.MySeat, want);
        ApplyShow(MeSeat, want);
    }

    /// <summary>
    /// 某一席表态明牌/不明牌（本地视角）。**所有端都走这里** —— 各端看到的是同一串决定，
    /// 所以倍数和后续阶段天然一致。
    /// </summary>
    private void ApplyShow(int localSeat, bool want)
    {
        if (localSeat < 0 || localSeat >= SeatCount) return;
        if (_showDone[localSeat]) return;
        _showDone[localSeat] = true;
        _showCardsSeat[localSeat] = want;
        if (want)
        {
            _multiplier *= 2;
            Entry.Logger.Info($"斗地主：{localSeat} 号位明牌 → 倍数 ×2 = {_multiplier}");
            if (localSeat == MeSeat) PopMultiplier("明牌");
            DouSpireMusic.Play(DouSpireMusicCue.Climax);   // 明牌也是翻倍时刻，配高潮曲
        }
        // 动作：明牌 = 亮个相（蓄力）；不明牌 = 原地不动（别为"不干什么"演一段）
        // 2026-10-09：蓄力不光有动作，胸口还要亮一下（用户："每个角色出形态的时候不是也有特效吗"）
        if (want)
        {
            ActorPlay(localSeat, a => a.Play(DouSpireActor.CastAnim));
            SeatVfx(localSeat, "vfx/common/vfx_common_clean_flare_1", 1.2f);
        }
        if (!AllDone(_showDone)) return;

        _askingShow = false;
        _showCards = _showCardsSeat[MeSeat];
        ShowShowCards(false);
        BeginRps(_dealSeed);
    }

    /// <summary>主机替 AI 表态明牌 —— 2026-10-09 起按手牌分决定（同一把尺子 <see cref="AiWantsShow"/>）。</summary>
    private void HostDecideAiShow()
    {
        for (var s = 0; s < SeatCount; s++)
        {
            if (IsHumanLocal(s)) continue;
            var want = AiWantsShow(s);
            DouSpireNet.CastShow(DouSpireNet.ToGlobal(s), want);
            ApplyShow(s, want);
        }
    }

    /// <summary>
    /// AI 的明牌决策 —— **看手牌**（用户 2026-10-09："大概牌稍微好一点就明牌，不好就不明牌"）。
    /// 尺子 = <see cref="GameState.LandlordScore"/>（炸弹 +6、王炸 +8、2/王/A 加分）：
    /// ≥5 分（有炸弹或大牌成堆）就明 —— 翻倍补偿 + 吓人；差牌也有 12% 概率诈一炮，不死板。
    /// ⚠️ 必须在 <c>_game</c> 建好之后调；地主的手要**扣掉底牌**再算（明牌时底牌还没亮）。
    /// </summary>
    private bool AiWantsShow(int seat)
    {
        try
        {
            var hand = _game.Players[seat].Hand;
            if (_game.Players[seat].IsLandlord)
                hand = hand.Where(c => !_game.Bottom.Contains(c)).ToList();
            var score = GameState.LandlordScore(hand);
            var want = score >= 5 || System.Random.Shared.Next(100) < 12;
            Entry.Logger.Info($"斗地主：{seat} 号 AI 手牌分 {score} → {(want ? "明牌" : "不明牌")}");
            return want;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：明牌决策算分失败，默认不明（不影响玩）: {e.Message}");
            return false;
        }
    }

    // ⚠️ 必须**只看实际座位数**（2026-10-09 用户实测抓到的卡死）：表态数组固定开 4 格，
    // 三人局永远空着第 4 格 —— 按 Length 判，三人局明牌点完也"永远没表态完"，
    // BeginRps 永远不执行 → 明牌按钮点不动、整局卡死。数组是给四人局扩的，判断不能跟着扩。
    private bool AllDone(bool[] flags)
    {
        for (var s = 0; s < SeatCount; s++) if (!flags[s]) return false;
        return true;
    }

    /// <summary>明牌阶段的按钮开关（跟抢地主那套一个路子）。</summary>
    private void ShowShowCards(bool on)
    {
        if (_showYes != null) _showYes.Visible = on;
        if (_showNo != null) _showNo.Visible = on;
        _playBtn.Visible = !on;
        _passBtn.Visible = !on;
        _hintBtn.Visible = !on;
        if (on)
        {
            _nextBtn.Visible = false;
            if (_repickBtn != null) _repickBtn.Visible = false;
        }
    }

    // ══════════════════════════ 猜拳抢地主 ══════════════════════════
    // 2026-10-07 换掉原来"轮流表态"那套。用户原话：
    //   "我们不用抢地主。我们用游戏自带的石头剪刀布。" / "你可以选要不要石头剪刀布，不选就不当。"
    // 一局：① 问你要不要抢（不抢 = 直接当农民）→ ② 要抢的人一起出拳 →
    //      ③ 赢的当地主；平局重出；谁都不抢就换一副牌重发。
    // 手势图直接借游戏联机那套（按角色分，见 DouSpireAssets.Hand）。

    // ══════════════ 明牌（抢地主**之前**问）══════════════
    //  用户 2026-10-07："明牌在最前面。若选择明牌底注会翻倍。然后是抢地主，抢完问你加不加倍。"
    private bool _askingShow;             // 正在问"要不要明牌"
    private bool _showCards;              // 我选了明牌
    private Button? _showYes;
    private Button? _showNo;
    /// <summary>每一席明没明牌 —— 明了牌的那家，手牌要摊开给大家看。</summary>
    private readonly bool[] _showCardsSeat = new bool[4];
    /// <summary>摊牌用的行（每席一个，放在各自 HUD 下面）。</summary>
    private readonly HBoxContainer[] _showRow = new HBoxContainer[4];
    private int _dealSeed;                // 这一局的牌种子（明牌 → 抢地主 都用它）
    private int _multiplier = 1;          // 本局倍数（明牌 ×2、各家加倍、炸弹都会乘上来）

    // ══════════════ 加倍（抢完地主后问）══════════════
    //  用户 2026-10-07："抢完问你加不加倍"。
    private bool _askingDouble;
    private Button? _doubleYes;
    private Button? _doubleNo;

    // ── 2v2 扣牌阶段（开局后、出牌前）──
    /// <summary>后手队扣牌进行中。扣完（PendingDiscards 清空）才许出牌。</summary>
    private bool _discarding;
    /// <summary>「扣这张」按钮 —— 我在后手队时显示，选中恰好一张牌后点它。</summary>
    private Button? _discardBtn;

    // ── 鸡煲代打（2026-10-09）──
    /// <summary>
    /// 「鸡煲代打」开关：开着 = AI 替「我」打完**全程**（明牌 / 抢地主 / 猜拳 / 加倍 /
    /// 扣牌 / 出牌全包），用当前难度最高的那套脑子（设置里是 DouZero狠，四人局自动退规则 AI）。
    /// **故意用 static** —— 收桌重开（切人数 / F9）也不忘，"一直托管"。
    /// </summary>
    private static bool _autoPilot;
    /// <summary>「鸡煲代打」按钮（跟出牌那排同一个样式，随时能点）。</summary>
    private Button? _autoBtn;

    private bool _bidding;                // 猜拳抢地主进行中（沿用旧名字，改动最小）
    private Button? _bidYes;              // 「抢地主（猜拳）」
    private Button? _bidNo;               // 「不抢」
    private Button? _rpsRock, _rpsScissors, _rpsPaper;   // 出拳的三个按钮
    private int _bidSeed;                 // 这一副牌的种子，猜完还用同一副开局
    private int _bidHolder = -1;          // 猜拳赢家 = 地主座位
    private readonly int[] _bidScore = new int[4];
    private RpsStep _rpsStep = RpsStep.Off;
    private readonly bool[] _rpsWant = new bool[4];       // 要不要参与猜拳
    private readonly int[] _rpsThrow = new int[4];        // 0 石头 / 1 剪刀 / 2 布，-1 还没出
    private readonly bool[] _rpsAlive = new bool[4];      // 平局重出时还留在场上的
    private double _rpsTimer;
    private double _rpsSwingT;                            // "摆手"动画的相位
    private int _rpsCycle;                                // 摆到第几手（用来让手不停地换）
    private double _rpsPickT;                             // 我还没点按钮时的等待秒数
    private Control? _rpsPanel;                           // 中央那块"亮拳"面板
    private readonly Label[] _rpsName = new Label[4];
    private readonly TextureRect[] _rpsArt = new TextureRect[4];

    /// <summary>猜拳走到哪一步了。Throw = 等/摆，Swing = 出了拳、还在摆，Reveal = 定格亮了。</summary>
    private enum RpsStep { Off, Want, Throw, Swing, Reveal }

    /// <summary>手势的中文名：0 石头 / 1 剪刀 / 2 布。</summary>
    private static string GestureName(int g) => g switch { 0 => "石头", 1 => "剪刀", 2 => "布", _ => "？" };

    /// <summary>a 能不能赢 b（石头赢剪刀、剪刀赢布、布赢石头）。</summary>
    private static bool Beats(int a, int b)
        => (a == 0 && b == 1) || (a == 1 && b == 2) || (a == 2 && b == 0);

    private void BeginRps(int seed)
    {
        _bidSeed = seed;
        _bidHolder = -1;
        _rpsTimer = DouSpireConfig.AiDelay;
        _netWait = "";
        _netAskWait = 0;
        _netAskSeat = -1;
        // 清全部 4 格（理由同 BeginShowCards：此刻 SeatCount 可能还是上一局的）
        for (var s = 0; s < _rpsWant.Length; s++)
        {
            _rpsWant[s] = false;
            _rpsThrow[s] = -1;
            _rpsAlive[s] = false;
            _wantDone[s] = false;
            _throwDone[s] = false;
        }
        _bidding = true;

        // 先拿这一副牌看看三家各多少分（固定让 0 号位当地主，这样"我"永远是 17 张，看得准）
        try
        {
            var preview = BuildView(seed, _netOn ? 0 : 1);
            for (var s = 0; s < SeatCount; s++)
            {
                var hand = preview.Players[s].Hand;
                if (preview.Players[s].IsLandlord)
                    hand = hand.Where(c => !preview.Bottom.Contains(c)).ToList();
                _bidScore[s] = GameState.LandlordScore(hand);
            }
            _game = preview;      // 桌上先摆这一副牌，玩家得看着自己的牌决定抢不抢
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：猜拳前算分失败，直接按手牌评分定地主: {e.Message}");
            _bidding = false;
            StartRound(seed, -1);
            return;
        }

        _lastAction = new string[4];
        _selected.Clear();
        _aiTimer = DouSpireConfig.AiDelay;
        _picking = false;
        ShowPicking(false);
        _lastCurrentSeat = -1;
        _endSfxDone = false;
        _lordAnimSeat = -1;
        _watchCurrent = -1;
        _watchSelected = -1;
        for (var i = 0; i < SeatCount; i++) _watchHand[i] = 0;
        // 出拳按钮贴我自己的手势图（换角色后重开一局就跟着换）
        var meEntry = DouSpireCharacters.HandEntry(_chars[MeSeat]);
        if (_rpsRock != null) _rpsRock.Icon = DouSpireAssets.Hand(meEntry, 0);
        if (_rpsScissors != null) _rpsScissors.Icon = DouSpireAssets.Hand(meEntry, 1);
        if (_rpsPaper != null) _rpsPaper.Icon = DouSpireAssets.Hand(meEntry, 2);
        Entry.Logger.Info($"斗地主：猜拳手势图（{meEntry}）"
                          + $"{(DouSpireAssets.Hand(meEntry, 0) != null ? "读到" : "没读到，按钮退回纯文字")}。");
        // 发牌：**只让自己那一席开口**（三家一起说太吵）。
        // 音效也不叠了 —— 以前"抽牌声 + 角色说话"同时响，糊成一团（用户 2026-10-07："挤在一起了。不好听"）
        WhaleReact(MeSeat, DouSpireCharacters.Mood.Deal, 2.0);
        ShowRps(RpsStep.Want);
        Refresh();

        // AI 的意向：主机定了就广播（放最后，理由同 BeginShowCards）
        if (_netOn && _netHost) HostDecideAiWant();
    }

    /// <summary>我表态：抢（进猜拳）/ 不抢（直接当农民）。AI 的意向这时候也一起定下来。</summary>
    private void RpsWantSay(bool want)
    {
        if (!_bidding || _rpsStep != RpsStep.Want) return;

        // 我自己的音效/动作立刻给，不等网络
        if (!DouSpireVoice.ReactBid(_chars[MeSeat].GuestId, want, MeSeat))
        {
            if (want) { DouSpireSfx.Landlord(); DouSpireSfx.VoiceBid(); }
            else { DouSpireSfx.Pass(); DouSpireSfx.VoicePass(); }
        }

        if (_netOn && !_netHost)
        {
            DouSpireNet.SendWant(want);
            LockRpsButtons("等你对面表态…");
            Refresh();
            return;
        }

        if (_netOn) DouSpireNet.CastWant(DouSpireNet.MySeat, want);

        if (!_netOn)
        {
            // 单人：AI 的意向本地算 —— 手牌分接近全场最高就抢，另外留一点随机，别每局都三家一起抢。
            var top = Mathf.Max(_bidScore[0], Mathf.Max(_bidScore[1], _bidScore[2]));
            for (var s = 1; s < SeatCount; s++)
                ApplyWant(s, _bidScore[s] >= top - 2 || System.Random.Shared.Next(100) < 35);
        }

        ApplyWant(MeSeat, want);
    }

    /// <summary>
    /// 某一席表态抢/不抢（本地视角）。**所有端都走这里** —— 各端看到同一串决定，
    /// 所以"谁进了猜拳""三家都不抢"这些判断天然一致。
    /// </summary>
    private void ApplyWant(int localSeat, bool want)
    {
        if (localSeat < 0 || localSeat >= SeatCount) return;
        if (_wantDone[localSeat]) return;
        _wantDone[localSeat] = true;
        _rpsWant[localSeat] = want;
        if (localSeat != MeSeat) DouSpireVoice.ReactBid(_chars[localSeat].GuestId, want, localSeat);
        _lastAction[localSeat] = want ? "抢地主" : "不抢";
        // 动作：抢 = 蓄力亮相（胸口亮一下）；不抢 = 原地待着
        if (want)
        {
            ActorPlay(localSeat, a => a.Play(DouSpireActor.CastAnim));
            SeatVfx(localSeat, "vfx/common/vfx_common_clean_flare_1", 1.2f);
        }
        if (!AllDone(_wantDone)) return;

        for (var s = 0; s < SeatCount; s++) _rpsAlive[s] = _rpsWant[s];

        if (!_rpsWant.Any(x => x))
        {
            _redealCount++;
            Entry.Logger.Info($"斗地主：三家都不抢（第 {_redealCount} 次），换一副牌重发。");
            PopText("三家都不抢 —— 换一副牌", DouSpireUi.Gold, 4);

            if (_redealCount >= 6)
            {
                // 兜底：一直没人抢就按手牌分强行定一个地主，绝不无限换牌
                var best = 0;
                for (var s = 1; s < SeatCount; s++) if (_bidScore[s] > _bidScore[best]) best = s;
                Entry.Logger.Warn($"斗地主：连着 {_redealCount} 把没人抢，强行按手牌分定 {best} 号位当地主。");
                _bidHolder = best;
                BeginDouble();
                return;
            }

            var ns = System.Random.Shared.Next();
            if (_netOn)
            {
                // 种子由主机定并广播 —— 客机等 ActNewRound
                if (_netHost) { DouSpireNet.CastNewRound(ns); NetNewRound(ns); }
            }
            else BeginRps(ns);
            return;
        }

        _redealCount = 0;
        BeginThrow();
    }

    /// <summary>主机替 AI 表态抢不抢。</summary>
    private void HostDecideAiWant()
    {
        var top = Mathf.Max(_bidScore[0], Mathf.Max(_bidScore[1], _bidScore[2]));
        for (var s = 0; s < SeatCount; s++)
        {
            if (IsHumanLocal(s)) continue;
            var want = _bidScore[s] >= top - 2 || System.Random.Shared.Next(100) < 35;
            DouSpireNet.CastWant(DouSpireNet.ToGlobal(s), want);
            ApplyWant(s, want);
        }
    }

    /// <summary>开一轮出拳：**先让桌中央那几只手摆起来**，等大家各出一手。</summary>
    private void BeginThrow()
    {
        for (var s = 0; s < SeatCount; s++) { _rpsThrow[s] = -1; _throwDone[s] = !_rpsAlive[s]; }
        _rpsSwingT = 0;
        _rpsCycle = 0;
        _rpsPickT = 0;
        _netWait = "";

        if (_rpsAlive[MeSeat])
        {
            ShowRps(RpsStep.Throw);      // 等我点按钮
        }
        else
        {
            _rpsTimer = 1.6;             // 我不参与 → 直接看他们摆
            ShowRps(RpsStep.Swing);
        }

        // AI 的拳：主机**立刻**定下来并广播（不定的话"等所有人的拳到齐"那道闸永远开不了）
        if (_netOn && _netHost) HostDecideAiThrow();
        Refresh();
    }

    /// <summary>
    /// 我出拳：0 石头 / 1 剪刀 / 2 布。
    /// **只记下来，先不亮** —— 要跟对手同一刻亮，不然我先出了就被对手看光。
    /// </summary>
    private void RpsThrowSay(int gesture)
    {
        if (!_bidding || _rpsStep != RpsStep.Throw) return;
        if (!_rpsAlive[MeSeat] || _rpsThrow[MeSeat] >= 0) return;

        if (_netOn && !_netHost)
        {
            DouSpireNet.SendThrow(gesture);
            _rpsPickT = -1;
            _rpsTimer = 0.9;
            _rpsSwingT = 0;
            ShowRps(RpsStep.Swing);
            LockRpsButtons("出拳了 —— 等别人…");
            Refresh();
            return;
        }
        if (_netOn) DouSpireNet.CastThrow(DouSpireNet.MySeat, gesture);
        ApplyThrow(MeSeat, gesture);
    }

    /// <summary>某一席出了拳（本地视角）。</summary>
    private void ApplyThrow(int localSeat, int gesture)
    {
        if (localSeat < 0 || localSeat >= SeatCount) return;
        if (!_rpsAlive[localSeat] || _rpsThrow[localSeat] >= 0) return;
        _rpsThrow[localSeat] = gesture;
        _throwDone[localSeat] = true;

        if (localSeat == MeSeat)
        {
            _rpsPickT = -1;
            _rpsSwingT = 0;
            _rpsTimer = 0.9;                 // 出完再摆一拍（"石头…剪刀…布！"）就一起定格
            ShowRps(RpsStep.Swing);
        }
        else if (_rpsStep == RpsStep.Throw && AllThrowDone())
        {
            _rpsSwingT = 0;
            _rpsTimer = 0.9;
            ShowRps(RpsStep.Swing);
        }
        Refresh();
    }

    /// <summary>主机替 AI 出拳。</summary>
    private void HostDecideAiThrow()
    {
        for (var s = 0; s < SeatCount; s++)
        {
            if (!_rpsAlive[s] || IsHumanLocal(s)) continue;
            var g = System.Random.Shared.Next(3);
            DouSpireNet.CastThrow(DouSpireNet.ToGlobal(s), g);
            ApplyThrow(s, g);
        }
    }

    /// <summary>场上还在的人是不是都出拳了（联机时这是"定格"的闸门 —— 少一个人的拳就不能亮）。</summary>
    private bool AllThrowDone()
    {
        for (var s = 0; s < SeatCount; s++)
        {
            if (!_rpsAlive[s]) continue;
            if (!_throwDone[s]) return false;
        }
        return true;
    }

    /// <summary>猜拳的节奏：摆手动画、AI 迟迟不出、定格、算胜负、平局重出，都在这里推。</summary>
    private void StepRps(double delta)
    {
        if (!_bidding) return;

        switch (_rpsStep)
        {
            case RpsStep.Throw:
                TickSwing(delta);
                _rpsPickT += delta;
                if (_rpsPickT > 12.0)          // 我迟迟不点 → 随机替我出一手，绝不卡在猜拳上
                {
                    // 联机时这一句会自动走"客机上报 / 主机广播"那条路（见 RpsThrowSay）
                    RpsThrowSay(System.Random.Shared.Next(3));
                }
                return;

            case RpsStep.Swing:
                TickSwing(delta);
                // 联机：**场上每个人的拳都到齐了才起表** —— 少一个人的拳就定格会亮出错的手
                if (_netOn && !AllThrowDone()) return;
                // 联机：**定格由主机下令**（ActRpsPhase=Freeze），客机不自己起表。
                // 客机自己起表的话会永远慢主机一个 RTT：主机已经进了下一拍、把新的拳发出来了，
                // 客机还停在定格里，那几条消息会被下面 ApplyThrow 的 `_rpsThrow >= 0` 当"重复"丢掉
                // —— 平局重出时必然死锁（2026-10-08 自检抓到）。
                if (_netOn && !_netHost) return;
                _rpsTimer -= delta;
                if (_rpsTimer > 0) return;
                if (_netOn) DouSpireNet.CastRpsPhase(0, -1);
                BeginFreeze();
                return;

            case RpsStep.Reveal:
                // 联机：下一阶段（平局重出 / 定完地主进加倍）同样由主机下令
                if (_netOn && !_netHost) return;
                _rpsTimer -= delta;
                if (_rpsTimer > 0) return;
                if (_netOn)
                {
                    if (ResolveRps())
                    {
                        DouSpireNet.CastRpsPhase(2, DouSpireNet.ToGlobal(_bidHolder));
                        EndRps();
                    }
                    else
                    {
                        PopText("平局 —— 再出一拳", DouSpireUi.Gold, 4);
                        DouSpireNet.CastRpsPhase(1, -1);
                        BeginThrow();
                    }
                    return;
                }
                if (ResolveRps()) EndRps();
                else
                {
                    PopText("平局 —— 再出一拳", DouSpireUi.Gold, 4);
                    BeginThrow();
                }
                return;
        }
    }

    /// <summary>
    /// 摆手动画 —— 手不停地在 石头→剪刀→布 之间切，跟真人出拳前晃手一样。
    /// 这也是"看不到 AI 出什么"的关键：**这一刻谁都还没定手势**。
    /// 节奏 0.20s 一次 —— 原来 0.11s 切得太快，眼睛跟不上，像抽帧不像"摇"。
    ///（用户 2026-10-07："你这个手速度切换的太快了。官方那边是一直在摇然后出"）
    /// </summary>
    private void TickSwing(double delta)
    {
        _rpsSwingT += delta;
        var step = (int)(_rpsSwingT / 0.20);
        if (step == _rpsCycle) return;
        _rpsCycle = step;
        PaintRpsHands();
    }

    /// <summary>
    /// 定格。**到这一刻才去定 AI 出什么** —— 之前它们只是空手在摆，
    /// 所以我没法提前偷看（用户 2026-10-07："ai已经事先出来，我可以看他出的出啊"）。
    /// </summary>
    private void BeginFreeze()
    {
        // 单人：AI 的拳到这一刻才定（之前它们只是空手在摆，所以我没法提前偷看）。
        // 联机：所有拳都是主机广播来的，到这一步必然已经齐了（上面那道闸门保证的），不用也不该再随机。
        if (!_netOn)
        {
            for (var s = 0; s < SeatCount; s++)
                if (_rpsAlive[s] && _rpsThrow[s] < 0)
                    _rpsThrow[s] = System.Random.Shared.Next(3);
        }

        // 亮拳那一刻：出拳的人各摆一个出招动作（用户 2026-10-09："想看到更多的动作效果"）
        for (var s = 0; s < SeatCount; s++)
            if (_rpsAlive[s]) ActorPlay(s, a => a.PlayAttack());

        _rpsTimer = 0.9;
        ShowRps(RpsStep.Reveal);
        PaintRpsHands();
        DouSpireSfx.Select();
        Refresh();
    }

    /// <summary>
    /// 数一数谁赢了。出了唯一赢家就写进 <see cref="_bidHolder"/> 并返回 true；平局返回 false。
    /// 平局 = 全场手势全同、或者三种手势凑齐（互相克制），或者赢家不止一个 —— 这三种都得重出。
    /// </summary>
    private bool ResolveRps()
    {
        var alive = new List<int>();
        for (var s = 0; s < SeatCount; s++) if (_rpsAlive[s]) alive.Add(s);
        if (alive.Count == 1) { _bidHolder = alive[0]; return true; }

        var kinds = new List<int>();
        foreach (var s in alive) if (!kinds.Contains(_rpsThrow[s])) kinds.Add(_rpsThrow[s]);
        if (kinds.Count != 2) return false;     // 全同 / 三种都有 → 平

        var win = Beats(kinds[0], kinds[1]) ? kinds[0] : kinds[1];
        var winners = alive.Where(s => _rpsThrow[s] == win).ToList();
        if (winners.Count == 1) { _bidHolder = winners[0]; return true; }

        // 两个人出了同一把赢拳 → 他俩之间再平，接着重出
        for (var s = 0; s < SeatCount; s++) _rpsAlive[s] = winners.Contains(s);
        return false;
    }

    /// <summary>猜拳完了：赢家当地主 → 接着问加倍。</summary>
    private void EndRps()
    {
        ShowRps(RpsStep.Off);
        _bidding = false;

        if (_bidHolder < 0)     // 兜底：真出不来赢家就换一副牌，绝不卡在这儿
        {
            Entry.Logger.Warn("斗地主：猜拳没定出地主，换一副牌重发。");
            var ns = System.Random.Shared.Next();
            if (_netOn) { if (_netHost) { DouSpireNet.CastNewRound(ns); NetNewRound(ns); } }
            else BeginRps(ns);
            return;
        }

        var names = SeatNames();
        if (_game.Players.Count == 4)
        {
            Entry.Logger.Info($"斗地主：猜拳定先手 —— {names[_bidHolder]}（本地 {_bidHolder} 号位）先出");
            PopText($"{names[_bidHolder]} 先出！（他那队先手）", DouSpireUi.Gold, 4);
        }
        else
        {
            Entry.Logger.Info($"斗地主：猜拳定地主 —— {names[_bidHolder]}（本地 {_bidHolder} 号位）");
            PopText($"{names[_bidHolder]} 抢到地主！", DouSpireUi.Gold, 4);
        }
        DouSpireSfx.Landlord();
        // ★ 2026-10-09：加冕光线 —— 抢到地主/先手的人胸口打一束光（用户要看"光线"）
        SeatVfx(_bidHolder, "vfx/common/vfx_common_ray", 1.4f);
        DouSpireMusic.Play(DouSpireMusicCue.Climax);   // 抢到地主 = 开局高潮，插一段
        BeginDouble();
    }

    // ══════════════ 加倍 ══════════════

    /// <summary>猜拳定完地主：**先让 AI 各自决定，再问我要不要加倍**。</summary>
    private void BeginDouble()
    {
        _netWait = "";
        _netAskWait = 0;
        _netAskSeat = -1;
        for (var s = 0; s < _doubleDone.Length; s++) _doubleDone[s] = false;

        // AI 的决定：**手牌分高就加倍；分一般也经常加倍**。
        //（用户 2026-10-07："ai为什么不加倍" —— 原来卡死在"分 ≥ 12"这一条上，
        //  分数达不到就永远不加。）
        // 单人：这里本地算；联机：主机算完广播（见 HostDecideAiDouble）。
        if (!_netOn)
        {
            for (var s = 0; s < SeatCount; s++)
            {
                if (s == MeSeat) continue;
                var want = _bidScore[s] >= 10 || System.Random.Shared.Next(100) < 65;
                ApplyDouble(s, want);
            }
        }

        _askingDouble = true;
        _bidding = false;
        ShowRps(RpsStep.Off);
        ShowDouble(true);
        Refresh();

        if (_netOn && _netHost) HostDecideAiDouble();
    }

    /// <summary>我表态：加倍 / 不加倍。然后正式开局。</summary>
    private void DoubleSay(bool want)
    {
        if (!_askingDouble) return;

        if (_netOn && !_netHost)
        {
            DouSpireNet.SendDouble(want);
            ShowDouble(false);
            _netWait = "等你对面表态…";
            Refresh();
            return;
        }
        if (_netOn) DouSpireNet.CastDouble(DouSpireNet.MySeat, want);
        ApplyDouble(MeSeat, want);
    }

    /// <summary>某一席表态加倍/不加倍（本地视角）。</summary>
    private void ApplyDouble(int localSeat, bool want)
    {
        if (localSeat < 0 || localSeat >= _doubleDone.Length) return;
        if (_doubleDone[localSeat]) return;
        _doubleDone[localSeat] = true;
        if (want)
        {
            _multiplier *= 2;
            Entry.Logger.Info($"斗地主：{_game.Players[localSeat].Name} 加倍"
                              + $"（手牌分 {_bidScore[localSeat]}）→ 倍数 {_multiplier}");
            if (localSeat == MeSeat) PopMultiplier("加倍");
            DouSpireMusic.Play(DouSpireMusicCue.Climax);   // 加倍也是高潮时刻
            ActorPlay(localSeat, a => a.Play(DouSpireActor.CastAnim));   // 加倍 = 蓄力亮相
            SeatVfx(localSeat, "vfx/common/vfx_common_clean_flare_1", 1.2f);   // 胸口亮一下
        }
        if (!AllDone(_doubleDone)) return;

        _askingDouble = false;
        ShowDouble(false);
        // 正式开局：**只有主机发号施令**（ActStart），客机照着开 —— 免得两端差一拍
        if (_netOn)
        {
            if (_netHost)
            {
                DouSpireNet.CastStart(DouSpireNet.ToGlobal(_bidHolder));
                NetStart(_bidHolder);
            }
        }
        else StartRound(_bidSeed, _bidHolder);
    }

    /// <summary>主机替 AI 表态加倍。</summary>
    private void HostDecideAiDouble()
    {
        for (var s = 0; s < SeatCount; s++)
        {
            if (IsHumanLocal(s)) continue;
            var want = _bidScore[s] >= 10 || System.Random.Shared.Next(100) < 65;
            DouSpireNet.CastDouble(DouSpireNet.ToGlobal(s), want);
            ApplyDouble(s, want);
        }
    }

    /// <summary>加倍阶段的按钮开关。</summary>
    private void ShowDouble(bool on)
    {
        if (_doubleYes != null) _doubleYes.Visible = on;
        if (_doubleNo != null) _doubleNo.Visible = on;
        _playBtn.Visible = !on;
        _passBtn.Visible = !on;
        _hintBtn.Visible = !on;
        if (on)
        {
            _nextBtn.Visible = false;
            if (_repickBtn != null) _repickBtn.Visible = false;
        }
    }

    /// <summary>猜拳阶段的按钮换班：出牌那三个跟「抢/不抢」「石头/剪刀/布」对调。</summary>
    private void ShowRps(RpsStep step)
    {
        _rpsStep = step;
        var want = step == RpsStep.Want;
        // 只有"我也在场上"的时候才给我出拳按钮 —— 我没参与就别摆
        var throwin = step == RpsStep.Throw && _rpsAlive[MeSeat];
        if (_bidYes != null) _bidYes.Visible = want;
        if (_bidNo != null) _bidNo.Visible = want;
        if (_rpsRock != null) _rpsRock.Visible = throwin;
        if (_rpsScissors != null) _rpsScissors.Visible = throwin;
        if (_rpsPaper != null) _rpsPaper.Visible = throwin;
        if (_rpsPanel != null)
            _rpsPanel.Visible = step is RpsStep.Throw or RpsStep.Swing or RpsStep.Reveal;
        var play = step == RpsStep.Off;
        _playBtn.Visible = play;
        _passBtn.Visible = play;
        _hintBtn.Visible = play;
        if (!play)
        {
            _nextBtn.Visible = false;
            if (_repickBtn != null) _repickBtn.Visible = false;
        }
    }

    /// <summary>画中央那几只手：摆动时手不停换，定格后亮真手。</summary>
    private void PaintRpsHands()
    {
        if (_rpsPanel is null) return;
        var names = SeatNames();
        for (var s = 0; s < SeatCount; s++)
        {
            // 编外角色没有自己的手 → 一家借一个（不然三家全撞成摄政王的手）
            var entry = DouSpireCharacters.HandEntry(_chars[s]);
            var who = s == MeSeat ? "我" : names[s];
            string label;
            Texture2D? tex;
            var scale = 1f;

            if (!_rpsAlive[s])
            {
                label = who + "　不抢";
                tex = DouSpireAssets.Hand(entry, -1);        // 举"指"的手＝这轮不参与
            }
            else if (_rpsStep == RpsStep.Reveal && _rpsThrow[s] >= 0)
            {
                label = who + "　" + GestureName(_rpsThrow[s]);
                tex = DouSpireAssets.Hand(entry, _rpsThrow[s]);
                scale = 1.12f;                                // 定格时轻轻一“顿”
            }
            else
            {
                label = who;
                // 摆手：各家错开一格，看着更像真的在晃
                tex = DouSpireAssets.Hand(entry, (_rpsCycle + s) % 3);
                scale = 1f + 0.08f * Mathf.Sin((float)(_rpsSwingT * 11.0));
            }

            _rpsName[s].Text = label;
            _rpsName[s].AddThemeColorOverride("font_color", DouSpireCharacters.ColorOf(_chars[s]));
            _rpsArt[s].Texture = tex;
            _rpsArt[s].Scale = new Vector2(scale, scale);
        }
    }

    /// <summary>
    /// 客机在等主机回话时，把这一阶段的按钮全撤掉（防手快点两下）。
    /// 中央横幅走 <see cref="_netWait"/> —— <see cref="RefreshRpsUi"/> 会优先显示它。
    /// </summary>
    private void LockRpsButtons(string msg)
    {
        if (_bidYes != null) _bidYes.Visible = false;
        if (_bidNo != null) _bidNo.Visible = false;
        if (_rpsRock != null) _rpsRock.Visible = false;
        if (_rpsScissors != null) _rpsScissors.Visible = false;
        if (_rpsPaper != null) _rpsPaper.Visible = false;
        _netWait = msg;
    }

    /// <summary>明牌 / 猜拳 / 加倍阶段的临时界面：中央给提示，底牌扣着不亮。</summary>
    private void RefreshRpsUi()
    {
        var names = SeatNames();
        _centerLabel.Text = !string.IsNullOrEmpty(_netWait)
            ? _netWait
            : _askingShow
            ? "要明牌吗？—— 亮出你的手牌，底注翻倍（×2）"
            : _askingDouble
            ? $"要加倍吗？—— 当前倍数 ×{_multiplier}"
            : _rpsStep == RpsStep.Want
            ? "抢地主 —— 要抢的人一起猜拳；不抢的直接当农民"
            : _rpsStep == RpsStep.Throw
            ? "出拳！—— 石头 / 剪刀 / 布"
            : _rpsStep == RpsStep.Swing
            ? "石头… 剪刀… 布！"
            : _rpsStep == RpsStep.Reveal
            ? "亮拳！"
            : _bidHolder >= 0 ? $"{names[_bidHolder]} 抢到地主！" : "";

        if (_rpsStep is RpsStep.Throw or RpsStep.Swing or RpsStep.Reveal) PaintRpsHands();

        // 猜拳 / 明牌阶段谁都不该看见那三张底牌
        foreach (var ch in _bottomRow.GetChildren().ToArray())
        {
            _bottomRow.RemoveChild(ch);
            ch.QueueFree();
        }
    }

    /// <summary>
    /// 背景音乐跟着局势走（用户："斗地主好像是有两种的，开局比较舒缓，
    /// 后面有人牌少了或者王炸了，突然就切音乐了"）：
    /// 平时那首舒缓的；**有人只剩 ≤2 张、或者场上正摆着炸弹**，就换紧张那首。
    ///
    /// 2026-10-08：改成按**时机**分槽（见 <see cref="DouSpireMusic"/>）——
    /// 高潮曲（炸弹 / 王炸 / 加倍）是**临时插播**，放几秒自己退回来。
    /// 所以这里必须先放它一马，否则每帧都会被局势曲盖掉、高潮根本听不见。
    /// </summary>
    private void UpdateMusic()
    {
        // 高潮曲的退场由 _Process 里的 DouSpireMusic.Tick() 负责（这里不跑就没人跑）
        if (DouSpireMusic.ClimaxActive) return;     // 高潮期间不抢它的话筒

        if (_bidding) { DouSpireMusic.Play(DouSpireMusicCue.Table); return; }

        var tense = _game.Winner is null
                    && (_game.Players.Any(p => p.Hand.Count <= 2) || IsBomb(_game.LastPlay));
        DouSpireMusic.Play(tense ? DouSpireMusicCue.Tense : DouSpireMusicCue.Table);
    }

    /// <summary>
    /// 抢完了，正式开始这一局（底牌归地主）。
    ///
    /// <paramref name="landlordSeat"/> 是**本地视角**的座位号；联机时换算成全局座位号去建牌局
    /// （见 <see cref="BuildView"/>）。
    /// </summary>
    private void StartRound(int seed, int landlordSeat)
    {
        // 双保险：无论从哪里进来，都要退出"明牌 / 猜拳 / 加倍"状态、把出牌按钮换回来
        _bidding = false;
        _askingShow = false;
        _askingDouble = false;
        _netWait = "";
        _roundStarted = true;
        ShowRps(RpsStep.Off);
        ShowShowCards(false);
        ShowDouble(false);

        _game = BuildView(seed, _netOn ? DouSpireNet.ToGlobal(Mathf.Max(0, landlordSeat)) : landlordSeat);
        _lastAction = new string[4];
        _selected.Clear();
        _aiTimer = DouSpireConfig.AiDelay;
        _savedGame = _netOn ? null : _game;     // 联机不存档（下次开桌由主机广播重开）
        _savedMultiplier = _multiplier;
        _savedActions = _lastAction;
        _picking = false;
        // 音效：**只留定地主那一声**。发牌声在明牌阶段已经响过，这里再响就叠了。
        DouSpireSfx.Landlord();
        _lastCurrentSeat = -1;
        _endSfxDone = false;
        _lordAnimSeat = -1;
        _watchCurrent = -1;
        _watchSelected = -1;
        _watchLastPlay = null;
        _lastNetTurn = -1;
        _remoteWait = 0;
        for (var i = 0; i < SeatCount; i++) _watchHand[i] = 0;
        // 发牌：**只让自己那一席开口**（三家一起说太吵）
        WhaleReact(MeSeat, DouSpireCharacters.Mood.Deal, 2.0);
        ShowPicking(false);
        Refresh();

        var lord = landlordSeat >= 0 && landlordSeat < SeatCount ? _game.Players[landlordSeat].Name : "？";
        if (_game.Players.Count == 4)
            Entry.Logger.Info($"斗地主：正式开局 —— 先出 {lord}（本地 {landlordSeat} 号位），"
                              + $"倍数 ×{_multiplier}。");
        else
            Entry.Logger.Info($"斗地主：正式开局 —— 地主 {lord}（本地 {landlordSeat} 号位），"
                              + $"倍数 ×{_multiplier}"
                              + (_netOn ? $"；联机 {(_netHost ? "主机" : "客机")}，我坐 {DouSpireNet.MySeat} 号位。" : "。"));

        // 2v2：后手队每家扣一张，扣完才开打（内核里 PendingDiscards 不清空 TryPlay 直接报错）
        // ★ 2026-10-10：联机 4 席也走这段（主机驱动，见 StepDiscard / NetDiscard）。
        if (_game.Players.Count == 4 && _game.PendingDiscards.Count > 0)
            BeginDiscard();
    }

    // ══════════════ 2v2 扣牌阶段 ══════════════

    /// <summary>开局扣牌：后手队每家扣一张（摊开）。AI 自动扣，我点牌 + 按按钮。</summary>
    private void BeginDiscard()
    {
        _discarding = true;
        _aiTimer = DouSpireConfig.AiDelay;
        _discardWait = 0;                 // 新一阶段：远程等待计时清零
        ShowDiscardBtn(_game.PendingDiscards.Contains(MeSeat));
        Entry.Logger.Info("斗地主（2v2）：扣牌阶段 —— 待扣座位 "
                          + string.Join("、", _game.PendingDiscards));
        PopText("后手队各扣一张牌", DouSpireUi.Gold, 3);
        Refresh();
    }

    /// <summary>扣牌按钮显示开关（轮到我扣时才亮）。</summary>
    private void ShowDiscardBtn(bool on)
    {
        if (_discardBtn != null)
        {
            _discardBtn.Visible = on;
            _discardBtn.Disabled = !on;
        }
    }

    /// <summary>扣牌阶段每帧：AI 席按节奏自动扣；我那席等按钮。
    /// ★ 2026-10-10：联机时**只有主机**跑这里（主机替 AI 席扣 + 广播；真人席的扣牌由
    ///   `DiscardSay` 上报后经 `NetDiscard` 落地）。客机 `_Process` 里直接跳过这个阶段。</summary>
    private void StepDiscard(double delta)
    {
        if (!_discarding) return;
        if (_netOn && !_netHost) return;                 // 客机不自己推进扣牌 —— 等主机广播
        if (_game is null) { EndDiscard(); return; }     // 事故第七条：碰 _game 前先判 null
        if (_game.PendingDiscards.Count == 0) { EndDiscard(); return; }

        var s = _game.PendingDiscards[0];

        // 我这一席：等按钮；没开「鸡煲代打」就干等（自己点），开了就 AI 替我扣。
        var mineWaiting = s == MeSeat && !_autoPilot;

        // 别人那一席（联机真人）：等他自己上报；掉线 / 挂机等太久 → 主机替他扣，绝不卡死。
        //
        // ★★ 2026-10-11 修 P0（用户实测："队友不扣牌 → 我这桌一直出不了牌"）：
        //   这段计时**原来写在 `_aiTimer` 门控之后** —— 而 `_aiTimer` 每 `AiDelay` 秒才放行一帧，
        //   于是 `_discardWait` 每秒只涨 `delta / AiDelay`（≈0.014 秒），
        //   30 秒的兜底实际要等**半个多小时**，等于根本没有兜底 ⇒ 主机永远停在扣牌阶段。
        //   现在把计时提到门控**之前**、每帧累加 —— 15 秒一到真的会替扣。
        //   顺带每 5 秒报一次"还在等谁"，下次再卡住，看一眼日志就知道卡在谁身上。
        var overdue = false;
        if (!mineWaiting && _netOn && s != MeSeat && IsHumanLocal(s))
        {
            _discardWait += delta;
            _discardLogWait += delta;
            if (_discardLogWait >= 5.0)
            {
                _discardLogWait = 0;
                Entry.Logger.Info($"斗地主（2v2）：还在等 {s} 号位（{_game.Players[s].Name}）扣牌"
                                  + $" —— 已等 {_discardWait:0} 秒（上限 {DiscardWaitLimit:0} 秒）。");
            }
            if (_discardWait < DiscardWaitLimit) return;
            _discardWait = 0;
            _discardLogWait = 0;
            overdue = true;                                  // 超时兜底：立刻替他扣，不再等节奏
            Entry.Logger.Info($"斗地主（2v2）：{_game.Players[s].Name} 太久没扣牌，主机替他扣。");
        }
        else
        {
            _discardWait = 0;
            _discardLogWait = 0;
            if (mineWaiting) return;                         // 我自己那一席：老老实实等按钮
        }

        // AI 席 / 开了代打的我 / 远程真人超时兜底 —— 都走这里落地。
        if (!overdue)
        {
            _aiTimer -= delta;
            if (_aiTimer > 0) return;
            _aiTimer = DouSpireConfig.AiDelay;
        }

        var card = RuleAi.ChooseDiscard(_game.Players[s].Hand);
        // 主机 / 单人：定牌 → 落地（ApplyDiscard 内部已写日志；广播由这里补发）
        if (!ApplyDiscard(s, card, out var err))
            Entry.Logger.Warn($"斗地主（2v2）：扣牌失败 —— {err}");
        else if (_netOn)
            DouSpireNet.CastDiscard(DouSpireNet.ToGlobal(s), card);
    }

    /// <summary>我点「扣这张」：必须恰好选中一张。</summary>
    private void DiscardSay()
    {
        if (!_discarding || !_game.PendingDiscards.Contains(MeSeat)) return;
        if (_selected.Count != 1) { PopText("先点一张牌（只扣一张）", DouSpireUi.Dim, 2); return; }

        var card = _selected.First();

        // ★ 2026-10-10：联机客机**只上报**，等主机校验 + 广播回来再真正落地（同出牌口径）。
        if (_netOn && !_netHost)
        {
            DouSpireNet.SendDiscard(card);
            _selected.Clear();
            ShowDiscardBtn(false);           // 等主机广播前先收起来，防连点
            _statusLabel.Text = "扣牌中…（等主机确认）";
            Entry.Logger.Info($"斗地主（2v2）：我上报扣 {GameState.CardName(card)}（等主机确认）");
            return;
        }

        if (!ApplyDiscard(MeSeat, card, out var err)) { PopText(err, DouSpireUi.Gold, 2); return; }
        _selected.Clear();
        if (_netOn) DouSpireNet.CastDiscard(DouSpireNet.MySeat, card);   // 主机自己扣 → 广播
    }

    /// <summary>两家都扣完 → 正常开打。</summary>
    private void EndDiscard()
    {
        _discarding = false;
        ShowDiscardBtn(false);
        _aiTimer = DouSpireConfig.AiDelay;
        PopText("扣牌完毕 —— 开打！", DouSpireUi.Gold, 2);
        Entry.Logger.Info("斗地主（2v2）：扣牌完毕，正式开打。");
        Refresh();
    }

    /// <summary>「鸡煲代打」开关：点按钮切换。开着时 <see cref="TickAutoPilot"/> 接管我的全部表态。</summary>
    private void ToggleAutoPilot()
    {
        _autoPilot = !_autoPilot;
        if (_autoBtn != null) _autoBtn.Text = _autoPilot ? "✓ 鸡煲代打中" : "鸡煲代打";
        PopText(_autoPilot ? "鸡煲代打 —— AI 替你打到底" : "鸡煲代打关了 —— 自己动手",
            DouSpireUi.Gold, 3);
        Entry.Logger.Info($"斗地主：鸡煲代打 {(_autoPilot ? "开" : "关")}。");
        Refresh();
    }

    /// <summary>
    /// 鸡煲代打每帧 tick：替「我」回答**问话阶段**（明牌 / 抢地主 / 猜拳出拳 / 加倍）。
    /// 扣牌阶段在 <see cref="StepDiscard"/> 里放行、出牌阶段在主循环的 MeSeat 闸口放行 ——
    /// 那两处本来就有现成的 AI 节奏，借它们的道，不另起炉灶。
    /// 决策全部照抄 AI 自己的口径：AI 明牌我就明牌、AI 抢我就抢、AI 加倍我就加倍。
    /// </summary>
    private void TickAutoPilot()
    {
        if (_broken || _game is null) return;

        if (_askingShow)
        {
            if (!_showDone[MeSeat]) ShowCardsSay(AiWantsShow(MeSeat));   // 2026-10-09：跟着手牌分走，不再无脑跟明
            return;
        }

        if (_bidding)
        {
            if (_rpsStep == RpsStep.Want && !_wantDone[MeSeat])
            {
                // 跟 HostDecideAiWant / 单人抢地主同一个口径：分够高或手气来了就抢
                var top = 0;
                for (var s = 0; s < SeatCount; s++) top = Mathf.Max(top, _bidScore[s]);
                RpsWantSay(_bidScore[MeSeat] >= top - 2 || System.Random.Shared.Next(100) < 35);
            }
            else if (_rpsStep == RpsStep.Throw && _rpsAlive[MeSeat] && _rpsThrow[MeSeat] < 0)
            {
                RpsThrowSay(System.Random.Shared.Next(3));   // 出拳没技术含量 → 随机
            }
            return;
        }

        if (_askingDouble)
        {
            if (!_doubleDone[MeSeat])
                DoubleSay(_bidScore[MeSeat] >= 10 || System.Random.Shared.Next(100) < 65);
            return;
        }
        // 扣牌 / 出牌：StepDiscard 和主循环里放行（见那两处的 !_autoPilot）
    }

    // 客机代打：记"这一席已经上报过一手了"，等主机的广播（Current 会走掉）再重置。
    private int _netAutoSeat = -1;
    private double _netAutoWait;

    /// <summary>
    /// **客机上的「鸡煲代打」** —— 只做一件事：轮到我时，把 AI 替我选好的牌**上报**给主机。
    ///
    /// 为什么不能复用 <see cref="StepAi"/>：那个函数会 `ApplyPlay()` **本地落地**。
    /// 客机的 `_game` 只能由主机的广播推进（各端自己推 = 各走各的，必炸）。
    /// 所以这条通路只 `SendPlay` / `SendPass`，出了牌就停手等广播。
    /// </summary>
    private void TickAutoPilotOnline(double delta)
    {
        if (_broken || _game is null) return;

        // 问话阶段（明牌 / 抢地主 / 加倍）与扣牌阶段由 TickAutoPilot / StepDiscard 管，
        // 这里只负责"该我出牌"的那一下。
        if (_askingShow || _askingDouble || _bidding || _discarding
            || _game.Winner is not null || _game.Current != MeSeat)
        {
            _netAutoSeat = -1;
            _netAutoWait = 0;
            return;
        }

        _netAutoWait += delta;

        // ★ 2026-10-10 断线兜底（第二道保险）：线已经断了就**别再重报**了。
        //   正常路径上 `NetLinkGuard` 已经先一步收桌（`_broken` 会让本函数在开头就 return），
        //   但万一它没来得及（比如 `_picking` 分支、或守卫判断的时序差一帧），
        //   代打也绝不能无限重报 —— 用户的原话：「报几次就放弃，而不是无限重试」。
        if (DouSpireNet.LinkLost) { _netAutoSeat = -1; _netAutoWait = 0; return; }

        // 已经上报过一手了 → 等主机的广播（Current 一变上面就重置了）。
        // 主机 12 秒还没回音（丢包 / 被拒）才补报一次 —— 绝不每一帧刷屏。
        if (_netAutoSeat == MeSeat && _netAutoWait < 12.0) return;
        if (_netAutoWait < DouSpireConfig.AiDelay) return;

        _netAutoSeat = MeSeat;
        _netAutoWait = 0;

        var move = DouZeroAi.Choose(_game, MeSeat) ?? RuleAi.Choose(_game);
        if (move is null || move.Count == 0)
        {
            if (_game.MustLead)
            {
                // 领出时"要不起"是非法动作 → 甩最小的一张（和 StepAi 同款兜底）
                var fallback = new List<Card> { _game.Me.Hand.OrderBy(c => c.Rank).First() };
                Entry.Logger.Info("斗地主：鸡煲代打（客机）上报 1 张兜底牌。");
                DouSpireNet.SendPlay(fallback);
                return;
            }
            Entry.Logger.Info("斗地主：鸡煲代打（客机）上报「要不起」。");
            DouSpireNet.SendPass();
            return;
        }

        Entry.Logger.Info($"斗地主：鸡煲代打（客机）上报一手 {move.Count} 张。");
        DouSpireNet.SendPlay(move);
    }

    private static void DropRound()
    {
        _savedGame = null;
        _savedActions = new string[4];
    }

    public override void _Ready()
    {
        SetMeta(DouSpireUi.MetaTag, true);
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 200;
        MouseFilter = MouseFilterEnum.Stop;

        // ⚠️ 别改成只用 SetAnchorsPreset：把 Control 挂在 **Window 根**下时，
        // 引擎算"父级可锚定矩形"用的那把尺子在这套环境里对不上（实测 UI 全挤到左上角），
        // 所以这里自己量、自己设尺寸。
        FitToViewport();
        DouSpireUi.UseScreen(this);
        Diagnose();

        // ⚠️ 必须最先定：InitCharacters 要靠它决定"三家是谁"（联机用 run 里的角色，单人抽签）
        // OpenPending = 我是客机、已经请求开桌、还在等主机应答（这时候也要按联机走，别弹选人界面）
        _netOn = DouSpireNet.Active || DouSpireNet.OpenPending;
        _netHost = DouSpireNet.IsHost;
        if (_netOn) DouSpireNet.Live = this;

        // ★★ 2026-10-09 修：`Open()` 里那句 `DouSpireLayout.Load(设置人数)` 是在**还不知道是不是
        //   联机**的时候跑的；联机桌恒 3 席，于是 3 人局会拿 4 人局的坐标去摆 ——
        //   多出来的第 4 席坐标上就挂着一张没人回收的立绘（用户实测："还有旧的静态贴图"）。
        //   这里知道 `_netOn` 了，按**本桌真实席位数**再读一次摆位表。
        DouSpireLayout.Load(SeatCount);

        try
        {
            // ⚠️ 必须在 BuildScene **之前**：立绘是搭场景那一步就挂上去的。
            // 晚一步 _chars 还是空数组 → SeatArt(null) 直接返回 null → 三家立绘一个都不生成。
            // 这就是"画好的角色图为什么不放上去"的真凶。
            InitCharacters();
            BuildScene();
            DouSpireUi.StyleFont(this);
            DouSpireAssets.ReportOnce();
            DouSpireAssets.ReportSpireOnce();   // 尖塔牌面那 15 张读到没有，写一行日志
            DouSpireAssets.ReportGildedOnce();  // 烫金牌面入库了几张（用户正在一张张画，这条就是答案）
            RestoreOrPick();
        }
        catch (System.Exception e)
        {
            _broken = true;
            Entry.Logger.Error($"DouSpire 桌面搭建失败，已自动收桌: {e}");
            Entry.Toast($"斗地主桌面起不来：{e.Message}", "DouSpire");
            CallDeferred(MethodName.QueueFree);
        }
    }

    // ══════════════════════════ 贴满窗口 ══════════════════════════

    /// <summary>
    /// 把这一层对齐到视口的可见矩形 —— **不靠锚点**。
    ///
    /// 为什么：`Control` 直接挂在 **Window 根**下时，"父级可锚定矩形"是引擎按
    /// 视口算的，实测在这套环境里它给出的尺寸和真正的可见区域对不上，
    /// 结果整棵 UI 全挤在左上角（见 2026-10-06 的截图）。
    /// 这里改成：**等锚点（左右上下都 0，让 Size 成为权威）+ 显式 Position/Size**，
    /// 尺寸从视口直接量。窗口尺寸一变就重量一次。
    /// </summary>
    private void FitToViewport()
    {
        var r = ViewportRect();
        if (r.Size.X < 8f || r.Size.Y < 8f) return;          // 量不到就别乱动
        if (Size == r.Size && Position == r.Position) return; // 已经对了，别来回设（免得递归）
        AnchorLeft = AnchorTop = AnchorRight = AnchorBottom = 0f;
        Position = r.Position;
        Size = r.Size;
    }

    /// <summary>视口的可见矩形：取几个来源里"最像话"的那个。</summary>
    private Rect2 ViewportRect()
    {
        // 优先级：视口可见矩形 → 窗口本身尺寸 → DisplayServer
        try
        {
            var vp = GetViewport();
            if (vp != null)
            {
                var r = vp.GetVisibleRect();
                if (r.Size.X >= 8f && r.Size.Y >= 8f) return r;
            }
        }
        catch (System.Exception) { }
        try
        {
            var win = GetWindow();
            if (win != null)
            {
                var s = win.Size;
                if (s.X >= 8 && s.Y >= 8) return new Rect2(Vector2.Zero, s);
            }
        }
        catch (System.Exception) { }
        try
        {
            var s = DisplayServer.WindowGetSize();
            if (s.X >= 8 && s.Y >= 8) return new Rect2(Vector2.Zero, s);
        }
        catch (System.Exception) { }
        return new Rect2(0, 0, 1920, 1080);
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        // 窗口/视口尺寸变了就重量一次（FitToViewport 内部会判重，不会递归）
        if (what == (int)NotificationResized || what == (int)NotificationWMSizeChanged) FitToViewport();
    }

    /// <summary>布局自检：把真实的 rect 写进日志（怀疑量错时看这个）。</summary>
    private async void Diagnose()
    {
        try
        {
            await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
            LogRects("0.35s");
            await ToSignal(GetTree().CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);
            LogRects("1.75s");
        }
        catch (System.Exception) { }
    }

    private void LogRects(string when)
    {
        if (!GodotObject.IsInstanceValid(this)) return;
        var vp = GetViewport();
        var vis = vp?.GetVisibleRect() ?? new Rect2();
        var win = GetWindow();
        var sb = new System.Text.StringBuilder();
        sb.Append($"布局自检({when}) 可见矩形={vis} 窗口尺寸={(win != null ? win.Size.ToString() : "?")} ");
        sb.Append($"DisplayServer={DisplayServer.WindowGetSize()} ");
        if (win != null) sb.Append($"内容缩放模式={win.ContentScaleMode} 比例={win.ContentScaleAspect} 基准={win.ContentScaleSize} ");
        sb.Append($"本层rect={GetRect()} 锚点=({AnchorLeft:0.##},{AnchorTop:0.##},{AnchorRight:0.##},{AnchorBottom:0.##}) ");
        sb.Append($"父={(GetParent()?.GetClass() ?? "null")} ");
        if (_scene != null) sb.Append($"_scene={_scene.GetRect()} ");
        if (_handRow != null) sb.Append($"手牌行={_handRow.GetRect()} 子数={_handRow.GetChildCount()} ");
        if (_hud[0] != null) sb.Append($"HUD0={_hud[0].GetRect()} ");
        if (_picker != null) sb.Append($"_picker={_picker.GetRect()} ");
        sb.Append($"Screen={DouSpireUi.Screen:0.###} UiScale={DouSpireConfig.UiScale:0.##} Px(96)={DouSpireUi.Px(96):0.#}");
        Entry.Logger.Info(sb.ToString());
    }

    /// <summary>
    /// 开局前先把"三家是谁"定下来 —— **必须在 <see cref="BuildScene"/> 之前调**。
    ///
    /// 立绘是搭场景那一步挂上去的，晚一步这里还是 null，<see cref="DouSpireCharacters.SeatArt"/>
    /// 拿到 null 直接返回 → 三家立绘一个都不生成。有上一局存着的角色就接着用，否则随机发三个
    /// （用户要求「抽到哪几个角色就用哪几个 UI」）。
    /// </summary>
    private void InitCharacters()
    {
        // ★★ 我这一席 = **我 run 里正在玩的那个角色** —— 单人和联机都要成立。
        //   用户 2026-10-08：「我不应该选角色。我现在使用的是 mod 角色卡芙卡…
        //   人家有自己的立绘，还有自己的攻击和出招动作特效。我要求以后即使加入别的 mod 角色
        //   也能直接应用他的立绘和动作」。
        //   mod 角色（卡芙卡 / 酒狐）同样在 ModelDb.AllCharacters 里，所以天然认得。
        //
        //   ⚠️⚠️ 这段**必须在 `_netOn` 分支之前**。原来是联机先 return → 我这一席的角色
        //   被主机广播的池子下标覆盖 → 用户实测看到的是**铁甲战士**（真 bug，2026-10-08）。
        //   联机时**只改我自己这一席**，另外两家仍用主机广播的下标（保证各端座位表一致）。
        //   ⚠️ **不写回 _savedChars** —— 那是跨局记忆，写进去会让"离开篝火后按 F9"也沿用这个角色。
        var mine = (_stakePlayer is not null || _netOn) ? DouSpireCharacters.MyRunCharacter() : null;

        if (_netOn)
        {
            _chars = BuildNetChars();
            if (mine is not null) _chars[MeSeat] = new DouSpireCharacters.SeatPick(mine, null);
            Entry.Logger.Info("斗地主（联机）：三家 = "
                              + string.Join("、", _chars.Select(c => DouSpireCharacters.Name(c)))
                              + (mine is not null
                                 ? $"（我这一席用 run 角色：{DouSpireCharacters.Name(mine)}）"
                                 : "（拿不到我的 run 角色，用主机广播的）"));
            return;
        }

        // ★ 篝火赌注模式：**先把我这一席钉成当前 run 里正在玩的那个角色**（不抽签）——
        //   这样不管后面弹不弹选人界面，默认选中的都是"我现在用的角色"
        //   （用户 2026-10-08："我现在使用的是 mod 角色酒狐，应该直接让我操控他"；
        //   弹不弹选人由设置 RestSitePickSeat 决定，见 RestoreOrPick）。
        if (mine is not null)
        {
            _chars = DouSpireCharacters.Assign(new DouSpireCharacters.SeatPick(mine, null),
                                               System.Random.Shared.Next(),
                                               (int)DouSpireConfig.TableSize);
            Entry.Logger.Info("斗地主（篝火）：我 = " + DouSpireCharacters.Name(mine)
                              + "；各席 = " + string.Join("、", _chars.Select(c => DouSpireCharacters.Name(c))));
            return;
        }

        _chars = _savedChars ?? DouSpireCharacters.Assign(default, System.Random.Shared.Next(),
                                                          (int)DouSpireConfig.TableSize);
        Entry.Logger.Info("斗地主：各席 = "
                          + string.Join("、", _chars.Select(c => DouSpireCharacters.Name(c))));
    }

    /// <summary>有存着的局就接着打，没有就先选人。联机两个都不走（由网络开桌直接摆牌）。</summary>
    private void RestoreOrPick()
    {
        // ★★ 2026-10-09 修：**"切人数重开"必须停在选人界面**。
        //   玩家在选人界面点「四人桌」→ 走的是 ReopenForNewTableSize（Close 再 Open）。
        //   新桌子原来会顺着下面那条"存档恢复 / 篝火直接开局"的路**直接开局**，
        //   于是玩家看到的是"一切人数就直接进去了"（用户实测原话）。
        //   重开的理由本来就是"要重新选人"，所以这里一律弹选人。
        if (_reopenInPicker)
        {
            _reopenInPicker = false;
            DropRound();              // 上一局的残留存档也清掉，别让它抢在选人前面
            _picking = true;
            ShowPicking(true);
            return;
        }

        if (_netOn)
        {
            // 联机不选人（三家由 run 里的角色决定）、也不接上一局 —— BeginNetSession 会摆牌
            _picking = false;
            ShowPicking(false);
            return;
        }

        // ★ 篝火赌注模式：我这一席已经由 InitCharacters() 钉成当前 run 里正在玩的角色。
        //   设置「篝火开局弹选角色」（默认开）→ 照常弹选人界面，**但角色已预选好** ——
        //   不想换直接点「开始打牌」，想玩客串角色（鲸鱼娘/椿/八千代/雪莉）就翻箭头。
        //   （用户 2026-10-08 晚："建议还是可以改成选角色，不然我们做的角色不是可游玩角色，
        //   玩家就一直玩不到"；设置关掉则回到早上那个"直接操控 run 角色"的行为。）
        //
        //   ⚠️⚠️ **不弹选人之后必须真的开局**（2026-10-08 修的真 bug）：
        //   原来这里只 `ShowPicking(false); Refresh();`，**没调 Deal()** ⇒ `_game` 从没被创建：
        //     ① 牌桌空着、「没有正常开局」；
        //     ② 头像/HUD 刷不出来；
        //     ③ 筹码还是字段初值 1000 —— 因为 `ResetMoney()` 是在 `Deal → BeginShowCards` 里调的，
        //        没开局就永远不会按我身上的真实金币重发（用户："没有读取我身上的99金币，还是以一千金币为抵住"）。
        //   现在走**和「开始打牌」按钮（`:2700`）/「再来一局」（`:1863`）完全一样**的流程：DropRound() → Deal()。
        if (_stakePlayer is not null && _chars[MeSeat] is { IsEmpty: false })
        {
            if (DouSpireConfig.RestSitePickSeat)
            {
                _picking = true;
                ShowPicking(true);
                return;
            }

            _picking = false;
            ShowPicking(false);
            // 延后一帧再开局：这条路径是在 `_Ready` 里跑的，Deal() 会建牌局并重刷整套 UI，
            // 等这棵场景树搭完再动更稳（和 DebugAutoOpen 用 CallDeferred 同理）。
            Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(this)) return;
                DropRound();
                Deal();
            }).CallDeferred();
            return;
        }

        // ★ 防御：**打完的局绝不恢复**（2026-10-09 玩家反馈）—— 旧版本收桌时可能已经
        //   把带 Winner 的终局写进了存档，恢复出来就是那张"点鸡煲代打才出按钮"的死桌子。
        //   现在恢复前先检查，终局直接扔掉、落到下面的选人界面开新局。
        if (_savedGame is not null && _savedGame.Winner is not null)
        {
            Entry.Logger.Info("斗地主：存档的上一局已经打完了，不恢复 —— 直接开新的选人。");
            _savedGame = null;
        }

        if (_savedGame is not null)
        {
            _game = _savedGame;
            _lastAction = _savedActions;
            // ⚠️ 恢复的是"正在打"的局 —— 阶段标志必须**全部归位**。原来只补了 _game：
            //    _roundStarted 还是 false → PlayLocked 把出牌按钮全锁死 → 收桌回来这局
            //    谁也出不了牌（2026-10-09 用户实测"收桌后再开开不了牌"）。
            _bidding = false;
            _askingShow = false;
            _askingDouble = false;
            _discarding = false;
            _roundStarted = true;
            _picking = false;
            ShowPicking(false);
            ShowRps(RpsStep.Off);
            ShowShowCards(false);
            ShowDouble(false);
            ShowDiscardBtn(false);
            _multiplier = _savedMultiplier;
            Refresh();
            // 2v2：存的那局正卡在扣底牌 → 重新进扣牌阶段（联机不存档，这条只对单人成立）
            if (!_netOn && _game.Players.Count == 4 && _game.PendingDiscards.Count > 0) BeginDiscard();
            return;
        }

        _picking = true;
        ShowPicking(true);
    }

    public override void _ExitTree()
    {
        // 活角色先请走 —— 它们是独立的场景实例，不跟着这层自动消失
        for (var s = 0; s < SeatCount; s++) DespawnActor(s);

        // ── 联机：收桌要说一声，不然对面会一直等 ──
        var wasNet = _netOn;
        // ★★ 2026-10-09 修（联机实测：每帧 NullReferenceException ×272 ＋「AI 代打不出牌」
        //   ＋ 对面桌面被关）：正如 `_silentExit` 注释里讲的，旧桌子的 _ExitTree 会排在
        //   **新桌子 _Ready 之后**跑。那时 `DouSpireNet.Live` 已经是新桌子，旧桌子却还
        //   当自己是当前桌 —— 于是：
        //     ① 朝对面广播一次**假的"收桌"** → 队友的桌面被关掉；
        //     ② `DouSpireNet.Live = null` + `CloseTable()` 把**新桌子**的联机状态一起清掉
        //        → 新桌子 `_netOn` 仍为 true 却没有联机 backing，**永远等不到主机的摆牌广播**
        //        → `_game` 永远 null → `_Process` 每帧炸 → AI 出牌/扣牌/猜拳全部停摆。
        //   判据：**只有"我还是当前那张桌子"才有资格广播 / 动全局状态**；
        //         被静默回收的残骸（_silentExit）直接闭嘴。
        var stillCurrent = !_silentExit && ReferenceEquals(DouSpireNet.Live, this);
        if (_netOn && DouSpireNet.Active && stillCurrent)
        {
            if (_netHost)
            {
                Entry.Logger.Info("斗地主：主机收桌，通知所有人一起收。");
                DouSpireNet.CastClose();
            }
            else
            {
                Entry.Logger.Info("斗地主：我离桌，通知主机把我这一席交给电脑。");
                DouSpireNet.SendLeave();
            }
            DouSpireNet.CloseTable(false);
        }
        // ⚠️ **只有我还占着 Live 时才清** —— 否则会把新桌子刚注册好的 Live 一并抹掉
        if (ReferenceEquals(DouSpireNet.Live, this)) DouSpireNet.Live = null;
        _netOn = false;
        _netHost = false;

        if (_broken) return;
        if (wasNet) return;      // 联机的牌局不存档（下次开桌由主机广播重开）
        // ⚠️ 只存**真正开打的局**（_roundStarted）：明牌/猜拳/加倍阶段的 _game 只是"预览局"
        //    （地主是摆样子用的）。2026-10-09 实测：在明牌阶段收桌 → 预览局被当存档 →
        //    下次开桌直接"接着打"一局从没开始过的牌，AI 自己就出起牌了。
        // ⚠️ **打完的局也不存**（2026-10-09 玩家反馈："打完了退出再进入直接换成新的选人物"）：
        //    终局存档恢复出来就是一张打完的死桌子，玩家只能干瞪眼再退出。
        //    现在带着 Winner 收桌 = 这局翻篇，下次开桌直接弹选人。
        if (_roundStarted && _game is not null && _game.Winner is null)
        {
            _savedGame = _game;
            _savedMultiplier = _multiplier;
        }
        _savedActions = _lastAction;
        _savedChars = _chars;
    }

    // ════════════════════════════════════════════════════════
    //  搭场景
    // ════════════════════════════════════════════════════════

    private void BuildScene()
    {
        // —— 0. 纯色兜底（背景图缺失时就是它）——
        AddChild(DouSpireUi.Fill(new ColorRect
        {
            Color = DouSpireUi.Deep,
            MouseFilter = MouseFilterEnum.Ignore,
        }));

        // —— 1. 整屏背景（★ 2026-10-10 起可切换：左上角「室内」钮）——
        //   以前这里写死 `DouSpireAssets.Bg`（= bg/hall.png，斗地主那间酒馆）。
        //   现在走 `ApplyBg()`，背后是 `DouSpireConfig.DouSpireTableScene`。
        ApplyBg();
        // —— 2. 暗角：**不画了** ——
        // 两个原因：① 新桌图四周本来就有暗角；② 它是加在 _scene **下面**的，
        // 桌子一铺满整屏就被完全盖住（等于白画一层）。

        _scene = new Control { MouseFilter = MouseFilterEnum.Ignore };
        DouSpireUi.Fill(_scene);
        AddChild(_scene);

        // —— 3. 桌面（★ 2026-10-10 起可切换：左上角「桌面」钮）——
        //   改成 `ApplyTable()`，背后是 `DouSpireConfig.DouSpireTableStyle`。
        ApplyTable();

        // —— 4/5. 星盘 / 两摞牌堆：**不画了** ——
        // 新桌图自己就带中央纹章和右下角的两摞牌，再叠一层就是重影。

        // —— 4.5. 杀戮尖塔 2 主题水印（用户："我不是要你在桌面上添加杀戮尖塔2的那个主题水印了吗"）
        // ★ 2026-10-10 两处改动（用户："换成那种渲染过的版本。并且要往上移动，现在不好看"）：
        //   ① 素材：白描 alpha 版 → **彩色渲染版**（游戏本体主菜单 logo，走 res:// 引用，
        //      见 DouSpireAssets.Watermark 的注释）。
        //      所以这里**不能再压淡金 Modulate** —— 那会把金/青两色一起染黄、把彩色洗掉。
        //      现在只给一点点整体透明度，让它老实当"桌面水印"、不抢牌面。
        //   ② 位置：从和最后一手共用的 TrickAy(0.518) 抬到 MarkAy(0.335)，见上面的常量注释。
        var mark = DouSpireUi.Sprite(DouSpireAssets.Watermark, MarkAx, MarkAy,
            DouSpireUi.Px(MarkW), DouSpireUi.Px(MarkH));
        if (mark != null)
        {
            // 2026-10-07：用户："中间这个杀戮尖塔可以变为主页的那种实体，不要透明的。"
            // 2026-10-10：换成彩色版后不再染色，只留整体透明度。
            mark.Modulate = new Color(1f, 1f, 1f, 0.88f);
            _scene.AddChild(mark);
        }

        // —— 6. 三个回合光环（先加，压在立绘下面）——
        // 缩小：原来 560×560 快把整个角色都罩住了（用户："轮到你时的这个指标 ui 太大了"）。
        // ★★ 2026-10-09 修：**建满数组那 4 个坑，别按 `SeatCount` 建**。
        //   建场景这一刻 `_game` 还没好（联机要等主机广播摆牌），`SeatCount` 给的是"猜"的数；
        //   按猜的数建，就一定存在"多出来的那一坑**没人回收**"的可能 —— 它建出来时是什么样，
        //   以后就一直是什么样（用户实测："还有旧的静态贴图"）。
        //   现在反过来：**数组多长就建多长**，一坑不多一坑不少；显不显示**全部**交给
        //   `RefreshSeatArt()` / `TickWhaleFaces()` / `FillHud()` 按真值管。
        for (var s = 0; s < _seatGlow.Length; s++)
        {
            var lay = DouSpireLayout.Seats[s];
            var glow = DouSpireUi.Sprite(DouSpireAssets.TurnGlow, lay.ArtX, lay.ArtY,
                DouSpireUi.Px(230), DouSpireUi.Px(230));
            _seatGlow[s] = glow;
            if (glow != null)
            {
                glow.Visible = false;
                glow.Modulate = new Color(1f, 1f, 1f, 0.55f);
                _scene.AddChild(glow);
            }
        }

        // —— 7. 各家立绘 ——（节点总是建出来，图在 RefreshSeatArt 里按"抽到谁"贴）
        for (var s = 0; s < _seatArt.Length; s++)
        {
            var lay = DouSpireLayout.Seats[s];
            var art = MakeSeatArt(s, DouSpireUi.Px(lay.ArtW), DouSpireUi.Px(lay.ArtH));
            _seatArt[s] = art;
            _scene.AddChild(art);
        }

        // —— 7.5. 各席的**活角色**（用户："为什么我们不把角色直接放在三对应的地方呢"）——
        // 请得到活人就把它那张静态立绘藏起来；请不到（比如编外角色）就还是静态立绘顶着。
        // ★★ 2026-10-09 修：原来这里只调 `SpawnActors()` —— 那只管"请活人"，
        //   **不管立绘的显隐**。于是"超出本桌席位"的那几个坑就一直保持建出来时的样子挂着。
        //   现在改调 `RefreshSeatArt()`：它内部会调 `SpawnActors()`，而且**顺带遍历整个数组**
        //   把该藏的藏掉 —— 开桌那一瞬间就不会有残留（用户："把那个静态贴图给我彻底删了"）。
        RefreshSeatArt();

        // —— 8. 皇冠 / 地主勋章：**不再单独画大的** ——
        // 改成 HUD 头像旁边的两枚小图标（见 BuildHud），用户："做的太大了。而且放在头像旁边就行了"。

        // —— 9. 底牌 3 张：贴到屏幕最顶上、缩小（用户："按道理来说太大了，应该在最顶上"）。
        // 三张永远是 _game.Bottom 那三张（引擎里 Bottom 是快照，地主拿走它也不会少）。
        // 牌位宽 99、中心间距 116 → 牌 92 宽 + 24 的缝，正好一张一张落进框里
        // 底牌行：3 人 3 张（340px）；4 人 2v2 是 4 张暗底（430px）。明着摆 —— v1 先当"明底"给全桌看。
        // ★ 2026-10-10：按这一桌几席算（联机恒 4 席），别读设置里的 TableSize。
        _bottomRow = MakeRow(BottomCardsAx, BottomCardsAy,
            DouSpireUi.Px(SeatCount == 4 ? 430 : 340), DouSpireUi.Px(150), DouSpireUi.Gap(24));
        _scene.AddChild(_bottomRow);

        // 底牌的说明小字（不然看不出那三张是什么）
        _bottomTag = new Label
        {
            Text = "底牌",
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _bottomTag.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        _bottomTag.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(15));
        // 2026-10-07：这两个小字原来正好压在左边第一张底牌上（用户："底牌被挡住了"）。
        // 用户给的两条路里选了"不显示" —— 三张牌落进牌位框里本来就一眼看得出是底牌。
        //（节点照旧建出来，只是不挂进场景树。）

        // —— 10. 中央：场上最后一手 ——
        // 斗地主的做法：牌**小**、而且**同类牌叠起来只露数字那一竖条**（不是摊开成一条长龙）。
        // 所以牌宽 84、间距 -52 → 每张只露出左边约 32 像素（点数 + 小花色就画在那里）。
        _trickRow = MakeRow(TrickAx, TrickAy, DouSpireUi.Px(640), DouSpireUi.Px(150), DouSpireUi.Gap(-52));
        _scene.AddChild(_trickRow);

        // —— 11. 中央文字：这一手是谁出的 / 结果 ——
        _centerLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _centerLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.92f));
        _centerLabel.SetMeta("douspire_big", true);
        _centerLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(26));
        // 挪到出牌框下方（框底 ≈ y0.674），别再压在场上的牌上
        _scene.AddChild(DouSpireUi.Place(_centerLabel, 0.502f, 0.680f, DouSpireUi.Px(900), DouSpireUi.Px(90)));

        // —— 12. 各家 HUD ——
        // ★★ 2026-10-09 修：跟立绘是**同一个道理** —— **建满数组那 4 个坑**，
        //   然后"超出本桌席位"的直接不显示。以前按 `SeatCount` 建，一旦建场景那一刻猜大了
        //   （`_game` 还没好），多出来的那个信息框就**再也没人管** ——
        //   `FillHud` / `RefreshCore` 全都只跑到 `SeatCount` ⇒ 屏幕上多一个
        //   "没名字、剩 0 张"的空框，玩家看上去就是"多了一个人"
        //   （用户实测原话："四个人不是四个人是三个人"）。
        for (var s = 0; s < _hud.Length; s++)
        {
            _hud[s] = BuildHud(s);
            var hudNode = PlaceHud(_hud[s], s);
            hudNode.Visible = s < SeatCount;      // 真值说了算；RefreshCore 里每轮再兜一次
            _scene.AddChild(hudNode);

            // 12.5 明牌展示行 —— 明牌那家的手牌摊在自家 HUD 正下方（平时藏着）
            // 0.115 → 0.165：原来离头像框太近，牌的顶边被 HUD 那块框压住
            //（用户 2026-10-07："其他人的明牌被那个头像框挡住了"）
            // 0.165 → 0.105（2026-10-09）：0.165 会让明牌行**砸在立绘头上/胸口**
            //（HUD 中心 + 0.165 = 明牌行中心；HUD 高 142 → 下沿 +0.0475，立绘上沿最多到 +0.116）。
            // 收回到 0.105：上沿 0.08（HUD 下沿 0.0475 之下）、下沿 0.13，
            // 正好卡在"信息框底下、立绘顶上"那条缝里 —— 谁都不压。
            //（用户 2026-10-09："我又发现自己的立绘被明牌挡住了"）
            var lay = DouSpireLayout.Seats[s];
            var row = MakeRow(lay.HudX, lay.HudY + 0.105f,
                              DouSpireUi.Px(430), DouSpireUi.Px(76), DouSpireUi.Gap(-46));
            row.Visible = false;
            _scene.AddChild(row);
            _showRow[s] = row;
        }

        // —— 12.7 立绘提到卡牌行上面 ——（用户 2026-10-09："立绘应该在卡牌上面"）
        // 明牌展示行（12.5）比第 8 节的静态立绘建得晚 → 牌压在人身上。把立绘挪到前面。
        // 活角色本来就建在最后、天然在最上；信息框在请完活角色后还会自己 MoveToFront。
        foreach (var art in _seatArt)
            if (art is not null && GodotObject.IsInstanceValid(art)) art.MoveToFront();

        // —— 13. 标题 ——
        _titleLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _titleLabel.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        _titleLabel.SetMeta("douspire_big", true);
        // 2026-10-07 夜：横幅还是嫌小（用户："你这个横幅 ui 还是太小了"）。
        // 原来借的是游戏那张 4:1 的卷轴图，KeepAspectCentered 下怎么给尺寸都铺不宽，
        // 所以干脆自己画一条大横幅：深底 + 亮金边 + 大圆角，文字跟着一起放大。
        _titleLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(30));
        var banner = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        // 2026-10-07 方向二：深色玻璃自绘底 → 牛皮皮革九宫格（用户生成的素材，panel_leather_slim.png）。
        // 横幅只有 84 UI 高，必须用扁档图 + 边距 40（Scaled 后 ~53px，×2=106 < 112 实际像素高，
        // 且大于 47px 的包角——两个约束都要满足，见 DouSpireAssets.PanelLeatherSlim 注释）。
        banner.AddThemeStyleboxOverride("panel",
            DouSpireUi.TextureBox(DouSpireAssets.PanelLeatherSlim, 40, DouSpireUi.Px(24), DouSpireUi.Px(10))
            ?? (StyleBox)DouSpireUi.BoxGlow(new Color(0.055f, 0.045f, 0.03f, 0.92f), 16, DouSpireUi.Gold, 3));
        _scene.AddChild(DouSpireUi.Place(banner, 0.5f, 0.058f, DouSpireUi.Px(900), DouSpireUi.Px(84)));
        _scene.AddChild(DouSpireUi.Place(_titleLabel, 0.5f, 0.058f, DouSpireUi.Px(860), DouSpireUi.Px(72)));

        // —— 13.5 倍数 ——
        // **顶部中央**（标题条下面那枚圆盘的正下方）常驻显示当前倍数；
        // 翻倍的时候在牌桌中央弹一个大字（见 PopMultiplier）。
        // 用户 2026-10-07："倍数可以直接在我画红圈的那个顶部调下面显示"
        _multiplierLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        // 颜色：**白色 + 深色描边** —— 原来用金色，跟上面那枚金色圆盘撞在一起看不清
        //（用户 2026-10-07："倍数显示和那个金黄色的挡住了颜色装了"）
        _multiplierLabel.AddThemeColorOverride("font_color", Colors.White);
        _multiplierLabel.AddThemeColorOverride("font_outline_color", new Color(0.10f, 0.07f, 0.03f));
        _multiplierLabel.AddThemeConstantOverride("outline_size", Mathf.RoundToInt(DouSpireUi.Scaled(7)));
        _multiplierLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(40));
        _scene.AddChild(DouSpireUi.Place(_multiplierLabel, 0.5f, 0.115f,
                                         DouSpireUi.Px(420), DouSpireUi.Px(62)));

        _multiplierPop = new Label
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        // 弹出动画：**又大又亮 + 厚描边**（用户："弹出来的特效太小了。大一些再大点"）
        _multiplierPop.AddThemeColorOverride("font_color", new Color(1f, 0.94f, 0.55f));
        _multiplierPop.AddThemeColorOverride("font_outline_color", new Color(0.14f, 0.05f, 0.02f));
        _multiplierPop.AddThemeConstantOverride("outline_size", Mathf.RoundToInt(DouSpireUi.Scaled(16)));
        // 用户 2026-10-07："直接给我按现在的大十倍我看看" —— 150 × 10 = 1500。
        // （这个尺寸视觉上会撑出屏幕，是**故意留给他对比**的，定下来就往回收）
        _multiplierPop.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(1500));
        // 牌桌中央偏上，缩放要围着中心，所以单独设 PivotOffset（按新尺寸来）
        var popW = DouSpireUi.Px(1800);
        var popH = DouSpireUi.Px(600);
        _multiplierPop.PivotOffset = new Vector2(popW / 2f, popH / 2f);
        _scene.AddChild(DouSpireUi.Place(_multiplierPop, 0.5f, 0.30f, popW, popH));

        // —— 14. 出牌历史（可关）——
        // 挪到右侧中部 + 加深色底板：原来那条灰字直接压在"提示"按钮上（用户："牌局记录和这个提示挡住了"），
        // 而且灰字压在桌图上看不清（用户："字体颜色也不明显"）。
        var logPanel = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        // 2026-10-07 方向二：牛皮皮革九宫格（主档），边距 50 > 62px 包角在 480 宽档的实际绘制尺寸。
        logPanel.AddThemeStyleboxOverride("panel",
            DouSpireUi.TextureBox(DouSpireAssets.PanelLeather, 50, DouSpireUi.Px(16), DouSpireUi.Px(10))
            ?? (StyleBox)DouSpireUi.Box(new Color(0.05f, 0.06f, 0.08f, 0.72f), 10, new Color(0.34f, 0.30f, 0.20f), 1));
        _logLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        _logLabel.AddThemeColorOverride("font_color", new Color(0.90f, 0.91f, 0.80f, 0.96f));
        _logLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(14));
        DouSpireUi.Fill(_logLabel);
        _logLabel.OffsetLeft = DouSpireUi.Px(12);
        _logLabel.OffsetTop = DouSpireUi.Px(8);
        _logLabel.OffsetRight = -DouSpireUi.Px(12);
        _logLabel.OffsetBottom = -DouSpireUi.Px(8);
        logPanel.AddChild(_logLabel);
        // 左下角那块地方有限（上面是我的信息框、下面是我的立绘），面板收窄收矮一截
        _scene.AddChild(DouSpireUi.Place(logPanel, LogAx, LogAy, DouSpireUi.Px(360), DouSpireUi.Px(130)));

        // —— 15. 我的手牌 ——
        // 不用 HBoxContainer：它只能给**所有**牌同一个间距，做不到"同类牌叠紧、不同类牌摊开"。
        // 改成普通 Control，每张牌的 x 在 RebuildHand 里自己算。
        _handRow = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _scene.AddChild(DouSpireUi.Place(_handRow, 0.5f, HandAy, DouSpireUi.Px(1560), DouSpireUi.Px(206)));

        // —— 16. 状态提示 + 操作按钮 ——
        // 状态做成一条**带底板的金色条幅**、放在手牌正上方：原来那行小金字直接写在桌图上，
        // 用户："那个轮到你出牌的那个提示也不明显""字体颜色也不明显"。
        _statusPanel = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        // 2026-10-07 方向二：牛皮皮革九宫格（主档），边距 50（同日志/HUD）。
        _statusPanel.AddThemeStyleboxOverride("panel",
            DouSpireUi.TextureBox(DouSpireAssets.PanelLeather, 50, DouSpireUi.Px(20), DouSpireUi.Px(8))
            ?? (StyleBox)DouSpireUi.BoxGlow(new Color(0.07f, 0.06f, 0.04f, 0.88f), 12, DouSpireUi.Gold, 2));
        _statusLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _statusLabel.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        _statusLabel.SetMeta("douspire_big", true);
        _statusLabel.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(21));
        DouSpireUi.Fill(_statusLabel);
        _statusPanel.AddChild(_statusLabel);
        // 2026-10-07：用户："轮到你出牌的指标 ui 太丑了，你直接删了。"
        // 面板还照常建出来（别处还在给它赋文字），只是**不挂进场景树**，于是完全不显示。

        var actions = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        actions.AddThemeConstantOverride("separation", DouSpireUi.Scaled(14));
        // 2026-10-07：原来主按钮用 BtnPrimary、次按钮用 BtnSecondary —— 两套底图的九宫格边距
        // 不一样，画出来"出牌"就比别人小一圈（用户："出牌还是与其他两个按钮大小不一"）。
        // 现在一律用同一套底图，靠字色区分主次。
        _playBtn = MakeActionButton("出牌", DouSpireAssets.BtnSecondary, OnPlayPressed);
        _playBtn.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        _passBtn = MakeActionButton("要不起", DouSpireAssets.BtnSecondary, OnPassPressed);
        _hintBtn = MakeActionButton("提示", DouSpireAssets.BtnSecondary, OnHintPressed);
        // ★★ 2026-10-09 修：联机时**必须走 NewDeal()**（主机定种子 + 广播）。
        //   原来这里直接 `DropRound(); Deal();` —— `Deal()` 的种子是各端本地随机的，
        //   这条链路一个包都不发 ⇒ 第二局两端拿到两副不同的牌，一开局立刻分叉。
        _nextBtn = MakeActionButton("再来一局", DouSpireAssets.BtnSecondary, NewDeal);
        _nextBtn.Visible = false;
        // 「重新选人」：丢掉这一局 → 回选人界面（角色还留着上局的选择，翻箭头换人 → 点「开始打牌」开新局）。
        // 筹码照旧接着算（_moneySeeded 不动）。只在牌局结束后和「再来一局」一起亮出来。
        // ★ 联机：谁点都**全桌一起弹**（主机代发，见 DouSpireNet.PickOpenAll）——
        //   用户 2026-10-09：「谁点了重新选人都弹出来」。
        _repickBtn = MakeActionButton("重新选人", DouSpireAssets.BtnSecondary, () =>
        {
            if (_netOn) { DouSpireNet.PickOpenAll(); return; }
            DropRound();
            _picking = true;
            ShowPicking(true);
        });
        _repickBtn.Visible = false;
        // 猜拳抢地主用的两个按钮，平时藏着（猜拳阶段才跟另外三个对调）
        _bidYes = MakeActionButton("抢地主（猜拳）", DouSpireAssets.BtnSecondary, () => RpsWantSay(true));
        _bidNo = MakeActionButton("不抢", DouSpireAssets.BtnSecondary, () => RpsWantSay(false));
        _bidYes.Visible = false;
        _bidNo.Visible = false;
        // 出拳的三个按钮（贴我自己角色的手势图，进猜拳时才摆出来）
        _rpsRock = MakeActionButton("石头", DouSpireAssets.BtnSecondary, () => RpsThrowSay(0));
        _rpsScissors = MakeActionButton("剪刀", DouSpireAssets.BtnSecondary, () => RpsThrowSay(1));
        _rpsPaper = MakeActionButton("布", DouSpireAssets.BtnSecondary, () => RpsThrowSay(2));
        foreach (var b in new[] { _rpsRock, _rpsScissors, _rpsPaper })
        {
            b.Visible = false;
            b.AddThemeConstantOverride("icon_max_width", DouSpireUi.Scaled(46));
        }
        // 明牌用的两个按钮（在抢地主**之前**问）
        _showYes = MakeActionButton("明牌 ×2", DouSpireAssets.BtnSecondary, () => ShowCardsSay(true));
        _showNo = MakeActionButton("不明牌", DouSpireAssets.BtnSecondary, () => ShowCardsSay(false));
        _showYes.Visible = false;
        _showNo.Visible = false;
        // 加倍用的两个按钮（抢完地主后问）
        _doubleYes = MakeActionButton("加倍", DouSpireAssets.BtnSecondary, () => DoubleSay(true));
        _doubleNo = MakeActionButton("不加倍", DouSpireAssets.BtnSecondary, () => DoubleSay(false));
        _doubleYes.Visible = false;
        _doubleNo.Visible = false;
        // 2v2 扣牌按钮（开局后、出牌前；我在后手队才出现）
        _discardBtn = MakeActionButton("扣这张", DouSpireAssets.BtnSecondary, DiscardSay);
        _discardBtn.Visible = false;
        // 「鸡煲代打」：点一下 = AI 用当前最高难度替我打完这一局全程；再点一下收回。
        // 跟出牌那几个按钮同一套底图、同一个尺寸（用户："ui跟出牌这几个按钮一样"）。
        _autoBtn = MakeActionButton(_autoPilot ? "✓ 鸡煲代打中" : "鸡煲代打", DouSpireAssets.BtnSecondary, ToggleAutoPilot);
        foreach (var b in new[] { _playBtn, _passBtn, _hintBtn, _nextBtn, _repickBtn!,
                                  _bidYes, _bidNo, _rpsRock, _rpsScissors, _rpsPaper,
                                  _showYes, _showNo, _doubleYes, _doubleNo, _discardBtn,
                                  _autoBtn })
            actions.AddChild(b);
        _scene.AddChild(DouSpireUi.Place(actions, 0.5f, ActionAy, DouSpireUi.Px(720), DouSpireUi.Px(64)));

        // ════════════════════════════════════════════════════════
        //  17. 左上角那排小圆钮（2026-10-10 用户拍板）
        //
        //  ★★ 用户原话：「给斗地主加，德州的那两个按钮。然后这些按钮都移动到左上角在
        //     第一个角色面板信息的还有上面，适当缩小。」
        //
        //  即：**室内 / 桌面 / 牌面 / 收桌** 四颗，全部挪到
        //      「第一个角色面板信息框（座位 2 上家，HUD 在 x0.115 y0.140）的**左上方**」，
        //      排成一行、用德州同款**小圆钮**（共用 DouSpireKnob，比原来的大方按钮小）。
        //
        //  为什么是"座位 2 那一格的上方"：四人局里 2 号席的 HUD 就在屏幕左上（见
        //  `assets/layout.txt` 的 `2 = ... 0.115 0.140`），所以"左上角第一个面板"
        //  就是它；按钮排在它上沿往上一点的位置（y 0.140 - HUD半高 - 钮半高 - 间距）。
        //
        //  ⚠️ 这排钮**不吃 `DouSpireTheme` 的皮肤配色**（见 DouSpireKnob 顶部注释）——
        //     用户当初对德州那两颗的要求就是"和每张桌子配都不奇怪"，所以写死中性香槟金。
        // ════════════════════════════════════════════════════════
        {
            // 四颗钮从左往右：室内 / 桌面 / 牌面 / 收桌。
            // ★ 用「相对锚点 + 像素偏移」摆：锚点钉在 HUD 那一列的 x、钮排的 y，
            //   像素偏移只用来排四颗钮的间距（`Place` 的 ox/oy 是相对锚点的像素量）。
            var knob = DouSpireUi.Px(DouSpireKnob.Size);
            var step = knob + DouSpireUi.Gap(12);      // 钮心间距（直径 + 一点缝）
            const float colX = 0.115f;                 // 和 HUD 左缘同一条竖线
            const float rowY = 0.048f;                 // 在 HUD 上沿（0.140）之上
            // 第一颗钮的**左缘**贴住 colX ⇒ 钮心要往右挪半颗
            var ox0 = knob * 0.5f;

            Button AddKnob(DouSpireKnobIcon.Kind kind, System.Action act, string tip,
                           out Label? cap, int idx)
            {
                var b = DouSpireKnob.Make(kind, act, out cap, tooltip: tip);
                _scene.AddChild(DouSpireUi.Place(b, colX, rowY, knob, knob, ox0 + step * idx, 0f));
                return b;
            }

            _bgBtn    = AddKnob(DouSpireKnobIcon.Kind.Room,  CycleBg,       "换室内背景",            out _bgCap,    0);
            _tableBtn = AddKnob(DouSpireKnobIcon.Kind.Desk,  CycleTable,    "换桌面台面",            out _tableCap, 1);
            _faceBtn  = AddKnob(DouSpireKnobIcon.Kind.Card,  ToggleCardSkin, "换牌面（经典/尖塔/烫金）", out _faceCap,  2);
            _menuBtn  = AddKnob(DouSpireKnobIcon.Kind.Close, () => Close(Entry.Logger),
                                "收起桌面（F9）", out _closeCap, 3);

            _closeCap?.SetText("收桌");
            _faceCap?.SetText("牌面");
            UpdateKnobCaptions();
            UpdateFaceBtn();
        }

        // —— 17.5 猜拳：三只大手从三个方向伸过来（明牌之后、开局之前用；平时藏着）——
        // 三家各一只手，**左 / 中 / 右** 摆开；左右两只转 90° 朝桌子中间伸进来。
        // ⚠️ 别拿宝物房那张图当底 —— chest_room_act_1.png 是 Spine 骨骼用的**散图集**
        //    （手、石块、箱子零件全打在一张图上），铺上去满屏是碎片。
        //（用户 2026-10-07："为什么还有宝箱的素材碎片。而且不是这个猜拳，是三只大手从三个方向伸过来"）
        {
            var layer = new Control { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            _scene.AddChild(DouSpireUi.Place(layer, 0.5f, 0.45f, DouSpireUi.Px(1500), DouSpireUi.Px(620)));

            // 座位 0（我）在中间偏下、1（下家）在右、2（上家）在左；4 人时 2（对家）从顶上伸下来
            // 用户 2026-10-07："我这个手应该往下移动" —— 我这只从下方伸上来，别跟别人挤一排。
            // ★ 2026-10-10：按**这一桌几席**（`_openSeats`）算，不再读设置里的 `TableSize`
            //   —— 联机恒 4 席，设置里可能还停在 3。
            var fourSeats = SeatCount == 4;
            var xs = fourSeats ? new[] { 0.50f, 0.850f, 0.50f, 0.150f } : new[] { 0.50f, 0.850f, 0.150f };
            var ys = fourSeats ? new[] { 0.85f, 0.48f, 0.20f, 0.48f } : new[] { 0.85f, 0.48f, 0.48f };
            var labelY = fourSeats ? new[] { 0.700f, 0.115f, 0.045f, 0.115f } : new[] { 0.700f, 0.115f, 0.115f };
            var rot = fourSeats
                ? new[] { 0f, -Mathf.Pi / 2f, Mathf.Pi, Mathf.Pi / 2f }      // 对家倒挂，朝桌子中间伸
                : new[] { 0f, -Mathf.Pi / 2f, Mathf.Pi / 2f };              // 左右两只朝中间伸

            for (var s = 0; s < SeatCount; s++)
            {
                var nm = new Label
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                nm.SetMeta("douspire_big", true);
                nm.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(30));
                nm.AddThemeColorOverride("font_color", DouSpireUi.Gold);
                layer.AddChild(DouSpireUi.Place(nm, xs[s], labelY[s], DouSpireUi.Px(460), DouSpireUi.Px(54)));

                var art = new TextureRect
                {
                    MouseFilter = MouseFilterEnum.Ignore,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                };
                // 旋转和"一缩一放"都绕中心来，不然手会飘出格子
                art.PivotOffset = new Vector2(DouSpireUi.Px(160), DouSpireUi.Px(160));
                art.Rotation = rot[s];
                layer.AddChild(DouSpireUi.Place(art, xs[s], ys[s], DouSpireUi.Px(320), DouSpireUi.Px(320)));

                _rpsName[s] = nm;
                _rpsArt[s] = art;
            }
            _rpsPanel = layer;
        }

        // —— 18. 选人覆盖层 ——
        var overlay = new Control { MouseFilter = MouseFilterEnum.Stop };
        DouSpireUi.Fill(overlay);
        AddChild(overlay);
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        DouSpireUi.Fill(center);
        overlay.AddChild(center);
        center.AddChild(BuildPicker());
        _picker = overlay;
    }

    /// <summary>贴图缺失时 <see cref="DouSpireUi.Backdrop"/> 会返回 null —— 那就什么都不加。</summary>
    private void TryAdd(Control? c)
    {
        if (c != null) AddChild(c);
    }

    private void TryAddTo(Control parent, Control? c)
    {
        if (c != null) parent.AddChild(c);
    }

    /// <summary>
    /// 桌上的一排展示牌。间距（<paramref name="gap"/>）由调用方给 ——
    /// **斗地主的规矩是"同类牌叠起来、只露出数字那一竖条"**，不是每张都摊开，
    /// 所以要能给负数（负得越多叠得越狠，见 <see cref="DouSpireUi.Gap"/>）。
    /// </summary>
    private static HBoxContainer MakeRow(float ax, float ay, float boxW, float boxH, int gap)
    {
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        row.AddThemeConstantOverride("separation", gap);
        DouSpireUi.Place(row, ax, ay, boxW, boxH);
        return row;
    }

    /// <summary>
    /// 座位立绘的节点。**不管有没有图都先建出来** —— 图会随选人 / 抽到的角色变
    /// （见 <see cref="RefreshSeatArt"/>），这里若提前 return null，节点就不在，后面想换也换不了。
    /// </summary>
    private TextureRect MakeSeatArt(int seat, float w, float h)
    {
        var tr = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        var tex = SeatTexture(seat);
        tr.Texture = tex;
        // ★★ 2026-10-09 修：**默认不显示**。
        //   以前这里是 `Visible = tex != null`（图有就亮），于是「有人忘了回收这一席」的
        //   后果是**多挂一张图**（永久重影）；现在反过来 —— 只有 `RefreshSeatArt()`
        //   明确判定"这一席**没有**活模型"才亮。这样任何漏网分支的表现都是"少一张"，
        //   而不是"多一张"。（用户实测："还有旧的静态贴图"，就是这么来的。）
        tr.Visible = false;
        var lay = DouSpireLayout.Seats[seat];
        DouSpireUi.Place(tr, lay.ArtX, lay.ArtY, w, h);
        return tr;
    }

    /// <summary>
    /// 按当前三家是谁刷新座位立绘 —— **「抽到哪几个角色就放哪几个 UI」**。
    /// 换一局、改选人之后都调它。同一套角色重复调不会重复贴图。
    /// </summary>
    private void RefreshSeatArt()
    {
        var key = string.Join("|", _chars.Select(DouSpireCharacters.EntryOf));
        var changed = key != _seatArtKey;
        _seatArtKey = key;

        // 换了人 → 活角色也得换（老的先请走，新的再上来）
        if (changed) SpawnActors();

        // ★★ 2026-10-09 修：这里以前循环到 `SeatCount` 就停 —— 可节点是**建满 4 个坑**的
        //   （`_seatArt` 是长度 4 的数组）。3 人局里第 4 个坑**永远没人管**，
        //   它的 `Visible` 就保持建出来时的样子 ⇒ 永久挂在屏幕上（用户实测："还有旧的静态贴图"）。
        //   现在**遍历整个数组**：超出本桌席位的、或那一席还没定角色的，一律收掉。
        //   ⚠️ 而且这段**不再被 `changed` 挡在门外** —— 「显不显示」是每帧都得对的状态，
        //   只有「贴哪张图」才是换人才换的重活。以前 key 没变就整个 return，
        //   等于"第一次算错了就永远错下去"，玩家只能收桌重开。
        for (var s = 0; s < _seatArt.Length; s++)
        {
            if (_seatArt[s] is null) continue;

            if (s >= SeatCount || _chars[s].IsEmpty)
            {
                _seatArt[s].Visible = false;
                continue;
            }

            // 活人已经站那儿了 → 静态立绘让位（两套都在会重影）
            if (_actors[s] is { Alive: true })
            {
                _seatArt[s].Visible = false;
                continue;
            }

            // 贴图只在"换人"或"这个坑还是空的"时去取 —— 别每帧翻资产表
            if (changed || _seatArt[s].Texture is null) _seatArt[s].Texture = SeatTexture(s);
            _seatArt[s].Visible = _seatArt[s].Texture != null;
        }
        if (changed) Entry.Logger.Info($"斗地主：座位立绘 = {key}");
    }

    // ════════════════════════════════════════════════════════
    //  活角色（游戏本体那个带骨骼的，会做动作）
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 把三家请上桌 —— 用**游戏自带那个带 Spine 骨骼的角色**，不是我们自己画的图。
    ///
    /// 用户原话（2026-10-07）：「为什么我们不把角色直接放在三对应的地方呢。
    /// 打出对应的牌型会出现动作。然后我们只需要设置时机。什么都不用调，他有自己骨骼。」
    ///
    /// 所以这里只管两件事：**摆位置**（脚底踩在立绘框的下沿）、**换人时重建**。
    /// 动作由 <see cref="ActorPlay"/> 在出牌那一瞬间喊一声，动画和音效都是角色自带的。
    ///
    /// ⚠️ 编外角色（DS鲸鱼娘）没有骨骼 —— 她走 <see cref="WhaleReact"/> 那套表情，这里跳过。
    /// ⚠️ 任何一席失败都只记日志，那一席退回静态立绘，牌桌照玩。
    /// </summary>
    private void SpawnActors()
    {
        for (var s = 0; s < SeatCount; s++) DespawnActor(s);
        if (!DouSpireConfig.LiveActors) return;

        for (var s = 0; s < SeatCount; s++)
        {
            var model = _chars[s].Model;
            if (model is null) continue;

            try
            {
                var lay = DouSpireLayout.Seats[s];

                // 立绘框是**按比例**摆的（见 DouSpireUi.Place：锚点 = 比例，偏移 = 半个尺寸），
                // 所以活角色也得照同一把尺子换算，别自己 invent 一套，否则换分辨率就跑位。
                var parentW = _scene.Size.X;
                var parentH = _scene.Size.Y;
                if (parentW < 8f || parentH < 8f) { parentW = 1920f * DouSpireUi.Screen; parentH = 1080f * DouSpireUi.Screen; }

                // 脚底 = 立绘框的**下沿**（角色原点在脚上，不是中心）
                var footX = lay.ArtX * parentW;
                var footY = lay.ArtY * parentH
                            + DouSpireUi.Px(lay.ArtH) * 0.5f
                            + DouSpireConfig.ActorYOffset * DouSpireUi.Screen;

                _actors[s] = DouSpireActor.Spawn(
                    _scene, model,
                    new Vector2(footX, footY),
                    DouSpireUi.Px(lay.ArtH),
                    // 精灵默认朝右 → 只有站在**屏幕右半边**的那家翻个面，才是脸朝桌子中间
                    DouSpireConfig.ActorFaceCenter && lay.ArtX > 0.5f,
                    // 给"第三方 mod 角色的换装补丁"一个真 Creature（本体角色用不着，见 DouSpireActor.Spawn）。
                    // 拿不到就是 null —— 那边会记一条日志，不影响玩。
                    StakeCreature());
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"斗地主：第 {s} 席的活角色没请上来，用静态立绘顶着: {e.Message}");
                _actors[s] = null;
            }
        }

        // 角色是**后加进 _scene** 的，会把先建好的信息框压到下面 —— 用户截图里那把镰刀
        // 就压在右上角的信息框上。请完角色顺手把三个信息框提到最前面。
        foreach (var h in _hud)
            if (h is not null && GodotObject.IsInstanceValid(h)) h.MoveToFront();
    }

    /// <summary>请走一席的活角色（换人 / 收桌）。</summary>
    private void DespawnActor(int seat)
    {
        var a = _actors[seat];
        if (a is null) return;
        try { a.Despawn(); }
        catch (System.Exception e) { Entry.Logger.Warn($"斗地主：角色退场时出了点岔子: {e.Message}"); }
        _actors[seat] = null;
    }

    /// <summary>
    /// 让第 <paramref name="seat"/> 席做一个动作。**这就是"我们只需要设置时机"里那个时机。**
    /// 这一席没有活人（编外角色 / 加载失败）就什么都不做 —— 那是正常降级，不刷日志。
    /// </summary>
    private void ActorPlay(int seat, System.Action<DouSpireActor> act)
    {
        if (_actors[seat] is not { Alive: true } a) return;
        try { act(a); }
        catch (System.Exception e) { Entry.Logger.Warn($"斗地主：角色动作没做成（不影响玩）: {e.Message}"); }
    }

    /// <summary>
    /// 这一席现在该贴哪张图 ——
    /// 鲸鱼娘有临时表情就贴表情，否则贴**待机动画的当前帧**（她要"一直动"）；
    /// 游戏本体角色（或素材缺失）走静态立绘。
    /// </summary>
    private Texture2D? SeatTexture(int seat)
    {
        if (_chars[seat].IsGuest)
        {
            // ① 会动的表情（最活）→ ② 静态表情图 → ③ 待机动画
            if (_faceAnim[seat] is string anim)
            {
                var a = DouSpireCharacters.FaceAnim(_chars[seat].GuestId, anim, _faceAnimFrame);
                if (a != null) return a;
            }
            if (_whaleFace[seat] is string label)
            {
                var t = DouSpireCharacters.Face(_chars[seat].GuestId, label);
                if (t != null) return t;
            }
            var idle = DouSpireCharacters.IdleFrame(_chars[seat].GuestId, _idleFrame);
            if (idle != null) return idle;
        }
        return DouSpireCharacters.SeatArt(_chars[seat]);
    }

    /// <summary>给某一席换个表情 + 说一句（表情只有编外角色有，配音谁都有）。</summary>
    private void WhaleReact(int seat, DouSpireCharacters.Mood mood, double hold = 2.4)
    {
        // ★★ 2026-10-11 修 P1（用户实测："我打斗地主，我上面那席（左边那个）永远听不到声音，
        //    为什么其他的电脑我能听到"）★★
        //
        //   原来是 `if (seat < 0 || seat > 2) return;` —— 这是**3 人斗地主时代**写的硬编码。
        //   2026-10-09 加了 4 人 2v2 之后忘了放开，于是 **座位 3（上家·左，屏幕左中那一席）
        //   从此完全哑掉**：语音、表情、立绘反应全都没有。
        //
        //   为什么日志里看着"正常"：座位 3 唯一还能出声的是**抢地主 / 不抢**——
        //   那条走 `ApplyWant` → `DouSpireVoice.ReactBid`，**不经过这个函数**。
        //   诊断日志实锤：座位 3 全部记录只有 9 条，**清一色是 15_bid / 16_nobid**。
        //
        //   现在按**数组自己的长度**判（`_chars` 固定 4 格），4 人局不会再有人被吞。
        //   德州那边一直用的是 `seat >= _chars.Length`，是这里没跟上。
        if (seat < 0 || seat >= _chars.Length) return;
        var c = _chars[seat];

        // 配音先播 —— **不能放在"是不是编外角色"的判断后面**。
        //（2026-10-07 的 bug：原来这里先 `if (!IsGuest) return;`，
        //  游戏角色在那一行就被挡掉了 → 储君那套语音永远不响，用户"装了还是听不见"。）
        // 分发本身已抽到 DouSpireVoice.ReactAs（斗地主 / 德州共用同一条），免得两处漂。
        DouSpireVoice.ReactAs(c, mood, seat);

        if (!c.IsGuest) return;      // 表情只有编外角色有；游戏角色走骨骼动画
        var pool = DouSpireCharacters.FacesOf(c.GuestId, mood);
        if (pool.Length == 0) return;

        var pick = pool[System.Random.Shared.Next(pool.Length)];

        // 有「会动的」版本就用它（表情固定但人在动），没做过的退回静态表情图
        if (DouSpireCharacters.HasFaceAnim(c.GuestId, pick))
        {
            _faceAnim[seat] = pick;
            _faceAnimFrame = 0;
            _whaleFace[seat] = null;
        }
        else
        {
            _faceAnim[seat] = null;
            _whaleFace[seat] = pick;
        }

        _whaleFaceUntil[seat] = Time.GetTicksMsec() / 1000.0 + hold;
        if (_seatArt[seat] is null) return;
        var tex = SeatTexture(seat);
        _seatArt[seat].Texture = tex;
        _seatArt[seat].Visible = tex != null;
    }

    /// <summary>
    /// 编外角色的待机动画 —— 游戏本体角色自带骨骼动画，鲸鱼娘没有，
    /// 所以按 12fps 循环播她自己那 57 帧（<c>assets/whale/idle/</c>），让她"一直动"。
    /// 她正在做临时表情时让位给表情，表情到点自然接回动画。
    /// </summary>
    private void TickIdleAnimation(double delta)
    {
        try
        {
            const double step = 1.0 / DouSpireCharacters.IdleFps;
            _idleClock += delta;
            if (_idleClock < step) return;

            // 掉帧时按实际时间跳格，别让动画慢下来
            var advance = (int)(_idleClock / step);
            _idleClock -= advance * step;
            _idleFrame += advance;
            _faceAnimFrame += advance;

            for (var s = 0; s < SeatCount; s++)
            {
                if (!_chars[s].IsGuest) continue;
                if (_actors[s] is { Alive: true }) continue;   // 活模型（哪怕是站着的摆件）在 → 静态立绘别掺和
                if (_seatArt[s] is null) continue;

                // ① 「会动的表情」优先 —— 表情是固定的，但呼吸/摆头还在走
                if (_faceAnim[s] is string anim)
                {
                    var at = DouSpireCharacters.FaceAnim(_chars[s].GuestId, anim, _faceAnimFrame);
                    if (at is not null)
                    {
                        _seatArt[s].Texture = at;
                        _seatArt[s].Visible = true;
                    }
                    continue;
                }

                // ② 静态表情期间别覆盖它
                if (_whaleFace[s] is not null) continue;

                // ③ 常态：播待机动画
                var tex = DouSpireCharacters.IdleFrame(_chars[s].GuestId, _idleFrame);
                if (tex is null) continue;
                _seatArt[s].Texture = tex;
                _seatArt[s].Visible = true;
            }
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主待机动画出错（不影响玩）: {e.Message}");
        }
    }

    /// <summary>
    /// 鲸鱼娘的表情推进 —— 每帧看一眼「谁出了什么牌 / 谁要不起 / 谁赢了」，
    /// 给她贴对应的表情，到点自动回常态。游戏本体角色自带动作音效，编外角色只能这样补。
    /// </summary>
    private void TickWhaleFaces()
    {
        try
        {
            // ⓪ 活人站岗 → 静态立绘必须退场（每帧兜底）。活角色可能**异步加载**：
            //    RefreshSeatArt 判断的那一刻它还没就绪（Alive=false），静态图被放了出来，
            //    之后模型加载完也没人再隐藏 → 用户看到"游戏模型 + 我们的静态贴图"叠着
            //    （2026-10-09 用户实测，太子那席重影）。
            //    ★★ 2026-10-09 再修：**扫整个数组**，不只看本桌那几席 ——
            //    "超出本桌席位"的坑（3 人局里的第 4 坑）同样必须收干净，
            //    这是"旧的静态贴图"最后一道闸（用户："把那个静态贴图给我彻底删了"）。
            for (var s = 0; s < _seatArt.Length; s++)
            {
                if (_seatArt[s] is not { Visible: true }) continue;
                if (s >= SeatCount || _actors[s] is { Alive: true }) _seatArt[s].Visible = false;
            }

            var now = Time.GetTicksMsec() / 1000.0;

            // ① 到点回常态（静态表情和会动的表情都要清）
            for (var s = 0; s < SeatCount; s++)
            {
                if (_whaleFace[s] is null && _faceAnim[s] is null) continue;
                if (now < _whaleFaceUntil[s]) continue;
                _whaleFace[s] = null;
                _faceAnim[s] = null;
                if (_seatArt[s] is null) continue;
                var tex = SeatTexture(s);
                _seatArt[s].Texture = tex;
                _seatArt[s].Visible = tex != null;
            }

            if (_game is null) return;

            // ② 场上换了一手牌 → 出牌那个人按牌型做表情；炸弹还要翻倍
            if (!ReferenceEquals(_game.LastPlay, _watchLastPlay))
            {
                _watchLastPlay = _game.LastPlay;
                var who = _game.TrickHolder;
                if (_game.LastPlay is not null && who >= 0 && who < SeatCount)
                {
                    WhaleReact(who, DouSpireCharacters.MoodOf(_game.LastPlay));

                    // 炸弹 / 王炸 → 倍数翻倍 + 大特效（真斗地主的规矩，用户 2026-10-07 要的）
                    if (IsBomb(_game.LastPlay))
                    {
                        _multiplier *= 2;
                        Entry.Logger.Info($"斗地主：炸弹！倍数 ×2 → {_multiplier}");
                        PopMultiplier("炸弹");
                        DouSpireMusic.Play(DouSpireMusicCue.Climax);   // 高潮曲插播

                        // ★ 2026-10-09：光束音效（HyperBeam）早就有了，画面一直没跟上 ——
                        //   用户："鸡煲的光线射我我也只听到了声音没看到特效"。
                        //   现在出牌人胸口炸一朵命中爆闪 + 一圈冲击光线；
                        //   中央的筹码爆炸（PopMultiplier 里那条）重写后也能看见了。
                        SeatVfx(who, "vfx/common/vfx_common_hit_flare", 1.7f);
                        SeatVfx(who, "vfx/common/vfx_common_outward_streaks", 1.4f);

                        // ★ 王炸专属：一道雷从天上劈到牌桌中央（2026-10-08 加，学本体打闪电那套）。
                        //   `vfx_attack_lightning` 只有贴图 + 动画播放器、**没有脚本**，
                        //   不会像 bounce_spark 那样抢坐标（坑见 SpawnGameVfx 的注释），可放心用。
                        if (_game.LastPlay.Type == ComboType.Rocket)
                        {
                            SeatVfx(who, "vfx/vfx_attack_lightning", 1.2f);
                            SpawnGameVfx("vfx/vfx_attack_lightning", TableCenter(), 1.3f);
                        }
                    }
                    else
                    {
                        // 其他牌型：闪一下 + 中央弹牌型名（用户："每个都加点。三带一啊。连对。飞机啊啥的。都加"）
                        ShowComboFx(_game.LastPlay);
                    }
                }
            }

            // ③ 谁喊了「要不起」→ 他做无奈表情，压住他的那位做得意表情
            for (var s = 0; s < SeatCount; s++)
            {
                var pass = _lastAction[s] == "要不起";
                var was = _watchPass[s];
                _watchPass[s] = pass;
                if (!pass || was) continue;
                WhaleReact(s, DouSpireCharacters.Mood.Pass);
                var holder = _game.TrickHolder;
                if (holder >= 0 && holder < SeatCount && holder != s)
                    WhaleReact(holder, DouSpireCharacters.Mood.Beat);
            }

            // ④ 轮到我 → 起计时；**5 秒她催一句、20 秒自动过**
            if (_game.Current == MeSeat)
            {
                if (_watchCurrent != MeSeat)
                {
                    _myTurnSince = Time.GetTicksMsec() / 1000.0;
                    _myTurnNagged = false;
                }

                var waited = Time.GetTicksMsec() / 1000.0 - _myTurnSince;

                // 5 秒还没出牌 → 说句"让我想想"（比一出牌就说话自然）
                if (!_myTurnNagged && waited >= 5.0)
                {
                    _myTurnNagged = true;
                    WhaleReact(MeSeat, DouSpireCharacters.Mood.MyTurn);
                }

                // 20 秒 → 超时托管这一手；设 0 = 不限时。
                // 2026-10-09 修 bug（玩家反馈"一直弹 轮到你领出，不能要不起 / 牌都出完了"）：
                //   原来超时直接点"要不起"，但**领出时"要不起"是非法动作** —— 永远失败；
                //   更糟的是计时被清成 0，下一帧 waited 变成"从纪元到现在"的天文数字
                //   → **每帧都触发** → 报错弹窗无限刷屏，人这边一张没出，AI 倒把牌打完了。
                //   现在领出时照 AI 的兜底（StepAi 同款）甩最小的一张；失败也不再连点，
                //   重置成"现在"，等一个完整时限才会再试。
                if (DouSpireConfig.TurnTimeLimit > 0
                    && waited >= DouSpireConfig.TurnTimeLimit
                    && _game.Winner is null)
                {
                    if (_game.MustLead)
                    {
                        var fallback = new List<Card> { _game.Me.Hand.OrderBy(c => c.Rank).First() };
                        if (_netOn && !_netHost)
                            DouSpireNet.SendPlay(fallback);   // 客机：只上报，等主机广播落地
                        else
                        {
                            if (_netOn) DouSpireNet.CastPlay(DouSpireNet.MySeat, fallback);
                            ApplyPlay(MeSeat, fallback, out _);
                        }
                    }
                    else
                    {
                        OnPassPressed();
                    }

                    // ⚠️ 别清成 0 —— 清成 0 会让 waited 变成天文数字，下一帧立刻再触发
                    _myTurnSince = Time.GetTicksMsec() / 1000.0;
                    _myTurnNagged = false;
                }
            }
            else
            {
                _myTurnSince = 0;
                _myTurnNagged = false;
            }
            _watchCurrent = _game.Current;

            // ⑤ 谁报单了（剩 1 张）—— 自己报单是"要跑了"，对家报单是"要防"
            for (var s = 0; s < SeatCount; s++)
            {
                var n = _game.Players[s].Hand.Count;
                if (n == 1 && _watchHand[s] != 1)
                {
                    // 2026-10-09：队友报单（2v2）单独一个心情 —— 他那队要跑了，是好消息，
                    // 语音走 12_matelast（卢本伟装了"给阿姨倒杯卡布奇诺"）；别家照旧。
                    var lastMood = s == MeSeat ? DouSpireCharacters.Mood.LastOne
                        : SeatCount == 4 && _game.Players[s].Team == _game.Players[MeSeat].Team
                            ? DouSpireCharacters.Mood.MateLastOne
                            : DouSpireCharacters.Mood.RivalLastOne;
                    WhaleReact(s, lastMood);
                    if (s == MeSeat) DouSpireSfx.LastOne();   // 只给"我"报单时响，别吵
                    // 报单 = 亮剑时刻，摆个蓄力架势（2026-10-09："想看到更多的动作效果"）
                    ActorPlay(s, a => a.Play(DouSpireActor.CastAnim));
                    SeatVfx(s, "vfx/common/vfx_common_clean_flare_1", 1.2f);   // 胸口亮一下
                }
                _watchHand[s] = n;
            }

            // ⑥ 我在挑牌（每点一下牌给个反应，但别太吵）
            var sel = _selected.Count;
            if (sel > 0 && sel != _watchSelected)
                WhaleReact(MeSeat, DouSpireCharacters.Mood.Picking, 1.2);
            _watchSelected = sel;

            // ⑦ 本局结束 → 赢家开心，另外两家丧；顺手把这一局的钱算了
            var winner = _game.Winner;
            if (winner is int w)
            {
                if (_winFaceDone) return;
                _winFaceDone = true;
                _roundOverAt = Time.GetTicksMsec() / 1000.0;   // 庆祝窗口起点（按钮晚 2.5s 再亮）
                // 2v2 按队算输赢 —— 2026-10-09 抓的 bug：原来按座位比，队赢但最后一手是
                // 对家出的 ⇒ Winner 是对家 ⇒ 我这边被当"输了"，金币雨也永远不炸。
                var meWon = SeatCount == 4
                    ? _game.Players[MeSeat].Team == _game.Players[w].Team
                    : w == MeSeat;
                for (var s = 0; s < SeatCount; s++)
                {
                    var sWon = SeatCount == 4
                        ? _game.Players[s].Team == _game.Players[w].Team
                        : s == w;
                    WhaleReact(s, sWon ? DouSpireCharacters.Mood.Win : DouSpireCharacters.Mood.Lose, 6.0);
                }

                // ★ 终局演出（2026-10-08 晚 重做）：赢了 = **金币雨** ——
                //   9 蓬金币从左到右连着炸，铺满半张桌子；输了 = 一小撮意思一下。
                //（用户："我要大量的金币大量的金币降下来"；上一版只炸一蓬，被结算节奏整个埋掉）
                if (meWon) CoinRain();
                else CoinDrop(240, 0.30f, 0.70f);  // 输了也撒一撮（20 倍口径同步放大）

                PayOut(w);

                // 庆祝窗口结束时再刷一次按钮 —— RefreshButtons 平时只在操作时被调，
                // 结算后没人推它，不补这一下按钮 2.5s 后不会自己亮出来。
                GetTree().CreateTimer(2.5).Timeout += () =>
                {
                    if (_game.Winner is not null) RefreshButtons();
                };
            }
            else
            {
                _winFaceDone = false;
                _moneyPaidOut = false;
            }
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"鲸鱼娘表情推进出错（不影响玩）: {e.Message}");
        }
    }

    /// <summary>
    /// 2v2 队色：座位 0+2（我+对家）= 暖红、1+3（下家+上家）= 青蓝 ——
    /// 跟内核 PlayerState.Team 0/1 一一对应（交接文档：座位 0+2 一队）。
    /// </summary>
    private static Color TeamColor(int seat) =>
        seat % 2 == 0 ? new Color(1f, 0.45f, 0.42f) : new Color(0.45f, 0.76f, 1f);

    /// <summary>
    /// HUD 摆在座位表里给它指定的位置 —— 和它的立绘同一个来源，
    /// 不会再出现"立绘在左下、头像框跑到左上角"这种事。
    /// </summary>
    private static Control PlaceHud(Control hud, int seat)
    {
        var lay = DouSpireLayout.Seats[seat];
        return DouSpireUi.Place(hud, lay.HudX, lay.HudY,
            DouSpireUi.Px(DouSpireLayout.HudW), DouSpireUi.Px(DouSpireLayout.HudH));
    }

    /// <summary>一家的 HUD：深色底板 + 头像 + 名字/身份 + 剩牌数 + 地主小标识。</summary>
    private Control BuildHud(int seat)
    {
        var box = new Control { MouseFilter = MouseFilterEnum.Ignore };

        // 深色底板：HUD 直接坐在桌图的羊皮纸/木纹上，浅色字根本看不清（用户："字体颜色也不明显"）。
        var back = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        back.Size = new Vector2(DouSpireUi.Px(DouSpireLayout.HudW), DouSpireUi.Px(DouSpireLayout.HudH));
        // ⚠️ 2026-10-07 夜：上一版拿 hud/nameplate.png 当底板是**错的** ——
        // 它是 1000×260 的胶囊形，九宫格拉伸到 330×120 时中间那条可拉伸带几乎为 0，
        // 结果整个底板没渲染出来，HUD 只剩一行字浮在桌布上（用户发的那张截图就是）。
        // 2026-10-07 方向二：代码画的"深色玻璃"板 → 牛皮皮革九宫格（用户生成素材，主档）。
        // HUD 只有 120 UI 高（160 实际px），边距 50（Scaled 后 ~67px，×2=133 < 160，且盖住 62px 包角）。
        back.AddThemeStyleboxOverride("panel",
            DouSpireUi.TextureBox(DouSpireAssets.PanelLeather, 50, DouSpireUi.Px(12), DouSpireUi.Px(8))
            ?? (StyleBox)DouSpireUi.BoxGlow(new Color(0.05f, 0.042f, 0.032f, 0.92f), 14, new Color(0.72f, 0.58f, 0.30f), 2));
        box.AddChild(back);

        // （2026-10-09：2v2 队标从"色条"改成身份栏直接写「红队 / 蓝队」——用户："分红队蓝队
        //  打字写在信息框，不要用那个颜色线了"。色条代码已删，队色只用在文字颜色上。）

        // 头像：**直接放在深色底板上**。
        // 原来外面还套一圈"官方头像底板"（plate），用户："这个头像边框 ui 有什么用呢，太丑了" → 拆了。
        var avatar = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        avatar.AnchorLeft = avatar.AnchorRight = 0f;
        avatar.AnchorTop = avatar.AnchorBottom = 0f;
        avatar.OffsetLeft = DouSpireUi.Px(10);
        avatar.OffsetTop = DouSpireUi.Px(16);
        avatar.OffsetRight = DouSpireUi.Px(98);
        avatar.OffsetBottom = DouSpireUi.Px(104);
        box.AddChild(avatar);
        _hudAvatar[seat] = avatar;

        // ★ 出牌倒计时环（2026-10-10 加）—— 套在**头像外圈**。
        //   为什么选这儿：信息框里 112px 左边那条竖带已经被头像占满，塞不下任何东西；
        //   伸到框外又会被别人家的 HUD 撞。套在头像外面是唯一"不抢位置还最显眼"的地方，
        //   而且正好挨着那个「▶ 轮到谁」的大三角。
        //   位置/尺寸和上面的头像对齐（头像 10,16 ~ 98,104，中心 54,60），
        //   半径按控件短边算，所以换分辨率不会跑偏。
        //   必须**加在 avatar 之后** —— 不然环会被头像盖住。
        var timer = new DouSpireTurnTimer();
        timer.Position = new Vector2(DouSpireUi.Px(0), DouSpireUi.Px(6));
        timer.Size = new Vector2(DouSpireUi.Px(108), DouSpireUi.Px(108));
        box.AddChild(timer);
        _turnTimer[seat] = timer;

        // —— 地主的小皇冠 / 小勋章：贴在头像框右上 / 右下角 ——
        // 原来是 230×145 的皇冠 + 150×150 的勋章悬浮在立绘上，用户："做的太大了。而且放在头像旁边就行了"。
        // 2026-10-07：原来这两枚贴在头像右边缘（84 / 86），跟头像糊成一团
        //（用户："地主标和农民标放在一起了"）。挪到信息框右侧的空白区，上下分开。
        // 2026-10-07：原来地主家同时挂"皇冠 + 勋章"两枚（用户："地主代币和农民代币在一起"）。
        // 只留皇冠 —— 地主标识一枚就够，两枚挤一起反而分不清。
        _hudCrown[seat] = MakeHudMini(box, DouSpireAssets.Crown, 260f, 20f, 56f, 40f);

        // 名字
        var name = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Left };
        name.AddThemeColorOverride("font_color", Colors.White);
        name.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(19));
        name.Position = new Vector2(DouSpireUi.Px(112), DouSpireUi.Px(6));
        name.Size = new Vector2(DouSpireUi.Px(212), DouSpireUi.Px(28));
        box.AddChild(name);
        _hudName[seat] = name;

        // 「轮到谁出牌」的大三角 —— 挂在信息框**左外侧**（Position 是负的，故意伸到框外）。
        // 框里那 112px 左边一条竖带从头像（OffsetLeft 10 ~ OffsetRight 98）一直排到名字，
        // 挤不进任何东西；伸到框外反而最显眼，也不会跟名字/身份/剩牌数抢位置。
        // 用户 2026-10-09："到谁出牌了那个三角形太小了，加大一点" —— 从 19 号字符换成 34 号独立标。
        var turnMark = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "▶",
            Visible = false,
        };
        turnMark.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        turnMark.AddThemeColorOverride("font_outline_color", new Color(0.10f, 0.06f, 0.02f));
        turnMark.AddThemeConstantOverride("outline_size", Mathf.RoundToInt(DouSpireUi.Scaled(6)));
        turnMark.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(34));
        var markW = DouSpireUi.Px(52);
        turnMark.Position = new Vector2(-DouSpireUi.Px(58), 0f);
        turnMark.Size = new Vector2(markW, markW);
        // 呼吸缩放要围着中心转，不然会往右边甩
        turnMark.PivotOffset = new Vector2(markW / 2f, markW / 2f);
        box.AddChild(turnMark);
        _hudTurn[seat] = turnMark;

        // 身份标签（地主/农民）
        var tag = new Label { MouseFilter = MouseFilterEnum.Ignore };
        tag.AddThemeColorOverride("font_color", new Color(0.86f, 0.82f, 0.70f));
        tag.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(14));
        tag.Position = new Vector2(DouSpireUi.Px(112), DouSpireUi.Px(36));
        tag.Size = new Vector2(DouSpireUi.Px(212), DouSpireUi.Px(22));
        box.AddChild(tag);
        _hudTag[seat] = tag;

        // 钱（金币图标 + 数字）—— 贴在"身份"这一行的右侧，那块本来是空的
        var goldTex = DouSpireAssets.First(DouSpireAssets.GoldIcon);
        var gold = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = goldTex,
            Visible = goldTex != null,
        };
        gold.Position = new Vector2(DouSpireUi.Px(198), DouSpireUi.Px(32));
        gold.Size = new Vector2(DouSpireUi.Px(24), DouSpireUi.Px(24));
        box.AddChild(gold);
        _hudGoldIcon[seat] = gold;

        var money = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        money.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.42f));
        money.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(17));
        money.Position = new Vector2(DouSpireUi.Px(226), DouSpireUi.Px(32));
        money.Size = new Vector2(DouSpireUi.Px(108), DouSpireUi.Px(24));
        box.AddChild(money);
        _hudMoney[seat] = money;

        // 剩几张 —— 就用大字说清楚，不再画那条谁也看不懂的白条
        //（用户："这个白条……毫无意义。还是无法清晰的表明。如果没有意义就直接去掉"）
        var count = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Left };
        count.AddThemeColorOverride("font_color", Colors.White);
        count.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(22));
        count.Position = new Vector2(DouSpireUi.Px(112), DouSpireUi.Px(58));
        count.Size = new Vector2(DouSpireUi.Px(212), DouSpireUi.Px(32));
        box.AddChild(count);
        _hudCount[seat] = count;

        // 最近动作
        var play = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        play.AddThemeColorOverride("font_color", new Color(0.78f, 0.76f, 0.66f, 0.95f));
        play.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(13));
        // 信息框加高之后，这一行给足两行的高度 —— "出顺子（5678910JQK）"这种长记录不再往外溢
        play.Position = new Vector2(DouSpireUi.Px(112), DouSpireUi.Px(84));
        play.Size = new Vector2(DouSpireUi.Px(216), DouSpireUi.Px(52));
        box.AddChild(play);
        _hudPlay[seat] = play;

        return box;
    }

    /// <summary>HUD 里的一枚小图标（贴图缺失就返回 null，界面少个装饰而已）。</summary>
    private static TextureRect? MakeHudMini(Control parent, string[] chain, float x, float y, float w, float h)
    {
        var tex = DouSpireAssets.First(chain);
        if (tex == null) return null;
        var tr = new TextureRect
        {
            Texture = tex,
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Visible = false,
        };
        tr.AnchorLeft = tr.AnchorRight = 0f;
        tr.AnchorTop = tr.AnchorBottom = 0f;
        tr.OffsetLeft = DouSpireUi.Px(x);
        tr.OffsetTop = DouSpireUi.Px(y);
        tr.OffsetRight = DouSpireUi.Px(x + w);
        tr.OffsetBottom = DouSpireUi.Px(y + h);
        parent.AddChild(tr);
        return tr;
    }

    private Button MakeActionButton(string text, string[] skin, System.Action onPressed)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = FocusModeEnum.None,
            // 2026-10-07：三个按钮原来宽度跟着文字长短走（用户："不要三个按钮大小不一"）。
            // 宽度要够最长的那个"出牌（12）"，否则它一变宽就又不齐了。
            CustomMinimumSize = new Vector2(DouSpireUi.Px(212), DouSpireUi.Px(50)),
        };
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        if (!DouSpireUi.ImageButton(btn, skin, 14, 30, 12) && !DouSpireUi.ApplyGradButton(btn, 12))
        {
            btn.AddThemeStyleboxOverride("normal", DouSpireUi.Box(DouSpireUi.Panel, 10, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("hover", DouSpireUi.Box(new Color(0.16f, 0.14f, 0.10f, 0.95f), 10, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("pressed", DouSpireUi.Box(DouSpireUi.Panel, 10, DouSpireUi.Gold, 3));
            btn.AddThemeStyleboxOverride("disabled", DouSpireUi.Box(new Color(0.10f, 0.10f, 0.11f, 0.7f), 10, new Color(0.3f, 0.3f, 0.3f), 1));
        }
        btn.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        btn.AddThemeColorOverride("font_hover_color",
            DouSpireTheme.Skin.Light ? Colors.Black : Colors.White);
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.5f, 0.52f));
        btn.SetMeta("douspire_big", true);
        btn.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(24));
        return btn;
    }

    // ★ 2026-10-10：「收桌」原来那颗大方圆钮（`MakeRoundButton`）**删了** ——
    //   现在四颗角钮统一走 `DouSpireKnob.Make`（德州同款小圆钮），
    //   留着一个没人调的工厂只会误导下一个人。

    /// <summary>
    /// 牌面在 **经典斗地主 → 尖塔牌面 → 烫金赌桌** 之间循环切，切完立刻重绘整桌。
    /// 改的是设置项本身，框架会自动存（<c>AutoSaveModSettingsValueBinding</c>）。
    /// 设置页那个下拉和这个按钮是**同一个值**，两边随便哪边改都行。
    /// </summary>
    private void ToggleCardSkin()
    {
        DouSpireConfig.CardSkin = DouSpireConfig.CardSkin switch
        {
            DouSpireCardSkin.经典斗地主 => DouSpireCardSkin.尖塔牌面,
            DouSpireCardSkin.尖塔牌面 => DouSpireCardSkin.烫金赌桌,
            _ => DouSpireCardSkin.经典斗地主,
        };
        Entry.Logger.Info($"斗地主：牌面切成「{DouSpireConfig.CardSkin}」。");
        UpdateFaceBtn();
        Refresh();          // 手牌、底牌、场上那一手都会跟着换脸
    }

    /// <summary>
    /// 把「牌面」钮的状态刷出来。
    ///
    /// ★★ 2026-10-10：钮从**大方按钮**换成了**小圆钮**（用户："这些按钮都移动到左上角…
    ///    适当缩小"）—— 圆钮上**不写字**（见 <see cref="DouSpireKnob"/>），
    ///    所以当前值改小字 <see cref="_faceCap"/> + 悬停提示，不再往 `Text` 里塞。
    /// </summary>
    private void UpdateFaceBtn()
    {
        var name = DouSpireConfig.CardSkin switch
        {
            DouSpireCardSkin.尖塔牌面 => "尖塔",
            DouSpireCardSkin.烫金赌桌 => "烫金",
            _ => "经典",
        };
        if (_faceCap is not null && GodotObject.IsInstanceValid(_faceCap))
            _faceCap.Text = $"牌面·{name}";
        if (_faceBtn is not null && GodotObject.IsInstanceValid(_faceBtn))
            _faceBtn.TooltipText = "换牌面 —— 现在：" + name;
    }

    // ════════════════════════════════════════════════════════
    //  室内 / 桌面（左上角两颗圆钮，2026-10-10）
    //
    //  ★ 用户：「给斗地主加，德州的那两个按钮」⇒ 和德州**同一套圆钮长相**
    //    （共用 DouSpireKnob），逻辑也照德州的 ApplyScene/ApplyTable 那套抄，
    //    只是存的是斗地主自己那两个设置值（见 DouSpireConfig.DouSpireTableScene /
    //    DouSpireTableStyle）。
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 铺**当前室内**那张整屏背景（<see cref="DouSpireConfig.DouSpireTableScene"/>）。
    ///
    /// 单独抽成一个方法，是为了左上角那颗「室内」钮能**当场换** ——
    /// 不抽的话换房间得收桌重开，试四个房间要开关四次。
    ///
    /// 挂在 <c>this</c> 上（不是 <c>_scene</c>）—— 背景要在整张桌子**下面**，
    /// 见 BuildScene 里 <c>_scene</c> 是后加的。
    /// </summary>
    private void ApplyBg()
    {
        // 先撤掉旧的（换房间 / 重复调用都要安全）
        if (_bgLayer != null && GodotObject.IsInstanceValid(_bgLayer))
        {
            if (_bgLayer.GetParent() == this) RemoveChild(_bgLayer);
            _bgLayer.QueueFree();
        }
        _bgLayer = null;

        var tex = DouSpireUi.Backdrop(DouSpireAssets.DouSpireTableBgOf(DouSpireConfig.DouSpireTableScene));
        if (tex is null)
        {
            // 四张牌室图一张都读不到 —— 注意**绝不掉回 `bg/hall.png` 酒馆**：
            // 那张圆桌烤在画里，叠上我们自己的桌图会重影（德州那边踩过同一个坑）。
            // 退回一块深色底，至少不脏。
            var cover = new ColorRect { Color = DouSpireUi.Deep, MouseFilter = MouseFilterEnum.Ignore };
            DouSpireUi.Fill(cover);
            _bgLayer = cover;
        }
        else _bgLayer = tex;

        AddChild(_bgLayer);
        // ★ 必须挪到最底：0 = 纯色兜底，1 = 背景，2+ = 桌子/座位/UI。
        //   换房间时新节点是"加在末尾"的，不挪一下会盖住整张桌子。
        MoveChild(_bgLayer, 1);
    }

    /// <summary>轮换室内背景（左上角那颗「室内」钮）。换完**当场生效**。</summary>
    private void CycleBg()
    {
        var all = DouSpireScenes.All;
        var i = System.Array.IndexOf(all, DouSpireConfig.DouSpireTableScene);
        DouSpireConfig.DouSpireTableScene = all[(i + 1 + all.Length) % all.Length];
        Entry.Logger.Info($"斗地主：室内换成「{DouSpireAssets.SceneName(DouSpireConfig.DouSpireTableScene)}」"
                          + $"（图 = assets/bg/{DouSpireAssets.SceneKey(DouSpireConfig.DouSpireTableScene)}.png）。");
        ApplyBg();
        UpdateKnobCaptions();
    }

    /// <summary>
    /// 铺**当前桌面**那张桌图（<see cref="DouSpireConfig.DouSpireTableStyle"/>）。
    ///
    /// ★★ 2026-10-10：斗地主**自己的桌图还没画**，所以现在实际取的是德州那批
    ///    `table/table_poker*.png` 占位（见 <see cref="DouSpireAssets.DouSpireTableOf"/>）。
    ///    画好新图之后只要改那个函数，**这里一行都不用动**。
    ///
    /// ⚠️ 摆位和德州不一样：斗地主那张桌图是**整幅 1920×1080、椭圆外已挖空**，
    ///    所以按整屏铺（德州是按 846 高居中摆的）。
    /// </summary>
    private void ApplyTable()
    {
        if (_tableLayer != null && GodotObject.IsInstanceValid(_tableLayer))
        {
            if (_tableLayer.GetParent() == _scene) _scene.RemoveChild(_tableLayer);
            _tableLayer.QueueFree();
        }
        _tableLayer = null;
        if (_scene is null) return;

        var tex = DouSpireUi.Backdrop(DouSpireAssets.DouSpireTableOf(DouSpireConfig.DouSpireTableStyle));
        if (tex is not null)
        {
            // 整屏铺（原版那张斗地主桌图就是整幅 1920×1080）
            DouSpireUi.Fill(tex);
            _tableLayer = tex;
        }
        else
        {
            // 没桌子贴图 → 一块椭圆形深色台面，至少构图在
            var fallback = DouSpireUi.Place(new Panel { MouseFilter = MouseFilterEnum.Ignore },
                TableAx, TableAy, DouSpireUi.Px(1720), DouSpireUi.Px(957));
            fallback.AddThemeStyleboxOverride("panel",
                DouSpireUi.BoxGlow(new Color(0.13f, 0.09f, 0.06f, 0.93f), 160, new Color(0.34f, 0.26f, 0.15f), 3));
            _tableLayer = fallback;
        }

        _scene.AddChild(_tableLayer);
        // ★ 钉在第 2 层：0 = 纯色兜底，1 = 背景，2 = 桌子，3+ = 座位 / UI。
        _scene.MoveChild(_tableLayer, 0);
    }

    /// <summary>轮换桌面（左上角那颗「桌面」钮）。换完**当场生效**。</summary>
    private void CycleTable()
    {
        var all = DouSpireTableStyles.All;
        var i = System.Array.IndexOf(all, DouSpireConfig.DouSpireTableStyle);
        DouSpireConfig.DouSpireTableStyle = all[(i + 1 + all.Length) % all.Length];
        Entry.Logger.Info($"斗地主：桌面换成「{DouSpireAssets.TableName(DouSpireConfig.DouSpireTableStyle)}」"
                          + $"（图 = assets/table/{DouSpireAssets.TableKey(DouSpireConfig.DouSpireTableStyle)}.png）。");
        ApplyTable();
        UpdateKnobCaptions();
    }

    /// <summary>把两颗圆钮下面那行小字刷成当前值（换完立刻回执）。</summary>
    private void UpdateKnobCaptions()
    {
        if (_bgCap is not null && GodotObject.IsInstanceValid(_bgCap))
            _bgCap.Text = DouSpireAssets.SceneShortName(DouSpireConfig.DouSpireTableScene);
        if (_tableCap is not null && GodotObject.IsInstanceValid(_tableCap))
            _tableCap.Text = DouSpireAssets.TableShortName(DouSpireConfig.DouSpireTableStyle);
        if (_bgBtn is not null && GodotObject.IsInstanceValid(_bgBtn))
            _bgBtn.TooltipText = "换室内背景 —— 现在：" + DouSpireAssets.SceneName(DouSpireConfig.DouSpireTableScene);
        if (_tableBtn is not null && GodotObject.IsInstanceValid(_tableBtn))
            _tableBtn.TooltipText = "换桌面台面 —— 现在：" + DouSpireAssets.TableName(DouSpireConfig.DouSpireTableStyle);
    }

    // ════════════════════════════════════════════════════════
    //  选人界面
    // ════════════════════════════════════════════════════════

    private Control BuildPicker()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        StyleBox? bgTex = DouSpireUi.TextureBox(DouSpireAssets.PanelBg, 18, DouSpireUi.Px(40), DouSpireUi.Px(28));
        // 二游皮肤开着就用现画的玻璃面板；选「原样」才借游戏那张九宫格 popup 底。
        panel.AddThemeStyleboxOverride("panel", bgTex is null || DouSpireTheme.On
            ? DouSpireUi.BoxGlow(new Color(0.06f, 0.09f, 0.11f, 0.97f), 14, DouSpireUi.Gold)
            : bgTex);

        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", DouSpireUi.Scaled(10));
        panel.AddChild(box);

        // 四角装饰（皮肤设成「原样」时不挂）。必须在 VBox 之后加，才会画在它上面。
        DouSpireTheme.Decorate(panel, 22f);

        var title = new Label
        {
            Text = "斗地主尖塔 —— 选角色", MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        title.SetMeta("douspire_big", true);
        title.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(28));
        box.AddChild(title);

        var hint = new Label
        {
            // ★ 联机的那一行提示写清楚"只能翻自己那一行" + "大家都点了才开局" ——
            //   不然玩家会以为箭头坏了（别人那一行的箭头是**故意藏起来**的）。
            Text = _netOn
                ? "联机桌：每人只翻自己那一行（默认是你 run 里的角色，可随意换）；翻完点「开始打牌」，两边都点了才发牌。"
                : "坐在桌边的是尖塔里的角色。左右箭头换人。",
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(DouSpireUi.ScaledF(720), 0),
        };
        hint.AddThemeColorOverride("font_color", DouSpireUi.Dim);
        box.AddChild(hint);

        // ── 牌桌人数：两个按钮二选一（用户 2026-10-09："设计几个按钮，可以选三个人或者四个人"）
        //    改的就是设置页里那个 TableSize —— 这里点按钮 = 直接切，不用去设置页翻下拉。
        // ★★ 2026-10-09 修：**联机时这两个按钮根本不建**。
        //   联机桌坐几个人由"在场几个人"决定（`DouSpireNet.Seats` 恒 3，缺的位由 AI 补），
        //   点它只会走「按新人数重开牌桌」那条路 —— 那条路会把队友的桌子一起弄没。
        //   用户实测原话："选人直接选三人桌或者四人桌另一个人会直接闪退"。
        // （`SetPickerTableSize()` 里还留了一道 `_netOn` 兜底，别的入口绕过来也拦住。）
        var sizeRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        sizeRow.AddThemeConstantOverride("separation", DouSpireUi.Scaled(8));
        box.AddChild(sizeRow);
        if (_netOn)
        {
            var netSizeHint = new Label
            {
                Text = "联机桌恒 3 席 —— 人数由在场的人决定。",
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            netSizeHint.AddThemeColorOverride("font_color", DouSpireUi.Dim);
            netSizeHint.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(16));
            sizeRow.AddChild(netSizeHint);
        }
        else
        {
            _pickSize3 = MakeActionButton("三人桌", DouSpireAssets.BtnSecondary, () => SetPickerTableSize(3));
            _pickSize4 = MakeActionButton("四人桌（2v2）", DouSpireAssets.BtnSecondary, () => SetPickerTableSize(4));
            sizeRow.AddChild(_pickSize3);
            sizeRow.AddChild(_pickSize4);
        }

        // 选人行固定建 4 行，几人桌就亮几行（显隐在 ApplyPickerSize 里统一管）
        // ⚠️ 必须先把 seat 拷进局部变量再进闭包 —— for 循环变量全体行共享，
        //    循环结束时它=4，箭头一按 CycleSeat(4) 直接越界（2026-10-09 用户实测换角色全灭）
        for (var seat = 0; seat < 4; seat++)
        {
            var seatIdx = seat;
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            row.AddThemeConstantOverride("separation", DouSpireUi.Scaled(10));
            box.AddChild(row);
            _pickRow[seat] = row;

            var tagLabel = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(DouSpireUi.ScaledF(130), 0),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            tagLabel.AddThemeColorOverride("font_color", DouSpireUi.Dim);
            row.AddChild(tagLabel);
            _pickTag[seat] = tagLabel;

            _pickLeft[seat] = MakeArrow(DouSpireAssets.ArrowLeft, "◀", () => PickArrow(seatIdx, -1));
            _pickRight[seat] = MakeArrow(DouSpireAssets.ArrowRight, "▶", () => PickArrow(seatIdx, +1));
            row.AddChild(_pickLeft[seat]!);
            row.AddChild(_pickRight[seat]!);

            var portrait = new TextureRect
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(DouSpireUi.ScaledF(76), DouSpireUi.ScaledF(76)),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            row.AddChild(portrait);
            _pickPortrait[seat] = portrait;

            var name = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(DouSpireUi.ScaledF(180), 0),
            };
            name.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(20));
            row.AddChild(name);
            _pickName[seat] = name;

            // ★ 联机专用的一列：「我」/ 队友的名字 / 电脑。谁控谁一眼看得见
            //   （用户 2026-10-09：「显示每个玩家所控制的人」）。单人时整列收起。
            var who = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(DouSpireUi.ScaledF(96), 0),
            };
            who.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(16));
            who.AddThemeColorOverride("font_color", DouSpireUi.Dim);
            who.Visible = false;
            row.AddChild(who);
            _pickWho[seat] = who;
        }

        // ★ 选人倒计时（2026-10-10 加，用户："然后选人也要限制时间。"）——
        //   队友一直不点「开始打牌」时，主机到点会**自己开局**（见 TickPickWait），
        //   这里只把"还剩几秒"亮出来。没有这行字的话，干等的那 20 秒看着像卡死。
        var pickCd = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
        };
        pickCd.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        pickCd.AddThemeColorOverride("font_outline_color", new Color(0.10f, 0.06f, 0.02f));
        pickCd.AddThemeConstantOverride("outline_size", Mathf.RoundToInt(DouSpireUi.Scaled(5)));
        pickCd.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(18));
        box.AddChild(pickCd);
        _pickCountdown = pickCd;

        var start = MakeActionButton("开始打牌", DouSpireAssets.BtnPrimary, PickStartPressed);
        _pickStartBtn = start;
        box.AddChild(start);

        ApplyPickerSize();     // 按当前人数亮行、刷标签和按钮上的 ✓
        return panel;
    }

    /// <summary>选人行的方位标签：4 人 = 我/下家/对家/上家，3 人 = 我/下家/上家。</summary>
    private string SeatTag(int seat) => PickerSeats == 4
        ? seat switch { 0 => "我", 1 => "下家（右）", 2 => "对家（上）", _ => "上家（左）" }
        : seat switch { 0 => "我", 1 => "下家（右）", _ => "上家（左）" };

    /// <summary>选人界面里切 3 人 / 4 人桌。切到 4 人时给多出来的那一席补个不撞脸的角色。</summary>
    private void SetPickerTableSize(int seats)
    {
        // ★★ 2026-10-09 修：**联机时不许切人数**。联机桌上坐几个人是"在场几个人"说了算
        //   （`DouSpireNet.Seats` 恒 3，缺的位由 AI 补），切人数只会走到
        //   「按新人数重开牌桌」那条路 —— 那条路会把队友的桌子一起弄没
        //   （用户实测原话："选人直接选三人桌或者四人桌另一个人会直接闪退"）。
        //   联机下这两个按钮根本不会建出来（见选人界面那段），这里是**兜底**：
        //   别的入口绕过来也拦住。
        if (_netOn)
        {
            Entry.Toast("联机时人数由在场的人决定，不能自己切～", "斗地主");
            return;
        }

        if ((int)DouSpireConfig.TableSize == seats) return;
        DouSpireConfig.TableSize = (DouSpireTableSize)seats;
        Entry.Logger.Info($"斗地主：选人界面切到 {seats} 人桌。");

        if (seats == 4 && _chars[3].IsEmpty)
        {
            _chars[3] = DouSpireCharacters.Cycle(_chars[0], 1);
            for (var guard = 0; guard < 16; guard++)     // 不许和已选的撞脸，撞了就再拨一格
            {
                var clash = false;
                for (var s = 0; s < 3; s++)
                    if (!_chars[s].IsEmpty && SameSeat(_chars[s], _chars[3])) { clash = true; break; }
                if (!clash) break;
                _chars[3] = DouSpireCharacters.Cycle(_chars[3], 1);
            }
        }

        ApplyPickerSize();

        // ★ 桌面 UI（信息框/手牌行/明牌展示行……）是**开桌那一刻**按当时人数一次建死的，
        //   中途切人数不会跟着变 → 4 人局里 FillHud(3) 撞空引用，整桌停摆
        //  （2026-10-09 用户实测：角色错位、没手牌、猜拳不动，全是一次崩溃的残骸）。
        //   最稳的修法：整套桌子按新人数**重开一次** —— 角色先写进 _savedChars，重开后原样回来。
        ReopenForNewTableSize();
    }

    /// <summary>选人界面切了人数 → 收桌、立刻按新人数重开（角色原样带回）。</summary>
    private void ReopenForNewTableSize()
    {
        _savedChars = _chars;              // 把这一轮挑的角色直接交给下一张桌子
        var stake = _stakePlayer;          // 篝火赌注模式也要带过去
        // ★★ 2026-10-09：重开的**理由**就是"要重新选人" —— 让新桌子停在选人界面，
        //   别顺着"存档恢复"那条路直接开局（用户实测："切换模式就直接进去了"）。
        _reopenInPicker = true;
        Entry.Logger.Info("斗地主：按新人数重开牌桌（停在选人界面）。");
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(this)) return;
            // ★★ 必须是 **silent** 关闭：`QueueFree()` 帧末才回收，旧桌子的 `_ExitTree` 会排在
            //   下面 `Open()` 建出来的新桌子之后才跑；不静音的话它会朝对面广播假"收桌"、
            //   并把新桌子的联机状态一并清掉（2026-10-09 联机实测到的整串故障）。
            Close(Entry.Logger, silent: true);
            // ★ 重开时把**新人数**显式传进去（`Open` 里 `DouSpireLayout.Load` 和 `_openSeats` 都要它）
            var n = (int)DouSpireConfig.TableSize;
            if (stake is not null) { _stakePlayer = stake; Open(Entry.Logger, keepStake: true, seats: n); }
            else Open(Entry.Logger, seats: n);
            Entry.Toast($"已按 {(int)DouSpireConfig.TableSize} 人桌重开 —— 角色都还在，点「开始打牌」。", "斗地主");
        }).CallDeferred();
    }

    /// <summary>按当前人数显隐选人行、刷方位标签、给人数按钮打 ✓，然后刷新头像。</summary>
    private void ApplyPickerSize()
    {
        // ⚠️ 用 `PickerSeats` 而不是 `TableSize`：联机下按钮压根没建，
        //    行数也得按联机的 3 来（不然会多亮一行空席）。
        var seats = PickerSeats;
        for (var s = 0; s < 4; s++)
        {
            if (_pickRow[s] != null) _pickRow[s].Visible = s < seats;
            if (_pickTag[s] != null) _pickTag[s].Text = SeatTag(s);

            // ★ 联机选角：自己那一席随便翻；**电脑席**也能翻（本机替 AI 挑角色 —— 用户
            //   2026-10-10："如果是电脑我们也应该可以动电脑那一行选角才行。不然不好玩。"）；
            //   别人的**真人**席不能动 —— 那是他自己选的角色（用户 2026-10-09：
            //   「每个人可以自己选择自己要玩的角色」、「显示每个玩家所控制的人」）。
            var mine = !_netOn
                       || s == MeSeat
                       || (s < seats && !DouSpireNet.IsHumanSeat(DouSpireNet.ToGlobal(s)));
            if (_pickLeft[s] != null) _pickLeft[s]!.Visible = s < seats && mine;
            if (_pickRight[s] != null) _pickRight[s]!.Visible = s < seats && mine;
            if (_pickWho[s] != null) _pickWho[s]!.Visible = _netOn && s < seats;
        }
        if (_pickSize3 != null) _pickSize3.Text = seats == 3 ? "✓ 三人桌" : "三人桌";
        if (_pickSize4 != null) _pickSize4.Text = seats == 4 ? "✓ 四人桌（2v2）" : "四人桌（2v2）";

        // 「开始打牌」在联机时是"我准备好了" —— 按钮文案跟着准备状态走
        ResetPickStartBtn();

        RefreshPicker();
    }

    /// <summary>把「开始打牌」按钮恢复成"还没准备"的样子（重开选人 / 换角色时调）。</summary>
    private void ResetPickStartBtn()
    {
        if (_pickStartBtn is null) return;
        var b = _pickStartBtn;
        b.Text = _netOn ? "开始打牌（大家都要点一次）" : "开始打牌";
        b.Disabled = false;
    }

    private static Button MakeArrow(string[] chain, string fallbackText, System.Action onPressed)
    {
        var btn = new Button { Text = fallbackText, FocusMode = FocusModeEnum.None };
        // ★★ 2026-10-10 用户：「**按钮没有声音，加个本体点按钮的声音**」。
        //   箭头以前**一声不响**（MakeActionButton 有 Select()，这个没有）。
        //   现在补上：按下 = 官方 ui_click，划过 = 很轻的 ui_hover —— 和别的按钮一致。
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        btn.MouseEntered += DouSpireSfx.Hover;
        var tex = DouSpireAssets.First(chain);
        if (tex != null)
        {
            btn.Text = "";
            btn.CustomMinimumSize = new Vector2(DouSpireUi.ScaledF(52), DouSpireUi.ScaledF(52));
            btn.AddThemeStyleboxOverride("normal", DouSpireUi.TextureBox(tex));
            btn.AddThemeStyleboxOverride("hover", DouSpireUi.TextureBox(tex));
            btn.AddThemeStyleboxOverride("pressed", DouSpireUi.TextureBox(tex));
        }
        return btn;
    }

    /// <summary>
    /// 选人行上的箭头 —— 翻角色、刷界面；联机时**顺带广播**出去（别人那边也跟着变）。
    ///
    /// ★ 2026-10-10 放开：联机时**电脑那一席也能翻**（用户："2 虽然明确只能选自己那一行，
    ///   但是如果是电脑我们也应该可以动电脑那一行选角才行。不然不好玩。"）。
    ///   别人的**真人**席仍然不能动 —— 那是他自己选的角色。
    ///   电脑席走 <see cref="DouSpireNet.PickSetAi"/>：客机上报 → 主机校验"那一席确实是电脑"
    ///   → 广播给全桌，所以两台机器看到的 AI 永远是同一个（不会各改各的）。
    /// </summary>
    private void PickArrow(int seat, int delta)
    {
        if (_netOn && seat != MeSeat && DouSpireNet.IsHumanSeat(DouSpireNet.ToGlobal(seat)))
            return;      // 别人的真人席：只能他自己翻

        CycleSeat(seat, delta);
        RefreshPicker();
        if (!_netOn) return;

        var idx = DouSpireCharacters.PoolIndexOf(_chars[seat]);
        if (idx < 0) return;
        if (seat == MeSeat) DouSpireNet.PickSet(idx);
        else DouSpireNet.PickSetAi(DouSpireNet.ToGlobal(seat), idx);   // 电脑席
    }

    /// <summary>
    /// 「开始打牌」——
    /// 单人：直接开局。
    /// 联机：**本席先"准备"**，等所有真人都点了，才由主机定种子开局
    /// （不然先点的人会把还在翻箭头的队友直接拽进牌局）。
    /// </summary>
    private void PickStartPressed()
    {
        if (_picking == false) return;

        if (!_netOn)
        {
            DropRound();
            Deal();
            return;
        }

        _pickReady[MeSeat] = true;
        if (_pickStartBtn is not null)
        {
            _pickStartBtn.Text = "✓ 已准备，等队友…";
            _pickStartBtn.Disabled = true;
        }
        Entry.Logger.Info("斗地主（联机）：我点了「开始打牌」—— 等队友也点。");
        DouSpireNet.PickReady();
        RefreshPicker();
    }

    private void CycleSeat(int seat, int delta)
    {
        _chars[seat] = DouSpireCharacters.Cycle(_chars[seat], delta);

        // ★★ 2026-10-10 用户实测：「**选人的时候你点下面的按钮，上面的角色有可能会动**」
        //   —— 根因就是原来这段：撞脸时去**改别人那一行**（`_chars[other] = Cycle(...)`）。
        //   用户要求「**每个按钮只能动那一排角色**」⇒ 现在**单人 / 联机走同一条逻辑**：
        //   只管自己，撞了就**自己继续拨**，绝不碰别人那一席。
        //   （联机那边本来就是这个写法，理由也一样：本地私自改别人的角色，各端就不同步了。）
        //   ⚠️ 拨不满的极端情况：池子里的角色比座位少时，可能撞到最后还是撞（guard 兜住不死循环）。
        for (var guard = 0; guard < 16; guard++)
        {
            var clash = false;
            for (var other = 0; other < PickerSeats; other++)
            {
                if (other == seat || _chars[other].IsEmpty) continue;
                if (SameSeat(_chars[other], _chars[seat])) { clash = true; break; }
            }
            if (!clash) break;
            _chars[seat] = DouSpireCharacters.Cycle(_chars[seat], delta);
        }
    }

    /// <summary>两席是不是同一个人 —— 显式按引用比，别走 record 自动生成的 Equals。</summary>
    private static bool SameSeat(DouSpireCharacters.SeatPick a, DouSpireCharacters.SeatPick b)
        => a.GuestId == b.GuestId && ReferenceEquals(a.Model, b.Model);

    private void ShowPicking(bool on)
    {
        _picker.Visible = on;
        _scene.Visible = !on;
        if (on)
        {
            ClearTurnTimers();     // 回选人界面：桌上的倒计时环一律收掉
            RefreshPicker();
        }
    }

    private void RefreshPicker()
    {
        // 选人界面只在单人出现。人数一律用 `PickerSeats`（联机 = 3，单人 = 设置里那个）——
        // **别看 `SeatCount`**：它读 `_game`，而切人数那一瞬间 `_game` 还是上一局的，读了就会刷错行数。
        for (var seat = 0; seat < PickerSeats; seat++)
        {
            var c = _chars[seat];
            var tex = DouSpireCharacters.Icon(c);
            // 同 FillHud：游戏回收过的贴图碰不得，失效就退回无图（选人界面不能崩）
            if (!DouSpireCharacters.IsAliveTexture(tex)) tex = null;
            try
            {
                _pickPortrait[seat].Texture = tex;
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"斗地主：选人头像贴图已失效，退回无图（{e.GetType().Name}）。");
                tex = null;
                try { _pickPortrait[seat].Texture = null; } catch (System.Exception) { }
            }
            _pickPortrait[seat].Visible = tex != null;
            _pickName[seat].Text = DouSpireCharacters.HasRoster ? DouSpireCharacters.Name(c) : "（没取到角色列表）";
            _pickName[seat].AddThemeColorOverride("font_color", DouSpireCharacters.ColorOf(c));

            // 联机专用：**本人 / 队友名 / 电脑** —— 谁控谁一眼看得见。
            // ★★ 2026-10-10 用户：「多人联机每[人]看到的应该是 本人 客机名字 客机名字 客机名字
            //   这样的选人视角。不足四人则往上多加电脑名」。
            //   `ToGlobal(seat)` 按"我坐 0 号位"旋转 → 每台机器第一行都是自己（「本人」），
            //   后面按全局座位序排真人名字，空出来的补「电脑」。
            if (_pickWho[seat] is { } who)
            {
                if (!_netOn) { who.Visible = false; continue; }
                var g = DouSpireNet.ToGlobal(seat);
                var human = DouSpireNet.IsHumanSeat(g);
                var label = seat == MeSeat ? "本人" : DouSpireNet.NameOfSeat(g);
                // ★ 2026-10-10：电脑那一席**本机也能翻箭头**（用户："如果是电脑我们也应该可以动
                //   电脑那一行选角才行。不然不好玩。"）—— 名字后面点出来，不然没人知道能点。
                if (!human) label = "电脑 · 可换";
                who.Text = _pickReady[seat] ? $"{label} ✓已选" : label;
                who.AddThemeColorOverride("font_color",
                    human ? DouSpireUi.Gold : DouSpireUi.Dim);
            }
        }

        UpdatePickCountdown();
    }

    // ════════════════════════════════════════════════════════
    //  玩家操作
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// 现在能不能出牌 —— 明牌 / 抢地主 / 加倍这三个阶段里，桌上摆的是**预览牌**，
    /// 那时候让"出牌"生效会把预览局打乱（联机时更会把主机的牌局带歪）。所以一律锁住。
    /// </summary>
    private bool PlayLocked()
        => _picking || _broken || _bidding || _askingShow || _askingDouble || _discarding || !_roundStarted;

    private void OnPlayPressed()
    {
        if (PlayLocked()) return;
        if (_game.Winner is not null) return;
        if (_game.Current != MeSeat) { Entry.Toast("还没轮到你出牌", "出牌"); return; }
        if (_selected.Count == 0) { Entry.Toast("先点一张牌选上", "出牌"); return; }

        var picked = _game.Players[MeSeat].Hand.Where(_selected.Contains).ToList();

        if (_netOn && !_netHost)
        {
            // 客机：**只上报，本地不推进** —— 等主机校验完广播回来再落地，
            // 这样两边的牌局永远一模一样（主机是唯一权威）。
            DouSpireNet.SendPlay(picked);
            _netWait = "";
            _statusLabel.Text = "出牌中…（等主机确认）";
            return;
        }

        if (!ApplyPlay(MeSeat, picked, out var error)) { Entry.Toast(error, "出不了"); return; }
        if (_netOn) DouSpireNet.CastPlay(DouSpireNet.MySeat, picked);
    }

    private void OnPassPressed()
    {
        if (PlayLocked()) return;
        if (_game.Current != MeSeat || _game.Winner is not null) return;

        if (_netOn && !_netHost)
        {
            DouSpireNet.SendPass();
            _statusLabel.Text = "上报中…（等主机确认）";
            return;
        }

        if (!ApplyPass(MeSeat, out var error)) { Entry.Toast(error, "要不起"); return; }
        if (_netOn) DouSpireNet.CastPass(DouSpireNet.MySeat);
    }

    // ════════════════════════════════════════════════════════
    //  落地一手牌 —— **全桌唯一的通路**
    //
    //  单人：按钮/ AI 直接调它。
    //  联机：主机校验完调它并广播；客机收到广播后调它。
    //  所有端的 _game 都只在"这一手被认可"之后才前进，所以永远不会各走各的。
    // ════════════════════════════════════════════════════════

    private bool ApplyPlay(int localSeat, List<Card> picked, out string error)
    {
        error = "";
        if (localSeat < 0 || localSeat >= SeatCount) { error = "座位号不对"; return false; }
        if (_game.Winner is not null) { error = "牌局已经结束了"; return false; }
        if (_game.Current != localSeat) { error = "还没轮到这一家出牌"; return false; }
        if (picked.Count == 0) { error = "没选牌"; return false; }
        if (!_game.TryPlay(picked, out error)) return false;

        _lastAction[localSeat] = $"出 {Describe(_game.LastPlay!)}";
        if (_game.LastPlay?.Type == ComboType.Rocket) DouSpireSfx.Rocket();
        else DouSpireSfx.PlayCards(picked.Count, IsBomb(_game.LastPlay));
        // 活角色做动作 —— 出的是哪个牌型就出哪个招（炸弹上大招），音效是角色自带的
        var combo = _game.LastPlay;
        ActorPlay(localSeat, a => a.PlayCombo(combo!.Type));
        _selected.Clear();
        _aiTimer = DouSpireConfig.AiDelay;
        _remoteWait = 0;
        _netWait = "";
        Refresh();
        return true;
    }

    private bool ApplyPass(int localSeat, out string error)
    {
        error = "";
        if (localSeat < 0 || localSeat >= SeatCount) { error = "座位号不对"; return false; }
        if (_game.Winner is not null) { error = "牌局已经结束了"; return false; }
        if (_game.Current != localSeat) { error = "还没轮到这一家"; return false; }
        if (!_game.TryPass(out error)) return false;

        _lastAction[localSeat] = "要不起";
        DouSpireSfx.Pass();
        ActorPlay(localSeat, a => a.PlayHit());
        _selected.Clear();
        _aiTimer = DouSpireConfig.AiDelay;
        _remoteWait = 0;
        _netWait = "";
        Refresh();
        return true;
    }

    /// <summary>提示：直接帮我选出"最省的那手能压的牌"。压不住就只说一声。</summary>
    private void OnHintPressed()
    {
        if (PlayLocked()) return;
        if (_game.Current != MeSeat || _game.Winner is not null) return;

        WhaleReact(MeSeat, DouSpireCharacters.Mood.Hint, 1.6);
        _selected.Clear();
        if (_game.MustLead)
        {
            foreach (var c in RuleAi.ChooseLead(_game.Players[MeSeat].Hand, _game.Judge)) _selected.Add(c);
        }
        else
        {
            var moves = MoveGenerator.Beating(_game.Players[MeSeat].Hand, _game.LastPlay!, _game.Judge);
            if (moves.Count == 0)
            {
                Entry.Toast("这手压不住，只能要不起。", "提示");
                Refresh();
                return;
            }

            // 跟牌必须**同牌型、同张数**（斗地主的规矩）——
            // 所以场上是"三张"时只能出更大的三张，不能出三带一（那是另一种牌型，压不住）。
            // 在所有能压的里挑最省的那一手。
            var best = moves
                .OrderBy(m => m.Combo.Type == _game.LastPlay!.Type ? 0 : 1)
                .ThenBy(m => m.Combo.CardCount)
                .ThenBy(m => m.Combo.MainRank)
                .First();
            foreach (var c in best.Cards) _selected.Add(c);
        }
        Refresh();
    }

    private void OnCardToggled(Card card, Button btn)
    {
        if (_selected.Contains(card)) _selected.Remove(card);
        else _selected.Add(card);
        DouSpireSfx.Select();

        // 只做轻量重绘：整棵 UI 重建会把刚点下去的按钮 QueueFree 掉，点击容易丢。
        var wrapper = btn.GetParent<Control>();
        var lift = DouSpireUi.Px(18);
        btn.Position = new Vector2(0, _selected.Contains(card) ? -lift : 0);
        if (wrapper != null) btn.Size = new Vector2(btn.Size.X, btn.Size.Y);   // 触发一次重绘
        if (DouSpireConfig.CardSkin == DouSpireCardSkin.尖塔牌面)
            ApplySpireClickLayer(btn, _selected.Contains(card));   // 卡面是卡节点画的，这里只换边框
        else
            ApplyCardSkin(btn, card, _selected.Contains(card));

        RefreshButtons();
        _statusLabel.Text = StatusText();
    }

    // ════════════════════════════════════════════════════════
    //  电脑走子
    // ════════════════════════════════════════════════════════

    public override void _Process(double delta)
    {
        // 高潮曲到点自己退场。**必须在下面所有 return 之前** ——
        // Refresh() 只在状态变化时跑，玩家挂机不动时它不跑，那时高潮曲就退不了场。
        DouSpireMusic.Tick();

        // ★ 客机断线哨兵（2026-10-10）。**必须放在 `_picking` 那个 return 之前** ——
        //   选人阶段也会断线（队友在选人界面挂了 / 退了），放下面的话那段期间永远轮不到它。
        //   桌子还没开起来（`DouSpireNet.Active == false`，比如还在等主机应答）时它自己会让路。
        //   它返回 true = 这一帧刚把桌子收掉了 → 立刻收手，别再碰已经 QueueFree 的节点。
        if (NetLinkGuard(delta)) return;

        // ★ 联机选角：主机在选人界面里等队友点「开始打牌」。**必须放在 `_picking` 那个 return
        //   之前** —— 不然队友挂机 / 掉线时全桌会永久卡在选人界面上（`_Process` 进不来）。
        if (_picking)
        {
            // 主机：读数 + 到点自动开局（TickPickWait）；客机：**只读数**（裁定权在主机）。
            if (_netOn && _netHost) TickPickWait(delta);
            else if (_netOn) TickPickCountdown(delta);
            return;
        }

        if (_broken) return;

        // ★ 结算按钮的**时间驱动亮出**（2026-10-09 修，玩家："打完没有第一时间显示再来一把和选人物，
        //   必须点鸡煲代打才显示"）：
        //   原来只靠"庆祝窗口起点 + 2.5s 定时器"补亮 —— 但那个定时器排在 PayOut **后面**，
        //   PayOut 一抛异常（被 try 吞掉）定时器就永远不创建，按钮再也不亮，
        //   直到玩家点了鸡煲代打触发一次 Refresh 才被顺带带出来。
        //   现在每帧看一眼：到点没亮就补亮，走哪条路都兜得住。
        if (_game is { Winner: not null }
            && _nextBtn is not null && !_nextBtn.Visible
            && Time.GetTicksMsec() / 1000.0 - _roundOverAt >= 2.5)
        {
            RefreshButtons();
        }

        // 编外角色的动画/表情要先推进 —— 下面几个 return 会让座位轮到自己时提前退出
        TickIdleAnimation(delta);
        TickWhaleFaces();
        TickTurnMark(delta);       // 「轮到谁」大三角的呼吸缩放
        SyncTurnTimer();           // 出牌倒计时环：跟着"当前该谁"挂/收

        // 客机在等主机开桌：什么都不推进。
        // ⚠️ 现在主机会先**收集**"大家都点了没"（最长 25 秒，见 DouSpireNet.GatherTimeoutSeconds）
        //    才真正发牌，所以这里的等待上限必须比它长 —— 原来的 12 秒会把桌子提前收掉，
        //    主机 25 秒后开桌时客机又重开一遍（看起来像闪了一下，还容易漏掉开桌消息）。
        if (_netOn && DouSpireNet.OpenPending)
        {
            _netOpenWait += delta;
            if (_netOpenWait > 40.0)
            {
                Entry.Logger.Warn("斗地主：等主机开桌超时（对面没装 mod / 不在篝火？），收桌。");
                Entry.Toast("对面没应答 —— 确认他也装了斗地主 mod、并且也在篝火里。", "斗地主");
                DouSpireNet.OpenPending = false;
                DouSpireNet.CloseTable(false);
                Close(Entry.Logger);
            }
            return;
        }

        // 联机：离开了篝火（别人点了继续 / 换房间）就自动收桌，不然一直挡着屏幕
        if (_netOn) NetRoomGuard();

        // 联机：远程真人卡在"问话阶段"（明牌 / 抢地主 / 加倍）不动 → 主机替他表态。
        // 必须在下面那两个 return **之前**跑 —— 那三个阶段的 return 会让这里永远轮不到。
        if (_netOn) NetTickAskTimeout(delta);

        // 鸡煲代打：问话阶段（明牌 / 抢地主 / 猜拳 / 加倍）替我表态 ——
        // 也必须在各阶段的 return 之前跑，不然永远轮不到。
        if (_autoPilot) TickAutoPilot();

        // ★★ 2026-10-09 修（联机实测：每帧 NullReferenceException ×272 ＋「AI 代打不出牌」）：
        //   **联机桌的 `_game` 要等主机广播摆牌才建**（见 RestoreOrPick 里 `if (_netOn)` 那条
        //   提前 return —— 注释写着"BeginNetSession 会摆牌"），而那个分支把 `_picking` 设成了
        //   false ⇒ 上面两道 return 拦不住，一路走到下面 `_game.Winner` 就是**每帧一炸**。
        //   更要命的是：`_Process` 一抛异常，**它后面所有代码都不会执行** ——
        //   StepAi / StepDiscard / StepRps 全被跳过，于是"电脑一张牌都不出"（用户实测原话），
        //   而 `_broken` 没被置位，界面看着还活着 —— 就是一桌"假活"的牌桌。
        //   德州桌（`DouSpirePokerTable._Process`）从一开始就有这条守卫，斗地主桌漏了。
        //   位置讲究：放在 OpenPending 超时 / NetRoomGuard / NetTickAskTimeout / 代打 之后 ——
        //   那几件事**不需要 `_game` 也要跑**（客机等主机超时收桌就靠它们）。
        if (_game is null) return;

        // **明牌 / 加倍阶段：牌局还没开始，谁都不许动** —— AI 也得停。
        //（2026-10-07 的 bug：用户还在选明牌，AI 已经把地主抢了、牌都出了。
        //  原因就是这里只拦了抢地主，漏了这两个阶段。）
        if (_askingShow || _askingDouble) return;

        // 猜拳阶段走自己那套节奏，别掉进下面的出牌流程（客机也要跑 —— 摆手动画靠它推）
        if (_bidding) { StepRps(delta); return; }

        // **客机到此为止**：牌局只跟着主机的广播走，绝不自己推 AI、绝不自己判回合。
        // 上面那几行动画推进（含炸弹翻倍 / 金币结算）还是要跑 —— 它们是从 _game 的
        // 变化里"读"出来的，所以客机也会跟着翻倍、跟着结算自己的金币。
        //   ★★ 2026-10-09 修：**「鸡煲代打」是唯一的例外** —— 它替的是"我这一席"，
        //   而"我这一席"只有我自己能表态 ⇒ 必须由我**上报**给主机（`SendPlay` / `SendPass`），
        //   落地照样等主机的广播。原来这里一刀切 `return`，
        //   结果客机开代打**一张牌都不会出**（用户实测："鸡煲代打不出牌"）。
        if (_netOn && !_netHost)
        {
            if (_autoPilot) TickAutoPilotOnline(delta);
            return;
        }

        // 2v2 扣牌阶段：后手队按节奏扣牌（AI 自动 / 我点按钮），扣完才进正常出牌轮
        if (_discarding) { StepDiscard(delta); return; }

        if (_game.Winner is not null) return;
        // 鸡煲代打开着 → 我这席也交给 StepAi 打（同一套最高难度脑子、同一个出牌节奏）
        if (_game.Current == MeSeat && !_autoPilot) return;  // 轮到我（主机自己）→ 等按钮

        // 远程真人的回合：等他的输入；等太久（掉线 / 挂机）主机替他出，绝不卡死
        if (_netOn)
        {
            if (_game.Current != _lastNetTurn) { _lastNetTurn = _game.Current; _remoteWait = 0; }
            // ★★ 2026-10-09 修：**代打开着时，我这一席不算"远程真人"**。
            //   原来这里把主机自己的座位也当"远程真人"去等 —— 默认等 30 秒
            //   （或者 TurnTimeLimit+10），于是一开代打就要干等半分钟才动一下，
            //   看起来就是"代打不出牌"（用户实测原话）。我自己的牌不需要等我自己。
            if (DouSpireNet.IsHumanSeat(DouSpireNet.ToGlobal(_game.Current))
                && !(_autoPilot && _game.Current == MeSeat))
            {
                _remoteWait += delta;
                // ★ 2026-10-10：从 `+10` 收到 `+2` —— 用户要的是"每个人出牌限制 20 秒"，
                //   而屏幕上那枚倒计时环读的正是 TurnTimeLimit（默认 20）。
                //   原来多给 10 秒的宽限，环读到 0 之后还要干等 10 秒，看着像卡住。
                //   留 2 秒只是给网络往返兜个底。
                var limit = DouSpireConfig.TurnTimeLimit > 0 ? DouSpireConfig.TurnTimeLimit + 2.0 : 30.0;
                if (_remoteWait < limit) return;
                _remoteWait = 0;
                Entry.Logger.Info($"斗地主：{_game.Players[_game.Current].Name} 太久没动，主机替他出。");
            }
        }

        _aiTimer -= delta;
        if (_aiTimer > 0) return;

        try { StepAi(); }
        catch (System.Exception e)
        {
            _broken = true;
            Entry.Logger.Error($"DouSpire 电脑出牌出错，桌面停摆: {e}");
        }
    }

    /// <summary>联机：换房间了就收桌（在篝火里开桌，离开篝火这桌就没意义了）。</summary>
    private void NetRoomGuard()
    {
        if (DouSpireNet.RoomKey < 0) return;
        var now = DouSpireNet.RoomKeyNow();
        if (now < 0 || now == DouSpireNet.RoomKey) return;
        Entry.Logger.Info($"斗地主：房间变了（{DouSpireNet.RoomKey} → {now}），自动收桌。");
        NetClosed();
    }

    /// <summary>
    /// ★ 2026-10-10 加：**客机的断线哨兵**。
    ///
    /// ── 修的什么 ────────────────────────────────────────────────
    /// 用户实测（日志第 2700 行起）：主机那头断了
    /// （Steam 那边写着 <c>Connection ..._Connected -&gt; ..._ClosedByPeer</c> +
    ///  <c>Steam connection closed by host. Reason: 4001, Timeout</c>），
    /// 客机这边**一无所知** —— 牌桌"假活"着，`鸡煲代打` 每 12 秒重报一次，
    /// `SendIn` 每次刷一行"没送出去"，**刷了 3000 多行、十几分钟不放弃**。
    /// 玩家的原话：「正常应该报几次就放弃并弹提示，而不是无限重试。」
    ///
    /// ── 两个信号 + 一个心跳，任一命中就收桌 ──────────────────────
    ///  ① **硬信号**：联机 run 没了（`NetService` 不再是 Host/Client）= 对面退了 / 掉线了。
    ///     单独一帧为 false **不算**（换房间那一瞬间也会假 false），连续 2.5 秒才认。
    ///  ② **软信号**：<see cref="DouSpireNet.LinkLost"/> —— 连着 3 次发不出去、且持续 8 秒以上。
    ///  ③ **心跳**：客机每 10 秒丢一个空包（见 <see cref="DouSpireNet.SendPing"/>）——
    ///     没有它，②在"客机干等主机"的时候永远没机会触发（那段时间客机一个包都不发）。
    ///
    /// ── 为什么是"收桌"而不是"等它自己好" ────────────────────────
    /// 客机的 `_game` **只能靠主机的广播推进**（见 <see cref="TickAutoPilotOnline"/> 的注释）。
    /// 线断掉以后主机那边牌局还在往前走，这边永远补不回来 —— 这局**已经救不回来了**，
    /// 继续挂着只会让玩家以为游戏卡死。
    /// </summary>
    /// <returns>true = **这一帧把桌子收掉了**，调用方应当立刻 return（别再碰已经 QueueFree 的节点）。</returns>
    private bool NetLinkGuard(double delta)
    {
        // 主机不用管这个：主机发不出去只意味着某个客机掉线，牌局照常 ——
        // 而且"远程真人不动"早有两道兜底（`_remoteWait` 超时替他出 / `NetTickAskTimeout`）。
        if (!_netOn || _netHost || !DouSpireNet.Active) { _netDeadWait = 0; return false; }

        // ③ 心跳：每 10 秒往主机丢一个空包。
        //   为什么必须有 —— "等主机出牌"的那段时间客机**一个包都不发**，
        //   `SendFailStreak` 永远是 0，下面的软信号根本没机会触发。
        //   有了心跳，断线最多 30 秒（3 个心跳 × 10 秒）内一定被发现。
        _netPingWait += delta;
        if (_netPingWait >= 10.0)
        {
            _netPingWait = 0;
            DouSpireNet.SendPing();
        }

        // ① 硬信号：联机 run 本身没了
        if (!DouSpireNet.InMultiplayerRun())
        {
            _netDeadWait += delta;
            // 单独一帧为 false **不算** —— 换房间 / 重连的那一瞬间也会假 false。
            // 判据取"连续 2.5 秒 **且**（期间至少失败过一次 **或** 已经持续 10 秒）"：
            // 前者是"服务没了 + 消息也发不出去"的双重佐证，后者是"这么久都缓不过来，
            // 不可能是抖动"。两道一起才收桌，免得把一次瞬时抖动判成断线（那是冤案）。
            if (_netDeadWait >= 2.5
                && (DouSpireNet.SendFailStreak >= 1 || _netDeadWait >= 10.0))
            {
                NetLinkLost("主机已经离开联机了（他退了 / 掉线了）");
                return true;
            }
        }
        else
        {
            _netDeadWait = 0;
        }

        // ② 软信号：消息连着发不出去
        if (DouSpireNet.LinkLost)
        {
            NetLinkLost($"和主机的连接断了（连着 {DouSpireNet.SendFailStreak} 次发不出去）");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 线断了 —— **收桌 + 把原因明明白白告诉玩家**。只做一次（`_broken` 兜底）。
    ///
    /// 顺序讲究：**先 <see cref="DouSpireNet.CloseTable"/> 再 <see cref="Close"/>**。
    /// 反过来的话 `Close` 会触发 `_ExitTree` 里那段"客机离桌通知主机"
    /// （`SendLeave`），而那一下必然又发不出去 —— 白刷一行日志。
    /// </summary>
    private void NetLinkLost(string reason)
    {
        if (_broken) return;
        _broken = true;
        _netDeadWait = 0;
        Entry.Logger.Warn($"斗地主：{reason} —— 这局收桌（断线救不回来）。");
        DouSpireNet.CloseTable(false);
        Close(Entry.Logger);
        Entry.Toast($"{reason}。这局救不回来了 —— 回篝火重新点「斗地主」再开一桌。", "斗地主");
    }

    /// <summary>
    /// 主机侧：远程真人卡在"问话阶段"（明牌 / 抢地主 / 加倍）不动 → 主机替他表态，绝不卡死。
    ///
    /// 只盯**远程真人**：AI 主机在进阶段时就当场定了（`HostDecideAi*`），
    /// 主机自己也不用催（他自己看得见界面）。出拳阶段有自己的 12 秒兜底（`RpsThrowSay`），这里不重复管。
    ///
    /// 替他选的默认值都取"最保守"的那个 —— 不明牌 / 不抢 / 不加倍，不替他翻倍输钱。
    /// </summary>
    private void NetTickAskTimeout(double delta)
    {
        if (!_netOn || !_netHost) { _netAskWait = 0; _netAskSeat = -1; return; }

        bool[]? done = null;
        if (_askingShow) done = _showDone;
        else if (_askingDouble) done = _doubleDone;
        else if (_bidding && _rpsStep == RpsStep.Want) done = _wantDone;

        var seat = -1;
        if (done != null)
            for (var s = 0; s < SeatCount; s++)
                if (!done[s] && s != MeSeat && IsHumanLocal(s)) { seat = s; break; }

        if (seat < 0) { _netAskWait = 0; _netAskSeat = -1; return; }
        if (seat != _netAskSeat) { _netAskSeat = seat; _netAskWait = 0; }

        _netAskWait += delta;
        var limit = DouSpireConfig.TurnTimeLimit > 0 ? DouSpireConfig.TurnTimeLimit + 10.0 : 30.0;
        if (_netAskWait < limit) return;
        _netAskWait = 0;

        Entry.Logger.Info($"斗地主：{SeatNames()[seat]} 在问话阶段没动，主机替他表态（保守选项）。");
        if (_askingShow) { NetShow(seat, false); return; }        // 不明牌
        if (_askingDouble) { NetDouble(seat, false); return; }    // 不加倍
        NetWant(seat, false);                                     // 不抢
    }

    /// <summary>
    /// 电脑走一手（也可能是**主机替掉线的真人走一手**）。
    /// 走完**广播给所有人** —— 这是主机唯一向外说"牌局动了"的地方（除了四个问话阶段）。
    /// </summary>
    private void StepAi()
    {
        var seat = _game.Current;                                   // 本地视角
        var gSeat = _netOn ? DouSpireNet.ToGlobal(seat) : seat;      // 全局座位号
        // 2026-10-09 起默认走 DouZero 神经网络（设置里可切回规则 AI）；
        // 模型缺失/推理出错自动退回 RuleAi —— mod 可以没有 DouZero，但不能没有 AI。
        var move = DouZeroAi.Choose(_game, seat) ?? RuleAi.Choose(_game);

        if (move is null || move.Count == 0)
        {
            if (_game.MustLead)
            {
                // 兜底：领出时"要不起"是非法动作，甩最小的一张 —— 绝不让 AI 卡死
                var fallback = new List<Card> { _game.Me.Hand.OrderBy(c => c.Rank).First() };
                if (_netOn) DouSpireNet.CastPlay(gSeat, fallback);
                ApplyPlay(seat, fallback, out _);
                return;
            }
            if (_netOn) DouSpireNet.CastPass(gSeat);
            ApplyPass(seat, out _);
            return;
        }

        if (_netOn) DouSpireNet.CastPlay(gSeat, move);
        ApplyPlay(seat, move, out _);
    }

    // ════════════════════════════════════════════════════════
    //  刷新
    // ════════════════════════════════════════════════════════

    /// <summary>渲染出错就停摆 + 记日志，绝不把异常抛进 Godot 的信号派发里（那会直接崩游戏）。</summary>
    private void Refresh()
    {
        if (_broken || _picking) return;

        // ★★ 2026-10-10 **事故第七条**：`_game` 为 null 的窗口期不许进 RefreshCore。
        //
        //   症状（昨晚朋友实测的日志）：点「牌面」按钮 →
        //     `DouSpireTable.RefreshCore()` NullReferenceException
        //     → catch 到 → `_broken = true` → **整桌永久停摆**（"桌面渲染出错，已停摆"）。
        //
        //   为什么会有这个窗口：**`_picking == false` 并不代表 `_game` 建好了。**
        //   联机桌 `RestoreOrPick()` 会提前 return 并把 `_picking` 设成 false（牌局要等主机广播
        //   才建，见事故第四条）；"点完开始打牌"到"主机广播落地"之间同理。
        //   事故第四条只给 `_Process` 加了守卫，**`Refresh()` 这条 UI 回调路漏了** ——
        //   而按钮是活的，玩家一按就打穿。
        //
        //   德州桌的同名函数（`DouSpirePokerTable.Refresh()`）一直有这道守卫，这里补齐对齐。
        if (_game is null)
        {
            // 顺手把上局残留的动作按钮收干净：它们建出来默认就是 `Visible = true`，
            // 而唯一设置可见性的 `RefreshButtons()` 只在 RefreshCore 里跑 —— `_game` 为 null 时
            // 跑不到 ⇒ 不收的话这几颗会一直亮着，玩家一点就点进 `_game`（同一个 bug 的另一扇门）。
            HideActionButtonsWhileWaiting();
            return;
        }

        try
        {
            RefreshCore();
            if (_bidding || _askingShow || _askingDouble) RefreshRpsUi();
            UpdateMusic();
        }
        catch (System.Exception e)
        {
            _broken = true;
            Entry.Logger.Error($"DouSpire 桌面渲染出错，已停摆: {e}");
        }
    }

    /// <summary>
    /// 一局还没建起来（`_game is null`）时，把"上一局留下的"动作按钮全收掉。
    ///
    /// 为什么需要：这些按钮建出来默认 `Visible = true`，而唯一的可见性开关
    /// <c>RefreshButtons()</c> 只在 <c>RefreshCore()</c> 里跑 —— `_game` 为 null 时根本跑不到，
    /// 于是它们会带着"上一局的亮法"留在屏幕上。玩家一按，处理函数（`OnPlayPressed` 等）
    /// 第一件事就是摸 `_game` ⇒ NRE。
    ///
    /// **常驻开关不在此列** —— 「鸡煲代打」那种不碰 `_game` 的按钮本来就该一直亮着能点。
    /// </summary>
    private void HideActionButtonsWhileWaiting()
    {
        foreach (var b in new[]
                 {
                     _playBtn, _passBtn, _hintBtn, _nextBtn, _repickBtn,
                     _bidYes, _bidNo, _rpsRock, _rpsScissors, _rpsPaper,
                     _showYes, _showNo, _doubleYes, _doubleNo, _discardBtn,
                 })
        {
            if (b is null) continue;
            try { b.Visible = false; } catch (System.Exception) { }
        }
    }

    private void RefreshCore()
    {
        // 三家立绘跟着"抽到谁"走（换局 / 改选人后这里会重贴）
        RefreshSeatArt();

        // ★★ 2026-10-09 兜底：**超出本桌席位**的信息框 / 明牌行一律收掉。
        //   节点是建满 4 个坑的（见 BuildScene 第 12 段），`FillHud` 却只跑到 `SeatCount` ——
        //   这里就是那"多出来的一个坑"每轮的闸门（用户："四个人不是四个人是三个人"）。
        for (var s = SeatCount; s < _hud.Length; s++)
        {
            if (_hud[s] is { Visible: true }) _hud[s].Visible = false;
            if (_showRow[s] is { Visible: true }) _showRow[s].Visible = false;
        }

        // 新的一轮：上一轮各家的动作全部作废
        if (_game.MustLead)
            for (var i = 0; i < SeatCount; i++) _lastAction[i] = "";

        var landlord = _game.Players.FirstOrDefault(p => p.IsLandlord);

        _titleLabel.Text = "斗地主尖塔"
                           + (landlord is null ? "" : $"   地主：{landlord.Name}")
                           + $"   第 {_game.TurnNo} 手"
                           + (_game.Winner is null ? "" : "   —— 本局结束");

        // 地主的小皇冠 / 小勋章：跟着地主那一家的 HUD 走（FillHud 里统一开关）。
        var landlordSeat = -1;
        for (var i = 0; i < SeatCount; i++)
            if (landlord != null && ReferenceEquals(_game.Players[i], landlord)) { landlordSeat = i; break; }
        for (var s = 0; s < SeatCount; s++)
        {
            var isLord = s == landlordSeat;
            if (_hudCrown[s] != null) _hudCrown[s]!.Visible = isLord;
            if (_hudBadge[s] != null) _hudBadge[s]!.Visible = isLord;
        }

        // 定地主：当上那一位摆个"蓄力称王"的架势（只做一次）
        if (landlordSeat >= 0 && landlordSeat != _lordAnimSeat)
        {
            _lordAnimSeat = landlordSeat;
            ActorPlay(landlordSeat, a => a.PlayLandlord());
            DouSpireSfx.LordCrown();   // 主权之刃 —— 加冕的味儿
            WhaleReact(landlordSeat, DouSpireCharacters.Mood.Landlord, 3.0);
        }

        // 底牌 3 张：**永远是那三张**（引擎里 Bottom 是快照，地主拿走也不会少一张），
        // 贴在最顶上、牌画小一点（用户："太大了，应该在最顶上"）。
        RebuildRow(_bottomRow, _game.Bottom.OrderByDescending(c => c.Rank).ToList(), DouSpireUi.Px(92));

        // 场上最后一手 —— **只画这一手**（别人出牌就把我上次的顶掉，不会堆在桌上）；
        // 牌小一号（84），配合 MakeRow 里的负间距 → 只露数字。
        var trickCards = _game.LastCards is null || _game.LastCards.Count == 0
            ? new List<Card>()
            : _game.LastCards.OrderByDescending(c => c.Rank).ToList();
        RebuildRow(_trickRow, trickCards, DouSpireUi.Px(84));

        if (_game.LastPlay is null)
        {
            _centerLabel.Text = _game.Winner is null
                ? $"场上暂无出牌\n（这一轮由 {_game.Players[_game.Current].Name} 领出）"
                : ResultWithMultiplier();
        }
        else
        {
            var who = _game.TrickHolder >= 0 ? _game.Players[_game.TrickHolder].Name : "?";
            _centerLabel.Text = $"{who} 出了 {_game.LastPlay.TypeName}";
        }

        for (var s = 0; s < SeatCount; s++) FillHud(s);

        // 顶部中央的倍数常驻显示（**1 倍也显示** —— 用户："可以直接显示"，好一眼看出现在几倍）
        if (_multiplierLabel != null)
            _multiplierLabel.Text = $"倍数 ×{_multiplier}";

        // 明牌：把摊了牌那家的手牌摆出来（小牌，只露点数）。
        // **只摊别人家的** —— 我自己明牌是"亮给对手看"，没必要在我这儿再摊一遍，
        // 那反而会挡住我自己的座位（用户 2026-10-07："我明牌只需要看手牌就行了。不需要给我自己看。给敌人看就行了"）
        for (var s = 0; s < SeatCount; s++)
        {
            if (_showRow[s] is null) continue;
            var show = _showCardsSeat[s] && s != MeSeat && _game.Winner is null;
            _showRow[s].Visible = show;
            if (show)
                RebuildRow(_showRow[s], _game.Players[s].Hand.OrderByDescending(c => c.Rank).ToList(),
                           DouSpireUi.Px(66));
        }

        // 立绘永远压在最上层。
        // 日志面板 / 明牌行 / 信息框全都是**后建的在上**，光靠 `_Ready` 里那一次 MoveToFront
        // 是兜不住的（用户 2026-10-09："我又发现自己的立绘被明牌挡住了"）。每轮重提一次，
        // 顺序上谁也别想盖住人。三、四个节点的 MoveToFront，开销可以忽略。
        BringArtToFront();

        // ── 音效 + 动作：轮到谁（只在换人时响一声）、本局结束（只响一次）──
        if (_game.Winner is not null)
        {
            if (!_endSfxDone)
            {
                _endSfxDone = true;
                // 2v2 按队算（同上：Winner 是"最后一手那个座位"，不是"队"）
                var me = SeatCount == 4
                    ? _game.Players[MeSeat].Team == _game.Players[_game.Winner.Value].Team
                    : _game.Winner.Value == MeSeat;
                if (me) DouSpireSfx.Win();
                else DouSpireSfx.Lose();
                // 结算曲：赢了/输了各一首（槽位空着就静音，让上面那几声音效自己响）
                DouSpireMusic.Play(me ? DouSpireMusicCue.Win : DouSpireMusicCue.Lose);

                // 赢家起势、输家倒下 —— 都是角色自带的动画（2v2 同队一起起势）
                for (var s = 0; s < SeatCount; s++)
                {
                    var win = SeatCount == 4
                        ? _game.Players[s].Team == _game.Players[_game.Winner.Value].Team
                        : s == _game.Winner.Value;
                    ActorPlay(s, a => { if (win) a.PlayWin(); else a.PlayLose(); });
                }
            }
        }
        else
        {
            _endSfxDone = false;
            if (_game.Current != _lastCurrentSeat)
            {
                _lastCurrentSeat = _game.Current;
                if (_game.Current == MeSeat) DouSpireSfx.MyTurn();
            }
        }

        _statusLabel.Text = StatusText();
        _logLabel.Text = DouSpireConfig.ShowLog ? string.Join("\n", _game.Log.TakeLast(5)) : "";

        RebuildHand();
        RefreshButtons();
    }

    /// <summary>
    /// 把三席立绘提到最上层 —— **明牌行 / 日志面板 / 信息框都不许压在人身上**
    /// （用户 2026-10-09："我又发现自己的立绘被明牌挡住了"）。
    ///
    /// 为什么要每轮重提：`BuildScene` 里立绘是第 7 段建的，后面的明牌行（12.5）、HUD（12）、
    /// 日志面板（14）、按钮（16）**全都建在它之后 ⇒ 天然盖在它上面**。`_Ready` 末尾那次
    /// MoveToFront（第 12.7 段）只救得了那一刻的节点，后面动态改可见性 / 重新贴图都白搭。
    /// </summary>
    private void BringArtToFront()
    {
        for (var s = 0; s < _seatArt.Length; s++)
        {
            var art = _seatArt[s];
            if (art is null || !GodotObject.IsInstanceValid(art) || !art.Visible) continue;
            art.MoveToFront();
        }
        // ★ 2026-10-10：**左上角那排小圆钮必须比立绘还高**。
        //   它们和立绘同在 `_scene` 这一层，上面刚把立绘全提了顶 ——
        //   不提钮的话，只要立绘和钮在屏幕上有一丁点重叠（或者座位数一变、立绘变大），
        //   钮就被盖住点不着。四颗一起提，顺序保持在最后的是「收桌」。
        BringKnobsToFront();
    }

    /// <summary>把左上角那排圆钮提到最前（比立绘、明牌行、日志面板都高）。</summary>
    private void BringKnobsToFront()
    {
        foreach (var b in new[] { _bgBtn, _tableBtn, _faceBtn, _menuBtn })
        {
            if (b is null || !GodotObject.IsInstanceValid(b)) continue;
            b.MoveToFront();
        }
    }

    /// <summary>
    /// 「轮到谁出牌」那个大三角的呼吸缩放（<see cref="_hudTurn"/>）。
    /// 只在它可见时改 Scale —— 轮到谁，谁那个三角一涨一缩，比原来那个塞在名字里的小字符醒目得多。
    /// </summary>
    private void TickTurnMark(double delta)
    {
        _turnMarkPhase += delta;
        var k = 1f + 0.14f * Mathf.Sin((float)(_turnMarkPhase * 4.2));
        for (var s = 0; s < _hudTurn.Length; s++)
        {
            var m = _hudTurn[s];
            if (m is null || !GodotObject.IsInstanceValid(m) || !m.Visible) continue;
            m.Scale = new Vector2(k, k);
        }
    }

    /// <summary>
    /// 把出牌倒计时环挂到"当前该出牌的那一家"头上（2026-10-10 加）。
    ///
    /// 规则：
    /// <list type="bullet">
    ///   <item>牌局**真开打了**（<c>_roundStarted</c>），而且不在明牌 / 抢地主 / 加倍 / 扣牌
    ///         这些"还没轮到出牌"的阶段，也没结算、没崩；</item>
    ///   <item>轮到**真人**（我 / 联机队友）→ 金环读秒，上限 = 设置里的「出牌时间上限」（默认 20 秒，
    ///         设 0 = 不限时，环上显示 ∞）；</item>
    ///   <item>轮到**电脑** → 暗环慢慢转（它 1.5 秒就出牌，读 20 秒是骗人的）；</item>
    ///   <item>其余时候（选人 / 阶段切换 / 结算）→ 全部收起来。</item>
    /// </list>
    ///
    /// ⚠️ 只在"换人"那一帧动 —— 每帧 Arm 一次等于秒数永远停在满格（这个坑很容易踩）。
    /// </summary>
    private void SyncTurnTimer()
    {
        if (_turnTimer[0] is null || _game is null) return;

        var cur = _game.Current;
        var live = !_broken && _roundStarted && _game.Winner is null
                   && !_askingShow && !_askingDouble && !_bidding && !_discarding
                   && cur >= 0 && cur < SeatCount && cur < _turnTimer.Length;

        if (!live)
        {
            if (_timerSeat >= 0)
            {
                foreach (var t in _turnTimer) t?.Clear();
                _timerSeat = -1;
            }
            return;
        }

        if (cur == _timerSeat) return;      // 还是那一家 —— 别重置计时

        foreach (var t in _turnTimer) t?.Clear();
        _timerSeat = cur;

        var t2 = _turnTimer[cur];
        if (t2 is null) return;

        // 联机里"真人"包含队友那一席（他挂机时主机 20 秒后替他出，见 _remoteWait）。
        var human = !_netOn || DouSpireNet.IsHumanSeat(DouSpireNet.ToGlobal(cur));
        if (human) t2.Arm(DouSpireConfig.TurnTimeLimit);
        else t2.Think();
    }

    /// <summary>把所有倒计时环收起来（选人界面 / 收桌时调，见 <see cref="ShowPicking"/>）。</summary>
    private void ClearTurnTimers()
    {
        foreach (var t in _turnTimer) t?.Clear();
        _timerSeat = -1;
    }

    private void FillHud(int seat)
    {
        var p = _game.Players[seat];
        var c = _chars[seat];
        var turn = _game.Winner is null && _game.Current == seat;

        if (_hudAvatar[seat] != null)
        {
            var tex = DouSpireCharacters.Icon(c);
            // ⚠️ 本体立绘是游戏自己的 AssetCache 给的，换房间时会被回收成"已释放对象"。
            //    直接 SetTexture 会抛 ObjectDisposedException → 整桌停摆（godot.log:3685 实测）。
            //    所以**失效就退回无图**，绝不把异常放出去。
            if (!DouSpireCharacters.IsAliveTexture(tex)) tex = null;
            try
            {
                _hudAvatar[seat]!.Texture = tex;
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"斗地主：HUD 头像贴图已失效，退回无图（{e.GetType().Name}）。");
                tex = null;
                try { _hudAvatar[seat]!.Texture = null; } catch (System.Exception) { }
            }
            _hudAvatar[seat]!.Visible = tex != null;
        }

        // 三角已经是独立节点了（见 _hudTurn），名字里不再塞字符
        _hudName[seat].Text = p.Name;
        _hudName[seat].AddThemeColorOverride("font_color",
            turn ? Colors.White : DouSpireCharacters.ColorOf(c));

        // 「轮到谁」的大三角：亮 / 灭；换人时把呼吸动画归零（不然接着上一家的相位，看着像卡住）
        if (_hudTurn[seat] is { } mark && mark.Visible != turn)
        {
            mark.Visible = turn;
            mark.Scale = Vector2.One;
            if (turn) _turnMarkPhase = 0;
        }

        // 身份栏要分清三个阶段（桌上那副在正式开局前都是**预览牌**）：
        //   明牌 / 抢地主还没人抢 → "待定"
        //   抢地主有人抢了 / 加倍阶段 → 谁抢的是谁
        //   正式开局 → 按 _game
        // 2v2（2026-10-09）：没有地主，这行直接写**红队 / 蓝队**（用户拍板：
        //   "分红队蓝队打字写在信息框，不要用那个颜色线了"）——0+2 红队、1+3 蓝队。
        if (SeatCount == 4)
        {
            _hudTag[seat].Text = seat % 2 == 0 ? "红队" : "蓝队";
            _hudTag[seat].AddThemeColorOverride("font_color", TeamColor(seat));
        }
        else
        {
            var landlordPending = _askingShow || (_bidding && _bidHolder < 0);
            var isLordNow = _bidding || _askingDouble
                ? seat == _bidHolder
                : p.IsLandlord;
            _hudTag[seat].Text = landlordPending ? "待定" : (isLordNow ? "地主" : "农民");
            _hudTag[seat].AddThemeColorOverride("font_color",
                landlordPending ? new Color(0.72f, 0.70f, 0.62f)
                                : (isLordNow ? new Color(1f, 0.85f, 0.42f) : new Color(0.80f, 0.86f, 0.78f)));
        }

        // 剩几张：直接用大字说清楚（那条"剩牌进度条"谁也看不懂，已经删了）
        _hudCount[seat].Text = $"剩 {p.Hand.Count} 张";
        _hudCount[seat].AddThemeColorOverride("font_color",
            p.Hand.Count <= 3 ? new Color(1f, 0.52f, 0.42f) : Colors.White);

        _hudPlay[seat].Text = string.IsNullOrEmpty(_lastAction[seat]) ? "" : _lastAction[seat];

        // 钱（负数是红的，一眼看出谁输了）
        if (_hudMoney[seat] != null)
        {
            _hudMoney[seat].Text = _money[seat].ToString();
            _hudMoney[seat].AddThemeColorOverride("font_color",
                _money[seat] >= 0 ? new Color(1f, 0.86f, 0.42f) : new Color(1f, 0.45f, 0.40f));
        }

        // 2026-10-07：用户："这个很大的黄色指标，你没看见吗。不要了。太丑了"
        // → 回合光环整条撤掉，永远不显示。
        if (_seatGlow[seat] != null) _seatGlow[seat]!.Visible = false;

        // 轮到谁，谁的名字前有 ▶、HUD 全亮；别家压暗一点
        if (_hud[seat] != null) _hud[seat].Modulate = turn ? Colors.White : new Color(1f, 1f, 1f, 0.86f);
    }

    /// <summary>这一手是不是炸弹 / 王炸 —— 出牌音效要额外加一声破甲，听着更有劲。</summary>
    private static bool IsBomb(Combo? c) =>
        c is not null && (c.Type == ComboType.Bomb || c.Type == ComboType.Rocket);

    /// <summary>
    /// **每种牌型出牌时的特效** —— 越大的牌闪得越猛、字弹得越大。
    /// 用户 2026-10-07："每个都加点。三带一啊。连对。飞机啊啥的。都加"。
    /// 炸弹/王炸不在这儿（它们走 <see cref="PopMultiplier"/>，那边已经闪了）。
    /// </summary>
    private void ShowComboFx(Combo combo)
    {
        Color col;
        string label;

        // 2026-10-08 晚：每种牌型再叠一个**本体特效场景**（都先查过脚本才敢用：
        //   slime/sandy 挂共用粒子脚本 NVfxParticleSystem（和金币爆炸同款，不碰坐标、3 秒自毁）；
        //   starry 挂 NStarryImpactVfx，只放粒子不抢位置；heavy_blunt 会把位置覆盖回原点，
        //   和 bounce_spark 同款坑 —— 弃用）。位置 = 牌桌中央（见 TableCenter）。
        switch (combo.Type)
        {
            case ComboType.Single:
            case ComboType.Pair:
                col = new Color(1f, 1f, 1f); label = combo.TypeName;
                break;
            case ComboType.Triple:
            case ComboType.TripleWithSingle:
            case ComboType.TripleWithPair:
                col = new Color(1f, 0.92f, 0.55f); label = combo.TypeName;
                SpawnGameVfx("vfx/vfx_slime_impact", TableCenter(), 0.9f);
                break;
            case ComboType.Straight:
            case ComboType.ConsecutivePairs:
                col = new Color(0.45f, 0.85f, 1f); label = combo.TypeName;
                SpawnGameVfx("vfx/vfx_sandy_impact", TableCenter(), 0.9f);
                break;
            case ComboType.Plane:
            case ComboType.PlaneWithSingles:
            case ComboType.PlaneWithPairs:
            case ComboType.FourWithTwoSingles:
            case ComboType.FourWithTwoPairs:
                col = new Color(0.72f, 0.48f, 1f); label = combo.TypeName;
                SpawnGameVfx("vfx/vfx_starry_impact", TableCenter(), 1.0f);   // 星辰冲击，最大场面
                break;
            default:
                return;   // 炸弹 / 王炸：交给 PopMultiplier
        }

        PopText(label, col, combo.CardCount);   // 闪屏撤了，只留牌型名
    }

    /// <summary>
    /// 出牌时在桌面中央弹一个**牌型名**（"顺子""飞机"这种），张数越多字越大。
    /// 跟倍数那个大弹字共用一套手感，但小一号、位置低一点，免得两个撞在一起。
    /// </summary>
    private void PopText(string text, Color color, int cards)
    {
        try
        {
            var lbl = new Label
            {
                Text = text,
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            lbl.AddThemeColorOverride("font_color", color);
            lbl.AddThemeColorOverride("font_outline_color", new Color(0.10f, 0.07f, 0.03f));
            lbl.AddThemeConstantOverride("outline_size", Mathf.RoundToInt(DouSpireUi.Scaled(10)));
            // 张数越多字越大：3 张 → 56 号，14 张 → 约 120 号
            var size = Mathf.Clamp(DouSpireUi.Scaled(30 + cards * 9), DouSpireUi.Scaled(48), DouSpireUi.Scaled(130));
            lbl.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(size));

            var w = DouSpireUi.Px(900);
            var h = DouSpireUi.Px(220);
            lbl.PivotOffset = new Vector2(w / 2f, h / 2f);
            AddChild(DouSpireUi.Place(lbl, 0.5f, 0.40f, w, h));
            lbl.ZIndex = 180;

            lbl.Modulate = new Color(1f, 1f, 1f, 1f);
            lbl.Scale = new Vector2(0.7f, 0.7f);
            var tw = CreateTween();
            tw.TweenProperty(lbl, "scale", new Vector2(1.06f, 1.06f), 0.2)
              .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(lbl, "modulate:a", 0.0f, 0.45).SetDelay(0.22);
            tw.TweenCallback(Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(lbl)) lbl.QueueFree();
            }));
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：牌型弹字没出来（不影响玩）: {e.Message}");
        }
    }

    /// <summary>
    /// 播一个**游戏自带的特效场景**（粒子那种），比如 <c>"vfx/vfx_coin_explosion_jumbo"</c>。
    ///
    /// ⚠️ 参数是**内部路径**（`vfx/xxx`），**不是** `res://scenes/vfx/xxx.tscn`。
    ///    本体 `VfxCmd` 内部走 `SceneHelper.GetScenePath`（= `"res://scenes/" + path + ".tscn"`），
    ///    传完整 `res://` 会拼成 `res://scenes/res://...tscn.tscn` → **加载为空且不报错**。
    ///
    /// 走的是 **`VfxCmd.PlayNonCombatVfx`** —— 本体专门给「非战斗场景」留的入口，正好是我们的牌桌。
    /// 它内部已经把这四件事都做对了（本体所有播特效的地方都这么写）：
    ///   1. `PreloadManager.Cache.GetScene(path)` —— 走本体资源缓存（**没预载也会懒加载**，
    ///      只是会打一条 `Asset not cached:` 警告，不会失败）；
    ///   2. `Instantiate&lt;Node2D&gt;(PackedScene.GenEditState.Disabled)`；
    ///   3. `AddChildSafely`（非主线程/父级没 ready 时自动 CallDeferred）；
    ///   4. `GlobalPosition = position` —— 所以位置传**视口像素**即可，不用操心父级缩放。
    ///
    /// ── 2026-10-08 改（原实现的问题）──────────────────────────────
    /// 原来是自己 `ResourceLoader.Load` + `AddChild` + 设 `Position`，而且用的是
    /// `res://scenes/vfx/bounce_spark_vfx.tscn`（**破盾火花**）。反编译后确认那个场景根本不能这么用：
    ///   · 它的脚本 `NBounceSparkVfx._Ready()` 里 `base.GlobalPosition = _startPosition;`
    ///     —— **把我们设的位置直接覆盖掉**（`_startPosition` 默认 `(0,0)` 加一点随机抖动）；
    ///   · 同一处还 `base.Scale = ... * Rng.Chaotic.NextFloat(0.1f, 0.8f)` —— **缩放也被覆盖**；
    ///   · `_Process` 里按 `_gravity(0,1500)` 跑抛物线，并在 `_floorY` 处弹跳，
    ///     而 `_floorY` / 起点是从**战斗房间的怪物坐标**推的
    ///     （`NCombatRoom.Instance.GetCreatureNode(target).GetBottomOfHitbox()`）——
    ///     牌桌上压根没有战斗房间，`NCombatRoom.Instance` 是 null。
    ///   ⇒ 所以在牌桌上它要么落在原点附近、要么第一帧就"落地弹跳"飞出画面。
    ///     换成语义也更配斗地主的 `vfx_coin_explosion_jumbo`（筹码爆炸）：
    ///     纯 `GPUParticles2D`，脚本 `NVfxParticleSystem._Ready()` 只做两件事 ——
    ///     把子粒子的 `Emitting` 置 true、`_lifetime`(3s) 后自毁。**不碰位置、不碰缩放**。
    /// </summary>
    /// <summary>
    /// ★ 终局金币雨（2026-10-09 重做 v2）：赢了以后 40+ 枚金币从天上掉下来铺满桌子。
    ///
    /// 为什么不用本体粒子（<see cref="SpawnGameVfx"/>）：实测 VfxCmd 那条**节点生成了但看不见**
    /// —— 日志里"特效已播 @(960,475)"，桌面上一个粒子都没有（炸弹的筹码爆炸同理）。
    /// 不再赌本体的粒子场景，直接用我们自己 assets 里的金币图标 + Tween 控制下落/旋转/淡出，
    /// 挂在 <see cref="_scene"/> 层上 —— 我们自己画的，渲染层级就在自己手里。
    /// </summary>
    private void CoinRain() => CoinDrop(840, 0.03f, 0.97f);   // 用户："数量太少太少，直接 20 倍"

    /// <summary>下一场金币：count 枚，横跨 xMin~xMax（屏幕比例）。输了撒一小撮用的同一个入口。</summary>
    private void CoinDrop(int count, float xMin, float xMax)
    {
        var tex = DouSpireAssets.First(DouSpireAssets.GoldIcon);
        if (tex is null)
        {
            Entry.Logger.Warn("斗地主：金币图标没找到，金币雨退回本体粒子（看不见也认了）。");
            SpawnGameVfx("vfx/vfx_coin_explosion_jumbo", TableCenter(), 1.2f);
            return;
        }

        // 三档大小混着下，密起来才有"雨"的感觉；数量大时延迟拉长，别一帧全砸下来
        float[] sizeMul = { 0.75f, 1.0f, 1.35f };
        var delayMax = count > 200 ? 2.4f : 1.0f;
        var stage = _scene.Size;
        for (var i = 0; i < count; i++)
        {
            var size = DouSpireUi.Px(64) * sizeMul[System.Random.Shared.Next(sizeMul.Length)];
            var coin = new TextureRect
            {
                Texture = tex,
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Size = new Vector2(size, size),
                PivotOffset = new Vector2(size / 2f, size / 2f),
            };
            var x = xMin + (float)System.Random.Shared.NextDouble() * (xMax - xMin);
            coin.Position = new Vector2(x * stage.X - size / 2f, -size * 1.5f);
            _scene.AddChild(coin);

            // 每枚：随机延迟 → 加速下落（边落边转）→ 落地弹一下 → 淡出自毁
            var delay = (float)System.Random.Shared.NextDouble() * delayMax;
            var fall = 0.55f + (float)System.Random.Shared.NextDouble() * 0.65;
            var landY = stage.Y * (0.50f + (float)System.Random.Shared.NextDouble() * 0.36f);
            var drift = (float)GD.RandRange(-70, 70);
            var spin = (float)GD.RandRange(-3.2, 3.2);

            var tw = CreateTween();
            tw.TweenInterval(delay);
            tw.TweenProperty(coin, "position",
                    new Vector2(coin.Position.X + drift, landY), fall)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tw.Parallel().TweenProperty(coin, "rotation", spin, fall);
            tw.TweenProperty(coin, "position:y", landY - size * 0.9f, 0.13)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(coin, "position:y", landY, 0.15)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tw.TweenInterval(0.15);
            tw.TweenProperty(coin, "modulate:a", 0.0f, 0.4);
            tw.TweenCallback(Callable.From(coin.QueueFree));
        }
    }

    private Node2D? SpawnGameVfx(string innerPath, Vector2 at, float scale = 1f)
    {
        try
        {
            // ── 2026-10-09 重写：不再走 VfxCmd.PlayNonCombatVfx ─────────────
            // VfxCmd 把节点挂到**外面的场景**里，而牌桌根节点 ZIndex=200 整层盖在上面
            // → 日志里"特效已播"但桌面上一粒粒子都看不见（金币雨当初的同一个病）。
            // 现在自己实例化、自己挂到 _scene 层（跟金币雨同一层）：
            // 渲染层级、坐标、缩放全在我们手里，保证看得见。
            var resPath = "res://scenes/" + innerPath + ".tscn";

            PackedScene? scene = null;
            try { scene = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetScene(resPath); }
            catch (System.Exception e)
            {
                DouSpireAssets.Diag($"特效缓存取不到，改走直载  {innerPath}: {e.Message}");
            }
            scene ??= Godot.ResourceLoader.Load<PackedScene>(resPath);   // ⚠️ 全限定：MegaCreat.Assets 会遮蔽 Godot.ResourceLoader
            if (scene is null)
            {
                Entry.Logger.Warn($"斗地主：特效场景加载为空 {resPath}");
                return null;
            }

            var node = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
            if (node is null)
            {
                Entry.Logger.Warn($"斗地主：特效场景实例化为空 {resPath}");
                return null;
            }

            _scene.AddChild(node);
            // _scene 铺满视口且 scale=1 → 本地坐标就是视口像素（口径同 TableCenter）。
            node.Position = at;
            // 相对 ZIndex：牌桌根 200 + 40 = 240，压过所有牌和 HUD，保证特效永远在最上面。
            node.ZIndex = 40;
            if (!Mathf.IsEqualApprox(scale, 1f)) node.Scale = Vector2.One * scale;
            Entry.Logger.Info($"斗地主：特效已播 @{at} ×{scale:0.##}  {innerPath}");
            DouSpireAssets.Diag($"特效  {innerPath}  @{at} ×{scale:0.##}");

            // 兜底清理：粒子场景一般自己 3s 自毁；没有自毁脚本的（如贴图翻页那种）靠这条收尸。
            GetTree().CreateTimer(6.0).Timeout += () =>
            {
                if (GodotObject.IsInstanceValid(node)) node.QueueFree();
            };
            return node;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：特效播不出来 {innerPath}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 在**某个座位的立绘胸口**播一个本体特效 —— 出牌人专属的落点。
    /// 胸口 = 立绘中心往上抬一截（ArtY - 0.08），炸闪/蓄力/加冕都打在这儿才像"长在他身上"。
    /// </summary>
    private void SeatVfx(int seat, string innerPath, float scale = 1f)
    {
        try
        {
            var stage = _scene.Size;
            var lay = DouSpireLayout.Seats[Mathf.Clamp(seat, 0, DouSpireLayout.Seats.Length - 1)];
            SpawnGameVfx(innerPath, new Vector2(lay.ArtX * stage.X, (lay.ArtY - 0.08f) * stage.Y), scale);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：座位特效没播出来（不影响玩）: {e.Message}");
        }
    }

    /// <summary>
    /// **整屏闪一下**（炸弹 / 王炸 / 赢输 用）。
    /// 用户 2026-10-07："画面闪一下"。
    /// 做法就是铺一张全屏纯色贴图上透明度做 Tween —— 稳、不依赖任何外部资源。
    /// </summary>
    private void FlashScreen(Color color, float peak = 0.55f, double fade = 0.45)
    {
        try
        {
            var flash = new ColorRect
            {
                Color = new Color(color.R, color.G, color.B, 0f),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            // 铺满整屏，压在牌桌最上面
            DouSpireUi.Fill(flash);
            flash.ZIndex = 200;
            AddChild(flash);

            var tw = CreateTween();
            tw.TweenProperty(flash, "color:a", peak, 0.07);      // 猛地亮起来
            tw.TweenProperty(flash, "color:a", 0.0, fade);       // 再散掉
            tw.TweenCallback(Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(flash)) flash.QueueFree();
            }));
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：闪屏没做出来（不影响玩）: {e.Message}");
        }
    }

    /// <summary>牌桌中央（视口像素）—— 大特效的默认落点，口径同 <see cref="PopMultiplier"/>。</summary>
    private Vector2 TableCenter()
    {
        // ⚠️ `VfxCmd.PlayNonCombatVfx` 设的是 **GlobalPosition**，所以这里给**视口像素**。
        //    牌桌根 Control 被 `FitToViewport()` 铺满视口且 scale=1 → `Size` 就是视口尺寸。
        //    量不到尺寸时退回 1920×1080 设计稿口径。
        var vs = Size;
        if (vs.X < 8f || vs.Y < 8f) vs = new Vector2(DouSpireUi.Px(1920f), DouSpireUi.Px(1080f));
        return new Vector2(vs.X * 0.5f, vs.Y * 0.44f);
    }

    /// <summary>
    /// 倍数翻倍的时候，在**牌堆中央**弹一个大字（明牌 / 加倍 / 炸弹 都会调它）。
    /// 用户 2026-10-07："倍数会翻倍用一个动画出现在牌堆中央"。
    /// </summary>
    private void PopMultiplier(string reason)
    {
        if (_multiplierPop is null) return;
        try
        {
            // 闪屏撤了 —— 用户 2026-10-07："我不喜欢闪屏。啥用没有啊，还影响观感。"

            // 放**游戏自带的筹码爆炸**（`vfx_coin_explosion_jumbo`）：
            //   · 语义跟斗地主最配（筹码炸开）；
            //   · 它是纯粒子系统，脚本 `NVfxParticleSystem` 不碰位置/缩放 ——
            //     而原来那个 `bounce_spark_vfx` 会被自己的脚本抢走 `GlobalPosition` 和 `Scale`，
            //     还会按「战斗房间的地面」做重力弹跳（牌桌上没有战斗房间）。详见 `SpawnGameVfx`。
            //
            // ⚠️ 位置口径见 <see cref="TableCenter"/>。
            SpawnGameVfx("vfx/vfx_coin_explosion_jumbo", TableCenter(), MultiplierVfxScale);

            _multiplierPop.Text = $"{reason}　×{_multiplier}";
            _multiplierPop.Modulate = new Color(1f, 1f, 1f, 1f);
            _multiplierPop.Scale = new Vector2(0.55f, 0.55f);
            _multiplierPop.Visible = true;

            var tw = CreateTween();
            tw.TweenProperty(_multiplierPop, "scale", new Vector2(1.12f, 1.12f), 0.26f)
              .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(_multiplierPop, "modulate:a", 0.0f, 0.55f).SetDelay(0.4f);
            tw.TweenCallback(Callable.From(() =>
            {
                if (_multiplierPop is not null) _multiplierPop.Visible = false;
            }));
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：倍数动画没放出来（不影响玩）: {e.Message}");
        }
    }

    /// <summary>
    /// 每局开局重新发筹码。
    ///
    /// **普通开桌（F9 / 主菜单）**：三家各 1000，纯记分板，不碰真实金币。
    ///
    /// **篝火赌注模式**（<see cref="_stakePlayer"/> 非 null）：**三家都发我身上的真实金币**。
    /// <list type="bullet">
    ///   <item>对手不再是 1000 —— 我揣 99 本金、对手揣 1000，等于让我白嫖（用户："ai应该跟我的钱差不多"）。</item>
    ///   <item>「一份底注」= 本金 ÷ <c>BetUnitDivisor</c>（下限 1，上限仍是设置里的底注）。</item>
    ///   <item><b>封顶</b>：一份不超过 本金 × <c>BetMaxSwingPercent</c>% ÷ 2。
    ///         地主一个人吃两份，所以一局最多让我输赢本金的那个百分比 ——
    ///         不然一把「明牌+加倍+炸弹」的 ×64 会把 99 金币的人一次打回零元。
    ///         归一化后倍数照常显示（牌局的乐趣保留），只是金币按封顶结算。</item>
    ///   <item>身上 0 金币（或读出来是负数）⇒ **不赌**：这一桌按纯记分板跑，绝不往真实金币上写
    ///         —— 「如果我是零元就拒绝我斗地主」「不能让我白手起家」。
    ///         （正常情况下根本走不到这里，入口 <see cref="DouSpireRestSiteOption.OnSelect"/> 已经拦了；
    ///          这是兜底，防止别的调用路径绕过入口。）</item>
    /// </list>
    /// </summary>
    private void ResetMoney()
    {
        var wallet = -1;
        var player = _stakePlayer;
        if (player is not null)
        {
            try { wallet = player.Gold; }
            catch (System.Exception e) { Entry.Logger.Warn($"斗地主：读不到身上金币（按 1000 发）: {e.Message}"); }
        }

        // ① 普通开桌：纯记分板
        if (wallet < 0)
        {
            _stakeUnit = DouSpireConfig.BaseStake;
            _stakeCap = 0;
            _stakeCapped = false;
            if (_moneySeeded)
            {
                // 这个牌桌会话已经发过筹码了 → **接着算**，别重发 1000。
                // 用户 2026-10-08 晚："打完一局又变1000了，我本来是负数的。应该继续计算" ——
                // 连着「再来一局」时，上一局输成负数就背着负数打下一局。
                // （收桌重开 = 新的桌子 = 重新发 1000，这是合理的"新一坐"。）
                Entry.Logger.Info("斗地主：记分板接着算 —— 三家筹码 "
                                  + string.Join(" / ", _money) + "（不重发 1000）。");
                return;
            }
            for (var s = 0; s < SeatCount; s++) _money[s] = 1000;
            _moneySeeded = true;
            return;
        }

        // ② 赌注模式但我身上没钱 ⇒ 关掉赌注（牌照样能打，金币一分不动）
        if (wallet <= 0)
        {
            _stakePlayer = null;
            for (var s = 0; s < SeatCount; s++) _money[s] = 1000;
            _stakeUnit = DouSpireConfig.BaseStake;
            _stakeCap = 0;
            _stakeCapped = false;
            Entry.Logger.Warn("斗地主：篝火赌注开局时身上是 0 金币 —— 这一桌改成纯记分板，不碰真实金币。");
            return;
        }

        // ③ 赌注模式：三家都发我身上的钱
        for (var s = 0; s < SeatCount; s++) _money[s] = wallet;

        // 一份底注 = 本金的 1/BetUnitDivisor，夹在 [1, 设置里的底注]
        var unit = wallet / Mathf.Max(1, DouSpireConfig.BetUnitDivisor);
        if (unit < 1) unit = 1;
        if (unit > DouSpireConfig.BaseStake) unit = DouSpireConfig.BaseStake;
        _stakeUnit = unit;

        // 封顶：地主一个人对两个农民 ⇒ 一份的上限 = 本金 × pct% ÷ 2
        var pct = Mathf.Clamp(DouSpireConfig.BetMaxSwingPercent, 1, 100);
        _stakeCap = Mathf.Max(1, wallet * pct / 100 / 2);
        _stakeCapped = false;

        Entry.Logger.Info($"斗地主：篝火赌注开局 —— 三家各 {wallet} 金币，一份 {_stakeUnit}，"
                          + $"一份封顶 {_stakeCap}（一局最多输赢 {pct}% 本金）。");
    }

    /// <summary>
    /// 结算这一局的钱。斗地主的账：
    /// **地主一个人对两个农民** —— 谁赢就从对方每人手里拿一份「底注 × 倍数」。
    /// 所以地主赢 = 自己 +2 份、两个农民各 -1 份；农民赢反过来。
    /// 只结一次（<c>_moneyPaidOut</c> 挡着）。
    ///
    /// **封顶**（赌注模式）：一份不超过 <see cref="_stakeCap"/> ——
    /// 倍数是牌局的东西（明牌/加倍/炸弹照乘不误，HUD 也照显示），
    /// 金币是钱包的东西，一局最多动本金的一个固定百分比。撞上了会在结算文案里写明。
    /// </summary>
    private void PayOut(int winnerSeat)
    {
        if (_moneyPaidOut) return;
        _moneyPaidOut = true;

        var stake = _stakeUnit * _multiplier;   // 一份 = 本局底注 × 倍数
        _stakeCapped = _stakeCap > 0 && stake > _stakeCap;
        if (_stakeCapped) stake = _stakeCap;

        var landlordWon = _game.Players[winnerSeat].IsLandlord;

        var myDelta = 0;
        var fourP = _game.Players.Count == 4;
        for (var s = 0; s < SeatCount; s++)
        {
            var isLandlord = _game.Players[s].IsLandlord;
            var delta = 0;
            if (fourP)
            {
                // 2v2：按队算 —— 赢家队每人 +1 份、输家队每人 −1 份（零和，没有地主）
                delta = _game.Players[s].Team == _game.Players[winnerSeat].Team ? stake : -stake;
            }
            else if (landlordWon)
                delta = isLandlord ? stake * 2 : -stake;
            else
                delta = isLandlord ? -stake * 2 : stake;

            _money[s] += delta;
            if (s == MeSeat) myDelta = delta;
            Entry.Logger.Info($"斗地主：{_game.Players[s].Name} {(delta >= 0 ? "+" : "")}{delta}"
                              + $"（底注{_stakeUnit} × 倍数{_multiplier}"
                              + (_stakeCapped ? $"→封顶{stake}" : "") + $"）→ {_money[s]}");
        }

        SettleGold(myDelta);

        // ★ 联机 + 我是主机：把**其他人**的输赢也写进他们各自的 Player（主机是权威，
        //   游戏自己的状态同步会把结果带到各端）。用户 2026-10-08：「读取身上的钱」。
        if (DouSpireNet.Active && DouSpireNet.IsHost) SettleAllGold(winnerSeat);

        Refresh();
    }

    /// <summary>
    /// 篝火模式：把这一局**我这一席的输赢**落到玩家的真实金币上（1 筹码 = 1 金币）。
    ///
    /// 直接改 <c>Player.Gold</c>，**不走 <c>PlayerCmd.GainGold/LoseGold</c>** ——
    /// 那两个是 async 且要经过 <c>Hook.ModifyGoldGained(runState, Creature.CombatState, …)</c>，
    /// 而篝火处没有战斗、<c>CombatState</c> 可能是 null，容易在钩子里炸。
    /// 代价是绕过「加钱加成」类遗物、也不进本局的金币统计 —— 对一个彩蛋玩法可以接受。
    /// 输了不会变负数（钳到 0）。
    /// </summary>
    private void SettleGold(int myDelta)
    {
        var player = _stakePlayer;
        if (player is null || myDelta == 0) return;

        ApplyGold(player, myDelta);
    }

    /// <summary>
    /// 把某位玩家的真实金币按 delta 调整（钳 0，写日志）。只做加法，不做任何"是谁"的判断。
    /// </summary>
    private static void ApplyGold(Player player, int delta)
    {
        try
        {
            var before = player.Gold;
            player.Gold = Mathf.Max(0, before + delta);
            var who = DouSpireCharacters.Name(player.Character);
            var line = $"斗地主：篝火金币结算 {who} {before} {(delta >= 0 ? "+" : "")}{delta} → {player.Gold}";
            Entry.Logger.Info(line);
            WriteDiag(line);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：篝火金币结算失败（牌局本身不受影响）: {e.Message}");
        }
    }

    /// <summary>
    /// **联机时把每个人的输赢都落到真实金币上** —— 用户 2026-10-08：「读取身上的钱」。
    ///
    /// 以前只结算"我"这一席（<see cref="SettleGold"/>），其他人的金币**谁都不写**，
    /// 主机的权威状态里他们的钱一分没动 ⇒ 各端看到的钱对不上。
    ///
    /// 现在分两种情况：
    /// <list type="bullet">
    ///   <item>**联机 + 我是主机**：主机是权威，把**每个真人席**的输赢都写进他的 Player
    ///     （座位→Player 是主机开桌时留下的，见 <see cref="DouSpireNet.PlayerOfSeat"/>），
    ///     游戏自己的玩家状态同步会把结果带到各端。</item>
    ///   <item>**联机 + 我是客机**：只写我自己那席（主机也在写同一笔，两边算出来一样）；
    ///     写不了别人的 —— 客机也没有权限改别人的权威数据。</item>
    ///   <item>**单人**：跟以前一模一样。</item>
    /// </list>
    /// </summary>
    private void SettleAllGold(int winnerSeat)
    {
        var stake = _stakeUnit * _multiplier;   // 一份 = 本局底注 × 倍数
        if (_stakeCap > 0 && stake > _stakeCap) stake = _stakeCap;
        var fourP = _game.Players.Count == 4;
        var landlordWon = _game.Players[winnerSeat].IsLandlord;
        var winnerTeam = _game.Players[winnerSeat].Team;

        for (var s = 0; s < SeatCount; s++)
        {
            var delta = 0;
            if (fourP)
            {
                // 2v2：按队算（赢家队每人 +1 份、输家队 −1 份），和 PayOut 完全一致
                delta = _game.Players[s].Team == winnerTeam ? stake : -stake;
            }
            else if (landlordWon) delta = _game.Players[s].IsLandlord ? stake * 2 : -stake;
            else delta = _game.Players[s].IsLandlord ? -stake * 2 : stake;
            if (delta == 0) continue;

            // 自己那席已经在 PayOut 里结算过了，这里只补"别人"的
            if (s == MeSeat) continue;

            var p = DouSpireNet.PlayerOfSeat(DouSpireNet.ToGlobal(s));   // 本地席 → 全局席 → 玩家
            if (p is null) continue;
            ApplyGold(p, delta);
        }
    }

    /// <summary>
    /// 结算文本 + **本局倍数**（明牌 ×2、各家加倍、炸弹都会乘上来）。
    /// 倍数 = 1 时就不啰嗦了，直接给原文。
    /// 撞上金币封顶时补一句 —— 否则玩家看着「×64」却只拿到一点点，会以为是 bug。
    /// </summary>
    private string ResultWithMultiplier()
    {
        var txt = _game.ResultText();
        if (_multiplier <= 1) return txt;
        var extra = _stakeCapped
            ? $"\n（本局倍数 ×{_multiplier}　金币按封顶 {_stakeCap}/份 结算）"
            : $"\n（本局倍数 ×{_multiplier}）";
        return txt + extra;
    }

    private string StatusText()
    {
        if (_game.Winner is not null) return ResultWithMultiplier();
        if (_game.Current != MeSeat)
        {
            var who = _game.Players[_game.Current].Name;
            // 主机等远程真人：把"还有几秒就替他出"报出来，免得看着像卡死了
            if (_netOn && _netHost && DouSpireNet.IsHumanSeat(DouSpireNet.ToGlobal(_game.Current)))
            {
                var limit = DouSpireConfig.TurnTimeLimit > 0 ? DouSpireConfig.TurnTimeLimit + 2.0 : 30.0;
                var left = Mathf.Max(0.0, limit - _remoteWait);
                return $"等着 {who} 出牌……（{left:0} 秒没动就替他出）";
            }
            return $"等着 {who} 出牌……";
        }
        if (_selected.Count == 0)
            return _game.MustLead
                ? "轮到你领出 —— 随便出什么都行"
                : $"轮到你跟牌 —— 场上 {_game.LastPlay!.TypeName}，得出更大的";

        var picked = _game.Players[MeSeat].Hand.Where(_selected.Contains).ToList();
        var parsed = _game.Judge.TryParse(picked);
        if (parsed is null) return $"已选 {picked.Count} 张 —— 这手不成牌型";
        if (!ComboJudge.Beats(parsed, _game.LastPlay))
            return $"已选 {parsed.TypeName} —— 压不住场上的 {_game.LastPlay!.TypeName}";
        return $"已选 {Describe(parsed)} —— 可以出";
    }

    private void RefreshButtons()
    {
        // 明牌 / 抢地主 / 加倍阶段：牌局还没开始，出牌那三个按钮一律不亮
        //（桌上摆的是"预览牌"，让它们生效会把预览局打乱）
        var locked = PlayLocked();
        var myTurn = !locked && _game.Winner is null && _game.Current == MeSeat;
        _playBtn.Disabled = !myTurn || _selected.Count == 0;
        _passBtn.Disabled = !myTurn || _game.MustLead;
        _hintBtn.Disabled = !myTurn;
        // 结算后留 2.5s 庆祝窗口（金币雨 + 胜利语音），按钮再亮 —— 免得手一点庆祝就没了
        var celebrated = Time.GetTicksMsec() / 1000.0 - _roundOverAt >= 2.5;
        var over = !locked && _game.Winner is not null && celebrated;
        _nextBtn.Visible = over;
        if (_repickBtn != null) _repickBtn.Visible = over;   // 和「再来一局」同进同退

        // 出牌三件套的**显隐**也在这里统一管（之前只管 Disabled）：
        //   牌局一结束 / 还没开始（选人、明牌、猜拳、加倍、扣牌）就藏掉 ——
        //   否则结算那一排「出牌 要不起 提示 再来一局 重新选人 鸡煲代打」六个按钮
        //   挤成一长排，左边的会压到左下角的日志面板（用户 2026-10-09："会不会和再开一局和重新选人挡住"）。
        //   平时（出牌阶段）照旧亮着；各问话阶段的显隐本来也归 ShowRps / ShowDouble 管，口径一致。
        var trioOn = !locked && _rpsStep == RpsStep.Off && _game.Winner is null;
        _playBtn.Visible = trioOn;
        _passBtn.Visible = trioOn;
        _hintBtn.Visible = trioOn;
        _playBtn.Text = _selected.Count > 0 ? $"出牌（{_selected.Count}）" : "出牌";
    }

    // ════════════════════════════════════════════════════════
    //  联机收信口 —— 全部由 DouSpireNet 调，参数一律是**本地视角**的座位号
    //
    //  规则很简单：
    //    主机收到的是"客机的表态/出牌"，它校验完再广播出去；
    //    客机收到的是"主机的广播"，照做就行。
    //  两边的落地点都只有 Apply* 那几个函数，所以牌局永远同步。
    // ════════════════════════════════════════════════════════

    // ⚠️ 这四个"问话阶段"的收信口，主机收到客机的表态后**必须回声广播**。
    //   原因：客机在 ShowCardsSay / RpsWantSay / RpsThrowSay / DoubleSay 里**只上报、不本地落地**
    //   （等主机确认），所以它自己那一席的 _showDone/_wantDone/_throwDone/_doubleDone 只能靠
    //   主机广播回来才置位。不回播的话客机的"三家都表态了吗"永远差自己这一票 →
    //   永远卡在"等你对面表态…"上（2026-10-08 自检抓到）。
    //   顺序：**先回声、再本地落地** —— 落地可能顺带触发下一阶段的广播（比如 BeginThrow 会广播 AI 的拳），
    //   回声排在这些前面，客机收到的消息次序才和主机的状态机推进次序一致。

    /// <summary>明牌表态（客机→主机 或 主机广播）。</summary>
    internal void NetShow(int localSeat, bool want)
    {
        if (!_netOn || !_askingShow) return;
        if (_netHost) DouSpireNet.CastShow(DouSpireNet.ToGlobal(localSeat), want);
        ApplyShow(localSeat, want);
    }

    /// <summary>抢地主意向。</summary>
    internal void NetWant(int localSeat, bool want)
    {
        if (!_netOn || !_bidding) return;
        if (_netHost) DouSpireNet.CastWant(DouSpireNet.ToGlobal(localSeat), want);
        ApplyWant(localSeat, want);
    }

    /// <summary>出拳。</summary>
    internal void NetThrow(int localSeat, int gesture)
    {
        if (!_netOn || !_bidding) return;
        if (_netHost) DouSpireNet.CastThrow(DouSpireNet.ToGlobal(localSeat), gesture);
        ApplyThrow(localSeat, gesture);
    }

    /// <summary>加倍表态。</summary>
    internal void NetDouble(int localSeat, bool want)
    {
        if (!_netOn || !_askingDouble) return;
        if (_netHost) DouSpireNet.CastDouble(DouSpireNet.ToGlobal(localSeat), want);
        ApplyDouble(localSeat, want);
    }

    /// <summary>主机宣布正式开局。</summary>
    internal void NetStart(int localLandlordSeat)
    {
        if (!_netOn) return;
        if (_roundStarted) return;                     // 已经开过了，别重复发牌
        StartRound(_bidSeed, localLandlordSeat);
    }

    /// <summary>
    /// 主机下令推进猜拳阶段：0 = 定格亮拳，1 = 平局重出，2 = 定完地主、进加倍。
    ///
    /// 为什么这几拍不能各端自己数秒：两端的计时器差着一个网络往返，主机先动、
    /// 消息比客机的计时器先到，客机还停在上一拍，收下来会被当"重复"丢掉。
    /// 所以**只有主机推进**，客机照着走。
    /// </summary>
    internal void NetRpsPhase(int phase, int localLandlordSeat)
    {
        if (!_netOn || _netHost) return;
        switch (phase)
        {
            case 0:
                BeginFreeze();
                break;
            case 1:
                PopText("平局 —— 再出一拳", DouSpireUi.Gold, 4);
                BeginThrow();
                break;
            default:
                _bidHolder = localLandlordSeat;
                EndRps();
                break;
        }
    }

    /// <summary>出牌。</summary>
    internal void NetPlay(int localSeat, List<Card> cards)
    {
        if (!_netOn) return;

        if (_netHost)
        {
            // 主机：这是客机上报的一手 —— 校验；过了就落地 + 广播给其他人（自己不用收广播）
            if (!ApplyPlay(localSeat, cards, out var err))
            {
                Entry.Logger.Warn($"斗地主：{localSeat} 号位报上来的牌没过（{err}），打回去。");
                DouSpireNet.CastErr(DouSpireNet.ToGlobal(localSeat), err);
                return;
            }
            DouSpireNet.CastPlay(DouSpireNet.ToGlobal(localSeat), cards);
            return;
        }

        // 客机：照主机说的做
        if (!ApplyPlay(localSeat, cards, out var e2) && e2.Length > 0)
        {
            Entry.Logger.Warn($"斗地主：主机广播的出牌没落地（{e2}）—— 可能已经不同步了，建议重开一桌。");
            WarnDesync();
        }
    }

    /// <summary>
    /// 客机发现"主机的广播落不了地" = 这桌已经分叉了。
    /// 只写日志的话玩家只会看到**一桌永远不动的牌**，不知道该怎么办
    /// （用户 2026-10-09 原话："主机打牌了，客机显示还没打，还在等待主机出牌"）——
    /// 所以弹一次提示，告诉他点哪儿能救。**每分钟最多一次**，绝不刷屏。
    /// </summary>
    private void WarnDesync()
    {
        if (_netHost) return;
        var now = Time.GetTicksMsec() / 1000.0;
        if (now - _desyncToastAt < 60.0) return;
        _desyncToastAt = now;
        Entry.Toast("这桌和主机对不上了 —— 点「重新选人」或「再来一局」重开一局。", "斗地主");
    }

    /// <summary>要不起。</summary>
    internal void NetPass(int localSeat)
    {
        if (!_netOn) return;

        if (_netHost)
        {
            if (!ApplyPass(localSeat, out var err))
            {
                Entry.Logger.Warn($"斗地主：{localSeat} 号位报上来的要不起没过（{err}）。");
                DouSpireNet.CastErr(DouSpireNet.ToGlobal(localSeat), err);
                return;
            }
            DouSpireNet.CastPass(DouSpireNet.ToGlobal(localSeat));
            return;
        }

        if (!ApplyPass(localSeat, out var e2) && e2.Length > 0)
        {
            Entry.Logger.Warn($"斗地主：主机广播的要不起没落地（{e2}）。");
            WarnDesync();
        }
    }

    /// <summary>
    /// 2v2 扣牌阶段「某座位扣了某张牌」—— 主机权威，全桌唯一通路。
    ///
    /// ★ 2026-10-10 加：联机扩到 4 席后，2v2 的"后手队每家扣一张"也必须走网络，
    ///   否则各端各扣各的 → 一开局就分叉。规则和出牌一模一样：
    ///   · 主机：这是客机上报的 → 校验（这个座位确实待扣、这张牌确实在他手里）→ 落地 + 广播；
    ///   · 客机：照主机广播的做。
    /// </summary>
    internal void NetDiscard(int localSeat, Card card)
    {
        if (!_netOn) return;

        if (_netHost)
        {
            if (!ApplyDiscard(localSeat, card, out var err))
            {
                Entry.Logger.Warn($"斗地主：{localSeat} 号位报上来的扣牌没过（{err}），打回去。");
                DouSpireNet.CastErr(DouSpireNet.ToGlobal(localSeat), err);
                return;
            }
            DouSpireNet.CastDiscard(DouSpireNet.ToGlobal(localSeat), card);
            return;
        }

        if (!ApplyDiscard(localSeat, card, out var e2) && e2.Length > 0)
        {
            Entry.Logger.Warn($"斗地主：主机广播的扣牌没落地（{e2}）。");
            WarnDesync();
        }
    }

    /// <summary>落地一次扣牌（主机校验后 / 客机收到广播后都走这里）。</summary>
    private bool ApplyDiscard(int localSeat, Card card, out string error)
    {
        error = "";
        if (_game is null) { error = "牌局还没建好"; return false; }
        if (localSeat < 0 || localSeat >= SeatCount) { error = "座位号不对"; return false; }
        if (!_game.PendingDiscards.Contains(localSeat)) { error = "这一席不用扣牌"; return false; }
        if (!_game.Players[localSeat].Hand.Contains(card)) { error = "这张牌不在他手里"; return false; }
        if (!_game.TryDiscard(localSeat, card, out error)) return false;

        Entry.Logger.Info($"斗地主（2v2）：{_game.Players[localSeat].Name} 扣 {GameState.CardName(card)}");
        ActorPlay(localSeat, a => a.PlayCombo(ComboType.Single));   // 扣牌 = 打出一张的单张动作
        _discardWait = 0;                                           // 换下一席：远程等待计时清零
        ShowDiscardBtn(_game.PendingDiscards.Contains(MeSeat));
        if (_game.PendingDiscards.Count == 0) EndDiscard();
        else Refresh();
        return true;
    }

    /// <summary>三家都不抢 / 猜拳定不出地主 → 主机换了新种子重发。</summary>
    internal void NetNewRound(int seed)
    {
        if (!_netOn) return;
        Entry.Logger.Info($"斗地主：主机开新一局（种子 {seed}）—— 全桌用这一副，重新发。");
        // 开局了 → 选人阶段的"准备票"全部作废（下一轮重新选人时要重新点）
        for (var s = 0; s < _pickReady.Length; s++) _pickReady[s] = false;
        DouSpireNet.ClearReady();
        DropRound();          // 上一局的存档残留清掉（和「再来一局」按钮同一条口径）
        _dealSeed = seed;
        _redealCount = 0;
        BeginShowCards();
    }

    /// <summary>
    /// 主机：定一个新种子，广播给全桌并本地开这一局。
    /// 被「再来一局 / 开始打牌」和"客机请求开新一局"两处调用。
    /// </summary>
    internal void NetHostStartNewRound()
    {
        if (!_netOn || !_netHost) return;
        var seed = System.Random.Shared.Next();
        Entry.Logger.Info($"斗地主：主机开新一局（种子 {seed}），广播给全桌。");
        DouSpireNet.CastNewRound(seed);
        NetNewRound(seed);
    }

    /// <summary>
    /// 「再来一局」/「开始打牌」 —— **联机时必须由主机定种子**。
    ///
    /// ★★ 2026-10-09 修：原来两个按钮都是 `DropRound(); Deal();`，而 `Deal()` 里
    ///    `_dealSeed = System.Random.Shared.Next()` 是**各端各自随机**，这条链路更是
    ///    **一个网络包都不发** ⇒ 联机桌上两端发到的根本不是同一副牌，一开局就分叉。
    ///    用户实测：第一局打完点「再来一局」→ 第二局立刻
    ///    「主机广播的出牌没落地（手里没有 3）」，紧接着「还没轮到这一家」，整局作废。
    /// </summary>
    private void NewDeal()
    {
        DropRound();

        if (!_netOn) { Deal(); return; }

        if (_netHost) { NetHostStartNewRound(); return; }

        Entry.Toast("已请主机开新一局 —— 等他按「再来一局」。", "斗地主");
        _statusLabel.Text = "已请主机开新一局…";
        DouSpireNet.SendNewRound();
    }

    /// <summary>主机打回了我的操作（牌局不同步 / 不该我出牌）。</summary>
    internal void NetErr(int localSeat, string msg)
    {
        if (!_netOn) return;
        Entry.Toast(string.IsNullOrWhiteSpace(msg) ? "主机没认这一手" : msg, "出不了");
        if (localSeat == MeSeat) _selected.Clear();
        Refresh();
    }

    /// <summary>某一席离桌 → 改由电脑接管（主机决定的，广播给所有人）。</summary>
    internal void NetSeatAi(int localSeat)
    {
        if (!_netOn) return;
        Entry.Logger.Info($"斗地主：{localSeat} 号位现在由电脑接管。");

        // ★★ 2026-10-10 修（用户实测崩栈，P0）：
        //   `DouSpireTable.NetSeatAi → NullReferenceException`，调用链
        //   `HandleIn → HostGotLeave → Live.NetSeatAi`。
        //   根因：**选人阶段 `_game` 还是 null**（牌要等大家都点「开始打牌」才发，
        //   见 `ReturnToPickerForNet`），而下面那句无条件读 `_game.Players[...].Name`
        //   —— 队友在**选人界面**离桌就当场炸。
        //   炸了以后 `HandleIn` 的 catch 把异常吞掉，他那一席的后续处理全断，
        //   主机就停在"还差 1 个真人"上，看起来就是"卡死"。
        //   现在分两段：牌局还没建 = 只管选人界面；建好了 = 走原来那套。
        if (_game is null)
        {
            if (_picking)
            {
                // 他那一票作废（万一他以后又补回来，不该白捡一个"已准备"）
                if (localSeat >= 0 && localSeat < _pickReady.Length) _pickReady[localSeat] = false;
                RefreshPicker();
            }
            return;
        }

        if (localSeat < 0 || localSeat >= _game.Players.Count) return;
        if (localSeat != MeSeat) Entry.Toast($"{_game.Players[localSeat].Name} 离桌了，电脑替他打。", "斗地主");

        // ⚠️ 光把座位标成 AI 是不够的（2026-10-08 自查抓到的死锁）：
        //    如果此刻正好卡在"等他表态"或"等他出拳"，那三道闸门
        //    （AllDone(_showDone/_wantDone/_doubleDone) 和 AllThrowDone()）永远等不到他那一票
        //    —— 而 NetTickAskTimeout 只盯**真人**席，改完 AI 就不再盯它了 → 全桌永久卡死。
        //    所以这里立刻替他把当前这一拍补上。
        //    四个 HostDecideAi* 都是幂等的（内部只看 IsHumanLocal + Apply* 的 *Done 标志），
        //    重跑一遍不会重复决定别人，也不会把已经决定过的那一席再决定一次。
        if (_netHost)
        {
            if (_askingShow) HostDecideAiShow();
            else if (_askingDouble) HostDecideAiDouble();
            else if (_bidding)
            {
                if (_rpsStep == RpsStep.Want) HostDecideAiWant();
                else if (_rpsStep is RpsStep.Throw or RpsStep.Swing) HostDecideAiThrow();
            }
        }

        Refresh();
    }

    /// <summary>主机收桌了（或我自己发现该收了）。</summary>
    internal void NetClosed()
    {
        if (!IsOpen) return;
        DouSpireNet.CloseTable(false);
        DouSpireTable.Close(Entry.Logger);
        Entry.Toast("斗地主这桌收了。", "斗地主");
    }

    // ════════════════════════════════════════════════════════
    //  牌
    // ════════════════════════════════════════════════════════

    private void RebuildRow(HBoxContainer row, List<Card> cards, float cardW)
    {
        foreach (var child in row.GetChildren())
        {
            row.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var card in cards) row.AddChild(MakeCardVisual(card, cardW, interactive: false));
    }

    private void RebuildHand()
    {
        foreach (var child in _handRow.GetChildren())
        {
            _handRow.RemoveChild(child);
            child.QueueFree();
        }

        var hand = _game.Players[MeSeat].Hand;
        _selected.RemoveWhere(c => !hand.Contains(c));

        // 手写排版：**同点数的牌叠紧（只露一条边）**，换了点数才摊开。
        // HBoxContainer 只能给所有牌同一个间距，做不到这件事 —— 所以 _handRow 改成了普通 Control。
        //（用户："我手牌里的三个 4 为什么没有叠起来，我不是告诉过你同样的牌应该重叠吗。就露出一个边边"）
        // 120 → 116：手牌往下挪了之后，120 的牌高 168 会顶出屏幕底一截，收窄 4px 正好落回屏幕内
        var w = DouSpireUi.Px(116);
        // 2026-10-07：用户："牌太分散" → 间距收回来（原来 40/86 摊得像扇子）
        var sameStep = DouSpireUi.Px(34);      // 同点数：只露一条边
        var nextStep = DouSpireUi.Px(62);      // 换点数：露出角标那一条就够
        var h = w * 1.40f;
        var lift = DouSpireUi.Px(24);

        var ordered = hand.OrderByDescending(c => c.Rank).ToList();

        // 先算总宽，再整体居中
        var total = w;
        for (var i = 1; i < ordered.Count; i++)
            total += ordered[i].Rank == ordered[i - 1].Rank ? sameStep : nextStep;

        var rowW = _handRow.Size.X;
        if (rowW <= 0) rowW = DouSpireUi.Px(1560);
        var x = Mathf.Max(0f, (rowW - total) / 2f);

        for (var i = 0; i < ordered.Count; i++)
        {
            var card = ordered[i];
            var wrapper = MakeHandCard(card, w, h, lift);
            wrapper.Position = new Vector2(x, 0);
            _handRow.AddChild(wrapper);
            x += i + 1 < ordered.Count && ordered[i + 1].Rank == card.Rank ? sameStep : nextStep;
        }
    }

    /// <summary>手牌：外面套一层 wrapper 才好在"选中"时把牌往上抬（自己排版时直接挪 wrapper 的 Position）。</summary>
    private Control MakeHandCard(Card card, float w, float h, float lift)
    {
        var wrapper = new Control
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(w, h + lift),
        };

        var btn = new Button
        {
            ToggleMode = true,
            ButtonPressed = _selected.Contains(card),
            FocusMode = FocusModeEnum.None,
            TooltipText = CardNames.Full(card),
            Position = new Vector2(0, _selected.Contains(card) ? -lift : 0),
            Size = new Vector2(w, h),
        };
        btn.SetMeta("douspire_big", true);

        // 尖塔牌面：按钮只当"点击层"，卡面整个交给游戏自己的卡节点
        if (DouSpireConfig.CardSkin == DouSpireCardSkin.尖塔牌面)
        {
            var node = TrySpireCardNode(card, w, h);
            if (node != null)
            {
                ApplySpireClickLayer(btn, _selected.Contains(card));
                btn.AddChild(node);
                btn.Pressed += () => OnCardToggled(card, btn);
                btn.MouseEntered += DouSpireSfx.Hover;
                wrapper.AddChild(btn);
                return wrapper;
            }
        }

        ApplyCardSkin(btn, card, _selected.Contains(card));
        AddCardPips(btn, card, w, h);
        btn.Pressed += () => OnCardToggled(card, btn);
        btn.MouseEntered += DouSpireSfx.Hover;   // 划过一声很轻的官方 hover 音

        wrapper.AddChild(btn);
        return wrapper;
    }

    /// <summary>
    /// 尖塔牌面下按钮的底 —— **只留一圈金边表示"选中"**，其余透明，
    /// 好让下面那张游戏卡节点透出来（卡面是它画的，按钮不能盖）。
    /// </summary>
    private static void ApplySpireClickLayer(Button btn, bool selected)
    {
        var empty = new StyleBoxEmpty();
        var sel = DouSpireUi.Box(new Color(0f, 0f, 0f, 0f), 10, DouSpireUi.Gold, 4);
        btn.AddThemeStyleboxOverride("normal", selected ? sel : empty);
        btn.AddThemeStyleboxOverride("hover", selected ? sel : empty);
        btn.AddThemeStyleboxOverride("pressed", sel);
        btn.AddThemeStyleboxOverride("focus", empty);
    }

    /// <summary>展示用的牌（底牌 / 场上）——不可点。</summary>
    private Control MakeCardVisual(Card card, float w, bool interactive)
    {
        var h = w * 1.40f;

        // 尖塔牌面：**直接上游戏自己的卡节点**（百科全书里那张），成不了才退回我们画的
        if (DouSpireConfig.CardSkin == DouSpireCardSkin.尖塔牌面)
        {
            var node = TrySpireCardNode(card, w, h);
            if (node != null) return node;
        }

        var panel = new Panel
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(w, h),
            // ⚠️ HBoxContainer（底牌 / 场上最后一手用的就是这个）默认会把子节点
            // **竖向拉满格子的高度**，牌就被压窄成一条了
            //（用户："你看这个牌顶部的牌，还有打出去的牌，为什么感觉扭曲了"）。
            // ShrinkCenter = 保持自己声明的最小尺寸、在格子里居中，比例就不会走样。
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        ApplyCardSkin(panel, card, false);
        AddCardPips(panel, card, w, h);
        return panel;
    }

    /// <summary>套牌面底图：普通牌用 card_body.png，选中叠高亮框，王用整张王图。</summary>
    private static void ApplyCardSkin(Control host, Card card, bool selected)
    {
        Texture2D? joker = null;
        // 尖塔牌面 = **我们斗地主那套牌面 + 中间换成一张尖塔卡图**，
        // 所以牌底还是用我们自己的，王的整张图也不用了 —— 大小王同样贴尖塔卡图（灵体 / 神化）。
        // ⚠️ 烫金赌桌**没有画王**（德州扑克里没有大小王），所以王照旧用经典那两张整图。
        if (DouSpireConfig.CardSkin != DouSpireCardSkin.尖塔牌面
            && card.IsJoker && card.Suit == Suit.Joker)
            joker = DouSpireAssets.First(DouSpireAssets.JokerOf(card.Rank == Ranks.BigJoker));

        StyleBox sb;
        if (joker != null)
        {
            sb = DouSpireUi.TextureBox(joker);
        }
        else if (DouSpireConfig.CardSkin == DouSpireCardSkin.尖塔牌面)
        {
            // 尖塔牌面：**整张牌就用游戏自己的卡框**（攻击红 / 技能绿 / 能力蓝）。
            // ⚠️ 别染色 —— 之前拿角色色去乘，把框染得又脏又糊（用户："颜色不好看，用尖塔本来的颜色"）。
            var frame = DouSpireAssets.SpireFrame(card.Rank);
            sb = frame != null
                ? DouSpireUi.TextureBox(frame)
                : DouSpireUi.Box(DouSpireAssets.SpireTypeColor(DouSpireAssets.SpireFrameKind(card.Rank)), 9,
                                 selected ? DouSpireUi.Gold : new Color(0.9f, 0.86f, 0.72f),
                                 selected ? 4 : 2);
        }
        else if (DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌)
        {
            // 烫金赌桌：**跟经典走同一条排版**，只是把牌底换成烫金那张
            //（同一比例 512:716，整图拉伸）。素材还没画出来时
            // GildedBody 会自己退回经典牌底，所以这里不需要再写一层兜底。
            var body = DouSpireAssets.First(DouSpireAssets.GildedBody(selected));
            sb = body != null
                ? DouSpireUi.TextureBox(body)
                : DouSpireUi.Box(selected ? DouSpireUi.CardFaceOn : DouSpireUi.CardFace, 8,
                                 selected ? DouSpireUi.Gold : new Color(0.25f, 0.22f, 0.20f), 2);
        }
        else
        {
            var body = DouSpireAssets.First(selected ? DouSpireAssets.CardBodyOn : DouSpireAssets.CardBody);
            // 牌底图和牌的比例本来就是同一个（512:716 ≈ 1:1.4），整图拉伸即可 ——
            // 用九宫格反而会把那圈手绘描边**拉粗变形**（用户："你这个黑色的边框是真的丑"）。
            sb = body != null
                ? DouSpireUi.TextureBox(body)
                : DouSpireUi.Box(selected ? DouSpireUi.CardFaceOn : DouSpireUi.CardFace, 8,
                                 selected ? DouSpireUi.Gold : new Color(0.25f, 0.22f, 0.20f), 2);
        }

        switch (host)
        {
            case Button b:
                b.AddThemeStyleboxOverride("normal", sb);
                b.AddThemeStyleboxOverride("hover", sb);
                b.AddThemeStyleboxOverride("pressed", sb);
                b.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
                break;
            case Panel p:
                p.AddThemeStyleboxOverride("panel", sb);
                break;
        }

        // 选中的牌靠**换整张底图**区分（金框那张），不再往牌面上叠东西 ——
        // 叠一层框会把点数花色压住，而且那张框中间是透明的。
        host.Modulate = Colors.White;
    }

    /// <summary>牌面点数和花色：左上角标 + 中央大花色 + 右下镜像角标。</summary>
    /// <summary>
    /// 牌面 —— 2026-10-07 晚重做：
    /// ① 点数不再用字体渲染，直接贴用户给的 13 张字形图（<c>cards/pip_*.png</c>）；
    /// ② 一张牌只放**一枚**花色（用户："一张牌用一个花色 ui 就行了"），摆在正中央。
    ///
    /// 2026-10-07 深夜又加了一条：**尖塔牌面不再自己做一套卡**，而是
    /// **完全照我们斗地主这套排，只把正中央那枚花色换成尖塔的卡图**。
    ///（用户："不论怎么改 UI 都很丑，应该换种思路。发的尖塔对应的卡，但是效果是我们斗地主的牌"）
    /// 所以两种模式只差中央那一样东西，别的（点数、花色、牌底、字体）全都一样。
    /// </summary>
    private static void AddCardPips(Control host, Card card, float w, float h)
    {
        // 尖塔牌面：整张就是尖塔的卡，走另一条路（不画我们的点数 / 花色）
        if (DouSpireConfig.CardSkin == DouSpireCardSkin.尖塔牌面)
        {
            AddSpireFace(host, card, w, h);
            return;
        }

        if (card.IsJoker) return;

        var gilded = DouSpireConfig.CardSkin == DouSpireCardSkin.烫金赌桌;

        // 左上角：点数（叠牌时露出来的就是这一条）
        var pip = DouSpireAssets.First(gilded
            ? DouSpireAssets.GildedPip(card.Rank)
            : new[] { $"cards/pip_{Ranks.Name(card.Rank)}.png" });
        if (pip != null)
        {
            var r = new TextureRect
            {
                Texture = pip,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            r.AnchorLeft = r.AnchorRight = 0f;
            r.AnchorTop = r.AnchorBottom = 0f;
            r.OffsetLeft = w * 0.06f;
            r.OffsetTop = h * 0.030f;
            r.OffsetRight = w * 0.46f;
            r.OffsetBottom = h * 0.245f;
            host.AddChild(r);
        }

        // 正中央：一枚花色
        var suit = DouSpireAssets.First(gilded
            ? DouSpireAssets.GildedSuit(card.Suit)
            : DouSpireAssets.SuitOf(card.Suit));
        if (suit == null) return;

        var s = new TextureRect
        {
            Texture = suit,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        s.AnchorLeft = s.AnchorRight = 0.5f;
        s.AnchorTop = s.AnchorBottom = 0.5f;
        s.OffsetLeft = -w * 0.27f;
        s.OffsetTop = -h * 0.19f;
        s.OffsetRight = w * 0.27f;
        s.OffsetBottom = h * 0.19f;
        host.AddChild(s);
    }

    /// <summary>
    /// 尖塔牌面：**直接拿游戏自己的卡节点来渲染** —— 实例化 <c>res://scenes/cards/card.tscn</c>
    /// （<c>MegaCrit.Sts2.Core.Nodes.Cards.NCard</c>），把这张点数对应的 <c>CardModel</c> 喂给它。
    ///
    /// 这样出来的牌面跟**百科全书 / 卡库**里那张一模一样：费用宝珠、名牌、卡图、
    /// 攻击/技能标、描述说明，**全是游戏自己画的，我们一个像素都不碰**。
    ///（用户 2026-10-07："我意思是发百科全书的那种卡。你不要再自己设计 ui 设计卡面了，你已经走进死胡同了"）
    ///
    /// 任何一步出问题都返回 null —— 调用方退回原来那套牌面，桌子绝不会崩。
    /// </summary>
    private static Control? TrySpireCardNode(Card card, float w, float h)
    {
        try
        {
            // 按**游戏内部类名**找（可靠）；找不到才退回按中文名找
            var model = DouSpireCharacters.CardModelByClass(DouSpireAssets.SpireCardClass(card.Rank))
                        ?? DouSpireCharacters.CardModelOf(DouSpireAssets.SpireFaceName(card.Rank));
            if (model is null)
            {
                if (!_spireNodeWarned)
                {
                    _spireNodeWarned = true;
                    Entry.Logger.Warn($"斗地主：找不到 {DouSpireAssets.SpireCardClass(card.Rank)}"
                                      + $"（{DouSpireAssets.SpireFaceName(card.Rank)}）的卡模型，这张退回自己画的。");
                }
                return null;
            }

            var scene = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn");
            if (scene is null)
            {
                Entry.Logger.Warn("斗地主：读不到 scenes/cards/card.tscn，尖塔牌面退回自己画的。");
                return null;
            }

            var node = scene.Instantiate();

            // 固定大小的盒子（先声明，推迟执行的那段要用它当"格子"的参照）
            var box = new Control
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(w, h),
            };
            DouSpireUi.Fill(box);

            // 先把模型喂上（保底能出卡图）
            if (node is MegaCrit.Sts2.Core.Nodes.Cards.NCard nc)
            {
                nc.Model = model;

                // ⚠️ **初始化要等它真的进了场景树再做**。
                // 在树外调 OnInstantiated / UpdateVisuals，卡图能出来，可**文字控件还是空的**
                // → 游戏就画成坏卡占位（"Broken Card" / "If you can read this, there is a bug."）。
                // 所以这里推迟一帧：等 AddChild 完、_Ready 跑过之后再初始化。
                var self = nc;
                Callable.From(() =>
                {
                    if (!GodotObject.IsInstanceValid(self)) return;
                    try
                    {
                        self.OnInstantiated();
                        SetDisplayingPile(self, "Hand");
                        self.Model = model;

                        // 费用宝珠旁边那个**斜杠** = 游戏的"这张牌打不出"标记。
                        // 我们只是展示，当然要当成能打的，不然每张牌都杵着一道杠。
                        //（用户 2026-10-07："能不能把哪些/费用的斜杠去掉"）
                        // 前后各来一遍 —— 刷新卡面有可能把它重置回去。
                        self.SetPretendCardCanBePlayed(true);
                        RefreshCardNode(self);
                        self.SetPretendCardCanBePlayed(true);

                        // ⚠️ 卡面内容是**按固定像素（实测 300×422）**排的 —— 只给格子尺寸它不会自己缩。
                        // 做法：让它保持自己的原始尺寸，再**整体缩放**到我们格子那么宽。
                        //（用户 2026-10-07："现在实在是太大了，屏幕都被这些牌塞满了"）
                        var natural = self.GetCurrentSize();
                        if (natural.X > 1f)
                        {
                            var k = (w / natural.X) * Mathf.Clamp(DouSpireConfig.SpireCardZoom, 0.3f, 2f);
                            self.SetAnchorsPreset(Control.LayoutPreset.TopLeft);   // 别跟着格子拉伸
                            self.Size = natural;
                            self.PivotOffset = Vector2.Zero;
                            self.Scale = new Vector2(k, k);

                            // ⚠️ 这张卡的画面是**从原点往上**画的（原点在下边），
                            //    所以 Position = 0 时它整个浮在格子**上方**一个卡高的地方
                            //    —— 这正是"手牌被加倍/不加倍压住"的原因。
                            //    按可见高度往下推一格，就落进格子里了。
                            self.Position = new Vector2(0f, natural.Y * k);
                            WriteDiag($"尖塔牌面：原始尺寸 {natural.X:0}x{natural.Y:0} → 缩到 {w:0} 宽（k={k:0.000}）");

                            // ── 自己纠偏 ──
                            // 那张卡内部怎么排的（内容有没有负偏移、锚点在哪）我们管不住，
                            // 干脆**量出来它实际画在屏幕的哪**，再把它推回格子左上角。
                            // 再等一帧，是因为排版要到下一帧才算完（实测早一帧量到的是 (0,0)）。
                            var self2 = self;
                            var box2 = box;
                            Callable.From(() =>
                            {
                                if (!GodotObject.IsInstanceValid(self2) || !GodotObject.IsInstanceValid(box2)) return;
                                try
                                {
                                    var want = box2.GetGlobalRect().Position;
                                    var got = self2.GetGlobalRect().Position;
                                    var d = want - got;
                                    if (d.Length() > 0.5f) self2.Position += d;

                                    if (!_spirePosLogged)
                                    {
                                        _spirePosLogged = true;
                                        WriteDiag($"尖塔牌面：纠偏后卡顶 y={self2.GetGlobalRect().Position.Y:0}，"
                                                  + $"格子顶 y={want.Y:0}，修正 {d.Y:0}px，"
                                                  + $"可见 {self2.Size.X * self2.Scale.X:0}x{self2.Size.Y * self2.Scale.Y:0}");
                                    }
                                }
                                catch (System.Exception e)
                                {
                                    WriteDiag("尖塔牌面：纠偏失败 " + e.Message);
                                }
                            }).CallDeferred();
                        }
                        else
                        {
                            WriteDiag($"尖塔牌面：GetCurrentSize 返回 {natural.X:0}x{natural.Y:0}，算不了缩放");
                        }

                        DumpCardNode(self);
                    }
                    catch (System.Exception e)
                    {
                        if (!_spireNodeWarned)
                        {
                            _spireNodeWarned = true;
                            Entry.Logger.Warn($"斗地主：进树后初始化卡节点失败: {e.Message}");
                        }
                    }
                }).CallDeferred();
            }

            // ⚠️ **不能用 SetAnchorsPreset** —— 它只改锚点、不动偏移，
            //    而游戏卡的偏移是照它自己的 210×294 写死的（日志里 Size = 0x0），
            //    结果卡片永远按 210×294 摆 → 看着就是"位置全漂了"。
            //    必须用 SetAnchorsAndOffsetsPreset，让它老老实实铺满我们给的格子。
            //（用户 2026-10-07："因为缩放导致牌的位置全部发生了漂移"）
            if (node is Control c)
            {
                c.MouseFilter = MouseFilterEnum.Ignore;
                box.AddChild(c);
                // ⚠️ **别给它 FullRect** —— 那张卡的内部排版是照它自己的 300×422 摆的，
                // 把它撑成我们那个 1560×206 的格子，内容就会按畸形的框去排、整体往上跑。
                // 保持原尺寸，等进了树再由下面那步整体缩放（见 deferred 里那段）。
            }
            else if (node is Node2D n2)
            {
                box.AddChild(n2);
            }
            else
            {
                node.QueueFree();
                return null;
            }

            if (!_spireNodeOk)
            {
                _spireNodeOk = true;
                var ctl = node as Control;
                var line = $"尖塔牌面：用上游戏卡节点（类型 {node.GetType().Name}，"
                           + $"是Control={ctl != null}"
                           + (ctl != null ? $"，尺寸 {ctl.Size.X:0}x{ctl.Size.Y:0}" : "")
                           + $"，模型 {model.GetType().Name}「{model.Title}」）";
                Entry.Logger.Info("斗地主：" + line);
                WriteDiag(line);
            }
            return box;
        }
        catch (System.Exception e)
        {
            if (!_spireNodeWarned)
            {
                _spireNodeWarned = true;
                Entry.Logger.Warn($"斗地主：游戏卡节点用不了，尖塔牌面退回自己画的: {e.Message}");
            }
            return null;
        }
    }

    private static bool _spireNodeOk;
    private static bool _spireNodeWarned;
    private static bool _spireRefreshLogged;

    /// <summary>
    /// 设 <c>NCard.DisplayingPile</c>（这张牌"显示在哪个牌堆"）。
    /// 枚举类型（<c>PileType</c>）在哪个命名空间不好写死，所以反射设值 ——
    /// 可取值已经确认：None/Draw/Hand/Discard/Exhaust/Play/Deck。
    /// </summary>
    private static void SetDisplayingPile(MegaCrit.Sts2.Core.Nodes.Cards.NCard nc, string pile)
    {
        try
        {
            var p = nc.GetType().GetProperty("DisplayingPile");
            if (p is null || !p.CanWrite) return;
            p.SetValue(nc, System.Enum.Parse(p.PropertyType, pile));
        }
        catch (System.Exception e)
        {
            if (!_spireNodeWarned)
            {
                _spireNodeWarned = true;
                Entry.Logger.Warn($"斗地主：设 DisplayingPile 失败: {e.Message}");
            }
        }
    }

    private static bool _spireDumped;
    private static bool _spirePosLogged;

    /// <summary>
    /// 一次性把 <c>NCard</c> 的**全部属性和方法**倒进日志 ——
    /// 光设 <c>Model</c> 卡片还是画成坏卡占位，说明初始化不止这一步，
    /// 得看清它到底有哪些入口（<c>SetCard</c>？<c>CardNode</c>？持有者？）。
    /// </summary>
    private static void DumpCardNode(MegaCrit.Sts2.Core.Nodes.Cards.NCard nc)
    {
        if (_spireDumped) return;
        _spireDumped = true;
        try
        {
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.DeclaredOnly;

            var t = nc.GetType();
            var props = t.GetProperties(F).Select(p => $"{p.PropertyType.Name} {p.Name}");
            var meths = t.GetMethods(F).Select(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(q => q.ParameterType.Name))})");

            WriteDiag("NCard 属性 → " + string.Join(" | ", props));
            WriteDiag("NCard 方法 → " + string.Join(" | ", meths));
            WriteDiag($"NCard 设完 Model 后：{(nc.Model is null ? "null" : nc.Model.GetType().Name)}");
        }
        catch (System.Exception e)
        {
            WriteDiag("NCard 结构导出失败: " + e.Message);
        }
    }

    /// <summary>游戏自己的日志很难找，所以关键诊断**也写一份到诊断日志**。</summary>
    private static void WriteDiag(string line) => DouSpireAssets.Diag(line);

    /// <summary>
    /// 让卡节点按当前模型把卡面画出来。
    /// <c>UpdateVisuals(PileType, CardPreviewMode)</c> 要两个枚举参数，我们手上没有那套枚举名的准确写法，
    /// 所以**用反射调**：参数按各类型的默认值（0）给，顺便把参数名打进日志，下次就能写死。
    /// </summary>
    private static void RefreshCardNode(MegaCrit.Sts2.Core.Nodes.Cards.NCard nc)
    {
        try
        {
            var mi = nc.GetType().GetMethod("UpdateVisuals");
            if (mi is null) return;

            var ps = mi.GetParameters();
            var args = new object?[ps.Length];
            var note = new System.Text.StringBuilder();
            for (var i = 0; i < ps.Length; i++)
            {
                var pt = ps[i].ParameterType;
                if (pt.IsEnum)
                {
                    // **按名字挑一个说得通的**，别写死数字。
                    // 实测（2026-10-07 日志）：
                    //   PileType        → None/Draw/Hand/Discard/Exhaust/Play/Deck
                    //   CardPreviewMode → None/Normal/Upgrade/MultiCreatureTargeting
                    // 之前挑到了 `None`，游戏就画成坏卡占位（"Broken Card"）。
                    // 现在优先 Hand / Normal —— 这两个才是"一张正常能打的牌"。
                    var names = System.Enum.GetNames(pt);
                    var pick = names.FirstOrDefault(n => n.Contains("hand", System.StringComparison.OrdinalIgnoreCase))
                            ?? names.FirstOrDefault(n => n.Contains("normal", System.StringComparison.OrdinalIgnoreCase))
                            ?? names.FirstOrDefault(n => n.Contains("none", System.StringComparison.OrdinalIgnoreCase))
                            ?? names.FirstOrDefault();
                    note.Append($"{pt.Name}[{string.Join("/", names)}]用{pick}  ");
                    args[i] = pick is null ? System.Enum.ToObject(pt, 0) : System.Enum.Parse(pt, pick);
                }
                else
                {
                    args[i] = pt.IsValueType ? System.Activator.CreateInstance(pt) : null;
                }
            }
            mi.Invoke(nc, args);

            if (!_spireRefreshLogged)
            {
                _spireRefreshLogged = true;
                Entry.Logger.Info($"斗地主：NCard.UpdateVisuals → {note}");
                WriteDiag($"尖塔牌面：UpdateVisuals → {note}");
            }
        }
        catch (System.Exception e)
        {
            if (!_spireRefreshLogged)
            {
                _spireRefreshLogged = true;
                Entry.Logger.Warn($"斗地主：NCard.UpdateVisuals 反射调用失败: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 尖塔牌面：**整张就是一张尖塔的卡** —— 游戏自己的卡框（见 <see cref="ApplyCardSkin"/>）
    /// + 这张卡自己的卡图 + 卡名。**不画我们的点数、花色**。
    ///
    /// 卡名**竖排**贴在左边那一竖条上 —— 叠牌时只有这一条露在外面，横排的名字会被旁边那张压掉。
    ///（用户 2026-10-07 深夜定的：整张尖塔卡、不显示点数、效果照旧是 233445、
    ///  "既然如此为什么不竖着放卡名"）
    /// </summary>
    private static void AddSpireFace(Control host, Card card, float w, float h)
    {
        // ── 卡图：右边大半张 ──
        var art = DouSpireAssets.SpireFace(card.Rank);
        if (art != null)
        {
            var a = new TextureRect
            {
                Texture = art,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            a.AnchorLeft = a.AnchorRight = 0f;
            a.AnchorTop = a.AnchorBottom = 0f;
            a.OffsetLeft = w * 0.37f;
            a.OffsetTop = h * 0.075f;
            a.OffsetRight = w * 0.965f;
            a.OffsetBottom = h * 0.870f;
            host.AddChild(a);
        }

        // ── 卡名：**竖排**在左边那一竖条 ──
        // ⚠️ 卡是每次重绘现建的，<see cref="DouSpireUi.StyleFont"/> 只在开桌时跑一次，
        //    所以字体得**手动套**（用户："字体不好看"）。
        var name = DouSpireAssets.SpireFaceName(card.Rank);
        if (name.Length == 0) return;

        var label = new Label
        {
            Text = string.Join("\n", name.ToCharArray()),   // 一字一行 = 竖排
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.SetMeta("douspire_big", true);
        label.AddThemeFontOverride("font", DouSpireUi.UiFont());
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", new Color(0.06f, 0.04f, 0.02f));
        label.AddThemeConstantOverride("outline_size", Mathf.Max(3, Mathf.RoundToInt(w * 0.05f)));
        label.AddThemeConstantOverride("line_spacing", 0);
        // 字多大要同时受两边约束：横的别超出这一竖条，竖的四字名也得塞得下
        var fs = Mathf.Min(w * 0.235f, h * 0.88f / Mathf.Max(1, name.Length));
        label.AddThemeFontSizeOverride("font_size", Mathf.Max(9, Mathf.RoundToInt(fs)));
        label.AnchorLeft = label.AnchorRight = 0f;
        label.AnchorTop = label.AnchorBottom = 0f;
        label.OffsetLeft = w * 0.01f;
        label.OffsetTop = h * 0.035f;
        label.OffsetRight = w * 0.37f;
        label.OffsetBottom = h * 0.975f;
        host.AddChild(label);
    }

    private static void AddCardPipsLegacy(Control host, Card card, float w, float h)
    {
        if (card.IsJoker) return;

        var ink = card.Suit switch
        {
            Suit.Heart or Suit.Diamond => DouSpireUi.Red,
            _ => DouSpireUi.Black,
        };

        // ── 排版照用户给的豆包样板：左上角"大点数 + 下面小花色"、中央一枚大花色、右下镜像 ──
        var rank = new Label
        {
            Text = Ranks.Name(card.Rank),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        rank.AddThemeColorOverride("font_color", ink);
        rank.SetMeta("douspire_big", true);
        rank.AddThemeFontSizeOverride("font_size", Mathf.Max(13, Mathf.RoundToInt(w * 0.30f)));
        rank.AnchorLeft = rank.AnchorRight = 0f;
        rank.AnchorTop = rank.AnchorBottom = 0f;
        rank.OffsetLeft = w * 0.09f;
        rank.OffsetTop = h * 0.030f;
        rank.OffsetRight = w * 0.61f;
        rank.OffsetBottom = h * 0.205f;
        host.AddChild(rank);

        var suitTex = DouSpireAssets.First(DouSpireAssets.SuitOf(card.Suit));

        if (suitTex != null)
        {
            var small = new TextureRect
            {
                Texture = suitTex,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            small.AnchorLeft = small.AnchorRight = 0f;
            small.AnchorTop = small.AnchorBottom = 0f;
            small.OffsetLeft = w * 0.115f;
            small.OffsetTop = h * 0.205f;
            small.OffsetRight = w * 0.375f;
            small.OffsetBottom = h * 0.345f;
            host.AddChild(small);
        }

        // ── 中央大花色（清晰明了：一眼看清什么花色）──
        if (suitTex != null)
        {
            var big = new TextureRect
            {
                Texture = suitTex,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            big.AnchorLeft = big.AnchorRight = 0.5f;
            big.AnchorTop = big.AnchorBottom = 0.5f;
            big.OffsetLeft = -w * 0.235f;
            big.OffsetTop = -h * 0.168f;
            big.OffsetRight = w * 0.235f;
            big.OffsetBottom = h * 0.168f;
            host.AddChild(big);
        }

        // ── 右下角的镜像角标（真扑克的样子，转 180°）──
        var mirror = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        var mw = w * 0.52f;
        var mh = h * 0.34f;
        mirror.AnchorLeft = mirror.AnchorRight = 1f;
        mirror.AnchorTop = mirror.AnchorBottom = 1f;
        mirror.OffsetLeft = -mw - w * 0.055f;
        mirror.OffsetTop = -mh - h * 0.028f;
        mirror.OffsetRight = -w * 0.055f;
        mirror.OffsetBottom = -h * 0.028f;
        mirror.Rotation = Mathf.Pi;
        mirror.PivotOffset = new Vector2(mw / 2f, mh / 2f);
        host.AddChild(mirror);

        var rank2 = new Label
        {
            Text = Ranks.Name(card.Rank),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        rank2.AddThemeColorOverride("font_color", ink);
        rank2.SetMeta("douspire_big", true);
        rank2.AddThemeFontSizeOverride("font_size", Mathf.Max(11, Mathf.RoundToInt(w * 0.30f)));
        rank2.AnchorLeft = rank2.AnchorRight = 0f;
        rank2.AnchorTop = rank2.AnchorBottom = 0f;
        rank2.OffsetLeft = mw * 0.17f;
        rank2.OffsetTop = mh * 0.09f;
        rank2.OffsetRight = mw * 1.17f;
        rank2.OffsetBottom = mh * 0.60f;
        mirror.AddChild(rank2);

        if (suitTex != null)
        {
            var small2 = new TextureRect
            {
                Texture = suitTex,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            };
            small2.AnchorLeft = small2.AnchorRight = 0f;
            small2.AnchorTop = small2.AnchorBottom = 0f;
            small2.OffsetLeft = mw * 0.22f;
            small2.OffsetTop = mh * 0.58f;
            small2.OffsetRight = mw * 0.72f;
            small2.OffsetBottom = mh * 1.00f;
            mirror.AddChild(small2);
        }
    }

    // ════════════════════════════════════════════════════════
    //  文字
    // ════════════════════════════════════════════════════════

    private static string CardText(Card c) => Ranks.Name(c.Rank);

    private static string Describe(Combo c)
        => $"{c.TypeName}（{string.Join(" ", c.Cards.OrderBy(x => x.Rank).Select(CardText))}）";
}

/// <summary>牌面全名，只给鼠标悬停提示用。</summary>
internal static class CardNames
{
    public static string Full(Card c) => c.Suit switch
    {
        Suit.Joker => c.Rank == Ranks.BigJoker ? "大王" : "小王",
        Suit.Spade => $"{Ranks.Name(c.Rank)}♠",
        Suit.Heart => $"{Ranks.Name(c.Rank)}♥",
        Suit.Club => $"{Ranks.Name(c.Rank)}♣",
        Suit.Diamond => $"{Ranks.Name(c.Rank)}♦",
        _ => Ranks.Name(c.Rank),
    };
}
