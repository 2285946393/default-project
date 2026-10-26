using MegaCrit.Sts2.Core.Commands.Builders;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal sealed class AfterAttackMirrorContext : CombatMirrorContext
{
	public required AttackCommand Command { get; init; }
}
