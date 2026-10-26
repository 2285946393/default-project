using MegaCrit.Sts2.Core.Entities.Creatures;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal sealed class ShouldDieMirrorContext : CombatMirrorContext
{
	public required Creature Creature { get; init; }
}
