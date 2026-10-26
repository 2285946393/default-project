using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class CardRewardPrediction
{
	[CompilerGenerated]
	private sealed class _003CCreateBaseRewards_003Ed__6 : IEnumerable<CardCreationResult>, IEnumerable, IEnumerator<CardCreationResult>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private CardCreationResult _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private Player player;

		public Player _003C_003E3__player;

		private CardCreationOptions options;

		public CardCreationOptions _003C_003E3__options;

		private Rng rewardRng;

		public Rng _003C_003E3__rewardRng;

		private CardRarityOdds rarityOdds;

		public CardRarityOdds _003C_003E3__rarityOdds;

		private int cardCount;

		public int _003C_003E3__cardCount;

		private List<CardModel> _003Cblacklist_003E5__2;

		private int _003Ci_003E5__3;

		CardCreationResult IEnumerator<CardCreationResult>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CCreateBaseRewards_003Ed__6(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Cblacklist_003E5__2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Expected O, but got Unknown
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003Cblacklist_003E5__2 = new List<CardModel>();
				_003Ci_003E5__3 = 0;
				break;
			case 1:
				_003C_003E1__state = -1;
				_003Ci_003E5__3++;
				break;
			}
			if (_003Ci_003E5__3 < cardCount)
			{
				CardModel val = CreateForReward(player, _003Cblacklist_003E5__2, options, rewardRng, rarityOdds);
				_003Cblacklist_003E5__2.Add(val.CanonicalInstance);
				if (!((Enum)options.Flags).HasFlag((Enum)(object)(CardCreationFlags)2))
				{
					RollForUpgrade(player, val, 0m, options.RngOverride ?? rewardRng);
				}
				_003C_003E2__current = new CardCreationResult(val);
				_003C_003E1__state = 1;
				return true;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<CardCreationResult> IEnumerable<CardCreationResult>.GetEnumerator()
		{
			_003CCreateBaseRewards_003Ed__6 _003CCreateBaseRewards_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CCreateBaseRewards_003Ed__ = this;
			}
			else
			{
				_003CCreateBaseRewards_003Ed__ = new _003CCreateBaseRewards_003Ed__6(0);
			}
			_003CCreateBaseRewards_003Ed__.player = _003C_003E3__player;
			_003CCreateBaseRewards_003Ed__.cardCount = _003C_003E3__cardCount;
			_003CCreateBaseRewards_003Ed__.options = _003C_003E3__options;
			_003CCreateBaseRewards_003Ed__.rewardRng = _003C_003E3__rewardRng;
			_003CCreateBaseRewards_003Ed__.rarityOdds = _003C_003E3__rarityOdds;
			return _003CCreateBaseRewards_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<CardCreationResult>)this).GetEnumerator();
		}
	}

	private static readonly HashSet<MethodInfo> WarnedUnsupportedAfterGeneratedHandlers = new HashSet<MethodInfo>();

	public static IReadOnlyList<CardModel> PredictCards(Player player, int cardCount, CardCreationOptions options, Action? afterGenerated = null, IEnumerable<AbstractModel>? extraResultModifiers = null)
	{
		return PredictCards(new RunPredictionContext(player), cardCount, options, afterGenerated, extraResultModifiers);
	}

	public static IReadOnlyList<CardModel> PredictCards(RunPredictionContext context, int cardCount, CardCreationOptions options, Action? afterGenerated = null, IEnumerable<AbstractModel>? extraResultModifiers = null)
	{
		List<CardCreationResult> list = CreateBaseRewards(context.Player, cardCount, options, context.Rng.Rewards, context.CardRarityOdds).ToList();
		ApplyRewardModifiers(context, list, options, afterGenerated, extraResultModifiers);
		return list.Select((CardCreationResult result) => result.Card).ToList();
	}

	public static IReadOnlyList<CardModel> ApplyRewardModifiersToExistingCards(RunPredictionContext context, IEnumerable<CardModel> cards, CardCreationOptions options, Action? afterGenerated = null, IEnumerable<AbstractModel>? extraResultModifiers = null)
	{
		List<CardCreationResult> list = cards.Select((Func<CardModel, CardCreationResult>)((CardModel card) => new CardCreationResult(card))).ToList();
		ApplyRewardModifiers(context, list, options, afterGenerated, extraResultModifiers);
		return list.Select((CardCreationResult result) => result.Card).ToList();
	}

	private static void ApplyRewardModifiers(RunPredictionContext context, List<CardCreationResult> results, CardCreationOptions options, Action? afterGenerated = null, IEnumerable<AbstractModel>? extraResultModifiers = null)
	{
		HookMirrors.TryModifyCardRewardOptions(context, results, options, out List<AbstractModel> _, extraResultModifiers);
		ApplyKnownAfterGeneratedModifiers(afterGenerated, results);
	}

	public static CardCreationOptions CloneOptions(CardCreationOptions options)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		CardCreationOptions val = new CardCreationOptions((IEnumerable<CardPoolModel>)options.CardPools.ToArray(), options.Source, options.RarityOdds, options.CardPoolFilter);
		val.WithFlags(options.Flags);
		if (options.RngOverride != null)
		{
			val.WithRngOverride(options.RngOverride.Clone());
		}
		return val;
	}

	[IteratorStateMachine(typeof(_003CCreateBaseRewards_003Ed__6))]
	internal static IEnumerable<CardCreationResult> CreateBaseRewards(Player player, int cardCount, CardCreationOptions options, Rng rewardRng, CardRarityOdds rarityOdds)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CCreateBaseRewards_003Ed__6(-2)
		{
			_003C_003E3__player = player,
			_003C_003E3__cardCount = cardCount,
			_003C_003E3__options = options,
			_003C_003E3__rewardRng = rewardRng,
			_003C_003E3__rarityOdds = rarityOdds
		};
	}

	private static CardModel CreateForReward(Player player, IEnumerable<CardModel> blacklist, CardCreationOptions options, Rng rewardRng, CardRarityOdds rarityOdds)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I4
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		options = HookMirrors.ModifyCardRewardCreationOptions(player.RunState, player, options);
		HashSet<CardModel> hashSet = blacklist.ToHashSet();
		IEnumerable<CardModel> enumerable = options.GetPossibleCards(player).Except(hashSet);
		CardModel[] source = CardFactory.FilterForPlayerCount(player.RunState, enumerable).ToArray();
		CardRarity selectedRarity = (CardRarity)0;
		IEnumerable<CardModel> enumerable2;
		if ((int)options.RarityOdds == 5)
		{
			enumerable2 = source.Where(delegate(CardModel card)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0009: Invalid comparison between Unknown and I4
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				//IL_000d: Invalid comparison between Unknown and I4
				CardRarity rarity = card.Rarity;
				return (int)rarity != 1 && (int)rarity != 5;
			});
		}
		else
		{
			HashSet<CardRarity> allowedRarities = source.Select((CardModel card) => card.Rarity).ToHashSet();
			selectedRarity = RollForRarity(rarityOdds, options.RarityOdds, options.Source, allowedRarities, ((Enum)options.Flags).HasFlag((Enum)(object)(CardCreationFlags)64));
			if ((int)selectedRarity == 0)
			{
				throw new InvalidOperationException($"Could not predict a valid card reward rarity. Odds: {options.RarityOdds}, candidates: {string.Join(",", source.Select((CardModel card) => ((AbstractModel)card).Id))}, card pools: {string.Join(",", options.CardPools.Select((CardPoolModel pool) => ((AbstractModel)pool).Id))}, blacklist: {string.Join(",", hashSet.Select((CardModel card) => ((AbstractModel)card).Id))}");
			}
			enumerable2 = source.Where((CardModel card) => card.Rarity == selectedRarity);
		}
		return PredictionUtils.CreateCard((options.RngOverride ?? rewardRng).NextItem<CardModel>(enumerable2) ?? throw new InvalidOperationException($"Could not predict a valid card reward. Selected rarity: {selectedRarity}, candidates: {string.Join(",", source.Select((CardModel card) => ((AbstractModel)card).Id))}, card pools: {string.Join(",", options.CardPools.Select((CardPoolModel pool) => ((AbstractModel)pool).Id))}, blacklist: {string.Join(",", hashSet.Select((CardModel card) => ((AbstractModel)card).Id))}"), player);
	}

	private static CardRarity RollForRarity(CardRarityOdds rarityOdds, CardRarityOddsType rollMethod, CardCreationSource source, HashSet<CardRarity> allowedRarities, bool forceRarityOddsChange)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		bool flag = forceRarityOddsChange;
		if (!flag)
		{
			bool flag2 = (int)source == 1;
			if (flag2)
			{
				bool flag3 = rollMethod - 1 <= 2;
				flag2 = flag3;
			}
			flag = flag2;
		}
		return GetNextAllowedRarity(flag ? rarityOdds.Roll(rollMethod) : rarityOdds.RollWithBaseOdds(rollMethod), allowedRarities.Contains);
	}

	private static CardRarity GetNextAllowedRarity(CardRarity rarity, Func<CardRarity, bool> isAllowed)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		CardRarity val = rarity;
		while (!isAllowed(rarity) && (int)rarity != 0)
		{
			rarity = CardRarityExtensions.GetNextHighestRarityWithWrapping(rarity);
			if (rarity == val)
			{
				return (CardRarity)0;
			}
		}
		return rarity;
	}

	internal static void RollForUpgrade(Player player, CardModel card, decimal baseChance, Rng rng)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		decimal num = (decimal)rng.NextFloat(1f);
		if (card.IsUpgradable)
		{
			decimal odds = baseChance;
			if ((int)card.Rarity != 4)
			{
				odds += (decimal)player.RunState.CurrentActIndex * AscensionHelper.GetValueIfAscension((AscensionLevel)7, 0.125m, 0.25m);
			}
			odds = HookMirrors.ModifyCardRewardUpgradeOdds(player.RunState, player, card, odds);
			if (num <= odds)
			{
				PredictionUtils.UpgradeCard(card);
			}
		}
	}

	private static void ApplyKnownAfterGeneratedModifiers(Action? afterGenerated, List<CardCreationResult> results)
	{
		if (afterGenerated == null)
		{
			return;
		}
		Delegate[] invocationList = afterGenerated.GetInvocationList();
		foreach (Delegate @delegate in invocationList)
		{
			if (IsTheFutureOfPotionsUpgradeCardsInReward(@delegate))
			{
				UpgradeAllValidCards(results);
			}
			else if (WarnedUnsupportedAfterGeneratedHandlers.Add(@delegate.Method))
			{
				Entry.Logger.Warn($"Card reward prediction does not safely mirror CardReward.AfterGenerated handler {@delegate.Method.DeclaringType?.FullName}.{@delegate.Method.Name}; preview may omit that modifier.", 1);
			}
		}
	}

	private static bool IsTheFutureOfPotionsUpgradeCardsInReward(Delegate handler)
	{
		string text = handler.Method.DeclaringType?.FullName ?? string.Empty;
		string text2 = handler.Target?.GetType().FullName ?? string.Empty;
		if (handler.Method.Name.Contains("UpgradeCardsInReward", StringComparison.Ordinal))
		{
			if (!text.Contains("TheFutureOfPotions", StringComparison.Ordinal))
			{
				return text2.Contains("TheFutureOfPotions", StringComparison.Ordinal);
			}
			return true;
		}
		return false;
	}

	private static void UpgradeAllValidCards(List<CardCreationResult> results)
	{
		foreach (CardCreationResult item in results.Where((CardCreationResult result) => result.Card.IsUpgradable))
		{
			item.ModifyCard(PredictionUtils.CreateUpgradedCard(item.Card));
		}
	}
}
