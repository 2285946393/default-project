using System.Text.Json.Serialization;
using IntentGraph2.Utils.JsonConverters;

namespace IntentGraph2.Models;

[JsonConverter(typeof(SecondaryInitialStateJsonConverter))]
public record SecondaryInitialState(string Id, Position Offset = default(Position));
