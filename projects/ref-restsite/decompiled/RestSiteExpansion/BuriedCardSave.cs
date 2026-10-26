using MegaCrit.Sts2.Core.Saves.Runs;

namespace RestSiteExpansion;

public sealed class BuriedCardSave
{
	public SerializableCard? Card { get; set; }

	public bool DefaultGranted { get; set; }
}
