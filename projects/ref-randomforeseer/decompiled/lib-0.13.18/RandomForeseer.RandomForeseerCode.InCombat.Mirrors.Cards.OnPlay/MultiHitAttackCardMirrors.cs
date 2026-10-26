using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class MultiHitAttackCardMirrors
{
	public static void AstralPulseOnPlay(AstralPulse card, CardOnPlayMirrorContext context)
	{
		context.AttackAllOpponents(2);
	}

	public static void BarrageOnPlay(Barrage card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle(context.OwnerState.OrbQueue.Orbs.Count);
	}

	public static void DaggerSprayOnPlay(DaggerSpray card, CardOnPlayMirrorContext context)
	{
		context.AttackAllOpponents(2);
	}

	public static void DismantleOnPlay(Dismantle card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle((!context.Target.HasPower<VulnerablePower>()) ? 1 : 2);
	}

	public static void FiendFireOnPlay(FiendFire card, CardOnPlayMirrorContext context)
	{
		PredictedCard[] array = context.OwnerState.Hand.Cards.ToArray();
		PredictedCard[] array2 = array;
		foreach (PredictedCard card2 in array2)
		{
			context.Simulator.Exhaust(card2);
		}
		context.AttackSingle(array.Length);
	}

	public static void FinisherOnPlay(Finisher card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		Finisher card2 = card;
		int num = CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && (int)entry.CardPlay.Card.Type == 1 && entry.CardPlay.Player == ((CardModel)card2).Owner);
		num += context2.History.OfType<CombatPredictionCardPlayFinishedEntry>().Count((CombatPredictionCardPlayFinishedEntry entry) => (int)entry.CardPlay.Card.Type == 1 && entry.CardPlay.Player == ((CardModel)card2).Owner);
		context2.AttackSingle(num);
	}

	public static void FlechettesOnPlay(Flechettes card, CardOnPlayMirrorContext context)
	{
		int hitCount = context.OwnerState.Hand.Cards.Count((PredictedCard predicted) => (int)predicted.Preview.Type == 2);
		context.AttackSingle(hitCount);
	}

	public static void GunkUpOnPlay(GunkUp card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle(((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue);
		context.Simulator.CreateAndAddGeneratedCardsToCombat<Slimed>(((CardModel)card).Owner, (PileType)3, 1, ((CardModel)card).Owner, (CardPilePosition)1);
	}

	public static void HeavenlyDrillOnPlay(HeavenlyDrill card, CardOnPlayMirrorContext context)
	{
		int num = context.ResolveEnergyXValue();
		if (num >= ((DynamicVar)((CardModel)card).DynamicVars.Energy).IntValue)
		{
			num *= 2;
		}
		context.AttackSingle(num);
	}

	public static void HelixDrillOnPlay(HelixDrill card, CardOnPlayMirrorContext context)
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		CardOnPlayMirrorContext context2 = context;
		HelixDrill card2 = card;
		int num = (from entry in CombatManager.Instance.History.Entries.OfType<EnergySpentEntry>()
			where ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && ((CombatHistoryEntry)entry).Actor.Player == ((CardModel)card2).Owner
			select entry).Sum((EnergySpentEntry entry) => entry.Amount);
		num += (from entry in context2.History.OfType<CombatPredictionEnergySpentEntry>()
			where entry.Player == ((CardModel)card2).Owner
			select entry).Sum((CombatPredictionEnergySpentEntry entry) => entry.Amount);
		int num2 = num;
		ResourceInfo resources = context2.CardPlay.Resources;
		num = num2 - ((ResourceInfo)(ref resources)).EnergyValue;
		context2.AttackSingle(Math.Max(0, num));
	}

	public static void LunarBlastOnPlay(LunarBlast card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		LunarBlast card2 = card;
		int num = CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && (int)entry.CardPlay.Card.Type == 2 && entry.CardPlay.Player == ((CardModel)card2).Owner);
		num += context2.History.OfType<CombatPredictionCardPlayFinishedEntry>().Count((CombatPredictionCardPlayFinishedEntry entry) => (int)entry.CardPlay.Card.Type == 2 && entry.CardPlay.Player == ((CardModel)card2).Owner);
		context2.AttackSingle(num);
	}

	public static void MaulOnPlay(Maul card, CardOnPlayMirrorContext context)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		context.AttackSingle(2);
		decimal baseValue = ((CardModel)card).DynamicVars["Increase"].BaseValue;
		foreach (PredictedCard item in context.OwnerState.AllCards.Where((PredictedCard predicted) => predicted.Preview is Maul))
		{
			((Maul)item.MutablePreview).BuffFromMaulPlay(baseValue);
		}
	}

	public static void PullFromBelowOnPlay(PullFromBelow card, CardOnPlayMirrorContext context)
	{
		PullFromBelow card2 = card;
		int num = CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry entry) => entry.CardPlay.Player == ((CardModel)card2).Owner && entry.WasEthereal);
		num += context.History.OfType<CombatPredictionCardPlayFinishedEntry>().Count((CombatPredictionCardPlayFinishedEntry entry) => entry.CardPlay.Player == ((CardModel)card2).Owner && entry.WasEthereal);
		context.AttackSingle(num);
	}

	public static void RadiateOnPlay(Radiate card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		Radiate card2 = card;
		int num = (from entry in CombatManager.Instance.History.Entries.OfType<StarsModifiedEntry>()
			where ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && entry.Amount > 0 && ((CombatHistoryEntry)entry).Actor == ((CardModel)card2).Owner.Creature
			select entry).Sum((StarsModifiedEntry entry) => entry.Amount);
		num += (from entry in context2.History.OfType<CombatPredictionStarsModifiedEntry>()
			where entry.Player == ((CardModel)card2).Owner && entry.Amount > 0
			select entry).Sum((CombatPredictionStarsModifiedEntry entry) => entry.Amount);
		context2.AttackAllOpponents(num);
	}

	public static void RattleOnPlay(Rattle card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		Creature osty = ((CardModel)card).Owner.Osty;
		if (osty != null && !context2.State.GetCreature(osty).IsDead)
		{
			int num = 1 + CombatManager.Instance.History.Entries.OfType<CreatureAttackedEntry>().Count((CreatureAttackedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && ((CombatHistoryEntry)entry).Actor == osty);
			num += context2.History.OfType<CombatPredictionCreatureAttackedEntry>().Count((CombatPredictionCreatureAttackedEntry entry) => entry.Attacker == osty);
			DamageCmd.Attack(((DynamicVar)((CardModel)card).DynamicVars.OstyDamage).BaseValue).FromOsty(osty, (CardModel)(object)card, context2.CardPlay).WithHitCount(num)
				.Targeting(context2.Target)
				.Simulate(context2.Simulator);
		}
	}

	public static void SovereignBladeOnPlay(SovereignBlade card, CardOnPlayMirrorContext context)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		int intValue = ((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue;
		if ((int)((CardModel)card).TargetType == 3)
		{
			context.AttackAllOpponents(intValue);
		}
		else
		{
			context.AttackSingle(intValue);
		}
		int powerAmount = ((CardModel)card).Owner.Creature.GetPowerAmount<ParryPower>();
		if (powerAmount > 0)
		{
			context.GainBlock(((CardModel)card).Owner.Creature, powerAmount, ((CardModel)card).DynamicVars.CalculatedBlock.Props);
		}
	}

	public static void SpiteOnPlay(Spite card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		Spite card2 = card;
		bool flag = CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>().Any((DamageReceivedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && entry.Receiver == ((CardModel)card2).Owner.Creature && entry.Result.UnblockedDamage > 0) || context2.History.OfType<CombatPredictionDamageReceivedEntry>().Any((CombatPredictionDamageReceivedEntry entry) => entry.Receiver == ((CardModel)card2).Owner.Creature && entry.Result.UnblockedDamage > 0);
		context2.AttackSingle((!flag) ? 1 : ((DynamicVar)((CardModel)card2).DynamicVars.Repeat).IntValue);
	}

	public static void TearAsunderOnPlay(TearAsunder card, CardOnPlayMirrorContext context)
	{
		TearAsunder card2 = card;
		int num = 1 + CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>().Count((DamageReceivedEntry entry) => entry.Receiver == ((CardModel)card2).Owner.Creature && entry.Result.UnblockedDamage > 0);
		num += context.History.OfType<CombatPredictionDamageReceivedEntry>().Count((CombatPredictionDamageReceivedEntry entry) => entry.Receiver == ((CardModel)card2).Owner.Creature && entry.Result.UnblockedDamage > 0);
		context.AttackSingle(num);
	}

	public static void TwinStrikeOnPlay(TwinStrike card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle(2);
	}
}
