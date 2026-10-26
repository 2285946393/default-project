using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class DenseVegetationPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(DenseVegetation denseVegetation, EventOption option)
	{
		if (!(option.TextKey == "DENSE_VEGETATION.pages.INITIAL.options.REST"))
		{
			return Array.Empty<IHoverTip>();
		}
		return RestSitePrediction.PredictRestTips(((EventModel)denseVegetation).Owner);
	}
}
