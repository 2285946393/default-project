using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(CardSelectCmd), "FromChooseACardScreen")]
internal static class ChooseACardPredictionContextPatch
{
	[HarmonyPrefix]
	private static void Prefix(PlayerChoiceContext context, IReadOnlyList<CardModel> cards, out IDisposable? __state)
	{
		__state = ChooseACardPredictionContext.Enter(cards, context.LastInvolvedModel);
	}

	[HarmonyPostfix]
	private static void Postfix(ref Task<CardModel?> __result, ref IDisposable? __state)
	{
		if (__state != null)
		{
			__result = __result.WithFinally(__state.Dispose);
			__state = null;
		}
	}

	[HarmonyFinalizer]
	private static void Finalizer(IDisposable? __state)
	{
		__state?.Dispose();
	}
}
