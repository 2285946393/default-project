using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NTargetManager))]
internal static class CombatCardPredictionTargetManagerPatches
{
	[HarmonyPatch("StartTargeting", new Type[]
	{
		typeof(TargetType),
		typeof(Control),
		typeof(TargetMode),
		typeof(Func<bool>),
		typeof(Func<Node, bool>)
	})]
	[HarmonyPrefix]
	private static void ObservePredictionTargetsBeforeTargetingStarts(Control control)
	{
		try
		{
			CombatCardPredictionController.OnCardPlayTargetingStarting(control);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Error updating combat card prediction on targeting start: {ex}", 1);
			NCard val = (NCard)(object)((control is NCard) ? control : null);
			object context;
			if (val != null)
			{
				CardModel model = val.Model;
				if (model != null)
				{
					context = TelemetryContext.ForModel((AbstractModel)(object)model);
					goto IL_0061;
				}
			}
			context = null;
			goto IL_0061;
			IL_0061:
			ModTelemetry.CaptureException(ex, "combat_card_prediction_controller", "on_card_play_targeting_starting", context);
		}
	}
}
