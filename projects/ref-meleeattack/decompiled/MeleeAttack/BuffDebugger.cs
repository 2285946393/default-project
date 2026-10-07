using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class BuffDebugger
{
	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class SubscribePatch
	{
		private static void Postfix(NCombatRoom __instance)
		{
			if (!Enabled || __instance == null)
			{
				return;
			}
			foreach (NCreature creatureNode in __instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null)
				{
					creatureNode.Entity.PowerApplied -= OnPowerApplied;
					creatureNode.Entity.PowerApplied += OnPowerApplied;
				}
			}
			Log.Info("[BuffDebugger] 已订阅所有 Creature 的 PowerApplied 事件", 2);
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
	private static class UnsubscribePatch
	{
		private static void Postfix(NCombatRoom __instance)
		{
			if (!Enabled || __instance == null)
			{
				return;
			}
			foreach (NCreature creatureNode in __instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null)
				{
					creatureNode.Entity.PowerApplied -= OnPowerApplied;
				}
			}
			Log.Info("[BuffDebugger] 已取消订阅 PowerApplied 事件", 2);
		}
	}

	private static readonly bool Enabled;

	private static void OnPowerApplied(PowerModel power)
	{
		if (Enabled && power != null)
		{
			Log.Info("[BuffDebugger] PowerAdded: " + ((object)power).GetType().FullName, 2);
		}
	}
}
