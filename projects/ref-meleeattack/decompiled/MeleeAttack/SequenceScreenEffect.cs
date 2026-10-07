using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class SequenceScreenEffect
{
	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	public static class GrandFinaleSequencePatch
	{
		public static void Prefix(Creature creature, string triggerName)
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Invalid comparison between Unknown and I4
			if (!SettingsUI.IsGrandFinaleEnabled() || creature == null || (int)creature.Side != 1 || string.IsNullOrEmpty(triggerName) || (!triggerName.StartsWith("Attack", StringComparison.OrdinalIgnoreCase) && !triggerName.Equals("heavyAttack", StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}
			string creatureId = SimpleTeleportPatch.GetCreatureId(creature);
			if (!string.Equals(creatureId, "Silent", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			AttackCommand value = SimpleTeleportPatch._currentAttack.Value;
			if (value != null)
			{
				AbstractModel modelSource = value.ModelSource;
				CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
				if (val != null && ((AbstractModel)val).Id.Entry.Equals("GRAND_FINALE", StringComparison.OrdinalIgnoreCase) && ShouldTrigger(creature))
				{
					PlayShunshaEffectAsync(creature);
				}
			}
		}
	}

	private const string VFX_PATH = "res://scenes/华丽收场.tscn";

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

	public static async Task PlayShunshaEffectAsync(Creature caster = null)
	{
		if (_vfxScene == null)
		{
			_vfxScene = GD.Load<PackedScene>("res://scenes/华丽收场.tscn");
			if (_vfxScene == null)
			{
				GD.PrintErr("[SequenceScreenEffect] 无法加载 华丽收场.tscn，请检查路径");
				return;
			}
		}
		NCombatRoom room = NCombatRoom.Instance;
		if (room == null)
		{
			GD.PrintErr("[SequenceScreenEffect] 未找到 NCombatRoom");
			return;
		}
		Node2D vfx = _vfxScene.Instantiate<Node2D>((GenEditState)0);
		if (vfx == null)
		{
			GD.PrintErr("[SequenceScreenEffect] 实例化特效失败");
			return;
		}
		((Node)room).AddChild((Node)(object)vfx, false, (InternalMode)0);
		Viewport viewport = ((Node)room).GetViewport();
		Vector2 center;
		if (viewport != null)
		{
			Rect2 visibleRect = viewport.GetVisibleRect();
			center = ((Rect2)(ref visibleRect)).Size * 0.5f;
		}
		else
		{
			center = Vector2.Zero;
		}
		vfx.GlobalPosition = center;
		if (caster != null && SimpleTeleportPatch.TryGetCreatureNode(caster, out var _, out var casterNode) && casterNode != null)
		{
			Node2D visual = SimpleTeleportPatch.GetVisualNode(casterNode);
			if (visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual))
			{
				float signX = ((visual.Scale.X < 0f) ? (-1f) : 1f);
				float baseScaleX = Mathf.Abs(vfx.Scale.X);
				vfx.Scale = new Vector2(baseScaleX * signX, vfx.Scale.Y);
			}
		}
		float delay = 1.2f;
		Timer timer = new Timer();
		timer.WaitTime = delay;
		timer.OneShot = true;
		timer.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
			{
				((Node)vfx).QueueFree();
			}
		};
		((Node)vfx).AddChild((Node)(object)timer, false, (InternalMode)0);
		timer.Start(-1.0);
		await Task.CompletedTask;
	}
}
