using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace MeleeAttack;

public static class HitStopController
{
	[HarmonyPatch(typeof(AttackCommand), "Execute")]
	private static class AttackResetPatch
	{
		private static void Prefix(AttackCommand __instance)
		{
			_highDamageHitStopTriggered = false;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class UpgradeCardHitStopPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Invalid comparison between Unknown and I4
			if (dealer != null && (int)dealer.Side == 1 && target != null && (int)target.Side == 2 && cardSource != null && cardSource.IsUpgraded && !ExcludedHitStopCards.Contains(((AbstractModel)cardSource).Id.Entry) && !_highDamageHitStopTriggered && (!((AbstractModel)cardSource).Id.Entry.Equals("SHIV", StringComparison.OrdinalIgnoreCase) || !KnifeTrapFastForward.IsKnifeTrapOrigin))
			{
				HitStop.Apply(0.1f);
			}
		}
	}

	public static readonly HashSet<string> ExcludedHitStopCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BODY_SLAM" };

	internal static bool _highDamageHitStopTriggered = false;

	public static void TriggerHighDamageHitStop(float duration = 0.2f)
	{
		if (KnifeTrapFastForward.IsKnifeTrapOrigin)
		{
			_highDamageHitStopTriggered = true;
			return;
		}
		_highDamageHitStopTriggered = true;
		HitStop.Apply(duration);
	}

	public static void ResetHighDamageFlag()
	{
		_highDamageHitStopTriggered = false;
	}
}
