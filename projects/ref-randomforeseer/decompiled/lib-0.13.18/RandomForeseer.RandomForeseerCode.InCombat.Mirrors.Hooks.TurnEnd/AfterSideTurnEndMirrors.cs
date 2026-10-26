using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Shared;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal static class AfterSideTurnEndMirrors
{
	private static readonly MirrorMethodSpec AfterSideTurnEnd = MirrorMethodSpec.Hook("AfterSideTurnEnd", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CombatSide),
		typeof(IEnumerable<Creature>)
	});

	private static readonly MirrorMethodSpec AfterSideTurnEndLate = MirrorMethodSpec.Hook("AfterSideTurnEndLate", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CombatSide),
		typeof(IEnumerable<Creature>)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> Registry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> LateRegistry = CreateLateRegistry();

	public static void Invoke(AbstractModel listener, SideTurnEndMirrorContext context)
	{
		Registry.TryInvokeRegistered(listener, context, out var _);
	}

	public static void InvokeLate(AbstractModel listener, SideTurnEndMirrorContext context)
	{
		LateRegistry.TryInvokeRegistered(listener, context, out var _);
	}

	private static MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext>(AfterSideTurnEnd);
		methodMirrorRegistry.Register<ConsumingShadowPower>((Action<ConsumingShadowPower, SideTurnEndMirrorContext>)HandleConsumingShadowPower);
		methodMirrorRegistry.Register<ConstrictPower>((Action<ConstrictPower, SideTurnEndMirrorContext>)HandleConstrictPower);
		methodMirrorRegistry.Register<DarkEmbracePower>((Action<DarkEmbracePower, SideTurnEndMirrorContext>)HandleDarkEmbracePower);
		methodMirrorRegistry.Register<DemisePower>((Action<DemisePower, SideTurnEndMirrorContext>)HandleDemisePower);
		methodMirrorRegistry.Register<DoomPower>((Action<DoomPower, SideTurnEndMirrorContext>)DoomPowerMirrors.AfterSideTurnEnd);
		methodMirrorRegistry.Register<JossPaper>((Action<JossPaper, SideTurnEndMirrorContext>)JossPaperMirrors.AfterSideTurnEnd);
		methodMirrorRegistry.Register<MagicBombPower>((Action<MagicBombPower, SideTurnEndMirrorContext>)HandleMagicBombPower);
		methodMirrorRegistry.Register<ParryingShield>((Action<ParryingShield, SideTurnEndMirrorContext>)HandleParryingShield);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> CreateLateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext>(AfterSideTurnEndLate);
		methodMirrorRegistry.Register<DisintegrationPower>((Action<DisintegrationPower, SideTurnEndMirrorContext>)HandleDisintegrationPower);
		return methodMirrorRegistry;
	}

	private static void HandleConsumingShadowPower(ConsumingShadowPower power, SideTurnEndMirrorContext context)
	{
		if (!context.Participants.Contains(((PowerModel)power).Owner))
		{
			return;
		}
		Player player = ((PowerModel)power).Owner.Player;
		if (player != null && context.State.GetPlayerCombatState(player).OrbQueue.Orbs.Count != 0)
		{
			for (int i = 0; i < ((PowerModel)power).Amount; i++)
			{
				context.Simulator.OrbEvokeLast(player);
			}
		}
	}

	private static void HandleConstrictPower(ConstrictPower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner))
		{
			context.Simulator.Damage(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
		}
	}

	private static void HandleDarkEmbracePower(DarkEmbracePower power, SideTurnEndMirrorContext context)
	{
		DarkEmbracePower power2 = power;
		if (!context.Participants.Contains(((PowerModel)power2).Owner))
		{
			return;
		}
		Player player = ((PowerModel)power2).Owner.Player;
		if (player != null)
		{
			DarkEmbracePredictionState darkEmbracePredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new DarkEmbracePredictionState(power2));
			context.Simulator.Draw(player, ((PowerModel)power2).Amount * darkEmbracePredictionState.EtherealCount);
			darkEmbracePredictionState.EtherealCount = 0;
		}
	}

	private static void HandleDemisePower(DemisePower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner))
		{
			context.Simulator.Damage(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)6, null);
		}
	}

	private static void HandleMagicBombPower(MagicBombPower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner))
		{
			Creature applier = ((PowerModel)power).Applier;
			if (applier != null && context.State.GetCreature(applier).IsAlive)
			{
				context.Simulator.Damage(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
			}
		}
	}

	private static void HandleDisintegrationPower(DisintegrationPower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner))
		{
			context.Simulator.Damage(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
		}
	}

	private static void HandleParryingShield(ParryingShield relic, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((RelicModel)relic).Owner.Creature) && !((decimal)context.State.GetCreature(((RelicModel)relic).Owner.Creature).Block < ((DynamicVar)((RelicModel)relic).DynamicVars.Block).BaseValue))
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			if (val != null)
			{
				context.Simulator.Damage(val, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
			}
		}
	}
}
