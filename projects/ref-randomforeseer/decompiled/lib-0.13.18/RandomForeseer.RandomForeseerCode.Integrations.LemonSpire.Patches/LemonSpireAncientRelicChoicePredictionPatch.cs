using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace RandomForeseer.RandomForeseerCode.Integrations.LemonSpire.Patches;

[HarmonyPatchCategory("RandomForeseer.LemonSpire")]
[HarmonyPatch]
internal static class LemonSpireAncientRelicChoicePredictionPatch
{
	private static bool Prepare()
	{
		return LemonSpireTypes.Get("lemonSpire2.PlayerStateEx.PanelProvider.AncientRelicChoiceProvider") != null;
	}

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(LemonSpireTypes.Get("lemonSpire2.PlayerStateEx.PanelProvider.AncientRelicChoiceProvider"), "AddRelicItem", (Type[])null, (Type[])null);
	}

	private static void Prefix(HBoxContainer row, out HashSet<Control> __state)
	{
		__state = LemonSpireControlTree.SnapshotDescendants((Control)(object)row);
	}

	private static void Postfix(HBoxContainer row, Player player, RelicModel relic, HashSet<Control> __state)
	{
		foreach (Control item in LemonSpireControlTree.NewDescendants((Control)(object)row, __state))
		{
			if (item is NRelicBasicHolder)
			{
				LemonSpireControlHoverTips.Register(item, player, (AbstractModel)(object)relic, LemonSpirePredictionKind.AncientRelicChoice);
				break;
			}
		}
	}
}
