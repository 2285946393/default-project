using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class PotionPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(PotionModel potion)
	{
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.PotionPredictionEnabled)
		{
			return Array.Empty<IHoverTip>();
		}
		if (CombatManager.Instance.IsInProgress && potion.Owner.Creature.CombatState != null)
		{
			return CombatPotionPrediction.GetHoverTips(potion);
		}
		try
		{
			return GetOutOfCombatHoverTips(potion, potion.Owner);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Out-of-combat potion prediction failed for {((AbstractModel)potion).Id}: {ex}", 1);
			ModTelemetry.CaptureException(ex, "potion_prediction", "build_out_of_combat_hover_tips", TelemetryContext.ForModel((AbstractModel)(object)potion));
			return Array.Empty<IHoverTip>();
		}
	}

	public static IReadOnlyList<IHoverTip> GetHoverTips(Player player, PotionModel potion)
	{
		return GetHoverTips(PredictionUtils.CreatePotion(potion, player));
	}

	private static List<IHoverTip> GetOutOfCombatHoverTips(PotionModel potion, Player target)
	{
		List<IHoverTip> list = new List<IHoverTip>();
		ModSettings settings = ModData.Settings;
		if (settings.PotionCardGenerationPredictionEnabled && settings.Allows(PredictionFairness.UnfairInAllModes))
		{
			Rng rng = target.RunState.Rng.CombatCardGeneration.Clone();
			PotionCardGenerationResult potionCardGenerationResult = CardGenerationPotionMirrors.Generate(potion, target, rng);
			if ((object)potionCardGenerationResult != null)
			{
				list.AddRange(potionCardGenerationResult.Cards.SelectPreviews().ToPredictionHoverTips());
			}
		}
		if (settings.PotionGenerationPredictionEnabled && potion is EntropicBrew)
		{
			Rng rng2 = target.RunState.Rng.CombatPotionGeneration.Clone();
			list.AddRange(EntropicBrewMirrors.Generate(target, rng2).ToPredictionHoverTips());
		}
		return list;
	}
}
