using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class VulnerableCardMirrors
{
	public static void ShockwaveOnPlay(Shockwave card, CardOnPlayMirrorContext context)
	{
		int intValue = ((CardModel)card).DynamicVars["Power"].IntValue;
		foreach (Creature hittableEnemy in context.State.HittableEnemies)
		{
			ApplyWeakAndVulnerable(context, hittableEnemy, intValue, intValue);
		}
	}

	public static void ExposeOnPlay(Expose card, CardOnPlayMirrorContext context)
	{
		int intValue = ((CardModel)card).DynamicVars["Power"].IntValue;
		context.Simulator.LoseBlock(context.Target, context.State.GetCreature(context.Target).Block, ((CardModel)card).Owner.Creature);
		context.Simulator.RemovePower<ArtifactPower>(context.Target);
		context.ApplyPower<VulnerablePower>(context.Target, (decimal)intValue);
	}

	public static void DominateOnPlay(Dominate card, CardOnPlayMirrorContext context)
	{
		PowerModel val = context.ApplyPower<VulnerablePower>(context.Target);
		if (val == null)
		{
			val = (PowerModel)(object)context.Target.GetPower<VulnerablePower>();
		}
		int num = ((val != null) ? Math.Max(0, context.StateStore.GetPowerAmount(val).Amount) : 0);
		context.ApplyPower<StrengthPower>(((CardModel)card).Owner.Creature, (decimal)num);
	}

	public static void HighFiveOnPlay(HighFive card, CardOnPlayMirrorContext context)
	{
		Creature osty = ((CardModel)card).Owner.Osty;
		if (osty != null && context.State.GetCreature(osty).IsAlive)
		{
			DamageCmd.Attack(((DynamicVar)((CardModel)card).DynamicVars.OstyDamage).BaseValue).FromOsty(osty, (CardModel)(object)card, context.CardPlay).TargetingAllOpponents(context.CombatState)
				.Simulate(context.Simulator);
			context.ApplyPower<VulnerablePower>(context.State.HittableEnemies);
		}
	}

	public static void MoltenFistOnPlay(MoltenFist card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		if (!context.State.GetCreature(context.Target).IsAlive)
		{
			return;
		}
		VulnerablePower power = context.Target.GetPower<VulnerablePower>();
		if (power != null)
		{
			int amount = context.StateStore.GetPowerAmount((PowerModel)(object)power).Amount;
			if (amount > 0)
			{
				context.ApplyPower<VulnerablePower>(context.Target, (decimal)amount);
			}
		}
	}

	public static void MadScienceSappingOnPlay(MadScience card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		ApplyWeakAndVulnerable(context, context.Target, ((CardModel)card).DynamicVars["SappingWeak"].BaseValue, ((CardModel)card).DynamicVars["SappingVulnerable"].BaseValue);
	}

	private static void ApplyWeakAndVulnerable(CardOnPlayMirrorContext context, Creature target, decimal weakAmount, decimal vulnerableAmount)
	{
		context.ApplyPower<WeakPower>(target, weakAmount);
		context.ApplyPower<VulnerablePower>(target, vulnerableAmount);
	}
}
