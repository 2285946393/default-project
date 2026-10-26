using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace RestSiteExpansion;

[HarmonyPatch(typeof(HealRestSiteOption), "get_Description")]
internal static class HealRestSiteOptionTentBalanceDescriptionPatch
{
	private static void Postfix(HealRestSiteOption __instance, ref LocString __result)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		Player owner = RestSiteOptionAccess.GetOwner((RestSiteOption)(object)__instance);
		string optionId = ((RestSiteOption)__instance).OptionId;
		if (owner != null && TentBalanceGate.IsBlockedFor(owner, optionId))
		{
			__result = new LocString("relics", TentBalanceGate.BlockedLocKeyFor(optionId));
		}
	}
}
