using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class MerchantRestockPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(MerchantEntry entry)
	{
		ModSettings settings = ModData.Settings;
		if (ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.MerchantRestockPredictionEnabled && entry != null && entry.IsStocked && entry.EnoughGold)
		{
			Player player = entry._player;
			if (player != null)
			{
				IRunState runState = player.RunState;
				if (runState != null)
				{
					AbstractRoom currentRoom = runState.CurrentRoom;
					MerchantRoom val = (MerchantRoom)(object)((currentRoom is MerchantRoom) ? currentRoom : null);
					if (val != null)
					{
						MerchantInventory val2 = val.Inventories.Find((MerchantInventory inventory) => inventory.Player == player);
						if (val2 != null && HookMirrors.ShouldRefillMerchantEntry(player.RunState, entry, player))
						{
							RunPredictionContext context = new RunPredictionContext(player);
							MerchantCardEntry val3 = (MerchantCardEntry)(object)((entry is MerchantCardEntry) ? entry : null);
							if (val3 == null)
							{
								MerchantPotionEntry val4 = (MerchantPotionEntry)(object)((entry is MerchantPotionEntry) ? entry : null);
								if (val4 == null)
								{
									MerchantRelicEntry val5 = (MerchantRelicEntry)(object)((entry is MerchantRelicEntry) ? entry : null);
									if (val5 != null)
									{
										return PredictRelic(context, val5, val2);
									}
									return Array.Empty<IHoverTip>();
								}
								return PredictPotion(context, val4, val2);
							}
							return PredictCard(context, val3, val2);
						}
					}
				}
			}
		}
		return Array.Empty<IHoverTip>();
	}

	private static IReadOnlyList<IHoverTip> PredictCard(RunPredictionContext context, MerchantCardEntry entry, MerchantInventory inventory)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Expected O, but got Unknown
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		Player player = context.Player;
		HashSet<CardModel> second = inventory.CardEntries.Select(delegate(MerchantCardEntry other)
		{
			CardCreationResult creationResult = other.CreationResult;
			return (creationResult == null) ? null : creationResult.Card.CanonicalInstance;
		}).OfType<CardModel>().ToHashSet();
		IEnumerable<CardModel> options = entry._cardPool.Except(second);
		options = HookMirrors.ModifyMerchantCardPool(player.RunState, player, options);
		options = options.Where((CardModel card) => (int)card.Rarity != 1);
		options = CardFactory.FilterForPlayerCount(player.RunState, options);
		CardModel[] filteredOptions = options.ToArray();
		CardType? cardType2 = entry._cardType;
		IEnumerable<CardModel> enumerable;
		if (cardType2.HasValue)
		{
			CardType cardType = cardType2.GetValueOrDefault();
			CardRarity cardRarity2 = context.CardRarityOdds.RollWithoutChangingFutureOdds((CardRarityOddsType)4);
			cardRarity2 = HookMirrors.ModifyMerchantCardRarity(player.RunState, player, cardRarity2);
			cardRarity2 = CardFactory.GetNextAllowedRarity(cardRarity2, (Func<CardRarity, bool>)((CardRarity rarity) => filteredOptions.Any((CardModel card) => card.Rarity == rarity && card.Type == cardType)));
			if ((int)cardRarity2 == 0)
			{
				throw new InvalidOperationException($"Could not predict a merchant card rarity for type {cardType}.");
			}
			enumerable = filteredOptions.Where((CardModel card) => card.Rarity == cardRarity2 && card.Type == cardType);
		}
		else
		{
			CardRarity cardRarity = entry._cardRarity ?? throw new InvalidOperationException("Merchant card entry has neither a card type nor a card rarity.");
			cardRarity = HookMirrors.ModifyMerchantCardRarity(player.RunState, player, cardRarity);
			enumerable = filteredOptions.Where((CardModel card) => card.Rarity == cardRarity);
		}
		CardCreationResult val = new CardCreationResult(PredictionUtils.CreateCard(context.Rng.Shops.NextItem<CardModel>(enumerable) ?? throw new InvalidOperationException("Could not predict a merchant restock card."), player));
		CardRewardPrediction.RollForUpgrade(player, val.Card, -999999999m, context.Rng.Rewards);
		int num = 1;
		List<CardCreationResult> list = new List<CardCreationResult>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = val;
		HookMirrors.ModifyMerchantCardCreationResults(context, list);
		int cost = MerchantCardEntry.GetCost(val.Card);
		cost = Mathf.RoundToInt((float)cost * context.Rng.Shops.NextFloat(0.95f, 1.05f));
		cost = (int)HookMirrors.ModifyMerchantPrice(context.RunState, player, (MerchantEntry)(object)entry, cost);
		return new _003C_003Ez__ReadOnlyArray<IHoverTip>((IHoverTip[])(object)new IHoverTip[2]
		{
			(IHoverTip)(object)CreateTextTip(val.Card.Title, cost),
			PredictionHoverTipFactory.Card(val.Card)
		});
	}

	private static IReadOnlyList<IHoverTip> PredictPotion(RunPredictionContext context, MerchantPotionEntry entry, MerchantInventory inventory)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		PotionModel val = PotionFactory.CreateRandomPotionOutOfCombat(context.Player, context.Rng.Shops, (IEnumerable<PotionModel>)Array.Empty<PotionModel>());
		int cost = MerchantPotionEntry.GetCost(val.Rarity);
		cost = (int)Mathf.Round((float)cost * context.Rng.Shops.NextFloat(0.95f, 1.05f));
		cost = (int)HookMirrors.ModifyMerchantPrice(context.RunState, context.Player, (MerchantEntry)(object)entry, cost);
		return new _003C_003Ez__ReadOnlyArray<IHoverTip>((IHoverTip[])(object)new IHoverTip[2]
		{
			(IHoverTip)(object)CreateTextTip(val.Title.GetFormattedText(), cost),
			PredictionHoverTipFactory.Potion(val)
		});
	}

	private static IReadOnlyList<IHoverTip> PredictRelic(RunPredictionContext context, MerchantRelicEntry entry, MerchantInventory inventory)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		Player player = context.Player;
		RelicModel val = PredictionUtils.CreateRelic(entry.Model, player);
		RelicPickupPrediction.FastForwardRelicPickup(context, val);
		RelicRarity val2 = RelicFactory.RollRarity(context.Rng.Rewards);
		HashSet<RelicModel> blacklist = inventory.RelicEntries.Select(delegate(MerchantRelicEntry other)
		{
			RelicModel model = other.Model;
			return (model == null) ? null : model.CanonicalInstance;
		}).OfType<RelicModel>().ToHashSet();
		RelicModel val3 = context.RelicGrabBag.PullFromBack(val2, (Func<RelicModel, bool>)((RelicModel relic) => !blacklist.Contains(relic) && relic.IsAllowedInShops), context.RunState) ?? RelicFactory.FallbackRelic;
		int num = (int)Math.Round((float)val3.MerchantCost * context.Rng.Shops.NextFloat(0.85f, 1.15f));
		num = (int)HookMirrors.ModifyMerchantPrice(context.RunState, player, (MerchantEntry)(object)entry, num);
		num = (int)((AbstractModel)val).ModifyMerchantPrice(player, (MerchantEntry)(object)entry, (decimal)num);
		return new _003C_003Ez__ReadOnlyArray<IHoverTip>((IHoverTip[])(object)new IHoverTip[2]
		{
			(IHoverTip)(object)CreateTextTip(val3.Title.GetFormattedText(), num),
			PredictionHoverTipFactory.Relic(val3)
		});
	}

	private static HoverTip CreateTextTip(string modelName, int cost)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		string modelName2 = modelName;
		return PredictionHoverTipFactory.Text("merchant_restock", delegate(LocString description)
		{
			description.Add("Model", modelName2);
			description.Add("Cost", (decimal)cost);
		});
	}
}
