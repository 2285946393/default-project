using System.Collections.Generic;
using BaseLib.Config;
using DouSpire.Table;
using DouSpireRules;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace DouSpire;

/// <summary>
/// 电脑走子用哪套脑子。成员名故意写中文 —— BaseLib 下拉没本地化表时直接显示成员名。
/// </summary>
public enum DouSpireAiLevel
{
    /// <summary>老规则 AI —— 启发式，最快、零依赖。</summary>
    规则AI = 0,

    /// <summary>DouZero-WP —— 神经网络，求胜率的打法（稳）。</summary>
    DouZero稳 = 1,

    /// <summary>DouZero-ADP —— 神经网络，追求分差、敢炸（狠）。</summary>
    DouZero狠 = 2,
}

/// <summary>
/// **德州扑克**里电脑的性格。成员名故意写中文 —— BaseLib 下拉没本地化表时直接显示成员名。
///
/// 和内核的 <see cref="PokerAi.Style"/> 一一对应。这里再包一层顶层枚举，
/// 是为了让设置页拿到的永远是一个**顶层类型** —— 和本文件其它下拉（人数 / 界面风格 / 牌面）保持一致。
/// </summary>
public enum DouSpirePokerAiStyle
{
    /// <summary>保守：牌不好就弃，很少诈唬。适合新手看。</summary>
    保守 = 0,

    /// <summary>正常：按赔率打，偶尔诈唬。</summary>
    正常 = 1,

    /// <summary>激进：敢加注、诈唬多。牌桌热闹。</summary>
    激进 = 2,
}

/// <summary>
/// 21 点拿什么当赌注（用户 2026-10-11：「两条都要（设置里切）」）。
/// 成员名写中文，理由同 <see cref="DouSpireTableSize"/>。
/// </summary>
public enum DouSpireBlackjackStake
{
    /// <summary>押**金币** —— 和斗地主、德州一样，赢了进账输了掏钱。</summary>
    金币 = 0,

    /// <summary>
    /// 押**血量** —— 爆牌掉血、黑杰克回血。**这是 21 点最尖塔的那一档**：
    /// 一局就 20 秒，爆了立刻见血，节奏刚好。有三条保命线（见
    /// <see cref="DouSpireConfig.BlackjackHpCapPercent"/> 的注释），玩不死自己。
    /// </summary>
    血量 = 1,
}

/// <summary>
/// 一桌坐几个人。成员名故意写中文 —— BaseLib 下拉没本地化表时直接显示成员名。
/// </summary>
public enum DouSpireTableSize
{
    /// <summary>经典三人斗地主（一副牌 54 张，17/17/17 + 3 底）。</summary>
    三人经典 = 3,

    /// <summary>四人 2v2 组队（欢乐斗地主口径：两副牌去 345 共 84 张，对家组队，无地主，后手队扣牌）。</summary>
    四人2v2 = 4,
}

/// <summary>
/// mod 自己的设置页（挂到游戏的「设置 → 模组配置」里）。
/// 走 BaseLib 的配置框架：public static 属性会被反射成设置项、自动存/读
/// <c>user://mod_configs/DouSpire.cfg</c>。bool → 开关；enum → 下拉；带 [ConfigSlider] → 滑条。
/// </summary>
public sealed class DouSpireConfig : SimpleModConfig
{
    /// <summary>
    /// **牌局人数**：3 = 经典斗地主；4 = 2v2 组队（对家是队友）。
    /// 改完 F9 收桌重开生效；联机暂时固定 3 人。
    /// 用户 2026-10-09 拍板："四个人是2打2"。
    /// </summary>
    public static DouSpireTableSize TableSize { get; set; } = DouSpireTableSize.三人经典;

    /// <summary>斗地主桌面的界面缩放。改一下桌面就按新尺度重建。</summary>
    [ConfigSlider(0.7, 1.3, 0.05, Format = "{0:0.00}x")]
    public static float UiScale { get; set; } = 1.0f;

    /// <summary>
    /// 电脑出牌之间的停顿（秒）。太短看不清别人出了什么，太长嫌磨叽。
    /// 2026-10-07 用户："ai 出牌太快了，应该等 1-2 秒，出完了再等 1-2 秒" → 默认提到 1.5。
    /// </summary>
    [ConfigSlider(0.15, 3.0, 0.05, Format = "{0:0.00}s")]
    public static float AiDelay { get; set; } = 1.5f;

    /// <summary>桌面中央显示出牌历史（默认开）。</summary>
    public static bool ShowLog { get; set; } = true;

    /// <summary>
    /// 斗地主的音效（默认开）。用的是**杀戮尖塔 2 自己的音效**，
    /// 音量跟着游戏「设置 → 音频 → 音效」那条走。
    /// </summary>
    public static bool SfxEnabled { get; set; } = true;

    /// <summary>斗地主音效的音量（1.0 = 跟游戏音效一样响）。</summary>
    [ConfigSlider(0.0, 1.0, 0.05, Format = "{0:0.00}x")]
    public static float SfxVolume { get; set; } = 1.0f;

    // ── 下面四条是 2026-10-07 把声音拆开理的（用户："音乐配置有点乱了，
    //    主要是角色还有单独的配音"）。四类各管各的，想关哪个关哪个。──

    /// <summary>
    /// 背景音乐。**跟着局势换**：平时一首、有人快走完换紧张那首、
    /// 炸弹/王炸/加倍插一段高潮、赢了有胜利曲（见 <see cref="DouSpireMusicCue"/>）。
    ///
    /// 歌从哪来：丢进 <c>mods\DouSpire\assets\music\&lt;槽位&gt;\</c> 就用自己的，
    /// 槽位空着就**自动回退游戏本体的曲子** —— 什么都不放也有音乐。
    /// </summary>
    public static bool MusicEnabled { get; set; } = true;

