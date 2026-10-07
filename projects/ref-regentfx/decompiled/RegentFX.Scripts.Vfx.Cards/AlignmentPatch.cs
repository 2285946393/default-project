using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[HarmonyPatch]
public static class AlignmentPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Alignment), "OnPlay")]
	public static void OnPlay(Alignment __instance)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (CardFX.IsTypeEnabled<Alignment>() && LocalContext.IsMe(((CardModel)__instance).Owner))
		{
			Creature creature = ((CardModel)__instance).Owner.Creature;
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(creature) : null);
			if (val == null)
			{
				Entry.Logger.Info("Could not get creature nodes for VFX", 1);
			}
			else
			{
				FmodLite.Play("event:/RegentFx/sfx/alignment");
				VFXUtil.PlaySimple(CardFX.FromCard((CardModel)(object)__instance).VfxScenePath, val.VfxSpawnPosition);
			}
			Entry.StarEffectController?.OnPlayCard();
		}
	}
}
