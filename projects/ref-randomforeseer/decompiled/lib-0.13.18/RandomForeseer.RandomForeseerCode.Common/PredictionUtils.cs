using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class PredictionUtils
{
	public static CardModel CreateCard(CardModel card, Player player)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		card = (CardModel)((AbstractModel)card).MutableClone();
		card.Owner = player;
		return card;
	}

	public static void UpgradeCard(CardModel card)
	{
		if (card.IsUpgradable)
		{
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
		}
	}

	public static CardModel CreateUpgradedCard(CardModel card)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_001c: Expected O, but got Unknown
		if (!card.IsUpgradable)
		{
			return card;
		}
		CardModel val = (CardModel)((AbstractModel)card).MutableClone();
		UpgradeCard(val);
		return val;
	}

	public static void EnchantCard(EnchantmentModel enchantment, CardModel card, decimal amount)
	{
		if (enchantment.CanEnchant(card))
		{
			if (card.Enchantment == null)
			{
				card.EnchantInternal(enchantment, amount);
				enchantment.ModifyCard();
			}
			else
			{
				EnchantmentModel enchantment2 = card.Enchantment;
				enchantment2.Amount += (int)amount;
			}
			card.FinalizeUpgradeInternal();
		}
	}

	public static CardModel CreateEnchantedCard(EnchantmentModel enchantment, CardModel card, decimal amount)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		if (!enchantment.CanEnchant(card))
		{
			return card;
		}
		CardModel val = (CardModel)((AbstractModel)card).MutableClone();
		EnchantCard(enchantment, val, amount);
		return val;
	}

	public static RelicModel CreateRelic(RelicModel relic, Player player)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		relic = (RelicModel)((AbstractModel)relic).MutableClone();
		relic.Owner = player;
		return relic;
	}

	public static PotionModel CreatePotion(PotionModel potion, Player player)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		potion = (PotionModel)((AbstractModel)potion).MutableClone();
		potion.Owner = player;
		return potion;
	}

	public static CardModel PredictTransformResult(CardModel original, Rng rng, bool isInCombat)
	{
		IEnumerable<CardModel> defaultTransformationOptions = CardFactory.GetDefaultTransformationOptions(original, isInCombat);
		return rng.NextItem<CardModel>(defaultTransformationOptions) ?? throw new InvalidOperationException($"Could not predict a transform result for {((AbstractModel)original).Id}.");
	}

	public static IReadOnlyList<PotionModel> PredictPotionRewards(Player player, int count, Rng rng)
	{
		Player player2 = player;
		Rng rng2 = rng;
		return (from _ in Enumerable.Range(0, count)
			select PotionFactory.CreateRandomPotionOutOfCombat(player2, rng2, (IEnumerable<PotionModel>)null)).ToList();
	}
}
