using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal static class AfterAutoPostPlayPhaseEnteredMirrors
{
	private static readonly MirrorMethodSpec AfterAutoPostPlayPhaseEntered = MirrorMethodSpec.Hook("AfterAutoPostPlayPhaseEntered", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(Player)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterAutoPostPlayMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterAutoPostPlayMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterAutoPostPlayMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterAutoPostPlayMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterAutoPostPlayMirrorContext>(AfterAutoPostPlayPhaseEntered);
		methodMirrorRegistry.Register<HowlFromBeyond>((Action<HowlFromBeyond, AfterAutoPostPlayMirrorContext>)HandleHowlFromBeyond);
		methodMirrorRegistry.Register<IAmInvincible>((Action<IAmInvincible, AfterAutoPostPlayMirrorContext>)HandleIAmInvincible);
		methodMirrorRegistry.Register<StampedePower>((Action<StampedePower, AfterAutoPostPlayMirrorContext>)HandleStampedePower);
		return methodMirrorRegistry;
	}

	private static void HandleHowlFromBeyond(HowlFromBeyond card, AfterAutoPostPlayMirrorContext context)
	{
		if (context.Player == ((CardModel)card).Owner)
		{
			PredictedCard predictedCard = context.State.GetPlayerCombatState(((CardModel)card).Owner).ExhaustPile.Find((CardModel)(object)card);
			if (predictedCard != null)
			{
				context.Simulator.AutoPlay(predictedCard, null, (AutoPlayType)1);
			}
		}
	}

	private static void HandleIAmInvincible(IAmInvincible card, AfterAutoPostPlayMirrorContext context)
	{
		if (context.Player == ((CardModel)card).Owner && (context.State.GetPlayerCombatState(((CardModel)card).Owner).DrawPile.TopCard?.References(card) ?? false))
		{
			context.Simulator.AutoPlayFromDrawPile(((CardModel)card).Owner, 1, (CardPilePosition)2);
		}
	}

	private static void HandleStampedePower(StampedePower power, AfterAutoPostPlayMirrorContext context)
	{
		AfterAutoPostPlayMirrorContext context2 = context;
		if (context2.Player.Creature != ((PowerModel)power).Owner)
		{
			return;
		}
		SimCardPile hand = context2.State.GetPlayerCombatState(context2.Player).Hand;
		for (int i = 0; i < ((PowerModel)power).Amount; i++)
		{
			List<PredictedCard> list = hand.Cards.Where((PredictedCard card) => (int)card.Preview.Type == 1 && !card.GetKeywords(context2.Simulator).Contains((CardKeyword)4)).ToList();
			PredictedCard predictedCard = context2.Rng.Shuffle.NextItem<PredictedCard>((IEnumerable<PredictedCard>)list);
			if (predictedCard != null)
			{
				context2.Simulator.AutoPlay(predictedCard, null, (AutoPlayType)1);
			}
		}
	}
}
