using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NSelectedHandCardHolder), "CreateHoverTips")]
internal static class CombatTransformPredictionSelectedHoverTipsPatch
{
	[HarmonyPriority(0)]
	private static void Postfix(NSelectedHandCardHolder __instance)
	{
		NHoverTipSet? obj = PredictionHoverTipSetHelper.EnsureHoverTipSet((Control)(object)__instance, (HoverTipAlignment)0);
		if (obj != null)
		{
			obj.SetAlignmentForCardHolder((NCardHolder)(object)__instance);
		}
	}
}
