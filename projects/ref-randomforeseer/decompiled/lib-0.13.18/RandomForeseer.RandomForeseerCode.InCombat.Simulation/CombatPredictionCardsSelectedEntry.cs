using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardsSelectedEntry : CombatPredictionHistoryEntry
{
	public required IReadOnlyList<CardModel> SourceCards { get; init; }

	public required IReadOnlyList<CardModel> PreviewCards { get; init; }
}
