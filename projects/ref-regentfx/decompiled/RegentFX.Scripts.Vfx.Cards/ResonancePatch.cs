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
public static class ResonancePatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Resonance), "OnPlay")]
	public static void OnPlay(Resonance __instance)
	{
		if (CardFX.IsTypeEnabled<Resonance>() && LocalContext.IsMe(((CardModel)__instance).Owner))
		{
			MyOnPlay(__instance);
		}
	}

	private static async Task MyOnPlay(Resonance card)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature ownerNode = ((instance != null) ? instance.GetCreatureNode(((CardModel)card).Owner.Creature) : null);
		if (ownerNode != null)
		{
			FmodLite.Play("event:/RegentFx/sfx/Resonance");
			await VFXUtil.Wait(0.1f);
			VFXUtil.PlaySimpleBack(CardFX.FromCard((CardModel)(object)card).VfxScenePath, ownerNode.VfxSpawnPosition);
			await VFXUtil.Wait(0.1f);
			WorldEnvironmentUtil.TweenExposure(1.5f, 0.1f, (EaseType)2, (TransitionType)7);
			await VFXUtil.Wait(0.1f);
			WorldEnvironmentUtil.TweenExposure(1f, 0.3f, (EaseType)2, (TransitionType)7);
		}
		Resonance.PlayStarVfx();
		Entry.StarEffectController?.OnPlayCard();
	}
}
