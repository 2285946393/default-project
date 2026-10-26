using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal sealed class ModifyDamageMirrorContext : CombatMirrorContext
{
	public required Creature? Target { get; init; }

	public required Creature? Dealer { get; init; }

	public required decimal Amount { get; set; }

	public required ValueProp Props { get; init; }

	public required PredictedCard? CardSource { get; init; }

	public required CardPlay? CardPlay { get; init; }
}
