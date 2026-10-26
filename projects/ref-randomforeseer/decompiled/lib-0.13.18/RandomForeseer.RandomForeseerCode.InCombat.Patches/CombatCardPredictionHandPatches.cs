using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NHandCardHolder))]
internal static class CombatCardPredictionHandPatches
{
	[HarmonyPatch("DoCardHoverEffects")]
	[HarmonyPrefix]
	private static void UpdatePredictionOnCardHover(NHandCardHolder __instance, bool isHovered)
	{
		try
		{
			CombatCardPredictionController.OnCardHover(__instance, isHovered);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Error updating combat card prediction on hover: {ex}", 1);
			object context;
			if (__instance != null)
			{
				CardModel cardModel = ((NCardHolder)__instance).CardModel;
				if (cardModel != null)
				{
					context = TelemetryContext.ForModel((AbstractModel)(object)cardModel);
					goto IL_005b;
				}
			}
			context = null;
			goto IL_005b;
			IL_005b:
			ModTelemetry.CaptureException(ex, "combat_card_prediction_controller", "on_card_hover", context);
		}
	}
}
