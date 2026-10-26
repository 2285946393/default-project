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
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class SlipperyBridgePrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(SlipperyBridge slipperyBridge, EventOption option)
	{
		if (!option.TextKey.Contains("HOLD_ON", StringComparison.Ordinal))
		{
			return Array.Empty<IHoverTip>();
		}
		Player owner = ((EventModel)slipperyBridge).Owner;
		Rng val = ((EventModel)slipperyBridge).Rng.Clone();
		CardModel current = slipperyBridge._randomCardToLose;
		HashSet<CardModel> skipped = slipperyBridge._skippedRemovals?.ToHashSet() ?? new HashSet<CardModel>();
		List<CardModel> list = new List<CardModel>();
		int slipperyBridgeRerollPreviewCount = ModData.Settings.SlipperyBridgeRerollPreviewCount;
		for (int i = 0; i < slipperyBridgeRerollPreviewCount; i++)
		{
			if (current != null)
			{
				skipped.Add(current);
			}
			List<CardModel> list2 = ((current == null) ? owner.Deck.Cards.Where((CardModel card) => (int)card.Rarity != 1).ToList() : owner.Deck.Cards.Where((CardModel card) => ((object)card).GetType() != ((object)current).GetType()).ToList());
			list2.RemoveAll((CardModel card) => !card.IsRemovable || skipped.Contains(card));
			if (list2.Count == 0)
			{
				list2 = owner.Deck.Cards.Where((CardModel card) => card.IsRemovable).ToList();
			}
			current = val.NextItem<CardModel>((IEnumerable<CardModel>)list2);
			if (current == null)
			{
				break;
			}
			list.Add(current);
		}
		List<IHoverTip> list3 = new List<IHoverTip>();
		list3.AddRange(list.ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list3);
	}
}
