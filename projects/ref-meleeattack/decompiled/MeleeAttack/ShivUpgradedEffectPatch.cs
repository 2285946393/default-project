using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

[HarmonyPatch]
public static class ShivUpgradedEffectPatch
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class AfterDamageReceivedPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			if (dealer == null || (int)dealer.Side != 1)
			{
				return;
			}
			Shiv val = (Shiv)(object)((cardSource is Shiv) ? cardSource : null);
			if (val == null || !((CardModel)val).IsUpgraded)
			{
				return;
			}
			Player owner = ((CardModel)val).Owner;
			if (!(((owner != null) ? owner.Character : null) is Silent) || target == null || target.IsPlayer)
			{
				return;
			}
			float num = result.TotalDamage;
			if (!(num < 1f))
			{
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature val2 = ((instance != null) ? instance.GetCreatureNode(target) : null);
				if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
				{
					VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_grand_finale_impact");
				}
			}
		}
	}

	private const string VFX_PATH = "vfx/vfx_grand_finale_impact";

	private const float DAMAGE_THRESHOLD = 1f;
}
