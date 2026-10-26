using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace BloomlessSpire.Capabilities;

// 「翻找」抽到的那张牌：本回合费用 -1。
[RegisterModelCapability]
public sealed class CostReduceCapability : CardCapability, ICardEnergyCostContributor
{
    public int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
        => Math.Max(0, currentCost - 1);
}
