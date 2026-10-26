using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal sealed class BeforeBlockGainedMirrorContext : CombatMirrorContext
{
	public required Creature Creature { get; init; }

	public required decimal Amount { get; init; }

	public required ValueProp Props { get; init; }

	public required PredictedCard? Source { get; init; }
}
