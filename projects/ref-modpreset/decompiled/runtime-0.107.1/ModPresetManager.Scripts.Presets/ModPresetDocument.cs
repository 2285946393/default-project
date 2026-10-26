using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ModPresetManager.Scripts.Presets;

internal sealed class ModPresetDocument
{
	public const string FormatId = "mpm-preset";

	public const string LegacyFormatId = "mdv-preset";

	public const int CurrentSchema = 1;

	[JsonPropertyName("format")]
	public string Format { get; set; } = "mpm-preset";


	[JsonPropertyName("schema")]
	public int Schema { get; set; } = 1;


	[JsonPropertyName("game")]
	public string? GameVersion { get; set; }

	[JsonPropertyName("mods")]
	public List<ModPresetEntry> Mods { get; set; } = new List<ModPresetEntry>();


	public static bool IsSupportedFormat(string? format)
	{
		if (!string.Equals(format, "mpm-preset", StringComparison.Ordinal))
		{
			return string.Equals(format, "mdv-preset", StringComparison.Ordinal);
		}
		return true;
	}
}
