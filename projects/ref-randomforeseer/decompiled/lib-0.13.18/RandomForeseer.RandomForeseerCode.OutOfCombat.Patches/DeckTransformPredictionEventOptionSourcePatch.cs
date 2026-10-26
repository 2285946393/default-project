using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(EventOption), "Chosen")]
internal static class DeckTransformPredictionEventOptionSourcePatch
{
	[HarmonyPrefix]
	private static void EnterSource(EventOption __instance, out IDisposable? __state)
	{
		__state = DeckTransformPredictionContext.EnterEventOption(__instance);
	}

	[HarmonyFinalizer]
	private static void RestoreCallerSource(IDisposable? __state)
	{
		__state?.Dispose();
	}
}
