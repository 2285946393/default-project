using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;

internal static class GlassOrbMirrors
{
	public static void BeforeTurnEndOrbTrigger(GlassOrb orb, OrbMirrorContext context)
	{
		context.Simulator.TriggerOrbPassive((OrbModel)(object)orb, null);
	}

	public static void Passive(GlassOrb orb, OrbPassiveMirrorContext context)
	{
		decimal passiveVal = ((OrbModel)orb).PassiveVal;
		if (!(passiveVal <= 0m))
		{
			orb._passiveVal = Math.Max(0m, orb._passiveVal - 1m);
			Damage(orb, context, passiveVal);
		}
	}

	public static IReadOnlyList<Creature> Evoke(GlassOrb orb, OrbMirrorContext context)
	{
		if (!(((OrbModel)orb).EvokeVal <= 0m))
		{
			return Damage(orb, context, ((OrbModel)orb).EvokeVal);
		}
		return Array.Empty<Creature>();
	}

	private static IReadOnlyList<Creature> Damage(GlassOrb orb, OrbMirrorContext context, decimal value)
	{
		IReadOnlyList<Creature> hittableEnemies = context.State.HittableEnemies;
		context.Simulator.Damage(hittableEnemies, value, (ValueProp)4, ((OrbModel)orb).Owner.Creature);
		return hittableEnemies;
	}
}
