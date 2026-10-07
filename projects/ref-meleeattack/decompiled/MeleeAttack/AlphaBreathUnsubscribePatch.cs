using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
public static class AlphaBreathUnsubscribePatch
{
	private static void Postfix(NCombatRoom __instance)
	{
		if (__instance == null)
		{
			return;
		}
		foreach (NCreature creatureNode in __instance.CreatureNodes)
		{
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null)
			{
				creatureNode.Entity.PowerApplied -= AlphaBreath.OnCreaturePowerApplied;
			}
		}
		AlphaBreath.StopAllBreaths();
	}
}
