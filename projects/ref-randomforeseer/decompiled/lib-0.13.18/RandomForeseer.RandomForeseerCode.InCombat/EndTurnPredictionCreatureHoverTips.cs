using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Localization;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class EndTurnPredictionCreatureHoverTips
{
	private const string BlockStatusNone = "none";

	private const string BlockStatusBlocked = "blocked";

	private const string BlockStatusPartial = "partial";

	private static readonly Dictionary<Creature, IReadOnlyList<IHoverTip>> TipsByTarget = new Dictionary<Creature, IReadOnlyList<IHoverTip>>();

	public static void Set(DamagePrediction prediction, PredictionRisk risk)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		TipsByTarget.Clear();
		IHoverTip[] array = risk.ToHoverTips().ToArray();
		foreach (DamagePredictionTarget target in prediction.Targets)
		{
			if (target.DamageLines.Count != 0)
			{
				HoverTip val = PredictionHoverTipFactory.Text("end_turn_damage_details", delegate(LocString description)
				{
					description.Add("Creature", target.Target.Name);
					description.Add("TotalDamage", target.TotalDamage);
					description.Add("TotalUnblockedDamage", target.TotalUnblockedDamage);
					description.Add("BlockStatus", GetBlockStatus(target.TotalDamage, target.TotalUnblockedDamage));
					description.Add("WasTargetKilled", target.WasTargetKilled);
					List<string> list = new List<string>();
					list.AddRange(target.DamageLines.Select(FormatDamageLine));
					description.Add("Lines", (IList<string>)list);
				});
				Dictionary<Creature, IReadOnlyList<IHoverTip>> tipsByTarget = TipsByTarget;
				Creature target2 = target.Target;
				IHoverTip val2 = (IHoverTip)(object)val;
				IHoverTip[] array2 = array;
				int num = 0;
				IHoverTip[] array3 = (IHoverTip[])(object)new IHoverTip[1 + array2.Length];
				array3[num] = val2;
				num++;
				ReadOnlySpan<IHoverTip> readOnlySpan = new ReadOnlySpan<IHoverTip>(array2);
				readOnlySpan.CopyTo(new Span<IHoverTip>(array3).Slice(num, readOnlySpan.Length));
				num += readOnlySpan.Length;
				tipsByTarget[target2] = new _003C_003Ez__ReadOnlyArray<IHoverTip>(array3);
			}
		}
	}

	public static void Clear()
	{
		TipsByTarget.Clear();
	}

	public static IReadOnlyList<IHoverTip> GetHoverTips(Creature creature)
	{
		if (!TipsByTarget.TryGetValue(creature, out IReadOnlyList<IHoverTip> value))
		{
			return Array.Empty<IHoverTip>();
		}
		return value;
	}

	private static string FormatDamageLine(DamagePredictionLine line)
	{
		LocString obj = ModLocalization.Text("end_turn_damage_details.line");
		obj.Add("Source", line.Source.GetTitle());
		obj.Add("Damage", line.Damage);
		obj.Add("UnblockedDamage", line.UnblockedDamage);
		obj.Add("BlockStatus", GetBlockStatus(line.Damage, line.UnblockedDamage));
		return obj.GetFormattedText();
	}

	private static string GetBlockStatus(decimal totalDamage, decimal totalUnblockedDamage)
	{
		if (totalDamage == totalUnblockedDamage)
		{
			return "none";
		}
		if (!(totalUnblockedDamage == 0m))
		{
			return "partial";
		}
		return "blocked";
	}
}
