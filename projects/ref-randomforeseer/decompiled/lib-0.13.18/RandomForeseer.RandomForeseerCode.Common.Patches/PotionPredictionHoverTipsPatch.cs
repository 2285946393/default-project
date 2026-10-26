using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace RandomForeseer.RandomForeseerCode.Common.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class PotionPredictionHoverTipsPatch
{
	private static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
	{
		if (!((AbstractModel)__instance).IsMutable || __instance == null)
		{
			return;
		}
		Player owner = __instance.Owner;
		if (owner != null && owner.RunState is RunState)
		{
			IReadOnlyList<IHoverTip> hoverTips = PotionPrediction.GetHoverTips(__instance);
			if (hoverTips.Count > 0)
			{
				__result = __result.Concat(hoverTips);
			}
		}
	}
}
