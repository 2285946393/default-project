using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed record DamagePrediction(IReadOnlyList<DamagePredictionTarget> Targets)
{
	public static DamagePrediction Empty { get; } = new DamagePrediction(Array.Empty<DamagePredictionTarget>());


	public bool HasTargets => Targets.Count > 0;

	[CompilerGenerated]
	private DamagePrediction(DamagePrediction original)
	{
		Targets = original.Targets;
	}
}
