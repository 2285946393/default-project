using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class RestSitePrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(RestSiteOption option)
	{
		if (!(option is DigRestSiteOption))
		{
			if (option is HealRestSiteOption)
			{
				return PredictRestTips(option.Owner);
			}
			return Array.Empty<IHoverTip>();
		}
		return PredictDigTips(option.Owner);
	}

	public static IReadOnlyList<IHoverTip> PredictDigTips(Player player)
	{
		IReadOnlyList<RelicModel> relics = OutOfCombatPredictionUtils.PredictRelicRewards(player, 1);
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(player, relics);
	}

	public static IReadOnlyList<IHoverTip> PredictRestTips(Player player)
	{
		RunPredictionContext runPredictionContext = new RunPredictionContext(player);
		List<IHoverTip> list = new List<IHoverTip>();
		foreach (RelicModel item in player.Relics.Where((RelicModel relic) => !relic.IsMelted))
		{
			if (!(item is DreamCatcher))
			{
				if (item is TinyMailbox)
				{
					IReadOnlyList<PotionModel> potions = PredictionUtils.PredictPotionRewards(player, 2, runPredictionContext.Rng.Rewards);
					list.AddRange(potions.ToPredictionHoverTips());
				}
			}
			else
			{
				CardCreationOptions options = CardCreationOptions.ForRoom(player, (RoomType)1).WithFlags((CardCreationFlags)128);
				list.AddRange(CardRewardPrediction.PredictCards(runPredictionContext, 3, options).ToPredictionHoverTips());
			}
		}
		return list;
	}
}
