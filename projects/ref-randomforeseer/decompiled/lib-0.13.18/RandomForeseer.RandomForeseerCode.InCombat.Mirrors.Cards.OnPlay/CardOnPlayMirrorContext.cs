using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal sealed class CardOnPlayMirrorContext : CombatCardMirrorContext<CardModel>
{
	public required CardPlay CardPlay { get; init; }

	public Creature Target => CardPlay.Target ?? throw new InvalidOperationException("CardPlay has no target creature.");

	public Player TargetPlayer
	{
		get
		{
			Creature target = CardPlay.Target;
			return ((target != null) ? target.Player : null) ?? throw new InvalidOperationException("CardPlay has no target player.");
		}
	}

	public SimPlayerCombatState OwnerState => base.State.GetPlayerCombatState(base.PreviewCard.Owner);

	protected override AbstractModel GetDispatchSource(CardModel _)
	{
		return (AbstractModel)(object)base.OriginalCard;
	}
}
