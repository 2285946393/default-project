using MegaCrit.Sts2.Core.Modding;

namespace ModPresetManager.Scripts.Presets;

internal sealed class CurrentModEntry
{
	public required Mod Mod { get; init; }

	public required string Id { get; init; }

	public required string DisplayName { get; init; }

	public required string Version { get; init; }

	public required ModSource Source { get; init; }

	public required bool IsEnabled { get; set; }

	public required bool AffectsGameplay { get; init; }

	public ulong? WorkshopId { get; init; }
}
