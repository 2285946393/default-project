using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class MorphicGrovePrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(MorphicGrove morphicGrove, EventOption option)
	{
		if (!(option.TextKey == "MORPHIC_GROVE.pages.INITIAL.options.GROUP"))
		{
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(PredictGroup(morphicGrove).ToPredictionCardBundleHoverTips(PredictionCardBundleKind.Transform));
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}

	private static IReadOnlyList<IReadOnlyList<CardModel>> PredictGroup(MorphicGrove morphicGrove)
	{
		return OutOfCombatPredictionUtils.PredictDistinctDeckTransformResultBundles(((EventModel)morphicGrove).Owner, ((EventModel)morphicGrove).Rng, 2);
	}
}
