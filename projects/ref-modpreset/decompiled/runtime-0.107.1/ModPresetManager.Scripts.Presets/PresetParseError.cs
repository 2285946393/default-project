namespace ModPresetManager.Scripts.Presets;

internal enum PresetParseError
{
	None,
	Empty,
	InvalidJson,
	UnsupportedFormat,
	UnsupportedSchema,
	InvalidEntry,
	DuplicateMod
}
