using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NPotionHolder))]
internal static class CombatPotionPredictionHolderPatches
{
	[HarmonyPatch("OnFocus")]
	[HarmonyPrefix]
	private static void BeginHoverPrediction(NPotionHolder __instance)
	{
		try
		{
			CombatPotionPredictionController.OnPotionFocus(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on focus: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "begin_hover_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(__instance));
		}
	}

	[HarmonyPatch("OnUnfocus")]
	[HarmonyPrefix]
	private static void EndHoverPrediction(NPotionHolder __instance)
	{
		try
		{
			CombatPotionPredictionController.OnPotionUnfocus(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on unfocus: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "end_hover_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(__instance));
		}
	}

	[HarmonyPatch("TargetNode")]
	[HarmonyPrefix]
	private static void BeginTargetPrediction(NPotionHolder __instance)
	{
		try
		{
			CombatPotionPredictionController.OnPotionTargetingStart(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on targeting start: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "begin_target_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(__instance));
		}
	}

	[HarmonyPatch("RemoveUsedPotion")]
	[HarmonyPrefix]
	private static void EndUsedPotionPrediction(NPotionHolder __instance)
	{
		try
		{
			CombatPotionPredictionController.OnPotionRemoved(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on used potion removal: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "end_used_potion_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(__instance));
		}
	}

	[HarmonyPatch("DiscardPotion")]
	[HarmonyPrefix]
	private static void EndDiscardedPotionPrediction(NPotionHolder __instance)
	{
		try
		{
			CombatPotionPredictionController.OnPotionRemoved(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on discarded potion removal: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "end_discarded_potion_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(__instance));
		}
	}
}
