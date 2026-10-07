using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class FilterTrigger
{
	public static void StartFilter(Creature playerCreature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null && playerCreature != null)
		{
			DistortionFilter.Create(instance, playerCreature);
		}
	}
}
