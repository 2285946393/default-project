using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Orb;

internal sealed class AfterOrbEvokedMirrorContext : CombatMirrorContext
{
	public required OrbModel Orb { get; init; }

	public required IReadOnlyList<Creature> Targets { get; init; }
}
