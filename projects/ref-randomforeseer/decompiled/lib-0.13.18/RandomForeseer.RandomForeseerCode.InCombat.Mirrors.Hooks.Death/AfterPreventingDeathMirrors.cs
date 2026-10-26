using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal static class AfterPreventingDeathMirrors
{
	private static readonly MirrorMethodSpec AfterPreventingDeath = MirrorMethodSpec.Hook("AfterPreventingDeath", new Type[1] { typeof(Creature) });

	private static readonly MethodMirrorRegistry<AbstractModel, AfterPreventingDeathMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel preventer, AfterPreventingDeathMirrorContext context)
	{
		Registry.Invoke(preventer, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterPreventingDeathMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterPreventingDeathMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterPreventingDeathMirrorContext>(AfterPreventingDeath);
		methodMirrorRegistry.Register<FairyInABottle>((Action<FairyInABottle, AfterPreventingDeathMirrorContext>)FairyInABottleMirrors.AfterPreventingDeath);
		methodMirrorRegistry.Register<LizardTail>((Action<LizardTail, AfterPreventingDeathMirrorContext>)LizardTailMirrors.AfterPreventingDeath);
		return methodMirrorRegistry;
	}
}
