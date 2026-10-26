namespace ModPresetManager.Scripts.Presets;

internal sealed class PresetExportResult
{
	public bool Success { get; init; }

	public string Text { get; init; } = "";


	public int ModCount { get; init; }

	public string? InvalidModId { get; init; }

	public string? UnavailableModId { get; init; }

	public string? DuplicateModId { get; init; }
}
