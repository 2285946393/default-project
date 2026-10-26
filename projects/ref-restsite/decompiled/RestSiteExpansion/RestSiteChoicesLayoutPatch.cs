using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RestSiteExpansion;

[HarmonyPatch(typeof(NRestSiteRoom), "_Ready")]
internal static class RestSiteChoicesLayoutPatch
{
	private static void Prefix(NRestSiteRoom __instance)
	{
		RestSiteChoicesLayout.TryInstall(__instance);
	}
}
