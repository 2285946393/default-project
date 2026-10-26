using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NTopBar))]
internal static class NextActPredictionTopBarPatches
{
	[HarmonyPatch("Initialize")]
	[HarmonyPostfix]
	private static void Initialize(NTopBar __instance)
	{
		try
		{
			NextActPrediction.Initialize(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Next-Act prediction failed to initialize: {ex}", 1);
			ModTelemetry.CaptureException(ex, "next_act_prediction", "initialize");
		}
	}
}
