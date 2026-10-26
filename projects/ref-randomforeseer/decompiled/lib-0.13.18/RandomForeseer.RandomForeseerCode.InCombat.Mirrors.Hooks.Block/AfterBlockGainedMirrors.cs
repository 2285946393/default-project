using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal static class AfterBlockGainedMirrors
{
	private static readonly MirrorMethodSpec AfterBlockGained = MirrorMethodSpec.Hook("AfterBlockGained", new Type[4]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(CardModel)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterBlockGainedMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterBlockGainedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterBlockGainedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterBlockGainedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterBlockGainedMirrorContext>(AfterBlockGained);
		methodMirrorRegistry.Register<BeaconOfHopePower>((Action<BeaconOfHopePower, AfterBlockGainedMirrorContext>)HandleBeaconOfHopePower);
		methodMirrorRegistry.Register<JuggernautPower>((Action<JuggernautPower, AfterBlockGainedMirrorContext>)HandleJuggernautPower);
		return methodMirrorRegistry;
	}

	private static void HandleBeaconOfHopePower(BeaconOfHopePower power, AfterBlockGainedMirrorContext context)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		AfterBlockGainedMirrorContext context2 = context;
		BeaconOfHopePower power2 = power;
		BeaconOfHopePredictionState beaconOfHopePredictionState = context2.StateStore.Get<BeaconOfHopePredictionState>((AbstractModel)(object)power2);
		if (context2.Amount < 1m || context2.Creature != ((PowerModel)power2).Owner || context2.CombatState.CurrentSide != ((PowerModel)power2).Owner.Side || beaconOfHopePredictionState.HasAlreadyBeenGivenBlock)
		{
			return;
		}
		decimal num = context2.Amount * 0.5m;
		if (num < 1m)
		{
			return;
		}
		List<Creature> list = (from creature in context2.CombatState.GetTeammatesOf(((PowerModel)power2).Owner)
			where creature.IsPlayer && context2.State.GetCreature(creature).IsAlive && creature != ((PowerModel)power2).Owner
			select creature).ToList();
		beaconOfHopePredictionState.HasAlreadyBeenGivenBlock = true;
		try
		{
			foreach (Creature item in list)
			{
				context2.Simulator.GainBlock(item, num, (ValueProp)4);
			}
		}
		finally
		{
			beaconOfHopePredictionState.HasAlreadyBeenGivenBlock = false;
		}
	}

	private static void HandleJuggernautPower(JuggernautPower power, AfterBlockGainedMirrorContext context)
	{
		if (context.Creature == ((PowerModel)power).Owner && context.Amount > 0m)
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			if (val != null)
			{
				context.Simulator.Damage(val, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
			}
		}
	}
}
