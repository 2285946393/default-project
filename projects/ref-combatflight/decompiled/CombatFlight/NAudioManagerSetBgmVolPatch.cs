using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace CombatFlight;

[HarmonyPatch(typeof(NAudioManager), "SetBgmVol")]
internal static class NAudioManagerSetBgmVolPatch
{
	private static void Prefix(ref float volume)
	{
		FlightController.SuppressBgmWhileFlightSongIsPlaying(ref volume);
	}
}
