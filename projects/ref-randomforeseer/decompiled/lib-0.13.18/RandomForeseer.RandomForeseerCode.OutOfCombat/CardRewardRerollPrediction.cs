using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class CardRewardRerollPrediction
{
	private static readonly FieldRef<CardReward, Action?> AfterGeneratedField = AccessTools.FieldRefAccess<CardReward, Action>("AfterGenerated");

	public static IReadOnlyList<IHoverTip> GetHoverTips(CardReward reward)
	{
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.DriftwoodRerollPredictionEnabled || !reward.CanReroll)
		{
			return Array.Empty<IHoverTip>();
		}
		IReadOnlyList<CardModel> cards = CardRewardPrediction.PredictCards(((Reward)reward).Player, reward.OptionCount, CardRewardPrediction.CloneOptions(reward.RerollOptions), AfterGeneratedField.Invoke(reward));
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(cards.ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
