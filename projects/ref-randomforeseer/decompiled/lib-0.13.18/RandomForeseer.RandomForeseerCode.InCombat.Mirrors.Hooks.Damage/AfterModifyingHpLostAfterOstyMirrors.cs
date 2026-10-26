using System;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class AfterModifyingHpLostAfterOstyMirrors
{
	private static readonly MirrorMethodSpec AfterModifyingHpLostAfterOsty = MirrorMethodSpec.Hook("AfterModifyingHpLostAfterOsty", Array.Empty<Type>());

	private static readonly MethodMirrorRegistry<AbstractModel, AfterModifyingHpLostMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel modifier, AfterModifyingHpLostMirrorContext context)
	{
		Registry.Invoke(modifier, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterModifyingHpLostMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterModifyingHpLostMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterModifyingHpLostMirrorContext>(AfterModifyingHpLostAfterOsty);
		methodMirrorRegistry.RegisterIgnored<BeatingRemnant>();
		methodMirrorRegistry.Register<BufferPower>((Action<BufferPower, AfterModifyingHpLostMirrorContext>)HandleBufferPower);
		methodMirrorRegistry.RegisterIgnored<IntangiblePower>();
		methodMirrorRegistry.RegisterIgnored<TheBoot>();
		methodMirrorRegistry.RegisterIgnored<TungstenRod>();
		return methodMirrorRegistry;
	}

	private static void HandleBufferPower(BufferPower power, AfterModifyingHpLostMirrorContext context)
	{
		context.StateStore.GetPowerAmount((PowerModel)(object)power).Decrement();
	}
}
