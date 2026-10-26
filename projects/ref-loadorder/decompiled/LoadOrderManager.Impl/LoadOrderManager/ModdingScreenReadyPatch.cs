using System;
using System.Reflection;
using HarmonyLib;

namespace LoadOrderManager;

[HarmonyPatch]
internal static class ModdingScreenReadyPatch
{
	private const string ScreenTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen";

	private static MethodBase? TargetMethod()
	{
		return (AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen") ?? AccessTools.TypeByName("NModdingScreen"))?.GetMethod("_Ready", BindingFlags.Instance | BindingFlags.Public);
	}

	private static void Postfix(object __instance)
	{
		try
		{
			LoadOrderUiInjector.TryInject(__instance);
		}
		catch (Exception ex)
		{
			DebugLog.Error("UI inject failed.", ex);
		}
	}
}
