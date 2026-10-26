using MegaCrit.Sts2.Core.Entities.Players;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class AfterShuffleMirrorContext : CombatMirrorContext
{
	public required Player Player { get; init; }
}
