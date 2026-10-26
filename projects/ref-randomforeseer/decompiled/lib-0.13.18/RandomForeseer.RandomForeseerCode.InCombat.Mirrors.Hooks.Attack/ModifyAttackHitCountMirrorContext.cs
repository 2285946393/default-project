using MegaCrit.Sts2.Core.Commands.Builders;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal sealed class ModifyAttackHitCountMirrorContext : CombatMirrorContext
{
	public required AttackCommand Command { get; init; }

	public required int HitCount { get; set; }
}
