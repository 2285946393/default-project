using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatPredictionProjectionController
{
	private static object? _owner;

	private static bool _hasDamagePrediction;

	public static void Set(object owner, CombatPredictionProjection? projection)
	{
		_owner = owner;
		if ((object)projection != null)
		{
			ShowDamagePrediction(projection.DamagePrediction, projection.Risk);
			CombatPredictionCardHighlight.Show(projection.HighlightedCards);
		}
		else
		{
			ClearDamagePrediction();
			CombatPredictionCardHighlight.Clear();
		}
		EndTurnPredictionController.SetActionDamageOverride(_hasDamagePrediction);
	}

	public static void Release(object owner)
	{
		if (_owner == owner)
		{
			_owner = null;
			ClearDamagePrediction();
			CombatPredictionCardHighlight.Clear();
			EndTurnPredictionController.SetActionDamageOverride(active: false);
		}
	}

	private static void ShowDamagePrediction(DamagePrediction damagePrediction, PredictionRisk risk)
	{
		if (!damagePrediction.HasTargets)
		{
			ClearDamagePrediction();
			return;
		}
		CombatPredictionOverlay.Show(damagePrediction, risk);
		DamagePredictionHealthBarForecast.Set(damagePrediction);
		_hasDamagePrediction = true;
	}

	private static void ClearDamagePrediction()
	{
		if (_hasDamagePrediction)
		{
			CombatPredictionOverlay.Clear();
			DamagePredictionHealthBarForecast.Clear();
			_hasDamagePrediction = false;
		}
	}
}
