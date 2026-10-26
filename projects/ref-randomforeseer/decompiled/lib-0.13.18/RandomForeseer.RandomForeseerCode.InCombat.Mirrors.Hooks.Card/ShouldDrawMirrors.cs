using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class ShouldDrawMirrors
{
	private static readonly MirrorMethodSpec ShouldDraw = MirrorMethodSpec.Hook("ShouldDraw", new Type[2]
	{
		typeof(Player),
		typeof(bool)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ShouldDrawMirrorContext, bool> Registry = CreateRegistry();

	public static bool Invoke(AbstractModel listener, ShouldDrawMirrorContext context)
	{
		return Registry.Invoke(listener, context, defaultResult: true).Value;
	}

	private static MethodMirrorRegistry<AbstractModel, ShouldDrawMirrorContext, bool> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ShouldDrawMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ShouldDrawMirrorContext, bool>(ShouldDraw);
		methodMirrorRegistry.Register<NoDrawPower>((Func<NoDrawPower, ShouldDrawMirrorContext, bool>)HandleNoDrawPower);
		methodMirrorRegistry.Register<Fiddle>((Func<Fiddle, ShouldDrawMirrorContext, bool>)HandleFiddle);
		return methodMirrorRegistry;
	}

	private static bool HandleNoDrawPower(NoDrawPower power, ShouldDrawMirrorContext context)
	{
		if (!context.FromHandDraw && context.Player == ((PowerModel)power).Owner.Player)
		{
			return false;
		}
		return true;
	}

	private static bool HandleFiddle(Fiddle relic, ShouldDrawMirrorContext context)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!context.FromHandDraw && context.Player == ((RelicModel)relic).Owner && context.CombatState.CurrentSide == context.Player.Creature.Side)
		{
			return false;
		}
		return true;
	}
}
