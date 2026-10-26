using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using IntentGraph2.Models;

namespace IntentGraph2.Utils;

public static class IntentDefinitionEditorService
{
	public static IntentDefinitionList LoadEditableDefinitions(string monsterModelFullName, string? devOverrideFilePath = null, IReadOnlyDictionary<string, IntentDefinitionList>? runtimeDefinitions = null)
	{
		if (IntentGraphMod.LoadIntentDefinitionsFromFile(devOverrideFilePath ?? IntentGraphMod.GetDevIntentDefinitionFilePath()).TryGetValue(monsterModelFullName, out IntentDefinitionList value))
		{
			return Clone(value) ?? new IntentDefinitionList();
		}
		if (runtimeDefinitions == null)
		{
			runtimeDefinitions = IntentGraphMod.IntentDefinitions;
		}
		if (runtimeDefinitions.TryGetValue(monsterModelFullName, out IntentDefinitionList value2))
		{
			return Clone(value2) ?? new IntentDefinitionList();
		}
		return new IntentDefinitionList();
	}

	public static void SaveEditableDefinitions(string monsterModelFullName, IntentDefinitionList definitions, string? filePath = null)
	{
		string? filePath2 = filePath ?? IntentGraphMod.GetDevIntentDefinitionFilePath();
		Dictionary<string, IntentDefinitionList> dictionary = IntentGraphMod.LoadIntentDefinitionsFromFile(filePath2);
		dictionary[monsterModelFullName] = Clone(definitions) ?? new IntentDefinitionList();
		IntentGraphMod.SaveIntentDefinitionsToFile(filePath2, dictionary);
	}

	public static T? Clone<T>(T? value)
	{
		if (value == null)
		{
			return default(T);
		}
		return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, IntentGraphMod.SerializeOptions), IntentGraphMod.SerializeOptions);
	}

	public static string SerializeJson<T>(T value)
	{
		return JsonSerializer.Serialize(value, GetIndentedSerializerOptions());
	}

	public static T? DeserializeJson<T>(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return default(T);
		}
		return JsonSerializer.Deserialize<T>(text, IntentGraphMod.SerializeOptions);
	}

	public static string BuildReadOnlySummary(IntentDefinition definition)
	{
		List<string> list = new List<string> { IntentGraphMod.IntentGraphStrings.GetValueOrDefault("ui.editor.read_only.summary.none", "Not editable here: graph.") };
		if (definition.Graph != null)
		{
			list.Add(IntentGraphMod.IntentGraphStrings.GetValueOrDefault("ui.editor.read_only.summary.graph_present", "graph is present and remains read-only in this editor."));
		}
		return string.Join(Environment.NewLine, list.Distinct());
	}

	private static JsonSerializerOptions GetIndentedSerializerOptions()
	{
		return new JsonSerializerOptions(IntentGraphMod.SerializeOptions)
		{
			WriteIndented = true
		};
	}
}
