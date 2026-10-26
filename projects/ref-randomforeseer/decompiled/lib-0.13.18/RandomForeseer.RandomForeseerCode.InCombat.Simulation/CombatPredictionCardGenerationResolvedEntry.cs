using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardGenerationResolvedEntry : CombatPredictionHistoryEntry
{
	public required CombatPredictionCardGeneratedEntry OriginalEntry { get; init; }

	public required CardModel? PreviewCard { get; init; }
}
