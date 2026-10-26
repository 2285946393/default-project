using MegaCrit.Sts2.Core.Entities.Players;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionEnergySpentEntry : CombatPredictionHistoryEntry
{
	public required Player Player { get; init; }

	public required int Amount { get; init; }
}
