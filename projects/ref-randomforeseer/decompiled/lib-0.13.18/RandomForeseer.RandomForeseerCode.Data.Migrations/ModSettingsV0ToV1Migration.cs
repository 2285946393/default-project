using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace RandomForeseer.RandomForeseerCode.Data.Migrations;

internal sealed class ModSettingsV0ToV1Migration : IMigration
{
	private static readonly string[] LegacyCardPlayFeatureProperties = new string[7] { "EnableCombatCardPrediction", "EnableCardDrawPrediction", "EnableCombatCardSelectionPrediction", "EnableOrbPrediction", "EnablePotionGenerationPrediction", "EnableAutoPlayFromDrawPilePrediction", "EnableCombatDamagePrediction" };

	private static readonly string[] LegacyPotionFeatureProperties = new string[7] { "EnableCombatCardSelectionPrediction", "EnableOrbPrediction", "EnablePotionCardPrediction", "EnablePotionDrawPrediction", "EnablePotionGenerationPrediction", "EnableAutoPlayFromDrawPilePrediction", "EnableCombatDamagePrediction" };

	private static readonly (string LegacyName, string CurrentName)[] RenamedProperties = new(string, string)[26]
	{
		("EnableSingleplayerPrediction", "SingleplayerPredictionEnabled"),
		("EnableMultiplayerPrediction", "MultiplayerPredictionEnabled"),
		("EnableFairMode", "FairModeEnabled"),
		("EnableDriftWarnings", "ShowDriftWarnings"),
		("EnableTransformPrediction", "DeckTransformPredictionEnabled"),
		("EnableRelicPickupPrediction", "RelicPickupPredictionEnabled"),
		("EnableEventOptionPrediction", "EventOptionPredictionEnabled"),
		("EnableCrystalSphereClairvoyance", "CrystalSphereClairvoyanceEnabled"),
		("EnableDriftwoodRerollPrediction", "DriftwoodRerollPredictionEnabled"),
		("EnablePaelsWingSacrificePrediction", "PaelsWingSacrificePredictionEnabled"),
		("EnableRestSitePrediction", "RestSitePredictionEnabled"),
		("EnableNextActPrediction", "NextActPredictionEnabled"),
		("EnableCombatTransformPrediction", "CombatTransformPredictionEnabled"),
		("EnableEndTurnPrediction", "EndTurnPredictionEnabled"),
		("EnableFrozenEye", "FrozenEyeEnabled"),
		("EnableShufflePrediction", "ShufflePredictionEnabled"),
		("EnableCombatCardPrediction", "CombatCardGenerationPredictionEnabled"),
		("EnableCardDrawPrediction", "CardDrawPredictionEnabled"),
		("EnableCombatCardSelectionPrediction", "CombatCardSelectionPredictionEnabled"),
		("EnablePotionCardPrediction", "PotionCardGenerationPredictionEnabled"),
		("EnablePotionDrawPrediction", "PotionDrawPredictionEnabled"),
		("EnablePotionGenerationPrediction", "PotionGenerationPredictionEnabled"),
		("EnableAutoPlayFromDrawPilePrediction", "AutoPlayFromDrawPilePredictionEnabled"),
		("EnableCombatDamagePrediction", "CombatDamagePredictionEnabled"),
		("EnableRandomTargetAttackPrediction", "RandomTargetAttackPredictionEnabled"),
		("ShowDebugSettingsPage", "DebugSettingsEnabled")
	};

	public int FromVersion => 0;

	public int ToVersion => 1;

	public bool Migrate(JsonObject data)
	{
		bool value = HasAnyLegacyFeatureEnabled(data, LegacyCardPlayFeatureProperties);
		bool value2 = HasAnyLegacyFeatureEnabled(data, LegacyPotionFeatureProperties);
		(string, string)[] renamedProperties = RenamedProperties;
		for (int i = 0; i < renamedProperties.Length; i++)
		{
			var (legacyName, currentName) = renamedProperties[i];
			data.MoveProperty(legacyName, currentName);
		}
		SplitOrbPredictionSetting(data);
		data.SetIfMissing("CardPlayPredictionEnabled", value);
		data.SetIfMissing("PotionPredictionEnabled", value2);
		return true;
	}

	private static bool HasAnyLegacyFeatureEnabled(JsonObject data, IEnumerable<string> propertyNames)
	{
		JsonObject data2 = data;
		return propertyNames.Any((string propertyName) => data2.GetBoolean(propertyName).GetValueOrDefault(true));
	}

	private static void SplitOrbPredictionSetting(JsonObject data)
	{
		if (data.TryGetPropertyValue("EnableOrbPrediction", out JsonNode jsonNode))
		{
			data.SetIfMissing("CombatOrbGenerationPredictionEnabled", jsonNode);
			data.SetIfMissing("OrbDamagePredictionEnabled", jsonNode);
			data.Remove("EnableOrbPrediction");
		}
	}
}
