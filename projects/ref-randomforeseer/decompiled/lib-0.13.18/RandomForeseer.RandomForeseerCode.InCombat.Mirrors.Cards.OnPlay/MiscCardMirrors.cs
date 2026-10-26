using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class MiscCardMirrors
{
	public static void AlchemizeOnPlay(Alchemize card, CardOnPlayMirrorContext context)
	{
		PotionModel potion = PotionFactory.CreateRandomPotionInCombat(((CardModel)card).Owner, context.Rng.CombatPotionGeneration, (IEnumerable<PotionModel>)null);
		context.Simulator.History.PotionGenerated(potion);
	}

	public static void MadScienceOnPlay(MadScience card, CardOnPlayMirrorContext context)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		if (card != null)
		{
			CardType tinkerTimeType = card.TinkerTimeType;
			if ((int)tinkerTimeType != 1)
			{
				if ((int)tinkerTimeType == 2 && (int)card.TinkerTimeRider == 6)
				{
					CardGenerationCardMirrors.MadScienceChaosOnPlay(card, context);
					return;
				}
			}
			else if ((int)card.TinkerTimeRider == 1)
			{
				VulnerableCardMirrors.MadScienceSappingOnPlay(card, context);
				return;
			}
		}
		context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
	}

	public static void SecondWindOnPlay(SecondWind card, CardOnPlayMirrorContext context)
	{
		PredictedCard[] array = context.OwnerState.Hand.Cards.Where((PredictedCard predicted) => (int)predicted.Preview.Type != 1).ToArray();
		foreach (PredictedCard card2 in array)
		{
			context.Simulator.Exhaust(card2);
			context.GainBlock(((CardModel)card).Owner.Creature);
		}
	}
}
