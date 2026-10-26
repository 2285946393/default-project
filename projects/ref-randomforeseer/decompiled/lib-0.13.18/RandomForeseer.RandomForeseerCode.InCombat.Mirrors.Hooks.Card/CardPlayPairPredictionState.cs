using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class CardPlayPairPredictionState
{
	public Dictionary<CardPlay, int> Amounts { get; } = new Dictionary<CardPlay, int>(ReferenceEqualityComparer.Instance);

}
