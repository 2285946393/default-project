using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class AutomationPredictionState
{
	public int CardsLeft { get; set; } = ((PowerModel)power).DisplayAmount;


	public AutomationPredictionState(AutomationPower power)
	{
	}
}
