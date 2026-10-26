using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NEventOptionButton), "Create")]
internal static class EventOptionButtonCreatePatch
{
	private static void Prefix(EventModel eventModel, EventOption option)
	{
		EventOptionEventModelMap.Register(option, eventModel);
	}
}
