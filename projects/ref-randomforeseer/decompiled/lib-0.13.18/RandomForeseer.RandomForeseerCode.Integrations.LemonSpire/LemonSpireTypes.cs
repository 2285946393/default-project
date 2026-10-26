using System;
using System.Reflection;
using HarmonyLib;

namespace RandomForeseer.RandomForeseerCode.Integrations.LemonSpire;

internal static class LemonSpireTypes
{
	public const string ModId = "lemonSpire2";

	public const string PatchCategory = "RandomForeseer.LemonSpire";

	public const string AncientRelicChoiceProviderName = "lemonSpire2.PlayerStateEx.PanelProvider.AncientRelicChoiceProvider";

	public const string HandCardProviderName = "lemonSpire2.PlayerStateEx.PanelProvider.HandCardProvider";

	public const string ShopProviderName = "lemonSpire2.PlayerStateEx.PanelProvider.ShopProvider";

	public static Type? Get(string fullName)
	{
		Type type = AccessTools.TypeByName(fullName);
		if (type != null)
		{
			return type;
		}
		try
		{
			return Assembly.Load("lemonSpire2").GetType(fullName);
		}
		catch
		{
			return null;
		}
	}
}
