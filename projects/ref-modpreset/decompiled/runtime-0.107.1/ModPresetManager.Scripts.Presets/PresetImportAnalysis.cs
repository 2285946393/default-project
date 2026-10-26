using System.Collections.Generic;
using System.Linq;

namespace ModPresetManager.Scripts.Presets;

internal sealed class PresetImportAnalysis
{
	public required ModPresetDocument Preset { get; init; }

	public List<PresetImportItem> Items { get; init; } = new List<PresetImportItem>();


	public bool CanApply => Items.All((PresetImportItem item) => item.IsResolved);

	public int ExactCount => Items.Count((PresetImportItem item) => item.Status == PresetImportStatus.Exact);

	public int FallbackCount => Items.Count((PresetImportItem item) => item.Status == PresetImportStatus.LocalFallback);

	public int DownloadCount => Items.Count((PresetImportItem item) => item.Status == PresetImportStatus.DownloadRequired);

	public int ProblemCount => Items.Count((PresetImportItem item) => !item.IsResolved);
}
