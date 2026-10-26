using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class TabletOfTruthPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(TabletOfTruth tabletOfTruth, EventOption option)
	{
		int? randomUpgradeCountBeforeFinalAllUpgrade = GetRandomUpgradeCountBeforeFinalAllUpgrade(option);
		if (!(randomUpgradeCountBeforeFinalAllUpgrade > 0))
		{
			return Array.Empty<IHoverTip>();
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(OutOfCombatPredictionUtils.PredictUpgradedDeckCardsByNextItem(((EventModel)tabletOfTruth).Owner, randomUpgradeCountBeforeFinalAllUpgrade.Value, (CardModel card) => card.IsUpgradable, ((EventModel)tabletOfTruth).Rng.Clone()).ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}

	private static int? GetRandomUpgradeCountBeforeFinalAllUpgrade(EventOption option)
	{
		return option.TextKey switch
		{
			"TABLET_OF_TRUTH.pages.INITIAL.options.DECIPHER_1" => 4, 
			"TABLET_OF_TRUTH.pages.DECIPHER_1.options.DECIPHER" => 3, 
			"TABLET_OF_TRUTH.pages.DECIPHER_2.options.DECIPHER" => 2, 
			"TABLET_OF_TRUTH.pages.DECIPHER_3.options.DECIPHER" => 1, 
			_ => null, 
		};
	}
}
