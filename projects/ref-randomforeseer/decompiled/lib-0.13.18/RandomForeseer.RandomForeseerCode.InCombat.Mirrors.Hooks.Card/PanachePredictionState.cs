using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class PanachePredictionState
{
	public bool AlreadyApplied { get; set; } = ((PowerModel)power).GetInternalData<Data>().alreadyApplied;


	public int CardsLeft { get; set; } = ((PowerModel)power).DynamicVars["CardsLeft"].IntValue;


	public PanachePredictionState(PanachePower power)
	{
	}
}
