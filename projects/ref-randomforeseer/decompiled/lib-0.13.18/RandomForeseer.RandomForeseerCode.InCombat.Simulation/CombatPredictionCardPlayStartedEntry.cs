using MegaCrit.Sts2.Core.Entities.Cards;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardPlayStartedEntry : CombatPredictionHistoryEntry
{
	public required PredictedCard Card { get; init; }

	public required CardPlay CardPlay { get; init; }
}
