using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common;

internal sealed class SimCardPile
{
	private readonly List<PredictedCard> _cards = cards.ToList();

	public PileType Type { get; }

	public IReadOnlyList<PredictedCard> Cards => _cards;

	public bool IsEmpty => _cards.Count == 0;

	public PredictedCard? TopCard
	{
		get
		{
			if (!IsEmpty)
			{
				return _cards[0];
			}
			return null;
		}
	}

	public PredictedCard? BottomCard
	{
		get
		{
			if (!IsEmpty)
			{
				List<PredictedCard> cards = _cards;
				return cards[cards.Count - 1];
			}
			return null;
		}
	}

	public SimCardPile(PileType type, IEnumerable<PredictedCard> cards)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		Type = type;
		base._002Ector();
	}

	public SimCardPile(CardPile pile)
		: this(pile.Type, pile.Cards.Select((CardModel card) => new PredictedCard(card)))
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)


	public void Add(PredictedCard card)
	{
		_cards.Add(card);
	}

	public void Insert(int index, PredictedCard card)
	{
		_cards.Insert(index, card);
	}

	public bool Remove(PredictedCard card)
	{
		return _cards.Remove(card);
	}

	public void Clear()
	{
		_cards.Clear();
	}

	public SimCardPile Clone()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return new SimCardPile(Type, _cards.Select((PredictedCard card) => card.Clone()));
	}

	public PredictedCard? Find(CardModel card)
	{
		CardModel card2 = card;
		return _cards.Find((PredictedCard predicted) => predicted.References(card2));
	}
}
