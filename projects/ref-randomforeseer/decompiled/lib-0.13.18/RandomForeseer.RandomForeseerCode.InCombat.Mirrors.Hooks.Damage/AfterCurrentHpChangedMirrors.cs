using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class AfterCurrentHpChangedMirrors
{
	private static readonly MirrorMethodSpec AfterCurrentHpChanged = MirrorMethodSpec.Hook("AfterCurrentHpChanged", new Type[2]
	{
		typeof(Creature),
		typeof(decimal)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCurrentHpChangedMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterCurrentHpChangedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCurrentHpChangedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCurrentHpChangedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCurrentHpChangedMirrorContext>(AfterCurrentHpChanged);
		methodMirrorRegistry.RegisterIgnored<Crusher>();
		methodMirrorRegistry.RegisterIgnored<Rocket>();
		methodMirrorRegistry.Register<RedSkull>((Action<RedSkull, AfterCurrentHpChangedMirrorContext>)HandleRedSkull);
		methodMirrorRegistry.Register<NecroMasteryPower>((Action<NecroMasteryPower, AfterCurrentHpChangedMirrorContext>)HandleNecroMasteryPower);
		methodMirrorRegistry.RegisterIgnored<MeatOnTheBone>();
		return methodMirrorRegistry;
	}

	private static void HandleRedSkull(RedSkull relic, AfterCurrentHpChangedMirrorContext context)
	{
		SimCreatureState creature = context.State.GetCreature(((RelicModel)relic).Owner.Creature);
		decimal num = (decimal)creature.MaxHp * (((RelicModel)relic).DynamicVars["HpThreshold"].BaseValue / 100m);
		if ((decimal)creature.CurrentHp <= num != relic._strengthApplied)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleNecroMasteryPower(NecroMasteryPower power, AfterCurrentHpChangedMirrorContext context)
	{
		if (context.Delta < 0m && context.Creature.Monster is Osty && context.Creature.PetOwner == ((PowerModel)power).Owner.Player)
		{
			context.Simulator.Damage(context.State.HittableEnemies, -context.Delta * (decimal)((PowerModel)power).Amount, (ValueProp)6, ((PowerModel)power).Owner);
		}
	}
}
