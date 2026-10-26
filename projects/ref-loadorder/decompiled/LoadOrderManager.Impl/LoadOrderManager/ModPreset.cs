using System.Collections.Generic;

namespace LoadOrderManager;

internal sealed class ModPreset
{
	public string Name { get; set; } = "Default";


	public HashSet<string> DisabledMods { get; set; } = new HashSet<string>();


	public HashSet<string> KnownMods { get; set; } = new HashSet<string>();


	public bool AutoEnableNewMods { get; set; } = true;

}
