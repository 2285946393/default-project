using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards;

internal static class CardOnTurnEndInHandMirrors
{
	private static readonly MirrorMethodSpec OnTurnEndInHand = new MirrorMethodSpec(typeof(CardModel), "OnTurnEndInHand", BindingFlags.Instance | BindingFlags.NonPublic, new Type[1] { typeof(PlayerChoiceContext) });

	private static readonly MethodMirrorRegistry<CardModel, CardOnTurnEndInHandMirrorContext> Registry = CreateRegistry();

	public static MirrorDispatchResult Invoke(CombatPredictionSimulator simulator, PredictedCard card)
	{
		return Registry.Invoke(card.Preview, new CardOnTurnEndInHandMirrorContext
		{
			Simulator = simulator,
			Card = card
		});
	}

	private static MethodMirrorRegistry<CardModel, CardOnTurnEndInHandMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<CardModel, CardOnTurnEndInHandMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<CardModel, CardOnTurnEndInHandMirrorContext>(OnTurnEndInHand);
		methodMirrorRegistry.Register<BadLuck>((Action<BadLuck, CardOnTurnEndInHandMirrorContext>)HandleHpLoss);
		methodMirrorRegistry.Register<Beckon>((Action<Beckon, CardOnTurnEndInHandMirrorContext>)HandleHpLoss);
		methodMirrorRegistry.Register<Burn>((Action<Burn, CardOnTurnEndInHandMirrorContext>)HandleDamage);
		methodMirrorRegistry.Register<Decay>((Action<Decay, CardOnTurnEndInHandMirrorContext>)HandleDamage);
		methodMirrorRegistry.Register<Infection>((Action<Infection, CardOnTurnEndInHandMirrorContext>)HandleDamage);
		methodMirrorRegistry.Register<Toxic>((Action<Toxic, CardOnTurnEndInHandMirrorContext>)HandleDamage);
		methodMirrorRegistry.Register<Wither>((Action<Wither, CardOnTurnEndInHandMirrorContext>)HandleDamage);
		methodMirrorRegistry.Register<Regret>((Action<Regret, CardOnTurnEndInHandMirrorContext>)HandleRegret);
		methodMirrorRegistry.RegisterIgnored<Debt>();
		methodMirrorRegistry.RegisterIgnored<Doubt>();
		methodMirrorRegistry.RegisterIgnored<Shame>();
		return methodMirrorRegistry;
	}

	private static void HandleDamage(CardModel card, CardOnTurnEndInHandMirrorContext context)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		DamageOwner(context, ((DynamicVar)card.DynamicVars.Damage).BaseValue, card.DynamicVars.Damage.Props);
	}

	private static void HandleHpLoss(CardModel card, CardOnTurnEndInHandMirrorContext context)
	{
		DamageOwner(context, ((DynamicVar)card.DynamicVars.HpLoss).BaseValue, (ValueProp)14);
	}

	private static void HandleRegret(Regret card, CardOnTurnEndInHandMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Regret val = (Regret)context.MutablePreviewCard;
		DamageOwner(context, val.CardsInHand, (ValueProp)14);
		val.CardsInHand = 0;
	}

	private static void DamageOwner(CardOnTurnEndInHandMirrorContext context, decimal amount, ValueProp props)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Creature creature = context.PreviewCard.Owner.Creature;
		context.Simulator.Damage(new _003C_003Ez__ReadOnlySingleElementList<Creature>(creature), amount, props, creature, context.Card, null);
	}
}
