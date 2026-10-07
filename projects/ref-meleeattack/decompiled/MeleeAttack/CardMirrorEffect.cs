using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using WhiteScreenMod;

namespace MeleeAttack;

public static class CardMirrorEffect
{
	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	public static class MirrorCardPatch
	{
		public static void Prefix(CardModel __instance)
		{
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_023e: Unknown result type (might be due to invalid IL or missing references)
			//IL_021d: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
			if (!SettingsUI.IsGrandFinaleEnabled())
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
			if (obj == null || !((AbstractModel)__instance).Id.Entry.Equals("GRAND_FINALE", StringComparison.OrdinalIgnoreCase) || !ShouldTrigger(__instance))
			{
				return;
			}
			Player owner = __instance.Owner;
			Creature val = ((owner != null) ? owner.Creature : null);
			if (val == null || !SimpleTeleportPatch.TryGetCreatureNode(val, out var _, out var creatureNode) || creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				return;
			}
			_isGrandFinalePending = true;
			if (((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__byrdonis_nest_effect_added")))
			{
				NCreature obj2 = creatureNode;
				StringName obj3 = StringName.op_Implicit("__byrdonis_nest_effect_added");
				Variant val2 = default(Variant);
				val2 = ((GodotObject)obj2).GetMeta(obj3, val2);
				Node val3 = ((Variant)(ref val2)).As<Node>();
				if (GodotObject.IsInstanceValid((GodotObject)(object)val3) && !((GodotObject)val3).IsQueuedForDeletion())
				{
					val3.QueueFree();
				}
				((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__byrdonis_nest_effect_added"));
			}
			PackedScene mirrorScene = _mirrorScene;
			Node effect = ((mirrorScene != null) ? mirrorScene.Instantiate((GenEditState)0) : null);
			if (effect == null)
			{
				return;
			}
			HashSet<Node> keepSet = new HashSet<Node>();
			MarkKeepRecursive(effect, keepSet);
			PruneNodes(effect, keepSet);
			TintFeathers(effect);
			((Node)creatureNode).AddChild(effect, false, (InternalMode)0);
			SetMouseTransparentExcludingDarkOverlay(effect);
			Node obj4 = effect;
			Node2D val4 = (Node2D)(object)((obj4 is Node2D) ? obj4 : null);
			if (val4 != null)
			{
				val4.Position = POSITION_OFFSET;
			}
			else
			{
				Node obj5 = effect;
				Control val5 = (Control)(object)((obj5 is Control) ? obj5 : null);
				if (val5 != null)
				{
					val5.Position = POSITION_OFFSET;
				}
			}
			((GodotObject)creatureNode).SetMeta(StringName.op_Implicit("__byrdonis_nest_effect_added"), Variant.op_Implicit((GodotObject)(object)effect));
			Node obj6 = effect;
			CanvasItem val6 = (CanvasItem)(object)((obj6 is CanvasItem) ? obj6 : null);
			if (val6 == null)
			{
				return;
			}
			NCombatRoom instance = NCombatRoom.Instance;
			SceneTree val7 = ((instance != null) ? ((Node)instance).GetTree() : null);
			if (val7 == null)
			{
				return;
			}
			Tween val8 = val7.CreateTween();
			val8.SetParallel(false);
			val8.TweenInterval(10.0);
			val8.TweenProperty((GodotObject)(object)val6, NodePath.op_Implicit("modulate"), Variant.op_Implicit(new Color(1f, 1f, 1f, 0f)), 3.0);
			val8.Finished += delegate
			{
				//IL_005a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0060: Unknown result type (might be due to invalid IL or missing references)
				//IL_0061: Unknown result type (might be due to invalid IL or missing references)
				//IL_0066: Unknown result type (might be due to invalid IL or missing references)
				if (GodotObject.IsInstanceValid((GodotObject)(object)effect))
				{
					effect.QueueFree();
				}
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && ((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__byrdonis_nest_effect_added")))
				{
					NCreature obj7 = creatureNode;
					StringName obj8 = StringName.op_Implicit("__byrdonis_nest_effect_added");
					Variant val9 = default(Variant);
					val9 = ((GodotObject)obj7).GetMeta(obj8, val9);
					if (((Variant)(ref val9)).As<Node>() == effect)
					{
						((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__byrdonis_nest_effect_added"));
					}
				}
			};
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim")]
	private static class GrandFinaleWhiteFlashPatch
	{
		public static void Postfix(Creature creature, string triggerName, float waitTime)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Invalid comparison between Unknown and I4
			if (_isGrandFinalePending && creature != null && (int)creature.Side == 1 && !string.IsNullOrEmpty(triggerName) && triggerName.StartsWith("Attack", StringComparison.OrdinalIgnoreCase))
			{
				AttackCommand value = SimpleTeleportPatch._currentAttack.Value;
				AbstractModel obj = ((value != null) ? value.ModelSource : null);
				CardModel val = (CardModel)(object)((obj is CardModel) ? obj : null);
				if (val != null && ((AbstractModel)val).Id.Entry.Equals("GRAND_FINALE", StringComparison.OrdinalIgnoreCase))
				{
					_isGrandFinalePending = false;
					WhiteScreenEffect.TriggerWhiteFlash();
				}
			}
		}
	}

	private const string EFFECT_META_KEY = "__byrdonis_nest_effect_added";

	private const string MIRROR_VFX_PATH = "res://scenes/vfx/events/byrdonis_nest_vfx.tscn";

	private static readonly Vector2 POSITION_OFFSET = new Vector2(-600f, -650f);

	private static readonly HashSet<string> KEEP_NODE_NAMES = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "LightLine", "light_specks", "feathers" };

	private static readonly Color FEATHER_COLOR = Color.Color8((byte)158, (byte)0, (byte)76, byte.MaxValue);

	private static readonly PackedScene _mirrorScene = GD.Load<PackedScene>("res://scenes/vfx/events/byrdonis_nest_vfx.tscn");

	private static bool _isGrandFinalePending = false;

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

	private static void SetMouseTransparentExcludingDarkOverlay(Node node)
	{
		if (string.Equals(StringName.op_Implicit(node.Name), "dark_overlay", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Control val = (Control)(object)((node is Control) ? node : null);
		if (val != null)
		{
			val.MouseFilter = (MouseFilterEnum)2;
		}
		foreach (Node child in node.GetChildren(false))
		{
			SetMouseTransparentExcludingDarkOverlay(child);
		}
	}

	private static bool MarkKeepRecursive(Node node, HashSet<Node> keepSet)
	{
		bool flag = KEEP_NODE_NAMES.Contains(((object)node.Name).ToString());
		foreach (Node child in node.GetChildren(false))
		{
			if (MarkKeepRecursive(child, keepSet))
			{
				flag = true;
			}
		}
		if (flag)
		{
			keepSet.Add(node);
		}
		return flag;
	}

	private static void PruneNodes(Node node, HashSet<Node> keepSet)
	{
		Node[] array = ((IEnumerable<Node>)node.GetChildren(false)).ToArray();
		foreach (Node val in array)
		{
			if (!keepSet.Contains(val))
			{
				val.QueueFree();
			}
			else
			{
				PruneNodes(val, keepSet);
			}
		}
	}

	private static void TintFeathers(Node node)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (string.Equals(StringName.op_Implicit(node.Name), "feathers", StringComparison.OrdinalIgnoreCase))
		{
			ApplyTintRecursive(node, FEATHER_COLOR);
			return;
		}
		foreach (Node child in node.GetChildren(false))
		{
			TintFeathers(child);
		}
	}

	private static void ApplyTintRecursive(Node node, Color color)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		CanvasItem val = (CanvasItem)(object)((node is CanvasItem) ? node : null);
		if (val != null)
		{
			Color modulate = val.Modulate;
			val.Modulate = new Color(color.R, color.G, color.B, modulate.A);
		}
		foreach (Node child in node.GetChildren(false))
		{
			ApplyTintRecursive(child, color);
		}
	}
}
