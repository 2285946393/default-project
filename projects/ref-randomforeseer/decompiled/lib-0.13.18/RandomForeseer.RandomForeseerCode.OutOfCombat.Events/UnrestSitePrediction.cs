using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class UnrestSitePrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(UnrestSite unrestSite, EventOption option)
	{
		if (!(option.TextKey == "UNREST_SITE.pages.INITIAL.options.KILL"))
		{
			return Array.Empty<IHoverTip>();
		}
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(((EventModel)unrestSite).Owner, OutOfCombatPredictionUtils.PredictRelicRewards(((EventModel)unrestSite).Owner, 1));
	}
}
