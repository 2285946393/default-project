using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntentGraph2.Models;

namespace IntentGraph2.Utils.JsonConverters;

public class MoveReplacementJsonConverter : JsonConverter<MoveReplacement>
{
	public override MoveReplacement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			return new MoveReplacement(JsonSerializer.Deserialize<IntentOverride[]>(ref reader, options), null, null);
		}
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			using (JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader))
			{
				JsonElement rootElement = jsonDocument.RootElement;
				IntentOverride[] intentOverrides = null;
				if (rootElement.TryGetProperty("intentOverrides", out var value))
				{
					intentOverrides = value.Deserialize<IntentOverride[]>(options);
				}
				ArrowOverride arrowOverride = null;
				if (rootElement.TryGetProperty("arrowOverride", out var value2))
				{
					arrowOverride = value2.Deserialize<ArrowOverride>(options);
				}
				string currentMoveCondition = null;
				if (rootElement.TryGetProperty("currentMoveCondition", out var value3))
				{
					currentMoveCondition = value3.Deserialize<string>(options);
				}
				return new MoveReplacement(intentOverrides, arrowOverride, currentMoveCondition);
			}
		}
		throw new JsonException($"Unexpected token type: {reader.TokenType}");
	}

	public override void Write(Utf8JsonWriter writer, MoveReplacement value, JsonSerializerOptions options)
	{
		if (value.ArrowOverride == null && value.IntentOverrides != null && value.CurrentMoveCondition == null)
		{
			JsonSerializer.Serialize(writer, value.IntentOverrides, options);
			return;
		}
		writer.WriteStartObject();
		if (value.IntentOverrides != null)
		{
			writer.WritePropertyName("intentOverrides");
			JsonSerializer.Serialize(writer, value.IntentOverrides, options);
		}
		if (value.ArrowOverride != null)
		{
			writer.WritePropertyName("arrowOverride");
			JsonSerializer.Serialize(writer, value.ArrowOverride, options);
		}
		if (value.CurrentMoveCondition != null)
		{
			writer.WritePropertyName("currentMoveCondition");
			JsonSerializer.Serialize(writer, value.CurrentMoveCondition, options);
		}
		writer.WriteEndObject();
	}
}
