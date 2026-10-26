using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class RoomFullOfCheesePrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(RoomFullOfCheese roomFullOfCheese, EventOption option)
	{
		if (option.TextKey != "ROOM_FULL_OF_CHEESE.pages.INITIAL.options.GORGE")
		{
			return Array.Empty<IHoverTip>();
		}
		Player owner = ((EventModel)roomFullOfCheese).Owner;
		CardCreationOptions options = CardCreationOptions.ForNonCombatWithUniformOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(owner.Character.CardPool), (Func<CardModel, bool>)((CardModel card) => (int)card.Rarity == 2)).WithFlags((CardCreationFlags)1);
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(CardRewardPrediction.PredictCards(owner, 8, options).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
