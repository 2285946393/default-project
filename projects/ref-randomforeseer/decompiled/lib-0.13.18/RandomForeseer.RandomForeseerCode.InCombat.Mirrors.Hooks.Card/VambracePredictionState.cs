using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class VambracePredictionState
{
	public CardModel? TriggeringCard { get; set; } = relic._triggeringCard;


	public bool BlockGainedThisCombat { get; set; } = relic._blockGainedThisCombat;


	public VambracePredictionState(Vambrace relic)
	{
	}
}
