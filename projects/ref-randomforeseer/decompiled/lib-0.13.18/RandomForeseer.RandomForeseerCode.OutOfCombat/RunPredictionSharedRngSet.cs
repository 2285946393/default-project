using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal sealed class RunPredictionSharedRngSet
{
	public required Rng Niche { get; init; }

	public static RunPredictionSharedRngSet From(RunRngSet rng)
	{
		return new RunPredictionSharedRngSet
		{
			Niche = rng.Niche.Clone()
		};
	}

	public RunPredictionSharedRngSet Clone()
	{
		return new RunPredictionSharedRngSet
		{
			Niche = Niche.Clone()
		};
	}
}
