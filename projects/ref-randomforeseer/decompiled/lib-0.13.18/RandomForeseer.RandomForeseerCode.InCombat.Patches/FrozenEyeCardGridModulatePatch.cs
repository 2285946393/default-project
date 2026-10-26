using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NCardGrid))]
internal static class FrozenEyeCardGridModulatePatch
{
	private static readonly Color PredictedShuffleCardModulate = new Color(0.65f, 0.65f, 0.65f, 1f);

	private static readonly ConditionalWeakTable<NCard, StrongBox<Color>> OriginalModulates = new ConditionalWeakTable<NCard, StrongBox<Color>>();

	[HarmonyPatch("InitGrid", new Type[] { })]
	[HarmonyPostfix]
	private static void ApplyOnInitGrid(NCardGrid __instance)
	{
		ApplyPredictionModulate(__instance, __instance.CurrentlyDisplayedCardHolders);
	}

	[HarmonyPatch("AssignCardsToRow")]
	[HarmonyPostfix]
	private static void ApplyOnAssignCardsToRow(NCardGrid __instance, List<NGridCardHolder> row)
	{
		ApplyPredictionModulate(__instance, row);
	}

	private static void ApplyPredictionModulate(NCardGrid grid, IEnumerable<NGridCardHolder> holders)
	{
		if (!FrozenEyeDrawPileViewState.TryGetPredictedShuffleCards(grid, out HashSet<CardModel> predictedCards))
		{
			return;
		}
		foreach (NGridCardHolder holder in holders)
		{
			if (holder != null && ((CanvasItem)holder).Visible)
			{
				CardModel cardModel = ((NCardHolder)holder).CardModel;
				if (cardModel != null && predictedCards.Contains(cardModel))
				{
					ApplyPredictedModulate(holder);
					continue;
				}
			}
			RestoreModulate(holder);
		}
	}

	private static void ApplyPredictedModulate(NGridCardHolder holder)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		NCard cardNode = ((NCardHolder)holder).CardNode;
		if (cardNode != null)
		{
			if (!OriginalModulates.TryGetValue(cardNode, out StrongBox<Color> _))
			{
				OriginalModulates.Add(cardNode, new StrongBox<Color>(((CanvasItem)cardNode).Modulate));
			}
			((CanvasItem)cardNode).Modulate = PredictedShuffleCardModulate;
		}
	}

	private static void RestoreModulate(NGridCardHolder holder)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		NCard cardNode = ((NCardHolder)holder).CardNode;
		if (cardNode != null && OriginalModulates.TryGetValue(cardNode, out StrongBox<Color> value))
		{
			((CanvasItem)cardNode).Modulate = value.Value;
			OriginalModulates.Remove(cardNode);
		}
	}
}
