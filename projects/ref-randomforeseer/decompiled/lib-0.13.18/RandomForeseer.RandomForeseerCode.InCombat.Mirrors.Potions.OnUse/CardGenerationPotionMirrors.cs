using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

internal static class CardGenerationPotionMirrors
{
	public static void OnUse(PotionModel potion, PotionOnUseMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		PotionCardGenerationResult potionCardGenerationResult = Generate(potion, targetPlayer, context.Rng.CombatCardGeneration) ?? throw new UnreachableException($"No card generation policy for registered potion {((AbstractModel)potion).Id}.");
		if (potionCardGenerationResult.Cards.Count != 0)
		{
			if (potionCardGenerationResult.AddsToHand)
			{
				context.Simulator.AddGeneratedCardsToCombat(potionCardGenerationResult.Cards, (PileType)2, potion.Owner, (CardPilePosition)1);
				return;
			}
			context.History.CardGenerationOptions(potionCardGenerationResult.Cards);
			context.History.RecordRisk(PredictionRiskReason.UnresolvedPlayerChoice);
		}
	}

	public static PotionCardGenerationResult? Generate(PotionModel potion, Player target, Rng rng)
	{
		if (!(potion is AttackPotion))
		{
			if (!(potion is SkillPotion))
			{
				if (!(potion is PowerPotion))
				{
					if (!(potion is ColorlessPotion))
					{
						if (!(potion is CosmicConcoction))
						{
							if (potion is OrobicAcid)
							{
								return new PotionCardGenerationResult(GenerateOrobicAcidCards(target, rng), AddsToHand: true);
							}
							return null;
						}
						List<PredictedCard> list = new List<PredictedCard>();
						list.AddRange(from card in GenerateColorlessCards(target, ((DynamicVar)potion.DynamicVars.Cards).IntValue, rng)
							select card.Upgrade());
						return new PotionCardGenerationResult(new _003C_003Ez__ReadOnlyList<PredictedCard>(list), AddsToHand: true);
					}
					return new PotionCardGenerationResult(GenerateColorlessCards(target, 3, rng), AddsToHand: false);
				}
				return new PotionCardGenerationResult(GenerateCharacterCards(target, (CardType)3, 3, rng), AddsToHand: false);
			}
			return new PotionCardGenerationResult(GenerateCharacterCards(target, (CardType)2, 3, rng), AddsToHand: false);
		}
		return new PotionCardGenerationResult(GenerateCharacterCards(target, (CardType)1, 3, rng), AddsToHand: false);
	}

	private static List<PredictedCard> GenerateCharacterCards(Player player, CardType type, int count, Rng rng)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return (from candidate in player.GetUnlockedCharacterCards()
			where candidate.Type == type
			select candidate).GetDistinctForCombat(player, count, rng).ToList();
	}

	private static List<PredictedCard> GenerateColorlessCards(Player player, int count, Rng rng)
	{
		return player.GetUnlockedColorlessCards().GetDistinctForCombat(player, count, rng).ToList();
	}

	private static List<PredictedCard> GenerateOrobicAcidCards(Player player, Rng rng)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		List<PredictedCard> list = new List<PredictedCard>();
		CardType[] array = new CardType[3];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		CardType[] array2 = (CardType[])(object)array;
		foreach (CardType type in array2)
		{
			list.AddRange((from candidate in player.GetUnlockedCharacterCards()
				where candidate.Type == type
				select candidate).GetDistinctForCombat(player, 1, rng));
		}
		foreach (PredictedCard item in list)
		{
			item.SetToFreeThisTurn();
		}
		return list;
	}
}
