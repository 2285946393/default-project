using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using BloomlessSpire.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BloomlessSpire.Powers;

// 无限爪击：每回合开始时把一张幽灵爪击加入手牌。移植自一代 InfiniteClawsPower。
[RegisterPower]
public sealed class InfiniteClawsPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/InfiniteClawsPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/InfiniteClawsPower.png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        var combatState = player.Creature.CombatState!;
        var claw = combatState.CreateCard(ModelDb.Card<GhostClaw>(), player);
        await CardPileCmd.AddGeneratedCardToCombat(claw, PileType.Hand, player);
    }
}
