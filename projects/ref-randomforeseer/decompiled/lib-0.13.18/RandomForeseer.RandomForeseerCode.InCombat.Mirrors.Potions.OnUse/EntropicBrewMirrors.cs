using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

internal static class EntropicBrewMirrors
{
	public static void OnUse(EntropicBrew _, PotionOnUseMirrorContext context)
	{
		foreach (PotionModel item in Generate(context.TargetPlayer, context.Rng.CombatPotionGeneration))
		{
			context.History.PotionGenerated(item);
		}
	}

	public static IReadOnlyList<PotionModel> Generate(Player target, Rng rng)
	{
		return PredictionUtils.PredictPotionRewards(target, target.PotionSlots.Count, rng);
	}
}
