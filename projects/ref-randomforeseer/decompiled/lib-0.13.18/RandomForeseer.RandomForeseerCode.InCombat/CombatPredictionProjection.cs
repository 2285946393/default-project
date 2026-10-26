using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed record CombatPredictionProjection(IReadOnlyList<IHoverTip> HoverTips, DamagePrediction DamagePrediction, IReadOnlySet<CardModel> HighlightedCards, PredictionRisk Risk);
