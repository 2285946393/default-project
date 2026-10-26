using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BloomlessSpire.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BloomlessSpire.Powers;

// 「以爪拒之」挂在敌人身上：它被爪击牌攻击时，攻击者获得格挡。
[RegisterPower]
public sealed class ReturnBlockOnClawPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/ReturnBlockOnClawPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/ReturnBlockOnClawPower.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(2m, ValueProp.Move)
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (!ClawTags.HasClaw(card) || cardPlay.Target != Owner)
            return;

        var player = Owner.Player;
        if (player != null)
            await CreatureCmd.GainBlock(player.Creature, DynamicVars.Block, cardPlay);
    }
}
