using MegaCrit.Sts2.Core.Entities.Creatures;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionDamageReceivedEntry : CombatPredictionHistoryEntry
{
	public required Creature Receiver { get; init; }

	public required DamageResult Result { get; init; }

	public required Creature? Dealer { get; init; }

	public required PredictedCard? CardSource { get; init; }
}
