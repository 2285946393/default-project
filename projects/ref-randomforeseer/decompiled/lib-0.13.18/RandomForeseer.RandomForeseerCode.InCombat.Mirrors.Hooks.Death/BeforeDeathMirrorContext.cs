using MegaCrit.Sts2.Core.Entities.Creatures;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal sealed class BeforeDeathMirrorContext : CombatMirrorContext
{
	public required Creature Creature { get; init; }
}
