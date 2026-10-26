using MegaCrit.Sts2.Core.Entities.Players;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ShouldDrawMirrorContext : CombatMirrorContext
{
	public required Player Player { get; init; }

	public required bool FromHandDraw { get; init; }
}
