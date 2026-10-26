using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Runs;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class RelicChoiceSelectionHoverTips
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Control control)
	{
		NRelicBasicHolder val = (NRelicBasicHolder)(object)((control is NRelicBasicHolder) ? control : null);
		if (val != null)
		{
			NRelic relic = val.Relic;
			if (relic != null)
			{
				RelicModel model = relic.Model;
				if (model != null && NodeUtil.GetAncestorOfType<NChooseARelicSelection>((Node)(object)control) != null)
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
