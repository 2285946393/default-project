using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips.Patches;

[HarmonyPatch(typeof(NHoverTipSet))]
internal static class PredictionCardHoverTipSetAlignmentPatches
{
	[HarmonyPatch("SetAlignmentForCardHolder")]
	[HarmonyPrefix]
	private static void RecordCardHolderSourceRect(NHoverTipSet __instance, NCardHolder holder)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		NHoverTipCardContainer cardHoverTipContainer = __instance._cardHoverTipContainer;
		if (ShouldRecordSourceRect(cardHoverTipContainer))
		{
			PredictionCardHoverTipLayoutState.RecordSourceRect(cardHoverTipContainer, ((Control)holder.Hitbox).GetGlobalRect(), HoverTip.GetHoverTipAlignment((Control)(object)holder, 0.75f), 10f);
		}
	}

	[HarmonyPatch("SetAlignmentForCardHolder")]
	[HarmonyPostfix]
	private static void ApplyCardHolderFallbackLayout(NHoverTipSet __instance)
	{
		PredictionCardHoverTipLayout.ApplyFallbackLayoutIfStillOverflowing(__instance);
	}

	[HarmonyPatch("SetAlignmentForRelic")]
	[HarmonyPrefix]
	private static void RecordRelicSourceRect(NHoverTipSet __instance, NRelic relic)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		NHoverTipCardContainer cardHoverTipContainer = __instance._cardHoverTipContainer;
		if (ShouldRecordSourceRect(cardHoverTipContainer))
		{
			PredictionCardHoverTipLayoutState.RecordSourceRect(cardHoverTipContainer, ((Control)relic.Icon).GetGlobalRect(), HoverTip.GetHoverTipAlignment((Control)(object)relic, 0.75f));
		}
	}

	[HarmonyPatch("SetAlignmentForRelic")]
	[HarmonyPostfix]
	private static void ApplyRelicFallbackLayout(NHoverTipSet __instance)
	{
		PredictionCardHoverTipLayout.ApplyFallbackLayoutIfStillOverflowing(__instance);
	}

	[HarmonyPatch("SetAlignment")]
	[HarmonyPrefix]
	private static void RecordControlSourceRect(NHoverTipSet __instance, Control node, ref HoverTipAlignment alignment)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		NHoverTipCardContainer cardHoverTipContainer = __instance._cardHoverTipContainer;
		if (ShouldRecordSourceRect(cardHoverTipContainer))
		{
			if ((int)alignment == 0)
			{
				alignment = (HoverTipAlignment)3;
			}
			PredictionCardHoverTipLayoutState.RecordSourceRect(cardHoverTipContainer, node.GetGlobalRect(), alignment);
		}
	}

	[HarmonyPatch("SetAlignment")]
	[HarmonyPostfix]
	private static void ApplyControlFallbackLayout(NHoverTipSet __instance)
	{
		PredictionCardHoverTipLayout.ApplyFallbackLayoutIfStillOverflowing(__instance);
	}

	private static bool ShouldRecordSourceRect(NHoverTipCardContainer? container)
	{
		if (container != null)
		{
			return PredictionCardHoverTipLayoutState.HasPredictionCard(container);
		}
		return false;
	}
}
