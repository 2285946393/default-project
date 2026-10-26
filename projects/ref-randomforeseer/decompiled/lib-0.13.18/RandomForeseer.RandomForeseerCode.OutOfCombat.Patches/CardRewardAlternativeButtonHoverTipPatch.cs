using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NCardRewardSelectionScreen), "RefreshOptions")]
internal static class CardRewardAlternativeButtonHoverTipPatch
{
	private static void Postfix(IReadOnlyList<CardRewardAlternative> extraOptions, NCardRewardSelectionScreen __instance)
	{
		foreach (var (button, alternative) in ((IEnumerable)((Node)((Node)__instance).GetNode<Control>(NodePath.op_Implicit("UI/RewardAlternatives"))).GetChildren(false)).OfType<NCardRewardAlternativeButton>().Zip(extraOptions))
		{
			CardRewardAlternativeButtonHoverTips.RegisterButton((Control)(object)button, alternative);
		}
	}
}
