using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class BurstEffect
{
	private class PlayerEffects
	{
		public Tween GoldenTween;

		public Node FireEffect;

		public CancellationTokenSource GhostCts;

		public bool IsCleaning;
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class BurstSubscribePatch
	{
		private static void Postfix(NCombatRoom __instance)
		{
			if (__instance == null)
			{
				return;
			}
			foreach (NCreature creatureNode in __instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null)
				{
					creatureNode.Entity.PowerApplied -= OnPowerApplied;
				}
			}
			foreach (NCreature creatureNode2 in __instance.CreatureNodes)
			{
				if (creatureNode2 != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode2) && creatureNode2.Entity != null)
				{
					creatureNode2.Entity.PowerApplied += OnPowerApplied;
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
	private static class BurstUnsubscribePatch
	{
		private static void Postfix(NCombatRoom __instance)
		{
			if (__instance == null)
			{
				return;
			}
			foreach (NCreature creatureNode in __instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null)
				{
					creatureNode.Entity.PowerApplied -= OnPowerApplied;
				}
			}
			List<Creature> list = new List<Creature>();
			foreach (KeyValuePair<Creature, PlayerEffects> item in (IEnumerable<KeyValuePair<Creature, PlayerEffects>>)_effects)
			{
				list.Add(item.Key);
			}
			foreach (Creature item2 in list)
			{
				CleanupForCreature(item2);
			}
		}
	}

	private const string DEMON_IDLE_VFX_PATH = "res://scenes/vfx/forms/demon/vfx_demon_form_idle_vfx.tscn";

	private const float GHOST_INTERVAL = 1f;

	private const float MOVE_DISTANCE = 10f;

	private const float MOVE_DURATION = 1f;

	private const float SCALE_MULTIPLIER = 1.2f;

	private const float BIG_GHOST_SCALE = 1.5f;

	private const float FADE_DURATION = 1f;

	private const float GHOST_ALPHA = 0.5f;

	private const float GOLDEN_CYCLE_DURATION = 1.2f;

	private static readonly Color GOLDEN_BRIGHT;

	private static readonly Color GOLDEN_DARK;

	private static readonly ConditionalWeakTable<Creature, PlayerEffects> _effects;

	private static readonly PackedScene _demonIdleScene;

	static BurstEffect()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		GOLDEN_BRIGHT = new Color(1f, 0.85f, 0.2f, 1f);
		GOLDEN_DARK = new Color(1f, 0.55f, 0.05f, 1f);
		_effects = new ConditionalWeakTable<Creature, PlayerEffects>();
		_demonIdleScene = GD.Load<PackedScene>("res://scenes/vfx/forms/demon/vfx_demon_form_idle_vfx.tscn");
		BuffMonitor.PowerStackChanged += OnPowerStackChanged;
		BuffMonitor.PowerRemoved += OnPowerRemoved;
	}

	internal static void OnPowerApplied(PowerModel power)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (!(power is BurstPower))
		{
			return;
		}
		if (!SettingsUI.IsBurstEnabled())
		{
			return;
		}
		Creature owner = GetOwnerCreature(power);
		if (owner == null)
		{
			return;
		}
		Callable val = Callable.From((Action)delegate
		{
			if (owner != null && owner.GetCreatureNode() != null)
			{
				StartEffects(owner);
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private static void OnPowerStackChanged(PowerModel power, int oldStack, int newStack)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		BurstPower val = (BurstPower)(object)((power is BurstPower) ? power : null);
		if (val == null)
		{
			return;
		}
		if (!SettingsUI.IsBurstEnabled())
		{
			Creature ownerCreature = GetOwnerCreature((PowerModel)(object)val);
			if (ownerCreature != null)
			{
				CleanupForCreature(ownerCreature);
			}
			return;
		}
		Creature owner = GetOwnerCreature((PowerModel)(object)val);
		if (owner == null)
		{
			return;
		}
		if (newStack <= 0)
		{
			CleanupForCreature(owner);
			return;
		}
		Callable val2 = Callable.From((Action)delegate
		{
			if (owner != null)
			{
				NCreature creatureNode = owner.GetCreatureNode();
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					GhostSpawner.SpawnGhostWithScale(creatureNode, 0.5f, 1f, -10f, 1f, 1.5f);
				}
			}
		});
		((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
	}

	private static void OnPowerRemoved(PowerModel power)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		BurstPower val = (BurstPower)(object)((power is BurstPower) ? power : null);
		if (val == null)
		{
			return;
		}
		Creature owner = GetOwnerCreature((PowerModel)(object)val);
		if (owner == null)
		{
			return;
		}
		Callable val2 = Callable.From((Action)delegate
		{
			if (owner != null)
			{
				NCreature creatureNode = owner.GetCreatureNode();
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					GhostSpawner.SpawnGhostWithScale(creatureNode, 0.5f, 1f, -10f, 1f, 1.5f);
				}
				CleanupForCreature(owner);
			}
		});
		((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
	}

	private static Creature GetOwnerCreature(PowerModel power)
	{
		return (power != null) ? power.Owner : null;
	}

	private static void CleanupForCreature(Creature player)
	{
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		if (player == null || !_effects.TryGetValue(player, out var value) || value.IsCleaning)
		{
			return;
		}
		value.IsCleaning = true;
		try
		{
			NCreature creatureNode = player.GetCreatureNode();
			value.GhostCts?.Cancel();
			value.GhostCts?.Dispose();
			value.GhostCts = null;
			if (value.GoldenTween != null && GodotObject.IsInstanceValid((GodotObject)(object)value.GoldenTween))
			{
				value.GoldenTween.Kill();
				value.GoldenTween = null;
			}
			if (value.FireEffect != null && GodotObject.IsInstanceValid((GodotObject)(object)value.FireEffect))
			{
				value.FireEffect.QueueFree();
				value.FireEffect = null;
			}
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				((CanvasItem)creatureNode).Modulate = Colors.White;
			}
			_effects.Remove(player);
		}
		finally
		{
			value.IsCleaning = false;
		}
	}

	private static void StartEffects(Creature player)
	{
		if (player == null)
		{
			return;
		}
		if (_effects.TryGetValue(player, out var value))
		{
			if (value.GhostCts != null && !value.GhostCts.IsCancellationRequested)
			{
				return;
			}
			_effects.Remove(player);
		}
		NCreature creatureNode = player.GetCreatureNode();
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			GD.PrintErr("[BurstEffect] 无法获取玩家节点");
			return;
		}
		PlayerEffects playerEffects = new PlayerEffects();
		_effects.Add(player, playerEffects);
		ApplyGoldenGlow(creatureNode, out var tween);
		playerEffects.GoldenTween = tween;
		playerEffects.FireEffect = ApplyYellowFireEffect(player, creatureNode);
		ContinuousGhostTrail(player, (playerEffects.GhostCts = new CancellationTokenSource()).Token);
	}

	private static void ApplyGoldenGlow(NCreature node, out Tween tween)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		tween = null;
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Tween val = ((Node)node).CreateTween();
		val.SetLoops(0);
		val.SetTrans((TransitionType)1);
		val.SetEase((EaseType)2);
		val.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			if (GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				Color modulate2 = ((CanvasItem)node).Modulate;
				modulate2.R = Mathf.Lerp(GOLDEN_DARK.R, GOLDEN_BRIGHT.R, t);
				modulate2.G = Mathf.Lerp(GOLDEN_DARK.G, GOLDEN_BRIGHT.G, t);
				modulate2.B = Mathf.Lerp(GOLDEN_DARK.B, GOLDEN_BRIGHT.B, t);
				((CanvasItem)node).Modulate = modulate2;
			}
		}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 0.6000000238418579);
		val.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			if (GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				Color modulate = ((CanvasItem)node).Modulate;
				modulate.R = Mathf.Lerp(GOLDEN_BRIGHT.R, GOLDEN_DARK.R, t);
				modulate.G = Mathf.Lerp(GOLDEN_BRIGHT.G, GOLDEN_DARK.G, t);
				modulate.B = Mathf.Lerp(GOLDEN_BRIGHT.B, GOLDEN_DARK.B, t);
				((CanvasItem)node).Modulate = modulate;
			}
		}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 0.6000000238418579);
		tween = val;
	}

	private static Node ApplyYellowFireEffect(Creature owner, NCreature node)
	{
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return null;
		}
		if (_demonIdleScene == null)
		{
			GD.PrintErr("[BurstEffect] 无法加载恶魔形态场景资源");
			return null;
		}
		if (_effects.TryGetValue(owner, out var value) && value.FireEffect != null && GodotObject.IsInstanceValid((GodotObject)(object)value.FireEffect))
		{
			value.FireEffect.QueueFree();
			value.FireEffect = null;
		}
		Node val = null;
		try
		{
			val = _demonIdleScene.Instantiate((GenEditState)0);
			if (val == null)
			{
				return null;
			}
			Node val2 = FindNodeRecursive(val, "vfx_demon_form_idle_panning_fire");
			if (val2 == null)
			{
				GD.PrintErr("[BurstEffect] 未找到 vfx_demon_form_idle_panning_fire 节点");
				val.QueueFree();
				return null;
			}
			Node parent = val2.GetParent();
			if (parent != null)
			{
				parent.RemoveChild(val2);
			}
			((Node)node).AddChild(val2, false, (InternalMode)0);
			CanvasItem val3 = (CanvasItem)(object)((val2 is CanvasItem) ? val2 : null);
			if (val3 != null)
			{
				val3.Modulate = new Color(1f, 0.85f, 0f, 0.5f);
			}
			PropertyInfo property = ((object)val2).GetType().GetProperty("Emitting");
			if (property != null && property.CanWrite)
			{
				property.SetValue(val2, true);
			}
			return val2;
		}
		catch (Exception ex)
		{
			GD.PrintErr("[BurstEffect] ApplyYellowFireEffect 异常: " + ex.Message);
			return null;
		}
		finally
		{
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				val.QueueFree();
			}
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

	private static async Task ContinuousGhostTrail(Creature creature, CancellationToken token)
	{
		try
		{
			while (!token.IsCancellationRequested)
			{
				NCreature node = creature.GetCreatureNode();
				if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
				{
					GhostSpawner.SpawnGhostWithScale(node, 0.5f, 1f, -10f, 1f, 1.2f);
				}
				await Task.Delay(1000, token);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			Log.Warn("[BurstEffect] ContinuousGhostTrail error: " + ex.Message, 2);
		}
	}
}