    /// <summary>背景音乐的音量（1.0 = 跟游戏音乐一样响）。</summary>
    [ConfigSlider(0.0, 1.0, 0.05, Format = "{0:0.00}x")]
    public static float MusicVolume { get; set; } = 0.7f;

    /// <summary>高潮音乐 —— 炸弹 / 王炸 / 加倍 / 抢地主时插播的那首（放几秒自己退回局势曲）。</summary>
    public static bool MusicClimaxEnabled { get; set; } = true;

    /// <summary>人声台词（抢地主 / 不抢 / 赢了那几句）。</summary>
    public static bool VoiceEnabled { get; set; } = true;

    /// <summary>**角色自己那套配音**（出牌时的攻击 / 施法 / 倒地音效）。</summary>
    public static bool ActorVoiceEnabled { get; set; } = true;

    /// <summary>
    /// **轮到我出牌的时间上限（秒）** —— 到点自动"要不起"（领出时退化成甩最小的一张）。
    /// 中途第 5 秒会先说一句"让我想想"。设 0 = 不限时。
    /// 用户 2026-10-07："加出现时间限制 20 秒。超过五秒才触发那个思考声音"。
    /// </summary>
    [ConfigSlider(0.0, 60.0, 1.0, Format = "{0:0}秒")]
    public static float TurnTimeLimit { get; set; } = 20.0f;

    /// <summary>
    /// **底注** —— 一局输赢的基准（每个玩家开局 1000 筹码）。
    /// 实际一份 = 底注 × 倍数（明牌、加倍、炸弹都会把倍数乘上去）。
    /// 用户 2026-10-07："进房间你应该给每个玩家发钱 1000"。
    /// </summary>
    [ConfigSlider(10.0, 500.0, 10.0, Format = "{0:0}")]
    public static int BaseStake { get; set; } = 100;

    /// <summary>
    /// **篝火赌注：一份底注 = 我身上的钱 ÷ 这个数**（下限 1 金币，上限仍是 <see cref="BaseStake"/>）。
    /// 默认 20 ⇒ 一把平局（倍数 ×1）输赢约本金的 5%，小赌怡情。
    /// </summary>
    [ConfigSlider(5.0, 200.0, 5.0, Format = "1/{0:0}")]
    public static int BetUnitDivisor { get; set; } = 20;

    /// <summary>
    /// **篝火赌注：一局最多让我输/赢本金的百分之几**（封顶）。
    /// 地主一个人对两个农民，所以「一份」的封顶 = 本金 × 这个数 ÷ 100 ÷ 2。
    /// 超过的部分不结算（倍数照常显示，只是金币封顶）—— 免得一把 ×64 把人打回零元。
    /// 用户 2026-10-08：「不能让我赢太多。也不能让我输太多」。
    /// </summary>
    [ConfigSlider(5.0, 100.0, 5.0, Format = "{0:0}%")]
    public static int BetMaxSwingPercent { get; set; } = 25;

    /// <summary>
    /// 编外角色说话时，**对手那两席的音量倍数**（自己那席永远满音量）。
    /// 三家同时开口也不会互相掐掉 —— 每席一个播放器，各放各的。
    /// 用户 2026-10-07："放大你选的，你的对手的声音低一些，但是都能正常放"。
    ///
    /// ⚠️ 2026-10-08 用户："**敌方角色声音可以调大点了**" ⇒ 默认值从 0.75 提到 <b>1.0</b>
    /// （对手跟"我"一样响；再想更响最高能到 2.0）。
    /// ⚠️ **这条设置是存盘的** —— 改默认值**不会**覆盖玩家已经存下的旧值。
    /// 老玩家（包括本机那份 0.55）得自己去设置页把滑条拖上来。
    /// </summary>
    [ConfigSlider(0.0, 2.0, 0.05, Format = "{0:0.00}x")]
    public static float OpponentVoiceScale { get; set; } = 1.0f;

    // ── 语音「概率触发」（2026-10-11 用户："说句心里话，中文语音太尬了。所有语音改成概率触发。"）──
    //
    //  历史：2026-10-08 晚按用户要求把"按秒节流"那套频率阀门**整个撤掉**了
    //  （用户原话"我没让你们加频率阀门啊"），之后是"每次事件都开口"，句句都响。
    //  响了一个多月，用户听下来觉得**中文台词太密、太尬** ⇒ 现在改成"掷骰子"。
    //
    //  ⚠️ 这**不是**回到当年那套按秒数节流（那个玩家体感是"出牌没声音"），
    //     而是**按台词要紧程度分三档**、每档一个概率滑条：
    //     · 碎嘴（一局重复几十次的那几句）→ 默认只 35% 说出来
    //     · 普通（三带 / 顺子 / 轮到我）  → 默认 60%
    //     · 关键（炸弹 / 报单 / 胜负）    → 默认 100%（照旧必说）
    //  想全开就三条都拉到 100%，想彻底清净就都拉到 0%。消费方见
    //  <see cref="DouSpire.Table.DouSpireVoice.RollAdmit"/>。

    /// <summary>**碎嘴档**：出一张小牌 / 对子 / 要不起 / 压别人一手 —— 一局里重复几十次的那几句。</summary>
    [ConfigSlider(0.0, 100.0, 5.0, Format = "{0:0}%")]
    public static float VoiceChanceChatter { get; set; } = 35f;

    /// <summary>**普通档**：三带 / 顺子 / 连对 / 飞机 / 四带二 / 轮到我 / 点提示。</summary>
    [ConfigSlider(0.0, 100.0, 5.0, Format = "{0:0}%")]
    public static float VoiceChanceNormal { get; set; } = 60f;

