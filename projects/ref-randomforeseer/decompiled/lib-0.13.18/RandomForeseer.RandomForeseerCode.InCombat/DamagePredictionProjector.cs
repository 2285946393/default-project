using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class DamagePredictionProjector
{
	public static DamagePrediction Project(IEnumerable<CombatPredictionDamageReceivedEntry> history)
	{
		if (!ModData.Settings.CombatDamagePredictionEnabled)
		{
			return DamagePrediction.Empty;
		}
		List<DamagePredictionTarget> list = (from entry in history
			group entry by entry.Receiver).Select(delegate(IGrouping<Creature, CombatPredictionDamageReceivedEntry> group)
		{
			Creature key = group.Key;
			List<DamagePredictionLine> list2 = new List<DamagePredictionLine>();
			list2.AddRange(group.Select((CombatPredictionDamageReceivedEntry entry) => new DamagePredictionLine(entry.Result.TotalDamage, entry.Result.UnblockedDamage, entry.Result.WasTargetKilled, entry.Trace?.Source ?? throw new InvalidOperationException("Damage entry has no source trace."))));
			return new DamagePredictionTarget(key, new _003C_003Ez__ReadOnlyList<DamagePredictionLine>(list2));
		}).ToList();
		if (list.Count != 0)
		{
			return new DamagePrediction(list);
		}
		return DamagePrediction.Empty;
	}

	public static bool ShouldIncludeEntry(CombatPredictionDamageReceivedEntry entry)
	{
		ModSettings settings = ModData.Settings;
		if (!settings.OrbDamagePredictionEnabled && entry.Trace.Ancestors().Any((PredictionTraceFrame frame) => frame.Source is OrbModel))
		{
			return false;
		}
		if (!settings.RandomTargetAttackPredictionEnabled && entry.Trace.Ancestors().Any(delegate(PredictionTraceFrame frame)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Invalid comparison between Unknown and I4
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Invalid comparison between Unknown and I4
			AbstractModel source = frame.Source;
			CardModel val = (CardModel)(object)((source is CardModel) ? source : null);
			return val != null && (int)val.Type == 1 && (int)val.TargetType == 4;
		}))
		{
			return false;
		}
		return true;
	}
}
