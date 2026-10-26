using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Power;

internal sealed class AfterModifyingPowerAmountMirrorContext : CombatMirrorContext
{
	public required PowerModel Power { get; init; }
}
