using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace RegentFX.Scripts.Vfx.Powers;

[HarmonyPatch]
public static class BlackHolePatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(BlackHolePower), "DealDamageToAllEnemies")]
	private static void BlackholeBurst(BlackHolePower __instance)
	{
		if (PowerFX.IsTypeEnabled<BlackHole>() && LocalContext.IsMe(((PowerModel)__instance).Owner) && Blackhole.Blackholes.TryGetValue(((PowerModel)__instance).Owner, out Blackhole value))
		{
			value.Burst();
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				instance.ScreenShake((ShakeStrength)2, (ShakeDuration)1, -1f);
			}
		}
	}
}
