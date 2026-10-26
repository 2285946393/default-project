using MegaCrit.Sts2.Core.Entities.Players;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal sealed class AfterAutoPostPlayMirrorContext : CombatMirrorContext
{
	public required Player Player { get; init; }
}
