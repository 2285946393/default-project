using System.Text.Json.Serialization;
using IntentGraph2.Utils.JsonConverters;

namespace IntentGraph2.Models;

[JsonConverter(typeof(MoveReplacementJsonConverter))]
public record MoveReplacement(IntentOverride?[]? IntentOverrides, ArrowOverride? ArrowOverride, string? CurrentMoveCondition);
