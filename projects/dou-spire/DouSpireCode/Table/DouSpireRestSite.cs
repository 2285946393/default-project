using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace DouSpire.Table;

/// <summary>
/// 篝火（休息处）里的「斗地主」选项 —— 让这桌牌**长进游戏本体**，而不是只能从主菜单/F9 进。
///
/// 用户 2026-10-07 深夜的原话：
/// 「在游戏中的篝火处选项弹出可以斗地主。（单人和ai）… 用身上的游戏金币数量作为赌注。
///   可以通过篝火处的斗地主赚钱。不限次数。不然只能通过这个斗地主游戏玩到斗地主太纯了」
///
/// **怎么加进来的**（照着 <c>RestSiteExpansion</c> / <c>Miyabists2</c> 两个现成 mod 抄的）：
/// 官方给的口子是 <see cref="MegaCrit.Sts2.Core.Models.AbstractModel.TryModifyRestSiteOptions"/>，
/// 由 <see cref="DouSpireRestSiteSingleton"/> 覆写并把本选项 <c>Add</c> 进去。
/// 选项本体继承 RitsuLib 的 <see cref="ModRestSiteOptionTemplate"/> ——
/// 它把「标题/图标」这两个**非 virtual** 的属性用 Harmony 补丁改成可覆盖的，否则我们改不了标题。
/// </summary>
public sealed class DouSpireRestSiteOption : ModRestSiteOptionTemplate
{
    /// <summary>选项稳定 id。本地化键、图标文件名都由它拼出来，**改了要同步改注入的键**。</summary>
    public const string StableId = "DOU_SPIRE";

    public override string OptionId => StableId;

    /// <summary>
    /// 我们**想**用的图标路径。
    ///
    /// ⚠️⚠️ **这张图在 res:// 里其实并不存在** —— 本 mod 是 <c>has_pck: false</c>，没有 pck 就
    /// 没有任何东西被挂进 <c>res://</c>。原来靠 <c>PreloadManager.Cache.SetAsset</c> 往运行时
    /// 缓存里塞一张，平时看着是好的，**但只要有一次异步预加载走到这个路径，就会炸**（实测）。
    /// 所以对外一律用 <see cref="ResolveIconPath"/>，它会先验再给。
    /// </summary>
    public const string OwnIconResPath = "res://images/ui/rest_site/option_dou_spire.png";

    /// <summary>验不过时退回的本体图标（本体 <c>SlayTheSpire2.pck</c> 里真实存在）。</summary>
    private const string FallbackIconResPath = "res://images/ui/rest_site/option_clone.png";

    // ════════════════ 图标：自己的那张，来自一个**我们自己挂载**的小 pck ════════════════
    //
    // 挂载本身已经抽到 DouSpireRestIcon —— 斗地主和德州扑克**共用同一个包**
    // （assets/DouSpire.icon.pck 里有 option_dou_spire.png + option_dou_spire_poker.png），
    // 挂两次同一个包没有意义，所以收口到一个地方。
    // 这里只负责"先验再给"这一步 —— 为什么必须验，见下面 ResolveIconPath 的长注释。

    /// <summary>
    /// **篝火选项图标：只返回一个真的能加载的 <c>res://</c> 路径。**
    ///
    /// ── 为什么必须验（2026-10-08 用户报「斗地主变成别的图例和名字了，进去也是卡死」）──
    /// 用户装了 <c>RestSiteExpansion</c>，它**会重建休息处选项**（把每个选项重新实例化成
    /// <c>NRestSiteButton</c>），于是游戏走了一次异步资源加载去看这张图，链条是：
    /// <code>
    /// 走异步加载 → 文件不存在 → ERROR: Error loading resource: '…/option_dou_spire.png'
    ///   → AssetCache 把这条路径**永久标记成"加载失败"**
    ///     (AssetLoadException: Asset previously failed to load: … The game installation may be corrupted.)
    ///   → RestSiteOption.get_Icon 抛异常 → NRestSiteButton.Reload()/_Ready 半途中断
    ///     （那个按钮就此处于坏状态 —— 点它就卡死）
    ///   → RitsuLib 打印 "Missing resource path for DouSpireRestSiteOption.CustomIconPath
    ///     … Falling back to the base asset" 并把**标题和图标都换成本体的「挖掘 / Dig」**
    /// </code>
    /// 一个假路径连环引发三个症状：图标错、名字错、点进去卡死。
    ///
    /// ⇒ 现在的顺序：① 先试着挂图标包（见 <see cref="DouSpireRestIcon"/>）；
    ///   ② 再问 <c>ResourceLoader.Exists</c> —— 挂上了就**真的存在**，用它；
    ///   ③ 验不过退回一张本体真有的图。宁可图标朴素一点，也绝不能让选项坏掉。
    /// </summary>
    public static string ResolveIconPath()
    {
        if (_resolvedIconPath is not null) return _resolvedIconPath;

        if (DouSpireRestIcon.Exists(OwnIconResPath))
        {
            _resolvedIconPath = OwnIconResPath;
            return _resolvedIconPath;
        }

        Entry.Logger.Warn($"斗地主：自己在 res:// 里的那张篝火图标不存在（{OwnIconResPath}）"
                          + "—— 改用本体图标顶一下。");
        _resolvedIconPath = FallbackIconResPath;
        return _resolvedIconPath;
    }

