using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common;

internal sealed class TransformPrediction
{
	[CompilerGenerated]
	private int _003CmaxSelect_003EP;

	[CompilerGenerated]
	private bool _003CisInCombat_003EP;

	[CompilerGenerated]
	private Func<CardModel, CardModel>? _003CmapReplacement_003EP;

	public TransformPrediction(int maxSelect, bool isInCombat, Func<CardModel, CardModel>? mapReplacement = null)
	{
		_003CmaxSelect_003EP = maxSelect;
		_003CisInCombat_003EP = isInCombat;
		_003CmapReplacement_003EP = mapReplacement;
		base._002Ector();
	}

	public IReadOnlyList<IHoverTip> GetHoverTips(CardModel card, IEnumerable<CardModel> selectedCards, Rng previewRng)
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<CardModel> readOnlyList = PredictReplacements(card, previewRng);
		if (readOnlyList.Count == 0)
		{
			return Array.Empty<IHoverTip>();
		}
		List<CardModel> list = selectedCards.ToList();
		int activeIndex = list.IndexOf(card);
		bool num = activeIndex >= 0;
		if (!num)
		{
			activeIndex = Math.Min(_003CmaxSelect_003EP - 1, list.Count);
		}
		string key = (num ? "transform_selection_selected" : "transform_selection_unselected");
		string transformedCard = readOnlyList[activeIndex].Title;
		List<string> otherTransformedCards = (from replacement in readOnlyList.Where((CardModel _, int index) => index != activeIndex)
			select replacement.Title).Distinct().ToList();
		HoverTip val = PredictionHoverTipFactory.Text(key, delegate(LocString description)
		{
			description.Add("TransformedCard", transformedCard);
			description.Add("HasOtherTransformedCards", otherTransformedCards.Count > 0);
			description.Add("OtherTransformedCards", (IList<string>)otherTransformedCards);
		});
		IEnumerable<IHoverTip> collection = readOnlyList.Select((CardModel replacement, int index) => PredictionHoverTipFactory.Card(replacement, index != activeIndex));
		List<IHoverTip> list2 = new List<IHoverTip>();
		list2.Add((IHoverTip)(object)val);
		list2.AddRange(collection);
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
	}

	public IReadOnlyList<CardModel> PredictReplacements(CardModel card, Rng previewRng)
	{
		CardModel card2 = card;
		Rng previewRng2 = previewRng;
		if (_003CmaxSelect_003EP <= 0)
		{
			return Array.Empty<CardModel>();
		}
		List<CardModel> list = new List<CardModel>();
		list.AddRange(from _ in Enumerable.Range(0, _003CmaxSelect_003EP)
			select PredictNext(card2, previewRng2));
		return new _003C_003Ez__ReadOnlyList<CardModel>(list);
	}

	public CardModel PredictReplacement(CardModel card, int selectionIndex, Rng previewRng)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(selectionIndex, "selectionIndex");
		previewRng.Advance(selectionIndex);
		return PredictNext(card, previewRng);
	}

	public CardModel PredictNext(CardModel card, Rng previewRng)
	{
		CardModel val = PredictionUtils.PredictTransformResult(card, previewRng, _003CisInCombat_003EP);
		if (_003CmapReplacement_003EP == null)
		{
			return val;
		}
		return _003CmapReplacement_003EP(val);
	}
}
