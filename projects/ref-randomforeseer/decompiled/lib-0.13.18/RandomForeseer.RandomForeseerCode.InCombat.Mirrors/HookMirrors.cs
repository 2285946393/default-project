using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Orb;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Power;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors;

internal static class HookMirrors
{
	public static void BeforePowerAmountChanged(CombatPredictionSimulator simulator, PowerModel power, decimal amount, Creature target, Creature? applier, PredictedCard? cardSource)
	{
		PowerAmountChangedMirrorContext context = new PowerAmountChangedMirrorContext
		{
			Simulator = simulator,
			Power = power,
			Amount = amount,
			Target = target,
			Applier = applier,
			CardSource = cardSource
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			PowerAmountChangedMirrors.InvokeBefore(item, context);
		}
	}

	public static decimal ModifyPowerAmountGiven(CombatPredictionSimulator simulator, PowerModel power, Creature applier, decimal amount, Creature target, PredictedCard? cardSource, out List<AbstractModel> modifiers)
	{
		ModifyPowerAmountMirrorContext modifyPowerAmountMirrorContext = new ModifyPowerAmountMirrorContext
		{
			Simulator = simulator,
			Power = power,
			Target = target,
			Applier = applier,
			CardSource = cardSource,
			Amount = amount
		};
		modifiers = new List<AbstractModel>();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			modifyPowerAmountMirrorContext.Amount = amount;
			decimal num = ModifyPowerAmountMirrors.InvokeGivenAdditive(item, modifyPowerAmountMirrorContext);
			if (num != 0m)
			{
				modifiers.Add(item);
			}
			amount += num;
		}
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator))
		{
			modifyPowerAmountMirrorContext.Amount = amount;
			decimal num2 = ModifyPowerAmountMirrors.InvokeGivenMultiplicative(item2, modifyPowerAmountMirrorContext);
			if (num2 != 1m)
			{
				modifiers.Add(item2);
			}
			amount *= num2;
		}
		return amount;
	}

	public static decimal ModifyPowerAmountReceived(CombatPredictionSimulator simulator, PowerModel power, Creature target, decimal amount, Creature? applier, out List<AbstractModel> modifiers)
	{
		ModifyPowerAmountMirrorContext modifyPowerAmountMirrorContext = new ModifyPowerAmountMirrorContext
		{
			Simulator = simulator,
			Power = power,
			Target = target,
			Applier = applier,
			CardSource = null,
			Amount = amount
		};
		modifiers = new List<AbstractModel>();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			modifyPowerAmountMirrorContext.Amount = amount;
			PowerAmountModification powerAmountModification = ModifyPowerAmountMirrors.InvokeReceived(item, modifyPowerAmountMirrorContext);
			if (powerAmountModification.WasModified)
			{
				modifiers.Add(item);
				amount = powerAmountModification.Amount;
			}
		}
		return amount;
	}

	public static void AfterModifyingPowerAmountGiven(CombatPredictionSimulator simulator, IEnumerable<AbstractModel> modifiers, PowerModel power)
	{
		AfterModifyingPowerAmountMirrorContext context = new AfterModifyingPowerAmountMirrorContext
		{
			Simulator = simulator,
			Power = power
		};
		HashSet<AbstractModel> hashSet = modifiers.ToHashSet();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			if (hashSet.Contains(item))
			{
				PowerAmountChangedMirrors.InvokeAfterGiven(item, context);
			}
		}
	}

	public static void AfterModifyingPowerAmountReceived(CombatPredictionSimulator simulator, IEnumerable<AbstractModel> modifiers, PowerModel power)
	{
		AfterModifyingPowerAmountMirrorContext context = new AfterModifyingPowerAmountMirrorContext
		{
			Simulator = simulator,
			Power = power
		};
		HashSet<AbstractModel> hashSet = modifiers.ToHashSet();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			if (hashSet.Contains(item))
			{
				PowerAmountChangedMirrors.InvokeAfterReceived(item, context);
			}
		}
	}

	public static void AfterPowerAmountChanged(CombatPredictionSimulator simulator, PowerModel power, decimal amount, Creature target, Creature? applier, PredictedCard? cardSource)
	{
		PowerAmountChangedMirrorContext context = new PowerAmountChangedMirrorContext
		{
			Simulator = simulator,
			Power = power,
			Amount = amount,
			Target = target,
			Applier = applier,
			CardSource = cardSource
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			PowerAmountChangedMirrors.InvokeAfter(item, context);
		}
	}

	public static int ModifyXValue(CombatPredictionSimulator simulator, CardModel card, int value)
	{
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			value = item.ModifyXValue(card, value);
		}
		return value;
	}

	public static void ModifyKeywordsInCombat(CombatPredictionSimulator simulator, CardModel card, ISet<CardKeyword> keywords)
	{
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			item.TryModifyKeywordsInCombat(card, keywords);
		}
	}

	public static bool ShouldPayExcessEnergyCostWithStars(CombatPredictionSimulator simulator, Player player)
	{
		Player player2 = player;
		return IterateCombatHookListeners(simulator).Any((AbstractModel listener) => listener.ShouldPayExcessEnergyCostWithStars(player2));
	}

	public static bool ShouldAfflict(CombatPredictionSimulator simulator, CardModel card, AfflictionModel affliction)
	{
		CardModel card2 = card;
		AfflictionModel affliction2 = affliction;
		return IterateCombatHookListeners(simulator).All((AbstractModel listener) => listener.ShouldAfflict(card2, affliction2));
	}

	public static decimal ModifyEnergyGain(CombatPredictionSimulator simulator, Player player, decimal amount, out IEnumerable<AbstractModel> modifiers)
	{
		List<AbstractModel> list = new List<AbstractModel>();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			decimal num = amount;
			amount = item.ModifyEnergyGain(player, amount);
			if ((int)num != (int)amount)
			{
				list.Add(item);
			}
		}
		modifiers = list;
		return amount;
	}

	public static bool ShouldGainStars(CombatPredictionSimulator simulator, decimal amount, Player player)
	{
		Player player2 = player;
		return IterateCombatHookListeners(simulator).All((AbstractModel listener) => listener.ShouldGainStars(amount, player2));
	}

	public static bool ShouldEtherealTrigger(CombatPredictionSimulator simulator, CardModel card)
	{
		CardModel card2 = card;
		return IterateCombatHookListeners(simulator).All((AbstractModel listener) => listener.ShouldEtherealTrigger(card2));
	}

	public static bool ShouldFlush(CombatPredictionSimulator simulator, Player player)
	{
		return Hook.ShouldFlush(simulator.State.CombatState, player);
	}

	public static void BeforeFlush(CombatPredictionSimulator simulator, Player player)
	{
		BeforeFlushMirrorContext context = new BeforeFlushMirrorContext
		{
			Simulator = simulator,
			Player = player
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			BeforeFlushMirrors.Invoke(item, context);
		}
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator))
		{
			BeforeFlushMirrors.InvokeLate(item2, context);
		}
	}

	public static bool ShouldStopCombatFromEnding(CombatPredictionSimulator simulator)
	{
		return IterateCombatHookListeners(simulator, applyCompatibilityFilter: true, allowWhenCombatEnding: true).Any((AbstractModel listener) => listener.ShouldStopCombatFromEnding());
	}

	public static Creature ModifyUnblockedDamageTarget(CombatPredictionSimulator simulator, Creature originalTarget, decimal amount, ValueProp props, Creature? dealer)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		ModifyUnblockedDamageTargetMirrorContext modifyUnblockedDamageTargetMirrorContext = new ModifyUnblockedDamageTargetMirrorContext
		{
			Simulator = simulator,
			Target = originalTarget,
			Amount = amount,
			Props = props,
			Dealer = dealer
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator, applyCompatibilityFilter: true, allowWhenCombatEnding: true))
		{
			modifyUnblockedDamageTargetMirrorContext.Target = ModifyUnblockedDamageTargetMirrors.Invoke(item, modifyUnblockedDamageTargetMirrorContext);
		}
		return modifyUnblockedDamageTargetMirrorContext.Target;
	}

	public static bool ShouldCreatureBeRemovedFromCombatAfterDeath(CombatPredictionSimulator simulator, Creature creature)
	{
		Creature creature2 = creature;
		return IterateCombatHookListeners(simulator, applyCompatibilityFilter: true, allowWhenCombatEnding: true).All((AbstractModel listener) => listener.ShouldCreatureBeRemovedFromCombatAfterDeath(creature2));
	}

	public static bool ShouldAllowHitting(CombatPredictionSimulator simulator, Creature creature)
	{
		ShouldAllowHittingMirrorContext context = new ShouldAllowHittingMirrorContext
		{
			Simulator = simulator,
			Creature = creature
		};
		return IterateCombatHookListeners(simulator).All((AbstractModel listener) => ShouldAllowHittingMirrors.Invoke(listener, context));
	}

	public static decimal ModifyBlock(CombatPredictionSimulator simulator, Creature target, decimal block, ValueProp props, PredictedCard? cardSource, CardPlay? cardPlay, out List<AbstractModel> modifiers)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		modifiers = new List<AbstractModel>();
		CardModel val = cardSource?.Preview;
		EnchantmentModel val2 = ((val != null) ? val.Enchantment : null);
		if (val2 != null)
		{
			block += val2.EnchantBlockAdditive(block);
			block *= val2.EnchantBlockMultiplicative(block);
		}
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			decimal num = item.ModifyBlockAdditive(target, block, props, val, cardPlay);
			block += num;
			if (num != 0m)
			{
				modifiers.Add(item);
			}
		}
		ModifyBlockMultiplicativeMirrorContext modifyBlockMultiplicativeMirrorContext = new ModifyBlockMultiplicativeMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Amount = block,
			Props = props,
			CardSource = cardSource,
			CardPlay = cardPlay
		};
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator))
		{
			modifyBlockMultiplicativeMirrorContext.Amount = block;
			decimal num2 = ModifyBlockMultiplicativeMirrors.Invoke(item2, modifyBlockMultiplicativeMirrorContext);
			block *= num2;
			if (num2 != 1m)
			{
				modifiers.Add(item2);
			}
		}
		return Math.Max(0m, block);
	}

	public static void AfterModifyingBlockAmount(CombatPredictionSimulator simulator, decimal modifiedBlock, PredictedCard? cardSource, CardPlay? cardPlay, IReadOnlyList<AbstractModel> modifiers)
	{
		AfterModifyingBlockAmountMirrorContext context = new AfterModifyingBlockAmountMirrorContext
		{
			Simulator = simulator,
			ModifiedBlock = modifiedBlock,
			CardSource = cardSource,
			CardPlay = cardPlay
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			if (modifiers.Contains(item))
			{
				AfterModifyingBlockAmountMirrors.Invoke(item, context);
			}
		}
	}

	public static void BeforeBlockGained(CombatPredictionSimulator simulator, Creature creature, decimal amount, ValueProp props, PredictedCard? source)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		BeforeBlockGainedMirrorContext context = new BeforeBlockGainedMirrorContext
		{
			Simulator = simulator,
			Creature = creature,
			Amount = amount,
			Props = props,
			Source = source
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			BeforeBlockGainedMirrors.Invoke(item, context);
		}
	}

	public static void AfterBlockGained(CombatPredictionSimulator simulator, Creature creature, decimal amount, ValueProp props, PredictedCard? source)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		AfterBlockGainedMirrorContext context = new AfterBlockGainedMirrorContext
		{
			Simulator = simulator,
			Creature = creature,
			Amount = amount,
			Props = props,
			Source = source
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterBlockGainedMirrors.Invoke(item, context);
		}
	}

	public static void AfterBlockBroken(CombatPredictionSimulator simulator, Creature target, Creature? breaker)
	{
		AfterBlockBrokenMirrorContext context = new AfterBlockBrokenMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Breaker = breaker
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator, applyCompatibilityFilter: true, allowWhenCombatEnding: true))
		{
			AfterBlockBrokenMirrors.Invoke(item, context);
		}
	}

	public static bool ShouldDraw(CombatPredictionSimulator simulator, Player player, bool fromHandDraw, [NotNullWhen(false)] out AbstractModel? modifier)
	{
		ShouldDrawMirrorContext context = new ShouldDrawMirrorContext
		{
			Simulator = simulator,
			Player = player,
			FromHandDraw = fromHandDraw
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			if (!ShouldDrawMirrors.Invoke(item, context))
			{
				modifier = item;
				return false;
			}
		}
		modifier = null;
		return true;
	}

	public static void AfterCardDrawn(CombatPredictionSimulator simulator, PredictedCard card, bool fromHandDraw)
	{
		AfterCardDrawnMirrorContext context = new AfterCardDrawnMirrorContext
		{
			Simulator = simulator,
			Card = card,
			FromHandDraw = fromHandDraw
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterCardDrawnMirrors.InvokeEarly(item, context);
		}
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator))
		{
			AfterCardDrawnMirrors.Invoke(item2, context);
		}
	}

	public static void AfterCardExhausted(CombatPredictionSimulator simulator, PredictedCard card, bool causedByEthereal)
	{
		AfterCardExhaustedMirrorContext context = new AfterCardExhaustedMirrorContext
		{
			Simulator = simulator,
			Card = card,
			CausedByEthereal = causedByEthereal
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterCardExhaustedMirrors.Invoke(item, context);
		}
	}

	public static void ModifyShuffleOrder(CombatPredictionSimulator simulator, Player player, List<PredictedCard> cards, bool isInitialShuffle)
	{
		ModifyShuffleOrderMirrorContext context = new ModifyShuffleOrderMirrorContext
		{
			Simulator = simulator,
			Player = player,
			Cards = cards,
			IsInitialShuffle = isInitialShuffle
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			ModifyShuffleOrderMirrors.Invoke(item, context);
		}
	}

	public static void AfterShuffle(CombatPredictionSimulator simulator, Player player)
	{
		AfterShuffleMirrorContext context = new AfterShuffleMirrorContext
		{
			Simulator = simulator,
			Player = player
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterShuffleMirrors.Invoke(item, context);
		}
	}

	public static void AfterCardDiscarded(CombatPredictionSimulator simulator, PredictedCard card)
	{
		AfterCardDiscardedMirrorContext context = new AfterCardDiscardedMirrorContext
		{
			Simulator = simulator,
			Card = card
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterCardDiscardedMirrors.Invoke(item, context);
		}
	}

	public static void AfterCardGeneratedForCombat(CombatPredictionSimulator simulator, PredictedCard card, Player? creator)
	{
		AfterCardGeneratedForCombatMirrorContext context = new AfterCardGeneratedForCombatMirrorContext
		{
			Simulator = simulator,
			Card = card,
			Creator = creator
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterCardGeneratedForCombatMirrors.Invoke(item, context);
		}
	}

	public static bool ShouldPlay(CombatPredictionSimulator simulator, PredictedCard card, [NotNullWhen(false)] out AbstractModel? preventer, AutoPlayType autoPlayType)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		ShouldPlayMirrorContext context = new ShouldPlayMirrorContext
		{
			Simulator = simulator,
			Card = card,
			AutoPlayType = autoPlayType
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			if (!ShouldPlayMirrors.Invoke(item, context))
			{
				preventer = item;
				return false;
			}
		}
		preventer = null;
		return true;
	}

	public static decimal ModifyEnergyCostInCombat(CombatPredictionSimulator simulator, PredictedCard card, decimal originalCost)
	{
		if (originalCost < 0m)
		{
			return originalCost;
		}
		ModifyEnergyCostInCombatMirrorContext modifyEnergyCostInCombatMirrorContext = new ModifyEnergyCostInCombatMirrorContext
		{
			Simulator = simulator,
			Card = card,
			Cost = originalCost
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			modifyEnergyCostInCombatMirrorContext.Cost = ModifyEnergyCostInCombatMirrors.Invoke(item, modifyEnergyCostInCombatMirrorContext);
		}
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator))
		{
			modifyEnergyCostInCombatMirrorContext.Cost = ModifyEnergyCostInCombatMirrors.InvokeLate(item2, modifyEnergyCostInCombatMirrorContext);
		}
		return modifyEnergyCostInCombatMirrorContext.Cost;
	}

	public static decimal ModifyStarCost(CombatPredictionSimulator simulator, PredictedCard card, decimal originalCost)
	{
		if (originalCost < 0m)
		{
			return originalCost;
		}
		ModifyStarCostMirrorContext modifyStarCostMirrorContext = new ModifyStarCostMirrorContext
		{
			Simulator = simulator,
			Card = card,
			Cost = originalCost
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			modifyStarCostMirrorContext.Cost = ModifyStarCostMirrors.Invoke(item, modifyStarCostMirrorContext);
		}
		return modifyStarCostMirrorContext.Cost;
	}

	public static int ModifyCardPlayCount(CombatPredictionSimulator simulator, PredictedCard card, int originalPlayCount, Creature? target, out List<AbstractModel> modifiers)
	{
		ModifyCardPlayCountMirrorContext modifyCardPlayCountMirrorContext = new ModifyCardPlayCountMirrorContext
		{
			Simulator = simulator,
			Card = card,
			Target = target,
			PlayCount = originalPlayCount
		};
		modifiers = new List<AbstractModel>();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			int playCount = modifyCardPlayCountMirrorContext.PlayCount;
			modifyCardPlayCountMirrorContext.PlayCount = ModifyCardPlayCountMirrors.Invoke(item, modifyCardPlayCountMirrorContext);
			if (modifyCardPlayCountMirrorContext.PlayCount != playCount)
			{
				modifiers.Add(item);
			}
		}
		return modifyCardPlayCountMirrorContext.PlayCount;
	}

	public static void AfterModifyingCardPlayCount(CombatPredictionSimulator simulator, PredictedCard card, IReadOnlyList<AbstractModel> modifiers)
	{
		AfterModifyingCardPlayCountMirrorContext context = new AfterModifyingCardPlayCountMirrorContext
		{
			Simulator = simulator,
			Card = card
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			if (modifiers.Contains(item))
			{
				ModifyCardPlayCountMirrors.InvokeAfter(item, context);
			}
		}
	}

	public static CardLocation ModifyCardPlayResultLocation(CombatPredictionSimulator simulator, PredictedCard card, bool isAutoPlay, ResourceInfo resources, CardLocation originalLocation, out List<AbstractModel> modifiers)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		ModifyCardPlayResultLocationMirrorContext modifyCardPlayResultLocationMirrorContext = new ModifyCardPlayResultLocationMirrorContext
		{
			Simulator = simulator,
			Card = card,
			IsAutoPlay = isAutoPlay,
			Resources = resources,
			Location = originalLocation
		};
		modifiers = new List<AbstractModel>();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			CardLocation location = modifyCardPlayResultLocationMirrorContext.Location;
			modifyCardPlayResultLocationMirrorContext.Location = ModifyCardPlayResultLocationMirrors.Invoke(item, modifyCardPlayResultLocationMirrorContext);
			if (modifyCardPlayResultLocationMirrorContext.Location != location)
			{
				modifiers.Add(item);
			}
		}
		return modifyCardPlayResultLocationMirrorContext.Location;
	}

	public static void AfterModifyingCardPlayResultLocation(CombatPredictionSimulator simulator, PredictedCard card, CardLocation location, IReadOnlyList<AbstractModel> modifiers)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		AfterModifyingCardPlayResultLocationMirrorContext context = new AfterModifyingCardPlayResultLocationMirrorContext
		{
			Simulator = simulator,
			Card = card,
			Location = location
		};
		foreach (AbstractModel modifier in modifiers)
		{
			ModifyCardPlayResultLocationMirrors.InvokeAfter(modifier, context);
		}
	}

	public static void BeforeCardPlayed(CombatPredictionSimulator simulator, PredictedCard card, CardPlay cardPlay)
	{
		BeforeCardPlayedMirrorContext context = new BeforeCardPlayedMirrorContext
		{
			Simulator = simulator,
			Card = card,
			CardPlay = cardPlay
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			BeforeCardPlayedMirrors.Invoke(item, context);
		}
	}

	public static void AfterCardPlayed(CombatPredictionSimulator simulator, PredictedCard card, CardPlay cardPlay)
	{
		AfterCardPlayedMirrorContext context = new AfterCardPlayedMirrorContext
		{
			Simulator = simulator,
			Card = card,
			CardPlay = cardPlay
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator, applyCompatibilityFilter: true, allowWhenCombatEnding: true))
		{
			AfterCardPlayedMirrors.Invoke(item, context);
		}
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator, applyCompatibilityFilter: true, allowWhenCombatEnding: true))
		{
			AfterCardPlayedMirrors.InvokeLate(item2, context);
		}
	}

	public static void AfterCurrentHpChanged(CombatPredictionSimulator simulator, Creature creature, decimal delta)
	{
		AfterCurrentHpChangedMirrorContext context = new AfterCurrentHpChangedMirrorContext
		{
			Simulator = simulator,
			Creature = creature,
			Delta = delta
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			AfterCurrentHpChangedMirrors.Invoke(item, context);
		}
	}

	public static decimal ModifyDamage(CombatPredictionSimulator simulator, Creature? target, Creature? dealer, decimal damage, ValueProp props, PredictedCard? cardSource, CardPlay? cardPlay)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		CardModel val = cardSource?.Preview;
		EnchantmentModel val2 = ((val != null) ? val.Enchantment : null);
		if (val2 != null)
		{
			damage += val2.EnchantDamageAdditive(damage, props);
			damage *= val2.EnchantDamageMultiplicative(damage, props);
		}
		ModifyDamageMirrorContext modifyDamageMirrorContext = new ModifyDamageMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Dealer = dealer,
			Amount = damage,
			Props = props,
			CardSource = cardSource,
			CardPlay = cardPlay
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator, applyCompatibilityFilter: false))
		{
			modifyDamageMirrorContext.Amount = damage;
			damage += ModifyDamageMirrors.InvokeAdditive(item, modifyDamageMirrorContext);
		}
		foreach (AbstractModel item2 in IterateRunHookListeners(simulator, applyCompatibilityFilter: false))
		{
			modifyDamageMirrorContext.Amount = damage;
			damage *= ModifyDamageMirrors.InvokeMultiplicative(item2, modifyDamageMirrorContext);
		}
		decimal num = decimal.MaxValue;
		foreach (AbstractModel item3 in IterateRunHookListeners(simulator, applyCompatibilityFilter: false))
		{
			num = Math.Min(num, item3.ModifyDamageCap(target, props, dealer, val, cardPlay));
		}
		return Math.Max(0m, Math.Min(damage, num));
	}

	public static decimal ModifyHpLost(CombatPredictionSimulator simulator, Creature target, decimal amount, ValueProp props, Creature? dealer, PredictedCard? cardSource, HpLossHookPhase phases, out List<AbstractModel> modifiers)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		ModifyHpLostMirrorContext modifyHpLostMirrorContext = new ModifyHpLostMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Amount = amount,
			Props = props,
			Dealer = dealer,
			CardSource = cardSource
		};
		modifiers = new List<AbstractModel>();
		if (((Enum)phases).HasFlag((Enum)(object)(HpLossHookPhase)1))
		{
			foreach (AbstractModel item in IterateRunHookListeners(simulator))
			{
				decimal amount2 = modifyHpLostMirrorContext.Amount;
				modifyHpLostMirrorContext.Amount = ModifyHpLostMirrors.InvokeBeforeOsty(item, modifyHpLostMirrorContext);
				if (decimal.Truncate(amount2) != decimal.Truncate(modifyHpLostMirrorContext.Amount))
				{
					modifiers.Add(item);
				}
			}
			foreach (AbstractModel item2 in IterateRunHookListeners(simulator))
			{
				decimal amount3 = modifyHpLostMirrorContext.Amount;
				modifyHpLostMirrorContext.Amount = ModifyHpLostMirrors.InvokeBeforeOstyLate(item2, modifyHpLostMirrorContext);
				if (decimal.Truncate(amount3) != decimal.Truncate(modifyHpLostMirrorContext.Amount))
				{
					modifiers.Add(item2);
				}
			}
		}
		if (((Enum)phases).HasFlag((Enum)(object)(HpLossHookPhase)2))
		{
			foreach (AbstractModel item3 in IterateRunHookListeners(simulator))
			{
				decimal amount4 = modifyHpLostMirrorContext.Amount;
				modifyHpLostMirrorContext.Amount = ModifyHpLostMirrors.InvokeAfterOsty(item3, modifyHpLostMirrorContext);
				if (decimal.Truncate(amount4) != decimal.Truncate(modifyHpLostMirrorContext.Amount))
				{
					modifiers.Add(item3);
				}
			}
			foreach (AbstractModel item4 in IterateRunHookListeners(simulator))
			{
				decimal amount5 = modifyHpLostMirrorContext.Amount;
				modifyHpLostMirrorContext.Amount = ModifyHpLostMirrors.InvokeAfterOstyLate(item4, modifyHpLostMirrorContext);
				if (decimal.Truncate(amount5) != decimal.Truncate(modifyHpLostMirrorContext.Amount))
				{
					modifiers.Add(item4);
				}
			}
		}
		return modifyHpLostMirrorContext.Amount;
	}

	public static void AfterDamageGiven(CombatPredictionSimulator simulator, Creature target, DamageResult result, ValueProp props, Creature? dealer, PredictedCard? source)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		AfterDamageGivenMirrorContext context = new AfterDamageGivenMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Result = result,
			Props = props,
			Dealer = dealer,
			Source = source
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			AfterDamageGivenMirrors.Invoke(item, context);
		}
	}

	public static void AfterModifyingHpLostAfterOsty(CombatPredictionSimulator simulator, IReadOnlyList<AbstractModel> modifiers)
	{
		AfterModifyingHpLostMirrorContext context = new AfterModifyingHpLostMirrorContext
		{
			Simulator = simulator
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			if (modifiers.Contains(item))
			{
				AfterModifyingHpLostAfterOstyMirrors.Invoke(item, context);
			}
		}
	}

	public static void BeforeDamageReceived(CombatPredictionSimulator simulator, Creature target, decimal amount, ValueProp props, Creature? dealer, PredictedCard? source)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		BeforeDamageReceivedMirrorContext context = new BeforeDamageReceivedMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Amount = amount,
			Props = props,
			Dealer = dealer,
			Source = source
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			BeforeDamageReceivedMirrors.Invoke(item, context);
		}
	}

	public static void AfterDamageReceived(CombatPredictionSimulator simulator, Creature target, DamageResult result, ValueProp props, Creature? dealer, PredictedCard? source)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		AfterDamageReceivedMirrorContext context = new AfterDamageReceivedMirrorContext
		{
			Simulator = simulator,
			Target = target,
			Result = result,
			Props = props,
			Dealer = dealer,
			Source = source
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			AfterDamageReceivedMirrors.Invoke(item, context);
		}
		foreach (AbstractModel item2 in IterateRunHookListeners(simulator))
		{
			AfterDamageReceivedMirrors.InvokeLate(item2, context);
		}
	}

	public static void BeforeAttack(CombatPredictionSimulator simulator, AttackCommand command)
	{
		BeforeAttackMirrorContext context = new BeforeAttackMirrorContext
		{
			Simulator = simulator,
			Command = command
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			BeforeAttackMirrors.Invoke(item, context);
		}
	}

	public static int ModifyAttackHitCount(CombatPredictionSimulator simulator, AttackCommand command, int originalHitCount)
	{
		ModifyAttackHitCountMirrorContext modifyAttackHitCountMirrorContext = new ModifyAttackHitCountMirrorContext
		{
			Simulator = simulator,
			Command = command,
			HitCount = originalHitCount
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			modifyAttackHitCountMirrorContext.HitCount = ModifyAttackHitCountMirrors.Invoke(item, modifyAttackHitCountMirrorContext);
		}
		return modifyAttackHitCountMirrorContext.HitCount;
	}

	public static void AfterAttack(CombatPredictionSimulator simulator, AttackCommand command)
	{
		AfterAttackMirrorContext context = new AfterAttackMirrorContext
		{
			Simulator = simulator,
			Command = command
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterAttackMirrors.Invoke(item, context);
		}
	}

	public static bool ShouldDie(CombatPredictionSimulator simulator, Creature creature, [NotNullWhen(false)] out AbstractModel? preventer)
	{
		ShouldDieMirrorContext context = new ShouldDieMirrorContext
		{
			Simulator = simulator,
			Creature = creature
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			if (!ShouldDieMirrors.Invoke(item, context))
			{
				preventer = item;
				return false;
			}
		}
		foreach (AbstractModel item2 in IterateRunHookListeners(simulator))
		{
			if (!ShouldDieMirrors.InvokeLate(item2, context))
			{
				preventer = item2;
				return false;
			}
		}
		preventer = null;
		return true;
	}

	public static void AfterPreventingDeath(CombatPredictionSimulator simulator, AbstractModel preventer, Creature creature)
	{
		AfterPreventingDeathMirrorContext context = new AfterPreventingDeathMirrorContext
		{
			Simulator = simulator,
			Creature = creature
		};
		if (IterateRunHookListeners(simulator).Contains(preventer))
		{
			AfterPreventingDeathMirrors.Invoke(preventer, context);
		}
	}

	public static void BeforeDeath(CombatPredictionSimulator simulator, Creature creature)
	{
		BeforeDeathMirrorContext context = new BeforeDeathMirrorContext
		{
			Simulator = simulator,
			Creature = creature
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			BeforeDeathMirrors.Invoke(item, context);
		}
	}

	public static void AfterDeath(CombatPredictionSimulator simulator, Creature creature, bool wasRemovalPrevented)
	{
		AfterDeathMirrorContext context = new AfterDeathMirrorContext
		{
			Simulator = simulator,
			Creature = creature,
			WasRemovalPrevented = wasRemovalPrevented
		};
		foreach (AbstractModel item in IterateRunHookListeners(simulator))
		{
			AfterDeathMirrors.Invoke(item, context);
		}
	}

	public static int ModifyOrbPassiveTriggerCount(CombatPredictionSimulator simulator, OrbModel orb, int triggerCount, out List<AbstractModel> modifiers)
	{
		ModifyOrbPassiveTriggerCountMirrorContext modifyOrbPassiveTriggerCountMirrorContext = new ModifyOrbPassiveTriggerCountMirrorContext
		{
			Simulator = simulator,
			Orb = orb,
			TriggerCount = triggerCount
		};
		modifiers = new List<AbstractModel>();
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			int num = ModifyOrbPassiveTriggerCountMirrors.Invoke(item, modifyOrbPassiveTriggerCountMirrorContext);
			if (num != modifyOrbPassiveTriggerCountMirrorContext.TriggerCount)
			{
				modifyOrbPassiveTriggerCountMirrorContext.TriggerCount = num;
				modifiers.Add(item);
			}
		}
		return modifyOrbPassiveTriggerCountMirrorContext.TriggerCount;
	}

	public static void AfterOrbChanneled(CombatPredictionSimulator simulator, Player player, OrbModel orb)
	{
		AfterOrbChanneledMirrorContext context = new AfterOrbChanneledMirrorContext
		{
			Simulator = simulator,
			Player = player,
			Orb = orb
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterOrbChanneledMirrors.Invoke(item, context);
		}
	}

	public static void AfterOrbEvoked(CombatPredictionSimulator simulator, OrbModel orb, IReadOnlyList<Creature> targets)
	{
		AfterOrbEvokedMirrorContext context = new AfterOrbEvokedMirrorContext
		{
			Simulator = simulator,
			Orb = orb,
			Targets = targets
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterOrbEvokedMirrors.Invoke(item, context);
		}
	}

	public static void AfterAutoPostPlayPhaseEntered(CombatPredictionSimulator simulator, Player player)
	{
		AfterAutoPostPlayMirrorContext context = new AfterAutoPostPlayMirrorContext
		{
			Simulator = simulator,
			Player = player
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterAutoPostPlayPhaseEnteredMirrors.Invoke(item, context);
		}
	}

	public static void BeforeSideTurnEnd(CombatPredictionSimulator simulator, CombatSide side, IReadOnlyList<Creature> participants)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		SideTurnEndMirrorContext context = new SideTurnEndMirrorContext
		{
			Simulator = simulator,
			Side = side,
			Participants = participants
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			BeforeSideTurnEndMirrors.InvokeVeryEarly(item, context);
		}
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator))
		{
			BeforeSideTurnEndMirrors.InvokeEarly(item2, context);
		}
		foreach (AbstractModel item3 in IterateCombatHookListeners(simulator))
		{
			BeforeSideTurnEndMirrors.Invoke(item3, context);
		}
	}

	public static void AfterFlush(CombatPredictionSimulator simulator, Player player, IReadOnlyList<PredictedCard> flushedCards, IReadOnlyList<PredictedCard> retainedCards)
	{
		AfterFlushMirrorContext context = new AfterFlushMirrorContext
		{
			Simulator = simulator,
			Player = player,
			FlushedCards = flushedCards,
			RetainedCards = retainedCards
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterFlushMirrors.Invoke(item, context);
		}
	}

	public static void AfterSideTurnEnd(CombatPredictionSimulator simulator, CombatSide side, IReadOnlyList<Creature> participants)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		SideTurnEndMirrorContext context = new SideTurnEndMirrorContext
		{
			Simulator = simulator,
			Side = side,
			Participants = participants
		};
		foreach (AbstractModel item in IterateCombatHookListeners(simulator))
		{
			AfterSideTurnEndMirrors.Invoke(item, context);
		}
		foreach (AbstractModel item2 in IterateCombatHookListeners(simulator))
		{
			AfterSideTurnEndMirrors.InvokeLate(item2, context);
		}
	}

	private static IEnumerable<AbstractModel> IterateCombatHookListeners(CombatPredictionSimulator simulator, bool applyCompatibilityFilter = true, bool allowWhenCombatEnding = false)
	{
		if (!allowWhenCombatEnding && simulator.IsOverOrEnding)
		{
			return Array.Empty<AbstractModel>();
		}
		IEnumerable<AbstractModel> enumerable = simulator.State.IterateHookListeners();
		if (!applyCompatibilityFilter)
		{
			return enumerable;
		}
		return CompatibilityUtils.FilterHookListeners(enumerable);
	}

	private static IEnumerable<AbstractModel> IterateRunHookListeners(CombatPredictionSimulator simulator, bool applyCompatibilityFilter = true)
	{
		ICombatState combatState = simulator.State.CombatState;
		IEnumerable<AbstractModel> enumerable = combatState.RunState.IterateHookListeners(combatState);
		if (!applyCompatibilityFilter)
		{
			return enumerable;
		}
		return CompatibilityUtils.FilterHookListeners(enumerable);
	}
}
