using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal sealed class AfterFlushMirrorContext : CombatMirrorContext
{
	public required Player Player { get; init; }

	public required IReadOnlyList<PredictedCard> FlushedCards { get; init; }

	public required IReadOnlyList<PredictedCard> RetainedCards { get; init; }
}
