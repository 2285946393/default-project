using System;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal static class LizardTailMirrors
{
	private sealed class State
	{
		public bool WasUsed { get; set; } = relic.WasUsed;


		public State(LizardTail relic)
		{
		}
	}

	public static bool ShouldDieLate(LizardTail relic, ShouldDieMirrorContext context)
	{
		if (context.Creature == ((RelicModel)relic).Owner.Creature)
		{
			return GetState(relic, context).WasUsed;
		}
		return true;
	}

	public static void AfterPreventingDeath(LizardTail relic, AfterPreventingDeathMirrorContext context)
	{
		GetState(relic, context).WasUsed = true;
		decimal amount = Math.Max(1m, (decimal)context.Creature.MaxHp * (((DynamicVar)((RelicModel)relic).DynamicVars.Heal).BaseValue / 100m));
		context.Simulator.Heal(context.Creature, amount);
	}

	private static State GetState(LizardTail relic, CombatMirrorContext context)
	{
		LizardTail relic2 = relic;
		return context.StateStore.Get((AbstractModel)(object)relic2, () => new State(relic2));
	}
}
