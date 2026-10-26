using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class ShouldPlayMirrors
{
	private static readonly MirrorMethodSpec ShouldPlay = MirrorMethodSpec.Hook("ShouldPlay", new Type[2]
	{
		typeof(CardModel),
		typeof(AutoPlayType)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ShouldPlayMirrorContext, bool> Registry = CreateRegistry();

	public static bool Invoke(AbstractModel listener, ShouldPlayMirrorContext context)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (Registry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return dispatchResult.Value;
		}
		return listener.ShouldPlay(context.Card.Preview, context.AutoPlayType);
	}

	private static MethodMirrorRegistry<AbstractModel, ShouldPlayMirrorContext, bool> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ShouldPlayMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ShouldPlayMirrorContext, bool>(ShouldPlay);
		methodMirrorRegistry.Register<Enthralled>((Func<Enthralled, ShouldPlayMirrorContext, bool>)HandleEnthralled);
		methodMirrorRegistry.Register<Normality>((Func<Normality, ShouldPlayMirrorContext, bool>)HandleNormality);
		methodMirrorRegistry.Register<ChainsOfBindingPower>((Func<ChainsOfBindingPower, ShouldPlayMirrorContext, bool>)HandleChainsOfBindingPower);
		methodMirrorRegistry.Register<RingingPower>((Func<RingingPower, ShouldPlayMirrorContext, bool>)HandleRingingPower);
		methodMirrorRegistry.Register<SlothPower>((Func<SlothPower, ShouldPlayMirrorContext, bool>)HandleSlothPower);
		methodMirrorRegistry.Register<VelvetChoker>((Func<VelvetChoker, ShouldPlayMirrorContext, bool>)HandleVelvetChoker);
		return methodMirrorRegistry;
	}

	private static bool HandleEnthralled(Enthralled card, ShouldPlayMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Invalid comparison between Unknown and I4
		if (context.Card.Preview.Owner == ((CardModel)card).Owner && (int)context.AutoPlayType == 0 && !(context.Card.Preview is Enthralled))
		{
			PredictedCard? predictedCard = context.State.FindCard((CardModel)(object)card);
			if (predictedCard == null)
			{
				return true;
			}
			return (int)(predictedCard.GetPile(context.State)?.Type).GetValueOrDefault() != 2;
		}
		return true;
	}

	private static bool HandleNormality(Normality card, ShouldPlayMirrorContext context)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I4
		if (context.Card.Preview.Owner == ((CardModel)card).Owner)
		{
			PredictedCard? predictedCard = context.State.FindCard((CardModel)(object)card);
			if (predictedCard != null && (int)(predictedCard.GetPile(context.State)?.Type).GetValueOrDefault() == 2)
			{
				return CountCardPlaysStartedThisTurn(((CardModel)card).Owner, context) < 3;
			}
		}
		return true;
	}

	private static bool HandleChainsOfBindingPower(ChainsOfBindingPower power, ShouldPlayMirrorContext context)
	{
		ChainsOfBindingPower power2 = power;
		if (context.Card.Preview.Owner.Creature == ((PowerModel)power2).Owner && context.Card.Preview.Affliction is Bound)
		{
			return !context.StateStore.Get((AbstractModel)(object)power2, () => new ChainsOfBindingPredictionState(power2)).BoundCardPlayed;
		}
		return true;
	}

	private static bool HandleRingingPower(RingingPower power, ShouldPlayMirrorContext context)
	{
		if (context.Card.Preview.Owner.Creature == ((PowerModel)power).Owner && context.Card.Preview.Affliction is Ringing)
		{
			return CountCardPlaysStartedThisTurn(context.Card.Preview.Owner, context) == 0;
		}
		return true;
	}

	private static bool HandleSlothPower(SlothPower power, ShouldPlayMirrorContext context)
	{
		SlothPower power2 = power;
		if (context.Card.Preview.Owner.Creature == ((PowerModel)power2).Owner)
		{
			return context.StateStore.Get((AbstractModel)(object)power2, () => new CounterPredictionState(power2._cardsPlayedThisTurn)).Value < ((PowerModel)power2).Amount;
		}
		return true;
	}

	private static bool HandleVelvetChoker(VelvetChoker relic, ShouldPlayMirrorContext context)
	{
		VelvetChoker relic2 = relic;
		if (context.Card.Preview.Owner == ((RelicModel)relic2).Owner)
		{
			return context.StateStore.Get((AbstractModel)(object)relic2, () => new CounterPredictionState(relic2._cardsPlayedThisTurn)).Value < ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).IntValue;
		}
		return true;
	}

	private static int CountCardPlaysStartedThisTurn(Player player, ShouldPlayMirrorContext context)
	{
		ShouldPlayMirrorContext context2 = context;
		Player player2 = player;
		return CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && entry.CardPlay.Player == player2) + context2.History.OfType<CombatPredictionCardPlayStartedEntry>().Count((CombatPredictionCardPlayStartedEntry entry) => entry.CardPlay.Player == player2);
	}
}
