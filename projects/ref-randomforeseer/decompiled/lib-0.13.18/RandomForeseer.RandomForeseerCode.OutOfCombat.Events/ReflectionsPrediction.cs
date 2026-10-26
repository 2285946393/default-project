using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class ReflectionsPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Reflections reflections, EventOption option)
	{
		if (option.TextKey != "REFLECTIONS.pages.INITIAL.options.TOUCH_A_MIRROR")
		{
			return Array.Empty<IHoverTip>();
		}
		Player owner = ((EventModel)reflections).Owner;
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(PredictTouchAMirror(owner.Deck.Cards, ((EventModel)reflections).Rng).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}

	private static IReadOnlyList<CardModel> PredictTouchAMirror(IReadOnlyList<CardModel> deckCards, Rng realRng)
	{
		Rng val = realRng.Clone();
		List<PredictedCard> source = deckCards.Select((CardModel card) => new PredictedCard(card)).ToList();
		List<CardModel> list = new List<CardModel>();
		List<PredictedCard> list2 = source.Where((PredictedCard card) => card.Preview.IsUpgraded).ToList();
		for (int i = 0; i < 2; i++)
		{
			if (list2.Count <= 0)
			{
				break;
			}
			PredictedCard predictedCard = val.NextItem<PredictedCard>((IEnumerable<PredictedCard>)list2);
			if (predictedCard == null)
			{
				break;
			}
			list2.Remove(predictedCard);
			predictedCard.MutablePreview.DowngradeInternal();
			list.Add(predictedCard.Preview);
		}
		List<PredictedCard> list3 = source.Where((PredictedCard card) => card.Preview.IsUpgradable).ToList();
		for (int j = 0; j < 4; j++)
		{
			if (list3.Count <= 0)
			{
				break;
			}
			PredictedCard predictedCard2 = val.NextItem<PredictedCard>((IEnumerable<PredictedCard>)list3);
			if (predictedCard2 == null)
			{
				break;
			}
			list3.Remove(predictedCard2);
			list.Add(PredictionUtils.CreateUpgradedCard(predictedCard2.Preview));
		}
		return list;
	}
}
