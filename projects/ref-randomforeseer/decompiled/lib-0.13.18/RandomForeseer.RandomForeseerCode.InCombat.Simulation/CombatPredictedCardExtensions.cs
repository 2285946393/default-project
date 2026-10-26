using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal static class CombatPredictedCardExtensions
{
	public static SimCardPile? GetPile(this PredictedCard card, CombatPredictionState state)
	{
		return card.GetPile(state.GetPlayerCombatState(card.Preview.Owner));
	}

	public static SimCardPile? GetPile(this PredictedCard card, SimPlayerCombatState playerCombatState)
	{
		PredictedCard card2 = card;
		return playerCombatState.AllPiles.FirstOrDefault((SimCardPile pile) => pile.Cards.Contains(card2));
	}

	public static PredictedCard CreateClone(this PredictedCard card)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		CardModel val = (CardModel)((AbstractModel)card.Preview).MutableClone();
		val._cloneOf = card.Original;
		val.ExhaustOnNextPlay = false;
		return PredictedCard.FromGenerated(val);
	}

	public static PredictedCard CreateCloneForPlayer(this PredictedCard card, Player player)
	{
		PredictedCard predictedCard = card.CreateClone();
		predictedCard.MutablePreview._owner = player;
		return predictedCard;
	}

	public static void Afflict(this PredictedCard card, AfflictionModel affliction, decimal amount)
	{
		CardModel mutablePreview = card.MutablePreview;
		mutablePreview.Affliction = affliction;
		mutablePreview.Affliction.Card = mutablePreview;
		mutablePreview.Affliction._amount = (int)amount;
	}

	public static void ClearAffliction(this PredictedCard card)
	{
		if (card.Preview.Affliction != null)
		{
			CardModel mutablePreview = card.MutablePreview;
			mutablePreview.Affliction.ClearInternal();
			mutablePreview.Affliction = null;
		}
	}

	public static int GeneratePlayCount(this PredictedCard card, CombatPredictionSimulator simulator, Creature? target)
	{
		List<AbstractModel> modifiers;
		int result = HookMirrors.ModifyCardPlayCount(simulator, card, card.Preview.GetEnchantedReplayCount() + 1, target, out modifiers);
		HookMirrors.AfterModifyingCardPlayCount(simulator, card, modifiers);
		return result;
	}

	public static int GetEnergyCostWithModifiers(this PredictedCard card, CombatPredictionSimulator simulator)
	{
		CardEnergyCost energyCost = card.Preview.EnergyCost;
		int num = energyCost._base;
		if (num < 0 || energyCost.CostsX)
		{
			return num;
		}
		foreach (LocalCostModifier localModifier in energyCost._localModifiers)
		{
			num = localModifier.Modify(num);
		}
		num = (int)HookMirrors.ModifyEnergyCostInCombat(simulator, card, num);
		return Math.Max(0, num);
	}

	public static int GetEnergyAmountToSpend(this PredictedCard card, CombatPredictionSimulator simulator)
	{
		if (!card.Preview.EnergyCost.CostsX)
		{
			return Math.Max(0, card.GetEnergyCostWithModifiers(simulator));
		}
		return simulator.State.GetPlayerCombatState(card.Preview.Owner).Energy;
	}

	public static int GetStarCostWithModifiers(this PredictedCard card, CombatPredictionSimulator simulator)
	{
		if (card.Preview.HasStarCostX)
		{
			return simulator.State.GetPlayerCombatState(card.Preview.Owner).Stars;
		}
		int currentStarCost = card.Preview.CurrentStarCost;
		return (int)HookMirrors.ModifyStarCost(simulator, card, currentStarCost);
	}

	public static bool CostsEnergyOrStars(this PredictedCard card, CombatPredictionSimulator simulator)
	{
		if (card.Preview.EnergyCost.CostsX || card.GetEnergyCostWithModifiers(simulator) <= 0)
		{
			if (!card.Preview.HasStarCostX)
			{
				return card.GetStarCostWithModifiers(simulator) > 0;
			}
			return false;
		}
		return true;
	}

	public static IReadOnlySet<CardKeyword> GetKeywords(this PredictedCard card, CombatPredictionSimulator simulator)
	{
		HashSet<CardKeyword> hashSet = card.Preview.LocalKeywords.ToHashSet();
		HookMirrors.ModifyKeywordsInCombat(simulator, card.Preview, hashSet);
		return hashSet;
	}

	public static PredictedCard SetToFreeThisTurn(this PredictedCard card)
	{
		card.MutablePreview.SetToFreeThisTurn();
		return card;
	}

	public static PredictedCard SetToFreeThisCombat(this PredictedCard card)
	{
		card.MutablePreview.SetToFreeThisCombat();
		return card;
	}
}
