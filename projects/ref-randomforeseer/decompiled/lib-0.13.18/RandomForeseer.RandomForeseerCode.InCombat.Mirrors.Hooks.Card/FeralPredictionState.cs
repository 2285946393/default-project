using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class FeralPredictionState
{
	public int ZeroCostAttacksPlayed { get; set; } = ((PowerModel)power).GetInternalData<Data>().zeroCostAttacksPlayed;


	public FeralPredictionState(FeralPower power)
	{
	}
}
