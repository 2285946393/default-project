using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.InCombat;
using RandomForeseer.RandomForeseerCode.Integrations.LemonSpire;
using RandomForeseer.RandomForeseerCode.OutOfCombat;

namespace RandomForeseer.RandomForeseerCode.Common.Patches;

[HarmonyPatch(typeof(NHoverTipSet), "CreateAndShow", new Type[]
{
	typeof(Control),
	typeof(IEnumerable<IHoverTip>),
	typeof(HoverTipAlignment)
})]
internal static class ControlHoverTipPredictionPatch
{
	private static readonly PredictionHoverTipRegistry<Control> Registry = CreateRegistry();

	private static PredictionHoverTipRegistry<Control> CreateRegistry()
	{
		PredictionHoverTipRegistry<Control> predictionHoverTipRegistry = new PredictionHoverTipRegistry<Control>();
		predictionHoverTipRegistry.Register("merchant entry", MerchantEntryHoverTips.GetHoverTips);
		predictionHoverTipRegistry.Register("transform selection", TransformSelectionHoverTips.GetHoverTips);
		predictionHoverTipRegistry.Register("treasure room relic", TreasureRoomRelicHoverTips.GetHoverTips);
		predictionHoverTipRegistry.Register("relic choice selection", RelicChoiceSelectionHoverTips.GetHoverTips);
		predictionHoverTipRegistry.Register("rest site", RestSiteHoverTips.GetHoverTips);
		predictionHoverTipRegistry.Register("combat transform selected holder", CombatTransformSelectedHoverTips.GetHoverTips);
		predictionHoverTipRegistry.Register("card reward alternative", CardRewardAlternativeButtonHoverTips.GetHoverTips);
		predictionHoverTipRegistry.Register("lemonSpire", LemonSpireControlHoverTips.GetHoverTips);
		return predictionHoverTipRegistry;
	}

	private static void Prefix(Control owner, ref IEnumerable<IHoverTip> hoverTips)
	{
		IReadOnlyList<IHoverTip> hoverTips2 = Registry.GetHoverTips(owner);
		if (hoverTips2.Count > 0)
		{
			hoverTips = hoverTips.Concat(hoverTips2);
		}
	}
}
