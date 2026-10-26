using System;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;

internal static class OrbMirrors
{
	private static readonly MirrorMethodSpec Passive = new MirrorMethodSpec(typeof(OrbModel), "Passive", BindingFlags.Instance | BindingFlags.Public, new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature)
	});

	private static readonly MirrorMethodSpec Evoke = new MirrorMethodSpec(typeof(OrbModel), "Evoke", BindingFlags.Instance | BindingFlags.Public, new Type[1] { typeof(PlayerChoiceContext) });

	private static readonly MirrorMethodSpec BeforeTurnEndOrbTrigger = new MirrorMethodSpec(typeof(OrbModel), "BeforeTurnEndOrbTrigger", BindingFlags.Instance | BindingFlags.Public, new Type[1] { typeof(PlayerChoiceContext) });

	private static readonly MethodMirrorRegistry<OrbModel, OrbPassiveMirrorContext> PassiveRegistry = CreatePassiveRegistry();

	private static readonly MethodMirrorRegistry<OrbModel, OrbMirrorContext, IReadOnlyList<Creature>> EvokeRegistry = CreateEvokeRegistry();

	private static readonly MethodMirrorRegistry<OrbModel, OrbMirrorContext> BeforeTurnEndOrbTriggerRegistry = CreateBeforeTurnEndOrbTriggerRegistry();

	public static void InvokePassive(CombatPredictionSimulator simulator, OrbModel orb, Creature? target = null)
	{
		PassiveRegistry.Invoke(orb, new OrbPassiveMirrorContext
		{
			Simulator = simulator,
			Target = target
		});
	}

	public static IReadOnlyList<Creature> InvokeEvoke(CombatPredictionSimulator simulator, OrbModel orb)
	{
		return EvokeRegistry.Invoke(orb, new OrbMirrorContext
		{
			Simulator = simulator
		}, Array.Empty<Creature>()).Value;
	}

	public static void InvokeBeforeTurnEndOrbTrigger(CombatPredictionSimulator simulator, OrbModel orb)
	{
		BeforeTurnEndOrbTriggerRegistry.Invoke(orb, new OrbMirrorContext
		{
			Simulator = simulator
		});
	}

	private static MethodMirrorRegistry<OrbModel, OrbPassiveMirrorContext> CreatePassiveRegistry()
	{
		MethodMirrorRegistry<OrbModel, OrbPassiveMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<OrbModel, OrbPassiveMirrorContext>(Passive);
		methodMirrorRegistry.Register<LightningOrb>((Action<LightningOrb, OrbPassiveMirrorContext>)LightningOrbMirrors.Passive);
		methodMirrorRegistry.Register<FrostOrb>((Action<FrostOrb, OrbPassiveMirrorContext>)FrostOrbMirrors.Passive);
		methodMirrorRegistry.Register<DarkOrb>((Action<DarkOrb, OrbPassiveMirrorContext>)DarkOrbMirrors.Passive);
		methodMirrorRegistry.Register<GlassOrb>((Action<GlassOrb, OrbPassiveMirrorContext>)GlassOrbMirrors.Passive);
		methodMirrorRegistry.Register<PlasmaOrb>((Action<PlasmaOrb, OrbPassiveMirrorContext>)PlasmaOrbMirrors.Passive);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<OrbModel, OrbMirrorContext, IReadOnlyList<Creature>> CreateEvokeRegistry()
	{
		MethodMirrorRegistry<OrbModel, OrbMirrorContext, IReadOnlyList<Creature>> methodMirrorRegistry = new MethodMirrorRegistry<OrbModel, OrbMirrorContext, IReadOnlyList<Creature>>(Evoke);
		methodMirrorRegistry.Register<LightningOrb>((Func<LightningOrb, OrbMirrorContext, IReadOnlyList<Creature>>)LightningOrbMirrors.Evoke);
		methodMirrorRegistry.Register<FrostOrb>((Func<FrostOrb, OrbMirrorContext, IReadOnlyList<Creature>>)FrostOrbMirrors.Evoke);
		methodMirrorRegistry.Register<DarkOrb>((Func<DarkOrb, OrbMirrorContext, IReadOnlyList<Creature>>)DarkOrbMirrors.Evoke);
		methodMirrorRegistry.Register<GlassOrb>((Func<GlassOrb, OrbMirrorContext, IReadOnlyList<Creature>>)GlassOrbMirrors.Evoke);
		methodMirrorRegistry.Register<PlasmaOrb>((Func<PlasmaOrb, OrbMirrorContext, IReadOnlyList<Creature>>)PlasmaOrbMirrors.Evoke);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<OrbModel, OrbMirrorContext> CreateBeforeTurnEndOrbTriggerRegistry()
	{
		MethodMirrorRegistry<OrbModel, OrbMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<OrbModel, OrbMirrorContext>(BeforeTurnEndOrbTrigger);
		methodMirrorRegistry.Register<LightningOrb>((Action<LightningOrb, OrbMirrorContext>)LightningOrbMirrors.BeforeTurnEndOrbTrigger);
		methodMirrorRegistry.Register<FrostOrb>((Action<FrostOrb, OrbMirrorContext>)FrostOrbMirrors.BeforeTurnEndOrbTrigger);
		methodMirrorRegistry.Register<DarkOrb>((Action<DarkOrb, OrbMirrorContext>)DarkOrbMirrors.BeforeTurnEndOrbTrigger);
		methodMirrorRegistry.Register<GlassOrb>((Action<GlassOrb, OrbMirrorContext>)GlassOrbMirrors.BeforeTurnEndOrbTrigger);
		return methodMirrorRegistry;
	}
}
