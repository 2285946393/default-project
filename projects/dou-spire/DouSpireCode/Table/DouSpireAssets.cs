using System.Reflection;
using Godot;

namespace DouSpire.Table;

/// <summary>
/// 素材加载层：**每个槽位是一条候选链**，依次尝试：
///
/// <list type="number">
///   <item>美术放在 <c>mods\DouSpire\assets\</c> 里的图（可以随时替换，不用重编 dll）</item>
///   <item>**游戏本体自带的贴图**（<c>res://…</c>）—— 血条、按钮、牌背、面板、字体全都现成</item>
///   <item>都没有 → 返回 null，界面退回纯色块</item>
/// </list>
///
/// 两条加载路径：
/// <list type="bullet">
///   <item><c>res://…</c> 走 <see cref="ResourceLoader"/>（游戏自己的 pck，直接可用）</item>
///   <item>其它走 <see cref="Image.LoadFromFile"/>（我们的 <c>has_pck: false</c>，
///         所以**不能用 res:// 读自己的图**，只能按文件系统绝对路径读）</item>
/// </list>
///
/// 所有游戏内路径都是从 <c>SlayTheSpire2.pck</c> 里扫出来的真实路径。
/// 开桌时会打一条清单日志，明确写出每个槽位最终用了哪张图。
/// </summary>
public static class DouSpireAssets
{
    // ══════════════════════════════════════════════════════════════════
    //  槽位表 —— 候选链，靠前的优先
    //  以 "res://" 开头 = 游戏自带；否则 = 我们自己 assets/ 下的相对路径
    // ══════════════════════════════════════════════════════════════════

    /// <summary>整屏背景。游戏自带的「休息处（篝火）」背景就是整屏的。</summary>
    public static readonly string[] Bg =
    {
        "bg/hall.png",
        "res://images/rooms/rest_sites/overgrowth/overgrowth_rest_site_bg.png",
        "res://images/rooms/rest_sites/underdocks/underdocks_rest_site_bg.png",
        "res://images/rooms/rest_sites/glory/glory_rest_site_00.png",
        "res://images/rooms/rest_sites/hive/hive_rest_site_00.png",
        "res://animations/backgrounds/mainmenu/top/main_menu_top.png",
        "res://animations/backgrounds/neow_room/neow_bg.png",
    };

    /// <summary>暗角遮罩（可选）。游戏休息处自带一张纯遮罩图，正好拿来压暗四角。</summary>
    public static readonly string[] Vignette =
    {
        "fx/vignette.png",
        "res://images/rooms/rest_sites/vignette_2.png",
        "res://animations/backgrounds/neow_room/neow_vignette.png",
    };

    /// <summary>圆桌。游戏里没有现成圆桌，只能美术画（没画就兜底成一块深色椭圆台面）。</summary>
    public static readonly string[] Table =
    {
        "table/table_round.png",
    };

    // ── 斗地主桌的「室内 / 桌面」（2026-10-10）─────────────────────

    /// <summary>
    /// **斗地主桌的整屏背景** —— 按 <see cref="DouSpireConfig.DouSpireTableScene"/> 取图。
    ///
    /// ★★ 2026-10-10 用户：「**背景直接套用德州的那几个房间画面**」⇒ 直接复用
    ///    <see cref="PokerBgOf"/>（同一批 `bg/poker_*.png`）。四个房间都是 1920×1080 同比例，
    ///    铺满整屏不会变形。
    ///
    /// ⚠️ 和德州那条的区别只有一个：**默认房间不同**（斗地主默认暖木俱乐部，
    ///    而德州的默认值存在它自己的 <see cref="DouSpireConfig.PokerScene"/> 里）。
    ///    取不到图时返回**空链** → 调用方垫深色兜底，**绝不掉回 `bg/hall.png` 酒馆**
    ///    （那张圆桌烤在画里，叠斗地主自己的桌图会重影）。
    /// </summary>
    public static string[] DouSpireTableBgOf(DouSpireScene scene) => PokerBgOf(scene);

    /// <summary>
    /// **斗地主桌的台面**（桌上那张桌图的材质 / 配色）—— 按
    /// <see cref="DouSpireConfig.DouSpireTableStyle"/> 取图。
    ///
    /// ★★ 2026-10-10：**新桌图还没画**，所以这里先复用德州那批
    ///    `table/table_poker*.png` 占位。等你按提示词画好斗地主专用的几张之后，
    ///    只要在这里把路径前缀改成（比如）`table/ddz_*.png` 并补上回落链，
    ///    **调用方一行都不用动**。
    ///
    /// ⚠️ 斗地主桌的**功能刻线**（出牌区 / 三张底牌 / 牌堆 / 中央纹章）和德州**不一样**，
    ///    所以德州那批只能算"好看的占位"，不能当最终版 —— 画新图时记得照着斗地主的刻线来。
    /// </summary>
    public static string[] DouSpireTableOf(DouSpireTableStyle style)
    {
        // ── 2026-10-10 深夜**整批重做** ─────────────────────────────────────
        //  上一批四张（`table_ddz_galgame` / `flatposter` / `luminous` / `airylight`）
        //  是"**德州桌换个配色**"—— 中间一条 5 个公共牌位 + 庄家圆 + 座位仪表盘，
        //  用户一眼看穿：「怎么感觉现在这个斗地主的牌桌不太对啊…**直接抄了德州的棋盘**」。
        //  那四张已作废，挪去 `_retired/table-ddz-德州脸-20261010/`。
        //
        //  这一批 **7 款**走朋友三千的**魔搭 Image Studio**（最高优先级，见
        //  `D:\software\dsh-home\AGENTS.md` 六点九），**不挂德州桌当参考图**
        //  （上一批就是被参考图带歪的），刻线严格按斗地主写死的那套：
        //    顶部 4 连底牌框 / 左右双牌堆 / 中央小纹章 / 下方大出牌槽，
        //    中间那一竖条留素（金色 logo 水印叠在那儿）。
        //  出图提示词：`assets/斗地主桌面-魔搭提示词.md`；
        //  收尾脚本：`tools/gen-ddz-tables-ms.py` + `tools/finish-ddz-tables-ms.py`。
        //  **七档风格全部有自己的图了**，链尾仍挂德州桌兜底（新图缺席自动回落，不白屏）。
        var ddz = style switch
        {
            DouSpireTableStyle.经典绿绒   => "table/table_ddz_green",     // 翡翠绒 + 做旧黄铜 + 胡桃木
            DouSpireTableStyle.暗黑黄铜   => "table/table_ddz_brass",     // 酒红皮革 + 黄铜铆钉
            DouSpireTableStyle.黑曜石     => "table/table_ddz_obsidian",  // 近黑石面 + 熔金刻线
            DouSpireTableStyle.冰蓝水晶   => "table/table_ddz_frost",     // 月白石面 + 拉丝银
            DouSpireTableStyle.二次元明亮 => "table/table_ddz_anime",     // 薄荷台面 + 蜜色木（galgame）
            DouSpireTableStyle.东方漆器   => "table/table_ddz_lacquer",   // 朱红漆 + 黑漆 + 金箔
            DouSpireTableStyle.蒸汽朋克   => "table/table_ddz_steam",     // 油布 + 铆接黄铜 + 齿轮
            _ => null,
        };
        if (ddz is null) return PokerTableOf(style);
        var old = PokerTableOf(style);
        var chain = new string[old.Length + 1];
        chain[0] = ddz + ".png";
        for (var i = 0; i < old.Length; i++) chain[i + 1] = old[i];
        return chain;
    }

    // ── 以下是**玩法二（德州扑克）专用**的两条链，斗地主完全不碰 ──────────

    /// <summary>
    /// **德州扑克专用整屏背景** —— **一个「场景」一张**，玩家可以在设置页或牌桌上切换
    /// （<see cref="DouSpireConfig.PokerScene"/>）。用户 2026-10-09：
    /// 「**可以多做几个切换场景**」。
    ///
    /// ★ 为什么德州必须自己一条链（不能借 <see cref="Bg"/>）：那条链的第一张
    /// <c>bg/hall.png</c> 是**斗地主那间酒馆** —— 一张 1920×1080 的暗调酒馆，
    /// **圆桌是烤在画里的**。德州那张绿绒椭圆桌摆进去，就是用户说的
    /// 「**这个桌牌没铺满，旁边也是暗的不好看**」。
    ///
    /// 缺料时返回**空链** → 调用方垫一层暖色渐变（<c>PokerRoomFallback</c>），
    /// **绝不掉回酒馆**：掉回去就是原样重演那个 bug。
    /// </summary>
    public static string[] PokerBgOf(DouSpireScene scene) => new[] { "bg/" + SceneKey(scene) + ".png" };