    /// <summary>
    /// **关键档**：炸弹 / 王炸 / 报单 / 胜负 / 抢地主 / 发牌。
    /// 默认 100% = **照旧永远说得出来**（这些是玩家最想听到的那几句）。
    /// </summary>
    [ConfigSlider(0.0, 100.0, 5.0, Format = "{0:0}%")]
    public static float VoiceChanceImportant { get; set; } = 100f;

    /// <summary>
    /// 篝火开局**弹选角色界面**（我这一席默认已选中本局正在用的角色，不想换直接点「开始打牌」）。
    ///
    /// 历史：早上按用户"我不应该选角色，直接让我操控他"跳过了选人；晚上用户改主意 ——
    /// 「建议还是可以改成选角色，不然我们做的角色不是可游玩角色，玩家就一直玩不到」⇒ 默认<b>开</b>。
    /// 联机不受影响（联机永远坐自己 run 的角色）。
    /// </summary>
    public static bool RestSitePickSeat { get; set; } = true;

    /// <summary>电脑用哪套脑子出牌（联机时主机替 AI 走子也走这套）。
    /// · 规则AI —— 一直以来的那套启发式，最快、零依赖；
    /// · DouZero稳 / 狠 —— 快手开源的 DouZero 神经网络（Apache-2.0，Botzone 344 个 bot 排第一），
    ///   会配合、会算牌；稳 = WP 目标（求胜率），狠 = ADP 目标（追求分差，敢炸）。
    /// 模型缺失 / 推理出错时**自动退回规则 AI**，永远不会因为 AI 卡死牌局。
    /// 模型来自 HuggingFace palemoky/douzero-baselines 的 ONNX 转换（官方 baselines）。</summary>
    public static DouSpireAiLevel AiLevel { get; set; } = DouSpireAiLevel.DouZero狠;

    /// <summary>
    /// 用**活的角色**（游戏本体那个带 Spine 骨骼的）坐在桌边 —— 出牌会做动作、带角色自己的音效。
    /// 关掉就退回原来的静态立绘。取不到角色时也会自动退回，不用管。
    /// </summary>
    public static bool LiveActors { get; set; } = true;

    /// <summary>角色大小。觉得太大/太小就拨这个，改完重开一次桌子（F9 收、F9 开）生效。</summary>
    [ConfigSlider(0.3, 3.0, 0.05, Format = "{0:0.00}x")]
    public static float ActorScale { get; set; } = 1.0f;

    /// <summary>
    /// **第三方 mod 的角色改走"休息处那套"（坐着）**。
    ///
    /// 背景：有些 mod（比如卡芙卡）自己**不带角色场景**，是借本体的壳 + 用 Harmony 补丁
    /// 在自己 _Ready 里把骨骼换成自己的。那种补丁挂在 <c>NCreature</c> / <c>NRestSiteCharacter</c> 上，
    /// 而牌桌上我们没有 <c>NCreature</c> —— 只剩"休息处那套"能被触发。
    ///
    /// 默认关：先按正常流程走（站着那套 + 补一次 GenerateAnimator），
    /// **如果某张脸还是不对**，把这里打开重开桌子，那一席就改用坐姿。
    /// 只影响 mod 角色，本体角色永远走站着那套。
    /// 用户 2026-10-08："角色没有正确引用。我这边还战士。没有变成卡芙卡"。
    /// </summary>
    public static bool ModCharsPreferSitting { get; set; } = false;

    /// <summary>角色上下微调（正数往下）。脚底没踩在桌沿上就用这个挪。</summary>
    [ConfigSlider(-300.0, 300.0, 10.0, Format = "{0:0}px")]
    public static float ActorYOffset { get; set; } = 0.0f;

    /// <summary>
    /// 角色**脸朝牌桌中间**（默认开 —— 用户 2026-10-07："所有的角色应该面向牌桌"）。
    /// 精灵默认朝右，所以只有站在屏幕右半边的那家需要左右翻个面。
    /// </summary>
    public static bool ActorFaceCenter { get; set; } = true;

    /// <summary>
    /// 桌面 UI 风格。换一下，整桌的边、圆角、光晕一起变。
    /// 「原样」= 改造前那套（深绿毡 + 金边、不留光晕），随时可退。
    /// 下拉里显示的就是枚举成员名（中文），见 <see cref="DouSpireUiTheme"/>。
    /// </summary>
    public static DouSpireUiTheme UiTheme { get; set; } = DouSpireUiTheme.星穹铁道;

    /// <summary>
    /// **牌面**：经典扑克 / 尖塔卡面 / 烫金赌桌。
    /// 点数、牌力、顺子连对这些**一点没变** —— 只是把牌的脸换成杀戮尖塔的卡
    /// （打击、防御、大爆炸…），点数角标还留着，免得读不出顺子。
    /// 用户 2026-10-07："把牌面替换成打击和防御……效果仍然是 23456，只是卡面变成我们尖塔"。
    ///
    /// 2026-10-09 加第三档「烫金赌桌」（复古烫金 + 牛血红，配德州扑克那张赌桌）。
    /// 设置页的下拉和牌桌上那个「牌面：…」按钮改的是同一个值。
    /// </summary>
    public static DouSpireCardSkin CardSkin { get; set; } = DouSpireCardSkin.经典斗地主;

    /// <summary>
    /// 尖塔牌面里那张游戏卡的**额外缩放**。**默认 1.0 = 不动** ——
    /// 卡节点会自己铺满我们给的格子，这时位置最正。
    ///
    /// ⚠️ 设置值是**存盘的**：改过默认值不会覆盖玩家已经存下的旧值。
    /// 2026-10-07 就把默认从 0.22 改成 1.0，结果日志里还是 0.22 —— 存的那份说了算。
    /// 所以后来干脆**整个去掉**（用 1.0 / 不动）。
    /// </summary>
    [ConfigSlider(0.3, 2.0, 0.01, Format = "{0:0.00}x")]
    public static float SpireCardZoom { get; set; } = 1.0f;

