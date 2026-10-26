using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class LuminousChoirPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(LuminousChoir luminousChoir, EventOption option)
	{
		if (!(option.TextKey == "LUMINOUS_CHOIR.pages.INITIAL.options.OFFER_TRIBUTE"))
		{
			return Array.Empty<IHoverTip>();
		}
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(((EventModel)luminousChoir).Owner, OutOfCombatPredictionUtils.PredictRelicRewards(((EventModel)luminousChoir).Owner, 1));
	}
}
