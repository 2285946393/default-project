using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal static class PredictionHoverTipSetHelper
{
	private static readonly ConditionalWeakTable<Control, NHoverTipSet> OwnedHoverTips = new ConditionalWeakTable<Control, NHoverTipSet>();

	public static NHoverTipSet? EnsureHoverTipSet(Control owner, HoverTipAlignment alignment = 0)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (NHoverTipSet._activeHoverTips.ContainsKey(owner))
		{
			return null;
		}
		NHoverTipSet val = NHoverTipSet.CreateAndShow(owner, (IEnumerable<IHoverTip>)Array.Empty<IHoverTip>(), alignment);
		if (val == null)
		{
			return null;
		}
		OwnedHoverTips.AddOrUpdate(owner, val);
		return val;
	}

	public static void RemoveOwnedHoverTipSet(Control owner)
	{
		if (OwnedHoverTips.TryGetValue(owner, out NHoverTipSet value))
		{
			OwnedHoverTips.Remove(owner);
			if (NHoverTipSet._activeHoverTips.TryGetValue(owner, out var value2) && value2 == value)
			{
				NHoverTipSet.Remove(owner);
			}
		}
	}
}
