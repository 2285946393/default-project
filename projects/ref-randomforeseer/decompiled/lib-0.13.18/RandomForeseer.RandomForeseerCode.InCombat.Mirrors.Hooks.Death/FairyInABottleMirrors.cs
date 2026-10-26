using System;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal static class FairyInABottleMirrors
{
	private sealed class State
	{
		public bool WasUsed { get; set; }
	}

	public static bool ShouldDie(FairyInABottle potion, ShouldDieMirrorContext context)
	{
		if (context.Creature == ((PotionModel)potion).Owner.Creature)
		{
			return GetState(potion, context).WasUsed;
		}
		return true;
	}

	public static void AfterPreventingDeath(FairyInABottle potion, AfterPreventingDeathMirrorContext context)
	{
		GetState(potion, context).WasUsed = true;
		context.Simulator.Heal(context.Creature, Math.Max(1m, (decimal)context.Creature.MaxHp * 0.3m));
	}

	private static State GetState(FairyInABottle potion, CombatMirrorContext context)
	{
		return context.StateStore.Get<State>((AbstractModel)(object)potion);
	}
}
