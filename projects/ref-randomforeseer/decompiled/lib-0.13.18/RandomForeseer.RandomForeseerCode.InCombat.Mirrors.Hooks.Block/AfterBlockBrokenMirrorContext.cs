using MegaCrit.Sts2.Core.Entities.Creatures;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal sealed class AfterBlockBrokenMirrorContext : CombatMirrorContext
{
	public required Creature Target { get; init; }

	public required Creature? Breaker { get; init; }
}
