using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionHistory
{
	[CompilerGenerated]
	private PredictionTrace _003Ctrace_003EP;

	private readonly List<CombatPredictionHistoryEntry> _entries;

	private readonly Dictionary<Type, int> _entryCounts;

	private readonly Dictionary<CombatPredictionHistoryEntry, CombatPredictionHistoryEntry> _completions;

	public IReadOnlyList<CombatPredictionHistoryEntry> Entries => _entries;

	public CombatPredictionHistory(PredictionTrace trace)
	{
		_003Ctrace_003EP = trace;
		_entries = new List<CombatPredictionHistoryEntry>();
		_entryCounts = new Dictionary<Type, int>();
		_completions = new Dictionary<CombatPredictionHistoryEntry, CombatPredictionHistoryEntry>(ReferenceEqualityComparer.Instance);
		base._002Ector();
	}

	public IEnumerable<TEntry> OfType<TEntry>() where TEntry : CombatPredictionHistoryEntry
	{
		return _entries.OfType<TEntry>();
	}

	public int Count<TEntry>() where TEntry : CombatPredictionHistoryEntry
	{
		return _entryCounts.GetValueOrDefault(typeof(TEntry), 0);
	}

	public PredictionRisk GetRisk(IEnumerable<CombatPredictionHistoryEntry> entries)
	{
		CombatPredictionHistoryEntry combatPredictionHistoryEntry = null;
		foreach (CombatPredictionHistoryEntry entry in entries)
		{
			ValidateOwnership(entry);
			if (combatPredictionHistoryEntry == null || entry.Index > combatPredictionHistoryEntry.Index)
			{
				combatPredictionHistoryEntry = entry;
			}
		}
		if (combatPredictionHistoryEntry != null)
		{
			return GetRiskThrough(combatPredictionHistoryEntry.Index);
		}
		return PredictionRisk.None;
	}

	public PredictionRisk GetCurrentRisk()
	{
		if (_entries.Count != 0)
		{
			return GetRiskThrough(_entries.Count - 1);
		}
		return PredictionRisk.None;
	}

	public void RecordRisk(PredictionRiskReason reason)
	{
		Record(new CombatPredictionRiskEntry
		{
			Reason = reason
		});
	}

	public void CardAfflicted(PredictedCard card, AfflictionModel affliction)
	{
		Record(new CombatPredictionCardAfflictedEntry
		{
			Card = card,
			Affliction = affliction
		});
	}

	public CombatPredictionCardDrawnEntry CardDrawn(PredictedCard card, bool fromHandDraw)
	{
		return Record(new CombatPredictionCardDrawnEntry
		{
			Card = card,
			FromHandDraw = fromHandDraw
		});
	}

	public void CardDrawResolved(CombatPredictionCardDrawnEntry originalEntry, PredictedCard card)
	{
		Complete(originalEntry, new CombatPredictionCardDrawResolvedEntry
		{
			OriginalEntry = originalEntry,
			PreviewCard = PredictionCloner.CloneModel<CardModel>(card.Preview)
		});
	}

	public void CardCostsRandomized(IReadOnlyList<PredictedCard> cards)
	{
		Record(new CombatPredictionCardCostsRandomizedEntry
		{
			PreviewCards = CreatePreviewCards(cards)
		});
	}

	public void CardsSelected(IReadOnlyList<PredictedCard> cards)
	{
		CombatPredictionCardsSelectedEntry combatPredictionCardsSelectedEntry = new CombatPredictionCardsSelectedEntry();
		List<CardModel> list = new List<CardModel>();
		list.AddRange(cards.SelectOriginals());
		combatPredictionCardsSelectedEntry.SourceCards = new _003C_003Ez__ReadOnlyList<CardModel>(list);
		combatPredictionCardsSelectedEntry.PreviewCards = CreatePreviewCards(cards);
		Record(combatPredictionCardsSelectedEntry);
	}

	public void CardPlayStarted(PredictedCard card, CardPlay cardPlay)
	{
		Record(new CombatPredictionCardPlayStartedEntry
		{
			Card = card,
			CardPlay = cardPlay
		});
	}

	public void CardPlayFinished(PredictedCard card, CardPlay cardPlay, bool wasEthereal)
	{
		Record(new CombatPredictionCardPlayFinishedEntry
		{
			Card = card,
			CardPlay = cardPlay,
			WasEthereal = wasEthereal
		});
	}

	public CombatPredictionCardGeneratedEntry CardGenerated(PredictedCard card, CardGenerationResultKind resultKind)
	{
		return Record(new CombatPredictionCardGeneratedEntry
		{
			Card = card,
			ResultKind = resultKind
		});
	}

	public void CardGenerationResolved(CombatPredictionCardGeneratedEntry originalEntry, PredictedCard? card)
	{
		Complete(originalEntry, new CombatPredictionCardGenerationResolvedEntry
		{
			OriginalEntry = originalEntry,
			PreviewCard = ((card == null) ? null : PredictionCloner.CloneModel<CardModel>(card.Preview))
		});
	}

	public void CardGenerationOptions(IReadOnlyList<PredictedCard> cards)
	{
		Record(new CombatPredictionCardGenerationOptionsEntry
		{
			PreviewCards = CreatePreviewCards(cards)
		});
	}

	public void AutoPlayFromDrawPile(PredictedCard card)
	{
		Record(new CombatPredictionAutoPlayFromDrawPileEntry
		{
			Card = card
		});
	}

	public void PotionGenerated(PotionModel potion)
	{
		Record(new CombatPredictionPotionGeneratedEntry
		{
			PreviewPotion = PredictionCloner.CloneModel<PotionModel>(potion)
		});
	}

	public void CreatureAttacked(Creature attacker, IReadOnlyList<DamageResult> hitResults)
	{
		Record(new CombatPredictionCreatureAttackedEntry
		{
			Attacker = attacker,
			HitResults = hitResults
		});
	}

	public void EnergySpent(Player player, int amount)
	{
		Record(new CombatPredictionEnergySpentEntry
		{
			Player = player,
			Amount = amount
		});
	}

	public void StarsModified(Player player, int amount)
	{
		Record(new CombatPredictionStarsModifiedEntry
		{
			Player = player,
			Amount = amount
		});
	}

	public void DamageReceived(Creature receiver, Creature? dealer, DamageResult result, PredictedCard? cardSource)
	{
		Record(new CombatPredictionDamageReceivedEntry
		{
			Receiver = receiver,
			Result = result,
			Dealer = dealer,
			CardSource = cardSource
		});
	}

	public void OrbChanneled(OrbModel orb)
	{
		Record(new CombatPredictionOrbChanneledEntry
		{
			Orb = orb
		});
	}

	public TResolved GetResolvedEntry<TResolved>(CombatPredictionHistoryEntry originalEntry) where TResolved : CombatPredictionHistoryEntry
	{
		return ((_completions.GetValueOrDefault(originalEntry) ?? throw new InvalidOperationException("The deferred history entry has not been resolved.")) as TResolved) ?? throw new InvalidOperationException("The deferred history entry has an invalid resolution type.");
	}

	private TEntry Record<TEntry>(TEntry entry) where TEntry : CombatPredictionHistoryEntry
	{
		entry.Index = _entries.Count;
		entry.Trace = _003Ctrace_003EP.Current;
		_entries.Add(entry);
		CollectionsMarshal.GetValueRefOrAddDefault(_entryCounts, entry.GetType(), out var _)++;
		return entry;
	}

	private static IReadOnlyList<CardModel> CreatePreviewCards(IEnumerable<PredictedCard> cards)
	{
		List<CardModel> list = new List<CardModel>();
		list.AddRange(cards.Select((PredictedCard card) => PredictionCloner.CloneModel<CardModel>(card.Preview)));
		return new _003C_003Ez__ReadOnlyList<CardModel>(list);
	}

	private void Complete(CombatPredictionHistoryEntry originalEntry, CombatPredictionHistoryEntry resolvedEntry)
	{
		ValidateOwnership(originalEntry);
		if (_completions.ContainsKey(originalEntry))
		{
			throw new InvalidOperationException("The deferred history entry has already been resolved.");
		}
		Record(resolvedEntry);
		_completions.Add(originalEntry, resolvedEntry);
	}

	private void ValidateOwnership(CombatPredictionHistoryEntry entry)
	{
		int index = entry.Index;
		if (index < 0 || index >= _entries.Count || _entries[index] != entry)
		{
			throw new InvalidOperationException("The history entry does not belong to this history.");
		}
	}

	private CombatPredictionRisk GetRiskThrough(int boundaryIndex)
	{
		List<CombatPredictionRiskEntry> list = new List<CombatPredictionRiskEntry>();
		list.AddRange(_entries.Take(boundaryIndex + 1).OfType<CombatPredictionRiskEntry>());
		return new CombatPredictionRisk(new _003C_003Ez__ReadOnlyList<CombatPredictionRiskEntry>(list));
	}
}
