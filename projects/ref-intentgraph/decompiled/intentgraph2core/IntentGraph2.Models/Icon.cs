using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace IntentGraph2.Models;

public record Icon(float X = 0f, float Y = 0f, IntentType IntentType = 7, int? Value = null, int Times = 1, string ValueText = "", string TimesText = "", string VolatileValueText = "", string? RelativeTo = null, [property: JsonIgnore] string ImageResourcePath = "", [property: JsonIgnore] MoveDetailIconType MoveDetailType = MoveDetailIconType.None) : IRelativeToPosition
{
	[CompilerGenerated]
	protected virtual bool PrintMembers(StringBuilder builder)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		RuntimeHelpers.EnsureSufficientExecutionStack();
		builder.Append("X = ");
		builder.Append(X.ToString());
		builder.Append(", Y = ");
		builder.Append(Y.ToString());
		builder.Append(", IntentType = ");
		IntentType intentType = IntentType;
		builder.Append(((object)(IntentType)(ref intentType)).ToString());
		builder.Append(", Value = ");
		builder.Append(Value.ToString());
		builder.Append(", Times = ");
		builder.Append(Times.ToString());
		builder.Append(", ValueText = ");
		builder.Append((object?)ValueText);
		builder.Append(", TimesText = ");
		builder.Append((object?)TimesText);
		builder.Append(", VolatileValueText = ");
		builder.Append((object?)VolatileValueText);
		builder.Append(", RelativeTo = ");
		builder.Append((object?)RelativeTo);
		builder.Append(", ImageResourcePath = ");
		builder.Append((object?)ImageResourcePath);
		builder.Append(", MoveDetailType = ");
		builder.Append(MoveDetailType.ToString());
		return true;
	}

	[CompilerGenerated]
	public void Deconstruct(out float X, out float Y, out IntentType IntentType, out int? Value, out int Times, out string ValueText, out string TimesText, out string VolatileValueText, out string? RelativeTo, out string ImageResourcePath, out MoveDetailIconType MoveDetailType)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected I4, but got Unknown
		X = this.X;
		Y = this.Y;
		IntentType = (IntentType)(int)this.IntentType;
		Value = this.Value;
		Times = this.Times;
		ValueText = this.ValueText;
		TimesText = this.TimesText;
		VolatileValueText = this.VolatileValueText;
		RelativeTo = this.RelativeTo;
		ImageResourcePath = this.ImageResourcePath;
		MoveDetailType = this.MoveDetailType;
	}
}
