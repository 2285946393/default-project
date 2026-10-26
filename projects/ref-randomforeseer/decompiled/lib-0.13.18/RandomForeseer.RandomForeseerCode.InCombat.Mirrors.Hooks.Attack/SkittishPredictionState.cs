using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal sealed class SkittishPredictionState
{
	public bool HasGainedBlockThisTurn { get; set; } = power.HasGainedBlockThisTurn;


	public SkittishPredictionState(SkittishPower power)
	{
	}
}
