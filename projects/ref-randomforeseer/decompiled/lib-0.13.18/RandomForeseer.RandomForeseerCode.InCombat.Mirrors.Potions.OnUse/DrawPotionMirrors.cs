using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

internal static class DrawPotionMirrors
{
	public static void BottledPotentialOnUse(BottledPotential potion, PotionOnUseMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		context.Simulator.MoveHandToDrawPile(targetPlayer);
		context.Simulator.Shuffle(targetPlayer);
		context.Simulator.Draw(targetPlayer, ((DynamicVar)((PotionModel)potion).DynamicVars.Cards).BaseValue);
	}

	public static void ClarityOnUse(Clarity potion, PotionOnUseMirrorContext context)
	{
		context.Simulator.Draw(context.TargetPlayer, ((DynamicVar)((PotionModel)potion).DynamicVars.Cards).BaseValue);
	}

	public static void CureAllOnUse(CureAll potion, PotionOnUseMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		context.Simulator.GainEnergy(targetPlayer, ((DynamicVar)((PotionModel)potion).DynamicVars.Energy).BaseValue);
		context.Simulator.Draw(targetPlayer, ((DynamicVar)((PotionModel)potion).DynamicVars.Cards).BaseValue);
	}

	public static void GlowwaterPotionOnUse(GlowwaterPotion potion, PotionOnUseMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		context.Simulator.ExhaustHand(targetPlayer);
		context.Simulator.Draw(targetPlayer, ((DynamicVar)((PotionModel)potion).DynamicVars.Cards).BaseValue);
	}

	public static void SneckoOilOnUse(SneckoOil potion, PotionOnUseMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		SimCardPile hand = context.State.GetPlayerCombatState(targetPlayer).Hand;
		context.Simulator.Draw(targetPlayer, ((DynamicVar)((PotionModel)potion).DynamicVars.Cards).BaseValue);
		foreach (PredictedCard card in hand.Cards)
		{
			if (!card.Preview.EnergyCost.CostsX && card.Preview.EnergyCost.GetWithModifiers((CostModifiers)0) >= 0)
			{
				card.MutablePreview.EnergyCost.SetThisTurnOrUntilPlayed(context.Rng.CombatEnergyCosts.NextInt(4), false);
			}
		}
		context.History.CardCostsRandomized(hand.Cards);
	}

	public static void SwiftPotionOnUse(SwiftPotion potion, PotionOnUseMirrorContext context)
	{
		context.Simulator.Draw(context.TargetPlayer, ((DynamicVar)((PotionModel)potion).DynamicVars.Cards).BaseValue);
	}
}
