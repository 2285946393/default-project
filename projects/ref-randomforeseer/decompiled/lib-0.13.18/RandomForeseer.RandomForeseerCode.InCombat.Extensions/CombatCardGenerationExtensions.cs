using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Extensions;

internal static class CombatCardGenerationExtensions
{
	public static IEnumerable<CardModel> FilterForCombatAndPlayerCount(this IEnumerable<CardModel> cards, Player player)
	{
		return CardFactory.FilterForPlayerCount(player.RunState, CardFactory.FilterForCombat(cards));
	}

	public static IEnumerable<CardModel> TakeRandomDistinctForCombat(this IEnumerable<CardModel> cards, Player player, int count, Rng rng)
	{
		return IEnumerableExtensions.TakeRandom<CardModel>(cards.FilterForCombatAndPlayerCount(player), count, rng);
	}

	public static IEnumerable<CardModel> TakeRandomForCombat(this IEnumerable<CardModel> cards, Player player, int count, Rng rng)
	{
		List<CardModel> list = cards.FilterForCombatAndPlayerCount(player).ToList();
		if (list.Count == 0)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> list2 = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			list2.Add(rng.NextItem<CardModel>((IEnumerable<CardModel>)list));
		}
		return list2;
	}

	public static IEnumerable<PredictedCard> GetDistinctForCombat(this IEnumerable<CardModel> cards, Player player, int count, Rng rng)
	{
		Player player2 = player;
		return from card in cards.TakeRandomDistinctForCombat(player2, count, rng)
			select PredictedCard.Create(card, player2);
	}

	public static IEnumerable<PredictedCard> GetForCombat(this IEnumerable<CardModel> cards, Player player, int count, Rng rng)
	{
		Player player2 = player;
		return from card in cards.TakeRandomForCombat(player2, count, rng)
			select PredictedCard.Create(card, player2);
	}
}
