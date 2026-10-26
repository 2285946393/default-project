using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class EndTurnButtonHoverTipHelper
{
	private static Control? _hoverTipOwner;

	public static void ShowHoverTips(NEndTurnButton endTurnButton, IEnumerable<IHoverTip> hoverTips)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		HideHoverTips();
		Control val = CombatPredictionOverlay.ActiveIndicators.MinBy(delegate(Control indicator)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			Rect2 globalRect = indicator.GetGlobalRect();
			return ((Rect2)(ref globalRect)).Position.X;
		});
		if (val != null)
		{
			NHoverTipSet val2 = NHoverTipSet.CreateAndShow(val, hoverTips, HoverTip.GetHoverTipAlignment(val, 0.5f));
			if (val2 != null)
			{
				_hoverTipOwner = val;
				AvoidHoverTipOverlap(val2, (Control)(object)endTurnButton);
			}
		}
	}

	public static void HideHoverTips()
	{
		if (_hoverTipOwner != null)
		{
			NHoverTipSet.Remove(_hoverTipOwner);
			_hoverTipOwner = null;
		}
	}

	private static void AvoidHoverTipOverlap(NHoverTipSet tipSet, Control avoidOwner)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		if (!NHoverTipSet._activeHoverTips.TryGetValue(avoidOwner, out var value))
		{
			return;
		}
		Rect2 hoverTipSetRect = GetHoverTipSetRect(tipSet);
		Rect2 hoverTipSetRect2 = GetHoverTipSetRect(value);
		if (((Rect2)(ref hoverTipSetRect)).HasArea() && ((Rect2)(ref hoverTipSetRect2)).HasArea() && ((Rect2)(ref hoverTipSetRect)).Intersects(hoverTipSetRect2, false))
		{
			float num = ((Rect2)(ref hoverTipSetRect)).End.X - ((Rect2)(ref hoverTipSetRect2)).Position.X + 8f;
			if (!(num <= 0f))
			{
				MoveHoverTipSet(tipSet, Vector2.Left * num);
			}
		}
	}

	private static Rect2 GetHoverTipSetRect(NHoverTipSet tipSet)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		Rect2 globalRect = ((Control)tipSet._textHoverTipContainer).GetGlobalRect();
		Rect2 globalRect2 = ((Control)tipSet._cardHoverTipContainer).GetGlobalRect();
		if (((Rect2)(ref globalRect)).HasArea())
		{
			if (((Rect2)(ref globalRect2)).HasArea())
			{
				return ((Rect2)(ref globalRect)).Merge(globalRect2);
			}
			return globalRect;
		}
		return globalRect2;
	}

	private static void MoveHoverTipSet(NHoverTipSet tipSet, Vector2 offset)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		VFlowContainer textHoverTipContainer = tipSet._textHoverTipContainer;
		((Control)textHoverTipContainer).GlobalPosition = ((Control)textHoverTipContainer).GlobalPosition + offset;
		NHoverTipCardContainer cardHoverTipContainer = tipSet._cardHoverTipContainer;
		((Control)cardHoverTipContainer).GlobalPosition = ((Control)cardHoverTipContainer).GlobalPosition + offset;
	}
}
