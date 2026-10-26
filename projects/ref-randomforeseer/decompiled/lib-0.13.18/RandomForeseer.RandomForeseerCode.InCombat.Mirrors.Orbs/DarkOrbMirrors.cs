using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;

internal static class DarkOrbMirrors
{
	public static void BeforeTurnEndOrbTrigger(DarkOrb orb, OrbMirrorContext context)
	{
		context.Simulator.TriggerOrbPassive((OrbModel)(object)orb, null);
	}

	public static void Passive(DarkOrb orb, OrbPassiveMirrorContext context)
	{
		orb._evokeVal += ((OrbModel)orb).PassiveVal;
	}

	public static IReadOnlyList<Creature> Evoke(DarkOrb orb, OrbMirrorContext context)
	{
		OrbMirrorContext context2 = context;
		Creature val = context2.State.HittableEnemies.MinBy((Creature creature) => context2.State.GetCreature(creature).CurrentHp);
		if (val == null)
		{
			return Array.Empty<Creature>();
		}
		IReadOnlyList<Creature> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<Creature>(val);
		context2.Simulator.Damage(readOnlyList, ((OrbModel)orb).EvokeVal, (ValueProp)4, ((OrbModel)orb).Owner.Creature);
		return readOnlyList;
	}
}