    /// <summary>场景 → 文件名主干。★ **改这里就要同步改素材文件名**（两处必须一致）。</summary>
    public static string SceneKey(DouSpireScene scene) => scene switch
    {
        DouSpireScene.暖木俱乐部 => "poker_club",
        DouSpireScene.月夜私人牌室 => "poker_night",
        DouSpireScene.二次元画风 => "poker_anime",
        DouSpireScene.地下赌窟 => "poker_den",
        // ★ 2026-10-11 加（魔搭自由创作）
        DouSpireScene.雪夜温泉旅馆 => "poker_onsen",
        DouSpireScene.雨夜竹林茶室 => "poker_bamboo",
        _ => "poker_club",
    };

    /// <summary>场景的**短名**（牌桌右上角那个「场景：…」按钮用，越短越好）。</summary>
    public static string SceneShortName(DouSpireScene scene) => scene switch
    {
        DouSpireScene.暖木俱乐部 => "俱乐部",
        DouSpireScene.月夜私人牌室 => "月夜",
        DouSpireScene.二次元画风 => "二次元",
        DouSpireScene.地下赌窟 => "地窟",
        // ★ 2026-10-11 加 —— 按钮上只放两三个字，越长越挤。
        DouSpireScene.雪夜温泉旅馆 => "温泉",
        DouSpireScene.雨夜竹林茶室 => "竹林",
        _ => "俱乐部",
    };

    /// <summary>场景的**完整名**（日志 / 盘点用）。</summary>
    public static string SceneName(DouSpireScene scene) => scene.ToString();

    /// <summary>
    /// 德州那张赌桌（玩法二）—— **按「牌桌风格」取图**（<see cref="DouSpireConfig.PokerTableStyle"/>），
    /// 设置页里是个下拉，牌桌右上角也有个「牌桌：…」按钮能当场轮换。用户 2026-10-09：
    /// 「**你可以多想几个不同风格的牌桌**」。
    ///
    /// **自带 5 个公共牌位的刻线**，所以必须优先于通用圆桌 ——
    /// 通用圆桌上刻的是斗地主的分区（出牌区、三张底牌、牌堆），德州用不上。
    ///
    /// **七档的功能刻线布局完全一样**（中央 5 个公共牌位 / 底池 / 三个座位 / 庄家位 / 下注牌位），
    /// 代码叠牌的位置是写死的 —— 所以新画一张**可以换台面材质、配色和外圈装饰，但功能布局一个像素都不能动**。
    ///
    /// 缺料时先回落到默认经典绿绒桌（`table/table_poker.png`），再回落到 <see cref="Table"/>（斗地主那张圆桌），
    /// 所以**只画一两张也不会白屏**；未画的新风格会继续显示熟悉的绿绒桌，不会突然变成斗地主刻痕。
    /// </summary>
    public static string[] PokerTableOf(DouSpireTableStyle style)
    {
        var path = "table/" + TableKey(style) + ".png";
        return style == DouSpireTableStyle.经典绿绒
            ? new[] { path }
            : new[] { path, "table/table_poker.png" };
    }

    /// <summary>
    /// **21 点专属台面**（2026-10-11 加）—— 绿绒椭圆 + 深木围栏 + 中央金线椭圆，
    /// 顶上一个小圈（荷官位），下半弧三个圈（三家下注位）。
    ///
    /// 为什么不能直接复用德州那张：德州台面中央刻着 **5 个公共牌位**，
    /// 21 点一张公共牌都没有 —— 摆上去就是用户说的"**这桌怎么什么都没有**"。
    ///
    /// 出图：朋友三千的魔搭 Image Studio（`_work/gen-bj-table.py`），**无文字**，
    /// 功能元素（牌 / 筹码 / 我标记）全用代码叠 —— AI 画英文字母必糊。
    ///
    /// 缺图自动回落德州台面、再回落通用圆桌，**不会白屏**。
    /// </summary>
    public static readonly string[] BlackjackFelt = { "table/table_bj.png" };

    /// <summary>21 点台面的候选链：专属图 → 德州台面 → 通用圆桌。</summary>
    public static string[] BlackjackTableOf(DouSpireTableStyle style)
    {
        var poker = PokerTableOf(style);
        var chain = new string[poker.Length + 1];
        chain[0] = BlackjackFelt[0];
        for (var i = 0; i < poker.Length; i++) chain[i + 1] = poker[i];
        return chain;
    }

    /// <summary>牌桌风格 → 文件名主干。★ **改这里就要同步改素材文件名**（两处必须一致）。</summary>
    public static string TableKey(DouSpireTableStyle style) => style switch
    {
        // ★ 档 0 沿用旧文件名 `table_poker.png` —— 用户已经按 §1.1 出过图了，别改名。
        DouSpireTableStyle.经典绿绒 => "table_poker",
        DouSpireTableStyle.暗黑黄铜 => "table_poker_brass",
        DouSpireTableStyle.黑曜石 => "table_poker_obsidian",
        DouSpireTableStyle.冰蓝水晶 => "table_poker_frost",
        DouSpireTableStyle.二次元明亮 => "table_poker_anime",
        DouSpireTableStyle.东方漆器 => "table_poker_lacquer",
        DouSpireTableStyle.蒸汽朋克 => "table_poker_steam",
        _ => "table_poker",
    };

    /// <summary>牌桌的**短名**（牌桌右上角那个「牌桌：…」按钮用，越短越好）。</summary>
    public static string TableShortName(DouSpireTableStyle style) => style switch
    {
        DouSpireTableStyle.经典绿绒 => "绿绒",
        DouSpireTableStyle.暗黑黄铜 => "黄铜",
        DouSpireTableStyle.黑曜石 => "黑曜",
        DouSpireTableStyle.冰蓝水晶 => "冰蓝",
        DouSpireTableStyle.二次元明亮 => "二次元",
        DouSpireTableStyle.东方漆器 => "漆器",
        DouSpireTableStyle.蒸汽朋克 => "蒸汽",
        _ => "绿绒",
    };

    /// <summary>牌桌的**完整名**（日志 / 盘点用）。</summary>
    public static string TableName(DouSpireTableStyle style) => style.ToString();

    /// <summary>桌面中央的星盘凿刻（可选，也可以直接画进桌子图里）。</summary>
    public static readonly string[] Compass =
    {
        "table/compass_glass.png",
    };

    /// <summary>金皇冠（地主标识）。</summary>
    public static readonly string[] Crown =
    {
        "table/crown.png",
        "res://images/ui/combat/energy_star.png",
    };

    /// <summary>桌前那枚勋章。</summary>
    public static readonly string[] LandlordBadge =
    {
        "table/badge_landlord.png",
        "res://images/ui/combat/block.png",
    };

    /// <summary>两摞牌堆 —— 游戏自带的「抽牌堆 / 弃牌堆」图正好能用。</summary>
    public static readonly string[] DeckPile =
    {
        "table/deck_pile.png",
        "res://images/packed/combat_ui/draw_pile.png",
        "res://images/packed/combat_ui/discard_pile.png",
    };

    /// <summary>轮到谁的光环。</summary>
    public static readonly string[] TurnGlow =
    {
        "fx/turn_glow.png",
        "res://images/ui/combat/combat_reticle.png",
    };

    /// <summary>牌背。</summary>
    public static readonly string[] CardBack =
    {
        "cards/card_back.png",
        "res://images/packed/run_history/card_back.png",
        "res://images/timeline/epoch_card_back.png",
        "res://images/events/crystal_sphere/crystal_sphere_card_back.png",
    };

    /// <summary>牌面底板（不画点数花色，只要底板）。</summary>
    public static readonly string[] CardFace =
    {
        "cards/card_face.png",
        "res://images/atlases/ui_atlas.sprites/card/card_frame_skill_s.tres",
        "res://images/atlases/ui_atlas.sprites/card/card_frame_attack_s.tres",
    };

    /// <summary>牌背作废/未使用时的空位框（可选）。</summary>
    public static readonly string[] CardSlotEmpty =
    {
        "cards/card_slot_empty.png",
    };

