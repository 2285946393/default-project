using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class RestSiteHoverTips
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Control owner)
	{
		ModSettings settings = ModData.Settings;
		if (ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.RestSitePredictionEnabled)
		{
			NRestSiteButton val = (NRestSiteButton)(object)((owner is NRestSiteButton) ? owner : null);
			if (val != null)
			{
				return RestSitePrediction.GetHoverTips(val.Option);
			}
		}
		return Array.Empty<IHoverTip>();
	}
}
