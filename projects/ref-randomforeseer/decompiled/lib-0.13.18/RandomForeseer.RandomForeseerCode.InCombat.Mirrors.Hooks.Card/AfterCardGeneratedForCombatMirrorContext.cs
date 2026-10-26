using MegaCrit.Sts2.Core.Entities.Players;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class AfterCardGeneratedForCombatMirrorContext : CombatCardMirrorContext
{
	public required Player? Creator { get; init; }
}
