using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class AfterShuffleMirrors
{
	private static readonly MirrorMethodSpec AfterShuffle = MirrorMethodSpec.Hook("AfterShuffle", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(Player)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterShuffleMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterShuffleMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterShuffleMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterShuffleMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterShuffleMirrorContext>(AfterShuffle);
		methodMirrorRegistry.Register<BiiigHug>((Action<BiiigHug, AfterShuffleMirrorContext>)HandleBiiigHug);
		methodMirrorRegistry.Register<StratagemPower>((Action<StratagemPower, AfterShuffleMirrorContext>)HandleStratagemPower);
		methodMirrorRegistry.Register<TheAbacus>((Action<TheAbacus, AfterShuffleMirrorContext>)HandleTheAbacus);
		return methodMirrorRegistry;
	}

	private static void HandleBiiigHug(BiiigHug relic, AfterShuffleMirrorContext context)
	{
		if (((RelicModel)relic).Owner == context.Player)
		{
			context.Simulator.CreateAndAddGeneratedCardsToCombat<Soot>(context.Player, (PileType)1, 1, context.Player, (CardPilePosition)3);
		}
	}

	private static void HandleStratagemPower(StratagemPower power, AfterShuffleMirrorContext context)
	{
		if (((PowerModel)power).Owner.Player == context.Player)
		{
			context.History.RecordRisk(PredictionRiskReason.UnresolvedPlayerChoice);
		}
	}

	private static void HandleTheAbacus(TheAbacus relic, AfterShuffleMirrorContext context)
	{
		if (((RelicModel)relic).Owner == context.Player)
		{
			context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, ((RelicModel)relic).DynamicVars.Block);
		}
	}
}