    private static string? _resolvedIconPath;

    /// <summary>公开给 <see cref="ResolveIconPath"/> 的调用方判断"是不是我们自己的那张图"。</summary>
    public static bool IsOwnIcon(string path) => path == OwnIconResPath;

    public override RestSiteOptionAssetProfile AssetProfile => new(ResolveIconPath());

    public override string? CustomIconPath => ResolveIconPath();

    public override LocString? CustomTitle => new LocString("rest_site_ui", TitleKey);

    // Description 不用覆写：基类就是按 OPTION_<OptionId>.description 取，键正好对得上。
    public const string TitleKey = "OPTION_" + StableId + ".name";
    public const string DescriptionKey = "OPTION_" + StableId + ".description";

    public DouSpireRestSiteOption(Player owner)
        : base(owner)
    {
    }

    /// <summary>
    /// 点「斗地主」→ 开桌（金币赌注模式）。
    ///
    /// **返回 <c>false</c> 是故意的**：这个返回值表示「这次选择算不算把休息处用掉了」。
    /// 返回 true 篝火就结束、进下一层；返回 false 篝火留着 —— 于是可以**反复开桌**，
    /// 对应「不限次数」。官方自己也是这么干的（星见雅那个抽卡选项就返回 false）。
    ///
    /// **身上没钱就不开**（用户 2026-10-08：「如果我是零元就拒绝我斗地主」「不能让我白手起家」）：
    /// 读不到金币时**不拦**（宁可多开一桌，也别因为读不到就把人挡在门外）。
    /// </summary>
    public override Task<bool> OnSelect()
    {
        try
        {
            var gold = DouSpireTable.MyGoldNow();
            if (gold is <= 0)
            {
                Entry.Logger.Info($"斗地主：身上 {gold} 金币，拒绝开桌（没钱不赌）。");
                Entry.Toast("你身上一个金币都没有 —— 先去挣点钱再来斗地主。", "斗地主");
                return Task.FromResult(false);
            }

            // 单人 → 直接开桌；多人（篝火 + 联机 run）→ 把全队拉到同一张桌上（见 DouSpireNet）
            DouSpireNet.OpenAtRestSite(Owner, Entry.Logger);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：篝火开桌失败: {e.Message}");
            Entry.Toast("开桌失败，看日志（不影响这局游戏）。");
        }
        return Task.FromResult(false);
    }
}

/// <summary>
/// 把**两个**玩法入口（「斗地主」+「德州扑克」）挂到每个休息处的单例模型。
///
/// <see cref="HookedSingletonModel"/> 是 RitsuLib 提供的「会订阅钩子的单例模型」基类：
/// 传 <c>HookType.Run</c> 就订阅了 run 级钩子，于是 <c>TryModifyRestSiteOptions</c> 会被调用。
/// 这比自己写 relic / Harmony 补丁干净得多 —— 也正是别的 mod 的做法。
///
/// ⚠️ **两个选项故意共用这一个单例**（不是一人一个）—— 为什么，见
/// <see cref="TryModifyRestSiteOptions"/> 的注释（联机按下标同步，顺序必须写死）。
///
/// 注册在 <see cref="Entry.Initialize"/> 里：<c>CreateContentPack(ModId).Singleton&lt;…&gt;().Apply()</c>。
/// </summary>
public sealed class DouSpireRestSiteSingleton : HookedSingletonModel
{
    public DouSpireRestSiteSingleton()
        : base(HookType.Run)
    {
    }

