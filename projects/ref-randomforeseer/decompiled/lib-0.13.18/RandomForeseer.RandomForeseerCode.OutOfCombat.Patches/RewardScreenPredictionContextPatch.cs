using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NRewardsScreen), "ShowScreen")]
internal static class RewardScreenPredictionContextPatch
{
	private static void Postfix(RewardsSet set)
	{
		RewardPagePredictionContext.Register(set);
	}
}
