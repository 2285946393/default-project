using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards;

internal static class CardIsPlayableMirrors
{
	private delegate bool IsPlayableDelegate(CardModel card);

	private static readonly MethodInfo IsPlayableGetterMethod = AccessTools.PropertyGetter(typeof(CardModel), "IsPlayable") ?? throw new UnreachableException("Could not find CardModel.IsPlayable getter.");

	private static readonly IsPlayableDelegate OriginalIsPlayableGetter = (IsPlayableDelegate)Delegate.CreateDelegate(typeof(IsPlayableDelegate), IsPlayableGetterMethod);

	private static readonly MirrorMethodSpec IsPlayable = new MirrorMethodSpec(typeof(CardModel), IsPlayableGetterMethod.Name, BindingFlags.Instance | BindingFlags.NonPublic, Array.Empty<Type>());

	private static readonly MethodMirrorRegistry<CardModel, CardIsPlayableMirrorContext, bool> Registry = CreateRegistry();

	public static bool Invoke(CombatPredictionSimulator simulator, PredictedCard card)
	{
		CardIsPlayableMirrorContext context = new CardIsPlayableMirrorContext
		{
			Simulator = simulator,
			Card = card
		};
		if (!Registry.TryInvokeRegistered(card.Preview, context, out var dispatchResult))
		{
			return OriginalIsPlayableGetter(card.Preview);
		}
		return dispatchResult.Value;
	}

	private static MethodMirrorRegistry<CardModel, CardIsPlayableMirrorContext, bool> CreateRegistry()
	{
		MethodMirrorRegistry<CardModel, CardIsPlayableMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<CardModel, CardIsPlayableMirrorContext, bool>(IsPlayable);
		methodMirrorRegistry.Register<Clash>((Func<Clash, CardIsPlayableMirrorContext, bool>)HandleClash);
		methodMirrorRegistry.Register<GrandFinale>((Func<GrandFinale, CardIsPlayableMirrorContext, bool>)HandleGrandFinale);
		methodMirrorRegistry.Register<HighFive>((Func<HighFive, CardIsPlayableMirrorContext, bool>)HandleHighFive);
		return methodMirrorRegistry;
	}

	private static bool HandleClash(Clash card, CardIsPlayableMirrorContext context)
	{
		return context.State.GetPlayerCombatState(((CardModel)card).Owner).Hand.Cards.All((PredictedCard handCard) => (int)handCard.Preview.Type == 1);
	}

	private static bool HandleGrandFinale(GrandFinale card, CardIsPlayableMirrorContext context)
	{
		return context.State.GetPlayerCombatState(((CardModel)card).Owner).DrawPile.IsEmpty;
	}

	private static bool HandleHighFive(HighFive card, CardIsPlayableMirrorContext context)
	{
		Creature osty = ((CardModel)card).Owner.Osty;
		if (osty != null)
		{
			return context.State.GetCreature(osty).IsAlive;
		}
		return false;
	}
}
