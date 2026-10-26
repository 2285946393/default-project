using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class TreasureRoomRelicHoverTips
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Control control)
	{
		NTreasureRoomRelicHolder val = (NTreasureRoomRelicHolder)(object)((control is NTreasureRoomRelicHolder) ? control : null);
		if (val != null)
		{
			NRelic relic = val.Relic;
			if (relic != null)
			{
				RelicModel model = relic.Model;
				if (model != null)
				{
					Player me = LocalContext.GetMe((IPlayerCollection)(object)RunManager.Instance.State);
					if (me != null)
					{
						return RelicPickupPrediction.GetHoverTips(me, model);
					}
				}
			}
		}
		return Array.Empty<IHoverTip>();
	}
}
