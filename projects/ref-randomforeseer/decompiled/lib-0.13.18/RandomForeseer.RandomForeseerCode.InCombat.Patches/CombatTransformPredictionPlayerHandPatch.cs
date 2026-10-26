using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NPlayerHand))]
internal static class CombatTransformPredictionPlayerHandPatch
{
	[HarmonyPatch("SelectCards")]
	[HarmonyPrefix]
	private static void BeginSession(NPlayerHand __instance, CardSelectorPrefs prefs, AbstractModel? source)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CombatTransformPrediction.BeginSession(__instance, prefs, source);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat transform prediction failed to begin session: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_transform_prediction", "begin_session", (source != null) ? TelemetryContext.ForModel(source) : null);
		}
	}

	[HarmonyPatch("AfterCardsSelected")]
	[HarmonyPrefix]
	private static void EndSession(NPlayerHand __instance)
	{
		try
		{
			CombatTransformPrediction.EndSession(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat transform prediction failed to end session: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_transform_prediction", "end_session");
		}
	}

	[HarmonyPatch("_ExitTree")]
	[HarmonyPrefix]
	private static void CleanupSession(NPlayerHand __instance)
	{
		try
		{
			CombatTransformPrediction.EndSession(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat transform prediction failed to cleanup session: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_transform_prediction", "cleanup_session");
		}
	}
}
