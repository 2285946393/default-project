using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NCardPlay))]
internal static class CombatCardPredictionCardPlayPatches
{
	[HarmonyPatch("Cleanup")]
	[HarmonyPostfix]
	private static void CleanupPredictions(NCardPlay __instance)
	{
		try
		{
			CombatCardPredictionController.OnCardPlayCleanedUp(__instance.Holder);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Error updating combat card prediction on play cleanup: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_card_prediction_controller", "on_card_play_cleaned_up");
		}
	}
}
