using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class AfterCardDiscardedMirrors
{
	private static readonly MirrorMethodSpec AfterCardDiscarded = MirrorMethodSpec.Hook("AfterCardDiscarded", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(CardModel)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCardDiscardedMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterCardDiscardedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCardDiscardedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCardDiscardedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCardDiscardedMirrorContext>(AfterCardDiscarded);
		methodMirrorRegistry.Register<Tingsha>((Action<Tingsha, AfterCardDiscardedMirrorContext>)HandleTingsha);
		methodMirrorRegistry.Register<ToughBandages>((Action<ToughBandages, AfterCardDiscardedMirrorContext>)HandleToughBandages);
		return methodMirrorRegistry;
	}

	private static void HandleTingsha(Tingsha relic, AfterCardDiscardedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (((RelicModel)relic).Owner == context.PreviewCard.Owner && ((RelicModel)relic).Owner.Creature.Side == context.CombatState.CurrentSide)
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			if (val != null)
			{
				context.Simulator.Damage(val, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
			}
		}
	}

	private static void HandleToughBandages(ToughBandages relic, AfterCardDiscardedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (((RelicModel)relic).Owner == context.PreviewCard.Owner && ((RelicModel)relic).Owner.Creature.Side == context.CombatState.CurrentSide)
		{
			context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, ((RelicModel)relic).DynamicVars.Block);
		}
	}
}