    /// <summary>选中牌的高亮框（可选；没有就只做提亮）。</summary>
    public static readonly string[] CardFrameSelected =
    {
        "cards/card_frame_selected.png",
    };

    /// <summary>
    /// 牌面底图 —— **2026-10-07 换成用户给的豆包牌框**（<c>cards/card_slot_empty.png</c>：
    /// 深色石纹描边 + 米白牌心）。自己画的那张留在后面兜底。
    /// </summary>
    public static readonly string[] CardBody =
    {
        // 2026-10-07 晚：豆包新出的黑框牌面（洋红底已在 _work/key_cards.py 里抠成透明）
        "cards/card_face_std.png",
        "cards/card_slot_empty.png",
        "cards/card_body.png",
    };

    /// <summary>
    /// 选中高亮框（豆包画的 <c>cards/card_frame_selected.png</c>：金紫描边 + 粉色外发光，中间透明）。
    /// **叠在牌面上**用，不是替换牌面 —— 这样选没选中都是同一套牌面美术。
    /// </summary>
    public static readonly string[] CardSelFrame =
    {
        "cards/card_frame_selected.png",
    };

    /// <summary>HUD 底板（名牌素材）—— 替掉那块半透明纯色板。</summary>
    public static readonly string[] HudPlate =
    {
        "hud/nameplate.png",
    };

    /// <summary>选中态的牌面底图（暖金 + 外发光）。</summary>
    public static readonly string[] CardBodyOn =
    {
        // 金框 + 外发光 = 选中的牌（同一套豆包牌面）
        "cards/card_face_sel.png",
        "cards/card_body_on.png",
    };

    /// <summary>
    /// 桌面中央的杀戮尖塔 2 主题水印。
    ///
    /// ★ 2026-10-10 换成**彩色渲染版**（用户："换成那种渲染过的版本"）：
    ///   首选游戏本体主菜单那个金色 "Slay the spire" + 青色 "II" 的 logo。
    ///
    /// ⚠️ 走 <c>res://</c> 引用**游戏本体**的资源，不把 Mega Crit 的图拷进 <c>assets/</c>
    ///   —— 见 <c>参考图/README.md</c> 的版权提醒（"不要拷进 assets/ 打进 mod 发布"）。
    ///   <c>table/watermark.png</c> 就是当年被拷进来的那张白描版（960×484），
    ///   只作为**取不到本体资源时的兜底**保留，不再是首选。
    /// </summary>
    public static readonly string[] Watermark =
    {
        "res://animations/backgrounds/mainmenu/logo/main_menu_logo.png",
        "table/watermark.png",
    };

    /// <summary>主按钮（出牌 / 开始）。游戏自带「结束回合」按钮就是个大金按钮。</summary>
    public static readonly string[] BtnPrimary =
    {
        "buttons/btn_primary.png",
        "res://images/packed/combat_ui/end_turn_button.png",
        "res://images/atlases/ui_atlas.sprites/confirm_button.tres",
        "res://images/packed/common_ui/event_button.png",
    };

    /// <summary>次按钮（要不起 / 提示）。</summary>
    public static readonly string[] BtnSecondary =
    {
        "buttons/btn_secondary.png",
        "res://images/packed/common_ui/event_button.png",
        "res://images/atlases/ui_atlas.sprites/proceed_button.tres",
        "res://images/atlases/ui_atlas.sprites/confirm_button.tres",
    };

    /// <summary>右上角圆形按钮（收桌）。</summary>
    public static readonly string[] BtnRound =
    {
        "buttons/btn_round.png",
        "res://images/atlases/ui_atlas.sprites/back_button.tres",
        "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_settings.tres",
    };

    // ══════════════════════════════════════════════════════════
    //  德州桌面素材（2026-10-09 接入）
    //
    //  ★★ 背景：这批图 **18:51 就入库了**（`assets/poker/`、`assets/fx/`），
    //     但**代码里一条都没引用** —— 用户 2026-10-09 问
    //     「**要我画的那些筹码和那些 ui 怎么没用上？**」。
    //     以前德州桌的界面全是**代码用纯色块现画**的（底池＝一行黄字、按钮＝通用按钮），
    //     国际台先做了能跑的功能，素材后到，**接线一直没做**。这里补上。
    //  ★ 全部**只在图真的存在时生效**；缺图时调用方各自退回纯色 / 纯文字，
    //     不会白块（和 <see cref="First"/> 的老规矩一致）。
    //  ★ 接在哪：底池牌匾 / 筹码堆 / 牌型牌匾 / 动作图标 / 庄家按钮 ——
    //     见 `DouSpirePokerTable` 的 BuildScene ⑥⑦、MakeActionButton、BuildSeats。
    // ══════════════════════════════════════════════════════════

    /// <summary>一摞筹码（底池旁边那个）。</summary>
    public static readonly string[] PokerChipStack = { "poker/chip_stack.png" };

    /// <summary>单枚筹码。</summary>
    public static readonly string[] PokerChipSingle = { "poker/chip_single.png" };

    /// <summary>庄家按钮（D 字圆牌）—— 摆在庄家那一席的信息框角上。</summary>
    public static readonly string[] PokerDealer = { "poker/dealer_button.png" };

    /// <summary>盲注标记（还没接）。</summary>
    public static readonly string[] PokerBlind = { "poker/blind_marker.png" };

    /// <summary>底池牌匾 —— 垫在"底池 300"底下那块横牌。</summary>
    public static readonly string[] PokerPotPlate = { "poker/pot_plate.png" };

    /// <summary>公共牌牌匾（自带 5 个格子，暂未接 —— 会跟台面刻线打架）。</summary>
    public static readonly string[] PokerBoardPlate = { "poker/board_plate.png" };

    /// <summary>牌型牌匾 —— 垫在"你现在是：一对 A"底下那块细牌。</summary>
    public static readonly string[] PokerHandRankPlate = { "poker/handrank_plate.png" };

    /// <summary>通用木牌匾（日志框 / 选人面板用）。</summary>
    public static readonly string[] PokerPlateWood = { "poker/plate_wood.png" };

    /// <summary>赢家光环（结算时套在赢家身上）。</summary>
    public static readonly string[] PokerWinnerHalo = { "fx/winner_halo.png" };

    /// <summary>牌型图标（`rank_1`~`rank_10`，1 = 最弱的牌型）。</summary>
    public static string[] PokerRankIcon(int index1To10) =>
        new[] { $"poker/rank_{Mathf.Clamp(index1To10, 1, 10)}.png" };

    /// <summary>动作图标：弃牌 / 过牌 / 跟注 / 加注 / 全下。</summary>
    public static readonly string[] PokerActFold = { "poker/act_fold.png" };
    public static readonly string[] PokerActCheck = { "poker/act_check.png" };
    public static readonly string[] PokerActCall = { "poker/act_call.png" };
    public static readonly string[] PokerActRaise = { "poker/act_raise.png" };
    public static readonly string[] PokerActAllIn = { "poker/act_allin.png" };

    public static readonly string[] ArrowLeft =
    {
        "ui/btn_arrow_left.png",
        "res://images/packed/common_ui/settings_tiny_left_arrow.png",
        "res://images/atlases/ui_atlas.sprites/settings_tiny_left_arrow.tres",
    };

    public static readonly string[] ArrowRight =
    {
        "ui/btn_arrow_right.png",
        "res://images/packed/common_ui/settings_tiny_right_arrow.png",
        "res://images/atlases/ui_atlas.sprites/settings_tiny_right_arrow.tres",
    };

    /// <summary>「我」的头像框（游戏顶栏那个角色底框）。</summary>
    public static readonly string[] FrameCrest =
    {
        "hud/avatar_frame_crest.png",
        "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_char_backdrop.tres",
    };

    /// <summary>对家的头像框。</summary>
    public static readonly string[] FrameHex =
    {
        "hud/avatar_frame_hex.png",
        "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_char_backdrop.tres",
    };

    /// <summary>
    /// 头像底板 —— 从**官方 UI 素材包**里拷出来的顶栏角色底板（`images/atlases/ui_atlas.sprites/top_bar/top_bar_char_backdrop.png`）。
    /// 做成正方形贴着头像用，框和头像一样大。
    /// </summary>
    public static readonly string[] AvatarPlate =
    {
        "hud/plate_topbar.png",
        "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_char_backdrop.tres",
    };

