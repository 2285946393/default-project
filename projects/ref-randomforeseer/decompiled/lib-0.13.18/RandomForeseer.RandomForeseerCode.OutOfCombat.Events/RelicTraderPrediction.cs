using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class RelicTraderPrediction
{
	public static RelicModel? GetReceivedRelic(RelicTrader relicTrader, EventOption option)
	{
		IReadOnlyList<RelicModel> newRelics = relicTrader.NewRelics;
		switch (option.TextKey)
		{
		case "RELIC_TRADER.pages.INITIAL.options.TOP":
			if (newRelics.Count > 0)
			{
				return newRelics[0];
			}
			break;
		case "RELIC_TRADER.pages.INITIAL.options.MIDDLE":
			if (newRelics.Count > 1)
			{
				return newRelics[1];
			}
			break;
		case "RELIC_TRADER.pages.INITIAL.options.BOTTOM":
			if (newRelics.Count > 2)
			{
				return newRelics[2];
			}
			break;
		}
		return null;
	}
}
