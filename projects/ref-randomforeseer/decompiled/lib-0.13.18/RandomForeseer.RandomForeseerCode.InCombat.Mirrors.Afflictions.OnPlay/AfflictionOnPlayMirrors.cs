using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Afflictions.OnPlay;

internal static class AfflictionOnPlayMirrors
{
	private static readonly MirrorMethodSpec OnPlay = new MirrorMethodSpec(typeof(AfflictionModel), "OnPlay", BindingFlags.Instance | BindingFlags.Public, new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature)
	});

	private static readonly MethodMirrorRegistry<AfflictionModel, AfflictionOnPlayMirrorContext> Registry = new MethodMirrorRegistry<AfflictionModel, AfflictionOnPlayMirrorContext>(OnPlay);

	public static MirrorDispatchResult Invoke(CombatPredictionSimulator simulator, PredictedCard card, Creature? target, AfflictionModel affliction)
	{
		return Registry.Invoke(affliction, new AfflictionOnPlayMirrorContext
		{
			Simulator = simulator,
			Card = card,
			Target = target
		});
	}
}
