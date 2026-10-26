using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal class PredictionCardBundleHoverTip : CardHoverTip, IHoverTip
{
	public IReadOnlyList<CardModel> Cards { get; }

	public PredictionCardBundleKind Kind { get; }

	string IHoverTip.Id => string.Empty;

	bool IHoverTip.IsInstanced => true;

	AbstractModel? IHoverTip.CanonicalModel => null;

	public PredictionCardBundleHoverTip(IReadOnlyList<CardModel> cards, PredictionCardBundleKind kind)
	{
		Cards = cards;
		Kind = kind;
		((CardHoverTip)this)._002Ector(cards[0]);
	}
}
