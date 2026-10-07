using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[HarmonyPatch]
public static class ParticleWallPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ParticleWall), "OnPlay")]
	public static void OnPlay(ParticleWall __instance)
	{
		if (CardFX.IsTypeEnabled<ParticleWall>() && LocalContext.IsMe(((CardModel)__instance).Owner))
		{
			MyOnPlay(__instance);
		}
	}

	private static async Task MyOnPlay(ParticleWall card)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(((CardModel)card).Owner.Creature) : null);
		if (val != null)
		{
			FmodLite.Play("event:/RegentFx/sfx/particle_wall");
			int num = (VFXUtil.IsCharacterFacingRight(((CardModel)card).Owner.Creature) ? 1 : (-1));
			Vector2 position = val.VfxSpawnPosition + new Vector2(180f * (float)num, 110f);
			Node2D val2 = VFXUtil.PlaySimple(CardFX.FromCard((CardModel)(object)card).VfxScenePath, position, 3f);
			if (val2 != null)
			{
				val2.Scale *= new Vector2((float)num, 1f);
				val2.Scale *= 0.7f;
			}
		}
		Entry.StarEffectController?.OnPlayCard();
	}
}
