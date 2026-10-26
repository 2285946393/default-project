using System;
using System.Text.Json.Serialization;

namespace ModPresetManager.Scripts.Presets;

internal sealed class ModPresetEntry
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";


	[JsonPropertyName("version")]
	public string Version { get; set; } = "";


	[JsonPropertyName("source")]
	public string Source { get; set; } = "local";


	[JsonPropertyName("workshopId")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ulong? WorkshopId { get; set; }

	[JsonPropertyName("affectsGameplay")]
	public bool AffectsGameplay { get; set; }

	[JsonIgnore]
	public ModPresetSource ParsedSource
	{
		get
		{
			if (!string.Equals(Source, "workshop", StringComparison.OrdinalIgnoreCase))
			{
				return ModPresetSource.Local;
			}
			return ModPresetSource.Workshop;
		}
	}
}
