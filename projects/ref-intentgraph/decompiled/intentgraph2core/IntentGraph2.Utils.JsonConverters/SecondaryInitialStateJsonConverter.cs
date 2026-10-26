using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntentGraph2.Models;

namespace IntentGraph2.Utils.JsonConverters;

public class SecondaryInitialStateJsonConverter : JsonConverter<SecondaryInitialState>
{
	public override SecondaryInitialState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			return new SecondaryInitialState(reader.GetString());
		}
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			using (JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader))
			{
				JsonElement rootElement = jsonDocument.RootElement;
				if (!rootElement.TryGetProperty("id", out var value))
				{
					throw new JsonException("Missing required property: Id");
				}
				string? @string = value.GetString();
				if (string.IsNullOrEmpty(@string))
				{
					throw new JsonException("Invalid value for property: Id");
				}
				Position offset = default(Position);
				if (rootElement.TryGetProperty("offset", out var value2))
				{
					offset = value2.Deserialize<Position>(options);
				}
				return new SecondaryInitialState(@string, offset);
			}
		}
		throw new JsonException($"Unexpected token type: {reader.TokenType}");
	}

	public override void Write(Utf8JsonWriter writer, SecondaryInitialState value, JsonSerializerOptions options)
	{
		if (value.Offset == default(Position))
		{
			writer.WriteStringValue(value.Id);
			return;
		}
		writer.WriteStartObject();
		writer.WriteString("id", value.Id);
		writer.WritePropertyName("offset");
		JsonSerializer.Serialize(writer, value.Offset, options);
		writer.WriteEndObject();
	}
}
