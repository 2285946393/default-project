using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MeleeAttack;

public static class CardSmogEffect
{
	private class GhostlyBreathData
	{
		public Tween Tween;

		public Timer Timer;
	}

	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	public static class CardOnPlayPatch
	{
		public static void Prefix(CardModel __instance)
		{
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Invalid comparison between Unknown and I4
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0271: Unknown result type (might be due to invalid IL or missing references)
			//IL_0276: Unknown result type (might be due to invalid IL or missing references)
			//IL_025a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0179: Unknown result type (might be due to invalid IL or missing references)
			//IL_017e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
			if (!SettingsUI.IsWraithFormEnabled())
			{
				return;
			}
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
			if (obj == null || !((AbstractModel)__instance).Id.Entry.Equals("WRAITH_FORM", StringComparison.OrdinalIgnoreCase) || !ShouldTrigger(__instance))
			{
				return;
			}
			Player owner = __instance.Owner;
			Creature val = ((owner != null) ? owner.Creature : null);
			if (val == null || (int)val.Side != 1 || !SimpleTeleportPatch.TryGetCreatureNode(val, out var _, out var node) || node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				return;
			}
			if (!((GodotObject)node).HasMeta(StringName.op_Implicit("__mirror_effect_added")))
			{
				PackedScene mirrorScene = _mirrorScene;
				Node val2 = ((mirrorScene != null) ? mirrorScene.Instantiate((GenEditState)0) : null);
				if (val2 != null)
				{
					((Node)node).AddChild(val2, false, (InternalMode)0);
					Node2D val3 = (Node2D)(object)((val2 is Node2D) ? val2 : null);
					if (val3 != null)
					{
						val3.Position = MIRROR_POSITION_OFFSET;
					}
					else
					{
						Control val4 = (Control)(object)((val2 is Control) ? val2 : null);
						if (val4 != null)
						{
							val4.Position = MIRROR_POSITION_OFFSET;
						}
					}
					((GodotObject)val2).SetMeta(StringName.op_Implicit("__effect_offset"), Variant.op_Implicit(MIRROR_POSITION_OFFSET));
					SetMouseTransparent(val2);
					SetNodeAlphaRecursive(val2, 0.6f);
					CanvasItem val5 = (CanvasItem)(object)((val2 is CanvasItem) ? val2 : null);
					if (val5 != null)
					{
						val5.ZIndex = 1;
						val5.ZAsRelative = true;
					}
					((GodotObject)node).SetMeta(StringName.op_Implicit("__mirror_effect_added"), Variant.op_Implicit(true));
					((GodotObject)node).SetMeta(StringName.op_Implicit("__mirror_effect_ref"), Variant.op_Implicit((GodotObject)(object)val2));
				}
			}
			if (!((GodotObject)node).HasMeta(StringName.op_Implicit("__ghostly_follower_added")))
			{
				PackedScene ghostlyScene = _ghostlyScene;
				Node val6 = ((ghostlyScene != null) ? ghostlyScene.Instantiate((GenEditState)0) : null);
				if (val6 != null)
				{
					((Node)node).AddChild(val6, false, (InternalMode)0);
					Node2D val7 = (Node2D)(object)((val6 is Node2D) ? val6 : null);
					if (val7 != null)
					{
						val7.Position = GHOSTLY_POSITION_OFFSET;
					}
					((GodotObject)val6).SetMeta(StringName.op_Implicit("__effect_offset"), Variant.op_Implicit(GHOSTLY_POSITION_OFFSET));
					SetNodeAlphaRecursive(val6, 1f);
					SetMouseTransparent(val6);
					CanvasItem val8 = (CanvasItem)(object)((val6 is CanvasItem) ? val6 : null);
					if (val8 != null)
					{
						val8.ZIndex = 2;
						val8.ZAsRelative = true;
					}
					((GodotObject)node).SetMeta(StringName.op_Implicit("__ghostly_follower_added"), Variant.op_Implicit(true));
					((GodotObject)node).SetMeta(StringName.op_Implicit("__ghostly_effect_ref"), Variant.op_Implicit((GodotObject)(object)val6));
					StartGhostlyBreath(node, val6);
				}
			}
			Xue.RaiseHealthBarLayer(node, 3);
		}
	}

