using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeleeAttack;

public static class BodySlamEffect
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class BodySlamEffectPatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Invalid comparison between Unknown and I4
			if (dealer != null && (int)dealer.Side == 1 && target != null && (int)target.Side == 2 && cardSource != null && ((AbstractModel)cardSource).Id.Entry.Equals("BODY_SLAM", StringComparison.OrdinalIgnoreCase) && SettingsUI.IsBodySlamEnabled())
			{
				HitStop.Apply();
				if (!RadialBlur.IsActive)
				{
					RadialBlurTrigger.Trigger(0.08f, 0.5f);
				}
			}
		}
	}

	private const float HIT_STOP_DURATION = 0.2f;

	private const float BLUR_STRENGTH = 0.08f;

	private const float BLUR_DURATION = 0.5f;

	public static void ClearAll()
	{
	}
}
