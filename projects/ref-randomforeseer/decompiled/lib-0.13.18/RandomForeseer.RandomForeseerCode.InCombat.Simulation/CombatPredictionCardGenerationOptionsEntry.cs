using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionCardGenerationOptionsEntry : CombatPredictionHistoryEntry
{
	public required IReadOnlyList<CardModel> PreviewCards { get; init; }
}
