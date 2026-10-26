using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BloomlessSpire.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BloomlessSpire.Cards;

// 实用包：弱效诅咒。移植自一代 utilitypack/LesserHex。
// 敌人 虚弱1 + 易伤1 + 力量-1，消耗，并在弃牌堆生成一张强效诅咒。
[RegisterCard(typeof(BloomlessSpireCardPool))]
public sealed class LesserHex : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(1),
        new PowerVar<VulnerablePower>(1),
        new PowerVar<StrengthPower>(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public LesserHex() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await PowerCmd.Apply<WeakPower>(
            choiceContext, cardPlay.Target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext, cardPlay.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext, cardPlay.Target, -DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);

        var hex = Owner.Creature.CombatState!.CreateCard(ModelDb.Card<GreaterHex>(), Owner);
        if (IsUpgraded) CardCmd.Upgrade(hex);
        await CardPileCmd.AddGeneratedCardToCombat(hex, PileType.Discard, Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级效果体现在生成的强效诅咒是升级版
    }
}
