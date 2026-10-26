using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class HellraiserPredictionState
{
	public int InfiniteAutoPlaysThisTurn { get; set; } = ((PowerModel)power).GetInternalData<Data>().infiniteAutoPlaysThisTurn;


	public HellraiserPredictionState(HellraiserPower power)
	{
	}
}
