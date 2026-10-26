using System.Collections.Generic;
using System.Linq;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed record EndTurnPredictionResult(DamagePrediction DamagePrediction, PredictionRisk Risk)
{
	public static EndTurnPredictionResult FromDamageHistory(CombatPredictionSimulator simulator)
	{
		List<CombatPredictionDamageReceivedEntry> list = simulator.History.OfType<CombatPredictionDamageReceivedEntry>().Where(DamagePredictionProjector.ShouldIncludeEntry).ToList();
		return new EndTurnPredictionResult(DamagePredictionProjector.Project(list), simulator.History.GetRisk(list));
	}
}
