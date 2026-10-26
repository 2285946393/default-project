using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NHandCardHolder))]
internal static class CombatPredictionCardHighlightPatches
{
	[HarmonyPatch("UpdateCard")]
	[HarmonyPostfix]
	private static void ShowHighlightAfterCardUpdate(NHandCardHolder __instance)
	{
		try
		{
			CombatPredictionCardHighlight.ApplyHighlightToHolder(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat card highlight failed on update: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_card_highlight", "update_card");
		}
	}
}
