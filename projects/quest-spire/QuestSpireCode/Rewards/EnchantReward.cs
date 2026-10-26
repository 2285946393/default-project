using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace QuestSpire.Relics;

/// <summary>
/// 「给牌组里一张牌附魔」的奖励 —— 复用本体自带的**附魔系统**（`EnchantmentModel`，
/// 本体有 22 种：Sharp / Swift / Adroit / Nimble / Corrupted …）。
///
/// 和 <see cref="KeywordEnchantReward"/> 同一条路：**不自己弹选牌界面**，做成奖励界面上的一张奖励，
/// 玩家点了才开选牌界面（那是稳定时机）。附魔用 `CardCmd.Enchant(enchantModel, card, amount)`。
/// </summary>
public sealed class EnchantReward : CustomReward
{
    [CustomEnum(null)]
    public static RewardType QuestEnchantCard;

    private readonly EnchantmentModel _enchant;

    public EnchantReward(Player player, EnchantmentModel enchant) : base(player)
    {
        _enchant = enchant;
    }

    protected override RewardType RewardType => QuestEnchantCard;
    public override int RewardsSetIndex => 8;
    public override bool IsPopulated => true;

    protected override string IconPath =>
        ImageHelperExtensions.GetModImagePath("ui/reward_screen/reward_icon_card_upgrade.png");

    public override LocString Description => _enchant.Title;

    // ⚠️⚠️ **必须给一个真的反序列化方法，不能返回 null！**
    // BaseLib 在启动时（`GenEnumValues.FindAndGenerate` → `ModelDb.Init` → `NGame.GameStartup`）
    // 会自动实例化所有"带 static RewardType 字段的 CustomReward"并调 `Initialize()`，
    // 而 `Initialize()` 第一行就是 `DeserializeMethod.Target` —— 返回 null 会**空引用、启动直接崩**。
    // （2026-10-01 就是这里让玩家进不去游戏。）
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSave;

    public override SerializableReward ToSerializable() => new()
    {
        RewardType = QuestEnchantCard,
        // ⚠️ 启动时 BaseLib 会在一个"空壳实例"上调这里（`_enchant` 还是 null）→ 必须防空。
        PredeterminedModelId = _enchant?.Id ?? ModelId.none,
    };

    private static EnchantReward CreateFromSave(SerializableReward save, Player player)
        => new(player, ModelDb.GetById<EnchantmentModel>(save.PredeterminedModelId));

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
            // 已经带附魔的牌不能再附（本体一次只挂一个）。
            var picked = await CardSelectCmd.FromDeckGeneric(owner, prefs, c => c.Enchantment == null);
            var card = picked?.FirstOrDefault();
            if (card != null)
            {
                CardCmd.Enchant(_enchant.ToMutable(), card, 1);
                Entry.Logger.Info($"附魔：给「{card}」挂上「{_enchant.Id.Entry}」");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"附魔选牌没能完成（{e.GetType().Name}），退回随机一张。");
        }
        return EnchantRandom(owner);
    }

    private bool EnchantRandom(Player owner)
    {
        try
        {
            var deck = owner.Deck?.Cards;
            if (deck == null) return false;
            var plain = deck.Where(c => c.Enchantment == null).ToList();
            if (plain.Count == 0) return false;
            var random = plain[Random.Shared.Next(plain.Count)];
            CardCmd.Enchant(_enchant.ToMutable(), random, 1);
            Entry.Logger.Info($"附魔（随机兜底）：给「{random}」挂上「{_enchant.Id.Entry}」");
            return true;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"随机附魔也失败了: {e.Message}");
            return false;
        }
    }
}
