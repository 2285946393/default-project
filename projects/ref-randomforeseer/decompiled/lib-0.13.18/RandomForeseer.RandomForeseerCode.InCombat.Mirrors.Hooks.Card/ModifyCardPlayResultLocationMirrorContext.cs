using MegaCrit.Sts2.Core.Entities.Cards;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ModifyCardPlayResultLocationMirrorContext : CombatMirrorContext
{
	public required PredictedCard Card { get; init; }

	public required bool IsAutoPlay { get; init; }

	public required ResourceInfo Resources { get; init; }

	public required CardLocation Location { get; set; }
}
