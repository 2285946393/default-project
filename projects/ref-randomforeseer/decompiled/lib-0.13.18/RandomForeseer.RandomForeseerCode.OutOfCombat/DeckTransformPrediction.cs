using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class DeckTransformPrediction
{
	public static bool TryCreatePredictor(CardSelectorPrefs prefs, DeckTransformPredictionSource source, [NotNullWhen(true)] out DeckTransformPreviewPredictor? predictor)
	{
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.DeckTransformPredictionEnabled)
		{
			predictor = null;
			return false;
		}
		DeckTransformPreviewPredictor deckTransformPreviewPredictor;
		if (source is DeckTransformPredictionSource.Relic relic)
		{
			RelicModel model = relic.Model;
			Astrolabe val = (Astrolabe)(object)((model is Astrolabe) ? model : null);
			if (val == null)
			{
				NewLeaf val2 = (NewLeaf)(object)((model is NewLeaf) ? model : null);
				if (val2 == null || !IsPredictionAllowedForRelic(settings, (RelicModel)(object)val2))
				{
					goto IL_02b8;
				}
				deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((RelicModel)val2).Owner.RunState.Rng.Niche);
			}
			else
			{
				if (!IsPredictionAllowedForRelic(settings, (RelicModel)(object)val))
				{
					goto IL_02b8;
				}
				deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((RelicModel)val).Owner.RunState.Rng.Niche, upgradePreview: true);
			}
			goto IL_02bb;
		}
		if (source is DeckTransformPredictionSource.Event @event)
		{
			EventModel model2 = @event.Model;
			AromaOfChaos val3 = (AromaOfChaos)(object)((model2 is AromaOfChaos) ? model2 : null);
			if (val3 != null)
			{
				EventOption option = @event.Option;
				if (option != null && option.TextKey == "AROMA_OF_CHAOS.pages.INITIAL.options.LET_GO")
				{
					deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((EventModel)val3).Rng);
					goto IL_02bb;
				}
			}
			else
			{
				EndlessConveyor val4 = (EndlessConveyor)(object)((model2 is EndlessConveyor) ? model2 : null);
				if (val4 != null)
				{
					EventOption option = @event.Option;
					if (option != null && option.TextKey == "ENDLESS_CONVEYOR.pages.ALL.options.JELLY_LIVER")
					{
						deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((EventModel)val4).Rng);
						goto IL_02bb;
					}
				}
				else
				{
					MorphicGrove val5 = (MorphicGrove)(object)((model2 is MorphicGrove) ? model2 : null);
					if (val5 != null)
					{
						EventOption option = @event.Option;
						if (option != null && option.TextKey == "MORPHIC_GROVE.pages.INITIAL.options.GROUP")
						{
							deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((EventModel)val5).Rng);
							goto IL_02bb;
						}
					}
					else
					{
						Symbiote val6 = (Symbiote)(object)((model2 is Symbiote) ? model2 : null);
						if (val6 != null)
						{
							EventOption option = @event.Option;
							if (option != null && option.TextKey == "SYMBIOTE.pages.INITIAL.options.KILL_WITH_FIRE")
							{
								deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((EventModel)val6).Rng);
								goto IL_02bb;
							}
						}
						else
						{
							Trial val7 = (Trial)(object)((model2 is Trial) ? model2 : null);
							if (val7 != null)
							{
								EventOption option = @event.Option;
								if (option != null && option.TextKey == "TRIAL.pages.NONDESCRIPT.options.INNOCENT")
								{
									deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((EventModel)val7).Rng);
									goto IL_02bb;
								}
							}
							else
							{
								WhisperingHollow val8 = (WhisperingHollow)(object)((model2 is WhisperingHollow) ? model2 : null);
								if (val8 != null)
								{
									EventOption option = @event.Option;
									if (option != null && option.TextKey == "WHISPERING_HOLLOW.pages.INITIAL.options.HUG")
									{
										deckTransformPreviewPredictor = new DeckTransformPreviewPredictor(((CardSelectorPrefs)(ref prefs)).MaxSelect, ((EventModel)val8).Rng);
										goto IL_02bb;
									}
								}
							}
						}
					}
				}
			}
		}
		goto IL_02b8;
		IL_02b8:
		deckTransformPreviewPredictor = null;
		goto IL_02bb;
		IL_02bb:
		predictor = deckTransformPreviewPredictor;
		return predictor != null;
	}

	private static bool IsPredictionAllowedForRelic(ModSettings settings, RelicModel relic)
	{
		if (!settings.Allows(PredictionFairness.UnfairInSingleplayer))
		{
			return RewardPagePredictionContext.HasOtherPendingReward(relic);
		}
		return true;
	}
}
