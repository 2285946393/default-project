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
public static class GlowPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Glow), "OnPlay")]
	public static void OnPlay(Glow __instance)
	{
		if (CardFX.IsTypeEnabled<Glow>() && LocalContext.IsMe(((CardModel)__instance).Owner))
		{
			MyOnPlay(__instance);
		}
	}

	private static async Task MyOnPlay(Glow card)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(((CardModel)card).Owner.Creature) : null);
		if (val != null)
		{
			FmodLite.Play("event:/RegentFx/sfx/glow");
			VFXUtil.PlaySimple(CardFX.FromCard((CardModel)(object)card).VfxScenePath, val.VfxSpawnPosition);
			await VFXUtil.Wait(0.1f);
			WorldEnvironmentUtil.TweenExposure(2f, 0.1f, (EaseType)2, (TransitionType)7);
			await VFXUtil.Wait(0.1f);
			WorldEnvironmentUtil.TweenExposure(1f, 0.3f, (EaseType)2, (TransitionType)7);
		}
	}
}
