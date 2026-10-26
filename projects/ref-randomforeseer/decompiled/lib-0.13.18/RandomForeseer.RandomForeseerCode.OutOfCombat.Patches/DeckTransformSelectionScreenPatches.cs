using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NDeckTransformSelectScreen))]
internal static class DeckTransformSelectionScreenPatches
{
	[HarmonyPatch("OnCardClicked")]
	[HarmonyPostfix]
	private static void RefreshHoverTips(NDeckTransformSelectScreen __instance, CardModel card)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		NGridCardHolder cardHolder = ((NCardGridSelectionScreen)__instance)._grid.GetCardHolder(card);
		if (cardHolder != null && ((NCardHolder)cardHolder)._isHovered)
		{
			NHoverTipSet.Remove((Control)(object)cardHolder);
			if (!((CanvasItem)__instance._previewContainer).Visible)
			{
				((GodotObject)cardHolder).Call(MethodName.CreateHoverTips, Array.Empty<Variant>());
			}
		}
	}

	[HarmonyPatch("OpenPreviewScreen")]
	[HarmonyPrefix]
	private static void ResetPredictor(NDeckTransformSelectScreen __instance)
	{
		(__instance._cardToTransformation.Target as DeckTransformPreviewPredictor)?.Reset();
	}
}
