using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MeleeAttack;

public static class InkBladeEffect
{
	[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
	private static class CardOnPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
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
			if ((string?)obj == "SHIV")
			{
				_isShivPending = true;
				_isInkyShiv = __instance.Enchantment is Inky;
			}
		}
	}

	[HarmonyPatch(typeof(NShivThrowVfx), "_Ready")]
	private static class ShivVfxReadyPatch
	{
		private static void Postfix(NShivThrowVfx __instance)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			if (!_isShivPending)
			{
				return;
			}
			Color white = Colors.White;
			if (_isInkyShiv)
			{
				white = Colors.Black;
			}
			else
			{
				if (!KnifeTrapFastForward.IsKnifeTrapOrigin)
				{
					_isShivPending = false;
					_isInkyShiv = false;
					return;
				}
				white = GetRandomHighSaturationColor();
			}
			__instance.ApplyTint(white);
			_isShivPending = false;
			_isInkyShiv = false;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class InkBladeDamagePatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Invalid comparison between Unknown and I4
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			if (dealer == null || (int)dealer.Side != 1 || target == null || (int)target.Side != 2 || cardSource == null || !((AbstractModel)cardSource).Id.Entry.Equals("SHIV", StringComparison.OrdinalIgnoreCase) || !SimpleTeleportPatch.TryGetCreatureNode(target, out var _, out var node) || node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				return;
			}
			Color white = Colors.White;
			bool flag = false;
			if (cardSource.Enchantment is Inky)
			{
				white = Colors.Black;
				flag = true;
			}
			else
			{
				if (!KnifeTrapFastForward.IsKnifeTrapOrigin)
				{
					return;
				}
				white = GetRandomHighSaturationColor();
				flag = true;
			}
			if (flag)
			{
				染色.Apply(node, white);
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatResetPatch
	{
		private static void Postfix()
		{
			ClearAll();
			染色.ClearAll();
		}
	}

	private const string SHIV_CARD_ID = "SHIV";

	private static bool _isShivPending;

	private static bool _isInkyShiv;

	private static Color GetRandomHighSaturationColor()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)GD.RandRange(0, 360) / 360f;
		float num2 = (float)GD.RandRange(0.800000011920929, 1.0);
		float num3 = (float)GD.RandRange(0.800000011920929, 1.0);
		return Color.FromHsv(num, num2, num3, 1f);
	}

	public static void ClearAll()
	{
		_isShivPending = false;
		_isInkyShiv = false;
	}
}
