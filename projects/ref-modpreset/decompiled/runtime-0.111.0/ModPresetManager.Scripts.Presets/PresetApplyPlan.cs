using System.Collections.Generic;
using MegaCrit.Sts2.Core.Modding;

namespace ModPresetManager.Scripts.Presets;

internal sealed class PresetApplyPlan
{
	public required ModSettings ModSettings { get; init; }

	public required ModSettings? PreviousModSettings { get; init; }

	public required List<SettingsSaveMod> PreviousOrder { get; init; }

	public required List<SettingsSaveMod> FinalOrder { get; init; }

	public required int ChangedCount { get; init; }
}
