using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
public static class BloodlettingVfxPatch
{
	private const string CARD_ID = "BLOODLETTING";

	private const string VFX_PATH = "res://scenes/放血.tscn";

	private const float VFX_TOTAL_SECONDS = 1f;

	private const float VFX_FADE_IN = 0.3f;

	private const float VFX_FADE_OUT = 0.4f;

	private static readonly Color BLOOD_COLOR = new Color(0.55f, 0.05f, 0.05f, 1f);

	private const float COLOR_FADE_IN = 0.5f;

	private const float COLOR_HOLD_SECONDS = 0.6f;

	private const float COLOR_FADE_OUT = 0.2f;

	private static readonly Dictionary<NCreature, Tween> _activeTweens = new Dictionary<NCreature, Tween>();

	private static readonly Dictionary<NCreature, Color> _originalColors = new Dictionary<NCreature, Color>();

	private static readonly Dictionary<CardModel, float> _recentlyTriggered = new Dictionary<CardModel, float>();

	private static readonly object _dedupLock = new object();

	private static bool ShouldTrigger(CardModel card)
	{
		if (card == null)
		{
			return false;
		}
		float num = (float)((double)Time.GetTicksMsec() / 1000.0);
		lock (_dedupLock)
		{
			if (_recentlyTriggered.TryGetValue(card, out var value) && num - value < 0.2f)
			{
				return false;
			}
			_recentlyTriggered[card] = num;
			if (_recentlyTriggered.Count > 32)
			{
				List<CardModel> list = new List<CardModel>();
				foreach (KeyValuePair<CardModel, float> item in _recentlyTriggered)
				{
					if (num - item.Value > 1f)
					{
						list.Add(item.Key);
					}
				}
				foreach (CardModel item2 in list)
				{
					_recentlyTriggered.Remove(item2);
				}
			}
			return true;
		}
	}

	private static void Prefix(CardModel __instance)
	{
		if (!SettingsUI.IsBloodlettingEnabled() || __instance == null)
		{
			return;
		}
		ModelId id = ((AbstractModel)__instance).Id;
		string a = ((id != null) ? id.Entry : null);
		if (string.Equals(a, "BLOODLETTING", StringComparison.OrdinalIgnoreCase) && ShouldTrigger(__instance))
		{
			Player owner = __instance.Owner;
			Creature val = ((owner != null) ? owner.Creature : null);
			if (val != null && SimpleTeleportPatch.TryGetCreatureNode(val, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				PlayVfx(node);
				ApplyBloodColor(node);
			}
		}
	}

	private static void PlayVfx(NCreature creatureNode)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		PackedScene val = GD.Load<PackedScene>("res://scenes/放血.tscn");
		if (val == null)
		{
			GD.PrintErr("[BloodlettingVfx] 无法加载特效: res://scenes/放血.tscn");
			return;
		}
		Node2D vfx = val.Instantiate<Node2D>((GenEditState)0);
		if (vfx == null)
		{
			return;
		}
		((Node)creatureNode).AddChild((Node)(object)vfx, false, (InternalMode)0);
		Vector2 val2 = TargetCenter.GetCenter(creatureNode);
		if (val2 == Vector2.Zero)
		{
			val2 = ((Control)creatureNode).GlobalPosition;
		}
		vfx.GlobalPosition = val2;
		((CanvasItem)vfx).ZIndex = 5;
		((CanvasItem)vfx).ZAsRelative = false;
		Node2D body = creatureNode.Body;
		if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			float num = ((body.Scale.X < 0f) ? (-1f) : 1f);
			float num2 = Mathf.Abs(vfx.Scale.X);
			float y = vfx.Scale.Y;
			vfx.Scale = new Vector2(num2 * num, y);
		}
		float num3 = ((CanvasItem)vfx).Modulate.A;
		if (num3 <= 0f)
		{
			num3 = 1f;
		}
		((CanvasItem)vfx).Modulate = new Color(((CanvasItem)vfx).Modulate.R, ((CanvasItem)vfx).Modulate.G, ((CanvasItem)vfx).Modulate.B, 0f);
		float num4 = 0.29999998f;
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		Tween val3 = ((Node)vfx).CreateTween();
		val3.SetTrans((TransitionType)0);
		val3.SetEase((EaseType)2);
		val3.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(num3), 0.30000001192092896);
		if (num4 > 0f)
		{
			val3.TweenInterval((double)num4);
		}
		val3.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.4000000059604645);
		val3.TweenCallback(Callable.From((Action)delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
			{
				((Node)vfx).QueueFree();
			}
		}));
	}

	private static void ApplyBloodColor(NCreature node)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Node2D body = node.Body;
		if (body == null || !GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			return;
		}
		if (_activeTweens.TryGetValue(node, out var value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			value.Kill();
			_activeTweens.Remove(node);
		}
		if (!_originalColors.ContainsKey(node))
		{
			_originalColors[node] = ((CanvasItem)body).Modulate;
		}
		Color val = _originalColors[node];
		Color val2 = default(Color);
		((Color)(ref val2))._002Ector(BLOOD_COLOR.R, BLOOD_COLOR.G, BLOOD_COLOR.B, val.A);
		Tween val3 = ((Node)node).CreateTween();
		val3.SetTrans((TransitionType)0);
		val3.SetEase((EaseType)2);
		val3.TweenProperty((GodotObject)(object)body, NodePath.op_Implicit("modulate"), Variant.op_Implicit(val2), 0.5);
		val3.TweenInterval(0.6000000238418579);
		val3.TweenProperty((GodotObject)(object)body, NodePath.op_Implicit("modulate"), Variant.op_Implicit(val), 0.20000000298023224);
		val3.TweenCallback(Callable.From((Action)delegate
		{
			if (_activeTweens.ContainsKey(node))
			{
				_activeTweens.Remove(node);
			}
			if (_originalColors.ContainsKey(node))
			{
				_originalColors.Remove(node);
			}
		}));
		_activeTweens[node] = val3;
	}
}
