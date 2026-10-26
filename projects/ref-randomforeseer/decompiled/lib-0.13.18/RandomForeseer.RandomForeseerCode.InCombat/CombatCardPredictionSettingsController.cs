using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;
using RandomForeseer.RandomForeseerCode.Settings;
using STS2RitsuLib.Settings;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatCardPredictionSettingsController
{
	private static bool _isRegistered;

	public static void Register()
	{
		if (!_isRegistered)
		{
			SyncInferenceSetting();
			ModSettingsBindingWriteEvents.ValueWritten += OnSettingsValueWritten;
			_isRegistered = true;
		}
	}

	private static void OnSettingsValueWritten(IModSettingsBinding binding)
	{
		if (binding == SettingsUiBindings.InferCardOnPlayEffectsEnabled)
		{
			SyncInferenceSetting();
		}
		else if (binding == SettingsUiBindings.PredictBaseGameCardsOnly)
		{
			CardOnPlayMirrors.InvalidateLookupCache();
		}
	}

	private static void SyncInferenceSetting()
	{
		CardOnPlayMirrors.AllowInference = ModData.Settings.InferCardOnPlayEffectsEnabled;
	}
}
