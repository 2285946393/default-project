using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal sealed record PredictionCardHoverTipSourceRect(Rect2 Rect, HoverTipAlignment Alignment, float TextGap)
{
	[CompilerGenerated]
	private bool PrintMembers(StringBuilder builder)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		RuntimeHelpers.EnsureSufficientExecutionStack();
		builder.Append("Rect = ");
		Rect2 rect = Rect;
		builder.Append(((object)(Rect2)(ref rect)).ToString());
		builder.Append(", Alignment = ");
		HoverTipAlignment alignment = Alignment;
		builder.Append(((object)(HoverTipAlignment)(ref alignment)).ToString());
		builder.Append(", TextGap = ");
		builder.Append(TextGap.ToString());
		return true;
	}

	[CompilerGenerated]
	public void Deconstruct(out Rect2 Rect, out HoverTipAlignment Alignment, out float TextGap)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected I4, but got Unknown
		Rect = this.Rect;
		Alignment = (HoverTipAlignment)(int)this.Alignment;
		TextGap = this.TextGap;
	}
}
