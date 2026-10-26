using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Orb;

internal static class AfterOrbChanneledMirrors
{
	private static readonly MirrorMethodSpec Method = MirrorMethodSpec.Hook("AfterOrbChanneled", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(Player),
		typeof(OrbModel)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterOrbChanneledMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterOrbChanneledMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterOrbChanneledMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterOrbChanneledMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterOrbChanneledMirrorContext>(Method);
		methodMirrorRegistry.Register<Metronome>((Action<Metronome, AfterOrbChanneledMirrorContext>)HandleMetronome);
		return methodMirrorRegistry;
	}

	private static void HandleMetronome(Metronome relic, AfterOrbChanneledMirrorContext context)
	{
		Metronome relic2 = relic;
		if (context.Player == ((RelicModel)relic2).Owner)
		{
			MetronomePredictionState metronomePredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new MetronomePredictionState(relic2));
			metronomePredictionState.OrbsChanneled++;
			if (metronomePredictionState.OrbsChanneled == ((RelicModel)relic2).DynamicVars["OrbCount"].IntValue)
			{
				context.Simulator.Damage(context.State.HittableEnemies, ((RelicModel)relic2).DynamicVars.Damage, ((RelicModel)relic2).Owner.Creature);
			}
		}
	}
}
