using System;
using HarmonyLib;
using IntentGraph2.DevConsole;
using IntentGraph2.Utils;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace IntentGraph2.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
public class DevConsoleConstructorPatch
{
	public static void Postfix(DevConsole __instance)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		new Traverse((object)__instance).Method("RegisterCommand", new Type[1] { typeof(AbstractConsoleCmd) }, (object[])null).GetValue(new object[1]
		{
			new ReloadIntentsConsoleCmd()
		});
		IgLogger.Info("Registered reloadintents to DevConsole.");
		new Traverse((object)__instance).Method("RegisterCommand", new Type[1] { typeof(AbstractConsoleCmd) }, (object[])null).GetValue(new object[1]
		{
			new EditIntentConsoleCmd()
		});
		IgLogger.Info("Registered editintent to DevConsole.");
		new Traverse((object)__instance).Method("RegisterCommand", new Type[1] { typeof(AbstractConsoleCmd) }, (object[])null).GetValue(new object[1]
		{
			new ExportIntentsConsoleCmd()
		});
		IgLogger.Info("Registered exportintents to DevConsole.");
	}
}
