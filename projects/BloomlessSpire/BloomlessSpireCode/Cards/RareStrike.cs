using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BloomlessSpire.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BloomlessSpire.Cards;

// 实用包：稀有打击。移植自一代 utilitypack/RareStrike。
// 6 伤 + 每个稀有牌额外 N 伤害（手牌/抽牌堆/弃牌堆）。
[RegisterCard(typeof(BloomlessSpireCardPool))]
public sealed class RareStrike : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new IntVar("BonusPerRare", 2)
    ];

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    public RareStrike() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var bonus = DynamicVars["BonusPerRare"].BaseValue * CountRares();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + bonus)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    private int CountRares()
    {
        var count = 0;
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            foreach (var card in pileType.GetPile(Owner).Cards)
            {
                if (card.Rarity == CardRarity.Rare) count++;
            }
        }
        return count;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BonusPerRare"].UpgradeValueBy(2);
    }
}
