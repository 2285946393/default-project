using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class RewardPredictionPatch
{
	private static void Postfix(Reward __instance, ref IEnumerable<IHoverTip> __result)
	{
		RelicReward val = (RelicReward)(object)((__instance is RelicReward) ? __instance : null);
		IReadOnlyList<IHoverTip> readOnlyList;
		if (val != null)
		{
			RelicModel relic = val.Relic;
			if (relic != null)
			{
				Player player = __instance.Player;
				readOnlyList = RelicPickupPrediction.GetHoverTips(player, relic);
				goto IL_005d;
			}
		}
		else
		{
			PotionReward val2 = (PotionReward)(object)((__instance is PotionReward) ? __instance : null);
			if (val2 != null)
			{
				PotionModel potion = val2.Potion;
				if (potion != null)
				{
					Player player = __instance.Player;
					readOnlyList = PotionPrediction.GetHoverTips(player, potion);
					goto IL_005d;
				}
			}
		}
		readOnlyList = Array.Empty<IHoverTip>();
		goto IL_005d;
		IL_005d:
		IReadOnlyList<IHoverTip> readOnlyList2 = readOnlyList;
		if (readOnlyList2.Count > 0)
		{
			__result = __result.Concat(readOnlyList2);
		}
	}
}
