using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;

internal static class PlasmaOrbMirrors
{
	public static void Passive(PlasmaOrb orb, OrbPassiveMirrorContext context)
	{
		context.Simulator.GainEnergy(((OrbModel)orb).Owner, ((OrbModel)orb).PassiveVal);
	}

	public static IReadOnlyList<Creature> Evoke(PlasmaOrb orb, OrbMirrorContext context)
	{
		context.Simulator.GainEnergy(((OrbModel)orb).Owner, ((OrbModel)orb).EvokeVal);
		return new _003C_003Ez__ReadOnlySingleElementList<Creature>(((OrbModel)orb).Owner.Creature);
	}
}
