using MegaCrit.Sts2.Core.Modding;

namespace ModPresetManager.Scripts.Presets;

internal sealed class PresetImportItem
{
	public required int Order { get; init; }

	public required ModPresetEntry Entry { get; init; }

	public required PresetImportStatus Status { get; init; }

	public Mod? MatchedMod { get; init; }

	public string? CurrentVersion { get; init; }

	public bool IsResolved
	{
		get
		{
			PresetImportStatus status = Status;
			if ((uint)status <= 1u)
			{
				return true;
			}
			return false;
		}
	}
}
