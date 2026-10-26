using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal static class BeforeSideTurnEndMirrors
{
	private static readonly MirrorMethodSpec BeforeSideTurnEndVeryEarly = MirrorMethodSpec.Hook("BeforeSideTurnEndVeryEarly", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CombatSide),
		typeof(IEnumerable<Creature>)
	});

	private static readonly MirrorMethodSpec BeforeSideTurnEndEarly = MirrorMethodSpec.Hook("BeforeSideTurnEndEarly", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CombatSide),
		typeof(IEnumerable<Creature>)
	});

	private static readonly MirrorMethodSpec BeforeSideTurnEnd = MirrorMethodSpec.Hook("BeforeSideTurnEnd", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CombatSide),
		typeof(IEnumerable<Creature>)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> VeryEarlyRegistry = CreateVeryEarlyRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> EarlyRegistry = CreateEarlyRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> Registry = CreateRegistry();

	public static void InvokeVeryEarly(AbstractModel listener, SideTurnEndMirrorContext context)
	{
		VeryEarlyRegistry.Invoke(listener, context);
	}

	public static void InvokeEarly(AbstractModel listener, SideTurnEndMirrorContext context)
	{
		EarlyRegistry.Invoke(listener, context);
	}

	public static void Invoke(AbstractModel listener, SideTurnEndMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> CreateVeryEarlyRegistry()
	{
		MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext>(BeforeSideTurnEndVeryEarly);
		methodMirrorRegistry.Register<Orichalcum>((Action<Orichalcum, SideTurnEndMirrorContext>)OrichalcumMirrors.BeforeSideTurnEndVeryEarly);
		methodMirrorRegistry.Register<FakeOrichalcum>((Action<FakeOrichalcum, SideTurnEndMirrorContext>)OrichalcumMirrors.BeforeSideTurnEndVeryEarly);
		methodMirrorRegistry.RegisterIgnored<AsleepPower>();
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> CreateEarlyRegistry()
	{
		MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext>(BeforeSideTurnEndEarly);
		methodMirrorRegistry.Register<PlatingPower>((Action<PlatingPower, SideTurnEndMirrorContext>)HandlePlatingPower);
		methodMirrorRegistry.Register<RegenPower>((Action<RegenPower, SideTurnEndMirrorContext>)HandleRegenPower);
		methodMirrorRegistry.Register<PaelsEye>((Action<PaelsEye, SideTurnEndMirrorContext>)HandlePaelsEye);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, SideTurnEndMirrorContext>(BeforeSideTurnEnd);
		methodMirrorRegistry.Register<Orichalcum>((Action<Orichalcum, SideTurnEndMirrorContext>)OrichalcumMirrors.BeforeSideTurnEnd);
		methodMirrorRegistry.Register<FakeOrichalcum>((Action<FakeOrichalcum, SideTurnEndMirrorContext>)OrichalcumMirrors.BeforeSideTurnEnd);
		methodMirrorRegistry.Register<CloakClasp>((Action<CloakClasp, SideTurnEndMirrorContext>)HandleCloakClasp);
		methodMirrorRegistry.Register<RippleBasin>((Action<RippleBasin, SideTurnEndMirrorContext>)HandleRippleBasin);
		methodMirrorRegistry.Register<HailstormPower>((Action<HailstormPower, SideTurnEndMirrorContext>)HandleHailstormPower);
		methodMirrorRegistry.Register<ScreamingFlagon>((Action<ScreamingFlagon, SideTurnEndMirrorContext>)HandleScreamingFlagon);
		methodMirrorRegistry.Register<StoneCalendar>((Action<StoneCalendar, SideTurnEndMirrorContext>)HandleStoneCalendar);
		methodMirrorRegistry.Register<TheBombPower>((Action<TheBombPower, SideTurnEndMirrorContext>)HandleTheBombPower);
		methodMirrorRegistry.Register<DoomPower>((Action<DoomPower, SideTurnEndMirrorContext>)DoomPowerMirrors.BeforeSideTurnEnd);
		methodMirrorRegistry.Register<Regret>((Action<Regret, SideTurnEndMirrorContext>)HandleRegret);
		methodMirrorRegistry.Register<ChainsOfBindingPower>((Action<ChainsOfBindingPower, SideTurnEndMirrorContext>)HandleChainsOfBindingPower);
		methodMirrorRegistry.RegisterIgnored<PaelsTears>();
		methodMirrorRegistry.RegisterIgnored<SandpitPower>();
		return methodMirrorRegistry;
	}

	private static void HandlePlatingPower(PlatingPower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner))
		{
			context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
		}
	}

	private static void HandleRegenPower(RegenPower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner) && context.State.GetCreature(((PowerModel)power).Owner).IsAlive)
		{
			context.Simulator.Heal(((PowerModel)power).Owner, ((PowerModel)power).Amount);
		}
	}

	private static void HandlePaelsEye(PaelsEye relic, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((RelicModel)relic).Owner.Creature) && !relic._usedThisCombat && !AnyCardsPlayedThisTurn(((RelicModel)relic).Owner) && relic._wasOwnerPartOfLastPlayerTurn)
		{
			context.Simulator.ExhaustHand(((RelicModel)relic).Owner);
		}
	}

	private static void HandleCloakClasp(CloakClasp relic, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((RelicModel)relic).Owner.Creature))
		{
			int count = context.State.GetPlayerCombatState(((RelicModel)relic).Owner).Hand.Cards.Count;
			if (count > 0)
			{
				context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, (decimal)count * ((DynamicVar)((RelicModel)relic).DynamicVars.Block).BaseValue, (ValueProp)4);
			}
		}
	}

	private static void HandleRippleBasin(RippleBasin relic, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((RelicModel)relic).Owner.Creature) && !HasPlayedAttackThisTurn(((RelicModel)relic).Owner))
		{
			context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, ((RelicModel)relic).DynamicVars.Block);
		}
	}

	private static void HandleHailstormPower(HailstormPower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner))
		{
			Player player = ((PowerModel)power).Owner.Player;
			if (player != null && context.State.GetPlayerCombatState(player).OrbQueue.Orbs.Count((OrbModel orb) => orb is FrostOrb) >= ((PowerModel)power).DynamicVars["FrostOrbs"].IntValue)
			{
				context.Simulator.Damage(context.State.HittableEnemies, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
			}
		}
	}

	private static void HandleScreamingFlagon(ScreamingFlagon relic, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((RelicModel)relic).Owner.Creature) && context.State.GetPlayerCombatState(((RelicModel)relic).Owner).Hand.IsEmpty)
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
		}
	}

	private static void HandleStoneCalendar(StoneCalendar relic, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((RelicModel)relic).Owner.Creature))
		{
			PlayerCombatState playerCombatState = ((RelicModel)relic).Owner.PlayerCombatState;
			if (((playerCombatState != null) ? new int?(playerCombatState.TurnNumber) : null) == ((RelicModel)relic).DynamicVars["DamageTurn"].IntValue)
			{
				context.Simulator.Damage(context.State.HittableEnemies, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
			}
		}
	}

	private static void HandleTheBombPower(TheBombPower power, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((PowerModel)power).Owner) && !((decimal)((PowerModel)power).Amount > 1m))
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((PowerModel)power).DynamicVars.Damage, ((PowerModel)power).Owner);
		}
	}

	private static void HandleRegret(Regret card, SideTurnEndMirrorContext context)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (context.Participants.Contains(((CardModel)card).Owner.Creature))
		{
			SimPlayerCombatState playerCombatState = context.State.GetPlayerCombatState(((CardModel)card).Owner);
			PredictedCard predictedCard = playerCombatState.Hand.Find((CardModel)(object)card);
			if (predictedCard != null)
			{
				((Regret)predictedCard.MutablePreview).CardsInHand = playerCombatState.Hand.Cards.Count;
			}
		}
	}

	private static void HandleChainsOfBindingPower(ChainsOfBindingPower power, SideTurnEndMirrorContext context)
	{
		if (!context.Participants.Contains(((PowerModel)power).Owner))
		{
			return;
		}
		Player player = ((PowerModel)power).Owner.Player;
		if (player == null)
		{
			return;
		}
		foreach (PredictedCard allCard in context.State.GetPlayerCombatState(player).AllCards)
		{
			if (allCard.Preview.Affliction is Bound)
			{
				allCard.ClearAffliction();
			}
		}
	}

	private static bool HasPlayedAttackThisTurn(Player owner)
	{
		Player owner2 = owner;
		return CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(owner2.Creature.CombatState) && (int)entry.CardPlay.Card.Type == 1 && entry.CardPlay.Player == owner2);
	}

	private static bool AnyCardsPlayedThisTurn(Player owner)
	{
		Player owner2 = owner;
		PlayerCombatState playerCombatState = owner2.PlayerCombatState;
		if (playerCombatState != null && playerCombatState.TurnNumber == 1 && owner2.Relics.Any((RelicModel relic) => relic is WhisperingEarring))
		{
			return true;
		}
		return CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).Actor == owner2.Creature && ((CombatHistoryEntry)entry).HappenedThisTurn(owner2.Creature.CombatState) && !entry.CardPlay.IsAutoPlay);
	}
}
