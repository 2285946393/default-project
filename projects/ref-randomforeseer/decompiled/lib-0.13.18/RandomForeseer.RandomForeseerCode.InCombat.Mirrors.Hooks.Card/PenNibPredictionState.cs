using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class PenNibPredictionState
{
	public int AttacksPlayed { get; set; } = relic.AttacksPlayed;


	public CardModel? AttackToDouble { get; set; }

	public PenNibPredictionState(PenNib relic)
	{
	}
}
