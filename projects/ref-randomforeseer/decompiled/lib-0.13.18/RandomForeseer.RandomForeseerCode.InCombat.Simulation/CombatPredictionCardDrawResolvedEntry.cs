using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardDrawResolvedEntry : CombatPredictionHistoryEntry
{
	public required CombatPredictionCardDrawnEntry OriginalEntry { get; init; }

	public required CardModel PreviewCard { get; init; }
}
