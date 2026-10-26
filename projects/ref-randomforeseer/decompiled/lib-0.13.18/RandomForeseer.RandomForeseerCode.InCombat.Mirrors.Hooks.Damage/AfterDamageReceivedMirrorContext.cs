using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal sealed class AfterDamageReceivedMirrorContext : CombatMirrorContext
{
	public required Creature Target { get; init; }

	public required DamageResult Result { get; init; }

	public required ValueProp Props { get; init; }

	public required Creature? Dealer { get; init; }

	public required PredictedCard? Source { get; init; }
}
