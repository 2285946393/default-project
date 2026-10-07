using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace MeleeAttack;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
public static class CharacterSelectHoverFadeResetPatch
{
	[HarmonyPostfix]
	[HarmonyPatch("_Ready")]
	private static void ReadyPostfix()
	{
		CharacterSelectHoverFadePatch.ResetGlobalState();
	}

	[HarmonyPostfix]
	[HarmonyPatch("OnSubmenuOpened")]
	private static void OpenedPostfix()
	{
		CharacterSelectHoverFadePatch.ResetGlobalState();
	}
}
