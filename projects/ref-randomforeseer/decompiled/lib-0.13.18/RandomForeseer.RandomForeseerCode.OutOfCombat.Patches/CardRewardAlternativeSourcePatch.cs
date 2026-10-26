using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Rewards;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(CardRewardAlternative), "Generate")]
internal static class CardRewardAlternativeSourcePatch
{
	private static void Postfix(CardReward cardReward, IReadOnlyList<CardRewardAlternative> __result)
	{
		CardRewardAlternativeButtonHoverTips.RegisterReward(cardReward, __result);
	}
}
