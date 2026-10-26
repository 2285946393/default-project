using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class MusicBoxPredictionState
{
	public bool WasUsedThisTurn { get; set; } = relic._wasUsedThisTurn;


	public CardModel? CardBeingPlayed { get; set; }

	public MusicBoxPredictionState(MusicBox relic)
	{
	}
}
