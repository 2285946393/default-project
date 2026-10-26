using System.Collections.Generic;

namespace ModPresetManager.Scripts.Presets;

internal sealed class ModConfigurationComparison
{
	public required IReadOnlyList<CurrentModEntry> Loaded { get; init; }

	public required IReadOnlyList<CurrentModEntry> Saved { get; init; }

	public required bool Matches { get; init; }
}
