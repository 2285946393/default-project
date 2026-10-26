using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class WelcomeToWongosPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(WelcomeToWongos welcomeToWongos, EventOption option)
	{
		Player owner = ((EventModel)welcomeToWongos).Owner;
		string textKey = option.TextKey;
		if (!(textKey == "WELCOME_TO_WONGOS.pages.INITIAL.options.BARGAIN_BIN"))
		{
			if (textKey == "WELCOME_TO_WONGOS.pages.INITIAL.options.LEAVE")
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.AddRange(OutOfCombatPredictionUtils.PredictDowngradedDeckCardsByNextItem(owner, 1, (CardModel card) => card.IsUpgraded, ((EventModel)welcomeToWongos).Rng.Clone()).ToPredictionHoverTips());
				return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
			}
			return Array.Empty<IHoverTip>();
		}
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(owner, OutOfCombatPredictionUtils.PredictRelicRewards(owner, new _003C_003Ez__ReadOnlySingleElementList<RelicRarity>((RelicRarity)2), (RelicModel relic) => relic.IsAllowedInShops));
	}
}
