using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class CurlUpPredictionState
{
	public CardModel? PlayedCard { get; set; }

	public bool Consumed { get; set; }
}
