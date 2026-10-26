using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal static class PredictionCardHoverTipLayout
{
	private readonly record struct WrappedLayout(int Rows, float Scale);

	private const float Padding = 4f;

	private const float SideGap = 10f;

	private const float ViewportMargin = 12f;

	private const float TopGap = 12f;

	private const float MinScale = 0.55f;

	internal const float CardHolderTextGap = 10f;

	public static bool TryLayoutPredictionCardTips(NHoverTipCardContainer container, Vector2 globalStartLocation, HoverTipAlignment alignment)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		List<Control> list = ((IEnumerable)((Node)container).GetChildren(false)).OfType<Control>().ToList();
		if (!list.Any(PredictionCardHoverTipLayoutState.IsPredictionCard))
		{
			return false;
		}
		NGame instance = NGame.Instance;
		if (instance == null)
		{
			return false;
		}
		Rect2 viewportRect = ((CanvasItem)instance).GetViewportRect();
		Vector2 size = ((Rect2)(ref viewportRect)).Size;
		float availableWidth = size.X - 24f;
		Vector2 size2 = ApplyWrappedLayout(list, 1f, 1);
		Vector2 sidePosition = GetSidePosition(globalStartLocation, alignment, size2);
		if (FitsWithinViewport(sidePosition, size2, size))
		{
			((Control)container).Size = size2;
			((Control)container).GlobalPosition = sidePosition;
			return true;
		}
		WrappedLayout bestWrappedLayout = GetBestWrappedLayout(list, availableWidth);
		Vector2 size3 = (((Control)container).Size = ApplyWrappedLayout(list, bestWrappedLayout.Scale, bestWrappedLayout.Rows));
		if (!PredictionCardHoverTipLayoutState.TryGetSourceRect(container, out PredictionCardHoverTipSourceRect _))
		{
			((Control)container).GlobalPosition = ClampToViewport(GetSidePosition(globalStartLocation, alignment, size3), size3, size);
			return true;
		}
		((Control)container).GlobalPosition = GetSidePosition(globalStartLocation, alignment, size3);
		return true;
	}

	private static WrappedLayout GetBestWrappedLayout(IReadOnlyList<Control> tips, float availableWidth)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		float x = GetWrappedLayoutSize(tips, 1f, 1).X;
		if (x <= availableWidth)
		{
			return new WrappedLayout(1, 1f);
		}
		if (tips.Count == 1)
		{
			return new WrappedLayout(1, availableWidth / x);
		}
		if (x * 0.55f <= availableWidth)
		{
			return new WrappedLayout(1, availableWidth / x);
		}
		for (int i = 2; i <= tips.Count; i++)
		{
			float x2 = GetWrappedLayoutSize(tips, 1f, i).X;
			if (x2 * 0.55f <= availableWidth)
			{
				return new WrappedLayout(i, Mathf.Min(1f, availableWidth / x2));
			}
		}
		return new WrappedLayout(tips.Count, 0.55f);
	}

	private static Vector2 ApplyWrappedLayout(IReadOnlyList<Control> tips, float scale, int rows)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(1, rows);
		int num2 = Mathf.CeilToInt((float)tips.Count / (float)num);
		Vector2 zero = Vector2.Zero;
		float scaledPadding = 4f * scale;
		for (int i = 0; i < tips.Count; i++)
		{
			Control val = tips[i];
			int num3 = i / num2;
			int count = i % num2;
			int count2 = num3 * num2;
			float num4 = (from item in tips.Skip(count2).Take(num2)
				select item.Size.Y * scale).DefaultIfEmpty(0f).Max();
			float num5 = tips.Skip(count2).Take(count).Sum((Control item) => item.Size.X * scale + scaledPadding);
			float num6 = 0f;
			for (int j = 0; j < num3; j++)
			{
				int count3 = j * num2;
				num6 += (from item in tips.Skip(count3).Take(num2)
					select item.Size.Y * scale).DefaultIfEmpty(0f).Max() + scaledPadding;
			}
			Vector2 val2 = val.Size * scale;
			val.Scale = Vector2.One * scale;
			val.Position = new Vector2(num5, num6 + num4 - val2.Y);
			((Vector2)(ref zero))._002Ector(Mathf.Max(num5 + val2.X, zero.X), Mathf.Max(num6 + Mathf.Max(val2.Y, num4), zero.Y));
		}
		return zero;
	}

	private static Vector2 GetWrappedLayoutSize(IReadOnlyList<Control> tips, float scale, int rows)
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.Max(1, rows);
		int num2 = Mathf.CeilToInt((float)tips.Count / (float)num);
		float num3 = 4f * scale;
		float num4 = 0f;
		float num5 = 0f;
		for (int i = 0; i < num; i++)
		{
			List<Control> list = tips.Skip(i * num2).Take(num2).ToList();
			if (list.Count != 0)
			{
				num4 = Mathf.Max(num4, list.Sum((Control tip) => tip.Size.X * scale) + num3 * (float)(list.Count - 1));
				num5 += list.Max((Control tip) => tip.Size.Y * scale);
				if (i < num - 1)
				{
					num5 += num3;
				}
			}
		}
		return new Vector2(num4, num5);
	}

	private static Vector2 GetSidePosition(Vector2 globalStartLocation, HoverTipAlignment alignment, Vector2 size)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if ((int)alignment == 1)
		{
			return globalStartLocation + Vector2.Left * (size.X + 10f);
		}
		return globalStartLocation + Vector2.Right * 10f;
	}

	private static Vector2 GetVerticalFallbackPosition(Rect2 sourceRect, Vector2 size, Vector2 viewportSize)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		float num = ((Rect2)(ref sourceRect)).Position.X + ((Rect2)(ref sourceRect)).Size.X / 2f - size.X / 2f;
		float num2 = ((Rect2)(ref sourceRect)).Position.Y - size.Y - 12f;
		if (num2 >= 12f)
		{
			return ClampToViewport(new Vector2(num, num2), size, viewportSize);
		}
		float num3 = ((Rect2)(ref sourceRect)).End.Y + 12f;
		return ClampToViewport(new Vector2(num, num3), size, viewportSize);
	}

	private static Vector2 ClampToViewport(Vector2 position, Vector2 size, Vector2 viewportSize)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(Clamp(position.X, 12f, viewportSize.X - 12f - size.X), Clamp(position.Y, 12f, viewportSize.Y - 12f - size.Y));
	}

	private static bool FitsWithinViewport(Vector2 position, Vector2 size, Vector2 viewportSize)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (position.X >= 12f && position.Y >= 12f && position.X + size.X <= viewportSize.X - 12f)
		{
			return position.Y + size.Y <= viewportSize.Y - 12f;
		}
		return false;
	}

	private static float GetMaxViewportOverflow(Vector2 position, Vector2 size, Vector2 viewportSize)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		float x = MathF.Max(12f - position.X, position.X + size.X - (viewportSize.X - 12f));
		float y = MathF.Max(12f - position.Y, position.Y + size.Y - (viewportSize.Y - 12f));
		return MathF.Max(0f, MathF.Max(x, y));
	}

	private static Vector2 ClampHorizontalToViewport(Vector2 position, Vector2 size, Vector2 viewportSize)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(Clamp(position.X, 12f, viewportSize.X - 12f - size.X), position.Y);
	}

	private static bool FitsHorizontallyWithinViewport(Rect2 rect, Vector2 viewportSize)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (((Rect2)(ref rect)).Position.X >= 0f)
		{
			return ((Rect2)(ref rect)).End.X <= viewportSize.X;
		}
		return false;
	}

	private static float Clamp(float value, float min, float max)
	{
		if (!(max < min))
		{
			return Mathf.Clamp(value, min, max);
		}
		return min;
	}

	public static void ApplyFallbackLayoutIfStillOverflowing(NHoverTipSet tipSet)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		NHoverTipCardContainer cardHoverTipContainer = tipSet._cardHoverTipContainer;
		if (!PredictionCardHoverTipLayoutState.TryGetSourceRect(cardHoverTipContainer, out PredictionCardHoverTipSourceRect sourceRect) || !PredictionCardHoverTipLayoutState.HasPredictionCard(cardHoverTipContainer))
		{
			return;
		}
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			Rect2 viewportRect = ((CanvasItem)instance).GetViewportRect();
			Vector2 size = ((Rect2)(ref viewportRect)).Size;
			Rect2 globalRect = ((Control)cardHoverTipContainer).GetGlobalRect();
			VFlowContainer textHoverTipContainer = tipSet._textHoverTipContainer;
			Rect2 rect = (((IEnumerable)((Node)textHoverTipContainer).GetChildren(false)).OfType<Control>().Any() ? ((Rect2)(ref globalRect)).Merge(((Control)textHoverTipContainer).GetGlobalRect()) : globalRect);
			if (!FitsHorizontallyWithinViewport(globalRect, size) || !FitsHorizontallyWithinViewport(rect, size))
			{
				ApplyFallbackLayout(tipSet, sourceRect, size);
			}
		}
	}

	private static void ApplyFallbackLayout(NHoverTipSet tipSet, PredictionCardHoverTipSourceRect sourceRect, Vector2 viewportSize)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		NHoverTipCardContainer cardHoverTipContainer = tipSet._cardHoverTipContainer;
		VFlowContainer textHoverTipContainer = tipSet._textHoverTipContainer;
		bool num = ((IEnumerable)((Node)textHoverTipContainer).GetChildren(false)).OfType<Control>().Any();
		if (num)
		{
			((Control)textHoverTipContainer).GlobalPosition = GetFallbackTextPosition((Control)(object)textHoverTipContainer, sourceRect, viewportSize);
		}
		((Control)cardHoverTipContainer).GlobalPosition = GetFallbackCardPosition(((Control)cardHoverTipContainer).Size, sourceRect.Rect, viewportSize);
		if (num)
		{
			Rect2 globalRect = ((Control)cardHoverTipContainer).GetGlobalRect();
			if (((Rect2)(ref globalRect)).Intersects(((Control)textHoverTipContainer).GetGlobalRect(), false))
			{
				((Control)cardHoverTipContainer).GlobalPosition = GetFallbackCardPositionAvoidingText(((Control)cardHoverTipContainer).Size, sourceRect.Rect, ((Control)textHoverTipContainer).GetGlobalRect(), viewportSize);
			}
		}
	}

	private static Vector2 GetFallbackTextPosition(Control textContainer, PredictionCardHoverTipSourceRect sourceRect, Vector2 viewportSize)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = textContainer.Size;
		HoverTipAlignment alignment = sourceRect.Alignment;
		Rect2 rect;
		float num;
		if ((int)alignment != 1)
		{
			if ((int)alignment == 2)
			{
				rect = sourceRect.Rect;
				num = ((Rect2)(ref rect)).End.X + sourceRect.TextGap;
			}
			else
			{
				num = textContainer.GlobalPosition.X;
			}
		}
		else
		{
			rect = sourceRect.Rect;
			num = ((Rect2)(ref rect)).Position.X - size.X - sourceRect.TextGap;
		}
		float num2 = num;
		rect = sourceRect.Rect;
		float y = ((Rect2)(ref rect)).Position.Y;
		return ClampToViewport(new Vector2(num2, y), size, viewportSize);
	}

	private static Vector2 GetFallbackCardPosition(Vector2 cardSize, Rect2 sourceRect, Vector2 viewportSize)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return GetVerticalFallbackPosition(sourceRect, cardSize, viewportSize);
	}

	private static Vector2 GetFallbackCardPositionAvoidingText(Vector2 cardSize, Rect2 sourceRect, Rect2 textRect, Vector2 viewportSize)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		Vector2 sourceCenter = ((Rect2)(ref sourceRect)).GetCenter();
		Vector2 center = ((Rect2)(ref textRect)).GetCenter();
		float num = sourceCenter.X - cardSize.X / 2f;
		float num2 = ((center.X <= sourceCenter.X) ? (((Rect2)(ref textRect)).End.X + 10f) : (((Rect2)(ref textRect)).Position.X - cardSize.X - 10f));
		float num3 = ((Rect2)(ref sourceRect)).Position.Y - cardSize.Y - 12f;
		float num4 = ((Rect2)(ref sourceRect)).End.Y + 12f;
		float num5 = ((Rect2)(ref textRect)).Position.Y - cardSize.Y - 12f;
		float num6 = ((Rect2)(ref textRect)).End.Y + 12f;
		float num7 = Mathf.Min(num3, num5);
		float num8 = Mathf.Max(num4, num6);
		return ((IEnumerable<Vector2>)(object)new Vector2[4]
		{
			ClampHorizontalToViewport(new Vector2(num, num7), cardSize, viewportSize),
			ClampHorizontalToViewport(new Vector2(num, num8), cardSize, viewportSize),
			new Vector2(num2, num3),
			new Vector2(num2, num4)
		}).MinBy(delegate(Vector2 candidate)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			float maxViewportOverflow = GetMaxViewportOverflow(candidate, cardSize, viewportSize);
			bool flag = candidate.Y <= sourceCenter.Y;
			Vector2 val = candidate + cardSize / 2f;
			float item = ((Vector2)(ref val)).DistanceTo(sourceCenter);
			return (overflow: maxViewportOverflow, (!flag) ? 1 : 0, distanceToSource: item);
		});
	}
}
