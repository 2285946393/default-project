using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardDrawnEntry : CombatPredictionHistoryEntry
{
	public required PredictedCard Card { get; init; }

	public required bool FromHandDraw { get; init; }
}
