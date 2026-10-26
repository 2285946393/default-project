using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal abstract class CombatPredictionHistoryEntry
{
	public int Index { get; set; }

	public PredictionTraceFrame? Trace { get; set; }
}
