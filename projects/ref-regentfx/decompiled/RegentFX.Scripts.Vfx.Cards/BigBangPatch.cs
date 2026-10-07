using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[HarmonyPatch]
public static class BigBangPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(BigBang), "OnPlay")]
	public static void OnPlay(BigBang __instance)
	{
		if (CardFX.IsTypeEnabled<BigBang>() && LocalContext.IsMe(((CardModel)__instance).Owner))
		{
			MyOnPlay(__instance);
		}
	}

	private static async Task MyOnPlay(BigBang card)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(((CardModel)card).Owner.Creature) : null);
		if (val != null)
		{
			VFXUtil.PlaySimple(CardFX.FromCard((CardModel)(object)card).VfxScenePath, val.VfxSpawnPosition);
			VFXUtil.ShakeAfter(0.3f, (ShakeStrength)4, (ShakeDuration)2);
			await VFXUtil.Wait(0.25f);
			FmodLite.Play("event:/RegentFx/sfx/big_bang_1");
			WorldEnvironmentUtil.TweenExposure(3f, 0.15f, (EaseType)2, (TransitionType)7);
			await VFXUtil.Wait(0.15f);
			WorldEnvironmentUtil.TweenExposure(1f, 0.4f, (EaseType)2, (TransitionType)7);
		}
	}
}
