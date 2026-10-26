using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BloomlessSpire.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BloomlessSpire.Cards;

// 实用包：补充。移植自一代 utilitypack/Replenish。
// 从弃牌堆选 N 张牌回手，消耗（升级后不再消耗）。
[RegisterCard(typeof(BloomlessSpireCardPool))]
public sealed class Replenish : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;
    private const int BaseCards = 2;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(BaseCards)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Replenish() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = (int)DynamicVars.Cards.BaseValue;
        for (var i = 0; i < count; i++)
        {
            var discard = PileType.Discard.GetPile(Owner).Cards.ToList();
            if (discard.Count == 0) break;
            var picked = discard[Random.Shared.Next(discard.Count)];
            await CardPileCmd.Add(picked, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        // 一代升级后不再消耗
    }
}
