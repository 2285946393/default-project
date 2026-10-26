using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace RestSiteExpansion;

internal static class RestSiteOptionAccess
{
	private static readonly PropertyInfo? OwnerProperty = AccessTools.Property(typeof(RestSiteOption), "Owner");

	internal static Player? GetOwner(RestSiteOption option)
	{
		object? obj = OwnerProperty?.GetValue(option);
		return (Player?)((obj is Player) ? obj : null);
	}
}
