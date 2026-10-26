using MegaCrit.Sts2.Core.Commands.Builders;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal sealed class BeforeAttackMirrorContext : CombatMirrorContext
{
	public required AttackCommand Command { get; init; }
}
