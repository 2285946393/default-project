using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class ThisOrThatPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(ThisOrThat thisOrThat, EventOption option)
	{
		if (!(option.TextKey == "THIS_OR_THAT.pages.INITIAL.options.ORNATE"))
		{
			return Array.Empty<IHoverTip>();
		}
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(((EventModel)thisOrThat).Owner, OutOfCombatPredictionUtils.PredictRelicRewards(((EventModel)thisOrThat).Owner, 1));
	}
}
