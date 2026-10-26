using System.Collections.Generic;
using STS2RitsuLib.Combat.HealthBars;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed class DamagePredictionHealthBarForecastSource : IHealthBarForecastSource
{
	public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return DamagePredictionHealthBarForecast.GetSegments(context);
	}
}
