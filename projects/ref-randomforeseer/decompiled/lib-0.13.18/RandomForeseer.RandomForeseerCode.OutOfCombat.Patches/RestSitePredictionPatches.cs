using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NRestSiteButton))]
internal static class RestSitePredictionPatches
{
	[HarmonyPatch("OnFocus")]
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void OnFocusPostfix(NRestSiteButton __instance)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		PredictionHoverTipSetHelper.EnsureHoverTipSet((Control)(object)__instance, HoverTip.GetHoverTipAlignment((Control)(object)__instance, 0.75f));
	}

	[HarmonyPatch("OnUnfocus")]
	[HarmonyPostfix]
	private static void OnUnfocusPostfix(NRestSiteButton __instance)
	{
		PredictionHoverTipSetHelper.RemoveOwnedHoverTipSet((Control)(object)__instance);
	}
}
