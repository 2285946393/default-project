using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionRiskEntry : CombatPredictionHistoryEntry
{
	public required PredictionRiskReason Reason { get; init; }
}
