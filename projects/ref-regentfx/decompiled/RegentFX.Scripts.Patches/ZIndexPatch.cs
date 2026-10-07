using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public class ZIndexPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NCreature), "_Ready")]
	public static void Postfix(NCreature __instance)
	{
		((CanvasItem)__instance).ZIndex = 10;
		((CanvasItem)__instance).ZAsRelative = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	public static void NCombatRoomReadyPatch(NCombatRoom __instance)
	{
		if (__instance.BgContainer != null)
		{
			((CanvasItem)__instance.BgContainer).ZIndex = -20;
		}
		if (__instance.CombatVfxContainer != null)
		{
			((CanvasItem)__instance.CombatVfxContainer).ZIndex = 0;
		}
	}
}
