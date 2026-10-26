using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardGeneratedEntry : CombatPredictionHistoryEntry
{
	public required PredictedCard Card { get; init; }

	public required CardGenerationResultKind ResultKind { get; init; }
}
