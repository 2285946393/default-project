using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NPotionPopup))]
internal static class CombatPotionPredictionPopupPatches
{
	[HarmonyPatch("Create")]
	[HarmonyPrefix]
	private static void BeginActionPrediction(NPotionHolder holder)
	{
		try
		{
			CombatPotionPredictionController.OnPotionPopupOpen(holder);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on popup open: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "begin_action_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(holder));
		}
	}

	[HarmonyPatch("Remove")]
	[HarmonyPrefix]
	private static void EndActionPrediction(NPotionPopup __instance)
	{
		try
		{
			CombatPotionPredictionController.OnPotionPopupClose(__instance._holder);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on popup close: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "end_action_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(__instance));
		}
	}

	[HarmonyPatch("_ExitTree")]
	[HarmonyPrefix]
	private static void EndExitedPopupPrediction(NPotionPopup __instance)
	{
		try
		{
			CombatPotionPredictionController.OnPotionPopupClose(__instance._holder);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed on popup exit: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "end_exited_popup_prediction", CombatPotionPredictionTelemetry.GetTelemetryContext(__instance));
		}
	}
}
