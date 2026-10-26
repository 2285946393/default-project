using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class SymbiotePrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Symbiote symbiote, EventOption option)
	{
		if (!(option.TextKey == "SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE"))
		{
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(PredictKillWithFire(symbiote).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}

	private static IReadOnlyList<CardModel> PredictKillWithFire(Symbiote symbiote)
	{
		return OutOfCombatPredictionUtils.PredictDistinctDeckTransformResults(((EventModel)symbiote).Owner, ((EventModel)symbiote).Rng);
	}
}
