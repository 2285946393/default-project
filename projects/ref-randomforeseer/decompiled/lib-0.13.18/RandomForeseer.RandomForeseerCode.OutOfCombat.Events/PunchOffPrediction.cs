using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class PunchOffPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(PunchOff punchOff, EventOption option)
	{
		Player owner = ((EventModel)punchOff).Owner;
		return option.TextKey switch
		{
			"PUNCH_OFF.pages.INITIAL.options.NAB" => OutOfCombatPredictionUtils.RelicTipsWithPickup(owner, OutOfCombatPredictionUtils.PredictRelicRewards(owner, 1)), 
			"PUNCH_OFF.pages.INITIAL.options.I_CAN_TAKE_THEM" => PredictFightRewards(owner), 
			"PUNCH_OFF.pages.I_CAN_TAKE_THEM.options.FIGHT" => PredictFightRewards(owner), 
			_ => Array.Empty<IHoverTip>(), 
		};
	}

	private static IReadOnlyList<IHoverTip> PredictFightRewards(Player player)
	{
		if (!ModData.Settings.Allows(PredictionFairness.UnfairInAllModes))
		{
			return Array.Empty<IHoverTip>();
		}
		RunPredictionContext runPredictionContext = new RunPredictionContext(player);
		CombatEndEffectPrediction.FastForwardMonsterRoomCombatEndHooks(runPredictionContext);
		foreach (Player player2 in ((IPlayerCollection)runPredictionContext.RunState).Players)
		{
			OutOfCombatPredictionUtils.FastForwardMonsterRoomRewards(runPredictionContext.ForPlayer(player2));
			if (player2 == runPredictionContext.Player)
			{
				break;
			}
		}
		IReadOnlyList<RelicModel> relics = OutOfCombatPredictionUtils.PredictRelicRewards(runPredictionContext, 1);
		PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(player, runPredictionContext.Rng.Rewards, (IEnumerable<PotionModel>)null);
		List<IHoverTip> list = OutOfCombatPredictionUtils.RelicTipsWithPickup(player, relics).ToList();
		list.Add(PredictionHoverTipFactory.Potion(potion));
		return list;
	}
}
