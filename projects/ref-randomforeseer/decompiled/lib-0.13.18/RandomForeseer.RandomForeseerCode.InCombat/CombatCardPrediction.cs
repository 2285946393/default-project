using System;
using System.Collections;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatCardPrediction
{
	private readonly record struct ChoiceSimulationPlan(Player Creator, ChoiceSourceBehavior Behavior);

	private readonly record struct ChoiceSourceBehavior(bool SetsCardToFreeThisTurn = false);

	private static readonly Dictionary<Type, ChoiceSourceBehavior> ChoiceSourceBehaviors = new Dictionary<Type, ChoiceSourceBehavior>
	{
		[typeof(Abundance)] = new ChoiceSourceBehavior(SetsCardToFreeThisTurn: true),
		[typeof(Discovery)] = new ChoiceSourceBehavior(SetsCardToFreeThisTurn: true),
		[typeof(Quasar)] = default(ChoiceSourceBehavior),
		[typeof(Splash)] = new ChoiceSourceBehavior(SetsCardToFreeThisTurn: true),
		[typeof(AttackPotion)] = new ChoiceSourceBehavior(SetsCardToFreeThisTurn: true),
		[typeof(ColorlessPotion)] = new ChoiceSourceBehavior(SetsCardToFreeThisTurn: true),
		[typeof(PowerPotion)] = new ChoiceSourceBehavior(SetsCardToFreeThisTurn: true),
		[typeof(SkillPotion)] = new ChoiceSourceBehavior(SetsCardToFreeThisTurn: true),
		[typeof(Toolbox)] = default(ChoiceSourceBehavior)
	};

	public static IReadOnlyList<IHoverTip> GetHoverTips(CardModel card)
	{
		if (CombatManager.Instance.IsInProgress && ((AbstractModel)card).IsMutable && card != null)
		{
			Player owner = card.Owner;
			if (owner != null)
			{
				Creature creature = owner.Creature;
				if (creature != null && creature.CombatState != null)
				{
					List<IHoverTip> list = new List<IHoverTip>();
					if (ShouldShowCombatPlayPrediction(card))
					{
						try
						{
							IReadOnlyList<IHoverTip> hoverTips;
							IReadOnlyList<IHoverTip> collection = (IReadOnlyList<IHoverTip>)(CombatCardPredictionController.TryGetActiveHoverTips(card, out hoverTips) ? ((IEnumerable)hoverTips) : ((IEnumerable)(Predict(card, null)?.HoverTips ?? Array.Empty<IHoverTip>())));
							list.AddRange(collection);
						}
						catch (Exception ex)
						{
							Entry.Logger.Warn($"Combat card play prediction failed for {((AbstractModel)card).Id}: {ex}", 1);
							ModTelemetry.CaptureException(ex, "combat_card_prediction", "build_play_hover_tips", TelemetryContext.ForModel((AbstractModel)(object)card));
						}
					}
					try
					{
						list.AddRange(CombatTransformPrediction.GetCardHoverTips(card));
					}
					catch (Exception ex2)
					{
						Entry.Logger.Warn($"Combat transform prediction failed for {((AbstractModel)card).Id}: {ex2}", 1);
						ModTelemetry.CaptureException(ex2, "combat_card_prediction", "build_transform_hover_tips", TelemetryContext.ForModel((AbstractModel)(object)card));
					}
					return list;
				}
			}
		}
		return Array.Empty<IHoverTip>();
	}

	public static CombatPredictionProjection? Predict(CardModel card, Creature? target)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Invalid comparison between Unknown and I4
		if (CombatManager.Instance.IsInProgress)
		{
			ICombatState combatState = card.Owner.Creature.CombatState;
			if (combatState != null && CompatibilityUtils.IsPredictionAllowedForType(((object)card).GetType()) && TryResolveTarget(card, ref target))
			{
				using CombatPredictionSession combatPredictionSession = new CombatPredictionSession(combatState);
				CombatPredictionSimulator simulator = combatPredictionSession.Simulator;
				PredictedCard predictedCard = simulator.State.FindCard(card) ?? new PredictedCard(card);
				if (TryGetChoiceSimulationPlan(card, out var plan))
				{
					FastForwardChoiceResult(simulator, predictedCard, plan);
				}
				bool num;
				if (!ModData.Settings.FairModeEnabled)
				{
					num = !predictedCard.GetKeywords(simulator).Contains((CardKeyword)4);
				}
				else
				{
					SimCardPile pile = predictedCard.GetPile(simulator.State);
					if (pile == null || (int)pile.Type != 2)
					{
						goto IL_00b8;
					}
					num = simulator.CanPlay(predictedCard);
				}
				if (num)
				{
					simulator.ManualPlay(predictedCard, target, out PredictionTraceFrame frame);
					return CombatPredictionProjector.Project(simulator.History, frame);
				}
				goto IL_00b8;
				IL_00b8:
				return null;
			}
		}
		return null;
	}

	private static bool ShouldShowCombatPlayPrediction(CardModel card)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between Unknown and I4
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.CardPlayPredictionEnabled)
		{
			return false;
		}
		if (TryGetChoiceSimulationPlan(card, out var _))
		{
			return true;
		}
		CardPile pile = card.Pile;
		if (pile == null || (int)pile.Type != 2)
		{
			return false;
		}
		NPlayerHand instance = NPlayerHand.Instance;
		if (instance != null)
		{
			NCardHolder cardHolder = instance.GetCardHolder(card);
			if (cardHolder != null)
			{
				if ((int)instance.CurrentMode == 1)
				{
					return cardHolder is NHandCardHolder;
				}
				return false;
			}
		}
		return true;
	}

	private static bool TryResolveTarget(CardModel card, ref Creature? target)
	{
		if (card.IsValidTarget(target))
		{
			return true;
		}
		if (target == null)
		{
			IReadOnlyList<Creature> validTargets = card.GetValidTargets();
			if (validTargets != null && validTargets.Count >= 1)
			{
				Creature val = validTargets[0];
				target = val;
				return true;
			}
		}
		return false;
	}

	private static bool TryGetChoiceSimulationPlan(CardModel card, out ChoiceSimulationPlan plan)
	{
		plan = default(ChoiceSimulationPlan);
		if (!ChooseACardPredictionContext.TryGet(card, out AbstractModel source) || !ChoiceSourceBehaviors.TryGetValue(((object)source).GetType(), out var value))
		{
			return false;
		}
		CardModel val = (CardModel)(object)((source is CardModel) ? source : null);
		Player val4;
		if (val == null)
		{
			PotionModel val2 = (PotionModel)(object)((source is PotionModel) ? source : null);
			if (val2 == null)
			{
				RelicModel val3 = (RelicModel)(object)((source is RelicModel) ? source : null);
				val4 = ((val3 == null) ? null : val3.Owner);
			}
			else
			{
				val4 = val2.Owner;
			}
		}
		else
		{
			val4 = val.Owner;
		}
		Player val5 = val4;
		if (val5 == null)
		{
			return false;
		}
		plan = new ChoiceSimulationPlan(val5, value);
		return true;
	}

	private static void FastForwardChoiceResult(CombatPredictionSimulator simulator, PredictedCard selectedCard, ChoiceSimulationPlan plan)
	{
		if (plan.Behavior.SetsCardToFreeThisTurn)
		{
			selectedCard.SetToFreeThisTurn();
		}
		simulator.AddGeneratedCardToCombat(selectedCard, (PileType)2, plan.Creator, (CardPilePosition)1, CardGenerationResultKind.Fixed);
	}
}
