namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class AfterCardExhaustedMirrorContext : CombatCardMirrorContext
{
	public required bool CausedByEthereal { get; init; }
}
