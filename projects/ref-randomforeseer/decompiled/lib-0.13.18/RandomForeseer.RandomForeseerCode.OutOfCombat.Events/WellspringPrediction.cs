using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class WellspringPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Wellspring wellspring, EventOption option)
	{
		if (!(option.TextKey == "WELLSPRING.pages.INITIAL.options.BOTTLE"))
		{
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(OutOfCombatPredictionUtils.PredictUniformPotions(((EventModel)wellspring).Owner, 1).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
