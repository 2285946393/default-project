using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class PaelsWingSacrificePrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(CardReward reward, CardRewardAlternative alternative)
	{
		ModSettings settings = ModData.Settings;
		if (ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.PaelsWingSacrificePredictionEnabled)
		{
			object? target = alternative.OnSelect.Target;
			PaelsWing val = (PaelsWing)((target is PaelsWing) ? target : null);
			if (val != null && (val.RewardsSacrificed + 1) % ((RelicModel)val).DynamicVars["Sacrifices"].IntValue == 0)
			{
				return OutOfCombatPredictionUtils.RelicTipsWithPickup(((Reward)reward).Player, OutOfCombatPredictionUtils.PredictRelicRewards(((Reward)reward).Player, 1));
			}
		}
		return Array.Empty<IHoverTip>();
	}
}
