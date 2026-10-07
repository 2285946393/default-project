using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

public static class RoleColorFilter
{
	private const float BREATH_UP_TIME = 2f;

	private const float BREATH_DOWN_TIME = 2f;

	private static readonly Dictionary<string, Color> _targetColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
	{
		["Ironclad"] = new Color(1f, 0.5f, 0.5f, 1f),
		["Silent"] = new Color(0.5f, 1f, 0.5f, 1f),
		["Defect"] = new Color(0.4f, 0.7f, 1f, 1f),
		["Necrobinder"] = new Color(0.9f, 0.7f, 1f, 1f),
		["Regent"] = new Color(1f, 0.9f, 0.4f, 1f)
	};

	private static readonly Color _defaultTarget = new Color(0.7f, 0.7f, 0.7f, 1f);

	internal static readonly ConditionalWeakTable<NCreature, Tween> _breathTweens = new ConditionalWeakTable<NCreature, Tween>();

	public static Color GetTargetColor(Creature creature)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		object obj;
		if (creature == null)
		{
			obj = null;
		}
		else
		{
			Player player = creature.Player;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				CharacterModel character = player.Character;
				if (character == null)
				{
					obj = null;
				}
				else
				{
					ModelId id = ((AbstractModel)character).Id;
					obj = ((id != null) ? id.Entry : null);
				}
			}
		}
		string text = (string)obj;
		if (!string.IsNullOrEmpty(text) && _targetColors.TryGetValue(text, out var value))
		{
			return value;
		}
		return _defaultTarget;
	}

	public static void ApplyFilter(NCreature node, Color targetColor)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			if (_breathTweens.TryGetValue(node, out var value2) && GodotObject.IsInstanceValid((GodotObject)(object)value2))
			{
				value2.Kill();
				_breathTweens.Remove(node);
			}
			Color startColor = ((CanvasItem)node).Modulate;
			Color endColor = new Color(targetColor.R, targetColor.G, targetColor.B, startColor.A);
			Tween val = ((Node)node).CreateTween();
			val.SetLoops(0);
			val.SetTrans((TransitionType)1);
			val.SetEase((EaseType)2);
			val.TweenMethod(Callable.From<float>((Action<float>)delegate(float value)
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_007c: Unknown result type (might be due to invalid IL or missing references)
				Color modulate2 = ((CanvasItem)node).Modulate;
				modulate2.R = Mathf.Lerp(startColor.R, endColor.R, value);
				modulate2.G = Mathf.Lerp(startColor.G, endColor.G, value);
				modulate2.B = Mathf.Lerp(startColor.B, endColor.B, value);
				((CanvasItem)node).Modulate = modulate2;
			}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 2.0);
			val.TweenMethod(Callable.From<float>((Action<float>)delegate(float value)
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_007c: Unknown result type (might be due to invalid IL or missing references)
				Color modulate = ((CanvasItem)node).Modulate;
				modulate.R = Mathf.Lerp(endColor.R, startColor.R, value);
				modulate.G = Mathf.Lerp(endColor.G, startColor.G, value);
				modulate.B = Mathf.Lerp(endColor.B, startColor.B, value);
				((CanvasItem)node).Modulate = modulate;
			}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 2.0);
			_breathTweens.Add(node, val);
		}
	}

	internal static void StopBreathForNode(NCreature node)
	{
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node) && _breathTweens.TryGetValue(node, out var value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			value.Kill();
			_breathTweens.Remove(node);
		}
	}

	internal static void StopAllBreaths()
	{
		foreach (KeyValuePair<NCreature, Tween> item in (IEnumerable<KeyValuePair<NCreature, Tween>>)_breathTweens)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)item.Value))
			{
				item.Value.Kill();
			}
		}
		_breathTweens.Clear();
	}
}
