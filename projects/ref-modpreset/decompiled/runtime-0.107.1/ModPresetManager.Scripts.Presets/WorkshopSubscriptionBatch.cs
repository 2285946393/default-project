using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;

namespace ModPresetManager.Scripts.Presets;

internal sealed class WorkshopSubscriptionBatch : IDisposable
{
	private const long TimeoutMilliseconds = 300000L;

	private const long KickDownloadAfterMilliseconds = 5000L;

	private readonly Dictionary<ulong, WorkshopDownloadJob> _jobs = new Dictionary<ulong, WorkshopDownloadJob>();

	private readonly Callback<ItemInstalled_t>? _installedCallback;

	private readonly Callback<DownloadItemResult_t>? _downloadCallback;

	public static WorkshopSubscriptionBatch? Active { get; private set; }

	public ModPresetDocument Preset { get; }

	public IReadOnlyCollection<WorkshopDownloadJob> Jobs => _jobs.Values;

	public bool IsComplete
	{
		get
		{
			if (_jobs.Count > 0)
			{
				return _jobs.Values.All((WorkshopDownloadJob job) => job.IsTerminal);
			}
			return false;
		}
	}

	public int InstalledCount => _jobs.Values.Count((WorkshopDownloadJob job) => job.State == WorkshopDownloadState.Installed);

	public int FailedCount => _jobs.Values.Count(delegate(WorkshopDownloadJob job)
	{
		WorkshopDownloadState state = job.State;
		return (uint)(state - 4) <= 1u;
	});

	public bool IsSteamAvailable
	{
		get
		{
			try
			{
				return _installedCallback != null && _downloadCallback != null && SteamAPI.IsSteamRunning();
			}
			catch
			{
				return false;
			}
		}
	}

	public WorkshopSubscriptionBatch(ModPresetDocument preset)
	{
		if (Active != null)
		{
			throw new InvalidOperationException("A Workshop download batch is already active.");
		}
		Preset = preset;
		Active = this;
		try
		{
			_installedCallback = Callback<ItemInstalled_t>.Create((DispatchDelegate<ItemInstalled_t>)OnItemInstalled);
			_downloadCallback = Callback<DownloadItemResult_t>.Create((DispatchDelegate<DownloadItemResult_t>)OnDownloadResult);
		}
		catch
		{
			_installedCallback = null;
			_downloadCallback = null;
		}
	}

	public void Start(IEnumerable<PresetImportItem> items)
	{
		foreach (PresetImportItem item in items.Where(delegate(PresetImportItem item)
		{
			if (item.Status == PresetImportStatus.DownloadRequired)
			{
				ulong? workshopId = item.Entry.WorkshopId;
				if (workshopId.HasValue)
				{
					return workshopId.GetValueOrDefault() != 0;
				}
				return false;
			}
			return false;
		}))
		{
			ulong value = item.Entry.WorkshopId.Value;
			if (!_jobs.ContainsKey(value))
			{
				WorkshopDownloadJob workshopDownloadJob = new WorkshopDownloadJob
				{
					ModId = item.Entry.Id,
					WorkshopId = value,
					StartedAt = Environment.TickCount64
				};
				_jobs.Add(value, workshopDownloadJob);
				Start(workshopDownloadJob);
			}
		}
	}

	public void Poll()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		long tickCount = Environment.TickCount64;
		PublishedFileId_t val = default(PublishedFileId_t);
		ulong bytesDownloaded = default(ulong);
		ulong bytesTotal = default(ulong);
		foreach (WorkshopDownloadJob item in _jobs.Values.Where((WorkshopDownloadJob job) => !job.IsTerminal))
		{
			try
			{
				if (tickCount - item.StartedAt > 300000)
				{
					item.State = WorkshopDownloadState.TimedOut;
					continue;
				}
				((PublishedFileId_t)(ref val))._002Ector(item.WorkshopId);
				uint itemState = SteamUGC.GetItemState(val);
				if (IsInstalled(itemState))
				{
					item.State = WorkshopDownloadState.Installed;
				}
				else if ((itemState & 0x10u) != 0 || (itemState & 0x20u) != 0)
				{
					item.State = WorkshopDownloadState.Downloading;
					if (SteamUGC.GetItemDownloadInfo(val, ref bytesDownloaded, ref bytesTotal))
					{
						item.BytesDownloaded = bytesDownloaded;
						item.BytesTotal = bytesTotal;
					}
				}
				else if (!item.DownloadKicked && tickCount - item.StartedAt > 5000)
				{
					SteamUGC.DownloadItem(val, true);
					item.DownloadKicked = true;
				}
			}
			catch
			{
				item.State = WorkshopDownloadState.Failed;
			}
		}
	}

	public void Dispose()
	{
		_installedCallback?.Dispose();
		_downloadCallback?.Dispose();
		if (Active == this)
		{
			Active = null;
		}
	}

	private static void Start(WorkshopDownloadJob job)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			PublishedFileId_t val = default(PublishedFileId_t);
			((PublishedFileId_t)(ref val))._002Ector(job.WorkshopId);
			if (IsInstalled(SteamUGC.GetItemState(val)))
			{
				job.State = WorkshopDownloadState.Installed;
				return;
			}
			SteamUGC.SubscribeItem(val);
			job.State = WorkshopDownloadState.Downloading;
		}
		catch
		{
			job.State = WorkshopDownloadState.Failed;
		}
	}

	private void OnItemInstalled(ItemInstalled_t installed)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if ((ulong)installed.m_unAppID.m_AppId == 2868840 && _jobs.TryGetValue(installed.m_nPublishedFileId.m_PublishedFileId, out WorkshopDownloadJob value))
		{
			value.State = WorkshopDownloadState.Installed;
		}
	}

	private void OnDownloadResult(DownloadItemResult_t result)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		if (_jobs.TryGetValue(result.m_nPublishedFileId.m_PublishedFileId, out WorkshopDownloadJob value))
		{
			value.State = (((int)result.m_eResult == 1) ? WorkshopDownloadState.WaitingForInstall : WorkshopDownloadState.Failed);
		}
	}

	private static bool IsInstalled(uint state)
	{
		bool num = (state & 4) != 0;
		bool flag = (state & 8) != 0;
		if (num)
		{
			return !flag;
		}
		return false;
	}
}
