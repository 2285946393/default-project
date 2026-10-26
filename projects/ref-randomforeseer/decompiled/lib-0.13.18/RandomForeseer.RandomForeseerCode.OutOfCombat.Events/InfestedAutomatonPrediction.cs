using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class InfestedAutomatonPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(InfestedAutomaton infestedAutomaton, EventOption option)
	{
		Player owner = ((EventModel)infestedAutomaton).Owner;
		string textKey = option.TextKey;
		if (!(textKey == "INFESTED_AUTOMATON.pages.INITIAL.options.STUDY"))
		{
			if (textKey == "INFESTED_AUTOMATON.pages.INITIAL.options.TOUCH_CORE")
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.AddRange(CardRewardPrediction.PredictCards(owner, 1, CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(owner.Character.CardPool), (Func<CardModel, bool>)delegate(CardModel card)
				{
					CardEnergyCost energyCost = card.EnergyCost;
					return energyCost != null && energyCost.Canonical == 0 && !energyCost.CostsX;
				}).WithFlags((CardCreationFlags)16)).ToPredictionHoverTips());
				return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
			}
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list2 = new List<IHoverTip>();
		list2.AddRange(CardRewardPrediction.PredictCards(owner, 1, CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(owner.Character.CardPool), (Func<CardModel, bool>)((CardModel card) => (int)card.Type == 3))).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
	}
}
