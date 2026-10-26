using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NEndTurnButton))]
internal static class EndTurnPredictionButtonPatches
{
	[HarmonyPatch("OnFocus")]
	[HarmonyPostfix]
	private static void OnFocus(NEndTurnButton __instance)
	{
		EndTurnPredictionController.OnEndTurnButtonFocused(__instance);
	}

	[HarmonyPatch("OnUnfocus")]
	[HarmonyPostfix]
	private static void OnUnfocus()
	{
		EndTurnPredictionController.OnEndTurnButtonUnfocused();
	}
}
