using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;
using RandomForeseer.RandomForeseerCode.Localization;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed class CombatPredictionCausalTipBuilder
{
	private sealed class CausalGroup
	{
		public CausalCause Cause { get; }

		public CausalEffectKind Effect { get; }

		public List<AbstractModel> Models { get; }

		public PredictionTraceFrame SourceFrame => Cause.SourceFrame;

		public PredictionTraceFrame? ListenerFrame => Cause.ListenerFrame;

		public AbstractModel Source => Cause.Source;

		public AbstractModel? Listener => Cause.Listener;

		public CausalGroup(CausalCause cause, CausalEffectKind effect)
		{
			Cause = cause;
			Effect = effect;
			Models = new List<AbstractModel>();
			base._002Ector();
		}

		public bool IsMergeableWith(CausalCause cause, CausalEffectKind effect)
		{
			if (SourceFrame == cause.SourceFrame && Listener == cause.Listener)
			{
				return Effect == effect;
			}
			return false;
		}
	}

	private readonly record struct CausalCause(PredictionTraceFrame SourceFrame, PredictionTraceFrame? ListenerFrame)
	{
		public AbstractModel Source => SourceFrame.Source;

		public AbstractModel? Listener => ListenerFrame?.Source;
	}

	[CompilerGenerated]
	private PredictionTraceFrame _003CrootFrame_003EP;

	private const int MaxLines = 10;

	private readonly List<CausalGroup> _groups;

	public CombatPredictionCausalTipBuilder(PredictionTraceFrame rootFrame)
	{
		_003CrootFrame_003EP = rootFrame;
		_groups = new List<CausalGroup>();
		base._002Ector();
	}

	public void AddEffect(CombatPredictionHistoryEntry entry, CausalEffectKind effect, IEnumerable<AbstractModel> models)
	{
		PredictionTraceFrame trace = entry.Trace;
		if (trace != null)
		{
			CausalCause? causalCause = TryGetCause(trace);
			if (causalCause.HasValue)
			{
				CausalCause valueOrDefault = causalCause.GetValueOrDefault();
				AddGroup(valueOrDefault, effect, models);
			}
		}
	}

	public IHoverTip? Build()
	{
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		if (_groups.All(IsRootDirectEffect) && !_groups.Select((CausalGroup group) => group.SourceFrame).Distinct().Skip(1)
			.Any())
		{
			return null;
		}
		List<string> lines = _groups.Take(10).Select(FormatGroup).ToList();
		if (_groups.Count > 10)
		{
			LocString val = ModLocalization.Text("causal_prediction.more");
			val.Add("Count", (decimal)(_groups.Count - 10));
			lines.Add(val.GetFormattedText());
		}
		return (IHoverTip?)(object)PredictionHoverTipFactory.Text(GetRootTipKey(), delegate(LocString description)
		{
			description.Add("Lines", (IList<string>)lines);
		});
	}

	private static CausalCause? TryGetCause(PredictionTraceFrame trace)
	{
		PredictionTraceFrame predictionTraceFrame = trace.FindOriginatingEffect();
		if (predictionTraceFrame == null)
		{
			return null;
		}
		return new CausalCause(predictionTraceFrame, (trace.Source == predictionTraceFrame.Source) ? null : trace);
	}

	private void AddGroup(CausalCause cause, CausalEffectKind effect, IEnumerable<AbstractModel> models)
	{
		List<CausalGroup> groups = _groups;
		CausalGroup causalGroup2;
		if (groups != null)
		{
			int count = groups.Count;
			if (count >= 1)
			{
				CausalGroup causalGroup = groups[count - 1];
				if (causalGroup.IsMergeableWith(cause, effect))
				{
					List<CausalGroup> groups2 = _groups;
					causalGroup2 = groups2[groups2.Count - 1];
					goto IL_0053;
				}
			}
		}
		causalGroup2 = new CausalGroup(cause, effect);
		_groups.Add(causalGroup2);
		goto IL_0053;
		IL_0053:
		causalGroup2.Models.AddRange(models);
	}

	private bool IsRootDirectEffect(CausalGroup group)
	{
		if (group.ListenerFrame == null)
		{
			return group.SourceFrame.FindOriginatingAction() == _003CrootFrame_003EP;
		}
		return false;
	}

	private string FormatGroup(CausalGroup group)
	{
		LocString obj = ModLocalization.Text("causal_prediction." + GetEffectKey(group.Effect));
		obj.Add("Source", group.Source.GetTitle());
		obj.Add("Listener", group.Listener?.GetTitle() ?? string.Empty);
		List<string> list = new List<string>();
		list.AddRange(group.Models.Select((AbstractModel model) => model.GetTitle()));
		obj.Add("Models", (IList<string>)list);
		return obj.GetFormattedText();
	}

	private string GetRootTipKey()
	{
		PredictionActionKind? action = _003CrootFrame_003EP.Invocation.Action;
		return action switch
		{
			PredictionActionKind.CardPlay => "causal_prediction.card", 
			PredictionActionKind.PotionUse => "causal_prediction.potion", 
			_ => throw new InvalidOperationException($"Unexpected root action kind {action}."), 
		};
	}

	private static string GetEffectKey(CausalEffectKind effect)
	{
		return effect switch
		{
			CausalEffectKind.ChannelOrbs => "channel_orbs", 
			CausalEffectKind.DrawCards => "draw_cards", 
			CausalEffectKind.GenerateCards => "generate_cards", 
			CausalEffectKind.GeneratePotion => "generate_potion", 
			CausalEffectKind.PlayCard => "play_card", 
			CausalEffectKind.SelectCards => "select_cards", 
			_ => throw new ArgumentOutOfRangeException("effect", effect, null), 
		};
	}
}
