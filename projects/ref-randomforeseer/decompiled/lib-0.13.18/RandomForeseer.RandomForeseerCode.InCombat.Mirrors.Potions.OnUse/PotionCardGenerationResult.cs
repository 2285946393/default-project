using System.Collections.Generic;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

internal sealed record PotionCardGenerationResult(IReadOnlyList<PredictedCard> Cards, bool AddsToHand);
