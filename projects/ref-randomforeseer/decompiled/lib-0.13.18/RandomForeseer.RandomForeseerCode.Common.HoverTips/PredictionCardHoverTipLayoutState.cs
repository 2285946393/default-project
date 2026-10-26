using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal static class PredictionCardHoverTipLayoutState
{
	internal static readonly StringName PredictionCardMetaKey = StringName.op_Implicit("RandomForeseer_PredictionCard");

	private static readonly ConditionalWeakTable<NHoverTipCardContainer, PredictionCardHoverTipSourceRect> SourceRects = new ConditionalWeakTable<NHoverTipCardContainer, PredictionCardHoverTipSourceRect>();

	public static void MarkPredictionCard(Control? control)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		if (control != null)
		{
			StringName predictionCardMetaKey = PredictionCardMetaKey;
			bool flag = true;
			((GodotObject)control).SetMeta(predictionCardMetaKey, Variant.From<bool>(ref flag));
		}
	}

	public static bool IsPredictionCard(Control control)
	{
		return ((GodotObject)control).HasMeta(PredictionCardMetaKey);
	}

	public static bool HasPredictionCard(NHoverTipCardContainer container)
	{
		return ((IEnumerable)((Node)container).GetChildren(false)).OfType<Control>().Any(IsPredictionCard);
	}

	public static void RecordSourceRect(NHoverTipCardContainer container, Rect2 sourceRect, HoverTipAlignment alignment = 0, float textGap = 0f)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		SourceRects.AddOrUpdate(container, new PredictionCardHoverTipSourceRect(sourceRect, alignment, textGap));
	}

	public static bool TryGetSourceRect(NHoverTipCardContainer container, [NotNullWhen(true)] out PredictionCardHoverTipSourceRect? sourceRect)
	{
		return SourceRects.TryGetValue(container, out sourceRect);
	}
}
