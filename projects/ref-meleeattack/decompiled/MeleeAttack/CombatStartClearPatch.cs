using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class CombatStartClearPatch
{
	private static void Postfix(NCombatRoom __instance)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if (__instance == null)
		{
			return;
		}
		foreach (NCreature creatureNode in __instance.CreatureNodes)
		{
			bool? obj;
			if (creatureNode == null)
			{
				obj = null;
			}
			else
			{
				Creature entity = creatureNode.Entity;
				obj = ((entity != null) ? new bool?(entity.IsPlayer) : null);
			}
			bool? flag = obj;
			if (flag.GetValueOrDefault())
			{
				RoleColorFilter.StopBreathForNode(creatureNode);
				((CanvasItem)creatureNode).Modulate = Colors.White;
			}
		}
	}
}
