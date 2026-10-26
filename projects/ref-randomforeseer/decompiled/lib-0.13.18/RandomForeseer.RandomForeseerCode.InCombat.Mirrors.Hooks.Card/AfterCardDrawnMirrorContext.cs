namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class AfterCardDrawnMirrorContext : CombatCardMirrorContext
{
	public required bool FromHandDraw { get; init; }
}
