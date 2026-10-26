using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class PredictedCardExtensions
{
	public static PredictedCard Upgrade(this PredictedCard card)
	{
		if (card.Preview.IsUpgradable)
		{
			PredictionUtils.UpgradeCard(card.MutablePreview);
		}
		return card;
	}

	public static PredictedCard Enchant(this PredictedCard card, EnchantmentModel enchantment, decimal amount)
	{
		if (enchantment.CanEnchant(card.Preview))
		{
			PredictionUtils.EnchantCard(enchantment, card.MutablePreview, amount);
		}
		return card;
	}

	public static IEnumerable<PredictedCard> UpgradeIf(this IEnumerable<PredictedCard> cards, bool shouldUpgrade)
	{
		if (!shouldUpgrade)
		{
			return cards;
		}
		return cards.Select((PredictedCard card) => card.Upgrade());
	}

	public static IEnumerable<CardModel> SelectOriginals(this IEnumerable<PredictedCard> cards)
	{
		return cards.Select((PredictedCard card) => card.Original);
	}

	public static IEnumerable<CardModel> SelectPreviews(this IEnumerable<PredictedCard> cards)
	{
		return cards.Select((PredictedCard card) => card.Preview);
	}
}
