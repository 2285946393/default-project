using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal class PredictionCardHoverTip : CardHoverTip, IHoverTip
{
	public bool IsDimmed { get; }

	bool IHoverTip.IsInstanced => true;

	AbstractModel? IHoverTip.CanonicalModel => null;

	public PredictionCardHoverTip(CardModel card, bool isDimmed)
	{
		IsDimmed = isDimmed;
		((CardHoverTip)this)._002Ector(card);
	}
}
