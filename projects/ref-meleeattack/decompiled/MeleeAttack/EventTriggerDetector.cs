using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class EventTriggerDetector
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
				if (((creatureNode != null) ? creatureNode.Entity : null) != null)
				{
					Creature entity = creatureNode.Entity;
					entity.PowerApplied -= OnPowerApplied;
					entity.PowerApplied += OnPowerApplied;
					entity.PowerRemoved -= OnPowerRemoved;
					entity.PowerRemoved += OnPowerRemoved;
				}
			}
			Log.Info("[EventDetect] 已订阅所有生物的 PowerApplied/PowerRemoved 事件", 2);
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
				if (((creatureNode != null) ? creatureNode.Entity : null) != null)
				{
					creatureNode.Entity.PowerApplied -= OnPowerApplied;
					creatureNode.Entity.PowerRemoved -= OnPowerRemoved;
				}
			}
			Log.Info("[EventDetect] 已取消订阅生物事件", 2);
		}
	}

	[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
	private static class CardPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
			if (Enabled && __instance != null)
			{
				ModelId id = ((AbstractModel)__instance).Id;
				string text = ((id != null) ? id.Entry : null) ?? "unknown";
				Player owner = __instance.Owner;
				Creature target = ((owner != null) ? owner.Creature : null);
				LogEvent("CardOnPlay", target, "Card=" + text);
			}
		}
	}

	[HarmonyPatch(typeof(AttackCommand), "Execute")]
	private static class AttackExecutePatch
	{
		private static void Prefix(AttackCommand __instance)
		{
			if (Enabled)
			{
				Creature target = ((__instance != null) ? __instance.Attacker : null);
				LogEvent("AttackExecute_Begin", target);
			}
		}

		private static void Postfix(AttackCommand __instance)
		{
			if (Enabled)
			{
				Creature target = ((__instance != null) ? __instance.Attacker : null);
				LogEvent("AttackExecute_End", target);
			}
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim")]
	private static class TriggerAnimPatch
	{
		private static void Prefix(Creature creature, string triggerName, float waitTime)
		{
			if (Enabled)
			{
				LogEvent("TriggerAnim_Begin", creature, $"Trigger={triggerName}, WaitTime={waitTime}");
			}
		}

		private static void Postfix(Creature creature, string triggerName, float waitTime)
		{
			if (Enabled)
			{
				LogEvent("TriggerAnim_End", creature, "Trigger=" + triggerName);
			}
		}
	}

	private static readonly bool Enabled;

	private static readonly Stopwatch _stopwatch;

	private static void LogEvent(string eventName, Creature target = null, string extra = "")
	{
		if (Enabled)
		{
			long elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
			string value = ((target != null) ? target.Name : null) ?? "null";
			Log.Info($"[EventDetect][{elapsedMilliseconds}ms] {eventName} | Target={value} {extra}", 2);
		}
	}

	private static void OnPowerApplied(PowerModel power)
	{
		if (Enabled && power != null)
		{
			Creature target = ((power != null) ? power.Owner : null);
			LogEvent("PowerApplied", target, "Type=" + ((object)power).GetType().Name);
		}
	}

	private static void OnPowerRemoved(PowerModel power)
	{
		if (Enabled && power != null)
		{
			Creature target = ((power != null) ? power.Owner : null);
			LogEvent("PowerRemoved", target, "Type=" + ((object)power).GetType().Name);
		}
	}

	static EventTriggerDetector()
	{
		Enabled = false;
		_stopwatch = Stopwatch.StartNew();
		if (!Enabled)
		{
			return;
		}
		try
		{
			BuffMonitor.PowerStackChanged += OnPowerStackChanged;
			BuffMonitor.PowerRemoved += OnPowerRemovedMonitor;
		}
		catch
		{
		}
	}

	private static void OnPowerStackChanged(PowerModel power, int oldStack, int newStack)
	{
		if (Enabled && power != null)
		{
			Creature target = ((power != null) ? power.Owner : null);
			LogEvent("PowerStackChanged", target, $"Type={((object)power).GetType().Name}, {oldStack}->{newStack}");
		}
	}

	private static void OnPowerRemovedMonitor(PowerModel power)
	{
		if (Enabled && power != null)
		{
			Creature target = ((power != null) ? power.Owner : null);
			LogEvent("PowerRemoved(Monitor)", target, "Type=" + ((object)power).GetType().Name);
		}
	}
}
