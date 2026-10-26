using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal sealed class ModifyUnblockedDamageTargetMirrorContext : CombatMirrorContext
{
	public required Creature Target { get; set; }

	public required decimal Amount { get; init; }

	public required ValueProp Props { get; init; }

	public required Creature? Dealer { get; init; }
}
