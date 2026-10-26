using System;
using System.Threading;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Utils;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class DeckTransformPredictionContext
{
	private static readonly AsyncLocal<DeckTransformPredictionSource?> Current = new AsyncLocal<DeckTransformPredictionSource>();

	public static DeckTransformPredictionSource? CurrentSource => Current.Value;

	public static IDisposable EnterRelic(RelicModel relic)
	{
		return Enter(new DeckTransformPredictionSource.Relic(relic));
	}

	public static IDisposable? EnterEventOption(EventOption option)
	{
		if (!EventOptionEventModelMap.TryGetEvent(option, out EventModel eventModel))
		{
			return null;
		}
		return Enter(new DeckTransformPredictionSource.Event(eventModel, option));
	}

	private static IDisposable Enter(DeckTransformPredictionSource source)
	{
		DeckTransformPredictionSource previous = Current.Value;
		Current.Value = source;
		return new DisposableAction(delegate
		{
			Current.Value = previous;
		});
	}
}
