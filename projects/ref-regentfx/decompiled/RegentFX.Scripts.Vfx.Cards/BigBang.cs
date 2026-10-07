using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(BigBang))]
public class BigBang : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.None;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/big_bang.tscn";
}
