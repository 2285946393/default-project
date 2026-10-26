using System;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace IntentGraph2.Utils;

public static class HoverTipSetExtensions
{
	public static Control GetTextHoverTipContainer(this NHoverTipSet hoverTipSet)
	{
		object? obj = ((object)hoverTipSet).GetType().GetField("_textHoverTipContainer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(hoverTipSet);
		Control val = (Control)((obj is Control) ? obj : null);
		if (val == null)
		{
			throw new Exception("Failed to get _textHoverTipContainer from hoverTipSet");
		}
		return val;
	}
}
