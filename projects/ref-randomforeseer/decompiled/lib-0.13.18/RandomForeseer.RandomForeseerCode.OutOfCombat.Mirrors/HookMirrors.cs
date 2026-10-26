using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors.Hooks.CardCreation;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors;

internal static class HookMirrors
{
	public static CardCreationOptions ModifyCardRewardCreationOptions(IRunState runState, Player player, CardCreationOptions options)
	{
		foreach (AbstractModel item in IterateRunHookListeners(runState))
		{
			options = item.ModifyCardRewardCreationOptions(player, options);
		}
		foreach (AbstractModel item2 in IterateRunHookListeners(runState))
		{
			options = item2.ModifyCardRewardCreationOptionsLate(player, options);
		}
		return options;
	}

	public static decimal ModifyCardRewardUpgradeOdds(IRunState runState, Player player, CardModel card, decimal odds)
	{
		foreach (AbstractModel item in IterateRunHookListeners(runState))
		{
			odds = item.ModifyCardRewardUpgradeOdds(player, card, odds);
		}
		return odds;
	}

	public static IEnumerable<CardModel> ModifyMerchantCardPool(IRunState runState, Player player, IEnumerable<CardModel> options)
	{
		foreach (AbstractModel item in IterateRunHookListeners(runState))
		{
			options = item.ModifyMerchantCardPool(player, options);
		}
		return options;
	}

	public static CardRarity ModifyMerchantCardRarity(IRunState runState, Player player, CardRarity rarity)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		foreach (AbstractModel item in IterateRunHookListeners(runState))
		{
			rarity = item.ModifyMerchantCardRarity(player, rarity);
		}
		return rarity;
	}

	public static decimal ModifyMerchantPrice(IRunState runState, Player player, MerchantEntry entry, decimal price)
	{
		foreach (AbstractModel item in IterateRunHookListeners(runState))
		{
			price = item.ModifyMerchantPrice(player, entry, price);
		}
		return price;
	}

	public static bool ShouldRefillMerchantEntry(IRunState runState, MerchantEntry entry, Player player)
	{
		foreach (AbstractModel item in IterateRunHookListeners(runState))
		{
			if (item.ShouldRefillMerchantEntry(entry, player))
			{
				return true;
			}
		}
		return false;
	}

	public static void ModifyMerchantCardCreationResults(RunPredictionContext runContext, List<CardCreationResult> results)
	{
		ModifyMerchantCardCreationResultsMirrorContext context = new ModifyMerchantCardCreationResultsMirrorContext
		{
			RunContext = runContext,
			Results = results
		};
		foreach (AbstractModel item in IterateRunHookListeners(runContext.RunState))
		{
			ModifyMerchantCardCreationResultsMirrors.Invoke(item, context);
		}
	}

	public static bool TryModifyCardRewardOptions(RunPredictionContext runContext, List<CardCreationResult> results, CardCreationOptions options, out List<AbstractModel> modifiers, IEnumerable<AbstractModel>? extraListeners = null)
	{
		TryModifyCardRewardOptionsMirrorContext context = new TryModifyCardRewardOptionsMirrorContext
		{
			RunContext = runContext,
			Results = results,
			Options = options
		};
		AbstractModel[] second = CompatibilityUtils.FilterHookListeners(extraListeners ?? Array.Empty<AbstractModel>()).ToArray();
		modifiers = new List<AbstractModel>();
		foreach (AbstractModel item in IterateRunHookListeners(runContext.RunState).Concat(second))
		{
			if (TryModifyCardRewardOptionsMirrors.Invoke(item, context))
			{
				modifiers.Add(item);
			}
		}
		foreach (AbstractModel item2 in IterateRunHookListeners(runContext.RunState).Concat(second))
		{
			if (TryModifyCardRewardOptionsMirrors.InvokeLate(item2, context))
			{
				modifiers.Add(item2);
			}
		}
		return modifiers.Count > 0;
	}

	private static IEnumerable<AbstractModel> IterateRunHookListeners(IRunState runState)
	{
		return CompatibilityUtils.FilterHookListeners(runState.IterateHookListeners((ICombatState)null));
	}
}
