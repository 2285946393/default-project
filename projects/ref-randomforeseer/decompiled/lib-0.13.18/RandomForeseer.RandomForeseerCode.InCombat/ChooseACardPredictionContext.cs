using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Utils;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class ChooseACardPredictionContext
{
	private sealed class Registration
	{
		public HashSet<CardModel> Cards { get; }

		public AbstractModel Source { get; }

		public Registration(IEnumerable<CardModel> cards, AbstractModel source)
		{
			HashSet<CardModel> hashSet = new HashSet<CardModel>();
			foreach (CardModel card in cards)
			{
				hashSet.Add(card);
			}
			Cards = hashSet;
			Source = source;
			base._002Ector();
		}
	}

	private static readonly List<Registration> Registrations = new List<Registration>();

	public static bool TryGet(CardModel card, [NotNullWhen(true)] out AbstractModel? source)
	{
		CardModel card2 = card;
		lock (Registrations)
		{
			Registration registration = Registrations.FindLast((Registration item) => item.Cards.Contains(card2));
			if (registration != null)
			{
				source = registration.Source;
				return true;
			}
		}
		source = null;
		return false;
	}

	public static IDisposable? Enter(IReadOnlyList<CardModel> cards, AbstractModel? source)
	{
		if (cards.Count == 0 || source == null)
		{
			return null;
		}
		Registration registration = new Registration(cards, source);
		lock (Registrations)
		{
			Registrations.Add(registration);
		}
		return new DisposableAction(delegate
		{
			lock (Registrations)
			{
				Registrations.Remove(registration);
			}
		});
	}
}
