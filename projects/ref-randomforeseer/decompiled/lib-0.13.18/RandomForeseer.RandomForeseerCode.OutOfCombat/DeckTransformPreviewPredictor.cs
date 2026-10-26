using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal sealed class DeckTransformPreviewPredictor
{
	[CompilerGenerated]
	private Rng _003CrealRng_003EP;

	private readonly TransformPrediction _prediction;

	private Rng _previewRng;

	public DeckTransformPreviewPredictor(int maxSelect, Rng realRng, bool upgradePreview = false)
	{
		_003CrealRng_003EP = realRng;
		_prediction = new TransformPrediction(maxSelect, isInCombat: false, upgradePreview ? new Func<CardModel, CardModel>(PredictionUtils.CreateUpgradedCard) : null);
		_previewRng = _003CrealRng_003EP.Clone();
		base._002Ector();
	}

	public void Reset()
	{
		_previewRng = _003CrealRng_003EP.Clone();
	}

	public IReadOnlyList<IHoverTip> GetHoverTips(CardModel card, IEnumerable<CardModel> selectedCards)
	{
		return _prediction.GetHoverTips(card, selectedCards, _003CrealRng_003EP.Clone());
	}

	public CardTransformation PredictNext(CardModel original)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		CardModel val = _prediction.PredictNext(original, _previewRng);
		return new CardTransformation(original, val);
	}
}
