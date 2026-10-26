using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;

internal static class LightningOrbMirrors
{
	public static void BeforeTurnEndOrbTrigger(LightningOrb orb, OrbMirrorContext context)
	{
		context.Simulator.TriggerOrbPassive((OrbModel)(object)orb, null);
	}

	public static void Passive(LightningOrb orb, OrbPassiveMirrorContext context)
	{
		Damage(orb, context, ((OrbModel)orb).PassiveVal, context.Target);
	}

	public static IReadOnlyList<Creature> Evoke(LightningOrb orb, OrbMirrorContext context)
	{
		return Damage(orb, context, ((OrbModel)orb).EvokeVal, null);
	}

	private static IReadOnlyList<Creature> Damage(LightningOrb orb, OrbMirrorContext context, decimal value, Creature? target)
	{
		OrbMirrorContext context2 = context;
		List<Creature> list = (from creature in context2.State.GetOpponentsOf(((OrbModel)orb).Owner.Creature)
			where context2.State.GetCreature(creature).IsHittable
			select creature).ToList();
		if (list.Count == 0)
		{
			return Array.Empty<Creature>();
		}
		if (target == null)
		{
			target = context2.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)list);
		}
		if (target == null)
		{
			return Array.Empty<Creature>();
		}
		IReadOnlyList<Creature> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<Creature>(target);
		context2.Simulator.Damage(readOnlyList, value, (ValueProp)4, ((OrbModel)orb).Owner.Creature);
		return readOnlyList;
	}
}
