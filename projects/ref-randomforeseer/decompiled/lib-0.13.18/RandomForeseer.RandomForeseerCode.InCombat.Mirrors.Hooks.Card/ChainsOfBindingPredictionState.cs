using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ChainsOfBindingPredictionState
{
	public bool BoundCardPlayed { get; set; } = ((PowerModel)power).GetInternalData<Data>().boundCardPlayed;


	public ChainsOfBindingPredictionState(ChainsOfBindingPower power)
	{
	}
}
