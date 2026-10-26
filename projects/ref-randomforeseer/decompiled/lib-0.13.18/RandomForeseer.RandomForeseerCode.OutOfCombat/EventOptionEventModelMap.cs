using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class EventOptionEventModelMap
{
	private static readonly ConditionalWeakTable<EventOption, EventModel> EventModels = new ConditionalWeakTable<EventOption, EventModel>();

	public static void Register(EventOption option, EventModel eventModel)
	{
		EventModels.AddOrUpdate(option, eventModel);
	}

	public static bool TryGetEvent(EventOption option, [NotNullWhen(true)] out EventModel? eventModel)
	{
		return EventModels.TryGetValue(option, out eventModel);
	}
}
