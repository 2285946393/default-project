using System.Collections;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips.Patches;

[HarmonyPatch(typeof(NHoverTipCardContainer))]
internal static class PredictionCardHoverTipContainerPatches
{
	private static readonly Color DimmedCardModulate = new Color(0.6f, 0.6f, 0.6f, 1f);

	[HarmonyPatch("Add")]
	[HarmonyPrefix]
	private static bool AddPredictionCardBundleTip(NHoverTipCardContainer __instance, CardHoverTip cardTip)
	{
		if (!(cardTip is PredictionCardBundleHoverTip bundleTip))
		{
			return true;
		}
		PredictionCardHoverTipLayoutState.MarkPredictionCard(PredictionHoverTipControlFactory.CreateAndAddStack(__instance, bundleTip));
		return false;
	}

	[HarmonyPatch("Add")]
	[HarmonyPostfix]
	private static void MarkPredictionCardTip(NHoverTipCardContainer __instance, CardHoverTip cardTip)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (cardTip is PredictionCardHoverTip predictionCardHoverTip)
		{
			Control val = ((IEnumerable)((Node)__instance).GetChildren(false)).OfType<Control>().LastOrDefault();
			PredictionCardHoverTipLayoutState.MarkPredictionCard(val);
			if (predictionCardHoverTip.IsDimmed && val != null)
			{
				((CanvasItem)((Node)val).GetNode<NCard>(NodePath.op_Implicit("%Card"))).Modulate = DimmedCardModulate;
			}
		}
	}

	[HarmonyPatch("LayoutResizeAndReposition")]
	[HarmonyPrefix]
	private static bool LayoutPredictionCardTips(NHoverTipCardContainer __instance, Vector2 globalStartLocation, HoverTipAlignment alignment)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return !PredictionCardHoverTipLayout.TryLayoutPredictionCardTips(__instance, globalStartLocation, alignment);
	}
}
