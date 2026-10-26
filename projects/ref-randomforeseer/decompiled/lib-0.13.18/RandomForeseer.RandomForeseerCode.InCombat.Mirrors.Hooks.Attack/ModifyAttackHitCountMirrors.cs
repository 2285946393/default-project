using System;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal static class ModifyAttackHitCountMirrors
{
	private static readonly MirrorMethodSpec ModifyAttackHitCount = MirrorMethodSpec.Hook("ModifyAttackHitCount", new Type[2]
	{
		typeof(AttackCommand),
		typeof(int)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyAttackHitCountMirrorContext, int> Registry = new MethodMirrorRegistry<AbstractModel, ModifyAttackHitCountMirrorContext, int>(ModifyAttackHitCount);

	public static int Invoke(AbstractModel listener, ModifyAttackHitCountMirrorContext context)
	{
		return Registry.Invoke(listener, context, context.HitCount).Value;
	}
}
