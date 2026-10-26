using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionPotionGeneratedEntry : CombatPredictionHistoryEntry
{
	public required PotionModel PreviewPotion { get; init; }
}
