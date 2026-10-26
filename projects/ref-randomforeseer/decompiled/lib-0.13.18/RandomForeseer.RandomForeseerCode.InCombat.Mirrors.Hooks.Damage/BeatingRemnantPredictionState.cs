using MegaCrit.Sts2.Core.Models.Relics;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal sealed class BeatingRemnantPredictionState
{
	public decimal DamageReceivedThisTurn { get; set; } = relic._damageReceivedThisTurn;


	public BeatingRemnantPredictionState(BeatingRemnant relic)
	{
	}
}
