using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCreatureAttackedEntry : CombatPredictionHistoryEntry
{
	public required Creature Attacker { get; init; }

	public required IReadOnlyList<DamageResult> HitResults { get; init; }
}
