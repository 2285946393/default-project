using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Utils;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal static class PredictionCardDiscoveryScope
{
	private sealed class CardsSet
	{
		public CardModel[] Cards { get; }

		public CardsSet(CardModel[] cards)
		{
			Cards = cards;
			base._002Ector();
		}
	}

	private static readonly HashSet<CardsSet> ActiveCardSets = new HashSet<CardsSet>();

	public static IDisposable? Enter(CardHoverTip tip)
	{
		CardsSet cardsSet = ((tip is PredictionCardHoverTip) ? new CardsSet((CardModel[])(object)new CardModel[1] { tip.Card }) : ((!(tip is PredictionCardBundleHoverTip predictionCardBundleHoverTip)) ? null : new CardsSet(predictionCardBundleHoverTip.Cards.ToArray())));
		CardsSet cards = cardsSet;
		if (cards == null)
		{
			return null;
		}
		ActiveCardSets.Add(cards);
		return new DisposableAction(delegate
		{
			ActiveCardSets.Remove(cards);
		});
	}

	public static bool Contains(CardModel card)
	{
		CardModel card2 = card;
		return ActiveCardSets.Any((CardsSet set) => set.Cards.Contains(card2));
	}
}
