using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class CardGenerationCardMirrors
{
	public static void AbundanceOnPlay(Abundance card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = (from candidate in (from candidate in ((CardModel)card).Owner.GetUnlockedCharacterCards()
				where (int)candidate.Type == 3
				select candidate).GetDistinctForCombat(((CardModel)card).Owner, 3, context.Rng.CombatCardGeneration)
			select candidate.Upgrade()).ToList();
		RecordOptions(context, cards);
	}

	public static void BundleOfJoyOnPlay(BundleOfJoy card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = ((CardModel)card).Owner.GetUnlockedColorlessCards().GetDistinctForCombat(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue, context.Rng.CombatCardGeneration).ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void DistractionOnPlay(Distraction card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = (from generatedCard in (from candidate in ((CardModel)card).Owner.GetUnlockedCharacterCards()
				where (int)candidate.Type == 2
				select candidate).GetDistinctForCombat(((CardModel)card).Owner, 1, context.Rng.CombatCardGeneration)
			select generatedCard.SetToFreeThisTurn()).ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void DiscoveryOnPlay(Discovery card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = ((CardModel)card).Owner.GetUnlockedCharacterCards().GetDistinctForCombat(((CardModel)card).Owner, 3, context.Rng.CombatCardGeneration).ToList();
		RecordOptions(context, cards);
	}

	public static void InfernalBladeOnPlay(InfernalBlade card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = (from generatedCard in (from candidate in ((CardModel)card).Owner.GetUnlockedCharacterCards()
				where (int)candidate.Type == 1
				select candidate).GetDistinctForCombat(((CardModel)card).Owner, 1, context.Rng.CombatCardGeneration)
			select generatedCard.SetToFreeThisTurn()).ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void JackOfAllTradesOnPlay(JackOfAllTrades card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = (from candidate in ((CardModel)card).Owner.GetUnlockedColorlessCards()
			where !(candidate is JackOfAllTrades)
			select candidate).GetDistinctForCombat(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue, context.Rng.CombatCardGeneration).ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void JackpotOnPlay(Jackpot card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		List<PredictedCard> cards = ((CardModel)card).Owner.GetUnlockedCharacterCards().Where(delegate(CardModel candidate)
		{
			CardEnergyCost energyCost = candidate.EnergyCost;
			return energyCost != null && energyCost.Canonical == 0 && !energyCost.CostsX;
		}).GetForCombat(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue, context.Rng.CombatCardGeneration)
			.UpgradeIf(((CardModel)card).IsUpgraded)
			.ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void LargesseOnPlay(Largesse card, CardOnPlayMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		List<PredictedCard> cards = targetPlayer.GetUnlockedColorlessCards().GetDistinctForCombat(targetPlayer, 1, context.Rng.CombatCardGeneration).UpgradeIf(((CardModel)card).IsUpgraded)
			.ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void MadScienceChaosOnPlay(MadScience card, CardOnPlayMirrorContext context)
	{
		context.GainBlock(((CardModel)card).Owner.Creature);
		List<PredictedCard> cards = (from generatedCard in ((CardModel)card).Owner.GetUnlockedCharacterCards().GetDistinctForCombat(((CardModel)card).Owner, 1, context.Rng.CombatCardGeneration)
			select generatedCard.SetToFreeThisTurn()).ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void ManifestAuthorityOnPlay(ManifestAuthority card, CardOnPlayMirrorContext context)
	{
		context.GainBlock(((CardModel)card).Owner.Creature);
		List<PredictedCard> cards = ((CardModel)card).Owner.GetUnlockedColorlessCards().GetDistinctForCombat(((CardModel)card).Owner, 1, context.Rng.CombatCardGeneration).UpgradeIf(((CardModel)card).IsUpgraded)
			.ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void MetamorphosisOnPlay(Metamorphosis card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = (from generatedCard in (from candidate in ((CardModel)card).Owner.GetUnlockedCharacterCards()
				where (int)candidate.Type == 1
				select candidate).GetForCombat(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue, context.Rng.CombatCardGeneration)
			select generatedCard.SetToFreeThisCombat()).ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)1, ((CardModel)card).Owner, (CardPilePosition)3);
	}

	public static void QuasarOnPlay(Quasar card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = ((CardModel)card).Owner.GetUnlockedColorlessCards().GetDistinctForCombat(((CardModel)card).Owner, 3, context.Rng.CombatCardGeneration).UpgradeIf(((CardModel)card).IsUpgraded)
			.ToList();
		RecordOptions(context, cards);
	}

	public static void SplashOnPlay(Splash card, CardOnPlayMirrorContext context)
	{
		List<CardPoolModel> list = ((CardModel)card).Owner.UnlockState.CharacterCardPools.ToList();
		if (list.Count > 1)
		{
			list.Remove(((CardModel)card).Owner.Character.CardPool);
		}
		List<PredictedCard> cards = (from candidate in list.SelectMany(((CardModel)card).Owner.GetUnlockedCards)
			where (int)candidate.Type == 1
			select candidate).GetDistinctForCombat(((CardModel)card).Owner, 3, context.Rng.CombatCardGeneration).UpgradeIf(((CardModel)card).IsUpgraded).ToList();
		RecordOptions(context, cards);
	}

	public static void StokeOnPlay(Stoke card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> list = context.OwnerState.Hand.Cards.ToList();
		foreach (PredictedCard item in list)
		{
			context.Simulator.Exhaust(item);
		}
		List<PredictedCard> cards = ((CardModel)card).Owner.GetUnlockedCharacterCards().GetForCombat(((CardModel)card).Owner, list.Count, context.Rng.CombatCardGeneration).UpgradeIf(((CardModel)card).IsUpgraded)
			.ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void WhiteNoiseOnPlay(WhiteNoise card, CardOnPlayMirrorContext context)
	{
		List<PredictedCard> cards = (from generatedCard in (from candidate in ((CardModel)card).Owner.GetUnlockedCharacterCards()
				where (int)candidate.Type == 3
				select candidate).GetDistinctForCombat(((CardModel)card).Owner, 1, context.Rng.CombatCardGeneration)
			select generatedCard.SetToFreeThisTurn()).ToList();
		context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	private static void RecordOptions(CardOnPlayMirrorContext context, IReadOnlyList<PredictedCard> cards)
	{
		if (cards.Count != 0)
		{
			context.Simulator.History.CardGenerationOptions(cards);
			context.History.RecordRisk(PredictionRiskReason.UnresolvedPlayerChoice);
		}
	}
}
