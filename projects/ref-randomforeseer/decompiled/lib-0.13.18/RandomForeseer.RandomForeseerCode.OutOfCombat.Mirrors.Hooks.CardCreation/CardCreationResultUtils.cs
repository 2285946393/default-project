using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors.Hooks.CardCreation;

internal static class CardCreationResultUtils
{
	public static void UpgradeCardsOfType(IEnumerable<CardCreationResult> results, RelicModel modifyingRelic, CardType cardType)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		foreach (CardCreationResult item in results.Where((CardCreationResult result) => result.Card.Type == cardType && result.Card.IsUpgradable))
		{
			item.ModifyCard(PredictionUtils.CreateUpgradedCard(item.Card), modifyingRelic);
		}
	}

	public static void UpgradeValidCards(IEnumerable<CardCreationResult> results, RelicModel modifyingRelic)
	{
		foreach (CardCreationResult item in results.Where((CardCreationResult result) => result.Card.IsUpgradable))
		{
			item.ModifyCard(PredictionUtils.CreateUpgradedCard(item.Card), modifyingRelic);
		}
	}

	public static void EnchantValidCards<T>(IEnumerable<CardCreationResult> results, RelicModel modifyingRelic, decimal amount) where T : EnchantmentModel
	{
		T enchantment = ModelDb.Enchantment<T>();
		foreach (CardCreationResult item in results.Where((CardCreationResult result) => ((EnchantmentModel)enchantment).CanEnchant(result.Card)))
		{
			CardModel val = PredictionUtils.CreateEnchantedCard(((EnchantmentModel)enchantment).ToMutable(), item.Card, amount);
			item.ModifyCard(val, modifyingRelic);
		}
	}
}
