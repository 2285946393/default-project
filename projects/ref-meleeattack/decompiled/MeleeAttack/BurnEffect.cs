using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class BurnEffect
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class BurnDamagePatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Invalid comparison between Unknown and I4
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Invalid comparison between Unknown and I4
			if (SettingsUI.IsConflagrationEnabled() && dealer != null && (int)dealer.Side == 1 && target != null && (int)target.Side == 2 && cardSource != null && ((AbstractModel)cardSource).Id.Entry.Equals("CONFLAGRATION", StringComparison.OrdinalIgnoreCase) && SimpleTeleportPatch.TryGetCreatureNode(target, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				IncrementBurn(node);
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatResetPatch
	{
		private static void Postfix()
		{
			ClearAll();
			GD.Print("[BurnEffect] 战斗房间重置，已清理焚烧状态");
		}
	}

	private const int MAX_LEVEL = 4;

	private const float HOLD_DURATION = 1f;

	private static readonly Dictionary<NCreature, int> _burnLevels = new Dictionary<NCreature, int>();

	public static void Initialize()
	{
		GD.Print("[BurnEffect] 已初始化（使用统一染色系统）");
	}

	public static void ClearAll()
	{
		_burnLevels.Clear();
		GD.Print("[BurnEffect] 已清除焚烧层级状态");
	}

	public static void IncrementBurn(NCreature node)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Creature entity = node.Entity;
		if (entity == null || (int)entity.Side != 2)
		{
			return;
		}
		Node2D body = node.Body;
		if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			_burnLevels.TryGetValue(node, out var value);
			if (value < 4)
			{
				value++;
				_burnLevels[node] = value;
			}
			float num = (float)value / 4f;
			Color white = Colors.White;
			Color color = ((Color)(ref white)).Lerp(Colors.Black, num);
			染色.Apply(node, color, 1f, delegate
			{
				_burnLevels.Remove(node);
			});
			GD.Print($"[BurnEffect] {((Node)node).Name} 焚烧层数: {value}/{4}");
		}
	}
}
