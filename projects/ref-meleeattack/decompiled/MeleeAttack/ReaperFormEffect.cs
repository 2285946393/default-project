using System.Collections.Generic;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class ReaperFormEffect
{
	private const float MIN_ALPHA = 0.2f;

	private const float MAX_ALPHA = 0.7f;

	private const float BREATH_CYCLE = 2f;

	private static readonly Dictionary<NCreature, Node2D> _activeEffects = new Dictionary<NCreature, Node2D>();

	private static readonly Dictionary<NCreature, Timer> _followTimers = new Dictionary<NCreature, Timer>();

	internal static bool IsEffectAllowed => SettingsUI.IsReaperFormEnabled();

	public static void HideEffect(NCreature node)
	{
		if (_activeEffects.TryGetValue(node, out var value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			((CanvasItem)value).Visible = false;
		}
	}

	public static void ShowEffect(NCreature node)
	{
		if (_activeEffects.TryGetValue(node, out var value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			((CanvasItem)value).Visible = true;
		}
	}

	internal static void OnPowerApplied(PowerModel power)
	{
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Expected O, but got Unknown
		if (!(power is ReaperFormPower) || !IsEffectAllowed)
		{
			return;
		}
		Creature ownerCreature = GetOwnerCreature(power);
		if (ownerCreature == null)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		NCreature node = instance.GetCreatureNode(ownerCreature);
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		RemoveEffect(node);
		PackedScene val = GD.Load<PackedScene>("res://scenes/死神待机.tscn");
		if (val == null)
		{
			GD.PrintErr("[ReaperFormEffect] 无法加载特效场景: res://scenes/死神待机.tscn");
			return;
		}
		Node2D vfx = val.Instantiate<Node2D>((GenEditState)0);
		if (vfx == null)
		{
			return;
		}
		Node val2 = (Node)(((object)instance.BackCombatVfxContainer) ?? ((object)instance));
		val2.AddChild((Node)(object)vfx, false, (InternalMode)0);
		vfx.GlobalPosition = TargetCenter.GetCenter(node);
		float baseScaleX = Mathf.Abs(vfx.Scale.X);
		float baseScaleY = vfx.Scale.Y;
		SetMouseTransparent((Node)(object)vfx);
		((CanvasItem)vfx).Modulate = new Color(1f, 1f, 1f, 0.45f);
		Tween val3 = ((Node)vfx).CreateTween();
		val3.SetLoops(0);
		val3.SetTrans((TransitionType)1);
		val3.SetEase((EaseType)2);
		val3.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0.7f), 1.0);
		val3.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0.2f), 1.0);
		Timer followTimer = new Timer();
		followTimer.WaitTime = 0.019999999552965164;
		followTimer.OneShot = false;
		followTimer.Timeout += delegate
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			if (!GodotObject.IsInstanceValid((GodotObject)(object)vfx) || !GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				followTimer.Stop();
				((Node)followTimer).QueueFree();
			}
			else
			{
				vfx.GlobalPosition = TargetCenter.GetCenter(node);
				Node2D body = node.Body;
				if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
				{
					float num = ((body.Scale.X < 0f) ? (-1f) : 1f);
					if ((float)Mathf.Sign(vfx.Scale.X) != num)
					{
						vfx.Scale = new Vector2(baseScaleX * num, baseScaleY);
					}
				}
			}
		};
		((Node)instance).AddChild((Node)(object)followTimer, false, (InternalMode)0);
		followTimer.Start(-1.0);
		_activeEffects[node] = vfx;
		_followTimers[node] = followTimer;
		((Node)vfx).TreeExited += delegate
		{
			if (_followTimers.TryGetValue(node, out var value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				((Node)value).QueueFree();
			}
			_followTimers.Remove(node);
			_activeEffects.Remove(node);
		};
	}

	private static void RemoveEffect(NCreature node)
	{
		if (_activeEffects.TryGetValue(node, out var value))
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				((Node)value).QueueFree();
			}
			_activeEffects.Remove(node);
		}
		if (_followTimers.TryGetValue(node, out var value2))
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)value2))
			{
				((Node)value2).QueueFree();
			}
			_followTimers.Remove(node);
		}
	}

	internal static void OnPowerRemoved(PowerModel power)
	{
		if (!(power is ReaperFormPower))
		{
			return;
		}
		Creature ownerCreature = GetOwnerCreature(power);
		if (ownerCreature == null)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			NCreature creatureNode = instance.GetCreatureNode(ownerCreature);
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				RemoveEffect(creatureNode);
			}
		}
	}

	private static Creature GetOwnerCreature(PowerModel power)
	{
		PropertyInfo property = ((object)power).GetType().GetProperty("Owner");
		if (property != null)
		{
			object? value = property.GetValue(power);
			return (Creature)((value is Creature) ? value : null);
		}
		property = ((object)power).GetType().GetProperty("creature");
		if (property != null)
		{
			object? value2 = property.GetValue(power);
			return (Creature)((value2 is Creature) ? value2 : null);
		}
		property = ((object)power).GetType().GetProperty("entity");
		if (property != null)
		{
			object? value3 = property.GetValue(power);
			return (Creature)((value3 is Creature) ? value3 : null);
		}
		return null;
	}

	private static void SetMouseTransparent(Node node)
	{
		Control val = (Control)(object)((node is Control) ? node : null);
		if (val != null)
		{
			val.MouseFilter = (MouseFilterEnum)2;
		}
		foreach (Node child in node.GetChildren(false))
		{
			SetMouseTransparent(child);
		}
	}

	public static void ClearAll()
	{
		foreach (KeyValuePair<NCreature, Node2D> activeEffect in _activeEffects)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)activeEffect.Value))
			{
				((Node)activeEffect.Value).QueueFree();
			}
		}
		foreach (KeyValuePair<NCreature, Timer> followTimer in _followTimers)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)followTimer.Value))
			{
				((Node)followTimer.Value).QueueFree();
			}
		}
		_activeEffects.Clear();
		_followTimers.Clear();
	}
}
