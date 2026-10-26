using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips.Patches;

[HarmonyPatch(typeof(NHoverTipCardContainer), "Add")]
internal static class PredictionCardDiscoveryScopePatch
{
	[HarmonyPrefix]
	[HarmonyPriority(800)]
	private static void Enter(CardHoverTip cardTip, out IDisposable? __state)
	{
		__state = PredictionCardDiscoveryScope.Enter(cardTip);
	}

	[HarmonyFinalizer]
	private static void Exit(IDisposable? __state)
	{
		__state?.Dispose();
	}
}
