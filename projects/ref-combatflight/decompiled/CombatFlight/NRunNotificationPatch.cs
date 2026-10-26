using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace CombatFlight;

[HarmonyPatch(typeof(NRun), "_Notification")]
internal static class NRunNotificationPatch
{
	private const int NotificationPredelete = 1006;

	private static void Prefix(int what)
	{
		if (what == 1006)
		{
			FlightController.Unbind();
		}
	}
}
