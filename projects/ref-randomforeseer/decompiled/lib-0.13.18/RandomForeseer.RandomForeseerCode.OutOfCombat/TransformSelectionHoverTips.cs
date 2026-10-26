using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class TransformSelectionHoverTips
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Control owner)
	{
		NGridCardHolder val = (NGridCardHolder)(object)((owner is NGridCardHolder) ? owner : null);
		if (val != null)
		{
			NDeckTransformSelectScreen val2 = FindTransformSelectScreen((NCardHolder)(object)val);
			if (val2 != null && val2._cardToTransformation.Target is DeckTransformPreviewPredictor deckTransformPreviewPredictor)
			{
				return deckTransformPreviewPredictor.GetHoverTips(((NCardHolder)val).CardModel, val2._selectedCards);
			}
		}
		return Array.Empty<IHoverTip>();
	}

	private static NDeckTransformSelectScreen? FindTransformSelectScreen(NCardHolder holder)
	{
		for (Node parent = ((Node)holder).GetParent(); parent != null; parent = parent.GetParent())
		{
			NDeckTransformSelectScreen val = (NDeckTransformSelectScreen)(object)((parent is NDeckTransformSelectScreen) ? parent : null);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}
}
