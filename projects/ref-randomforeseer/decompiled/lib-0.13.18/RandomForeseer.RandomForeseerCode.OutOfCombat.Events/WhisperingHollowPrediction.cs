using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class WhisperingHollowPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(WhisperingHollow whisperingHollow, EventOption option)
	{
		string textKey = option.TextKey;
		if (!(textKey == "WHISPERING_HOLLOW.pages.INITIAL.options.GOLD"))
		{
			if (textKey == "WHISPERING_HOLLOW.pages.INITIAL.options.HUG")
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.AddRange(PredictHug(whisperingHollow).ToPredictionHoverTips());
				return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
			}
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list2 = new List<IHoverTip>();
		list2.AddRange(PredictionUtils.PredictPotionRewards(((EventModel)whisperingHollow).Owner, 2, ((EventModel)whisperingHollow).Owner.PlayerRng.Rewards.Clone()).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
	}

	private static IReadOnlyList<CardModel> PredictHug(WhisperingHollow whisperingHollow)
	{
		return OutOfCombatPredictionUtils.PredictDistinctDeckTransformResults(((EventModel)whisperingHollow).Owner, ((EventModel)whisperingHollow).Rng);
	}
}
