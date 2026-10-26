namespace ModPresetManager.Scripts.Presets;

internal sealed class PresetApplyResult
{
	public bool Success { get; init; }

	public int ChangedCount { get; init; }

	public string? Error { get; init; }
}
