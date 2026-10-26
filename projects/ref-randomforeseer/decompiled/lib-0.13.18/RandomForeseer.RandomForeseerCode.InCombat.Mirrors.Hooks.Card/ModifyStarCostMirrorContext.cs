using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ModifyStarCostMirrorContext : CombatMirrorContext
{
	public required PredictedCard Card { get; init; }

	public required decimal Cost { get; set; }
}
