using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal static class ShouldDieMirrors
{
	private static readonly MirrorMethodSpec ShouldDie = MirrorMethodSpec.Hook("ShouldDie", new Type[1] { typeof(Creature) });

	private static readonly MirrorMethodSpec ShouldDieLate = MirrorMethodSpec.Hook("ShouldDieLate", new Type[1] { typeof(Creature) });

	private static readonly MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool> Registry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool> LateRegistry = CreateLateRegistry();

	public static bool Invoke(AbstractModel listener, ShouldDieMirrorContext context)
	{
		return Registry.Invoke(listener, context, defaultResult: true).Value;
	}

	public static bool InvokeLate(AbstractModel listener, ShouldDieMirrorContext context)
	{
		return LateRegistry.Invoke(listener, context, defaultResult: true).Value;
	}

	private static MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool>(ShouldDie);
		methodMirrorRegistry.Register<FairyInABottle>((Func<FairyInABottle, ShouldDieMirrorContext, bool>)FairyInABottleMirrors.ShouldDie);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool> CreateLateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ShouldDieMirrorContext, bool>(ShouldDieLate);
		methodMirrorRegistry.Register<LizardTail>((Func<LizardTail, ShouldDieMirrorContext, bool>)LizardTailMirrors.ShouldDieLate);
		return methodMirrorRegistry;
	}
}
