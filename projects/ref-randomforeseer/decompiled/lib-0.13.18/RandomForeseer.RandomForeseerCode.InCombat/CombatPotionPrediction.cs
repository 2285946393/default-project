using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatPotionPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(PotionModel potion)
	{
		try
		{
			if (CombatPotionPredictionController.TryGetActiveHoverTips(potion, out IReadOnlyList<IHoverTip> hoverTips))
			{
				return hoverTips;
			}
			return Predict(potion, null)?.HoverTips ?? Array.Empty<IHoverTip>();
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Combat potion prediction failed for {((AbstractModel)potion).Id}: {ex}", 1);
			ModTelemetry.CaptureException(ex, "combat_potion_prediction", "build_hover_tips", TelemetryContext.ForModel((AbstractModel)(object)potion));
			return Array.Empty<IHoverTip>();
		}
	}

	public static CombatPredictionProjection? Predict(PotionModel potion, Creature? target)
	{
		if (CombatManager.Instance.IsInProgress)
		{
			ICombatState combatState = potion.Owner.Creature.CombatState;
			if (combatState != null && (target == null || target.CombatState == combatState) && PotionOnUseMirrors.CanMirror(potion))
			{
				using CombatPredictionSession combatPredictionSession = new CombatPredictionSession(combatState);
				CombatPredictionSimulator simulator = combatPredictionSession.Simulator;
				PredictionTraceFrame frame;
				return simulator.ManualUse(potion, target, out frame) ? CombatPredictionProjector.Project(simulator.History, frame) : null;
			}
		}
		return null;
	}
}
