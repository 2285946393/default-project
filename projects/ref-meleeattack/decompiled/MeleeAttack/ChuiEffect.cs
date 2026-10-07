using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

public static class ChuiEffect
{
	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	public static class ChuiTriggerAnimPatch
	{
		public static void Prefix(Creature creature, string triggerName)
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Invalid comparison between Unknown and I4
			if (!SettingsUI.IsBludgeonEnabled() || creature == null || (int)creature.Side != 1 || string.IsNullOrEmpty(triggerName) || (!triggerName.StartsWith("Attack", StringComparison.OrdinalIgnoreCase) && !triggerName.Equals("heavyAttack", StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}
			string creatureId = SimpleTeleportPatch.GetCreatureId(creature);
			if (!string.Equals(creatureId, "Ironclad", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			AttackCommand value = SimpleTeleportPatch._currentAttack.Value;
			if (value != null)
			{
				AbstractModel modelSource = value.ModelSource;
				CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
				if (val != null && ((AbstractModel)val).Id.Entry.Equals("BLUDGEON", StringComparison.OrdinalIgnoreCase) && SimpleTeleportPatch.TryGetCreatureNode(creature, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node) && ShouldTrigger(creature))
				{
					PlayBludgeonEffect(node);
				}
			}
		}
	}

	private const string VFX_PATH = "res://scenes/BLUDGEON.tscn";

	private static PackedScene _vfxScene;

	private static readonly Dictionary<Creature, float> _recentlyTriggered = new Dictionary<Creature, float>();

	private static readonly object _dedupLock = new object();

	private static bool ShouldTrigger(Creature creature)
	{
		if (creature == null)
		{
			return false;
		}
		float num = (float)((double)Time.GetTicksMsec() / 1000.0);
		lock (_dedupLock)
		{
			if (_recentlyTriggered.TryGetValue(creature, out var value) && num - value < 0.2f)
			{
				return false;
			}
			_recentlyTriggered[creature] = num;
			if (_recentlyTriggered.Count > 32)
			{
				List<Creature> list = new List<Creature>();
				foreach (KeyValuePair<Creature, float> item in _recentlyTriggered)
				{
					if (num - item.Value > 1f)
					{
						list.Add(item.Key);
					}
				}
				foreach (Creature item2 in list)
				{
					_recentlyTriggered.Remove(item2);
				}
			}
			return true;
		}
	}

	private static void PlayBludgeonEffect(NCreature creatureNode)
	{
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		if (_vfxScene == null)
		{
			_vfxScene = GD.Load<PackedScene>("res://scenes/BLUDGEON.tscn");
			if (_vfxScene == null)
			{
				GD.PrintErr("[ChuiEffect] 无法加载 BLUDGEON.tscn，请检查路径");
				return;
			}
		}
		Node2D vfx = _vfxScene.Instantiate<Node2D>((GenEditState)0);
		if (vfx == null)
		{
			GD.PrintErr("[ChuiEffect] 实例化特效失败");
			return;
		}
		((Node)creatureNode).AddChild((Node)(object)vfx, false, (InternalMode)0);
		Node2D body = creatureNode.Body;
		if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			float num = ((body.Scale.X < 0f) ? (-1f) : 1f);
			float num2 = Mathf.Abs(vfx.Scale.X);
			float y = vfx.Scale.Y;
			vfx.Scale = new Vector2(num2 * num, y);
		}
		Timer val = new Timer();
		val.WaitTime = 1.0;
		val.OneShot = true;
		val.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
			{
				((Node)vfx).QueueFree();
			}
		};
		((Node)vfx).AddChild((Node)(object)val, false, (InternalMode)0);
		val.Start(-1.0);
	}
}
