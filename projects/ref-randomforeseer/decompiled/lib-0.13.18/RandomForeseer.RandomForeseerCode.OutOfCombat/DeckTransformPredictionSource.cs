using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal abstract record DeckTransformPredictionSource
{
	internal sealed record Relic(RelicModel Model) : DeckTransformPredictionSource();

	internal sealed record Event(EventModel Model, EventOption Option) : DeckTransformPredictionSource();
}
