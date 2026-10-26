using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class AfterModifyingCardPlayCountMirrorContext : CombatMirrorContext
{
	public required PredictedCard Card { get; init; }
}
