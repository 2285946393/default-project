using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class BattlewornDummyPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(BattlewornDummy battlewornDummy, EventOption option)
	{
		if (!ModData.Settings.Allows(PredictionFairness.UnfairInAllModes))
		{
			return Array.Empty<IHoverTip>();
		}
		Player owner = ((EventModel)battlewornDummy).Owner;
		switch (option.TextKey)
		{
		case "BATTLEWORN_DUMMY.pages.INITIAL.options.SETTING_1":
		{
			List<IHoverTip> list2 = new List<IHoverTip>();
			list2.AddRange(OutOfCombatPredictionUtils.PredictUniformPotions(owner, 1).ToPredictionHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
		}
		case "BATTLEWORN_DUMMY.pages.INITIAL.options.SETTING_2":
		{
			List<IHoverTip> list = new List<IHoverTip>();
			list.AddRange(PredictSetting2(battlewornDummy).ToPredictionHoverTips());
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
		}
		case "BATTLEWORN_DUMMY.pages.INITIAL.options.SETTING_3":
			return OutOfCombatPredictionUtils.RelicTipsWithPickup(owner, OutOfCombatPredictionUtils.PredictRelicRewards(owner, 1));
		default:
			return Array.Empty<IHoverTip>();
		}
	}

	private static IReadOnlyList<CardModel> PredictSetting2(BattlewornDummy battlewornDummy)
	{
		Player owner = ((EventModel)battlewornDummy).Owner;
		Rng val = ((EventModel)battlewornDummy).Rng.Clone();
		RunPredictionContext runPredictionContext = new RunPredictionContext(owner);
		runPredictionContext.SharedRng.Niche.NextInt(1);
		CombatEndEffectPrediction.FastForwardMonsterRoomCombatEndHooks(runPredictionContext);
		return (from card in ListExtensions.StableShuffle<PredictedCard>(runPredictionContext.Deck.Cards.Where((PredictedCard card) => card.Preview.IsUpgradable).ToList(), val).Take(2)
			select card.Upgrade().Preview).ToList();
	}
}
