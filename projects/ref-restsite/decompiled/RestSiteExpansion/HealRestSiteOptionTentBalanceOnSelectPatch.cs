using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace RestSiteExpansion;

[HarmonyPatch(typeof(HealRestSiteOption), "OnSelect")]
internal static class HealRestSiteOptionTentBalanceOnSelectPatch
{
	private static bool Prefix(HealRestSiteOption __instance, ref Task<bool> __result)
	{
		Player owner = RestSiteOptionAccess.GetOwner((RestSiteOption)(object)__instance);
		string optionId = ((RestSiteOption)__instance).OptionId;
		if (owner == null || !TentBalanceGate.IsBlockedFor(owner, optionId))
		{
			return true;
		}
		MainFile.Logger.Info($"[TentBalance] 原版「休息」被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(optionId)}）：player={owner.NetId} 已选={TentBalanceGate.DescribeChoices(owner)}", 1);
		__result = Task.FromResult(result: false);
		return false;
	}
}
