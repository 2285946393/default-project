using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class AromaOfChaosPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(AromaOfChaos aromaOfChaos, EventOption option)
	{
		if (!(option.TextKey == "AROMA_OF_CHAOS.pages.INITIAL.options.LET_GO"))
		{
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(PredictLetGo(aromaOfChaos).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}

	private static IReadOnlyList<CardModel> PredictLetGo(AromaOfChaos aromaOfChaos)
	{
		return OutOfCombatPredictionUtils.PredictDistinctDeckTransformResults(((EventModel)aromaOfChaos).Owner, ((EventModel)aromaOfChaos).Rng);
	}
}
