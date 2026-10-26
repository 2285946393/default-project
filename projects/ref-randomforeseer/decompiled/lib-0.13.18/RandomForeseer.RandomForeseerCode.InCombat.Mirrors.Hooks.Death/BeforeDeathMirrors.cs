using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal static class BeforeDeathMirrors
{
	private static readonly MirrorMethodSpec BeforeDeath = MirrorMethodSpec.Hook("BeforeDeath", new Type[1] { typeof(Creature) });

	private static readonly MethodMirrorRegistry<AbstractModel, BeforeDeathMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, BeforeDeathMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, BeforeDeathMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, BeforeDeathMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, BeforeDeathMirrorContext>(BeforeDeath);
		methodMirrorRegistry.RegisterIgnored<Crusher>();
		methodMirrorRegistry.RegisterIgnored<Rocket>();
		methodMirrorRegistry.RegisterIgnored<HeistPower>();
		methodMirrorRegistry.RegisterIgnored<SwipePower>();
		return methodMirrorRegistry;
	}
}
