using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class CardDemonEffect
{
	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	public static class DemonCardPatch
	{
		public static void Prefix(CardModel __instance)
		{
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Invalid comparison between Unknown and I4
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			if (!SettingsUI.IsDemonFormEnabled())
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
			if (obj != null && ((AbstractModel)__instance).Id.Entry.Equals("DEMON_FORM", StringComparison.OrdinalIgnoreCase) && ShouldTrigger(__instance))
			{
				Player owner = __instance.Owner;
				Creature val = ((owner != null) ? owner.Creature : null);
				if (val != null && (int)val.Side == 1 && SimpleTeleportPatch.TryGetCreatureNode(val, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
				{
					AddMushroomEffect(node);
					AddOneShotEffectToRoot(_petalsScene, (Node)(object)node, PETALS_OFFSET, 20);
					AddOneShotEffectToRoot(_fireBurstScene, (Node)(object)node, FIRE_BURST_OFFSET, 20);
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CleanupPatch
	{
		private static void Postfix()
		{
			ClearAllEffects();
		}
	}

	private const string DEMON_VFX_PATH = "res://scenes/vfx/events/hungry_for_mushrooms_vfx.tscn";

	private const string PETALS_VFX_PATH = "res://scenes/vfx/grand_finale/vfx_grand_finale_petals.tscn";

	private const string FIRE_BURST_VFX_PATH = "res://scenes/vfx/fire_impact/vfx_fire_burst_center_flipbook.tscn";

	private static readonly Vector2 MUSHROOM_OFFSET = new Vector2(-670f, -650f);

	private static readonly Vector2 PETALS_OFFSET = new Vector2(0f, -300f);

	private static readonly Vector2 FIRE_BURST_OFFSET = new Vector2(0f, -250f);

	private const int ONE_SHOT_Z_INDEX = 20;

	private const float BREATH_MIN_ALPHA = 0.2f;

	private const float BREATH_MAX_ALPHA = 0.6f;

	private const float BREATH_CYCLE_DURATION = 4f;

	private const float BREATH_SIZE_MULTIPLIER = 1.3f;

	private static readonly PackedScene _demonScene = GD.Load<PackedScene>("res://scenes/vfx/events/hungry_for_mushrooms_vfx.tscn");

	private static readonly PackedScene _petalsScene = GD.Load<PackedScene>("res://scenes/vfx/grand_finale/vfx_grand_finale_petals.tscn");

	private static readonly PackedScene _fireBurstScene = GD.Load<PackedScene>("res://scenes/vfx/fire_impact/vfx_fire_burst_center_flipbook.tscn");

	private static readonly Dictionary<NCreature, Node> _mushroomEffects = new Dictionary<NCreature, Node>();

	private static ColorRect _bgBreathRect = null;

	private static ShaderMaterial _bgBreathMaterial = null;

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

	private static Node FindNodeRecursive(Node node, string name)
	{
		if (string.Equals(StringName.op_Implicit(node.Name), name, StringComparison.OrdinalIgnoreCase))
		{
			return node;
		}
		foreach (Node child in node.GetChildren(false))
		{
			Node val = FindNodeRecursive(child, name);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	private static void AddOneShotEffectToRoot(PackedScene scene, Node parent, Vector2 localPosition, int zIndex)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (scene == null || parent == null)
		{
			return;
		}
		Node val = scene.Instantiate((GenEditState)0);
		if (val != null)
		{
			Node2D val2 = (Node2D)(object)((val is Node2D) ? val : null);
			if (val2 != null)
			{
				val2.Position = localPosition;
			}
			SetZIndexRecursive(val, zIndex, zAsRelative: true);
			SetMouseTransparent(val);
			SetNodeOpacityRecursive(val, 1f);
			StartParticles(val);
			parent.AddChild(val, false, (InternalMode)0);
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

	private static void AddMushroomEffect(NCreature creatureNode)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		if (_demonScene == null || creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) || _mushroomEffects.ContainsKey(creatureNode))
		{
			return;
		}
		Node val = _demonScene.Instantiate((GenEditState)0);
		if (val == null)
		{
			return;
		}
		((Node)creatureNode).AddChild(val, false, (InternalMode)0);
		Node2D val2 = (Node2D)(object)((val is Node2D) ? val : null);
		if (val2 != null)
		{
			val2.Position = MUSHROOM_OFFSET;
		}
		else
		{
			Control val3 = (Control)(object)((val is Control) ? val : null);
			if (val3 != null)
			{
				val3.Position = MUSHROOM_OFFSET;
			}
		}
		SetMouseTransparent(val);
		SetNodeOpacityRecursive(val, 1f);
		StartParticles(val);
		Node val4 = FindNodeRecursive(val, "dark_overlay");
		if (val4 != null && GodotObject.IsInstanceValid((GodotObject)(object)val4))
		{
			val4.QueueFree();
		}
		_mushroomEffects[creatureNode] = val;
		if (_mushroomEffects.Count == 1)
		{
			StartBackgroundBreath();
		}
	}

	private static void RemoveMushroomEffect(NCreature creatureNode)
	{
		if (creatureNode != null && _mushroomEffects.TryGetValue(creatureNode, out var value))
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)value) && !((GodotObject)value).IsQueuedForDeletion())
			{
				value.QueueFree();
			}
			_mushroomEffects.Remove(creatureNode);
			if (_mushroomEffects.Count == 0)
			{
				StopBackgroundBreath();
			}
		}
	}

	private static void StartBackgroundBreath()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Expected O, but got Unknown
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Expected O, but got Unknown
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		if (_bgBreathRect == null || !GodotObject.IsInstanceValid((GodotObject)(object)_bgBreathRect))
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				Node val = (Node)(((object)instance.BackCombatVfxContainer) ?? ((object)instance));
				CanvasItem val2 = (CanvasItem)(((object)((val is CanvasItem) ? val : null)) ?? ((object)instance));
				Rect2 visibleRect = ((Node)instance).GetViewport().GetVisibleRect();
				Transform2D globalTransform = val2.GetGlobalTransform();
				Transform2D val3 = ((Transform2D)(ref globalTransform)).AffineInverse();
				Vector2 val4 = val3 * ((Rect2)(ref visibleRect)).Position;
				Vector2 val5 = val3 * (((Rect2)(ref visibleRect)).Position + ((Rect2)(ref visibleRect)).Size);
				Vector2 val6 = (val4 + val5) * 0.5f;
				Vector2 val7 = (val5 - val4) * 1.3f;
				Vector2 position = val6 - val7 * 0.5f;
				_bgBreathRect = new ColorRect
				{
					Position = position,
					Size = val7,
					Color = Colors.Black,
					Modulate = new Color(1f, 1f, 1f, 0.3f),
					MouseFilter = (MouseFilterEnum)2
				};
				Shader val8 = new Shader();
				val8.Code = "\r\n                shader_type canvas_item;\r\n                render_mode unshaded;\r\n                uniform float min_alpha = 0.2;\r\n                uniform float max_alpha = 0.6;\r\n                uniform float cycle_duration = 4.0;\r\n\r\n                void fragment() {\r\n                    float time = TIME;\r\n                    float t = time / cycle_duration;\r\n                    float wave = 0.5 + 0.5 * sin(t * 6.2832);\r\n                    float alpha = mix(min_alpha, max_alpha, wave);\r\n                    COLOR = vec4(0.0, 0.0, 0.0, alpha);\r\n                }\r\n            ";
				_bgBreathMaterial = new ShaderMaterial
				{
					Shader = val8
				};
				_bgBreathMaterial.SetShaderParameter(StringName.op_Implicit("min_alpha"), Variant.op_Implicit(0.2f));
				_bgBreathMaterial.SetShaderParameter(StringName.op_Implicit("max_alpha"), Variant.op_Implicit(0.6f));
				_bgBreathMaterial.SetShaderParameter(StringName.op_Implicit("cycle_duration"), Variant.op_Implicit(4f));
				((CanvasItem)_bgBreathRect).Material = (Material)(object)_bgBreathMaterial;
				val.AddChild((Node)(object)_bgBreathRect, false, (InternalMode)0);
			}
		}
	}

	private static void StopBackgroundBreath()
	{
		if (_bgBreathRect != null && GodotObject.IsInstanceValid((GodotObject)(object)_bgBreathRect))
		{
			((Node)_bgBreathRect).QueueFree();
			_bgBreathRect = null;
		}
		_bgBreathMaterial = null;
	}

	public static void ClearAllEffects()
	{
		foreach (KeyValuePair<NCreature, Node> mushroomEffect in _mushroomEffects)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)mushroomEffect.Value) && !((GodotObject)mushroomEffect.Value).IsQueuedForDeletion())
			{
				mushroomEffect.Value.QueueFree();
			}
		}
		_mushroomEffects.Clear();
		StopBackgroundBreath();
	}
}
