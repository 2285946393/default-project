using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ModifyShuffleOrderMirrorContext : CombatMirrorContext
{
	public required Player Player { get; init; }

	public required List<PredictedCard> Cards { get; init; }

	public required bool IsInitialShuffle { get; init; }
}
