using System;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal static class BeforeAttackMirrors
{
	private static readonly MirrorMethodSpec BeforeAttack = MirrorMethodSpec.Hook("BeforeAttack", new Type[1] { typeof(AttackCommand) });

	private static readonly MethodMirrorRegistry<AbstractModel, BeforeAttackMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, BeforeAttackMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, BeforeAttackMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, BeforeAttackMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, BeforeAttackMirrorContext>(BeforeAttack);
		methodMirrorRegistry.Register<GigantificationPower>((Action<GigantificationPower, BeforeAttackMirrorContext>)GigantificationPowerMirrors.BeforeAttack);
		methodMirrorRegistry.RegisterIgnored<HellraiserPower>();
		methodMirrorRegistry.Register<VigorPower>((Action<VigorPower, BeforeAttackMirrorContext>)VigorPowerMirrors.BeforeAttack);
		return methodMirrorRegistry;
	}
}
