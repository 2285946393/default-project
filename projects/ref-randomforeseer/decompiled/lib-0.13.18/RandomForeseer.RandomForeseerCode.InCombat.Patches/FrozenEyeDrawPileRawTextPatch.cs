using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.Localization;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(LocString), "GetRawText")]
internal static class FrozenEyeDrawPileRawTextPatch
{
	private static void Postfix(LocString __instance, ref string __result)
	{
		try
		{
			string locTable = __instance.LocTable;
			string locEntryKey = __instance.LocEntryKey;
			string text;
			if (!(locTable == "static_hover_tips"))
			{
				if (!(locTable == "gameplay_ui") || !(locEntryKey == "DRAW_PILE_INFO"))
				{
					goto IL_005c;
				}
				text = ReplaceDrawPileInfo(__result);
			}
			else
			{
				if (!(locEntryKey == "DRAW_PILE.description"))
				{
					goto IL_005c;
				}
				text = ReplaceDrawPileDescription(__result);
			}
			goto IL_005f;
			IL_005c:
			text = __result;
			goto IL_005f;
			IL_005f:
			__result = text;
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Failed to replace raw text for {__instance}: {ex}", 1);
			ModTelemetry.CaptureException(ex, "frozen_eye_patch", "replace_draw_pile_text", new { __instance.LocTable, __instance.LocEntryKey });
		}
	}

	private static string ReplaceDrawPileDescription(string text)
	{
		ModSettings settings = ModData.Settings;
		if (settings == null || !ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.FrozenEyeEnabled)
		{
			return text;
		}
		string obj = text.Split("\n\n", 2)[0];
		string rawText = ModLocalization.Text("frozen_eye.draw_pile_hover_view").GetRawText();
		return obj + "\n\n" + rawText;
	}

	private static string ReplaceDrawPileInfo(string text)
	{
		ModSettings settings = ModData.Settings;
		if (settings == null || !ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.FrozenEyeEnabled)
		{
			return text;
		}
		string obj = text.Split('\n', 2)[0];
		string rawText = ModLocalization.Text(settings.ShufflePredictionEnabled ? "frozen_eye.draw_pile_info_order_with_shuffle_prediction" : "frozen_eye.draw_pile_info_order").GetRawText();
		return obj + "\n" + rawText;
	}
}
