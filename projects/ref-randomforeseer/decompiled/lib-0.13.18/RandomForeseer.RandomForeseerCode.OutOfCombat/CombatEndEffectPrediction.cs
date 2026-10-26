using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class CombatEndEffectPrediction
{
	public static void FastForwardMonsterRoomCombatEndHooks(RunPredictionContext context)
	{
		foreach (Player player in ((IPlayerCollection)context.RunState).Players)
		{
			if (!player.IsActiveForHooks)
			{
				continue;
			}
			RunPredictionContext context2 = context.ForPlayer(player);
			foreach (RelicModel item in player.Relics.Where((RelicModel relic) => !relic.IsMelted))
			{
				FishingRod val = (FishingRod)(object)((item is FishingRod) ? item : null);
				if (val == null)
				{
					PaelsTooth val2 = (PaelsTooth)(object)((item is PaelsTooth) ? item : null);
					if (val2 != null)
					{
						FastForwardPaelsToothAfterCombatEnd(context2, val2);
					}
				}
				else
				{
					FastForwardFishingRodAfterCombatEnd(context2, val);
				}
			}
		}
	}

	private static void FastForwardFishingRodAfterCombatEnd(RunPredictionContext context, FishingRod fishingRod)
	{
		if ((fishingRod.CombatsSeen + 1) % ((RelicModel)fishingRod).DynamicVars["Combats"].IntValue == 0)
		{
			List<PredictedCard> list = context.Deck.Cards.Where((PredictedCard card) => card.Preview.IsUpgradable).ToList();
			context.SharedRng.Niche.NextItem<PredictedCard>((IEnumerable<PredictedCard>)list)?.Upgrade();
		}
	}

	private static void FastForwardPaelsToothAfterCombatEnd(RunPredictionContext context, PaelsTooth paelsTooth)
	{
		if (!context.Player.Creature.IsDead && paelsTooth.SerializableCards.Count != 0)
		{
			SerializableCard val = context.Rng.Rewards.NextItem<SerializableCard>((IEnumerable<SerializableCard>)paelsTooth.SerializableCards);
			if (val != null)
			{
				CardModel val2 = CardModel.FromSerializable(val);
				val2.Owner = context.Player;
				context.Deck.Add(PredictedCard.FromGenerated(val2).Upgrade());
			}
		}
	}
}
