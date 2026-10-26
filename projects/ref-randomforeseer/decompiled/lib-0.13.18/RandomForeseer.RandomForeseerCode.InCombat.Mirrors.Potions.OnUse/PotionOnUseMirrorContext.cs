using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

internal sealed class PotionOnUseMirrorContext : CombatMirrorContext<PotionModel>
{
	public required PotionModel Potion { get; init; }

	public required Creature? Target { get; init; }

	public Player TargetPlayer
	{
		get
		{
			Creature? target = Target;
			return ((target != null) ? target.Player : null) ?? throw new InvalidOperationException("This potion mirror requires a player target.");
		}
	}
}
