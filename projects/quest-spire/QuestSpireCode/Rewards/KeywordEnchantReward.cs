using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace QuestSpire.Relics;

/// <summary>
/// 把「从卡组里挑一张牌挂词条（保留 / 固有）」做成**奖励界面里的一张奖励**。
///
/// ⚠️ 为什么必须这么做（2026-09-25 日志实锤，别再改回去）：
/// 以前是在任务完成的瞬间**自己**调 `CardSelectCmd.FromDeckGeneric` 弹选牌界面。
/// 玩家实测「界面弹出来了但选不中，只能按前进」。日志原文：
///     [ERROR] [QuestSpire] 选牌附魔失败（固有）: TaskCanceledException
///       at NCardGridSelectionScreen._ExitTree()
///     （紧接着就是 TreasureRoom.EnterInternal —— 玩家正好在**进宝箱房**）
/// 也就是说房间切换把我们的选牌界面拆了、选牌任务被取消，玩家看到的那个界面根本点不动。
///
/// 正确姿势：**不自己弹界面**，把附魔做成奖励界面上的一张奖励，等玩家在**稳定时机**点它，
/// 才去开选牌界面。这条路和 BaseLib 的 `CardUpgradeReward` / `CardTransformReward` 完全一样，
/// 而那两件在我们这儿一直是好的。
///
/// 兜底：万一选牌还是没完成（玩家按了取消、或又撞上房间切换），就退回「随机一张」——
/// 宁可不够花哨，也绝不让奖励凭空消失。
/// </summary>
public sealed class KeywordEnchantReward : CustomReward
{
    // ⚠️ 2026-10-01：**给自己一个独立的奖励类型**，不再借 `CardUpgradeReward.CardUpgrade`。
    //
    // 为什么要改（真 bug，不是洁癖）：
    //   `CombatRoom.ToSerializable()` 会把本场还没领的 `ExtraRewards` 一起写进存档。
    //   而我们塞进搜刮单子的「附魔」奖励，借的是 CardUpgrade 这个类型 —— 于是玩家**打赢后
    //   没领就走/退出**，读档回来时 `Reward.FromSerializable` 按 CardUpgrade 把它还原成
    //   BaseLib 的「升级一张牌」，**附魔奖励被换成了别的奖励**。
    //
    // BaseLib 的注册机制（反编译确认）：只有**自己声明了 `static RewardType` 字段**的 CustomReward
    //   才会被自动注册（`CustomRewardPatches` 按字段扫描 → 建实例 → 读 `ToSerializable().RewardType`）。
    //   我们原来没有这个字段 → 压根没注册 → 读档就走了 CardUpgrade 那条。
    //   现在照着 BaseLib 自己的 `CardUpgradeReward` 补齐：自带类型 + 自带反序列化。
    [CustomEnum(null)]
    public static RewardType QuestEnchant;

    private readonly CardKeyword _keyword;
    private readonly string _displayName;

    public KeywordEnchantReward(Player player, CardKeyword keyword, string displayName) : base(player)
    {
        _keyword = keyword;
        _displayName = displayName;
    }

    protected override RewardType RewardType => QuestEnchant;

    // 和 BaseLib 的 CardUpgradeReward 同一个序号，多人局里能正常同步（只是排序用的键）。
    public override int RewardsSetIndex => 8;

    public override bool IsPopulated => true;

    // 图标借用游戏自带的「升级一张牌」图标（我们没为它专门画）。
    //
    // ⚠️ 2026-09-26 深夜踩的坑：这里直接写 `"ui/reward_screen/reward_icon_card_upgrade.png"` 会报
    //    `No loader found for resource` + `Error loading resource` —— 因为 BaseLib 的
    //    `CardUpgradeReward` 是把这个相对路径**再过一层 `ImageHelperExtensions.GetModImagePath`**
    //    才拿去加载的。这里必须照它一样处理，否则奖励框里的图标是坏的（玩家看到的"空框"就是这个）。
    protected override string IconPath =>
        ImageHelperExtensions.GetModImagePath("ui/reward_screen/reward_icon_card_upgrade.png");