    // ── 斗地主桌的「室内 / 桌面」切换（2026-10-10）─────────────────

    /// <summary>
    /// **斗地主桌的室内背景**（整屏背景）。
    ///
    /// ★★ 2026-10-10 用户：「**背景直接套用德州的那几个房间画面**」⇒ 和德州**共用同一套
    /// <see cref="DouSpireScene"/> / 同一批 `bg/poker_*.png`**，只是各存各的值
    /// （`DouSpireScene` = 德州存的、`DouSpireTableScene` = 斗地主存的，
    /// 所以你两边可以选不同的房间，互不干扰）。
    ///
    /// ⚠️ 默认是 <see cref="DouSpireScene.暖木俱乐部"/>，**不是**老那张 `bg/hall.png` 酒馆 ——
    ///    那张圆桌是**烤在画里**的，和斗地主自己那张桌图叠一起会重影。
    /// ⚠️ 加了新成员**必须**在 <see cref="InjectLabels"/> 里加
    ///    `["DOUSPIRE-TABLE_SCENE.&lt;成员名&gt;"]`，否则下拉里显示原始成员名（踩过）。
    /// </summary>
    public static DouSpireScene DouSpireTableScene { get; set; } = DouSpireScene.暖木俱乐部;

    /// <summary>
    /// **斗地主桌的台面**（桌上那张桌图的材质 / 配色）。
    ///
    /// ★★ 2026-10-10 用户：给斗地主加「桌面」钮，但**新桌图还没画** ⇒ 这里先接
    ///    德州的 <see cref="DouSpireTableStyle"/>（`table/table_poker*.png`）占位，
    ///    等你按提示词画好斗地主专用的几张，改 <see cref="DouSpireAssets.DouSpireTableOf"/>
    ///    的取图逻辑即可，**调用方一行不用动**。
    ///
    /// ⚠️ 加了新成员同上要进 <see cref="InjectLabels"/>。
    /// </summary>
    public static DouSpireTableStyle DouSpireTableStyle { get; set; } = DouSpireTableStyle.经典绿绒;

    // ── 德州扑克（玩法二，2026-10-09）─────────────────────────────

    /// <summary>
    /// 德州扑克的**牌室场景**（整屏背景）。设置页里是个下拉；牌桌右上角也有一个
    /// 「场景：…」按钮，点一下换一个、**当场生效**（不用收桌重开）。
    /// 用户 2026-10-09：「**可以多做几个切换场景**」。
    ///
    /// ⚠️ 加了新场景**必须**在 <see cref="InjectLabels"/> 里加
    /// `["DOUSPIRE-POKER_SCENE.&lt;成员名&gt;"]`，否则下拉里显示原始成员名（踩过）。
    /// ⚠️ 这个值**存盘**：改了默认值不会覆盖玩家存下的旧值。
    /// ⚠️ **斗地主那边不受影响** —— 它用的是 `bg/hall.png`（圆桌烤在画里的酒馆），
    ///    这套场景只换德州。
    /// </summary>
    public static DouSpireScene PokerScene { get; set; } = DouSpireScene.暖木俱乐部;

    /// <summary>
    /// 德州扑克的**牌桌风格**（桌面那张图的材质 / 配色）。设置页里是个下拉；牌桌右上角也有一个
    /// 「牌桌：…」按钮，点一下换一张、**当场生效**（不用收桌重开）。
    /// 用户 2026-10-09：「**你可以多想几个不同风格的牌桌**」。
    ///
    /// ⚠️ 七档的**刻线布局完全一样**，只有材质和配色不同 —— 代码叠牌的位置是写死的。
    /// ⚠️ 加了新风格**必须**在 <see cref="InjectLabels"/> 里加
    /// `["DOUSPIRE-POKER_TABLE_STYLE.&lt;成员名&gt;"]`，否则下拉里显示原始成员名（踩过）。
    /// ⚠️ 这个值**存盘**：改了默认值不会覆盖玩家存下的旧值。
    /// ⚠️ **斗地主那边不受影响** —— 它用的是 `table/table_round.png`。
    /// </summary>
    public static DouSpireTableStyle PokerTableStyle { get; set; } = DouSpireTableStyle.经典绿绒;

    /// <summary>
    /// **德州：电脑的性格**。只影响"加注门槛 / 跟注松紧 / 诈唬频率"，
    /// 不影响它看牌的能力（它**看不到别人的底牌**，见 <see cref="PokerAi"/>）。
    /// </summary>
    public static DouSpirePokerAiStyle PokerAiStyle { get; set; } = DouSpirePokerAiStyle.正常;

    /// <summary>
    /// **德州：一直显示"该不该跟"的建议**（默认开）。
    /// 新手友好那一条 —— 算"你的胜率 vs 要掏的钱"，给一句"建议跟 / 建议弃"。
    /// 用户 2026-10-09：「实话说我从来没玩过德州扑克……主要是观众要加的」→ 这是硬需求。
    /// 关掉就只剩牌型名，适合自己想练的玩家。
    /// </summary>
    public static bool PokerOddsHint { get; set; } = true;

    /// <summary>设置里选的性格 → 内核要的那个枚举。**写成方法**（不是属性）——
    /// 免得被 BaseLib 的反射当成一个"只读的设置项"生成到设置页上去。</summary>
    public static PokerAi.Style PokerStyle() => PokerAiStyle switch
    {
        DouSpirePokerAiStyle.保守 => PokerAi.Style.保守,
        DouSpirePokerAiStyle.激进 => PokerAi.Style.激进,
        _ => PokerAi.Style.正常,
    };

