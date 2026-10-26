using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NTargetManager))]
internal static class NTargetManagerPatches
{
	[HarmonyPatch("FinishTargeting")]
	[HarmonyPrefix]
	private static void OnTargetingFinishing(NTargetManager __instance)
	{
		try
		{
			CombatPredictionTargetObserver.OnTargetingFinishing(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat prediction target observer failed on targeting finishing: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_prediction_target_observer", "on_targeting_finishing");
		}
	}
}