	private const string MIRROR_META_KEY = "__mirror_effect_added";

	private const string GHOSTLY_FOLLOWER_META_KEY = "__ghostly_follower_added";

	private const string MIRROR_REF_KEY = "__mirror_effect_ref";

	private const string GHOSTLY_REF_KEY = "__ghostly_effect_ref";

	private const string MIRROR_VFX_PATH = "res://scenes/vfx/whole_screen/mirror_vfx.tscn";

	private const string GHOSTLY_VFX_PATH = "res://scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_2d_front.tscn";

	private const float MIRROR_ALPHA = 0.6f;

	private const float GHOSTLY_ALPHA = 0.8f;

	private static readonly Vector2 MIRROR_POSITION_OFFSET;

	private static readonly Vector2 GHOSTLY_POSITION_OFFSET;

	private static readonly PackedScene _mirrorScene;

	private static readonly PackedScene _ghostlyScene;

	private const string EFFECT_OFFSET_KEY = "__effect_offset";

	private const int MIRROR_RELATIVE_Z = 1;

	private const int GHOSTLY_RELATIVE_Z = 2;

	private const int HEALTH_BAR_RAISE_Z = 3;

	private const float BREATH_UP_TIME = 2f;

	private const float BREATH_DOWN_TIME = 2f;

	private const float BREATH_MAX_ALPHA = 0.7f;

	private const float BREATH_MIN_ALPHA = 0.1f;

	private const float FADE_OUT_DURATION = 2f;

	private static readonly ConditionalWeakTable<NCreature, GhostlyBreathData> _breathData;

	private static readonly Dictionary<CardModel, float> _recentlyTriggered;

	private static readonly object _dedupLock;

