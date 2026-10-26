using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace DouSpire.Table;

/// <summary>
/// 篝火（休息处）里的第二个选项：「**德州扑克**」（玩法二）。
///
/// 和 <see cref="DouSpireRestSiteOption"/>（斗地主）是**一对**：同一个单例、同一个方法里
/// 按固定顺序一起 Add 进去 —— 见 <see cref="DouSpireRestSiteSingleton.TryModifyRestSiteOptions"/>
/// 里那段"为什么不能拆成两个单例"的注释。**联机同步的是 uint 下标**，顺序必须是写死的。
///
/// 图标走 <see cref="DouSpireRestIcon"/> 挂的那同一个 pck
/// （包里的 <c>option_dou_spire_poker.png</c>），不重复挂包。
/// </summary>
public sealed class DouSpirePokerRestSiteOption : ModRestSiteOptionTemplate
{
    /// <summary>选项稳定 id。本地化键、图标文件名都由它拼出来，**改了要同步改注入的键**。</summary>
    public const string StableId = "DOU_SPIRE_POKER";

    public override string OptionId => StableId;

    /// <summary>
    /// 我们**想**用的图标路径。⚠️ **这张图在 res:// 里是否存在，取决于
    /// <c>assets/DouSpire.icon.pck</c> 挂上没有** —— 所以对外一律用
    /// <see cref="ResolveIconPath"/>，它会先验再给（验不过退回本体那张真有的图）。
    /// 直接把这个常量丢给游戏，就是 2026-10-08 那个"图标错 + 名字错 + 点进去卡死"的雷。
    /// </summary>
    public const string OwnIconResPath = "res://images/ui/rest_site/option_dou_spire_poker.png";

    /// <summary>验不过时退回的本体图标（本体 <c>SlayTheSpire2.pck</c> 里真实存在）。</summary>
    private const string FallbackIconResPath = "res://images/ui/rest_site/option_clone.png";

    private static string? _resolvedIconPath;

    /// <summary>
    /// **只返回一个真的能加载的 <c>res://</c> 路径。**
    /// 顺序：① 确保图标包挂上（<see cref="DouSpireRestIcon"/>）→ ② 问
    /// <c>ResourceLoader.Exists</c> —— 挂上了就**真的存在**，用它 → ③ 验不过退回本体真有的图。
    /// 宁可图标朴素一点，也绝不能让选项坏掉。
    /// </summary>
    public static string ResolveIconPath()
    {
        if (_resolvedIconPath is not null) return _resolvedIconPath;

        if (DouSpireRestIcon.Exists(OwnIconResPath))
        {
            _resolvedIconPath = OwnIconResPath;
            return _resolvedIconPath;
        }

        Entry.Logger.Warn($"德州：自己在 res:// 里的那张篝火图标不存在（{OwnIconResPath}）"
                          + "—— 改用本体图标顶一下。");
        _resolvedIconPath = FallbackIconResPath;
        return _resolvedIconPath;
    }

    /// <summary>公开给调用方判断"是不是我们自己的那张图"（用来打"图标包没挂上"的自检日志）。</summary>
    public static bool IsOwnIcon(string path) => path == OwnIconResPath;

    public override RestSiteOptionAssetProfile AssetProfile => new(ResolveIconPath());

    public override string? CustomIconPath => ResolveIconPath();

    public override LocString? CustomTitle => new LocString("rest_site_ui", TitleKey);

    // Description 不用覆写：基类就是按 OPTION_<OptionId>.description 取，键正好对得上。
    public const string TitleKey = "OPTION_" + StableId + ".name";
    public const string DescriptionKey = "OPTION_" + StableId + ".description";

    public DouSpirePokerRestSiteOption(Player owner)
        : base(owner)
    {
    }

    /// <summary>
    /// 点「德州扑克」→ 开桌（金币赌注模式）。
    ///
    /// **返回 <c>false</c> 是故意的**：这个返回值表示「这次选择算不算把休息处用掉了」。
    /// 返回 true 篝火就结束、进下一层；返回 false 篝火留着 —— 于是可以**反复开桌**，
    /// 和斗地主那个选项一样对应「不限次数」。
    ///
    /// **身上没钱就不开**（用户 2026-10-08：「如果我是零元就拒绝我」「不能让我白手起家」）：
    /// 读不到金币时**不拦**（宁可多开一桌，也别因为读不到就把人挡在门外）。
    /// </summary>
    public override Task<bool> OnSelect()
    {
        try
        {
            var gold = DouSpirePokerTable.MyGoldNow();
            if (gold is <= 0)
            {
                Entry.Logger.Info($"德州：身上 {gold} 金币，拒绝开桌（没钱不赌）。");
                Entry.Toast("你身上一个金币都没有 —— 先去挣点钱再来德州。", "德州扑克");
                return Task.FromResult(false);
            }

            // 走联机分流层：单人时它直接把本地桌开起来；多人 run 时它负责
            // 把全队拉到同一张桌（走新的 poker.* 频道，不动已上架的 DouSpireNet）。
            DouSpirePokerNet.OpenAtRestSite(Owner, Entry.Logger);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"德州：篝火开桌失败: {e.Message}");
            Entry.Toast("开桌失败，看日志（不影响这局游戏）。");
        }
        return Task.FromResult(false);
    }
}