    /// <summary>剩牌条的底槽 —— 直接借游戏的 HP 条。</summary>
    public static readonly string[] BarBg =
    {
        "hud/bar_bg.png",
        "res://images/ui/combat/health_bar_bg.png",
    };

    public static readonly string[] BarFillRed =
    {
        "hud/bar_fill_red.png",
        "hud/bar_fill.png",                       // 官方包里的原图（我们自己拷的，保证读得到）
        "res://images/ui/combat/health_bar_fill.png",
    };

    public static readonly string[] BarFillGreen =
    {
        "hud/bar_fill_green.png",
        "hud/bar_fill.png",
        "res://images/ui/combat/health_bar_fill.png",
    };

    /// <summary>
    /// **金币图标** —— 借游戏自己的「钱」图标
    ///（用户 2026-10-07："三个地方显示三个人的钱的 ui（尖塔自带的钱的样子）"）。
    /// 游戏资源都打包在 pck 里，路径只能靠试：自己放的图优先，其余是几个常见位置。
    /// 开桌时日志会写最终命中哪一条。
    /// </summary>
    public static readonly string[] GoldIcon =
    {
        "hud/gold.png",
        "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_gold.tres",
        "res://images/atlases/ui_atlas.sprites/top_bar/gold.tres",
        "res://images/ui/top_bar/gold.png",
        "res://images/packed/common_ui/gold.png",
        "res://images/ui/combat/gold.png",
        "res://images/atlases/ui_atlas.sprites/ui/gold.tres",
    };

    /// <summary>小宝石 —— 借游戏的「能量」图标（红/绿一眼区分地主与农民）。</summary>
    public static readonly string[] OrbRed =
    {
        "hud/orb_red.png",
        "res://images/atlases/ui_atlas.sprites/card/energy_ironclad.tres",
        "res://images/ui/combat/energy_counters/ironclad/ironclad_orb_layer_1.png",
    };

    public static readonly string[] OrbGreen =
    {
        "hud/orb_green.png",
        "res://images/atlases/ui_atlas.sprites/card/energy_silent.tres",
        "res://images/ui/combat/energy_counters/silent/silent_orb_layer_1.png",
    };

    /// <summary>顶部标题条 —— 借游戏的横幅 / 名牌底。</summary>
    public static readonly string[] Title =
    {
        "ui/title_banner.png",
        "res://images/packed/run_history/banner.png",
        "res://images/ui/reward_screen/reward_banner.png",
        "res://images/ui/combat/combat_nameplate_background.png",
    };

    /// <summary>选人面板底 —— 借游戏的九宫格面板（popup_vertical 是个标准九宫格）。</summary>
    public static readonly string[] PanelBg =
    {
        "ui/panel_bg.png",
        "res://images/atlases/ui_atlas.sprites/popup_vertical.tres",
        "res://images/packed/common_ui/submenu_panel.png",
        "res://images/ui/fuzzy_nine_patch_char_select.png",
        "res://images/ui/dialogue_nine_patch.png",
    };

    public static readonly string[] ToastBg =
    {
        "ui/toast_bg.png",
        "res://images/ui/tiny_nine_patch.png",
        "res://images/ui/dialogue_nine_patch.png",
    };

    /// <summary>
    /// 桌面各面板（标题条 / 日志 / HUD 底板 / 状态条）的通用底 —— 方向二：全部倒向游戏本体。
    /// 原来各自画"深色玻璃"StyleBox，跟写实木桌三套画风打架（Codex 2026-10-07 诊断，
    /// 用户拍板「用游戏原来的。然后卡图你就不要改了」）。
    /// popup_vertical 就是游戏弹窗自己的深棕皮革九宫格；美术想换底，丢一张
    /// <c>mods\DouSpire\assets\table\panel_leather.png</c> 即可，不用重编。
    /// </summary>
    public static readonly string[] PanelLeather =
    {
        "table/panel_leather.png",
        "res://images/atlases/ui_atlas.sprites/popup_vertical.tres",
        "res://images/packed/common_ui/submenu_panel.png",
        "res://images/ui/fuzzy_nine_patch_char_select.png",
        "res://images/ui/dialogue_nine_patch.png",
    };

    /// <summary>
    /// 扁档皮革底：给标题横幅这种**很扁**的面板用（900×84 UI）。
    /// 同一张皮革图缩得更小（360×240），包角只有约 47px，边距 40 也不会把黄铜角拉花；
    /// 主档（<see cref="PanelLeather"/>，480×320）包角约 62px，只给 HUD / 日志这些较高的面板用。
    /// </summary>
    public static readonly string[] PanelLeatherSlim =
    {
        "table/panel_leather_slim.png",
        "res://images/atlases/ui_atlas.sprites/popup_vertical.tres",
        "res://images/packed/common_ui/submenu_panel.png",
    };

    /// <summary>标题字体（游戏自带的中文衬线粗体，配斗地主很对味）。</summary>
    public static readonly string[] FontTitle =
    {
        "fonts/title.otf",
        "res://fonts/zhs/SourceHanSerifSC-Bold.otf",
        "res://fonts/zhs/SourceHanSerifSC-Medium.otf",
    };

    /// <summary>牌面点数用的字体（拉丁衬线更有牌感）。</summary>
    public static readonly string[] FontPip =
    {
        "fonts/pip.ttf",
        "res://fonts/spectral_bold.ttf",
        "res://fonts/zhs/SourceHanSerifSC-Bold.otf",
    };

    /// <summary>角色座位立绘：`seats/char_&lt;entry&gt;.png`（entry 是游戏内部 id）。</summary>
    public static string[] SeatOf(string entry) => new[] { $"seats/char_{entry}.png" };

    /// <summary>游戏里能玩的那五个角色 —— 只有他们各有自己一套联机手势图。</summary>
    public static readonly string[] GameHandChars = { "ironclad", "silent", "defect", "necrobinder", "regent" };

    /// <summary>手势代号 → 文件名里的英文：0 = 石头，1 = 剪刀，2 = 布，其它 = 指（准备中）。</summary>
    public static string HandGestureName(int g) => g switch
    {
        0 => "rock",
        1 => "scissors",
        2 => "paper",
        _ => "point",
    };

    /// <summary>
    /// 联机手势图（石头剪刀布）—— **游戏自带**，每个角色一套：
    /// <c>res://images/ui/hands/multiplayer_hand_&lt;角色&gt;_&lt;手势&gt;.png</c>。
    /// 编外角色（鲸鱼娘这些）没有自己的，借摄政王那套。
    /// </summary>
    public static Texture2D? Hand(string entry, int gesture)
    {
        // ⚠️ **大小写不敏感**地比 —— 游戏给的 `Id.Entry` 可能是 `Ironclad`，
        // 我们表里写的是小写 `ironclad`。用 Array.IndexOf 比会一个都匹配不上，
        // 三家全退回摄政王（用户 2026-10-07："猜拳时我用的原版的三个角色，出来的却是三个储君的手"）。
        var e = "regent";
        foreach (var cand in GameHandChars)
        {
            if (string.Equals(cand, entry, System.StringComparison.OrdinalIgnoreCase))
            {
                e = cand;
                break;
            }
        }
        return One($"res://images/ui/hands/multiplayer_hand_{e}_{HandGestureName(gesture)}.png");
    }

    // ══════════════════════════════════════════════════════════════════
    //  尖塔牌面模式（2026-10-07）
    //  点数照旧是斗地主那 13 点 + 大小王，**只把"脸"换成尖塔的卡**。
    //  15 张分布：5 个职业各 2 张 + 无色 2 张 + 先古（事件牌）3 张。
    //  卡图全部借游戏本体的 card_atlas，不是我们自己画的。
    // ══════════════════════════════════════════════════════════════════

