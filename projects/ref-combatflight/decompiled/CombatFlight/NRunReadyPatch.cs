using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace CombatFlight;

[HarmonyPatch(typeof(NRun), "_Ready")]
internal static class NRunReadyPatch
{
	private static void Postfix()
	{
		FlightController.Bind(RunManager.Instance.NetService);
	}
}
