using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class EventOptionPredictionHoverTipsPatch
{
	private static void Postfix(EventOption __instance, ref IEnumerable<IHoverTip> __result)
	{
		__result = __result.Where((IHoverTip tip) => !tip.IsPredictionHoverTip());
		IReadOnlyList<IHoverTip> hoverTips = EventOptionPrediction.GetHoverTips(__instance);
		if (hoverTips.Count > 0)
		{
			__result = __result.Concat(hoverTips);
		}
	}
}
