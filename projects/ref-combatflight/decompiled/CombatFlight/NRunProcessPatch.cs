using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace CombatFlight;

[HarmonyPatch(typeof(NRun), "_Process")]
internal static class NRunProcessPatch
{
	private static void Postfix(double delta)
	{
		FlightController.Update(delta);
	}
}
