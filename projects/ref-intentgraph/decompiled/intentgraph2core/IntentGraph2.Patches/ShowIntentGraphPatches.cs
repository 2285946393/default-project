using HarmonyLib;
using IntentGraph2.Utils;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IntentGraph2.Patches;

public class ShowIntentGraphPatches
{
	[HarmonyPatch(typeof(NCreature), "OnFocus")]
	public static class OnFocusPatch
	{
		public static void Postfix(NCreature __instance)
		{
			IntentGraphHost.Create(__instance);
		}
	}

	[HarmonyPatch(typeof(NCreature), "OnUnfocus")]
	public static class OnUnfocusPatch
	{
		public static void Prefix(NCreature __instance)
		{
			if (!IntentGraphMod.Config.PinableIntentGraph)
			{
				IntentGraphHost.Remove(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(NCreature), "_ExitTree")]
	public static class ExitTreePatch
	{
		public static void Prefix(NCreature __instance)
		{
			IntentGraphHost.Remove(__instance);
		}
	}
}
