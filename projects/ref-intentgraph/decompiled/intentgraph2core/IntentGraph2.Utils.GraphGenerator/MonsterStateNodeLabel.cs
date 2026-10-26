using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace IntentGraph2.Utils.GraphGenerator;

internal record MonsterStateNodeLabel
{
	internal enum LabelType
	{
		Unknown,
		Random,
		Condition
	}

	public LabelType Type { get; set; }

	public required string Text { get; set; }

	public bool IsTextGenerated { get; set; }

	public int MaxRepeat { get; set; }

	public float Weight { get; set; }

	public int Cooldown { get; set; }

	public bool UseOnlyOnce { get; set; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected MonsterStateNodeLabel(MonsterStateNodeLabel original)
	{
		Type = original.Type;
		Text = original.Text;
		IsTextGenerated = original.IsTextGenerated;
		MaxRepeat = original.MaxRepeat;
		Weight = original.Weight;
		Cooldown = original.Cooldown;
		UseOnlyOnce = original.UseOnlyOnce;
	}

	public MonsterStateNodeLabel()
	{
	}
}
