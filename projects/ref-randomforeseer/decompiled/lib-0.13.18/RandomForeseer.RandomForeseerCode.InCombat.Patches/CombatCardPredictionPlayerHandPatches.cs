using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NPlayerHand))]
internal static class CombatCardPredictionPlayerHandPatches
{
	[HarmonyPatch("StartCardPlay")]
	[HarmonyPrefix]
	private static void UpdatePredictionsOnCardPlayStarted(NHandCardHolder holder)
	{
		try
		{
			CombatCardPredictionController.OnCardPlayStarted(holder);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Error updating combat card prediction on play start: {ex}", 1);
			object context;
			if (holder != null)
			{
				CardModel cardModel = ((NCardHolder)holder).CardModel;
				if (cardModel != null)
				{
					context = TelemetryContext.ForModel((AbstractModel)(object)cardModel);
					goto IL_005a;
				}
			}
			context = null;
			goto IL_005a;
			IL_005a:
			ModTelemetry.CaptureException(ex, "combat_card_prediction_controller", "on_card_play_started", context);
		}
	}
}