    /// <summary>斗地主点数 → 借用的那张尖塔卡（card_atlas 下的相对路径）。</summary>
    /// <remarks>3..10 就是数字，J=11 Q=12 K=13 A=14 2=15，小王=16 大王=17（见 <c>Ranks</c>）。</remarks>
    private static string? SpireFaceId(int rank) => rank switch
    {
        3 => "ironclad/strike_ironclad",            // 打击
        4 => "ironclad/defend_ironclad",            // 防御
        5 => "silent/backstab",                     // 背刺
        6 => "silent/blade_dance",                  // 刀刃之舞
        7 => "defect/ball_lightning",               // 球状闪电
        8 => "defect/cold_snap",                    // 寒流
        9 => "necrobinder/bone_shards",             // 碎骨
        10 => "necrobinder/deaths_door",            // 死亡之门
        11 => "regent/big_bang",                    // 大爆炸
        12 => "regent/black_hole",                  // 黑洞
        13 => "colorless/dramatic_entrance",        // 闪亮登场
        14 => "colorless/finesse",                  // 妙计
        15 => "event/enlightenment",                // 开悟
        16 => "event/apparition",                   // 灵体（小王）
        17 => "event/apotheosis",                   // 神化（大王）
        _ => null,
    };

    /// <summary>尖塔牌面模式：这张点数的卡图（借游戏本体）。</summary>
    public static Texture2D? SpireFace(int rank)
    {
        var id = SpireFaceId(rank);
        return id is null ? null : One("res://images/atlases/card_atlas.sprites/" + id + ".tres");
    }

    /// <summary>尖塔牌面模式：这张点数叫什么（抄游戏官方中文名）。</summary>
    public static string SpireFaceName(int rank) => rank switch
    {
        3 => "打击",
        4 => "防御",
        5 => "背刺",
        6 => "刀刃之舞",
        7 => "球状闪电",
        8 => "寒流",
        9 => "碎骨",
        10 => "死亡之门",
        11 => "大爆炸",
        12 => "黑洞",
        13 => "闪亮登场",
        14 => "妙计",
        15 => "开悟",
        16 => "灵体",
        17 => "神化",
        _ => "",
    };

    /// <summary>
    /// 这点数对应卡的**游戏内部类名**（<c>Models.Cards.Xxx</c>）。
    /// 拿它去卡表里精确找模型 —— **不能用中文名找**：
    /// mod 也会加中文名一样的卡，而且本地化一改中文名就全错
    ///（用户 2026-10-07："有的牌发了有的牌没发，还发到 mod 角色的卡了"）。
    /// </summary>
    public static string SpireCardClass(int rank) => rank switch
    {
        3 => "StrikeIronclad",
        4 => "DefendIronclad",
        5 => "Backstab",
        6 => "BladeDance",
        7 => "BallLightning",
        8 => "ColdSnap",
        9 => "BoneShards",
        10 => "DeathsDoor",
        11 => "BigBang",
        12 => "BlackHole",
        13 => "DramaticEntrance",
        14 => "Finesse",
        15 => "Enlightenment",
        16 => "Apparition",
        17 => "Apotheosis",
        _ => "",
    };

    /// <summary>
    /// 尖塔牌面：这张点数该用**哪种卡框**（游戏自带，按卡的真实类型分）。
    /// 有游戏卡表就用真数据；读不到才按内置表猜。
    /// </summary>
    public static string SpireFrameKind(int rank)
    {
        var kind = DouSpireCharacters.CardKindOf(SpireFaceName(rank));
        if (kind is not null) return kind;
        return rank switch
        {
            3 or 5 or 7 or 8 or 9 or 13 => "attack",    // 打击 / 背刺 / 球状闪电 / 寒流 / 碎骨 / 闪亮登场
            12 => "power",                               // 黑洞
            _ => "skill",
        };
    }

    /// <summary>尖塔牌面：整张牌用的卡框图。</summary>
    public static Texture2D? SpireFrame(int rank)
        => One($"res://images/atlases/ui_atlas.sprites/card/card_frame_{SpireFrameKind(rank)}_s.tres");

    /// <summary>
    /// 卡框图**没加载出来**时的兜底底色 —— 按类型给个一眼能分的颜色
    ///（攻击红 / 技能绿 / 能力蓝 / 任务黄）。
    /// 之前这里一律退回同一块纯色板，结果所有牌一个颜色，用户："框全是同一个颜色"。
    /// </summary>
    public static Color SpireTypeColor(string kind) => kind switch
    {
        "attack" => new Color(0.62f, 0.19f, 0.16f),
        "power" => new Color(0.20f, 0.38f, 0.62f),
        "quest" => new Color(0.72f, 0.58f, 0.18f),
        _ => new Color(0.22f, 0.46f, 0.28f),          // skill 绿
    };

    /// <summary>尖塔牌面：卡图外面那圈画框（比卡框更贴住画面的那一层）。</summary>
    public static Texture2D? SpirePortraitBorder(int rank)
        => One($"res://images/atlases/ui_atlas.sprites/card/"
               + $"card_portrait_border_{SpireFrameKind(rank)}_s.tres");

    /// <summary>
    /// 开桌时把 15 张尖塔牌面**逐条报到日志里**（卡图 / 卡框 / 画框各自读到没有）。
    /// 卡框读不到就退回纯色兜底，那时"所有牌一个颜色"，看这条就知道是不是这条路的问题。
    /// </summary>
    public static void ReportSpireOnce()
    {
        if (_spireReported) return;
        _spireReported = true;
        try
        {
            var bad = new List<string>();
            for (var rank = 3; rank <= 17; rank++)
            {
                var art = SpireFace(rank) != null;
                var frame = SpireFrame(rank) != null;
                var border = SpirePortraitBorder(rank) != null;
                if (!art || !frame)
                    bad.Add($"{RanksName(rank)}({SpireFaceName(rank)}) 图{(art ? "有" : "无")} 框{(frame ? "有" : "无")} 画框{(border ? "有" : "无")}");
            }
            var line = bad.Count == 0
                ? "斗地主：尖塔牌面 15 张，卡图 + 卡框全部读到。"
                : "斗地主：尖塔牌面缺料 → " + string.Join("；", bad);
            Entry.Logger.Info(line);
            Diag(line);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：尖塔牌面自检失败: {e.Message}");
        }
    }

    private static bool _spireReported;

    private static string RanksName(int rank) => rank switch
    {
        11 => "J", 12 => "Q", 13 => "K", 14 => "A", 15 => "2",
        16 => "小王", 17 => "大王", _ => rank.ToString(),
    };

    /// <summary>
    /// 尖塔牌面模式：**花色 → 那一位角色的配色**（用户 2026-10-07 定的"花色换成角色色"）。
    /// ♠ 铁甲战士（红）· ♥ 沉默猎手（绿）· ♣ 故障机器人（蓝）· ♦ 亡灵束缚者（紫）。
    /// 颜色优先取游戏里角色自己的 NameColor，取不到才用兜底值。
    /// </summary>
    public static Color SuitColor(DouSpireRules.Suit s) => s switch
    {
        DouSpireRules.Suit.Spade => DouSpireCharacters.EntryColor("ironclad", new Color(0.78f, 0.24f, 0.20f)),
        DouSpireRules.Suit.Heart => DouSpireCharacters.EntryColor("silent", new Color(0.30f, 0.66f, 0.42f)),
        DouSpireRules.Suit.Club => DouSpireCharacters.EntryColor("defect", new Color(0.30f, 0.55f, 0.85f)),
        DouSpireRules.Suit.Diamond => DouSpireCharacters.EntryColor("necrobinder", new Color(0.60f, 0.36f, 0.75f)),
        _ => new Color(0.95f, 0.80f, 0.35f),      // 王：金
    };

    // ══════════════════════════════════════════════════════════════════
    //  烫金牌面（第三档牌风，配德州扑克赌桌）—— 2026-10-09
    //
    //  ★ 设计要点：**每一条链的末尾都指回经典斗地主那套**。
    //    用户是一张一张画的，可能今天只有花色、明天才有 J/Q/K；
    //    缺哪张就自动退回经典的同位置素材，牌桌上永远不会出现空洞。
    //    也就是说：**这套图一落地就立刻生效，不用等凑齐、不用改代码。**
    //
    //  目录：assets/cards/gilded/（由 tools/poker-assets.py --set gilded 生成）
    // ══════════════════════════════════════════════════════════════════

    /// <summary>烫金牌面：这张点数的字形图。缺就退回经典那套字形。</summary>
    public static string[] GildedPip(int rank) => new[]
    {
        $"cards/gilded/pip_{RanksName(rank)}.png",
        $"cards/pip_{RanksName(rank)}.png",
    };

