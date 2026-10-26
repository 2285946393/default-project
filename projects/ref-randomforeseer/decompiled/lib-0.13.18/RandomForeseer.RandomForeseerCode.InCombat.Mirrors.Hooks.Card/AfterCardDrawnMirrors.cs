using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class AfterCardDrawnMirrors
{
	private static readonly MirrorMethodSpec AfterCardDrawnEarly = MirrorMethodSpec.Hook("AfterCardDrawnEarly", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CardModel),
		typeof(bool)
	});

	private static readonly MirrorMethodSpec AfterCardDrawn = MirrorMethodSpec.Hook("AfterCardDrawn", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CardModel),
		typeof(bool)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext> EarlyRegistry = CreateEarlyRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext> Registry = CreateRegistry();

	public static void InvokeEarly(AbstractModel listener, AfterCardDrawnMirrorContext context)
	{
		EarlyRegistry.Invoke(listener, context);
	}

	public static void Invoke(AbstractModel listener, AfterCardDrawnMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext> CreateEarlyRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext>(AfterCardDrawnEarly);
		methodMirrorRegistry.Register<HellraiserPower>((Action<HellraiserPower, AfterCardDrawnMirrorContext>)HandleHellraiserPower);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCardDrawnMirrorContext>(AfterCardDrawn);
		methodMirrorRegistry.Register<ConfusedPower>((Action<ConfusedPower, AfterCardDrawnMirrorContext>)HandleConfusedPower);
		methodMirrorRegistry.Register<Slither>((Action<Slither, AfterCardDrawnMirrorContext>)HandleSlither);
		methodMirrorRegistry.Register<IterationPower>((Action<IterationPower, AfterCardDrawnMirrorContext>)HandleIterationPower);
		methodMirrorRegistry.Register<PagestormPower>((Action<PagestormPower, AfterCardDrawnMirrorContext>)HandlePagestormPower);
		methodMirrorRegistry.Register<ChainsOfBindingPower>((Action<ChainsOfBindingPower, AfterCardDrawnMirrorContext>)HandleChainsOfBindingPower);
		methodMirrorRegistry.Register<CorrosiveWavePower>((Action<CorrosiveWavePower, AfterCardDrawnMirrorContext>)HandleCorrosiveWavePower);
		methodMirrorRegistry.Register<SpeedsterPower>((Action<SpeedsterPower, AfterCardDrawnMirrorContext>)HandleSpeedsterPower);
		methodMirrorRegistry.Register<CacophonyPower>((Action<CacophonyPower, AfterCardDrawnMirrorContext>)HandleCacophonyPower);
		methodMirrorRegistry.Register<KinglyKick>((Action<KinglyKick, AfterCardDrawnMirrorContext>)HandleKinglyKick);
		methodMirrorRegistry.Register<KinglyPunch>((Action<KinglyPunch, AfterCardDrawnMirrorContext>)HandleKinglyPunch);
		methodMirrorRegistry.Register<AutomationPower>((Action<AutomationPower, AfterCardDrawnMirrorContext>)HandleAutomationPower);
		methodMirrorRegistry.Register<Void>((Action<Void, AfterCardDrawnMirrorContext>)HandleVoid);
		return methodMirrorRegistry;
	}

	private static void HandleHellraiserPower(HellraiserPower power, AfterCardDrawnMirrorContext context)
	{
		HellraiserPower power2 = power;
		AfterCardDrawnMirrorContext context2 = context;
		if (context2.PreviewCard.Owner.Creature != ((PowerModel)power2).Owner || !context2.PreviewCard.Tags.Contains((CardTag)1))
		{
			return;
		}
		HellraiserPredictionState hellraiserPredictionState = context2.StateStore.Get((AbstractModel)(object)power2, () => new HellraiserPredictionState(power2));
		bool flag = true;
		if (context2.State.HittableEnemies.All((Creature c) => HpDisplayExtensions.IsInfinite(context2.State.GetCreature(c).HpDisplay)))
		{
			if (hellraiserPredictionState.InfiniteAutoPlaysThisTurn >= 9)
			{
				flag = false;
			}
			hellraiserPredictionState.InfiniteAutoPlaysThisTurn++;
		}
		else
		{
			hellraiserPredictionState.InfiniteAutoPlaysThisTurn = 0;
		}
		if (flag)
		{
			context2.Simulator.AutoPlay(context2.Card, null, (AutoPlayType)1);
		}
	}

	private static void HandleConfusedPower(ConfusedPower power, AfterCardDrawnMirrorContext context)
	{
		if (context.PreviewCard.Owner == ((PowerModel)power).Owner.Player && context.PreviewCard.EnergyCost.Canonical >= 0)
		{
			SetRandomEnergyCost(context);
		}
	}

	private static void HandleSlither(Slither slither, AfterCardDrawnMirrorContext context)
	{
		if (context.Card.References(((EnchantmentModel)slither).Card) && context.State.GetPlayerCombatState(context.PreviewCard.Owner).Hand.Cards.Contains(context.Card))
		{
			SetRandomEnergyCost(context);
		}
	}

	private static void HandleIterationPower(IterationPower power, AfterCardDrawnMirrorContext context)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		Player player = ((PowerModel)power).Owner.Player;
		if (player != null && context.PreviewCard.Owner == player && (int)context.PreviewCard.Type == 4 && CountStatusCardsDrawnThisTurn(context.Simulator, player) <= 1)
		{
			context.Simulator.Draw(player, ((PowerModel)power).Amount);
		}
	}

	private static void HandlePagestormPower(PagestormPower power, AfterCardDrawnMirrorContext context)
	{
		Player player = ((PowerModel)power).Owner.Player;
		if (player != null && context.PreviewCard.Owner == player && context.Card.GetKeywords(context.Simulator).Contains((CardKeyword)2))
		{
			context.Simulator.Draw(player, ((PowerModel)power).Amount);
		}
	}

	private static void HandleChainsOfBindingPower(ChainsOfBindingPower power, AfterCardDrawnMirrorContext context)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		Player player = ((PowerModel)power).Owner.Player;
		if (player != null && context.PreviewCard.Owner == player && context.CombatState.CurrentSide == ((PowerModel)power).Owner.Side && ((AfflictionModel)ModelDb.Affliction<Bound>()).CanAfflict(context.PreviewCard) && CountBoundCardsAfflictedThisTurn(context.Simulator, player) < ((PowerModel)power).Amount)
		{
			context.Simulator.Afflict<Bound>(context.Card, (decimal)((PowerModel)power).Amount);
		}
	}

	private static void HandleSpeedsterPower(SpeedsterPower power, AfterCardDrawnMirrorContext context)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (!context.FromHandDraw && context.PreviewCard.Owner.Creature == ((PowerModel)power).Owner && context.PreviewCard.Owner.Creature.Side == context.CombatState.CurrentSide)
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
		}
	}

	private static void HandleCacophonyPower(CacophonyPower power, AfterCardDrawnMirrorContext context)
	{
		CacophonyPower power2 = power;
		CacophonyPredictionState cacophonyPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new CacophonyPredictionState(power2));
		cacophonyPredictionState.CardsDrawn--;
		if (cacophonyPredictionState.CardsDrawn <= 0)
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			cacophonyPredictionState.CardsDrawn = ((DynamicVar)((PowerModel)power2).CanonicalInstance.DynamicVars.Cards).IntValue;
			if (val != null)
			{
				context.Simulator.Damage(val, ((PowerModel)power2).Amount, (ValueProp)4, ((PowerModel)power2).Owner);
			}
		}
	}

	private static void HandleKinglyKick(KinglyKick card, AfterCardDrawnMirrorContext context)
	{
		if (context.Card.References(card))
		{
			context.MutablePreviewCard.EnergyCost.AddThisCombat(-1, false);
		}
	}

	private static void HandleKinglyPunch(KinglyPunch card, AfterCardDrawnMirrorContext context)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (context.Card.References(card))
		{
			KinglyPunch val = (KinglyPunch)context.MutablePreviewCard;
			decimal baseValue = ((CardModel)val).DynamicVars["Increase"].BaseValue;
			DamageVar damage = ((CardModel)val).DynamicVars.Damage;
			((DynamicVar)damage).BaseValue = ((DynamicVar)damage).BaseValue + baseValue;
			val.ExtraDamage += baseValue;
		}
	}

	private static void HandleAutomationPower(AutomationPower power, AfterCardDrawnMirrorContext context)
	{
		AutomationPower power2 = power;
		if (context.PreviewCard.Owner == ((PowerModel)power2).Owner.Player)
		{
			AutomationPredictionState automationPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new AutomationPredictionState(power2));
			automationPredictionState.CardsLeft--;
			if (automationPredictionState.CardsLeft <= 0)
			{
				context.Simulator.GainEnergy(context.PreviewCard.Owner, ((PowerModel)power2).Amount);
				automationPredictionState.CardsLeft = 10;
			}
		}
	}

	private static void HandleVoid(Void card, AfterCardDrawnMirrorContext context)
	{
		if (context.Card.References(card))
		{
			context.Simulator.LoseEnergy(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Energy).IntValue);
		}
	}

	private static void HandleCorrosiveWavePower(CorrosiveWavePower power, AfterCardDrawnMirrorContext context)
	{
		if (context.PreviewCard.Owner.Creature == ((PowerModel)power).Owner)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void SetRandomEnergyCost(AfterCardDrawnMirrorContext context)
	{
		context.MutablePreviewCard.EnergyCost.SetThisCombat(context.Rng.CombatEnergyCosts.NextInt(4), false);
	}

	private static int CountStatusCardsDrawnThisTurn(CombatPredictionSimulator simulator, Player player)
	{
		CombatPredictionSimulator simulator2 = simulator;
		Player player2 = player;
		return CombatManager.Instance.History.Entries.OfType<CardDrawnEntry>().Count((CardDrawnEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(simulator2.State.CombatState) && ((CombatHistoryEntry)entry).Actor == player2.Creature && (int)entry.Card.Type == 4) + simulator2.History.OfType<CombatPredictionCardDrawnEntry>().Count((CombatPredictionCardDrawnEntry entry) => entry.Card.Preview.Owner == player2 && (int)entry.Card.Preview.Type == 4);
	}

	private static int CountBoundCardsAfflictedThisTurn(CombatPredictionSimulator simulator, Player player)
	{
		CombatPredictionSimulator simulator2 = simulator;
		Player player2 = player;
		return CombatManager.Instance.History.Entries.OfType<CardAfflictedEntry>().Count((CardAfflictedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(simulator2.State.CombatState) && ((CombatHistoryEntry)entry).Actor == player2.Creature && entry.Affliction is Bound) + simulator2.History.OfType<CombatPredictionCardAfflictedEntry>().Count((CombatPredictionCardAfflictedEntry entry) => entry.Card.Preview.Owner == player2 && entry.Affliction is Bound);
	}
}
