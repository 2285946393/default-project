using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class AlphaBreath
{
	private class BreathData
	{
		public Tween LoopTween;

		public Tween FadeTween;

		public bool IsFadingOut;

		public bool IsCleaningUp;

		public Type PowerType;
	}

	private const float BREATH_UP_TIME = 2f;

	private const float BREATH_DOWN_TIME = 2f;

	private const float MIN_ALPHA = 0.1f;

	private const float MAX_ALPHA = 0.7f;

	private const float FADE_OUT_DURATION = 2f;

	private static readonly ConditionalWeakTable<NCreature, BreathData> _breathTable;

	static AlphaBreath()
	{
		_breathTable = new ConditionalWeakTable<NCreature, BreathData>();
		BuffMonitor.PowerRemoved += OnPowerRemoved;
		BuffMonitor.PowerStackChanged += OnPowerStackChanged;
	}

	private static void OnPowerRemoved(PowerModel power)
	{
		if (!(power is IntangiblePower) && !(power is WraithFormPower))
		{
			return;
		}
		Creature ownerCreature = GetOwnerCreature(power);
		if (ownerCreature != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(ownerCreature) : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				FadeOutBreath(val);
			}
		}
	}

	private static void OnPowerStackChanged(PowerModel power, int oldStack, int newStack)
	{
		if (newStack != 0 || (!(power is IntangiblePower) && !(power is WraithFormPower)))
		{
			return;
		}
		Creature ownerCreature = GetOwnerCreature(power);
		if (ownerCreature != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(ownerCreature) : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				FadeOutBreath(val);
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

	private static void StartBreath(NCreature node, Type powerType)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			ForceCleanup(node);
			((CanvasItem)node).Modulate = new Color(((CanvasItem)node).Modulate, 1f);
			BreathData breathData = new BreathData
			{
				PowerType = powerType
			};
			_breathTable.Add(node, breathData);
			Tween val = ((Node)node).CreateTween();
			val.SetLoops(0);
			val.SetTrans((TransitionType)1);
			val.SetEase((EaseType)2);
			val.TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0.7f), 2.0);
			val.TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0.1f), 2.0);
			breathData.LoopTween = val;
		}
	}

	private static void FadeOutBreath(NCreature node)
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node) || !_breathTable.TryGetValue(node, out var value) || value.IsFadingOut || value.IsCleaningUp)
		{
			return;
		}
		value.IsFadingOut = true;
		if (value.LoopTween != null && GodotObject.IsInstanceValid((GodotObject)(object)value.LoopTween))
		{
			value.LoopTween.Kill();
			value.LoopTween = null;
		}
		float a = ((CanvasItem)node).Modulate.A;
		if (a < 0.99f)
		{
			Tween val = ((Node)node).CreateTween();
			val.SetTrans((TransitionType)0);
			val.SetEase((EaseType)2);
			val.TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 2.0);
			value.FadeTween = val;
			val.TweenCallback(Callable.From((Action)delegate
			{
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0026: Unknown result type (might be due to invalid IL or missing references)
				if (GodotObject.IsInstanceValid((GodotObject)(object)node))
				{
					((CanvasItem)node).Modulate = new Color(((CanvasItem)node).Modulate, 1f);
				}
				ForceCleanup(node);
			}));
		}
		else
		{
			((CanvasItem)node).Modulate = new Color(((CanvasItem)node).Modulate, 1f);
			ForceCleanup(node);
		}
	}

	private static void ForceCleanup(NCreature node)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		if (_breathTable.TryGetValue(node, out var value))
		{
			if (value.IsCleaningUp)
			{
				return;
			}
			value.IsCleaningUp = true;
			if (value.LoopTween != null && GodotObject.IsInstanceValid((GodotObject)(object)value.LoopTween))
			{
				value.LoopTween.Kill();
				value.LoopTween = null;
			}
			if (value.FadeTween != null && GodotObject.IsInstanceValid((GodotObject)(object)value.FadeTween))
			{
				value.FadeTween.Kill();
				value.FadeTween = null;
			}
			_breathTable.Remove(node);
		}
		((CanvasItem)node).Modulate = new Color(((CanvasItem)node).Modulate, 1f);
	}

	internal static void StopAllBreaths()
	{
		List<NCreature> list = new List<NCreature>();
		foreach (KeyValuePair<NCreature, BreathData> item in (IEnumerable<KeyValuePair<NCreature, BreathData>>)_breathTable)
		{
			list.Add(item.Key);
		}
		foreach (NCreature item2 in list)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)item2))
			{
				ForceCleanup(item2);
			}
		}
		_breathTable.Clear();
	}

	internal static void OnCreaturePowerApplied(PowerModel power)
	{
		if ((!(power is IntangiblePower) && !(power is WraithFormPower)) || !SettingsUI.IsIntangibleEnabled())
		{
			return;
		}
		Creature ownerCreature = GetOwnerCreature(power);
		if (ownerCreature != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(ownerCreature) : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				StartBreath(val, ((object)power).GetType());
			}
		}
	}
}
