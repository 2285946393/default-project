using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Power;

internal static class PowerAppliedMirrors
{
	private static readonly MirrorMethodSpec BeforeApplied = new MirrorMethodSpec(typeof(PowerModel), "BeforeApplied", BindingFlags.Instance | BindingFlags.Public, new Type[4]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MirrorMethodSpec AfterApplied = new MirrorMethodSpec(typeof(PowerModel), "AfterApplied", BindingFlags.Instance | BindingFlags.Public, new Type[2]
	{
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MethodMirrorRegistry<PowerModel, PowerAppliedMirrorContext> BeforeRegistry = new MethodMirrorRegistry<PowerModel, PowerAppliedMirrorContext>(BeforeApplied);

	private static readonly MethodMirrorRegistry<PowerModel, PowerAppliedMirrorContext> AfterRegistry = new MethodMirrorRegistry<PowerModel, PowerAppliedMirrorContext>(AfterApplied);

	public static void InvokeBefore(CombatPredictionSimulator simulator, PowerModel power, Creature target, decimal amount, Creature? applier, PredictedCard? cardSource)
	{
		BeforeRegistry.Invoke(power, new PowerAppliedMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Amount = amount,
			Applier = applier,
			CardSource = cardSource
		});
	}

	public static void InvokeAfter(CombatPredictionSimulator simulator, PowerModel power, Creature target, decimal amount, Creature? applier, PredictedCard? cardSource)
	{
		AfterRegistry.Invoke(power, new PowerAppliedMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Amount = amount,
			Applier = applier,
			CardSource = cardSource
		});
	}
}
