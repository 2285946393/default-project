using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(CardSelectCmd), "FromDeckForTransformation", new Type[]
{
	typeof(Player),
	typeof(CardSelectorPrefs),
	typeof(Func<CardModel, CardTransformation>)
})]
internal static class DeckTransformPredictionInjectionPatch
{
	[HarmonyPrefix]
	private static void InjectPredictor(CardSelectorPrefs prefs, ref Func<CardModel, CardTransformation>? cardToTransformation)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		DeckTransformPredictionSource currentSource = DeckTransformPredictionContext.CurrentSource;
		if (cardToTransformation != null || (object)currentSource == null)
		{
			return;
		}
		try
		{
			if (DeckTransformPrediction.TryCreatePredictor(prefs, currentSource, out DeckTransformPreviewPredictor predictor))
			{
				cardToTransformation = predictor.PredictNext;
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Failed to inject deck transform preview predictor: {ex}", 1);
			ModTelemetry.CaptureException(ex, "deck_transform_prediction", "inject_preview_predictor", CreateTelemetryContext(currentSource));
		}
	}

	private static object? CreateTelemetryContext(DeckTransformPredictionSource source)
	{
		if (!(source is DeckTransformPredictionSource.Relic relic))
		{
			if (source is DeckTransformPredictionSource.Event @event)
			{
				return new
				{
					Event = TelemetryContext.ForModel((AbstractModel)(object)@event.Model),
					Option = @event.Option.TextKey
				};
			}
			return null;
		}
		return new
		{
			Relic = TelemetryContext.ForModel((AbstractModel)(object)relic.Model)
		};
	}
}
