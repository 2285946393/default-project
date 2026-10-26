using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class EndlessConveyorPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(EndlessConveyor endlessConveyor, EventOption option)
	{
		Player owner = ((EventModel)endlessConveyor).Owner;
		switch (option.TextKey)
		{
		case "ENDLESS_CONVEYOR.pages.INITIAL.options.OBSERVE_CHEF":
		{
			List<IHoverTip> list2 = new List<IHoverTip>();
			list2.AddRange(OutOfCombatPredictionUtils.PredictUpgradedDeckCardsByNextItem(owner, 1, (CardModel card) => card.IsUpgradable, ((EventModel)endlessConveyor).Rng.Clone()).ToPredictionHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
		}
		case "ENDLESS_CONVEYOR.pages.ALL.options.FRIED_EEL":
		{
			List<IHoverTip> list3 = new List<IHoverTip>();
			list3.AddRange(CardRewardPrediction.PredictCards(owner, 1, CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>((CardPoolModel)(object)ModelDb.CardPool<ColorlessCardPool>()), (Func<CardModel, bool>)null)).ToPredictionHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list3);
		}
		case "ENDLESS_CONVEYOR.pages.ALL.options.JELLY_LIVER":
		{
			List<IHoverTip> list5 = new List<IHoverTip>();
			list5.AddRange(PredictJellyLiver(endlessConveyor).ToPredictionHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list5);
		}
		case "ENDLESS_CONVEYOR.pages.ALL.options.SUSPICIOUS_CONDIMENT":
		{
			List<IHoverTip> list4 = new List<IHoverTip>();
			list4.AddRange(OutOfCombatPredictionUtils.PredictUniformPotions(owner, 1).ToPredictionHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list4);
		}
		case "ENDLESS_CONVEYOR.pages.ALL.options.SPICY_SNAPPY":
		{
			List<IHoverTip> list = new List<IHoverTip>();
			list.AddRange(OutOfCombatPredictionUtils.PredictUpgradedDeckCardsByNextItem(owner, 1, (CardModel card) => card.IsUpgradable, ((EventModel)endlessConveyor).Rng.Clone()).ToPredictionHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
		}
		default:
			return Array.Empty<IHoverTip>();
		}
	}

	private static IReadOnlyList<CardModel> PredictJellyLiver(EndlessConveyor endlessConveyor)
	{
		return OutOfCombatPredictionUtils.PredictDistinctDeckTransformResults(((EventModel)endlessConveyor).Owner, ((EventModel)endlessConveyor).Rng);
	}
}
