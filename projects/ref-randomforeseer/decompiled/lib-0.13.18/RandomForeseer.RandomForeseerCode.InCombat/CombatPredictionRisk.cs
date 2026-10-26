using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;
using RandomForeseer.RandomForeseerCode.Localization;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed class CombatPredictionRisk : PredictionRisk
{
	private const int MaxModelNamesPerLine = 2;

	public IReadOnlyList<CombatPredictionRiskEntry> Entries { get; }

	public override bool HasRisk => Entries.Count > 0;

	public CombatPredictionRisk(IReadOnlyList<CombatPredictionRiskEntry> entries)
	{
		Entries = entries;
		base._002Ector();
	}

	protected override IReadOnlyList<IHoverTip> GetHoverTips()
	{
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		List<AbstractModel> list = new List<AbstractModel>();
		List<AbstractModel> list2 = new List<AbstractModel>();
		bool flag = false;
		bool flag2 = false;
		foreach (CombatPredictionRiskEntry entry in Entries)
		{
			switch (entry.Reason)
			{
			case PredictionRiskReason.MethodNotMirrored:
			case PredictionRiskReason.MethodMirrorIncomplete:
				list.Add((entry.Trace?.FindOriginatingEffect() ?? entry.Trace)?.Source);
				break;
			case PredictionRiskReason.UnresolvedPlayerChoice:
				list2.Add(entry.Trace?.Source);
				break;
			case PredictionRiskReason.CardDrawLimitExceeded:
				flag = true;
				break;
			case PredictionRiskReason.OrbChannelLimitExceeded:
				flag2 = true;
				break;
			}
		}
		List<string> lines = new List<string>();
		if (list.Count > 0)
		{
			AddModelRiskLine(lines, "incomplete", list);
		}
		if (list2.Count > 0)
		{
			AddModelRiskLine(lines, "player_choice", list2);
		}
		if (flag)
		{
			lines.Add(ModLocalization.Text("drift_warning.card_draw_limit").GetFormattedText());
		}
		if (flag2)
		{
			lines.Add(ModLocalization.Text("drift_warning.orb_channel_limit").GetFormattedText());
		}
		return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>((IHoverTip)(object)PredictionHoverTipFactory.Text("drift_warning", delegate(LocString description)
		{
			description.Add("Lines", (IList<string>)lines);
		}));
	}

	private static void AddModelRiskLine(List<string> lines, string localizationKey, List<AbstractModel?> models)
	{
		bool flag = models.Any((AbstractModel model) => model == null);
		List<string> list = (from entry in models.OfType<AbstractModel>().DistinctBy((AbstractModel model) => model.Id)
			select entry.GetTitle()).ToList();
		if (list.Count == 0)
		{
			lines.Add(ModLocalization.Text("drift_warning." + localizationKey + "_unknown").GetFormattedText());
			return;
		}
		List<string> list2 = list.Take(2).ToList();
		string text = ((flag || list.Count > 2) ? "_more" : string.Empty);
		LocString val = ModLocalization.Text("drift_warning." + localizationKey + text);
		val.Add("Models", (IList<string>)list2);
		lines.Add(val.GetFormattedText());
	}
}
