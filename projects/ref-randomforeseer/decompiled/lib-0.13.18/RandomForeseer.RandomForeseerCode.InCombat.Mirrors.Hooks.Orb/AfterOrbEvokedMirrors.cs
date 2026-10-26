using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Orb;

internal static class AfterOrbEvokedMirrors
{
	private static readonly MirrorMethodSpec Method = MirrorMethodSpec.Hook("AfterOrbEvoked", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(OrbModel),
		typeof(IEnumerable<Creature>)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterOrbEvokedMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterOrbEvokedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterOrbEvokedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterOrbEvokedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterOrbEvokedMirrorContext>(Method);
		methodMirrorRegistry.Register<ThunderPower>((Action<ThunderPower, AfterOrbEvokedMirrorContext>)HandleThunderPower);
		return methodMirrorRegistry;
	}

	private static void HandleThunderPower(ThunderPower power, AfterOrbEvokedMirrorContext context)
	{
		AfterOrbEvokedMirrorContext context2 = context;
		if (context2.Orb.Owner == ((PowerModel)power).Owner.Player && context2.Orb is LightningOrb)
		{
			List<Creature> list = context2.Targets.Where((Creature creature) => context2.State.GetCreature(creature).IsAlive).ToList();
			if (list.Count > 0)
			{
				context2.Simulator.Damage((IReadOnlyList<Creature>)list, (decimal)((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
			}
		}
	}
}
