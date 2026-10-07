using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
{
	typeof(Creature),
	typeof(string),
	typeof(float)
})]
public static class ScytheVfxPatch
{
	private static readonly Dictionary<Creature, ulong> _lastTriggerTime = new Dictionary<Creature, ulong>();

	private const ulong MIN_INTERVAL_MS = 200uL;

	private static void Prefix(Creature creature, string triggerName, float waitTime)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I4
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Expected O, but got Unknown
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Expected O, but got Unknown
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Expected O, but got Unknown
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Expected O, but got Unknown
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Expected O, but got Unknown
		//IL_048d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Expected O, but got Unknown
		if (!SettingsUI.IsReaperFormEnabled() || creature == null || string.IsNullOrEmpty(triggerName) || !triggerName.Contains("Attack", StringComparison.OrdinalIgnoreCase) || (int)creature.Side != 1 || !AnyPlayerSideHasReaperForm())
		{
			return;
		}
		ulong ticksMsec = Time.GetTicksMsec();
		if (_lastTriggerTime.TryGetValue(creature, out var value) && ticksMsec - value < 200)
		{
			return;
		}
		_lastTriggerTime[creature] = ticksMsec;
		if (!SimpleTeleportPatch.TryGetCreatureNode(creature, out var room, out var attackerNode) || attackerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			return;
		}
		Vector2 center = TargetCenter.GetCenter(attackerNode);
		if (center == Vector2.Zero)
		{
			return;
		}
		NCreature buffHolder = FindReaperFormHolder(room);
		if (buffHolder != null && GodotObject.IsInstanceValid((GodotObject)(object)buffHolder))
		{
			ReaperFormEffect.HideEffect(buffHolder);
		}
		bool isRestored = false;
		Node2D slashVfx = CreateEffect("res://scenes/血斩.tscn", (Node)(object)attackerNode, center);
		if (slashVfx != null)
		{
			ApplyFlipOnce(slashVfx);
			Timer detachSlash = new Timer();
			detachSlash.WaitTime = 0.30000001192092896;
			detachSlash.OneShot = true;
			detachSlash.Timeout += delegate
			{
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_004f: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
				if (GodotObject.IsInstanceValid((GodotObject)(object)slashVfx))
				{
					Vector2 globalPosition = slashVfx.GlobalPosition;
					float x = slashVfx.Scale.X;
					float y2 = slashVfx.Scale.Y;
					((Node)attackerNode).RemoveChild((Node)(object)slashVfx);
					((Node)room).AddChild((Node)(object)slashVfx, false, (InternalMode)0);
					slashVfx.GlobalPosition = globalPosition;
					slashVfx.Scale = new Vector2(x, y2);
					((Node)detachSlash).QueueFree();
				}
			};
			((Node)attackerNode).AddChild((Node)(object)detachSlash, false, (InternalMode)0);
			detachSlash.Start(-1.0);
			Timer destroySlash = new Timer();
			destroySlash.WaitTime = 1.0;
			destroySlash.OneShot = true;
			destroySlash.Timeout += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)slashVfx))
				{
					((Node)slashVfx).QueueFree();
				}
				((Node)destroySlash).QueueFree();
			};
			((Node)attackerNode).AddChild((Node)(object)destroySlash, false, (InternalMode)0);
			destroySlash.Start(-1.0);
		}
		Node parent2 = (Node)(((object)room.BackCombatVfxContainer) ?? ((object)room));
		Node2D actionVfx = CreateEffect("res://scenes/血斩动作.tscn", parent2, center);
		if (actionVfx != null)
		{
			ApplyFlipOnce(actionVfx);
			Timer followTimer = new Timer();
			followTimer.WaitTime = 0.019999999552965164;
			followTimer.OneShot = false;
			followTimer.Timeout += delegate
			{
				//IL_0063: Unknown result type (might be due to invalid IL or missing references)
				if (!GodotObject.IsInstanceValid((GodotObject)(object)actionVfx) || !GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
				{
					followTimer.Stop();
					((Node)followTimer).QueueFree();
				}
				else
				{
					actionVfx.GlobalPosition = TargetCenter.GetCenter(attackerNode);
				}
			};
			((Node)room).AddChild((Node)(object)followTimer, false, (InternalMode)0);
			followTimer.Start(-1.0);
			Timer stopFollow = new Timer();
			stopFollow.WaitTime = 0.30000001192092896;
			stopFollow.OneShot = true;
			stopFollow.Timeout += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)followTimer))
				{
					followTimer.Stop();
					((Node)followTimer).QueueFree();
				}
				((Node)stopFollow).QueueFree();
			};
			((Node)room).AddChild((Node)(object)stopFollow, false, (InternalMode)0);
			stopFollow.Start(-1.0);
			Timer fadeOutStart = new Timer();
			fadeOutStart.WaitTime = 0.5;
			fadeOutStart.OneShot = true;
			fadeOutStart.Timeout += delegate
			{
				//IL_005b: Unknown result type (might be due to invalid IL or missing references)
				//IL_009e: Unknown result type (might be due to invalid IL or missing references)
				if (GodotObject.IsInstanceValid((GodotObject)(object)actionVfx))
				{
					Tween val3 = ((Node)actionVfx).CreateTween();
					val3.SetTrans((TransitionType)0);
					val3.SetEase((EaseType)2);
					val3.TweenProperty((GodotObject)(object)actionVfx, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.5);
					val3.TweenCallback(Callable.From((Action)delegate
					{
						if (GodotObject.IsInstanceValid((GodotObject)(object)actionVfx))
						{
							((Node)actionVfx).QueueFree();
						}
						if (!isRestored)
						{
							isRestored = true;
							if (buffHolder != null && GodotObject.IsInstanceValid((GodotObject)(object)buffHolder))
							{
								ReaperFormEffect.ShowEffect(buffHolder);
							}
						}
					}));
					((Node)fadeOutStart).QueueFree();
				}
			};
			((Node)room).AddChild((Node)(object)fadeOutStart, false, (InternalMode)0);
			fadeOutStart.Start(-1.0);
			Timer safeRestore = new Timer();
			safeRestore.WaitTime = 1.2000000476837158;
			safeRestore.OneShot = true;
			safeRestore.Timeout += delegate
			{
				if (!isRestored)
				{
					isRestored = true;
					if (buffHolder != null && GodotObject.IsInstanceValid((GodotObject)(object)buffHolder))
					{
						ReaperFormEffect.ShowEffect(buffHolder);
					}
				}
				((Node)safeRestore).QueueFree();
			};
			((Node)room).AddChild((Node)(object)safeRestore, false, (InternalMode)0);
			safeRestore.Start(-1.0);
		}
		else if (buffHolder != null && GodotObject.IsInstanceValid((GodotObject)(object)buffHolder))
		{
			ReaperFormEffect.ShowEffect(buffHolder);
		}
		void ApplyFlipOnce(Node2D vfx)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			if (vfx != null && GodotObject.IsInstanceValid((GodotObject)(object)vfx))
			{
				float num = Mathf.Abs(vfx.Scale.X);
				float y = vfx.Scale.Y;
				float flipSign = GetFlipSign();
				vfx.Scale = new Vector2(num * flipSign, y);
			}
		}
		static Node2D CreateEffect(string path, Node parent, Vector2 globalPos)
		{
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			PackedScene val = GD.Load<PackedScene>(path);
			if (val == null)
			{
				GD.PrintErr("[ScytheVfxPatch] 无法加载特效: " + path);
				return null;
			}
			Node2D val2 = val.Instantiate<Node2D>((GenEditState)0);
			if (val2 == null)
			{
				return null;
			}
			parent.AddChild((Node)(object)val2, false, (InternalMode)0);
			val2.GlobalPosition = globalPos;
			((CanvasItem)val2).ZIndex = 5;
			((CanvasItem)val2).ZAsRelative = false;
			return val2;
		}
		float GetFlipSign()
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			Node2D body = attackerNode.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				return (body.Scale.X < 0f) ? (-1f) : 1f;
			}
			return 1f;
		}
	}

	private static bool AnyPlayerSideHasReaperForm()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return false;
		}
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			Creature val = ((creatureNode != null) ? creatureNode.Entity : null);
			if (val == null || (int)val.Side != 1 || val.Powers == null)
			{
				continue;
			}
			foreach (PowerModel power in val.Powers)
			{
				if (power is ReaperFormPower)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static NCreature FindReaperFormHolder(NCombatRoom room)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		if (room == null)
		{
			return null;
		}
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			Creature val = ((creatureNode != null) ? creatureNode.Entity : null);
			if (val == null || (int)val.Side != 1 || val.Powers == null)
			{
				continue;
			}
			foreach (PowerModel power in val.Powers)
			{
				if (power is ReaperFormPower)
				{
					return creatureNode;
				}
			}
		}
		return null;
	}
}
