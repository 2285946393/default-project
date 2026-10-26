using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RandomForeseer.RandomForeseerCode.Data;
using STS2RitsuLib.Settings;

namespace RandomForeseer.RandomForeseerCode.Settings;

internal static class ModSettingsLoggingController
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = false,
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	private static JsonObject? _snapshot;

	private static bool _isRegistered;

	public static void Register()
	{
		if (!_isRegistered)
		{
			_snapshot = CaptureSnapshot();
			Entry.Logger.Info("Settings loaded: " + _snapshot.ToJsonString(JsonOptions), 1);
			ModSettingsBindingWriteEvents.ValueWritten += OnSettingsValueWritten;
			_isRegistered = true;
		}
	}

	private static void OnSettingsValueWritten(IModSettingsBinding binding)
	{
		if (binding == null || !(binding.ModId == "RandomForeseer") || !(binding.DataKey == "settings"))
		{
			return;
		}
		JsonObject snapshot = _snapshot;
		JsonObject jsonObject = (_snapshot = CaptureSnapshot());
		foreach (string item in snapshot.Select<KeyValuePair<string, JsonNode>, string>((KeyValuePair<string, JsonNode> property) => property.Key).Concat(jsonObject.Select<KeyValuePair<string, JsonNode>, string>((KeyValuePair<string, JsonNode> property) => property.Key)).Distinct<string>(StringComparer.Ordinal))
		{
			snapshot.TryGetPropertyValue(item, out JsonNode jsonNode);
			jsonObject.TryGetPropertyValue(item, out JsonNode jsonNode2);
			if (!JsonNode.DeepEquals(jsonNode, jsonNode2))
			{
				Entry.Logger.Info($"Setting changed: {item}: {FormatValue(jsonNode)} -> {FormatValue(jsonNode2)}", 1);
			}
		}
	}

	private static JsonObject CaptureSnapshot()
	{
		return JsonSerializer.SerializeToNode(ModData.Settings, JsonOptions).AsObject();
	}

	private static string FormatValue(JsonNode? value)
	{
		return value?.ToJsonString(JsonOptions) ?? "null";
	}
}
