using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal sealed class HardenedShellPredictionState
{
	public decimal DamageReceivedThisTurn { get; set; } = ((PowerModel)power).GetInternalData<Data>().damageReceivedThisTurn;


	public HardenedShellPredictionState(HardenedShellPower power)
	{
	}
}
