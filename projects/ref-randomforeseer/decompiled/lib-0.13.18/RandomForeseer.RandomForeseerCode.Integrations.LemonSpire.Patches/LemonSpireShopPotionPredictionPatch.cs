using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace RandomForeseer.RandomForeseerCode.Integrations.LemonSpire.Patches;

[HarmonyPatchCategory("RandomForeseer.LemonSpire")]
[HarmonyPatch]
internal static class LemonSpireShopPotionPredictionPatch
{
	private static bool Prepare()
	{
		return LemonSpireTypes.Get("lemonSpire2.PlayerStateEx.PanelProvider.ShopProvider") != null;
	}

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(LemonSpireTypes.Get("lemonSpire2.PlayerStateEx.PanelProvider.ShopProvider"), "AddPotionItem", (Type[])null, (Type[])null);
	}

	private static void Prefix(HBoxContainer row, out HashSet<Control> __state)
	{
		__state = LemonSpireControlTree.SnapshotDescendants((Control)(object)row);
	}

	private static void Postfix(HBoxContainer row, Player player, HashSet<Control> __state)
	{
		foreach (Control item in LemonSpireControlTree.NewDescendants((Control)(object)row, __state))
		{
			NPotionHolder val = (NPotionHolder)(object)((item is NPotionHolder) ? item : null);
			if (val == null)
			{
				continue;
			}
			NPotion potion = val.Potion;
			if (potion != null)
			{
				PotionModel model = potion.Model;
				if (model != null)
				{
					LemonSpireControlHoverTips.Register(item, player, (AbstractModel)(object)model, LemonSpirePredictionKind.ShopPotion);
					break;
				}
			}
		}
	}
}
