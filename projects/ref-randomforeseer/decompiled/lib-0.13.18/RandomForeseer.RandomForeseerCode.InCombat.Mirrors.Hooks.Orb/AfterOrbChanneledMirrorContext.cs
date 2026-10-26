using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Orb;

internal sealed class AfterOrbChanneledMirrorContext : CombatMirrorContext
{
	public required Player Player { get; init; }

	public required OrbModel Orb { get; init; }
}
