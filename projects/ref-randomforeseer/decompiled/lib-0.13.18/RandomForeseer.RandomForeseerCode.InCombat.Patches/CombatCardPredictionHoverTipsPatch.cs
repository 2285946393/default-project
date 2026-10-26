using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class CombatCardPredictionHoverTipsPatch
{
	private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		IReadOnlyList<IHoverTip> hoverTips = CombatCardPrediction.GetHoverTips(__instance);
		if (hoverTips.Count > 0)
		{
			__result = __result.Concat(hoverTips);
		}
	}
}