    /// <summary>烫金牌面：花色图。缺就退回经典那套花色。</summary>
    public static string[] GildedSuit(DouSpireRules.Suit s)
    {
        var name = s switch
        {
            DouSpireRules.Suit.Spade => "spade",
            DouSpireRules.Suit.Heart => "heart",
            DouSpireRules.Suit.Club => "club",
            DouSpireRules.Suit.Diamond => "diamond",
            _ => "joker",
        };
        return new[] { $"cards/gilded/suit_{name}.png", $"cards/suit_{name}.png" };
    }

    /// <summary>烫金牌面：牌底图（普通 / 选中各一张）。缺就退回经典那套牌底。</summary>
    public static string[] GildedBody(bool selected) => selected
        ? new[] { "cards/gilded/card_face_sel.png", "cards/card_face_sel.png", "cards/card_body_on.png" }
        : new[] { "cards/gilded/card_face.png", "cards/card_face_std.png", "cards/card_body.png" };

    /// <summary>烫金牌面：牌背。缺就退回经典牌背。</summary>
    public static string[] GildedBack => new[] { "cards/gilded/card_back.png", "cards/card_back.png" };

    /// <summary>
    /// 烫金牌面自检 —— 开桌时把这 20 张**逐条报到日志里**（哪些是自己画的、哪些还在用经典的）。
    /// 用户正在一张张画，这条日志就是"我这张到底生效了没"的答案。
    /// </summary>
    public static void ReportGildedOnce()
    {
        if (_gildedReported) return;
        _gildedReported = true;
        try
        {
            var mine = new List<string>();
            var miss = new List<string>();
            foreach (var n in new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" })
                (Has($"cards/gilded/pip_{n}.png") ? mine : miss).Add($"点数{n}");
            foreach (var n in new[] { "spade", "heart", "club", "diamond" })
                (Has($"cards/gilded/suit_{n}.png") ? mine : miss).Add($"花色{n}");
            foreach (var n in new[] { "card_face", "card_face_sel", "card_back" })
                (Has($"cards/gilded/{n}.png") ? mine : miss).Add(n);

            var line = $"斗地主：烫金牌面 已入库 {mine.Count}/20"
                       + (mine.Count > 0 ? $"（{string.Join("、", mine)}）" : "")
                       + (miss.Count > 0 ? $"；仍用经典兜底 {miss.Count} 张" : "；全套齐了。");
            Entry.Logger.Info(line);
            Diag(line);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：烫金牌面自检失败: {e.Message}");
        }
    }

    private static bool _gildedReported;

    /// <summary>猜拳那块的"宝箱"布景 —— 借宝物房那张整图（第一幕的箱子）。</summary>
    public static readonly string[] ChestRoom =
    {
        "res://animations/backgrounds/treasure_room/chest_room_act_1.png",
        "res://animations/backgrounds/treasure_room/chest_room_act_2.png",
        "res://images/atlases/ui_atlas.sprites/map/icons/map_chest.tres",
        "res://images/ui/run_history/treasure.png",
    };

    /// <summary>花色图标（游戏里**没有**花色贴图，只能靠字体画 Unicode 符号）。</summary>
    public static string[] SuitOf(DouSpireRules.Suit s) => s switch
    {
        DouSpireRules.Suit.Spade => new[] { "cards/suit_spade.png" },
        DouSpireRules.Suit.Heart => new[] { "cards/suit_heart.png" },
        DouSpireRules.Suit.Club => new[] { "cards/suit_club.png" },
        DouSpireRules.Suit.Diamond => new[] { "cards/suit_diamond.png" },
        _ => new[] { "cards/suit_joker.png" },
    };

    public static string[] JokerOf(bool big) =>
        new[] { big ? "cards/joker_big.png" : "cards/joker_small.png" };

    /// <summary>开桌盘点用的槽位清单（名字 → 候选链）。</summary>
    private static IEnumerable<(string Name, string[] Chain)> AllSlots()
    {
        yield return ("背景", Bg);
        yield return ("暗角", Vignette);
        yield return ("圆桌", Table);
        foreach (var style in DouSpireTableStyles.All)
            yield return ($"德州赌桌({TableName(style)})", PokerTableOf(style));
        foreach (var scene in DouSpireScenes.All)
            yield return ($"德州背景({SceneName(scene)})", PokerBgOf(scene));
        yield return ("星盘", Compass);
        yield return ("皇冠", Crown);
        yield return ("地主勋章", LandlordBadge);
        yield return ("牌堆", DeckPile);
        yield return ("回合光环", TurnGlow);
        yield return ("牌背", CardBack);
        yield return ("牌面底板", CardFace);
        yield return ("空位框", CardSlotEmpty);
        yield return ("选中框", CardFrameSelected);
        yield return ("牌面底图", CardBody);
        yield return ("牌面底图(选中)", CardBodyOn);
        yield return ("桌面水印", Watermark);
        yield return ("主按钮", BtnPrimary);
        yield return ("次按钮", BtnSecondary);
        yield return ("圆按钮", BtnRound);
        yield return ("左箭头", ArrowLeft);
        yield return ("右箭头", ArrowRight);
        yield return ("我的头像框", FrameCrest);
        yield return ("对家头像框", FrameHex);
        yield return ("头像底板(官方)", AvatarPlate);
        yield return ("剩牌条底", BarBg);
        yield return ("剩牌条红", BarFillRed);
        yield return ("剩牌条绿", BarFillGreen);
        yield return ("红宝石", OrbRed);
        yield return ("绿宝石", OrbGreen);
        yield return ("标题条", Title);
        yield return ("面板底", PanelBg);
        yield return ("标题字体", FontTitle);
        yield return ("点数字体", FontPip);
        // ★★ 2026-10-11：`seats/char_*.png`（9 张）和 `seats/icon_*.png`（4 张）
        //   已按用户要求**全部删除** —— 座位立绘不再走文件：
        //     本体 5 位 = 游戏自己的活模型（<see cref="DouSpireActor"/>），没有文件可盘；
        //     四位客人 = 待机动画（`&lt;id&gt;/idle/fNN.png`）。
        //   所以这里**不再列"立绘"槽位** —— 否则每次开桌盘点都会报 9 个"没有素材"的假警报。
        //   客人的待机帧照盘：这套真少了才是问题。
        foreach (var g in DouSpireCharacters.Guests)
            yield return ($"待机帧({g.Name})", new[] { $"{g.Id}/idle/f00.png" });
    }

    // ══════════════════════════════════════════════════════════════════

    // ══════════════════════════════════════════════════════════════════
    //  诊断日志（牌桌诊断 + 语音 + 音乐 + 特效，全部走这一个出口）
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 我们自己那份诊断日志的绝对路径（只解析一次）。
    ///
    /// ── 为什么不再写在 mod 目录里 ──────────────────────────────────────
    /// 原来写 `&lt;mod&gt;/../voice-log.txt`（就是 <c>mods/</c> 那层），有三个毛病：
    /// <list type="number">
    ///   <item>**订阅来的 mod 目录是只读的**（创意工坊），写不进去 —— 普通人根本拿不到这份日志，
    ///         只有自己编译的开发机能写。等于这个诊断对外全是废的。</item>
    ///   <item>mod 目录**不受 Steam "验证文件完整性" 管理**，但也不受清理保护 ——
    ///         用户实测这个文件已经涨到 **611KB / 7809 行**，还没人清。</item>
    ///   <item>它叫 voice-log 却还混着音乐、特效、卡牌自检的输出，名不副实。</item>
    /// </list>
    ///
    /// 现在改成 Godot 的 **<c>user://</c>**（本机实测 =
    /// <c>%APPDATA%/Godot/app_userdata/&lt;项目名&gt;/</c>）：
    /// 任何安装方式下都可写，且由游戏自己管，属于"标准位置"。
    /// 拿不到 user:// 时再退回老位置（至少开发机上还能写）。
    /// </summary>
    private static string DiagPath()
    {
        if (_diagPath.Length != 0) return _diagPath;
        try
        {
            var dir = ProjectSettings.GlobalizePath("user://");
            if (!string.IsNullOrEmpty(dir))
            {
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                // ★ 文件名跟着 Entry.ModId 走 —— 开发版（DouSpireDev）和工坊版（DouSpire）
                //   同时装着时各写各的日志，排查问题不会互相盖（2026-10-09 改名时顺手改）。
                return _diagPath = dir.Replace('\\', '/').TrimEnd('/') + "/" + Entry.ModId + "-diagnostics.log";
            }
        }
        catch (System.Exception) { /* 拿不到就退回下面的老位置 */ }

        var root = RootDir;
        return _diagPath = string.IsNullOrEmpty(root)
            ? "" : root + "/../voice-log.txt";
    }

    private static string _diagPath = "";

    /// <summary>
    /// 往诊断日志里追一行（自动带时间戳；**任何失败都吞掉**，日志绝不能把牌桌搞崩）。
    /// 想加新的诊断输出，一律走这里，不要再自己 <c>File.AppendAllText</c>。
    /// </summary>
    public static void Diag(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        try
        {
            var p = DiagPath();
            if (p.Length == 0) return;
            System.IO.File.AppendAllText(p,
                $"{System.DateTime.Now:HH:mm:ss}  {line}{System.Environment.NewLine}");
        }
        catch (System.Exception) { /* 写不了就算了 */ }
    }

    // ══════════════════════════════════════════════════════════════════

    /// <summary>我们自己 assets 目录的绝对路径（正斜杠）。</summary>
    public static string RootDir { get; } = ResolveRoot();

    private static readonly Dictionary<string, Texture2D?> Cache = new();
    private static readonly Dictionary<string, Font?> FontCache = new();
    private static readonly Dictionary<string, string> Chosen = new();   // 槽位 → 最终命中路径（诊断用）
    private static bool _reported;

    /// <summary>依次尝试一条候选链，返回第一张拿到的图。</summary>
    public static Texture2D? First(params string[] chain)
    {
        foreach (var path in chain)
        {
            var tex = One(path);
            if (tex != null) return tex;
        }
        return null;
    }

    /// <summary>同 <see cref="First"/>，但返回命中的那个路径（写日志用）。</summary>
    public static string? FirstHit(params string[] chain)
    {
        foreach (var path in chain)
            if (One(path) != null) return path;
        return null;
    }

    /// <summary>加载一个具体路径：`res://…` 走游戏 pck，其它走我们的 assets 目录。</summary>
    public static Texture2D? One(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (Cache.TryGetValue(path, out var cached)) return cached;

        Texture2D? tex = path.StartsWith("res://", StringComparison.Ordinal) ? LoadGame(path) : LoadOwn(path);
        Cache[path] = tex;
        return tex;
    }

    public static bool Has(string path) => One(path) != null;

    /// <summary>游戏自带的贴图/字体资源。</summary>
    private static Texture2D? LoadGame(string resPath)
    {
        try
        {
            // 先 Exists 再 Load：不存在的路径直接问会往日志里刷错误
            if (!ResourceLoader.Exists(resPath)) return null;
            return ResourceLoader.Load(resPath) as Texture2D;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"游戏资源 {resPath} 读不了（退回下一候选）: {e.Message}");
            return null;
        }
    }

