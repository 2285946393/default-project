using MegaCrit.Sts2.Core.Entities.Players;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal sealed class BeforeFlushMirrorContext : CombatMirrorContext
{
	public required Player Player { get; init; }
}
