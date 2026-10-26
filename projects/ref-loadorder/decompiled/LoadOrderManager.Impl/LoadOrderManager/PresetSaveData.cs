using System.Collections.Generic;

namespace LoadOrderManager;

internal sealed class PresetSaveData
{
	public int SchemaVersion { get; set; } = 2;


	public List<ModPreset> Profiles { get; set; } = new List<ModPreset>();


	public int CurrentProfileIndex { get; set; }
}
