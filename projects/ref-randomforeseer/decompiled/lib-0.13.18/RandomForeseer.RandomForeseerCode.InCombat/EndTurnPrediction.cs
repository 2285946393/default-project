using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class EndTurnPrediction
{
	public static EndTurnPredictionResult? Predict()
	{
		CombatState val = CombatManagerExtensions.get_LiveCombatState(CombatManager.Instance);
		if (val == null)
		{
			return null;
		}
		using CombatPredictionSession combatPredictionSession = new CombatPredictionSession((ICombatState)(object)val);
		CombatPredictionSimulator simulator = combatPredictionSession.Simulator;
		simulator.SimulateEndPlayerTurn();
		return EndTurnPredictionResult.FromDamageHistory(simulator);
	}

	public static bool ShouldPredict()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		ModSettings settings = ModData.Settings;
		if (ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.EndTurnPredictionEnabled)
		{
			CombatState? obj = CombatManagerExtensions.get_LiveCombatState(CombatManager.Instance);
			CombatSide? val = ((obj != null) ? new CombatSide?(obj.CurrentSide) : null);
			if (val.HasValue && (int)val.GetValueOrDefault() == 1)
			{
				return (int)RunManager.Instance.ActionQueueSynchronizer.CombatState == 2;
			}
		}
		return false;
	}
}
