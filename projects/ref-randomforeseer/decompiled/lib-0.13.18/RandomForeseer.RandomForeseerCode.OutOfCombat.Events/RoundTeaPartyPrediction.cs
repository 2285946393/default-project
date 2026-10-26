using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class RoundTeaPartyPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(RoundTeaParty roundTeaParty, EventOption option)
	{
		string textKey = option.TextKey;
		if ((!(textKey == "ROUND_TEA_PARTY.pages.INITIAL.options.PICK_FIGHT") && !(textKey == "ROUND_TEA_PARTY.pages.PICK_FIGHT.options.CONTINUE_FIGHT")) || 1 == 0)
		{
			return Array.Empty<IHoverTip>();
		}
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(((EventModel)roundTeaParty).Owner, OutOfCombatPredictionUtils.PredictRelicRewards(((EventModel)roundTeaParty).Owner, 1));
	}
}
