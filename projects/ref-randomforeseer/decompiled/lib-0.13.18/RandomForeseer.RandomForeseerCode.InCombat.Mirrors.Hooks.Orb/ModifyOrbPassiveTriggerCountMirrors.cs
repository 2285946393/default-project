using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Orb;

internal static class ModifyOrbPassiveTriggerCountMirrors
{
	private static readonly MirrorMethodSpec Method = MirrorMethodSpec.Hook("ModifyOrbPassiveTriggerCounts", new Type[2]
	{
		typeof(OrbModel),
		typeof(int)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyOrbPassiveTriggerCountMirrorContext, int> Registry = CreateRegistry();

	public static int Invoke(AbstractModel listener, ModifyOrbPassiveTriggerCountMirrorContext context)
	{
		return Registry.Invoke(listener, context, context.TriggerCount).Value;
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyOrbPassiveTriggerCountMirrorContext, int> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyOrbPassiveTriggerCountMirrorContext, int> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyOrbPassiveTriggerCountMirrorContext, int>(Method);
		methodMirrorRegistry.Register<GoldPlatedCables>((Func<GoldPlatedCables, ModifyOrbPassiveTriggerCountMirrorContext, int>)HandleGoldPlatedCables);
		return methodMirrorRegistry;
	}

	private static int HandleGoldPlatedCables(GoldPlatedCables relic, ModifyOrbPassiveTriggerCountMirrorContext context)
	{
		if (context.Orb.Owner == ((RelicModel)relic).Owner)
		{
			IReadOnlyList<OrbModel> orbs = context.State.GetPlayerCombatState(((RelicModel)relic).Owner).OrbQueue.Orbs;
			if (orbs != null && orbs.Count >= 1)
			{
				OrbModel val = orbs[0];
				if (context.Orb == val)
				{
					return context.TriggerCount + 1;
				}
			}
		}
		return context.TriggerCount;
	}
}
