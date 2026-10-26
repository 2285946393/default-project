using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace BloomlessSpire.Cards;

// 「爪击」标签：带这个标签的伤害牌会被爪击成长机制强化。
[RegisterOwnedCardTag(nameof(Claw))]
public class ClawTags
{
    public static readonly CardTag Claw =
        ModContentRegistry.GetQualifiedCardTagId(Entry.ModId, nameof(Claw)).GetModCardTag();

    // 被 MutateClaw 等卡牌动态赋予 Claw 标签的卡牌 ID
    public static readonly HashSet<ModelId> GrantedIds = new();

    public static bool HasClaw(CardModel card)
        => card.Tags.Contains(Claw) || GrantedIds.Contains(card.Id);
}

// 爪击包核心机制：把所有带「爪击」标签的伤害牌基础伤害永久 +value（手牌/抽牌堆/弃牌堆全覆盖）。
// 移植自一代 AbstractClawCard.ClawUp。
public static class ClawHelper
{
    public static void ClawUp(Player owner, int value)
    {
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            var pile = pileType.GetPile(owner);
            foreach (var card in pile.Cards)
            {
                if (ClawTags.HasClaw(card) && card.DynamicVars.Damage.BaseValue > 0)
                {
                    card.DynamicVars.Damage.BaseValue += value;
                }
            }
        }
    }
}
