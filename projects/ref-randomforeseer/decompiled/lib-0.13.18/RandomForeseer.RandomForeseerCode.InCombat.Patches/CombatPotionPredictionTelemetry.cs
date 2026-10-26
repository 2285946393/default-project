using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

internal static class CombatPotionPredictionTelemetry
{
	public static object? GetTelemetryContext(NPotionHolder holder)
	{
		if (holder != null)
		{
			NPotion potion = holder.Potion;
			if (potion != null)
			{
				PotionModel model = potion.Model;
				if (model != null)
				{
					return TelemetryContext.ForModel((AbstractModel)(object)model);
				}
			}
		}
		return null;
	}

	public static object? GetTelemetryContext(NPotionPopup popup)
	{
		return GetTelemetryContext(popup._holder);
	}
}
