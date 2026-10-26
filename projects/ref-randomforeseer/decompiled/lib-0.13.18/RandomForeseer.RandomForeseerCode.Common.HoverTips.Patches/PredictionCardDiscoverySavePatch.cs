using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips.Patches;

[HarmonyPatch(typeof(SaveManager), "MarkCardAsSeen")]
internal static class PredictionCardDiscoverySavePatch
{
	[HarmonyPrefix]
	private static bool SkipPredictionCard(CardModel card)
	{
		return !PredictionCardDiscoveryScope.Contains(card);
	}
}
