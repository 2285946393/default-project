using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace RandomForeseer.RandomForeseerCode.Data;

internal sealed class ModSettings
{
	public const int CurrentSchemaVersion = 3;

	[CompilerGenerated]
	private int _003CSlipperyBridgeRerollPreviewCount_003Ek__BackingField = 5;

	public static ModSettings Default { get; } = new ModSettings();


	[JsonPropertyName("schema_version")]
	public int SchemaVersion { get; set; } = 3;


	public bool SingleplayerPredictionEnabled { get; set; } = true;


	public bool MultiplayerPredictionEnabled { get; set; } = true;


	public bool FairModeEnabled { get; set; } = true;


	public bool ShowDriftWarnings { get; set; }

	public bool PredictBaseGameCardsOnly { get; set; }

	public bool InvokeBaseGameHookListenersOnly { get; set; }

	public bool DeckTransformPredictionEnabled { get; set; } = true;


	public bool NextActPredictionEnabled { get; set; } = true;


	public bool EventOptionPredictionEnabled { get; set; } = true;


	public int SlipperyBridgeRerollPreviewCount
	{
		get
		{
			return Math.Clamp(_003CSlipperyBridgeRerollPreviewCount_003Ek__BackingField, 1, 10);
		}
		set
		{
			_003CSlipperyBridgeRerollPreviewCount_003Ek__BackingField = Math.Clamp(value, 1, 10);
		}
	}

	public bool CrystalSphereClairvoyanceEnabled { get; set; } = true;


	public bool RelicPickupPredictionEnabled { get; set; } = true;


	public bool AncientRelicPickupPredictionEnabled { get; set; } = true;


	public bool MerchantRestockPredictionEnabled { get; set; } = true;


	public bool DriftwoodRerollPredictionEnabled { get; set; } = true;


	public bool PaelsWingSacrificePredictionEnabled { get; set; } = true;


	public bool RestSitePredictionEnabled { get; set; } = true;


	public bool CardPlayPredictionEnabled { get; set; } = true;


	public bool PotionPredictionEnabled { get; set; } = true;


	public bool CombatTransformPredictionEnabled { get; set; } = true;


	public bool EndTurnPredictionEnabled { get; set; } = true;


	public EndTurnPredictionDisplayMode EndTurnPredictionDisplayMode { get; set; }

	public EndTurnPredictionDisplayMode EndTurnHealthBarForecastDisplayMode { get; set; } = EndTurnPredictionDisplayMode.AlwaysDuringPlayerTurn;


	public bool FrozenEyeEnabled { get; set; } = true;


	public bool ShufflePredictionEnabled { get; set; } = true;


	public bool CombatCardGenerationPredictionEnabled { get; set; } = true;


	public bool CardDrawPredictionEnabled { get; set; } = true;


	public bool CombatCardSelectionPredictionEnabled { get; set; } = true;


	public bool CombatOrbGenerationPredictionEnabled { get; set; } = true;


	public bool PotionCardGenerationPredictionEnabled { get; set; } = true;


	public bool PotionDrawPredictionEnabled { get; set; } = true;


	public bool PotionGenerationPredictionEnabled { get; set; } = true;


	public bool AutoPlayFromDrawPilePredictionEnabled { get; set; } = true;


	public bool InferCardOnPlayEffectsEnabled { get; set; } = true;


	public bool ChainedCardEffectPredictionEnabled { get; set; } = true;


	public bool CombatDamagePredictionEnabled { get; set; } = true;


	public bool OrbDamagePredictionEnabled { get; set; } = true;


	public bool RandomTargetAttackPredictionEnabled { get; set; } = true;


	public string DamagePredictionHealthBarColor { get; set; } = "#E8C91A";


	public bool DebugSettingsEnabled { get; set; }

	public bool EventDebugRerollEnabled { get; set; }
}
