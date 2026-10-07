using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MeleeAttack;

[HarmonyPatch]
public static class KnifeCardsAnimPatch
{
	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	[HarmonyPriority(600)]
	private static class TriggerAnimKnifePatch
	{
		private static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			if (creature == null || (int)creature.Side != 1 || !triggerName.StartsWith("Attack"))
			{
				return true;
			}
			AttackCommand value = SimpleTeleportPatch._currentAttack.Value;
			if (value == null)
			{
				return true;
			}
			AbstractModel modelSource = value.ModelSource;
			AbstractModel obj = ((modelSource is CardModel) ? modelSource : null);
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				ModelId id = obj.Id;
				obj2 = ((id != null) ? id.Entry : null);
			}
			string text = (string)obj2;
			if (string.IsNullOrEmpty(text))
			{
				return true;
			}
			string creatureId = SimpleTeleportPatch.GetCreatureId(creature);
			if (!string.Equals(creatureId, "Silent", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (!SimpleTeleportPatch._silentDisplacementExcludedCards.Contains(text))
			{
				return true;
			}
			if (text.Equals("FLECHETTES", StringComparison.OrdinalIgnoreCase))
			{
				List<Creature> aliveTargets = SimpleTeleportPatch.GetAliveTargets(value);
				int value2 = SimpleTeleportPatch._currentHitIndex.Value;
				if (value2 < aliveTargets.Count)
				{
					Creature val = aliveTargets[value2];
					if (val != null && val.IsAlive)
					{
						SpawnShivVfxAtTarget(creature, val);
					}
				}
			}
			__result = RunShivAnimAndRestoreAsync(creature, waitTime);
			return false;
		}
	}

	private static readonly MethodInfo _originalTriggerAnim = typeof(CreatureCmd).GetMethod("TriggerAnim", new Type[3]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	});

	private static void SpawnShivVfxAtTarget(Creature attacker, Creature target)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if (attacker == null || target == null)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		NCreature creatureNode = instance.GetCreatureNode(attacker);
		NCreature creatureNode2 = instance.GetCreatureNode(target);
		if (creatureNode != null && creatureNode2 != null)
		{
			Vector2 vfxSpawnPosition = creatureNode.VfxSpawnPosition;
			Vector2 vfxSpawnPosition2 = creatureNode2.VfxSpawnPosition;
			NShivThrowVfx val = NShivThrowVfx.Create(vfxSpawnPosition, vfxSpawnPosition2, Colors.White);
			if (val == null)
			{
				GD.PrintErr("[KnifeCards] NShivThrowVfx.Create 返回 null");
				return;
			}
			((CanvasItem)val).ZIndex = 1;
			((CanvasItem)val).ZAsRelative = false;
			Node val2 = (Node)(((object)instance.BackCombatVfxContainer) ?? ((object)instance));
			val2.AddChild((Node)(object)val, false, (InternalMode)0);
		}
	}

	private static async Task RunShivAnimAndRestoreAsync(Creature creature, float waitTime)
	{
		await (Task)_originalTriggerAnim.Invoke(null, new object[3] { creature, "Shiv", waitTime });
		if (SimpleTeleportPatch.TryGetCreatureNode(creature, out var _, out var playerNode))
		{
			if (SimpleTeleportPatch._originalZIndices.TryRemove(playerNode, out var zWrapper))
			{
				((CanvasItem)playerNode).ZIndex = zWrapper.Value;
			}
			Xue.FadeInHealthBar(playerNode);
		}
	}
}