    /// <summary>
    /// 官方扩展点：休息处生成选项列表时，让每个模型有机会往里加东西
    /// （原版 Shovel 遗物就是靠这个加「挖掘」）。这里无条件加我们的**两个**玩法入口。
    ///
    /// ⚠️⚠️ **两个选项必须在同一个方法里、按写死的顺序加**（2026-10-09 晚，加德州时定的）。
    ///
    /// 原因是联机同步的**不是选项 id，是 uint 下标**：
    /// <c>RestSiteSynchronizer.ChooseLocalOption(int index)</c> 只把下标发出去，
    /// 每个玩家的选项表是**各自本地生成**的，接收端**唯一的校验就是"越界没有"**。
    /// → 两端的表只要差一个元素，同一个下标就指向**不同的选项**，而且**不报错，只是静默错**。
    ///
    /// 如果拆成两个单例（斗地主一个、德州一个），谁先被
    /// <c>Hook.ModifyRestSiteOptions</c> 调用取决于
    /// <c>runState.IterateHookListeners(null)</c> 的遍历顺序 —— **那个顺序不由我们控制**，
    /// 换个游戏版本、换个 mod 集合就可能变。写在一个方法里 → 顺序由代码写死，天然确定性。
    /// </summary>
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player is null || options is null) return false;

        EnsureText();
        EnsureIcon();

        options.Add(new DouSpireRestSiteOption(player));              // 下标 0：斗地主
        options.Add(new DouSpirePokerRestSiteOption(player));         // 下标 1：德州扑克
        options.Add(new DouSpireBlackjackRestSiteOption(player));     // 下标 2：21 点

        Entry.Logger.Info($"斗地主：篝火里挂上了「斗地主」+「德州扑克」+「21 点」三个选项"
                          + $"（玩家 {player.NetId}，身上金币 {player.Gold}）。");
        return true;
    }

    // ══════════════════════════ 标题 / 说明 ══════════════════════════

    /// <summary>
    /// 游戏标题走的是本地化表 <c>rest_site_ui</c> 的 <c>OPTION_&lt;id&gt;.name</c>。
    /// 本 mod 没有 pck、也没有本地化文件，所以**在运行时往那张表里合并两条键值**
    /// （<c>LocManager.GetTable</c> / <c>LocTable.MergeWith</c> 都是公开 API）。
    /// 不注入的话，取标题时会抛 <c>LocException: Key=… not found</c>。
    /// </summary>
    private static void EnsureText()
    {
        if (_textReady) return;
        try
        {
            var table = LocManager.Instance?.GetTable("rest_site_ui");
            if (table is null) return;

            table.MergeWith(new Dictionary<string, string>
            {
                [DouSpireRestSiteOption.TitleKey] = "斗地主",
                [DouSpireRestSiteOption.DescriptionKey] =
                    "开一桌斗地主，拿身上的金币下注：赢了进账，输了掏钱。想玩几把玩几把，不占这处休息。",
                [DouSpirePokerRestSiteOption.TitleKey] = "德州扑克",
                [DouSpirePokerRestSiteOption.DescriptionKey] =
                    "开一桌德州扑克（玩法二），同样拿身上的金币下注。没玩过也不要紧 —— "
                    + "桌上有牌型对照表和实时提示。想玩几把玩几把，不占这处休息。",
                [DouSpireBlackjackRestSiteOption.TitleKey] = "21 点",
                [DouSpireBlackjackRestSiteOption.DescriptionKey] =
                    "开一桌 21 点（玩法三），和庄家比谁更接近 21 点。默认押金币；"
                    + "在设置里切成「血量」就是拿命赌 —— 爆牌掉血、黑杰克回血，"
                    + "但单次最多掉当前血量的四分之一，永远给你留一口气。不占这处休息。",
            });
            _textReady = true;
            Entry.Logger.Info("斗地主：篝火选项的中文标题/说明已注入 rest_site_ui（斗地主 + 德州扑克）。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：注入篝火选项文案失败（选项会显示成空标题）: {e.Message}");
        }
    }

    // ══════════════════════════ 图标 ══════════════════════════

    /// <summary>
    /// 图标：<c>RestSiteOption.Icon</c> 只会去 <c>res://</c> 找。
    ///
    /// ⚠️ **现在什么都不用做了** —— <see cref="DouSpireRestSiteOption.ResolveIconPath"/>
    /// 会先把 <c>assets/DouSpire.icon.pck</c> 挂进虚拟文件系统，那张图就成了 res:// 里
    /// **真实存在的文件**，游戏自己会从 pck 里把贴图读出来。
    ///
    /// 这里保留成一个"自检点"：如果走到这一步发现用的还是本体图，就说明图标包没挂上
    /// （没这个文件 / 挂载失败），只在日志里说一句 —— 选项本身照常能点。
    ///
    /// ⚠️ 以前这里是**无条件**往那个假路径里塞贴图（<c>PreloadManager.Cache.SetAsset</c>）。
    /// 看着能用，实际是定时炸弹：只要有一次异步预加载走到那个路径，游戏就会真去读文件 →
    /// 失败 → 把路径永久标记成"加载失败" → 选项的图标、标题全被 RitsuLib 退回成本体的
    /// 「挖掘 / Dig」，而且按钮半途中断、点进去卡死。详见
    /// <see cref="DouSpireRestSiteOption.ResolveIconPath"/>。
    /// </summary>
    private static void EnsureIcon()
    {
        try
        {
            var dou = DouSpireRestSiteOption.ResolveIconPath();
            if (!DouSpireRestSiteOption.IsOwnIcon(dou))
                WarnOnce("篝火图标用的是本体那张（自己的图标包没挂上）。");

            var poker = DouSpirePokerRestSiteOption.ResolveIconPath();
            if (!DouSpirePokerRestSiteOption.IsOwnIcon(poker))
                WarnOnce("篝火图标用的是本体那张（自己的图标包没挂上）。");
        }
        catch (Exception e)
        {
            WarnOnce($"篝火选项图标检查失败: {e.Message}");
        }
    }

    private static void WarnOnce(string msg)
    {
        if (_warned) return;
        _warned = true;
        Entry.Logger.Warn("斗地主：" + msg);
    }

    private static bool _textReady;
    private static bool _warned;
}
