using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionOrbChanneledEntry : CombatPredictionHistoryEntry
{
	public required OrbModel Orb { get; init; }
}
