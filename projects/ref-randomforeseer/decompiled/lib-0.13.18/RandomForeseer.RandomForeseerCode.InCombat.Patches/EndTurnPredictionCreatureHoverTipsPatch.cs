using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class EndTurnPredictionCreatureHoverTipsPatch
{
	private static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
	{
		IReadOnlyList<IHoverTip> hoverTips = EndTurnPredictionCreatureHoverTips.GetHoverTips(__instance);
		if (hoverTips.Count > 0)
		{
			__result = __result.Concat(hoverTips);
		}
	}
}
