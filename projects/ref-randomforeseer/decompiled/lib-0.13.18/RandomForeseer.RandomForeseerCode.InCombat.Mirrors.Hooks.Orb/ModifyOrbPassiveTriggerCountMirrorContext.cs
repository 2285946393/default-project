using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Orb;

internal sealed class ModifyOrbPassiveTriggerCountMirrorContext : CombatMirrorContext
{
	public required OrbModel Orb { get; init; }

	public required int TriggerCount { get; set; }
}