    public override LocString Description
    {
        get
        {
            var loc = RewardLoc.Lookup("QUEST_SPIRE_ENCHANT_REWARD.description", "QUEST_SPIRE_ENCHANT_REWARD.generic");
            loc.Add("keyword", _displayName);
            return loc;
        }
    }

    // 读档还原：把词条存在 GoldAmount 里（照 BaseLib CardUpgradeReward 存 Amount 的做法）。
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSave;

    public override SerializableReward ToSerializable() => new()
    {
        RewardType = QuestEnchant,
        GoldAmount = (int)_keyword,
    };

    private static KeywordEnchantReward CreateFromSave(SerializableReward save, Player player)
    {
        var keyword = (CardKeyword)save.GoldAmount;
        return new KeywordEnchantReward(player, keyword, DisplayNameOf(keyword));
    }

    /// <summary>词条的中文名（读档还原时用来重建奖励文案；活着的实例直接用构造时传进来的名字）。</summary>
    private static string DisplayNameOf(CardKeyword keyword) => keyword switch
    {
        CardKeyword.Innate => "固有",
        CardKeyword.Retain => "保留",
        _ => keyword.ToString(),
    };

    public override void Populate() { }

    public override void MarkContentAsSeen() { }

    protected override async Task<bool> OnSelect()
    {
        var owner = Player;
        if (owner == null) return false;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        try
        {
            // 已经挂过这个词条的牌就不让再选，免得同一个词条叠两次。
            var picked = await CardSelectCmd.FromDeckGeneric(owner, prefs, c => !HasKeyword(c, _keyword));
            var card = picked?.FirstOrDefault();
            if (card != null)
            {
                card.AddKeyword(_keyword);
                Entry.Logger.Info($"附魔：给「{card}」挂上「{_displayName}」");
                return true;
            }
            Entry.Logger.Info($"附魔「{_displayName}」：玩家没选牌，退回随机一张。");
        }
        catch (System.Exception e)
        {
            // 选牌界面被拆掉了（房间切换 / 被取消）。退回随机一张，奖励绝不打水漂。
            Entry.Logger.Warn($"选牌附魔「{_displayName}」没能完成（{e.GetType().Name}），退回随机一张。");
        }

        return EnchantRandom(owner);
    }

    /// <summary>兜底：随便挑一张牌挂上。奖励宁可不够花哨，也绝不凭空消失。</summary>
    private bool EnchantRandom(Player owner)
    {
        try
        {
            var deck = owner.Deck?.Cards;
            if (deck == null || deck.Count == 0) return false;
            var random = deck[Random.Shared.Next(deck.Count)];
            random.AddKeyword(_keyword);
            Entry.Logger.Info($"附魔（随机兜底）：给「{random}」挂上「{_displayName}」");
            return true;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"随机附魔也失败了: {e.Message}");
            return false;
        }
    }

    private static bool HasKeyword(CardModel card, CardKeyword keyword)
    {
        try { return card.Keywords.Contains(keyword); }
        catch (System.Exception) { return false; }
    }
}

/// <summary>奖励文案：先从我们自己的本地化表取，取不到就退回游戏自带的表。</summary>
internal static class RewardLoc
{
    // 我们的 pck 里就是这张表（res://QuestSpire/localization/<语言>/relics.json）。
    // 万一哪天表名变了也不至于显示成一串内部 key —— 会顺着候选表往下找。
    private static readonly string[] CandidateTables = ["relics", "gameplay_ui"];

    public static LocString Lookup(string descriptionKey, string tableFallbackKey)
    {
        foreach (var table in CandidateTables)
            if (LocString.Exists(table, descriptionKey))
                return new LocString(table, descriptionKey);

        foreach (var table in CandidateTables)
            if (LocString.Exists(table, tableFallbackKey))
                return new LocString(table, tableFallbackKey);

        // 实在没有：显示词条本名（游戏自带的词条名一定是本地化好的），好过一串内部 key。
        return CardKeywordExtensions.GetTitle(CardKeyword.Retain);
    }
}
