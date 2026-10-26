using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common;

internal sealed class PredictedCard : IComparable<PredictedCard>
{
	private CardModel? _preview;

	public CardModel Original { get; }

	public CardModel Preview => _preview ?? Original;

	public CardModel MutablePreview
	{
		get
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Expected O, but got Unknown
			//IL_0022: Expected O, but got Unknown
			CardModel obj = _preview;
			if (obj == null)
			{
				CardModel val = (CardModel)((AbstractModel)Original).MutableClone();
				CardModel val2 = val;
				_preview = val;
				obj = val2;
			}
			return obj;
		}
	}

	public PredictedCard(CardModel original, CardModel? preview = null)
	{
		_preview = preview;
		Original = original;
		base._002Ector();
	}

	public static PredictedCard FromGenerated(CardModel card)
	{
		return new PredictedCard(card, card);
	}

	public static PredictedCard Create(CardModel canonicalCard, Player player)
	{
		return FromGenerated(PredictionUtils.CreateCard(canonicalCard, player));
	}

	public bool References(object? card)
	{
		if (Original != card)
		{
			return Preview == card;
		}
		return true;
	}

	public PredictedCard Clone()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		CardModel original = Original;
		CardModel? preview = _preview;
		return new PredictedCard(original, (CardModel)((preview != null) ? ((AbstractModel)preview).MutableClone() : null));
	}

	public int CompareTo(PredictedCard? other)
	{
		return ((AbstractModel)Preview).CompareTo((AbstractModel)(object)other?.Preview);
	}
}
