using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal sealed class RunPredictionPlayerRngSet
{
	public required Rng Rewards { get; init; }

	public required Rng Shops { get; init; }

	public static RunPredictionPlayerRngSet From(PlayerRngSet rng)
	{
		return new RunPredictionPlayerRngSet
		{
			Rewards = rng.Rewards.Clone(),
			Shops = rng.Shops.Clone()
		};
	}

	public RunPredictionPlayerRngSet Clone()
	{
		return new RunPredictionPlayerRngSet
		{
			Rewards = Rewards.Clone(),
			Shops = Shops.Clone()
		};
	}
}
