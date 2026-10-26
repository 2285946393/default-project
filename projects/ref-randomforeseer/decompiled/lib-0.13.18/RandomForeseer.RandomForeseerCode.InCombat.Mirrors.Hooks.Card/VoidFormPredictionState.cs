using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class VoidFormPredictionState
{
	public int CardsPlayedThisTurn { get; set; } = ((PowerModel)power).GetInternalData<Data>().cardsPlayedThisTurn;


	public VoidFormPredictionState(VoidFormPower power)
	{
	}
}
