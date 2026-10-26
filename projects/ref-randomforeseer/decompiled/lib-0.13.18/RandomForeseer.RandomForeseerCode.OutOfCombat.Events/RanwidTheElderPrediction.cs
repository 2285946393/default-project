using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class RanwidTheElderPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(RanwidTheElder ranwid, EventOption option)
	{
		Player owner = ((EventModel)ranwid).Owner;
		switch (option.TextKey)
		{
		case "RANWID_THE_ELDER.pages.INITIAL.options.POTION":
		case "RANWID_THE_ELDER.pages.INITIAL.options.GOLD":
			return OutOfCombatPredictionUtils.RelicTipsWithPickup(owner, OutOfCombatPredictionUtils.PredictRelicRewards(owner, 1));
		case "RANWID_THE_ELDER.pages.INITIAL.options.RELIC":
			return OutOfCombatPredictionUtils.RelicTipsWithPickup(owner, OutOfCombatPredictionUtils.PredictRelicRewards(owner, 2));
		default:
			return Array.Empty<IHoverTip>();
		}
	}
}
