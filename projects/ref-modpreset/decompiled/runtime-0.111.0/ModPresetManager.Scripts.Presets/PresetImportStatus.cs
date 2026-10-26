namespace ModPresetManager.Scripts.Presets;

internal enum PresetImportStatus
{
	Exact,
	LocalFallback,
	DownloadRequired,
	Missing,
	VersionMismatch,
	Ambiguous
}
