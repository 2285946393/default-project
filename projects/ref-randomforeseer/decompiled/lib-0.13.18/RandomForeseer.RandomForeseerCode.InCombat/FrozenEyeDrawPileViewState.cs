using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class FrozenEyeDrawPileViewState
{
	private static readonly ConditionalWeakTable<NCardGrid, HashSet<CardModel>> PredictedShuffleCardsByGrid = new ConditionalWeakTable<NCardGrid, HashSet<CardModel>>();

	public static bool TryGetPredictedShuffleCards(NCardGrid grid, [NotNullWhen(true)] out HashSet<CardModel>? predictedCards)
	{
		return PredictedShuffleCardsByGrid.TryGetValue(grid, out predictedCards);
	}

	public static void SetPredictedShuffleCards(NCardGrid grid, IEnumerable<CardModel> predictedCards)
	{
		HashSet<CardModel> hashSet = new HashSet<CardModel>();
		foreach (CardModel predictedCard in predictedCards)
		{
			hashSet.Add(predictedCard);
		}
		PredictedShuffleCardsByGrid.AddOrUpdate(grid, hashSet);
	}
}
