using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal static class PredictionHoverTipExtensions
{
	public static IEnumerable<IHoverTip> ToPredictionHoverTips(this IEnumerable<CardModel> cards)
	{
		return cards.Select(PredictionHoverTipFactory.Card);
	}

	public static IEnumerable<IHoverTip> ToPredictionHoverTips(this IEnumerable<PotionModel> potions)
	{
		return potions.Select(PredictionHoverTipFactory.Potion);
	}

	public static IEnumerable<IHoverTip> ToPredictionHoverTips(this IEnumerable<RelicModel> relics)
	{
		return relics.Select(PredictionHoverTipFactory.Relic);
	}

	public static IEnumerable<IHoverTip> ToPredictionHoverTips(this IEnumerable<OrbModel> orbs)
	{
		return orbs.Select(PredictionHoverTipFactory.Orb);
	}

	public static IEnumerable<IHoverTip> ToPredictionCardBundleHoverTips(this IEnumerable<IReadOnlyList<CardModel>> bundles, PredictionCardBundleKind kind = PredictionCardBundleKind.Regular)
	{
		return from cards in bundles
			where cards.Count > 0
			select PredictionHoverTipFactory.CardBundle(cards, kind);
	}

	public static bool IsPredictionHoverTip(this IHoverTip tip)
	{
		if (!tip.IsPredictionTextHoverTip())
		{
			return tip.IsPredictionCardHoverTip();
		}
		return true;
	}

	public static bool IsPredictionTextHoverTip(this IHoverTip tip)
	{
		return tip.Id.StartsWith("RandomForeseer:Prediction", StringComparison.Ordinal);
	}

	public static bool IsPredictionCardHoverTip(this IHoverTip tip)
	{
		if (tip is PredictionCardHoverTip || tip is PredictionCardBundleHoverTip)
		{
			return true;
		}
		return false;
	}
}
