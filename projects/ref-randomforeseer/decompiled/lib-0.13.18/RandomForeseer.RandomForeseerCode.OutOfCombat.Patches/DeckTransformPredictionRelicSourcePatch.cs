using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(RelicCmd), "Obtain", new Type[]
{
	typeof(RelicModel),
	typeof(Player),
	typeof(int)
})]
internal static class DeckTransformPredictionRelicSourcePatch
{
	[HarmonyPrefix]
	private static void EnterSource(RelicModel relic, out IDisposable? __state)
	{
		__state = DeckTransformPredictionContext.EnterRelic(relic);
	}

	[HarmonyFinalizer]
	private static void RestoreCallerSource(IDisposable? __state)
	{
		__state?.Dispose();
	}
}
