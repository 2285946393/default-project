using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Power;

internal sealed class PowerAmountChangedMirrorContext : CombatMirrorContext
{
	public required PowerModel Power { get; init; }

	public required decimal Amount { get; init; }

	public required Creature Target { get; init; }

	public required Creature? Applier { get; init; }

	public required PredictedCard? CardSource { get; init; }
}
