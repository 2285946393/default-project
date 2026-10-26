using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace IntentGraph2.Patches;

[HarmonyPatch(typeof(LocManager), "SetLanguage")]
public class LocManagerSetLanguagePatch
{
	public static void Postfix(LocManager __instance, string language)
	{
		IntentGraphMod.LoadIntentStrings(language);
	}
}
