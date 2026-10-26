using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardAfflictedEntry : CombatPredictionHistoryEntry
{
	public required PredictedCard Card { get; init; }

	public required AfflictionModel Affliction { get; init; }
}
