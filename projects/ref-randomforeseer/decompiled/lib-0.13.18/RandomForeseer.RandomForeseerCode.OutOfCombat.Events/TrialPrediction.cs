using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class TrialPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Trial trial, EventOption option)
	{
		Player player = ((EventModel)trial).Owner;
		switch (option.TextKey)
		{
		case "TRIAL.pages.MERCHANT.options.GUILTY":
			return OutOfCombatPredictionUtils.RelicTipsWithPickup(player, OutOfCombatPredictionUtils.PredictRelicRewards(player, 2));
		case "TRIAL.pages.NONDESCRIPT.options.GUILTY":
		{
			List<IHoverTip> list2 = new List<IHoverTip>();
			list2.AddRange(OutOfCombatPredictionUtils.PredictCardRewardBundles(player, 2, 3, () => CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(player.Character.CardPool), (Func<CardModel, bool>)null)).ToPredictionCardBundleHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
		}
		case "TRIAL.pages.NONDESCRIPT.options.INNOCENT":
		{
			List<IHoverTip> list = new List<IHoverTip>();
			list.AddRange(PredictNondescriptInnocent(trial).ToPredictionCardBundleHoverTips(PredictionCardBundleKind.Transform));
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
		}
		default:
			return Array.Empty<IHoverTip>();
		}
	}

	private static IReadOnlyList<IReadOnlyList<CardModel>> PredictNondescriptInnocent(Trial trial)
	{
		Player owner = ((EventModel)trial).Owner;
		CardModel item = PredictionUtils.CreateCard((CardModel)(object)ModelDb.Card<Doubt>(), owner);
		return OutOfCombatPredictionUtils.PredictDistinctDeckTransformResultBundles(owner, ((EventModel)trial).Rng, 2, new _003C_003Ez__ReadOnlySingleElementList<CardModel>(item));
	}
}
