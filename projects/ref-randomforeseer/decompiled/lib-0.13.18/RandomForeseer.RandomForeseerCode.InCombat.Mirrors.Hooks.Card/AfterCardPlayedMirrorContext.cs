using MegaCrit.Sts2.Core.Entities.Cards;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class AfterCardPlayedMirrorContext : CombatCardMirrorContext
{
	public required CardPlay CardPlay { get; init; }
}
