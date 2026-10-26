using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class CardDrawCardMirrors
{
	public static void CompileDriverOnPlay(CompileDriver card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		int drawCount = context.OwnerState.OrbQueue.Orbs.Select((OrbModel orb) => ((AbstractModel)orb).Id).Distinct().Count();
		context.Simulator.Draw(((CardModel)card).Owner, drawCount);
	}

	public static void CalculatedGambleOnPlay(CalculatedGamble card, CardOnPlayMirrorContext context)
	{
		PredictedCard[] array = context.OwnerState.Hand.Cards.ToArray();
		context.Simulator.DiscardAndDraw(array, array.Length);
	}

	public static void ConstellationOnPlay(Constellation card, CardOnPlayMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		context.Simulator.Draw(targetPlayer, ((DynamicVar)((CardModel)card).DynamicVars.Cards).BaseValue);
		context.Simulator.GainEnergy(targetPlayer, ((DynamicVar)((CardModel)card).DynamicVars.Energy).IntValue);
		context.GainBlock(targetPlayer.Creature);
	}

	public static void EscapePlanOnPlay(EscapePlan card, CardOnPlayMirrorContext context)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Invalid comparison between Unknown and I4
		IReadOnlyList<PredictedCard> readOnlyList = context.Simulator.Draw(((CardModel)card).Owner, 1);
		if (readOnlyList == null || readOnlyList.Count != 1)
		{
			return;
		}
		PredictedCard predictedCard = readOnlyList[0];
		if (predictedCard != null)
		{
			CardModel preview = predictedCard.Preview;
			if (preview != null && (int)preview.Type == 2)
			{
				context.GainBlock(((CardModel)card).Owner.Creature);
			}
		}
	}

	public static void ExpertiseOnPlay(Expertise card, CardOnPlayMirrorContext context)
	{
		foreach (PredictedCard item in context.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue))
		{
			item.MutablePreview.GiveSingleTurnRetain();
		}
	}

	public static void FetchOnPlay(Fetch card, CardOnPlayMirrorContext context)
	{
		Creature osty = ((CardModel)card).Owner.Osty;
		if (osty != null && !context.State.GetCreature(osty).IsDead)
		{
			DamageCmd.Attack(((DynamicVar)((CardModel)card).DynamicVars.OstyDamage).BaseValue).FromOsty(((CardModel)card).Owner.Osty, (CardModel)(object)card, context.CardPlay).Targeting(context.Target)
				.Simulate(context.Simulator);
			if (!HasCardPlayFinishedThisTurn(context.Simulator, context.Card))
			{
				context.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).BaseValue);
			}
		}
	}

	public static void FtlOnPlay(Ftl card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		if (CountCardPlaysFinishedThisTurn(context.Simulator, ((CardModel)card).Owner) < ((CardModel)card).DynamicVars["PlayMax"].IntValue)
		{
			context.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).BaseValue);
		}
	}

	public static void HuddleUpOnPlay(HuddleUp card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		foreach (Creature item in from creature in context2.State.GetTeammatesOf(((CardModel)card).Owner.Creature)
			where creature.IsPlayer && context2.State.GetCreature(creature).IsAlive
			select creature)
		{
			context2.Simulator.Draw(item.Player, ((DynamicVar)((CardModel)card).DynamicVars.Cards).BaseValue);
		}
	}

	public static void ImpatienceOnPlay(Impatience card, CardOnPlayMirrorContext context)
	{
		if (context.OwnerState.Hand.Cards.All((PredictedCard predicted) => (int)predicted.Preview.Type != 1))
		{
			context.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).BaseValue);
		}
	}

	public static void PillageOnPlay(Pillage card, CardOnPlayMirrorContext context)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
		context.AttackSingle();
		CardModel preview;
		do
		{
			IReadOnlyList<PredictedCard> readOnlyList = context.Simulator.Draw(((CardModel)card).Owner, 1);
			if (readOnlyList != null && readOnlyList.Count == 1)
			{
				PredictedCard predictedCard = readOnlyList[0];
				if (predictedCard != null)
				{
					preview = predictedCard.Preview;
					continue;
				}
				break;
			}
			break;
		}
		while (preview != null && (int)preview.Type == 1 && context.OwnerState.Hand.Cards.Count < CardPile.MaxCardsInHand);
	}

	public static void RebootOnPlay(Reboot card, CardOnPlayMirrorContext context)
	{
		context.Simulator.MoveHandToDrawPile(((CardModel)card).Owner);
		context.Simulator.Shuffle(((CardModel)card).Owner);
		context.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).BaseValue);
	}

	public static void RestlessnessOnPlay(Restlessness card, CardOnPlayMirrorContext context)
	{
		if (context.OwnerState.Hand.IsEmpty)
		{
			context.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue);
			context.Simulator.GainEnergy(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Energy).IntValue);
		}
	}

	public static void ScrapeOnPlay(Scrape card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		context2.AttackSingle();
		List<PredictedCard> cards = (from drawnCard in context2.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).IntValue)
			where drawnCard.Preview.EnergyCost.CostsX || drawnCard.GetEnergyCostWithModifiers(context2.Simulator) != 0
			select drawnCard).ToList();
		context2.Simulator.Discard(cards);
	}

	public static void ScrawlOnPlay(Scrawl card, CardOnPlayMirrorContext context)
	{
		int drawCount = CardPile.MaxCardsInHand - context.OwnerState.Hand.Cards.Count;
		context.Simulator.Draw(((CardModel)card).Owner, drawCount);
	}

	private static int CountCardPlaysFinishedThisTurn(CombatPredictionSimulator simulator, Player player)
	{
		CombatPredictionSimulator simulator2 = simulator;
		Player player2 = player;
		return CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(simulator2.State.CombatState) && entry.CardPlay.Player == player2) + simulator2.History.OfType<CombatPredictionCardPlayFinishedEntry>().Count((CombatPredictionCardPlayFinishedEntry entry) => entry.CardPlay.Player == player2);
	}

	private static bool HasCardPlayFinishedThisTurn(CombatPredictionSimulator simulator, PredictedCard card)
	{
		CombatPredictionSimulator simulator2 = simulator;
		PredictedCard card2 = card;
		if (!CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(simulator2.State.CombatState) && entry.CardPlay.Card == card2.Original))
		{
			return simulator2.History.OfType<CombatPredictionCardPlayFinishedEntry>().Any((CombatPredictionCardPlayFinishedEntry entry) => entry.Card.Original == card2.Original);
		}
		return true;
	}
}
