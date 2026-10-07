using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RegentFX.Scripts.Vfx.Powers;

[HarmonyPatch]
public static class PillarOfCreationPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(PillarOfCreationPower), "AfterCardGeneratedForCombat")]
	private static void PillarOfCreationActivate(PillarOfCreationPower __instance, Player? creator)
	{
		if (PowerFX.IsTypeEnabled<PillarOfCreation>() && LocalContext.IsMe(((PowerModel)__instance).Owner) && creator != null && creator.Creature == ((PowerModel)__instance).Owner && Pillar.Pillars.TryGetValue(((PowerModel)__instance).Owner, out Pillar value))
		{
			value.Activate();
		}
	}
}