    /// <summary>我们自己 assets 目录里的图。mod 没打 pck，所以只能按文件系统绝对路径读。</summary>
    private static Texture2D? LoadOwn(string rel)
    {
        try
        {
            if (string.IsNullOrEmpty(RootDir)) return null;
            var full = RootDir + "/" + rel.Replace('\\', '/');
            if (!File.Exists(full)) return null;
            var img = Image.LoadFromFile(full);
            if (img == null || img.GetWidth() <= 0) return null;
            return ImageTexture.CreateFromImage(img);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"素材 {rel} 加载失败: {e.Message}");
            return null;
        }
    }

    /// <summary>字体也走同一条候选链。</summary>
    public static Font? FirstFont(params string[] chain)
    {
        foreach (var path in chain)
        {
            if (string.IsNullOrEmpty(path)) continue;
            if (FontCache.TryGetValue(path, out var cached))
            {
                if (cached != null) return cached;
                continue;
            }

            Font? font = null;
            try
            {
                if (path.StartsWith("res://", StringComparison.Ordinal))
                {
                    if (ResourceLoader.Exists(path)) font = ResourceLoader.Load(path) as Font;
                }
                else if (!string.IsNullOrEmpty(RootDir))
                {
                    var full = RootDir + "/" + path;
                    if (File.Exists(full)) font = ResourceLoader.Load(full) as Font;
                }
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"字体 {path} 加载失败: {e.Message}");
            }

            FontCache[path] = font;
            if (font != null) return font;
        }
        return null;
    }

    /// <summary>
    /// 开桌时清点一遍，把「哪个槽位最终用了哪张图」写进日志。
    /// 美术想知道还缺什么、或者想确认自己的图有没有被用上，看这条就够了。
    /// </summary>
    public static void ReportOnce()
    {
        if (_reported) return;
        _reported = true;

        try
        {
            Chosen.Clear();
            var fromGame = new List<string>();
            var fromOwn = new List<string>();
            var none = new List<string>();

            foreach (var (name, chain) in AllSlots())
            {
                var hit = FirstHit(chain);
                if (hit == null) { none.Add(name); continue; }
                Chosen[name] = hit;
                if (hit.StartsWith("res://", StringComparison.Ordinal)) fromGame.Add($"{name}={hit}");
                else fromOwn.Add($"{name}={hit}");
            }

            Entry.Logger.Info($"斗地主素材：用自己的 {fromOwn.Count} 个，用游戏自带 {fromGame.Count} 个，"
                              + $"没有素材 {none.Count} 个。assets 目录：{RootDir}");

            if (fromOwn.Count > 0)
                Entry.Logger.Info("  自己的：" + string.Join("、", fromOwn));
            if (fromGame.Count > 0)
                Entry.Logger.Info("  借游戏的：" + string.Join("、", fromGame));
            if (none.Count > 0)
                Entry.Logger.Info("  走纯色兜底的：" + string.Join("、", none));
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主素材盘点失败（不影响玩）: {e.Message}");
        }
    }

    /// <summary>关桌时清缓存 —— 美术改完图重新开桌就能看到效果。</summary>
    public static void ClearCache()
    {
        Cache.Clear();
        FontCache.Clear();
        Chosen.Clear();
    }

    // ══════════════════════════════════════════════════════════════════

    private static string ResolveRoot()
    {
        var candidates = new List<string>();

        // 首选：用我们这个 dll 自己的位置 → 最稳（不管 exe 在哪）
        try
        {
            var loc = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(loc))
            {
                var dir = Path.GetDirectoryName(loc);
                if (!string.IsNullOrEmpty(dir)) candidates.Add(Path.Combine(dir, "assets"));
            }
        }
        catch (System.Exception) { }

        try
        {
            var exe = OS.GetExecutablePath();
            if (!string.IsNullOrEmpty(exe))
            {
                var dir = Path.GetDirectoryName(exe);
                if (!string.IsNullOrEmpty(dir))
                    candidates.Add(Path.Combine(dir, "mods", Entry.ModId, "assets"));
            }
        }
        catch (System.Exception) { }

        foreach (var c in candidates)
        {
            try { if (Directory.Exists(c)) return c.Replace('\\', '/'); } catch (System.Exception) { }
        }
        return (candidates.FirstOrDefault() ?? "").Replace('\\', '/');
    }
}

/// <summary>
/// 牌面长什么样。设置页里是个下拉。
///
/// ⚠️ 成员名故意写中文：BaseLib 的下拉在没本地化表时**直接显示成员名**
/// （同 <see cref="DouSpireUiTheme"/>），所以写什么玩家就看到什么。
/// </summary>
public enum DouSpireCardSkin
{
    /// <summary>经典斗地主 —— 原来的扑克牌面（点数 + 花色）。</summary>
    经典斗地主 = 0,

    /// <summary>尖塔牌面 —— 点子照旧，脸换成杀戮尖塔的卡（打击、防御、大爆炸…）。</summary>
    尖塔牌面 = 1,

    /// <summary>
    /// 烫金赌桌 —— 复古烫金 + 牛血红的牌面，**配德州扑克那张赌桌**（2026-10-09 加）。
    ///
    /// ⚠️ 这一套图是用户后补的，**可能一张都还没有**。所有取图都走
    /// <see cref="GildedPip"/> / <see cref="GildedSuit"/> / <see cref="GildedBody"/> /
    /// <see cref="GildedBack"/>，这些链**末尾一律指回经典那套** ——
    /// 所以缺料时它看起来就是「经典斗地主」，绝不会出现空洞或黑块。
    /// </summary>
    烫金赌桌 = 2,
}