	static CardSmogEffect()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		MIRROR_POSITION_OFFSET = new Vector2(-400f, -800f);
		GHOSTLY_POSITION_OFFSET = new Vector2(0f, -120f);
		_mirrorScene = GD.Load<PackedScene>("res://scenes/vfx/whole_screen/mirror_vfx.tscn");
		_ghostlyScene = GD.Load<PackedScene>("res://scenes/vfx/vfx_ghostly_power_up/vfx_ghostly_power_up_2d_front.tscn");
		_breathData = new ConditionalWeakTable<NCreature, GhostlyBreathData>();
		_recentlyTriggered = new Dictionary<CardModel, float>();
		_dedupLock = new object();
		CombatManager.Instance.CombatWon += OnCombatWon;
	}

	private static void OnCombatWon(CombatRoom room)
	{
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (val == null)
		{
			return;
		}
		IRunState runState = val.RunState;
		if (runState == null)
		{
			return;
		}
		Player me = LocalContext.GetMe((IPlayerCollection)(object)runState);
		if (me != null)
		{
			Creature creature = me.Creature;
			if (creature != null && SimpleTeleportPatch.TryGetCreatureNode(creature, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				ClearAllEffects(node);
			}
		}
	}

	public static void ClearAllEffects(NCreature creatureNode)
	{
		if (creatureNode != null)
		{
			RemoveMirrorEffect(creatureNode);
			RemoveGhostlyEffect(creatureNode);
			Xue.RestoreHealthBarLayer(creatureNode);
		}
	}

	public static void RestoreHealthBarLayerOnly(NCreature creatureNode)
	{
		Xue.RestoreHealthBarLayer(creatureNode);
	}

	public static void HideMirrorEffect(NCreature creatureNode)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) || !((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__mirror_effect_ref")))
		{
			return;
		}
		StringName obj = StringName.op_Implicit("__mirror_effect_ref");
		Variant val = default(Variant);
		val = ((GodotObject)creatureNode).GetMeta(obj, val);
		Node val2 = ((Variant)(ref val)).As<Node>();
		if (GodotObject.IsInstanceValid((GodotObject)(object)val2) && !((GodotObject)val2).IsQueuedForDeletion())
		{
			CanvasItem val3 = (CanvasItem)(object)((val2 is CanvasItem) ? val2 : null);
			if (val3 != null)
			{
				val3.Visible = false;
			}
		}
	}

	public static void ShowMirrorEffect(NCreature creatureNode)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) || !((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__mirror_effect_ref")))
		{
			return;
		}
		StringName obj = StringName.op_Implicit("__mirror_effect_ref");
		Variant val = default(Variant);
		val = ((GodotObject)creatureNode).GetMeta(obj, val);
		Node val2 = ((Variant)(ref val)).As<Node>();
		if (GodotObject.IsInstanceValid((GodotObject)(object)val2) && !((GodotObject)val2).IsQueuedForDeletion())
		{
			CanvasItem val3 = (CanvasItem)(object)((val2 is CanvasItem) ? val2 : null);
			if (val3 != null)
			{
				val3.Visible = true;
			}
		}
	}

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

	private static void SetNodeAlphaRecursive(Node node, float alpha)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		CanvasItem val = (CanvasItem)(object)((node is CanvasItem) ? node : null);
		if (val != null)
		{
			Color modulate = val.Modulate;
			modulate.A = alpha;
			val.Modulate = modulate;
		}
		foreach (Node child in node.GetChildren(false))
		{
			SetNodeAlphaRecursive(child, alpha);
		}
	}

	private static void StartGhostlyBreath(NCreature creatureNode, Node ghostlyNode)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		if (ghostlyNode != null && GodotObject.IsInstanceValid((GodotObject)(object)ghostlyNode))
		{
			if (!_breathData.TryGetValue(creatureNode, out var value))
			{
				value = new GhostlyBreathData();
				_breathData.Add(creatureNode, value);
			}
			StopGhostlyBreath(creatureNode);
			SetNodeAlphaRecursive(ghostlyNode, 1f);
			Tween val = ghostlyNode.CreateTween();
			val.SetLoops(0);
			val.SetTrans((TransitionType)1);
			val.SetEase((EaseType)2);
			val.TweenProperty((GodotObject)(object)ghostlyNode, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0.7f), 2.0);
			val.TweenProperty((GodotObject)(object)ghostlyNode, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0.1f), 2.0);
			value.Tween = val;
			value.Timer = null;
		}
	}

	private static void StopGhostlyBreath(NCreature creatureNode)
	{
		if (_breathData.TryGetValue(creatureNode, out var value))
		{
			if (value.Tween != null && GodotObject.IsInstanceValid((GodotObject)(object)value.Tween))
			{
				value.Tween.Kill();
				value.Tween = null;
			}
			if (value.Timer != null && GodotObject.IsInstanceValid((GodotObject)(object)value.Timer))
			{
				value.Timer.Stop();
				((Node)value.Timer).QueueFree();
				value.Timer = null;
			}
			_breathData.Remove(creatureNode);
		}
	}

	private static void RemoveMirrorEffect(NCreature creatureNode)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null)
		{
			return;
		}
		if (((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__mirror_effect_ref")))
		{
			StringName obj = StringName.op_Implicit("__mirror_effect_ref");
			Variant val = default(Variant);
			val = ((GodotObject)creatureNode).GetMeta(obj, val);
			Node val2 = ((Variant)(ref val)).As<Node>();
			if (GodotObject.IsInstanceValid((GodotObject)(object)val2) && !((GodotObject)val2).IsQueuedForDeletion())
			{
				val2.QueueFree();
			}
			((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__mirror_effect_ref"));
		}
		if (((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__mirror_effect_added")))
		{
			((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__mirror_effect_added"));
		}
	}

	private static void RemoveGhostlyEffect(NCreature creatureNode)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null)
		{
			return;
		}
		StopGhostlyBreath(creatureNode);
		if (((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__ghostly_effect_ref")))
		{
			StringName obj = StringName.op_Implicit("__ghostly_effect_ref");
			Variant val = default(Variant);
			val = ((GodotObject)creatureNode).GetMeta(obj, val);
			Node val2 = ((Variant)(ref val)).As<Node>();
			if (GodotObject.IsInstanceValid((GodotObject)(object)val2) && !((GodotObject)val2).IsQueuedForDeletion())
			{
				val2.QueueFree();
			}
			((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__ghostly_effect_ref"));
		}
		if (((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__ghostly_follower_added")))
		{
			((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__ghostly_follower_added"));
		}
	}
}
