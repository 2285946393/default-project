using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class PotionCourierPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(PotionCourier potionCourier, EventOption option)
	{
		Player owner = ((EventModel)potionCourier).Owner;
		if (!(option.TextKey == "POTION_COURIER.pages.INITIAL.options.RANSACK"))
		{
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(OutOfCombatPredictionUtils.PredictUniformPotions(owner, 1, null, (PotionModel potion) => (int)potion.Rarity == 2).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
