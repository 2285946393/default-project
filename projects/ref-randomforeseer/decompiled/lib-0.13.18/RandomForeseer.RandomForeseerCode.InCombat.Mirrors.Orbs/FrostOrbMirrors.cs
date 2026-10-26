using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;

internal static class FrostOrbMirrors
{
	public static void BeforeTurnEndOrbTrigger(FrostOrb orb, OrbMirrorContext context)
	{
		context.Simulator.TriggerOrbPassive((OrbModel)(object)orb, null);
	}

	public static void Passive(FrostOrb orb, OrbPassiveMirrorContext context)
	{
		Block(orb, context, ((OrbModel)orb).PassiveVal);
	}

	public static IReadOnlyList<Creature> Evoke(FrostOrb orb, OrbMirrorContext context)
	{
		return Block(orb, context, ((OrbModel)orb).EvokeVal);
	}

	private static IReadOnlyList<Creature> Block(FrostOrb orb, OrbMirrorContext context, decimal value)
	{
		context.Simulator.GainBlock(((OrbModel)orb).Owner.Creature, value, (ValueProp)4);
		if (!((OrbModel)orb).Owner.Creature.HasPower<HibernatePower>())
		{
			return new _003C_003Ez__ReadOnlySingleElementList<Creature>(((OrbModel)orb).Owner.Creature);
		}
		IReadOnlyList<Player> players = context.State.CombatState.Players;
		foreach (Player item in players)
		{
			if (item != ((OrbModel)orb).Owner)
			{
				context.Simulator.GainBlock(item.Creature, value, (ValueProp)4);
			}
		}
		List<Creature> list = new List<Creature>();
		list.AddRange(players.Select((Player player) => player.Creature));
		return new _003C_003Ez__ReadOnlyList<Creature>(list);
	}
}
