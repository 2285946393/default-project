using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class OutOfCombatPredictionUtils
{
	public static IReadOnlyList<CardModel> PredictDistinctDeckTransformResults(Player player, Rng realRng, int rngCounterOffset = 0, IEnumerable<CardModel>? extraTransformableCards = null)
	{
		Rng realRng2 = realRng;
		return (from card in PileTypeExtensions.GetPile((PileType)6, player).Cards.Concat(extraTransformableCards ?? Array.Empty<CardModel>())
			where (int)card.Type != 6 && card.IsTransformable
			select card).Select(delegate(CardModel card)
		{
			Rng rng = realRng2.Clone();
			if (rngCounterOffset != 0)
			{
				rng.Advance(rngCounterOffset);
			}
			return PredictionUtils.PredictTransformResult(card, rng, isInCombat: false);
		}).DistinctBy((CardModel card) => ((AbstractModel)card).Id).ToList();
	}

	public static IReadOnlyList<IReadOnlyList<CardModel>> PredictDistinctDeckTransformResultBundles(Player player, Rng realRng, int transformCount, IEnumerable<CardModel>? extraTransformableCards = null)
	{
		Player player2 = player;
		Rng realRng2 = realRng;
		IEnumerable<CardModel> extraTransformableCards2 = extraTransformableCards;
		return (from slot in Enumerable.Range(0, transformCount)
			select PredictDistinctDeckTransformResults(player2, realRng2, slot, extraTransformableCards2)).ToList();
	}

	public static void FastForwardDeckTransforms(RunPredictionContext context, int transformCount)
	{
		transformCount = Math.Min(transformCount, PileTypeExtensions.GetPile((PileType)6, context.Player).Cards.Count((CardModel card) => (int)card.Type != 6 && card.IsTransformable));
		context.SharedRng.Niche.Advance(transformCount);
	}

	public static CardCreationOptions CreateCharacterCardRewardOptions(Player player)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		return new CardCreationOptions((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(player.Character.CardPool), (CardCreationSource)3, (CardRarityOddsType)1, (Func<CardModel, bool>)null).WithFlags((CardCreationFlags)128);
	}

	public static CardCreationOptions CreateMonsterCombatCardRewardOptions(Player player)
	{
		return CardCreationOptions.ForRoom(player, (RoomType)1).WithFlags((CardCreationFlags)384);
	}

	public static IReadOnlyList<IReadOnlyList<CardModel>> PredictCardRewardBundles(Player player, int rewardCount, int optionCount, Func<CardCreationOptions> optionsFactory)
	{
		Func<CardCreationOptions> optionsFactory2 = optionsFactory;
		RunPredictionContext context = new RunPredictionContext(player);
		return (from _ in Enumerable.Range(0, rewardCount)
			select CardRewardPrediction.PredictCards(context, optionCount, optionsFactory2().WithFlags((CardCreationFlags)128))).ToList();
	}

	public static IReadOnlyList<CardModel> PredictUpgradedDeckCards(Player player, int count, Func<CardModel, bool> filter, Rng? rng = null)
	{
		if (rng == null)
		{
			rng = player.RunState.Rng.Niche.Clone();
		}
		return ListExtensions.StableShuffle<CardModel>(PileTypeExtensions.GetPile((PileType)6, player).Cards.Where(filter).ToList(), rng).Take(count).Select(PredictionUtils.CreateUpgradedCard)
			.ToList();
	}

	public static IReadOnlyList<CardModel> PredictUpgradedDeckCardsByNextItem(Player player, int count, Func<CardModel, bool> filter, Rng rng)
	{
		List<CardModel> list = PileTypeExtensions.GetPile((PileType)6, player).Cards.Where(filter).ToList();
		List<CardModel> list2 = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			CardModel val = rng.NextItem<CardModel>((IEnumerable<CardModel>)list);
			if (val == null)
			{
				break;
			}
			list.Remove(val);
			list2.Add(PredictionUtils.CreateUpgradedCard(val));
		}
		return list2;
	}

	public static IReadOnlyList<CardModel> PredictDowngradedDeckCardsByNextItem(Player player, int count, Func<CardModel, bool> filter, Rng rng)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		List<CardModel> list = PileTypeExtensions.GetPile((PileType)6, player).Cards.Where(filter).ToList();
		List<CardModel> list2 = new List<CardModel>();
		for (int i = 0; i < count; i++)
		{
			if (list.Count <= 0)
			{
				break;
			}
			CardModel val = rng.NextItem<CardModel>((IEnumerable<CardModel>)list);
			if (val == null)
			{
				break;
			}
			list.Remove(val);
			CardModel val2 = (CardModel)((AbstractModel)val).MutableClone();
			val2.DowngradeInternal();
			list2.Add(val2);
		}
		return list2;
	}

	public static IReadOnlyList<PotionModel> PredictUniformPotions(Player player, int count, Rng? rng = null, Func<PotionModel, bool>? filter = null)
	{
		if (rng == null)
		{
			rng = player.PlayerRng.Rewards.Clone();
		}
		if (filter == null)
		{
			filter = (PotionModel _) => true;
		}
		List<PotionModel> list = player.Character.PotionPool.GetUnlockedPotions(player.UnlockState).Concat(((PotionPoolModel)ModelDb.PotionPool<SharedPotionPool>()).GetUnlockedPotions(player.UnlockState)).Where(filter)
			.ToList();
		List<PotionModel> list2 = new List<PotionModel>();
		for (int i = 0; i < count; i++)
		{
			PotionModel val = rng.NextItem<PotionModel>((IEnumerable<PotionModel>)list);
			if (val == null)
			{
				break;
			}
			list2.Add(val);
		}
		return list2;
	}

	public static IReadOnlyList<RelicModel> PredictRelicRewards(Player player, int count)
	{
		return PredictRelicRewards(player, count, player.PlayerRng.Rewards.Clone());
	}

	public static IReadOnlyList<RelicModel> PredictRelicRewards(Player player, int count, Rng rng)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		RelicGrabBag val = player.RelicGrabBag.Clone();
		List<RelicModel> list = new List<RelicModel>();
		for (int i = 0; i < count; i++)
		{
			RelicRarity val2 = RelicFactory.RollRarity(rng);
			list.Add(val.PullFromFront(val2, player.RunState) ?? RelicFactory.FallbackRelic);
		}
		return list;
	}

	public static IReadOnlyList<RelicModel> PredictRelicRewards(Player player, IEnumerable<RelicRarity> rarities, Func<RelicModel, bool>? filter = null)
	{
		Func<RelicModel, bool> filter2 = filter;
		Player player2 = player;
		RelicGrabBag grabBag = player2.RelicGrabBag.Clone();
		return rarities.Select((RelicRarity rarity) => grabBag.PullFromFront(rarity, filter2 ?? ((Func<RelicModel, bool>)((RelicModel _) => true)), player2.RunState) ?? RelicFactory.FallbackRelic).ToList();
	}

	public static IReadOnlyList<RelicModel> PredictRelicRewards(RunPredictionContext context, int count)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		List<RelicModel> list = new List<RelicModel>(count);
		for (int i = 0; i < count; i++)
		{
			RelicRarity val = RelicFactory.RollRarity(context.Rng.Rewards);
			list.Add(context.RelicGrabBag.PullFromFront(val, context.RunState) ?? RelicFactory.FallbackRelic);
		}
		return list;
	}

	public static IReadOnlyList<IHoverTip> RelicTipsWithPickup(Player player, IReadOnlyList<RelicModel> relics)
	{
		List<IHoverTip> list = relics.ToPredictionHoverTips().ToList();
		foreach (RelicModel relic in relics)
		{
			list.AddRange(RelicPickupPrediction.GetHoverTips(player, relic));
		}
		return list;
	}

	public static void FastForwardMonsterRoomRewards(RunPredictionContext context)
	{
		FastForwardBeforeMonsterCardReward(context);
		CardCreationOptions options = CreateMonsterCombatCardRewardOptions(context.Player);
		CardRewardPrediction.PredictCards(context, 3, options);
	}

	public static void FastForwardBeforeMonsterCardReward(RunPredictionContext context)
	{
		bool num = context.PotionRewardOdds.Roll(context.Player, (RoomType)1);
		context.Rng.Rewards.NextInt(1);
		if (num)
		{
			PotionFactory.CreateRandomPotionOutOfCombat(context.Player, context.Rng.Rewards, (IEnumerable<PotionModel>)null);
		}
	}
}
