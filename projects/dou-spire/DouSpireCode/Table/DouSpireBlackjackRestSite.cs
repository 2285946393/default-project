using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace DouSpire.Table;

/// <summary>
/// 篝火（休息处）里的第三个选项：「**21 点**」（玩法三）。
///
/// 和 <see cref="DouSpireRestSiteOption"/>（斗地主）、<see cref="DouSpirePokerRestSiteOption"/>（德州）
/// 是**三个一对**：同一个单例、同一个方法里按固定顺序一起 Add 进去 —— 见
/// <see cref="DouSpireRestSiteSingleton.TryModifyRestSiteOptions"/> 里那段"为什么不能拆成三个单例"的注释。
/// **联机同步的是 uint 下标，顺序必须是写死的**（斗地主 0 / 德州 1 / 21 点 2）。
///
/// ★ **为什么这一档必须走篝火**：血量模式要有"血"可赌，而血量只在 run 里存在 ——
/// 主菜单那版（<see cref="DouSpireBlackjackEntry"/>）是筹码局，没有 run 也就没有血。
/// 所以「拿命赌」只有从篝火开桌这一条路。
///
/// 图标走 <see cref="DouSpireRestIcon"/> 挂的那同一个 pck
/// （包里的 <c>option_dou_spire_blackjack.png</c>），不重复挂包。
/// ⚠️ 那张图**还没画** —— 现阶段会走 <see cref="ResolveIconPath"/> 的降级分支，
/// 用本体那张真有的图顶着（选项照常能点，只是图标朴素）。
/// </summary>
public sealed class DouSpireBlackjackRestSiteOption : ModRestSiteOptionTemplate
{
    /// <summary>选项稳定 id。本地化键、图标文件名都由它拼出来，**改了要同步改注入的键**。</summary>
    public const string StableId = "DOU_SPIRE_BLACKJACK";

    public override string OptionId => StableId;

    /// <summary>我们**想**用的图标路径（还没画，现阶段大概率验不过）。</summary>
    public const string OwnIconResPath = "res://images/ui/rest_site/option_dou_spire_blackjack.png";

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

        Entry.Logger.Info($"21 点：自己在 res:// 里的那张篝火图标还没画（{OwnIconResPath}）"
                          + "—— 先用本体图标顶着，不影响开桌。");
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

    public DouSpireBlackjackRestSiteOption(Player owner)
        : base(owner)
    {
    }

    /// <summary>
    /// 点「21 点」→ 开桌。
    ///
    /// **返回 <c>false</c> 是故意的**：这个返回值表示「这次选择算不算把休息处用掉了」。
    /// 返回 true 篝火就结束、进下一层；返回 false 篝火留着 —— 于是可以**反复开桌**，
    /// 和斗地主、德州那两个选项一样对应「不限次数」。
    ///
    /// **拦人规则**：金币模式下没钱不开（用户 2026-10-08：「如果我是零元就拒绝我」）；
    /// 血量模式下**血只剩 1 点就不开**（再输就该死了 —— 三条保命线里的最后一条）。
    /// 读不到数值时**不拦**（宁可多开一桌，也别因为读不到就把人挡在门外）。
    ///
    /// ★ **联机直接拒绝**（2026-10-11 上架前加的一道闸）：21 点这版是**纯本地桌**，
    /// 没有像德州那样的联机分流层（<c>DouSpirePokerNet</c>）。而篝火选项的**选择是按
    /// uint 下标全队广播**的 —— 联机时一个人点了「21 点」，每个客户端都会各自开一张
    /// **自己的**本地桌、各发各的牌、各结算各的金币，直接跑岔。所以联机 run 里
    /// **统一拒绝**：所有人都会走到这一句、都返回 false，篝火原样留着，谁也不动。
    /// （等哪天 21 点也接上联机分流层，把这一段删掉即可。）
    /// </summary>
    public override Task<bool> OnSelect()
    {
        try
        {
            if (DouSpireNet.InMultiplayerRun())
            {
                Entry.Logger.Info("21 点：这是联机 run —— 21 点暂时只有单人版，拒绝开桌。");
                Entry.Toast("21 点暂时只支持单人 —— 联机请玩斗地主或德州扑克。", "21 点");
                return Task.FromResult(false);
            }

            var hp = DouSpireBlackjackTable.MyHpNow();
            if (DouSpireConfig.BlackjackUsesHp())
            {
                if (hp is <= 1)
                {
                    Entry.Logger.Info($"21 点：血量 {hp}，拒绝开桌（只剩一口气就别赌命了）。");
                    Entry.Toast("你就剩一口气了 —— 先回点血再来赌命。", "21 点");
                    return Task.FromResult(false);
                }
            }
            else
            {
                var gold = DouSpireBlackjackTable.MyGoldNow();
                if (gold is <= 0)
                {
                    Entry.Logger.Info($"21 点：身上 {gold} 金币，拒绝开桌（没钱不赌）。");
                    Entry.Toast("你身上一个金币都没有 —— 先去挣点钱再来 21 点。", "21 点");
                    return Task.FromResult(false);
                }
            }

            DouSpireBlackjackTable.OpenAtRestSite(Owner, Entry.Logger);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"21 点：篝火开桌失败: {e.Message}");
            Entry.Toast("开桌失败，看日志（不影响这局游戏）。");
        }
        return Task.FromResult(false);
    }
}
