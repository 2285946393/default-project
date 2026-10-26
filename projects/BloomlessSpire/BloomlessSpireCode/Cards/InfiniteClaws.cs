using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BloomlessSpire.Characters;
using BloomlessSpire.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BloomlessSpire.Cards;

// 无限爪击：获得能力「无限爪击」。移植自一代 InfiniteClaws。
[RegisterCard(typeof(BloomlessSpireCardPool))]
public sealed class InfiniteClaws : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Power;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { ClawTags.Claw };

    public InfiniteClaws() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InfiniteClawsPower>(choiceContext, Owner.Creature, 1, Owner.Creature, null);
    }

    protected override void OnUpgrade()
    {
        // 一代升级后变成固有；这里暂时不实现固有。
    }
}
