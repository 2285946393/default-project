using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace CombatFlight;

[HarmonyPatch(typeof(NGame), "_Input")]
internal static class NGameInputPatch
{
	private static bool Prefix(NGame __instance, InputEvent inputEvent)
	{
		if (!FlightController.HandleInput(inputEvent))
		{
			return true;
		}
		((Node)__instance).GetViewport().SetInputAsHandled();
		return false;
	}
}
