using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class DoorsOfLightAndDarkPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(DoorsOfLightAndDark doors, EventOption option)
	{
		if (!(option.TextKey == "DOORS_OF_LIGHT_AND_DARK.pages.INITIAL.options.LIGHT"))
		{
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(OutOfCombatPredictionUtils.PredictUpgradedDeckCards(((EventModel)doors).Owner, 2, (CardModel card) => card.IsUpgradable, ((EventModel)doors).Rng.Clone()).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
