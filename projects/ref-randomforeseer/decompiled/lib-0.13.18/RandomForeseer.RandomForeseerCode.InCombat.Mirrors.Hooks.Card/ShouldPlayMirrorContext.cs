using MegaCrit.Sts2.Core.Entities.Cards;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ShouldPlayMirrorContext : CombatMirrorContext
{
	public required PredictedCard Card { get; init; }

	public required AutoPlayType AutoPlayType { get; init; }
}
