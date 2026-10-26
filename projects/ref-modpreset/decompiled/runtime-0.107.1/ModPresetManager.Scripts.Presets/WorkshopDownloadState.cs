namespace ModPresetManager.Scripts.Presets;

internal enum WorkshopDownloadState
{
	Pending,
	Downloading,
	WaitingForInstall,
	Installed,
	Failed,
	TimedOut
}
