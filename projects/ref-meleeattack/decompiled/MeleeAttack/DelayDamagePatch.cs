using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

[HarmonyPatch]
public static class DelayDamagePatch
{
	[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
	private static class CardOnPlayPatch
	{
		public static void Prefix(CardModel __instance)
		{
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				ModelId id = ((AbstractModel)__instance).Id;
				obj = ((id != null) ? id.Entry : null);
			}
			if ((string?)obj == "MURDER")
			{
				_isMurderPending = true;
			}
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	[HarmonyPriority(600)]
	private static class TriggerAnimPatch
	{
		public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			if (creature == null || (int)creature.Side != 1 || !triggerName.StartsWith("Attack") || !_isMurderPending)
			{
				return true;
			}
			_isMurderPending = false;
			__result = RunDelayedAttack(creature, triggerName, waitTime);
			return false;
		}

		private static async Task RunDelayedAttack(Creature creature, string triggerName, float waitTime)
		{
			if (_originalTriggerAnim != null)
			{
				_originalTriggerAnim.Invoke(null, new object[3] { creature, triggerName, waitTime });
			}
			else
			{
				CreatureCmd.TriggerAnim(creature, triggerName, waitTime);
			}
			float totalWait = waitTime + 0f;
			await Task.Delay((int)(totalWait * 1000f));
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatResetPatch
	{
		public static void Postfix()
		{
			_isMurderPending = false;
		}
	}

	private const float EXTRA_DELAY = 0f;

	private static bool _isMurderPending = false;

	private static readonly MethodInfo? _originalTriggerAnim = AccessTools.Method(typeof(CreatureCmd), "TriggerAnim", new Type[3]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	}, (Type[])null);
}
