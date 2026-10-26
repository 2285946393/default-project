using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.Common;

internal abstract class PredictionRisk
{
	private sealed class EmptyPredictionRisk : PredictionRisk
	{
		public override bool HasRisk => false;

		protected override IEnumerable<IHoverTip> GetHoverTips()
		{
			return Array.Empty<IHoverTip>();
		}
	}

	public static PredictionRisk None { get; } = new EmptyPredictionRisk();


	public abstract bool HasRisk { get; }

	public IEnumerable<IHoverTip> ToHoverTips()
	{
		if (!HasRisk || !ModData.Settings.ShowDriftWarnings)
		{
			return Array.Empty<IHoverTip>();
		}
		return GetHoverTips();
	}

	protected abstract IEnumerable<IHoverTip> GetHoverTips();
}