/// <summary>
/// 德州扑克**牌室场景**（整屏背景）—— 设置页里是个下拉，牌桌右上角也有个按钮能当场轮换。
/// 用户 2026-10-09：「**可以多做几个切换场景**」。
///
/// ⚠️ 成员名故意写中文：BaseLib 的下拉在没本地化表时**直接显示成员名**
/// （同 <see cref="DouSpireCardSkin"/>），所以写什么玩家就看到什么。
///
/// ⚠️ **数字是存盘的**（玩家选过就存住了）。所以：
/// <list type="bullet">
///   <item>**只往末尾加新场景，别插队、别改老数字** —— 插队会让老玩家存的值指向另一个场景。</item>
///   <item>加了新场景**必须**在 <c>DouSpireConfig.InjectLabels()</c> 里加
///         <c>["DOUSPIRE-POKER_SCENE.&lt;成员名&gt;"]</c>，否则下拉里显示原始成员名（踩过）。</item>
///   <item>还要在 <see cref="DouSpireScenes.All"/> 里排上（按钮按那个顺序轮换）。</item>
/// </list>
/// </summary>
public enum DouSpireScene
{
    /// <summary>暖木俱乐部 —— 明亮暖金、大吊灯、酒柜镜面。最像"赌场"、最有牌局感。</summary>
    暖木俱乐部 = 0,

    /// <summary>月夜私人牌室 —— 大窗 + 深蓝夜色，室内暖灯。安静、高级、有故事感。</summary>
    月夜私人牌室 = 1,

    /// <summary>
    /// 二次元画风 —— **galgame 背景那种**（用户举例：《甜蜜女友3》，海豹社 / 原画 ピロ水）。
    ///
    /// ⚠️ **这一档和别的东西不是一套画风**：烫金赌桌、座位立绘都是暗黑奇幻，
    /// 而它是明亮柔和的动漫风 —— 属于用户主动要的"混搭"。做界面时别以为它是错的。
    /// </summary>
    二次元画风 = 2,

    /// <summary>地下赌窟 —— 砖拱、铁艺吊灯、烟雾、木箱酒桶。最"狠"最浓，但整体偏暗。</summary>
    地下赌窟 = 3,

    /// <summary>
    /// **雪夜温泉旅馆** —— 冬夜日式旅馆的牌室：敞开的障子门外是覆雪的木质回廊与夜色雪山，
    /// 门边一角冒着白汽的露天温泉；室内暖黄纸灯笼、水墨挂轴、覆雪矮松。
    ///
    /// <para>★ 2026-10-11 加 —— 用户："**你自己去魔搭生成想要的素材**"（授权自由创作）。
    /// 出图：朋友三千的魔搭 Image Studio（`D:/work/AI产出/魔搭自由创作/`），
    /// `qwen-image-2.1-4k` 出 4096 方图，居中裁 16:9 再缩到 1920×1080。
    /// **背景是 `StretchMode.Scale` 硬拉伸铺满的，所以交付图必须正好 16:9。**</para>
    /// </summary>
    雪夜温泉旅馆 = 4,

    /// <summary>
    /// **雨夜竹林茶室** —— 中式茶室：整排落地木格窗外是夜雨竹林、庭院石灯；
    /// 室内三盏纸灯笼、书法立轴、青瓷缸插枯荷。冷青调，比别档都更安静。
    ///
    /// <para>★ 2026-10-11 加，同 <see cref="雪夜温泉旅馆"/>。出图偏暗，
    /// 入库时做了 亮度 ×1.12 / 对比 ×1.06 的提亮（见 `process_for_mod.py`）。</para>
    /// </summary>
    雨夜竹林茶室 = 5,
}

/// <summary>
/// 场景的**固定顺序** —— 牌桌那个按钮就按这个顺序轮换。
/// ★ **只加在末尾，别插队**（顺序变了，玩家按按钮的习惯会跳）。
/// </summary>
public static class DouSpireScenes
{
    public static readonly DouSpireScene[] All =
    {
        DouSpireScene.暖木俱乐部,
        DouSpireScene.月夜私人牌室,
        DouSpireScene.二次元画风,
        DouSpireScene.地下赌窟,
        // ★ 2026-10-11 加（魔搭自由创作）：只往末尾追加，别插队。
        DouSpireScene.雪夜温泉旅馆,
        DouSpireScene.雨夜竹林茶室,
    };
}

/// <summary>
/// 德州扑克**牌桌风格** —— 设置页里是个下拉，牌桌右上角也有个「牌桌：…」按钮能当场轮换。
/// 用户 2026-10-09：「**你可以多想几个不同风格的牌桌**」。
///
/// ⚠️ **七档的功能刻线布局必须完全一样** —— 代码往桌上叠牌的位置是写死的（中央 5 个公共牌位、
/// 底池、三个座位、庄家位、下注牌位）。新画一张**可以换台面材质、配色和外圈装饰，
/// 但功能布局一个像素都不能动**；改了牌会叠歪。提示词里那段 `LAYOUT` 是**逐字共用**的，别改。
///
/// ⚠️ 成员名故意写中文：BaseLib 的下拉在没本地化表时**直接显示成员名**
/// （同 <see cref="DouSpireCardSkin"/>），所以写什么玩家就看到什么。
///
/// ⚠️ **数字是存盘的**（玩家选过就存住了）。所以：
/// <list type="bullet">
///   <item>**只往末尾加新风格，别插队、别改老数字** —— 插队会让老玩家存的值指向另一张桌。</item>
///   <item>加了新风格**必须**在 <c>DouSpireConfig.InjectLabels()</c> 里加
///         <c>["DOUSPIRE-POKER_TABLE_STYLE.&lt;成员名&gt;"]</c>，否则下拉里显示原始成员名（踩过）。</item>
///   <item>还要在 <see cref="DouSpireTableStyles.All"/> 里排上（按钮按那个顺序轮换）。</item>
///   <item>文件名主干在 <see cref="DouSpireAssets.TableKey"/>，**改名要两处一起改**。</item>
/// </list>
/// </summary>
public enum DouSpireTableStyle
{
    /// <summary>经典绿绒 —— 深青绿台呢 + 胡桃木 + 做旧黄铜。**✅ 已经出过图了**（`table_poker.png`），默认这一档。</summary>
    经典绿绒 = 0,

    /// <summary>暗黑黄铜 · 酒红皮革 —— 深胡桃木 + 酒红皮革台面 + 做旧黄铜。最正统的赌桌，配「暖木俱乐部」。</summary>
    暗黑黄铜 = 1,

    /// <summary>黑曜石 · 熔金纹 —— 近黑石面 + 熔金刻线。最"狠"最浓，配「地下赌窟」。</summary>
    黑曜石 = 2,

    /// <summary>冰蓝水晶 · 月白 —— 月白石面 + 拉丝银描边。冷调，配「月夜私人牌室」。</summary>
    冰蓝水晶 = 3,

    /// <summary>
    /// 二次元明亮 —— 薄荷绿台面 + 浅蜜色木 + 淡金线，**画成 galgame 那种动漫风**，配「二次元画风」场景。
    ///
    /// ⚠️ 和别的档不是一套画风（那几档是暗黑奇幻）—— 属于用户主动要的"混搭"，别以为画错了。
    /// </summary>
    二次元明亮 = 4,

    /// <summary>东方漆器 · 朱金 —— 朱红漆面 + 黑漆木 + 金箔。最"斗地主"的一档。**✅ 2026-10-11 补上图了**（`table_poker_lacquer.png`）。</summary>
    东方漆器 = 5,

    /// <summary>蒸汽朋克 · 黄铜齿轮 —— 油布台面 + 铆接黄铜板 + 铁艺齿轮。机械感最强。**⚠️ 还没出图，缺料时回落经典绿绒。**</summary>
    蒸汽朋克 = 6,
}

/// <summary>
/// 牌桌风格的**固定顺序** —— 牌桌那个按钮就按这个顺序轮换。
/// ★ **只加在末尾，别插队**（顺序变了，玩家按按钮的习惯会跳）。
/// </summary>
public static class DouSpireTableStyles
{
    public static readonly DouSpireTableStyle[] All =
    {
        DouSpireTableStyle.经典绿绒,
        DouSpireTableStyle.暗黑黄铜,
        DouSpireTableStyle.黑曜石,
        DouSpireTableStyle.冰蓝水晶,
        DouSpireTableStyle.二次元明亮,
        DouSpireTableStyle.东方漆器,
        DouSpireTableStyle.蒸汽朋克,
    };
}
