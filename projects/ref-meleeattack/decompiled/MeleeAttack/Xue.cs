using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

public static class Xue
{
	private const float FADE_IN_DURATION = 0.5f;

	private static readonly Dictionary<NCreature, int> _originalZIndices = new Dictionary<NCreature, int>();

	public static void MakeHealthBarTransparent(NCreature creatureNode)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			Control val = FindHealthBar(creatureNode);
			if (val != null)
			{
				Color modulate = ((CanvasItem)val).Modulate;
				modulate.A = 0f;
				((CanvasItem)val).Modulate = modulate;
			}
		}
	}

	public static void FadeInHealthBar(NCreature creatureNode)
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Invalid comparison between Unknown and I8
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		Control healthBar = FindHealthBar(creatureNode);
		if (healthBar == null)
		{
			return;
		}
		if (((GodotObject)healthBar).HasMeta(StringName.op_Implicit("__xue_fade_tween")))
		{
			Variant meta = ((GodotObject)healthBar).GetMeta(StringName.op_Implicit("__xue_fade_tween"), default(Variant));
			if ((long)((Variant)(ref meta)).VariantType == 24)
			{
				Tween val = ((Variant)(ref meta)).As<Tween>();
				if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
				{
					val.Kill();
				}
			}
			((GodotObject)healthBar).RemoveMeta(StringName.op_Implicit("__xue_fade_tween"));
		}
		float targetAlpha = 1f;
		if (!(Mathf.Abs(((CanvasItem)healthBar).Modulate.A - targetAlpha) < 0.001f))
		{
			Tween val2 = ((Node)healthBar).CreateTween();
			val2.SetEase((EaseType)2);
			val2.SetTrans((TransitionType)0);
			val2.TweenProperty((GodotObject)(object)healthBar, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(targetAlpha), 0.5);
			val2.Finished += delegate
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				Color modulate = ((CanvasItem)healthBar).Modulate;
				modulate.A = targetAlpha;
				((CanvasItem)healthBar).Modulate = modulate;
				((GodotObject)healthBar).RemoveMeta(StringName.op_Implicit("__xue_fade_tween"));
			};
			((GodotObject)healthBar).SetMeta(StringName.op_Implicit("__xue_fade_tween"), Variant.op_Implicit((GodotObject)(object)val2));
		}
	}

	internal static Control FindHealthBar(NCreature creatureNode)
	{
		Node nodeOrNull = (Node)(object)((Node)creatureNode).GetNodeOrNull<Control>(NodePath.op_Implicit("HealthBar"));
		if (nodeOrNull != null)
		{
			return (Control)(object)((nodeOrNull is Control) ? nodeOrNull : null);
		}
		nodeOrNull = (Node)(object)((Node)creatureNode).GetNodeOrNull<Control>(NodePath.op_Implicit("HPBar"));
		if (nodeOrNull != null)
		{
			return (Control)(object)((nodeOrNull is Control) ? nodeOrNull : null);
		}
		foreach (Node child in ((Node)creatureNode).GetChildren(false))
		{
			ProgressBar val = (ProgressBar)(object)((child is ProgressBar) ? child : null);
			if (val != null)
			{
				return (Control)(object)val;
			}
			TextureProgressBar val2 = (TextureProgressBar)(object)((child is TextureProgressBar) ? child : null);
			if (val2 != null)
			{
				return (Control)(object)val2;
			}
		}
		return null;
	}

	public static void RaiseHealthBarLayer(NCreature creatureNode, int additionalZ = 10)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		Control val = FindHealthBar(creatureNode);
		if (val == null)
		{
			return;
		}
		if (!_originalZIndices.ContainsKey(creatureNode))
		{
			_originalZIndices[creatureNode] = ((CanvasItem)val).ZIndex;
			if (!((GodotObject)val).HasMeta(StringName.op_Implicit("__xue_orig_zrel")))
			{
				((GodotObject)val).SetMeta(StringName.op_Implicit("__xue_orig_zrel"), Variant.op_Implicit(((CanvasItem)val).ZAsRelative));
			}
		}
		((CanvasItem)val).ZAsRelative = true;
		((CanvasItem)val).ZIndex = ((CanvasItem)val).ZIndex + additionalZ;
	}

	public static void RestoreHealthBarLayer(NCreature creatureNode)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		Control val = FindHealthBar(creatureNode);
		if (val != null)
		{
			if (_originalZIndices.TryGetValue(creatureNode, out var value))
			{
				((CanvasItem)val).ZIndex = value;
				_originalZIndices.Remove(creatureNode);
			}
			else
			{
				((CanvasItem)val).ZIndex = 0;
			}
			if (((GodotObject)val).HasMeta(StringName.op_Implicit("__xue_orig_zrel")))
			{
				StringName obj = StringName.op_Implicit("__xue_orig_zrel");
				Variant val2 = default(Variant);
				val2 = ((GodotObject)val).GetMeta(obj, val2);
				bool zAsRelative = ((Variant)(ref val2)).AsBool();
				((CanvasItem)val).ZAsRelative = zAsRelative;
				((GodotObject)val).RemoveMeta(StringName.op_Implicit("__xue_orig_zrel"));
			}
		}
	}

	public static List<Control> FindStatusContainers(NCreature creatureNode)
	{
		List<Control> list = new List<Control>();
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return list;
		}
		string[] array = new string[5] { "StatusEffects", "StatusContainer", "BuffContainer", "DebuffContainer", "Effects" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			Control nodeOrNull = ((Node)creatureNode).GetNodeOrNull<Control>(NodePath.op_Implicit(text));
			if (nodeOrNull != null)
			{
				list.Add(nodeOrNull);
			}
		}
		if (list.Count == 0)
		{
			foreach (Node child in ((Node)creatureNode).GetChildren(false))
			{
				Control val = (Control)(object)((child is Control) ? child : null);
				if (val != null && (((object)((Node)val).Name).ToString().IndexOf("status", StringComparison.OrdinalIgnoreCase) >= 0 || ((object)((Node)val).Name).ToString().IndexOf("effect", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					list.Add(val);
				}
			}
		}
		return list;
	}
}
