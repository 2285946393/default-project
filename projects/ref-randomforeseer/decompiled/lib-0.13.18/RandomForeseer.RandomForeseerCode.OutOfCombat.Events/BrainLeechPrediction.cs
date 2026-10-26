using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class BrainLeechPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(BrainLeech brainLeech, EventOption option)
	{
		Player owner = ((EventModel)brainLeech).Owner;
		string textKey = option.TextKey;
		if (!(textKey == "BRAIN_LEECH.pages.INITIAL.options.SHARE_KNOWLEDGE"))
		{
			if (textKey == "BRAIN_LEECH.pages.INITIAL.options.RIP")
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.AddRange(OutOfCombatPredictionUtils.PredictCardRewardBundles(owner, ((EventModel)brainLeech).DynamicVars["RewardCount"].IntValue, 3, () => CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>((CardPoolModel)(object)ModelDb.CardPool<ColorlessCardPool>()), (Func<CardModel, bool>)null).WithFlags((CardCreationFlags)17)).ToPredictionCardBundleHoverTips());
				return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
			}
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list2 = new List<IHoverTip>();
		list2.AddRange(CardRewardPrediction.PredictCards(owner, ((EventModel)brainLeech).DynamicVars["FromCardChoiceCount"].IntValue, CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(owner.Character.CardPool), (Func<CardModel, bool>)null)).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
	}
}