    /// <summary>圆角放大。二游普遍比传统 UI 更圆。</summary>
    [ConfigSlider(0.5, 2.0, 0.05, Format = "{0:0.00}x")]
    // ══════════════════════════ 21 点（玩法三，2026-10-11）══════════════════════════

    /// <summary>
    /// 21 点用什么下注。
    /// <see cref="DouSpireBlackjackStake.金币"/> = 和斗地主、德州一样拿身上的金币赌（默认）；
    /// <see cref="DouSpireBlackjackStake.血量"/> = **拿命赌** —— 爆牌掉血、黑杰克回血。
    /// 用户 2026-10-11 拍板「两条都要（设置里切）」。
    /// </summary>
    public static DouSpireBlackjackStake BlackjackStake { get; set; } = DouSpireBlackjackStake.金币;

    /// <summary>21 点的基础注（金币模式 = 多少金币；血量模式 = 多少点血）。</summary>
    public static int BlackjackBaseStake { get; set; } = 100;

    /// <summary>
    /// **血量模式的安全阀**：一次最多掉**当前血量**的百分之多少。
    /// 25% 是照斗地主那条「不能让我输太多」的规矩定的 —— 21 点一局就 20 秒，
    /// 连着爆几把也不至于把自己玩死。再大的注也只掉这么多。
    /// </summary>
    public static int BlackjackHpCapPercent { get; set; } = 25;

    /// <summary>是不是"拿命赌"那一档（给调用方省得每次都判一遍枚举）。</summary>
    public static bool BlackjackUsesHp() => BlackjackStake == DouSpireBlackjackStake.血量;

    public static float UiRadius { get; set; } = 1.0f;

    /// <summary>外发光强度。调到 0 就是完全不发光。</summary>
    [ConfigSlider(0.0, 2.5, 0.05, Format = "{0:0.00}x")]
    public static float UiGlow { get; set; } = 1.0f;

    /// <summary>面板不透明度。调低会透出后面的桌布／背景。</summary>
    [ConfigSlider(0.4, 1.0, 0.05, Format = "{0:0.00}x")]
    public static float UiPanelAlpha { get; set; } = 1.0f;

    /// <summary>当前游戏语言是不是英文（游戏的语言代码是 "eng"）。</summary>
    public static bool GameIsEnglish()
    {
        try { return LocManager.Instance?.Language == "eng"; }
        catch (System.Exception) { return false; }
    }

    public override void SetupConfigUI(Control optionContainer)
    {
        // ★ 必须在 base 之前：base 是一边生成一行一边去 settings_ui 查文字的，
        //   表里还没有键的话，那一行就直接显示属性名（比如 "UiScale"）。
        InjectLabels();
        base.SetupConfigUI(optionContainer);
    }

    // ══════════════════════ 文案注入：谁来调、什么时候调 ══════════════════════
    //
    // ★★ 2026-10-08 找到真病根（用户："这个界面我们的设置全是英文，我根本看不懂"）★★
    //
    //   原来 InjectLabels() **只**在 SetupConfigUI() 里调。而玩家实际看到的那张设置页
    //   （截图里标题「模组配置」、副标题「本页面镜像展示 BaseLib 自动生成的模组配置项。」）
    //   是 **RitsuLib 的 BaseLib 镜像页**：
    //       src/Settings/ModSettings/Mirrors/BaseLib/BaseLibMirrorSource.cs
    //   它**根本不调 SetupConfigUI**（整个 RitsuLib 源码里 "SetupConfigUI" 出现 **0 次**），
    //   而是自己反射读 BaseLib 的 ConfigProperties，每行文案走
    //       BaseLibMirrorHost.ResolveLabel(属性名) → BaseLib ModConfig.GetLabelText(属性名)
    //       → LocString.GetIfExists("settings_ui", $"DOUSPIRE-{SLUGIFY(属性名)}.title")
    //   查不到就**回退成属性名本身** —— 于是整页都是 UiScale / AiDelay / ShowLog…
    //
    //   ⇒ 既然没人替我们调，就**自己调**：mod 初始化时调一次 + 每次入口装好时再调一次
    //     + 挂上"游戏换语言"的回调。这样不管玩家从哪张页进来，键都早就在表里了。
    //   （RitsuLib 的每行文案是 ModSettingsText.Dynamic，**每次渲染都重新查**，
    //     所以只要在我们渲染之前进表就一定能读到。）
    //
    /// <summary>
    /// 把文案装进 <c>settings_ui</c>，并挂上"换语言时重刷"的回调。**幂等**，多处调用无所谓。
    /// mod 初始化（<see cref="Entry.Initialize"/>）和入口补装（<c>EnsureEntryPoints</c>）各调一次。
    /// </summary>
    public static void InstallLabels()
    {
        InjectLabels();

        if (_localeHooked) return;
        _localeHooked = true;
        try
        {
            LocString.SubscribeToLocaleChange(InjectLabels);
            Entry.Logger.Info("斗地主：已挂上「游戏换语言 → 重刷设置项文案」。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：挂语言切换回调失败（切完语言要重开一次设置页才变）: {e.Message}");
        }
    }

    private static bool _localeHooked;
    private static bool _warnedNoTable;

