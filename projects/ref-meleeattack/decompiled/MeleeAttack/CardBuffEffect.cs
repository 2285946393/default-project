using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class CardBuffEffect
{
	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class CardOnPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Invalid comparison between Unknown and I4
			if (!SettingsUI.IsThunderStormEnabled())
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
			string text = (string)obj;
			if (string.IsNullOrEmpty(text) || (!text.Equals("THUNDER", StringComparison.OrdinalIgnoreCase) && !text.Equals("STORM", StringComparison.OrdinalIgnoreCase)) || !ShouldTrigger(__instance))
			{
				return;
			}
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				return;
			}
			Creature val = null;
			foreach (NCreature creatureNode2 in instance.CreatureNodes)
			{
				if (creatureNode2 != null)
				{
					Creature entity = creatureNode2.Entity;
					if ((int)((entity != null) ? new CombatSide?(entity.Side) : null).GetValueOrDefault() == 1)
					{
						val = creatureNode2.Entity;
						break;
					}
				}
			}
			if (val != null)
			{
				NCreature creatureNode = instance.GetCreatureNode(val);
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					OnCardPlayed(creatureNode);
				}
			}
		}
	}

	private const string BUFF_TRIGGER_COUNT_KEY = "__buff_trigger_count";

	private const string BUFF_CONTAINER_META_KEY = "__buff_vfx_container";

	private const string BUFF_TIMER_META_KEY = "__buff_timer";

	private const string BUFF_VFX_PATH_1 = "res://scenes/vfx/lightning/vfx_lightning_flipbook_1.tscn";

	private const string BUFF_VFX_PATH_2 = "res://scenes/vfx/lightning/vfx_lightning_flipbook_2.tscn";

	private const string BUFF_DEFECT_ENERGY_PATH = "res://scenes/vfx/energy/defect/defect_energy_vfx_front.tscn";

	private const float BUFF_REPEAT_INTERVAL = 0.5f;

	private const int BUFF_Z_INDEX = 0;

	private static readonly Vector2 BUFF_POSITION_OFFSET = new Vector2(0f, -150f);

	private const float BUFF_OPACITY = 1f;

	private static readonly PackedScene _buffScene1 = GD.Load<PackedScene>("res://scenes/vfx/lightning/vfx_lightning_flipbook_1.tscn");

	private static readonly PackedScene _buffScene2 = GD.Load<PackedScene>("res://scenes/vfx/lightning/vfx_lightning_flipbook_2.tscn");

	private static readonly PackedScene _defectEnergyScene = GD.Load<PackedScene>("res://scenes/vfx/energy/defect/defect_energy_vfx_front.tscn");

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

	private static void SetZIndexRecursive(Node node, int zIndex, bool zAsRelative)
	{
		CanvasItem val = (CanvasItem)(object)((node is CanvasItem) ? node : null);
		if (val != null)
		{
			val.ZIndex = zIndex;
			val.ZAsRelative = zAsRelative;
		}
		foreach (Node child in node.GetChildren(false))
		{
			SetZIndexRecursive(child, zIndex, zAsRelative);
		}
	}

	private static void SetNodeOpacityRecursive(Node node, float opacity)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		CanvasItem val = (CanvasItem)(object)((node is CanvasItem) ? node : null);
		if (val != null)
		{
			Color modulate = val.Modulate;
			modulate.A = opacity;
			val.Modulate = modulate;
		}
		foreach (Node child in node.GetChildren(false))
		{
			SetNodeOpacityRecursive(child, opacity);
		}
	}

	private static void StartParticles(Node node)
	{
		PropertyInfo property = ((object)node).GetType().GetProperty("Emitting");
		if (property != null && property.CanWrite)
		{
			property.SetValue(node, true);
		}
		foreach (Node child in node.GetChildren(false))
		{
			StartParticles(child);
		}
	}

	private static Node CreateEffectInstance(bool useDefectEnergy)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (useDefectEnergy)
		{
			PackedScene defectEnergyScene = _defectEnergyScene;
			Node val = ((defectEnergyScene != null) ? defectEnergyScene.Instantiate((GenEditState)0) : null);
			if (val != null)
			{
				SetZIndexRecursive(val, 0, zAsRelative: true);
				SetMouseTransparent(val);
				SetNodeOpacityRecursive(val, 1f);
				StartParticles(val);
			}
			return val;
		}
		Node2D val2 = new Node2D();
		((Node)val2).Name = StringName.op_Implicit("LightningContainer");
		val2.Position = Vector2.Zero;
		PackedScene buffScene = _buffScene1;
		Node val3 = ((buffScene != null) ? buffScene.Instantiate((GenEditState)0) : null);
		if (val3 != null)
		{
			((Node)val2).AddChild(val3, false, (InternalMode)0);
			SetZIndexRecursive(val3, 0, zAsRelative: true);
			SetMouseTransparent(val3);
			SetNodeOpacityRecursive(val3, 1f);
			StartParticles(val3);
		}
		PackedScene buffScene2 = _buffScene2;
		Node val4 = ((buffScene2 != null) ? buffScene2.Instantiate((GenEditState)0) : null);
		if (val4 != null)
		{
			((Node)val2).AddChild(val4, false, (InternalMode)0);
			SetZIndexRecursive(val4, 0, zAsRelative: true);
			SetMouseTransparent(val4);
			SetNodeOpacityRecursive(val4, 1f);
			StartParticles(val4);
		}
		return (Node)(object)val2;
	}

	private static Node2D EnsureContainerExists(NCreature creatureNode)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return null;
		}
		Node2D val = null;
		foreach (Node child in ((Node)creatureNode).GetChildren(false))
		{
			Node2D val2 = (Node2D)(object)((child is Node2D) ? child : null);
			if (val2 != null && ((Node)val2).Name == StringName.op_Implicit("BuffEffectContainer"))
			{
				val = val2;
				break;
			}
		}
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val) && !((GodotObject)val).IsQueuedForDeletion())
		{
			return val;
		}
		Node2D val3 = new Node2D();
		((Node)val3).Name = StringName.op_Implicit("BuffEffectContainer");
		val3.Position = BUFF_POSITION_OFFSET;
		SetZIndexRecursive((Node)(object)val3, 0, zAsRelative: true);
		((Node)creatureNode).AddChild((Node)(object)val3, false, (InternalMode)0);
		return val3;
	}

	private static void RefreshEffectContent(Node2D container, bool useDefectEnergy)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		if (container == null || !GodotObject.IsInstanceValid((GodotObject)(object)container))
		{
			return;
		}
		foreach (Node child in ((Node)container).GetChildren(false))
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)child) && !((GodotObject)child).IsQueuedForDeletion())
			{
				child.QueueFree();
			}
		}
		Node val = CreateEffectInstance(useDefectEnergy);
		if (val != null)
		{
			((Node)container).AddChild(val, false, (InternalMode)0);
			Node2D val2 = (Node2D)(object)((val is Node2D) ? val : null);
			if (val2 != null)
			{
				val2.Position = Vector2.Zero;
			}
		}
	}

	private static void UpdateBuffEffect(NCreature creatureNode, int triggerCount)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		bool flag = triggerCount >= 2;
		if (((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__buff_timer")))
		{
			NCreature obj = creatureNode;
			StringName obj2 = StringName.op_Implicit("__buff_timer");
			Variant val = default(Variant);
			val = ((GodotObject)obj).GetMeta(obj2, val);
			Timer val2 = ((Variant)(ref val)).As<Timer>();
			if (GodotObject.IsInstanceValid((GodotObject)(object)val2) && !((GodotObject)val2).IsQueuedForDeletion())
			{
				((Node)val2).QueueFree();
			}
			((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__buff_timer"));
		}
		Node2D val3 = EnsureContainerExists(creatureNode);
		if (val3 == null)
		{
			return;
		}
		RefreshEffectContent(val3, flag);
		Timer timer = new Timer();
		timer.WaitTime = 0.5;
		timer.OneShot = false;
		timer.Autostart = true;
		bool currentMode = flag;
		timer.Timeout += delegate
		{
			if (!GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				((Node)timer).QueueFree();
			}
			else
			{
				Node2D val4 = null;
				foreach (Node child in ((Node)creatureNode).GetChildren(false))
				{
					Node2D val5 = (Node2D)(object)((child is Node2D) ? child : null);
					if (val5 != null && ((Node)val5).Name == StringName.op_Implicit("BuffEffectContainer"))
					{
						val4 = val5;
						break;
					}
				}
				if (val4 == null || !GodotObject.IsInstanceValid((GodotObject)(object)val4))
				{
					val4 = EnsureContainerExists(creatureNode);
					if (val4 == null)
					{
						return;
					}
				}
				RefreshEffectContent(val4, currentMode);
			}
		};
		((Node)creatureNode).AddChild((Node)(object)timer, false, (InternalMode)0);
		((GodotObject)creatureNode).SetMeta(StringName.op_Implicit("__buff_timer"), Variant.op_Implicit((GodotObject)(object)timer));
	}

	private static void OnCardPlayed(NCreature creatureNode)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			int num = 0;
			if (((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__buff_trigger_count")))
			{
				StringName obj = StringName.op_Implicit("__buff_trigger_count");
				Variant val = default(Variant);
				val = ((GodotObject)creatureNode).GetMeta(obj, val);
				num = ((Variant)(ref val)).AsInt32();
			}
			num++;
			((GodotObject)creatureNode).SetMeta(StringName.op_Implicit("__buff_trigger_count"), Variant.op_Implicit(num));
			UpdateBuffEffect(creatureNode, num);
		}
	}
}
