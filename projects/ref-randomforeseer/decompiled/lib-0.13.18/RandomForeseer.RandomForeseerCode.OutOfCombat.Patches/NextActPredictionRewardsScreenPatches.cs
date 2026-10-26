using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NRewardsScreen))]
internal static class NextActPredictionRewardsScreenPatches
{
	[HarmonyPatch("ShowScreen")]
	[HarmonyPostfix]
	private static void ShowPrediction(bool isTerminal, IRunState runState)
	{
		try
		{
			NextActPrediction.ShowIfEligible(isTerminal, runState);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Next-Act prediction failed on the rewards screen: {ex}", 1);
			ModTelemetry.CaptureException(ex, "next_act_prediction", "show_on_rewards_screen");
		}
	}
}