    /// <summary>
    /// 把本 mod 这批设置项的文案注入游戏的 <c>settings_ui</c> 本地化表。
    ///
    /// ── 为什么不再用"按节点路径改 Label"那招 ──────────────────────────────
    /// 原来是在 <c>base.SetupConfigUI()</c> 之后，按
    /// <c>optionContainer.GetNode&lt;MegaRichTextLabel&gt;("&lt;属性名&gt;/Label")</c>
    /// 钻进去逐行改文字。那是 **BaseLib 内部生成出来的节点结构**，它哪天改一下布局，
    /// 我们这 23 行就全部 <c>catch</c> 掉、**静默变回属性名**（没有任何报错提示）。
    ///
    /// ── 正门在哪 ────────────────────────────────────────────────────────
    /// BaseLib 自己在 <c>Config/ModConfig.cs: GetLabelText()</c> 里就是这么查的：
    /// <code>
    /// LocString.GetIfExists("settings_ui", $"{ModPrefix}{Slugify(属性名)}.title")
    /// </code>
    /// 而 <c>ModPrefix</c> = **根命名空间转大写 + "-"**（<c>Extensions/TypePrefix.cs</c>，
    /// <c>PrefixSplitChar = '-'</c>），所以我们是 <c>"DOUSPIRE-"</c>。
    /// 同规则的真样例见 BaseLib 自带 <c>localization/zhs/settings_ui.json</c>：
    /// 属性 <c>ShowModConfigInMainMenu</c> → 键 <c>BASELIB-SHOW_MOD_CONFIG_IN_MAIN_MENU.title</c>。
    ///
    /// ⚠️ 还有两个可选后缀（键名规则同上）：<c>.hover.title</c> / <c>.hover.desc</c> 悬停提示、
    ///   <c>.sliderFormat</c> 滑条的数值格式串。
    ///
    /// 这里**故意不缓存**：开一次设置页注入一次（合并几十条字符串，便宜），
    /// 这样中途换语言也能立刻跟上，不会留着上一次语言的一批文案。
    /// 出错只影响文字（退回属性名），绝不能让"打开设置页"这件事崩掉。
    /// </summary>
    public static void InjectLabels()
    {
        // ⚠️ 两种"还取不到"都要接住，**不能再静默**：
        //  ① LocManager.Instance 在本地化加载完成前是 null；
        //  ② LocTable 存在性由 LocManager.GetTable 检查，**查不到是抛 LocException**，不是返回 null。
        // 这次的真凶就是原来那句 `if (table is null) return;` —— 一条静默的早退，
        // 日志里既没有成功也没有失败，谁都不知道它压根没干活。
        LocTable? table;
        try
        {
            table = LocManager.Instance?.GetTable("settings_ui");
        }
        catch (System.Exception e)
        {
            WarnNoTable($"settings_ui 表还不在（本地化没加载完）: {e.Message}");
            return;
        }

        if (table is null)
        {
            WarnNoTable("LocManager 还没就绪");
            return;
        }

        try
        {
            bool en = GameIsEnglish();

            table.MergeWith(new Dictionary<string, string>
            {
                // 设置页里**这一页的标题**（RitsuLib 镜像页拿它当 mod 名显示）：
                // 键 = 前缀去掉末尾的 '-' + ".mod_title"（见 BaseLibMirrorHost.ResolveModDisplayNameText）。
                ["DOUSPIRE.mod_title"] = en ? "DouDiZhu Spire" : "斗地主尖塔",

                // ── 牌桌界面 ──
                ["DOUSPIRE-TABLE_SIZE.title"]      = en ? "Players at table" : "牌局人数（4 = 2v2 组队）",
                ["DOUSPIRE-TABLE_SIZE.三人经典"]   = en ? "3 players (classic)" : "3 人（经典斗地主）",
                ["DOUSPIRE-TABLE_SIZE.四人2v2"]    = en ? "4 players (2v2 teams)" : "4 人（2v2 组队）",
                ["DOUSPIRE-UI_SCALE.title"]        = en ? "Table UI scale" : "桌面界面缩放",
                ["DOUSPIRE-UI_THEME.title"]        = en ? "UI style" : "界面风格",
                ["DOUSPIRE-CARD_SKIN.title"]       = en ? "Card faces" : "牌面风格",
                ["DOUSPIRE-SPIRE_CARD_ZOOM.title"] = en ? "Spire card zoom" : "尖塔牌微调大小",
                ["DOUSPIRE-UI_RADIUS.title"]       = en ? "Corner roundness" : "圆角大小",
                ["DOUSPIRE-UI_GLOW.title"]         = en ? "Outer glow" : "外发光强度",
                ["DOUSPIRE-UI_PANEL_ALPHA.title"]  = en ? "Panel opacity" : "面板不透明度",

                // ── 斗地主桌的「室内 / 桌面」（2026-10-10）──
                ["DOUSPIRE-TABLE_SCENE.title"]     = en ? "Dou Dizhu: room scene" : "斗地主：室内背景",
                ["DOUSPIRE-TABLE_STYLE.title"]     = en ? "Dou Dizhu: table style" : "斗地主：桌面",

                // ── 德州扑克（玩法二）──
                ["DOUSPIRE-POKER_SCENE.title"]     = en ? "Poker room scene" : "德州：牌室场景",
                ["DOUSPIRE-POKER_TABLE_STYLE.title"] = en ? "Poker table style" : "德州：牌桌风格",
                ["DOUSPIRE-POKER_AI_STYLE.title"]  = en ? "Poker AI style" : "德州：电脑性格",
                ["DOUSPIRE-POKER_ODDS_HINT.title"] = en ? "Poker: show 'should I call?' hint" : "德州：显示「该不该跟」建议",

                // ── 出牌节奏 / 赌注 ──
                ["DOUSPIRE-AI_DELAY.title"]        = en ? "AI thinking delay" : "电脑出牌停顿",
                ["DOUSPIRE-TURN_TIME_LIMIT.title"] = en ? "Turn time limit (0 = off)" : "出牌时间上限（0 = 不限）",
                ["DOUSPIRE-BASE_STAKE.title"]      = en ? "Base stake" : "底注",
                // ⚠️ 2026-10-08：这三条是当天新加的设置，第一次漏了没注册 ——
                //    结果设置页里直接显示属性名（BetUnitDivisor / BetMaxSwingPercent /
                //    ModCharsPreferSitting），用户"看不太懂英文"就没法调。加设置项时**必须同时加这条**。
                ["DOUSPIRE-BET_UNIT_DIVISOR.title"] =
                    en ? "Bet unit (gold ÷ N)" : "一份底注 = 金币 ÷ N（越大越温和）",
                ["DOUSPIRE-BET_MAX_SWING_PERCENT.title"] =
                    en ? "Max win/loss per game (% of gold)" : "一局最多输赢（占身上金币的%）",
                ["DOUSPIRE-MOD_CHARS_PREFER_SITTING.title"] =
                    en ? "Mod characters: use sitting pose" : "其他 mod 的角色改用坐姿（备选，一般不用动）",
                ["DOUSPIRE-REST_SITE_PICK_SEAT.title"] =
                    en ? "Pick characters at rest site" : "篝火开局弹选角色（默认就是你现在的角色）",
                ["DOUSPIRE-SHOW_LOG.title"]        = en ? "Show play history" : "显示出牌历史",

                // ── 声音 ──
                ["DOUSPIRE-SFX_ENABLED.title"]          = en ? "Sound effects" : "斗地主音效",
                ["DOUSPIRE-SFX_VOLUME.title"]           = en ? "SFX volume" : "音效音量",
                ["DOUSPIRE-MUSIC_ENABLED.title"]        = en ? "Background music" : "背景音乐",
                ["DOUSPIRE-MUSIC_VOLUME.title"]         = en ? "Music volume" : "音乐音量",
                ["DOUSPIRE-MUSIC_CLIMAX_ENABLED.title"] = en ? "Climax music" : "高潮音乐",
                ["DOUSPIRE-VOICE_ENABLED.title"]        = en ? "Voice lines" : "人声台词",
                ["DOUSPIRE-ACTOR_VOICE_ENABLED.title"]  = en ? "Character voice" : "角色配音",
                ["DOUSPIRE-OPPONENT_VOICE_SCALE.title"] = en ? "Opponent voice volume" : "对手那两席的音量",
                // 2026-10-11 加的「语音概率触发」三档（用户："所有语音改成概率触发"）
                ["DOUSPIRE-VOICE_CHANCE_CHATTER.title"] =
                    en ? "Voice chance: chatter (small plays)" : "语音概率：碎嘴（出小牌 / 要不起）",
                ["DOUSPIRE-VOICE_CHANCE_NORMAL.title"] =
                    en ? "Voice chance: normal (combos / my turn)" : "语音概率：普通（顺子三带 / 轮到我）",
                ["DOUSPIRE-VOICE_CHANCE_IMPORTANT.title"] =
                    en ? "Voice chance: key lines (bomb / win / lose)" : "语音概率：关键（炸弹 / 报单 / 胜负）",

                // ── 桌边的角色 ──
                ["DOUSPIRE-LIVE_ACTORS.title"]       = en ? "Live characters" : "用活的角色",
                ["DOUSPIRE-ACTOR_SCALE.title"]       = en ? "Character size" : "角色大小",
                ["DOUSPIRE-ACTOR_Y_OFFSET.title"]    = en ? "Character Y offset" : "角色上下微调",
                ["DOUSPIRE-ACTOR_FACE_CENTER.title"] = en ? "Face the table" : "脸朝桌子中间",

                // ── 枚举下拉的**每一项**（键 = 前缀 + 大写下划线属性名 + "." + 枚举成员名）──
                // 见 BaseLib `Config/UI/NConfigDropdown.cs`：$"{modPrefix}{propertyName}.{value}"
                ["DOUSPIRE-UI_THEME.原样"]     = en ? "Original (green felt + gold)" : "原样（深绿毡 + 金边）",
                ["DOUSPIRE-UI_THEME.星穹铁道"] = en ? "Honkai: Star Rail style" : "崩坏：星穹铁道风",
                ["DOUSPIRE-UI_THEME.原神"]     = en ? "Genshin style" : "原神风",
                ["DOUSPIRE-UI_THEME.明日方舟"] = en ? "Arknights style" : "明日方舟风",
                ["DOUSPIRE-UI_THEME.蔚蓝档案"] = en ? "Blue Archive style" : "蔚蓝档案风",
                ["DOUSPIRE-CARD_SKIN.经典斗地主"] = en ? "Classic poker" : "经典斗地主",
                ["DOUSPIRE-CARD_SKIN.尖塔牌面"]   = en ? "Spire card faces" : "尖塔牌面",
                // ⚠️ 2026-10-09：新加第三档牌风时**必须同时加这一行**，
                //    否则设置页那个下拉里显示的就是原始成员名（这个坑踩过，见上面三条的注释）。
                ["DOUSPIRE-CARD_SKIN.烫金赌桌"]   = en ? "Gilded table" : "烫金赌桌",

                // 斗地主室内背景（2026-10-10 用户："背景直接套用德州的那几个房间画面"）
                // ★ 和德州**共用同一批 bg/poker_*.png**，只是各存各的设置值。
                // ⚠️ 键规则同上；加了新场景必须在这儿加一行（否则下拉显示原始成员名）。
                ["DOUSPIRE-TABLE_SCENE.暖木俱乐部"]   = en ? "Warm wood club" : "暖木俱乐部",
                ["DOUSPIRE-TABLE_SCENE.月夜私人牌室"] = en ? "Moonlit private room" : "月夜私人牌室",
                ["DOUSPIRE-TABLE_SCENE.二次元画风"]   = en ? "Anime (galgame) style" : "二次元画风",
                ["DOUSPIRE-TABLE_SCENE.地下赌窟"]     = en ? "Underground gambling den" : "地下赌窟",
                // ★ 2026-10-11 加（魔搭自由创作的两张新场景）
                ["DOUSPIRE-TABLE_SCENE.雪夜温泉旅馆"] = en ? "Snowy night onsen inn" : "雪夜温泉旅馆",
                ["DOUSPIRE-TABLE_SCENE.雨夜竹林茶室"] = en ? "Rainy bamboo tea room" : "雨夜竹林茶室",

                // 斗地主桌面（同上：先接德州那批 table/table_poker*.png 占位）
                ["DOUSPIRE-TABLE_STYLE.经典绿绒"]   = en ? "Classic green baize" : "经典绿绒",
                ["DOUSPIRE-TABLE_STYLE.暗黑黄铜"]   = en ? "Dark brass & oxblood leather" : "暗黑黄铜·酒红皮革",
                ["DOUSPIRE-TABLE_STYLE.黑曜石"]     = en ? "Obsidian & molten gold" : "黑曜石·熔金纹",
                ["DOUSPIRE-TABLE_STYLE.冰蓝水晶"]   = en ? "Ice blue & moonstone" : "冰蓝水晶·月白",
                ["DOUSPIRE-TABLE_STYLE.二次元明亮"] = en ? "Anime (bright pastel)" : "二次元明亮",
                ["DOUSPIRE-TABLE_STYLE.东方漆器"]   = en ? "Oriental lacquer & gold" : "东方漆器·朱金",
                ["DOUSPIRE-TABLE_STYLE.蒸汽朋克"]   = en ? "Steampunk brass & gears" : "蒸汽朋克·黄铜齿轮",

                // 德州牌室场景（2026-10-09 用户："可以多做几个切换场景"）
                // ⚠️ 键规则同上：`DOUSPIRE-` + 大写下划线属性名 + "." + 枚举成员名。
                //    加了新场景**必须**在这儿加一行，否则下拉里显示原始成员名（这个坑踩过）。
                ["DOUSPIRE-POKER_SCENE.暖木俱乐部"]   = en ? "Warm wood club" : "暖木俱乐部",
                ["DOUSPIRE-POKER_SCENE.月夜私人牌室"] = en ? "Moonlit private room" : "月夜私人牌室",
                ["DOUSPIRE-POKER_SCENE.二次元画风"]   = en ? "Anime (galgame) style" : "二次元画风",
                ["DOUSPIRE-POKER_SCENE.地下赌窟"]     = en ? "Underground gambling den" : "地下赌窟",
                // ★ 2026-10-11 加（魔搭自由创作的两张新场景）
                ["DOUSPIRE-POKER_SCENE.雪夜温泉旅馆"] = en ? "Snowy night onsen inn" : "雪夜温泉旅馆",
                ["DOUSPIRE-POKER_SCENE.雨夜竹林茶室"] = en ? "Rainy bamboo tea room" : "雨夜竹林茶室",

                // 德州牌桌风格（2026-10-09 用户："你可以多想几个不同风格的牌桌"）
                // ⚠️ 键规则同上：`DOUSPIRE-` + 大写下划线属性名 + "." + 枚举成员名。
                //    加了新风格**必须**在这儿加一行，否则下拉里显示原始成员名（这个坑踩过）。
                ["DOUSPIRE-POKER_TABLE_STYLE.经典绿绒"]   = en ? "Classic green baize" : "经典绿绒",
                ["DOUSPIRE-POKER_TABLE_STYLE.暗黑黄铜"]   = en ? "Dark brass & oxblood leather" : "暗黑黄铜·酒红皮革",
                ["DOUSPIRE-POKER_TABLE_STYLE.黑曜石"]     = en ? "Obsidian & molten gold" : "黑曜石·熔金纹",
                ["DOUSPIRE-POKER_TABLE_STYLE.冰蓝水晶"]   = en ? "Ice blue & moonstone" : "冰蓝水晶·月白",
                ["DOUSPIRE-POKER_TABLE_STYLE.二次元明亮"] = en ? "Anime (bright pastel)" : "二次元明亮",
                ["DOUSPIRE-POKER_TABLE_STYLE.东方漆器"]   = en ? "Oriental lacquer & gold" : "东方漆器·朱金",
                ["DOUSPIRE-POKER_TABLE_STYLE.蒸汽朋克"]   = en ? "Steampunk brass & gears" : "蒸汽朋克·黄铜齿轮",

                // 德州电脑性格的每一项（键规则同上：前缀 + 大写下划线属性名 + "." + 枚举成员名）
                ["DOUSPIRE-POKER_AI_STYLE.保守"] = en ? "Tight (few bluffs)" : "保守（很少诈唬）",
                ["DOUSPIRE-POKER_AI_STYLE.正常"] = en ? "Balanced" : "正常",
                ["DOUSPIRE-POKER_AI_STYLE.激进"] = en ? "Aggressive (lots of bluffs)" : "激进（诈唬多）",
            });

            _warnedNoTable = false;
            Entry.Logger.Info("斗地主：设置项文案已注入 settings_ui"
                              + $"（语言={(en ? "eng" : LocManager.Instance?.Language ?? "?")}）。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：注入设置项文案失败（设置页会显示成属性名）: {e.Message}");
        }
    }

    /// <summary>
    /// 取不到表时的告警 —— **只说一次**（这个函数在初始化、入口装好、换语言时都会被调，
    /// 每次都刷屏没意义）。它一旦出现，就说明设置页那一整屏会是属性名。
    /// </summary>
    private static void WarnNoTable(string why)
    {
        if (_warnedNoTable) return;
        _warnedNoTable = true;
        Entry.Logger.Warn($"斗地主：暂时注入不了设置项文案（{why}）—— 这一刻打开设置页会显示成英文属性名；"
                          + "入口装好之后会自动再刷一遍。");
    }
}
