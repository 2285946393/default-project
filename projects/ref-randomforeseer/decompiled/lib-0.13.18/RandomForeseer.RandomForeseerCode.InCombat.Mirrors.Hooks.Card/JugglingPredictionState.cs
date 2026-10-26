using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class JugglingPredictionState
{
	public int AttacksPlayedThisTurn { get; set; } = ((PowerModel)power).GetInternalData<Data>().attacksPlayedThisTurn;


	public JugglingPredictionState(JugglingPower power)
	{
	}
}
