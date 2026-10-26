using System;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace RandomForeseer.RandomForeseerCode.Integrations.LemonSpire.Patches;

[HarmonyPatchCategory("RandomForeseer.LemonSpire")]
[HarmonyPatch]
internal static class LemonSpireHandCardPredictionPatch
{
	private static bool Prepare()
	{
		return LemonSpireTypes.Get("lemonSpire2.PlayerStateEx.PanelProvider.HandCardProvider") != null;
	}

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(LemonSpireTypes.Get("lemonSpire2.PlayerStateEx.PanelProvider.HandCardProvider"), "UpdateContent", (Type[])null, (Type[])null);
	}

	private static void Postfix(Player player, Control content)
	{
		foreach (NDeckHistoryEntry item in LemonSpireControlTree.Descendants(content).OfType<NDeckHistoryEntry>())
		{
			LemonSpireControlHoverTips.Register((Control)(object)item, player, (AbstractModel)(object)item.Card, LemonSpirePredictionKind.HandCard);
		}
	}
}
