using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class WarHistorianRepyPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(WarHistorianRepy warHistorianRepy, EventOption option)
	{
		if (option.TextKey != "WAR_HISTORIAN_REPY.pages.INITIAL.options.UNLOCK_CHEST")
		{
			return Array.Empty<IHoverTip>();
		}
		Player owner = ((EventModel)warHistorianRepy).Owner;
		Rng rng = owner.PlayerRng.Rewards.Clone();
		IReadOnlyList<PotionModel> potions = PredictionUtils.PredictPotionRewards(owner, 2, rng);
		IReadOnlyList<RelicModel> relics = OutOfCombatPredictionUtils.PredictRelicRewards(owner, 2, rng);
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(potions.ToPredictionHoverTips());
		list.AddRange(OutOfCombatPredictionUtils.RelicTipsWithPickup(owner, relics));
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
