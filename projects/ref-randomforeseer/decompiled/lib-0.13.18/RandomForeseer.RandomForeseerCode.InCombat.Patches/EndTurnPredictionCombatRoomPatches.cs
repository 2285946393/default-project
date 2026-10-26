using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NCombatRoom))]
internal static class EndTurnPredictionCombatRoomPatches
{
	[HarmonyPatch("_EnterTree")]
	[HarmonyPostfix]
	private static void Subscribe()
	{
		EndTurnPredictionController.Subscribe();
	}

	[HarmonyPatch("_ExitTree")]
	[HarmonyPostfix]
	private static void Unsubscribe()
	{
		EndTurnPredictionController.Unsubscribe();
	}
}
