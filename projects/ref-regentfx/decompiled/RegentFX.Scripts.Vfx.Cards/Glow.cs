using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(Glow))]
public class Glow : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.None;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/glow.tscn";
}
