using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class TrashHeapPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(TrashHeap trashHeap, EventOption option)
	{
		Rng val = ((EventModel)trashHeap).Rng.Clone();
		string textKey = option.TextKey;
		if (!(textKey == "TRASH_HEAP.pages.INITIAL.options.DIVE_IN"))
		{
			if (textKey == "TRASH_HEAP.pages.INITIAL.options.GRAB")
			{
				return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(PredictionHoverTipFactory.Card(val.NextItem<CardModel>((IEnumerable<CardModel>)TrashHeap.Cards)));
			}
			return Array.Empty<IHoverTip>();
		}
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(((EventModel)trashHeap).Owner, new _003C_003Ez__ReadOnlySingleElementList<RelicModel>(val.NextItem<RelicModel>((IEnumerable<RelicModel>)TrashHeap.Relics)));
	}
}
