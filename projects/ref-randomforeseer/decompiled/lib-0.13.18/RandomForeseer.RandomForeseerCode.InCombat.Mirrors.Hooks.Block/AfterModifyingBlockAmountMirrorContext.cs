using MegaCrit.Sts2.Core.Entities.Cards;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal sealed class AfterModifyingBlockAmountMirrorContext : CombatMirrorContext
{
	public required decimal ModifiedBlock { get; init; }

	public required PredictedCard? CardSource { get; init; }

	public required CardPlay? CardPlay { get; init; }
}
