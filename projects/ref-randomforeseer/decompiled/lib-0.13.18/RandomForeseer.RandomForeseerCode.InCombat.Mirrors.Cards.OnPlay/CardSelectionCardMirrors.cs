using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class CardSelectionCardMirrors
{
	public static void AnointedOnPlay(Anointed card, CardOnPlayMirrorContext context)
	{
		int num = CardPile.MaxCardsInHand - context.OwnerState.Hand.Cards.Count;
		if (num > 0)
		{
			List<PredictedCard> list = IEnumerableExtensions.TakeRandom<PredictedCard>(context.OwnerState.DrawPile.Cards.Where((PredictedCard predictedCard) => (int)predictedCard.Preview.Rarity == 4), num, context.Rng.CombatCardSelection).ToList();
			if (list.Count != 0)
			{
				context.Simulator.History.CardsSelected(list);
				context.Simulator.AddToPile(list, (PileType)2, (CardPilePosition)1);
			}
		}
	}

	public static void BeatDownOnPlay(BeatDown card, CardOnPlayMirrorContext context)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Invalid comparison between Unknown and I4
		CardOnPlayMirrorContext context2 = context;
		List<PredictedCard> list = ListExtensions.StableShuffle<PredictedCard>(context2.OwnerState.DiscardPile.Cards.Where((PredictedCard predictedCard) => (int)predictedCard.Preview.Type == 1 && !predictedCard.GetKeywords(context2.Simulator).Contains((CardKeyword)4)).ToList(), context2.Rng.Shuffle).Take(((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue).ToList();
		if (list.Count == 0)
		{
			return;
		}
		context2.Simulator.History.CardsSelected(list);
		foreach (PredictedCard item in list)
		{
			if (context2.Simulator.IsOverOrEnding)
			{
				break;
			}
			Creature target = null;
			if ((int)item.Preview.TargetType == 2)
			{
				target = context2.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context2.State.HittableEnemies);
			}
			context2.Simulator.AutoPlay(item, target, (AutoPlayType)1);
		}
	}

	public static void CatastropheOnPlay(Catastrophe card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		for (int i = 0; i < ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue; i++)
		{
			IReadOnlyList<PredictedCard> cards = context2.OwnerState.DrawPile.Cards;
			PredictedCard predictedCard2 = ListExtensions.StableShuffle<PredictedCard>(cards.Where((PredictedCard predictedCard) => !predictedCard.GetKeywords(context2.Simulator).Contains((CardKeyword)4)).ToList(), context2.Rng.Shuffle).FirstOrDefault();
			if (predictedCard2 == null)
			{
				predictedCard2 = ListExtensions.StableShuffle<PredictedCard>(cards.ToList(), context2.Rng.Shuffle).FirstOrDefault();
			}
			if (predictedCard2 != null)
			{
				context2.Simulator.History.CardsSelected(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(predictedCard2));
				context2.Simulator.AutoPlay(predictedCard2, null, (AutoPlayType)1);
				continue;
			}
			break;
		}
	}

	public static void CinderOnPlay(Cinder card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		PredictedCard predictedCard = SelectRandomHandCard(context, (CardModel _) => true);
		if (predictedCard != null)
		{
			context.Simulator.History.CardsSelected(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(predictedCard));
			context.Simulator.Exhaust(predictedCard);
		}
	}

	public static void DrainPowerOnPlay(DrainPower card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		List<PredictedCard> list = IEnumerableExtensions.TakeRandom<PredictedCard>(context.OwnerState.DiscardPile.Cards.Where((PredictedCard predictedCard) => predictedCard.Preview.IsUpgradable), ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue, context.Rng.CombatCardSelection).ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (PredictedCard item in list)
		{
			context.Simulator.Upgrade(item);
		}
		context.Simulator.History.CardsSelected(list);
	}

	public static void HiddenGemOnPlay(HiddenGem card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		SimCardPile drawPile = context2.OwnerState.DrawPile;
		if (drawPile.IsEmpty)
		{
			return;
		}
		List<PredictedCard> list = drawPile.Cards.Where(delegate(PredictedCard predictedCard)
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Invalid comparison between Unknown and I4
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Invalid comparison between Unknown and I4
			if (!predictedCard.GetKeywords(context2.Simulator).Contains((CardKeyword)4))
			{
				CardType type2 = predictedCard.Preview.Type;
				if ((int)type2 != 4 && (int)type2 != 5)
				{
					return predictedCard.Preview.GetEnchantedReplayCount() < 1;
				}
			}
			return false;
		}).ToList();
		List<PredictedCard> list2 = list.Where(delegate(PredictedCard predictedCard)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			CardType type = predictedCard.Preview.Type;
			return type - 1 <= 2;
		}).ToList();
		PredictedCard predictedCard2 = context2.Rng.CombatCardSelection.NextItem<PredictedCard>((IEnumerable<PredictedCard>)((list2.Count == 0) ? list : list2));
		if (predictedCard2 != null)
		{
			CardModel mutablePreview = predictedCard2.MutablePreview;
			mutablePreview.BaseReplayCount += ((CardModel)card).DynamicVars["Replay"].IntValue;
			context2.Simulator.History.CardsSelected(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(predictedCard2));
		}
	}

	public static void SeekerStrikeOnPlay(SeekerStrike card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		context2.AttackSingle();
		if (!context2.Simulator.History.OfType<CombatPredictionRiskEntry>().Any((CombatPredictionRiskEntry entry) => entry.Reason == PredictionRiskReason.UnresolvedPlayerChoice && entry.Trace?.Source == context2.OriginalCard))
		{
			List<PredictedCard> list = ListExtensions.StableShuffle<PredictedCard>(context2.OwnerState.DrawPile.Cards.ToList(), context2.Rng.CombatCardSelection).Take(((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue).ToList();
			if (list.Count != 0)
			{
				context2.Simulator.History.CardsSelected(list);
				context2.History.RecordRisk(PredictionRiskReason.UnresolvedPlayerChoice);
			}
		}
	}

	public static void ThrashOnPlay(Thrash card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle(2);
		PredictedCard predictedCard = SelectRandomHandCard(context, (CardModel cardModel) => (int)cardModel.Type == 1);
		if (predictedCard != null)
		{
			context.Simulator.History.CardsSelected(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(predictedCard));
			decimal damage = default(decimal);
			DynamicVarSet dynamicVars = predictedCard.Preview.DynamicVars;
			if (dynamicVars.ContainsKey("CalculatedDamage"))
			{
				damage = ((CalculatedVar)(object)dynamicVars.CalculatedDamage).InvokeCalculate(context.Simulator, predictedCard, null);
			}
			else if (dynamicVars.ContainsKey("Damage"))
			{
				damage = ((DynamicVar)dynamicVars.Damage).BaseValue;
			}
			else if (dynamicVars.ContainsKey("OstyDamage"))
			{
				damage = ((DynamicVar)dynamicVars.OstyDamage).BaseValue;
			}
			else
			{
				Entry.Logger.Warn("Exhausted attack card " + ((AbstractModel)predictedCard.Preview).Id.Entry + " did not have an appropriate DamageVar", 1);
			}
			damage = HookMirrors.ModifyDamage(context.Simulator, null, predictedCard.Preview.Owner.Creature, damage, (ValueProp)8, predictedCard, null);
			DamageVar damage2 = ((CardModel)card).DynamicVars.Damage;
			((DynamicVar)damage2).BaseValue = ((DynamicVar)damage2).BaseValue + damage;
			card.ExtraDamage += damage;
			context.Simulator.Exhaust(predictedCard);
		}
	}

	public static void TrueGritOnPlay(TrueGrit card, CardOnPlayMirrorContext context)
	{
		context.GainBlock(((CardModel)card).Owner.Creature);
		if (((CardModel)card).IsUpgraded)
		{
			if (!context.OwnerState.Hand.IsEmpty)
			{
				context.History.RecordRisk(PredictionRiskReason.UnresolvedPlayerChoice);
			}
			return;
		}
		PredictedCard predictedCard = SelectRandomHandCard(context, (CardModel _) => true);
		if (predictedCard != null)
		{
			context.Simulator.History.CardsSelected(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(predictedCard));
			context.Simulator.Exhaust(predictedCard);
		}
	}

	public static void UproarOnPlay(Uproar card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		context2.AttackSingle(2);
		List<PredictedCard> list = context2.OwnerState.DrawPile.Cards.Where((PredictedCard predictedCard) => (int)predictedCard.Preview.Type == 1).ToList();
		PredictedCard predictedCard2 = ListExtensions.StableShuffle<PredictedCard>(list.Where((PredictedCard predictedCard) => !predictedCard.GetKeywords(context2.Simulator).Contains((CardKeyword)4)).ToList(), context2.Rng.Shuffle).FirstOrDefault();
		if (predictedCard2 == null)
		{
			predictedCard2 = ListExtensions.StableShuffle<PredictedCard>(list, context2.Rng.Shuffle).FirstOrDefault();
		}
		if (predictedCard2 != null)
		{
			context2.Simulator.History.CardsSelected(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(predictedCard2));
			context2.Simulator.AutoPlay(predictedCard2, null, (AutoPlayType)1);
		}
	}

	private static PredictedCard? SelectRandomHandCard(CardOnPlayMirrorContext context, Func<CardModel, bool> filter)
	{
		Func<CardModel, bool> filter2 = filter;
		IEnumerable<PredictedCard> enumerable = context.OwnerState.Hand.Cards.Where((PredictedCard card) => filter2(card.Preview));
		return context.Rng.CombatCardSelection.NextItem<PredictedCard>(enumerable);
	}
}
