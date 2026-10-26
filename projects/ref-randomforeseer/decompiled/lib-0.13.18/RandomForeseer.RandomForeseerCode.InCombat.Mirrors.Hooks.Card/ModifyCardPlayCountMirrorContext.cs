using MegaCrit.Sts2.Core.Entities.Creatures;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ModifyCardPlayCountMirrorContext : CombatMirrorContext
{
	public required PredictedCard Card { get; init; }

	public required Creature? Target { get; init; }

	public required int PlayCount { get; set; }
}
