using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace MeleeAttack;

[HarmonyPatch]
public static class SettingsPatch
{
	[HarmonyPatch(typeof(NSettingsScreen), "_Ready")]
	[HarmonyPostfix]
	private static void SettingsReady(NSettingsScreen __instance)
	{
		SettingsUI.TryInject(__instance);
	}

	[HarmonyPatch(typeof(NSettingsScreen), "OnSubmenuOpened")]
	[HarmonyPostfix]
	private static void SettingsSubmenuOpened(NSettingsScreen __instance)
	{
		SettingsUI.TryInject(__instance);
	}
}
