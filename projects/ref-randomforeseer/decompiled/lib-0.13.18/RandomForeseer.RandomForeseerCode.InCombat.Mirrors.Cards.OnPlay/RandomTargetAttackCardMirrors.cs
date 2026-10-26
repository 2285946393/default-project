using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class RandomTargetAttackCardMirrors
{
	public static void FlakCannonOnPlay(FlakCannon card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		List<PredictedCard> list = context2.OwnerState.AllCards.Where((PredictedCard predictedCard) => (int)predictedCard.Preview.Type == 4 && !context2.OwnerState.ExhaustPile.Cards.Contains(predictedCard)).ToList();
		foreach (PredictedCard item in list)
		{
			context2.Simulator.Exhaust(item);
		}
		context2.AttackRandomOpponents(list.Count);
	}

	public static void RicochetOnPlay(Ricochet card, CardOnPlayMirrorContext context)
	{
		context.AttackRandomOpponents(((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue);
	}

	public static void RipAndTearOnPlay(RipAndTear card, CardOnPlayMirrorContext context)
	{
		context.AttackRandomOpponents(2);
	}

	public static void StardustOnPlay(Stardust card, CardOnPlayMirrorContext context)
	{
		context.AttackRandomOpponents(context.ResolveStarXValue());
	}

	public static void SweepingGazeOnPlay(SweepingGaze card, CardOnPlayMirrorContext context)
	{
		Creature osty = ((CardModel)card).Owner.Osty;
		if (osty != null && context.State.GetCreature(osty).IsAlive)
		{
			DamageCmd.Attack(((DynamicVar)((CardModel)card).DynamicVars.OstyDamage).BaseValue).FromOsty(osty, (CardModel)(object)card, context.CardPlay).TargetingRandomOpponents(context.CombatState, true)
				.Simulate(context.Simulator);
		}
	}

	public static void SwordBoomerangOnPlay(SwordBoomerang card, CardOnPlayMirrorContext context)
	{
		context.AttackRandomOpponents(((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue);
	}

	public static void VolleyOnPlay(Volley card, CardOnPlayMirrorContext context)
	{
		context.AttackRandomOpponents(context.ResolveEnergyXValue());
	}
}
