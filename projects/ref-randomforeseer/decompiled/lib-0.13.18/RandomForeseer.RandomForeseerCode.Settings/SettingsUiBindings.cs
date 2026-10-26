using System;
using RandomForeseer.RandomForeseerCode.Data;
using STS2RitsuLib.Settings;

namespace RandomForeseer.RandomForeseerCode.Settings;

internal static class SettingsUiBindings
{
	public static IModSettingsValueBinding<bool> SingleplayerPredictionEnabled { get; } = Binding((ModSettings s) => s.SingleplayerPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.SingleplayerPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> MultiplayerPredictionEnabled { get; } = Binding((ModSettings s) => s.MultiplayerPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.MultiplayerPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> FairModeEnabled { get; } = Binding((ModSettings s) => s.FairModeEnabled, delegate(ModSettings s, bool v)
	{
		s.FairModeEnabled = v;
	});


	public static IModSettingsValueBinding<bool> ShowDriftWarnings { get; } = Binding((ModSettings s) => s.ShowDriftWarnings, delegate(ModSettings s, bool v)
	{
		s.ShowDriftWarnings = v;
	});


	public static IModSettingsValueBinding<bool> PredictBaseGameCardsOnly { get; } = Binding((ModSettings s) => s.PredictBaseGameCardsOnly, delegate(ModSettings s, bool v)
	{
		s.PredictBaseGameCardsOnly = v;
	});


	public static IModSettingsValueBinding<bool> InvokeBaseGameHookListenersOnly { get; } = Binding((ModSettings s) => s.InvokeBaseGameHookListenersOnly, delegate(ModSettings s, bool v)
	{
		s.InvokeBaseGameHookListenersOnly = v;
	});


	public static IModSettingsValueBinding<bool> DeckTransformPredictionEnabled { get; } = Binding((ModSettings s) => s.DeckTransformPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.DeckTransformPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> NextActPredictionEnabled { get; } = Binding((ModSettings s) => s.NextActPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.NextActPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> EventOptionPredictionEnabled { get; } = Binding((ModSettings s) => s.EventOptionPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.EventOptionPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<int> SlipperyBridgeRerollPreviewCount { get; } = Binding((ModSettings s) => s.SlipperyBridgeRerollPreviewCount, delegate(ModSettings s, int v)
	{
		s.SlipperyBridgeRerollPreviewCount = v;
	});


	public static IModSettingsValueBinding<bool> CrystalSphereClairvoyanceEnabled { get; } = Binding((ModSettings s) => s.CrystalSphereClairvoyanceEnabled, delegate(ModSettings s, bool v)
	{
		s.CrystalSphereClairvoyanceEnabled = v;
	});


	public static IModSettingsValueBinding<bool> RelicPickupPredictionEnabled { get; } = Binding((ModSettings s) => s.RelicPickupPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.RelicPickupPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> AncientRelicPickupPredictionEnabled { get; } = Binding((ModSettings s) => s.AncientRelicPickupPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.AncientRelicPickupPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> MerchantRestockPredictionEnabled { get; } = Binding((ModSettings s) => s.MerchantRestockPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.MerchantRestockPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> DriftwoodRerollPredictionEnabled { get; } = Binding((ModSettings s) => s.DriftwoodRerollPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.DriftwoodRerollPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> PaelsWingSacrificePredictionEnabled { get; } = Binding((ModSettings s) => s.PaelsWingSacrificePredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.PaelsWingSacrificePredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> RestSitePredictionEnabled { get; } = Binding((ModSettings s) => s.RestSitePredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.RestSitePredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> CardPlayPredictionEnabled { get; } = Binding((ModSettings s) => s.CardPlayPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.CardPlayPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> PotionPredictionEnabled { get; } = Binding((ModSettings s) => s.PotionPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.PotionPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> CombatTransformPredictionEnabled { get; } = Binding((ModSettings s) => s.CombatTransformPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.CombatTransformPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> EndTurnPredictionEnabled { get; } = Binding((ModSettings s) => s.EndTurnPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.EndTurnPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<EndTurnPredictionDisplayMode> EndTurnPredictionDisplayMode { get; } = Binding((ModSettings s) => s.EndTurnPredictionDisplayMode, delegate(ModSettings s, EndTurnPredictionDisplayMode v)
	{
		s.EndTurnPredictionDisplayMode = v;
	});


	public static IModSettingsValueBinding<EndTurnPredictionDisplayMode> EndTurnHealthBarForecastDisplayMode { get; } = Binding((ModSettings s) => s.EndTurnHealthBarForecastDisplayMode, delegate(ModSettings s, EndTurnPredictionDisplayMode v)
	{
		s.EndTurnHealthBarForecastDisplayMode = v;
	});


	public static IModSettingsValueBinding<bool> FrozenEyeEnabled { get; } = Binding((ModSettings s) => s.FrozenEyeEnabled, delegate(ModSettings s, bool v)
	{
		s.FrozenEyeEnabled = v;
	});


	public static IModSettingsValueBinding<bool> ShufflePredictionEnabled { get; } = Binding((ModSettings s) => s.ShufflePredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.ShufflePredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> CombatCardGenerationPredictionEnabled { get; } = Binding((ModSettings s) => s.CombatCardGenerationPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.CombatCardGenerationPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> CardDrawPredictionEnabled { get; } = Binding((ModSettings s) => s.CardDrawPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.CardDrawPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> CombatCardSelectionPredictionEnabled { get; } = Binding((ModSettings s) => s.CombatCardSelectionPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.CombatCardSelectionPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> CombatOrbGenerationPredictionEnabled { get; } = Binding((ModSettings s) => s.CombatOrbGenerationPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.CombatOrbGenerationPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> PotionCardGenerationPredictionEnabled { get; } = Binding((ModSettings s) => s.PotionCardGenerationPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.PotionCardGenerationPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> PotionDrawPredictionEnabled { get; } = Binding((ModSettings s) => s.PotionDrawPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.PotionDrawPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> PotionGenerationPredictionEnabled { get; } = Binding((ModSettings s) => s.PotionGenerationPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.PotionGenerationPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> AutoPlayFromDrawPilePredictionEnabled { get; } = Binding((ModSettings s) => s.AutoPlayFromDrawPilePredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.AutoPlayFromDrawPilePredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> InferCardOnPlayEffectsEnabled { get; } = Binding((ModSettings s) => s.InferCardOnPlayEffectsEnabled, delegate(ModSettings s, bool v)
	{
		s.InferCardOnPlayEffectsEnabled = v;
	});


	public static IModSettingsValueBinding<bool> ChainedCardEffectPredictionEnabled { get; } = Binding((ModSettings s) => s.ChainedCardEffectPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.ChainedCardEffectPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> CombatDamagePredictionEnabled { get; } = Binding((ModSettings s) => s.CombatDamagePredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.CombatDamagePredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> OrbDamagePredictionEnabled { get; } = Binding((ModSettings s) => s.OrbDamagePredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.OrbDamagePredictionEnabled = v;
	});


	public static IModSettingsValueBinding<bool> RandomTargetAttackPredictionEnabled { get; } = Binding((ModSettings s) => s.RandomTargetAttackPredictionEnabled, delegate(ModSettings s, bool v)
	{
		s.RandomTargetAttackPredictionEnabled = v;
	});


	public static IModSettingsValueBinding<string> DamagePredictionHealthBarColor { get; } = Binding((ModSettings s) => s.DamagePredictionHealthBarColor, delegate(ModSettings s, string v)
	{
		s.DamagePredictionHealthBarColor = v;
	});


	public static IModSettingsValueBinding<bool> DebugSettingsEnabled { get; } = Binding((ModSettings s) => s.DebugSettingsEnabled, delegate(ModSettings s, bool v)
	{
		s.DebugSettingsEnabled = v;
	});


	public static IModSettingsValueBinding<bool> EventDebugRerollEnabled { get; } = Binding((ModSettings s) => s.EventDebugRerollEnabled, delegate(ModSettings s, bool v)
	{
		s.EventDebugRerollEnabled = v;
	});


	private static IModSettingsValueBinding<TValue> Binding<TValue>(Func<ModSettings, TValue> getter, Action<ModSettings, TValue> setter)
	{
		Func<ModSettings, TValue> getter2 = getter;
		return (IModSettingsValueBinding<TValue>)(object)ModSettingsBindings.WithDefault<TValue>((IModSettingsValueBinding<TValue>)(object)ModSettingsBindings.Global<ModSettings, TValue>("RandomForeseer", "settings", getter2, setter), (Func<TValue>)(() => getter2(ModSettings.Default)), (IStructuredModSettingsValueAdapter<TValue>)null);
	}
}
