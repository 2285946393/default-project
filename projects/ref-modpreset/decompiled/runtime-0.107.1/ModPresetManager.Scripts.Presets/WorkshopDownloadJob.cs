namespace ModPresetManager.Scripts.Presets;

internal sealed class WorkshopDownloadJob
{
	public required string ModId { get; init; }

	public required ulong WorkshopId { get; init; }

	public WorkshopDownloadState State { get; set; }

	public ulong BytesDownloaded { get; set; }

	public ulong BytesTotal { get; set; }

	public long StartedAt { get; set; }

	public bool DownloadKicked { get; set; }

	public bool IsTerminal
	{
		get
		{
			WorkshopDownloadState state = State;
			if ((uint)(state - 3) <= 2u)
			{
				return true;
			}
			return false;
		}
	}
}
