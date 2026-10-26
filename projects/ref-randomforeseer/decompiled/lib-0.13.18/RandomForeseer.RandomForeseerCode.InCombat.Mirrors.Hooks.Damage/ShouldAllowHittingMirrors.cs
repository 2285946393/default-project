using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class ShouldAllowHittingMirrors
{
	private static readonly MirrorMethodSpec Method = MirrorMethodSpec.Hook("ShouldAllowHitting", new Type[1] { typeof(Creature) });

	private static readonly MethodMirrorRegistry<AbstractModel, ShouldAllowHittingMirrorContext, bool> Registry = CreateRegistry();

	public static bool Invoke(AbstractModel listener, ShouldAllowHittingMirrorContext context)
	{
		if (!Registry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return listener.ShouldAllowHitting(context.Creature);
		}
		return dispatchResult.Value;
	}

	private static MethodMirrorRegistry<AbstractModel, ShouldAllowHittingMirrorContext, bool> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ShouldAllowHittingMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ShouldAllowHittingMirrorContext, bool>(Method);
		methodMirrorRegistry.Register<DieForYouPower>((Func<DieForYouPower, ShouldAllowHittingMirrorContext, bool>)HandleDieForYouPower);
		return methodMirrorRegistry;
	}

	private static bool HandleDieForYouPower(DieForYouPower power, ShouldAllowHittingMirrorContext context)
	{
		return context.State.GetCreature(context.Creature).IsAlive;
	}
}
