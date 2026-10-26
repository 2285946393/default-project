using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal static class AfterBlockBrokenMirrors
{
	private static readonly MirrorMethodSpec AfterBlockBroken = MirrorMethodSpec.Hook("AfterBlockBroken", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(Creature)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterBlockBrokenMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterBlockBrokenMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterBlockBrokenMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterBlockBrokenMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterBlockBrokenMirrorContext>(AfterBlockBroken);
		methodMirrorRegistry.RegisterIgnored<BurrowedPower>();
		methodMirrorRegistry.Register<HandDrill>((Action<HandDrill, AfterBlockBrokenMirrorContext>)HandleHandDrill);
		return methodMirrorRegistry;
	}

	private static void HandleHandDrill(HandDrill relic, AfterBlockBrokenMirrorContext context)
	{
		if (context.Breaker != ((RelicModel)relic).Owner.Creature)
		{
			Creature? breaker = context.Breaker;
			if (((breaker != null) ? breaker.PetOwner : null) != ((RelicModel)relic).Owner)
			{
				return;
			}
		}
		if (!context.Target.IsPlayer)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}
}
