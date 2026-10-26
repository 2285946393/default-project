using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class PaelsLegionPredictionState
{
	public int Cooldown { get; set; } = relic._cooldown;


	public bool TriggeredBlockLastTurn { get; set; } = relic._triggeredBlockLastTurn;


	public CardPlay? AffectedCardPlay { get; set; } = relic._affectedCardPlay;


	public PaelsLegionPredictionState(PaelsLegion relic)
	{
	}
}
