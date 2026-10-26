using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class ImitationLearningPredictionState
{
	public List<(PredictedCard Card, PredictedCard Clone)> CardAndClones = new List<(PredictedCard, PredictedCard)>();

	public int Amount { get; set; } = ((PowerModel)power).Amount;


	public ImitationLearningPredictionState(ImitationLearningPower power)
	{
	}
}
