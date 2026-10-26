using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(EventModel), "SetEventState")]
internal static class EventModelSetEventStatePatch
{
	private static void Prefix(EventModel __instance, ref IEnumerable<EventOption> eventOptions)
	{
		foreach (EventOption item in eventOptions = eventOptions.Materialize())
		{
			EventOptionEventModelMap.Register(item, __instance);
		}
	}
}
