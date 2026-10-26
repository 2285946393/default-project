using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatTransformSelectedHoverTips
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Control owner)
	{
		NSelectedHandCardHolder val = (NSelectedHandCardHolder)(object)((owner is NSelectedHandCardHolder) ? owner : null);
		if (val != null)
		{
			CardModel cardModel = ((NCardHolder)val).CardModel;
			if (cardModel != null)
			{
				return CombatTransformPrediction.GetCardHoverTips(cardModel);
			}
		}
		return Array.Empty<IHoverTip>();
	}
}
