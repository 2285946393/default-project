using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed class CombatPredictionProjector
{
	private delegate CombatPredictionHistoryEntry? EntryHandler(CombatPredictionHistoryEntry entry);

	private readonly record struct EntryProjectionRule(EntryHandler Handler, bool Enabled);

	private const int MaxHoverTips = 10;

	private readonly CombatPredictionHistory _history;

	private readonly PredictionTraceFrame _rootFrame;

	private readonly Dictionary<Type, EntryProjectionRule> _rules = new Dictionary<Type, EntryProjectionRule>();

	private readonly List<IHoverTip> _hoverTips = new List<IHoverTip>();

	private readonly HashSet<CardModel> _highlightedCards = new HashSet<CardModel>();

	private readonly List<CombatPredictionDamageReceivedEntry> _damageEntries = new List<CombatPredictionDamageReceivedEntry>();

	private readonly List<CombatPredictionHistoryEntry> _relevantEntries = new List<CombatPredictionHistoryEntry>();

	private bool _projectionTruncated;

	private readonly CombatPredictionCausalTipBuilder _causalTips;

	public static CombatPredictionProjection Project(CombatPredictionHistory history, PredictionTraceFrame rootFrame)
	{
		return new CombatPredictionProjector(history, rootFrame).Project();
	}

	private CombatPredictionProjector(CombatPredictionHistory history, PredictionTraceFrame rootFrame)
	{
		PredictionActionKind? action = rootFrame.Invocation.Action;
		bool flag;
		if (action.HasValue)
		{
			PredictionActionKind valueOrDefault = action.GetValueOrDefault();
			if ((uint)valueOrDefault <= 1u)
			{
				flag = true;
				goto IL_0067;
			}
		}
		flag = false;
		goto IL_0067;
		IL_0067:
		if (!flag)
		{
			throw new ArgumentException($"Root frame must be a card-play or potion-use action, but was {action}.", "rootFrame");
		}
		_history = history;
		_rootFrame = rootFrame;
		_causalTips = new CombatPredictionCausalTipBuilder(rootFrame);
		RegisterRules();
	}

	private CombatPredictionProjection Project()
	{
		foreach (CombatPredictionHistoryEntry entry in _history.Entries)
		{
			if (_projectionTruncated)
			{
				break;
			}
			Dispatch(entry);
		}
		return FinalizeProjection();
	}

	private void RegisterRules()
	{
		PredictionActionKind? action = _rootFrame.Invocation.Action;
		bool flag = action switch
		{
			PredictionActionKind.CardPlay => true, 
			PredictionActionKind.PotionUse => false, 
			_ => throw new UnreachableException($"Unexpected action kind {action}."), 
		};
		ModSettings settings = ModData.Settings;
		Register<CombatPredictionCardGeneratedEntry>(HandleCardGenerated, flag ? settings.CombatCardGenerationPredictionEnabled : settings.PotionCardGenerationPredictionEnabled);
		Register<CombatPredictionCardGenerationOptionsEntry>(HandleCardGenerationOptions, flag ? settings.CombatCardGenerationPredictionEnabled : settings.PotionCardGenerationPredictionEnabled);
		Register<CombatPredictionPotionGeneratedEntry>(HandlePotionGenerated, settings.PotionGenerationPredictionEnabled);
		Register<CombatPredictionCardsSelectedEntry>(HandleCardsSelected, settings.CombatCardSelectionPredictionEnabled);
		Register<CombatPredictionAutoPlayFromDrawPileEntry>(HandleDrawPileAutoPlay, settings.AutoPlayFromDrawPilePredictionEnabled);
		Register<CombatPredictionCardDrawnEntry>(HandleCardDrawn, flag ? settings.CardDrawPredictionEnabled : settings.PotionDrawPredictionEnabled);
		Register<CombatPredictionCardCostsRandomizedEntry>(HandleCardCostsRandomized, flag ? settings.CardDrawPredictionEnabled : settings.PotionDrawPredictionEnabled);
		Register<CombatPredictionOrbChanneledEntry>(HandleOrbChanneled, settings.CombatOrbGenerationPredictionEnabled);
		Register<CombatPredictionDamageReceivedEntry>(HandleDamage, settings.CombatDamagePredictionEnabled);
	}

	private void Register<TEntry>(Func<TEntry, CombatPredictionHistoryEntry?> handler, bool enabled) where TEntry : CombatPredictionHistoryEntry
	{
		Func<TEntry, CombatPredictionHistoryEntry?> handler2 = handler;
		_rules.Add(typeof(TEntry), new EntryProjectionRule((CombatPredictionHistoryEntry entry) => handler2((TEntry)entry), enabled));
	}

	private void Dispatch(CombatPredictionHistoryEntry entry)
	{
		PredictionTraceFrame predictionTraceFrame = entry.Trace?.FindOriginatingAction();
		if (predictionTraceFrame == null || !predictionTraceFrame.Ancestors().Contains(_rootFrame))
		{
			return;
		}
		EntryProjectionRule value;
		if (!ModData.Settings.ChainedCardEffectPredictionEnabled && predictionTraceFrame != _rootFrame)
		{
			_projectionTruncated = true;
		}
		else if (_rules.TryGetValue(entry.GetType(), out value) && value.Enabled)
		{
			CombatPredictionHistoryEntry combatPredictionHistoryEntry = value.Handler(entry);
			if (combatPredictionHistoryEntry != null)
			{
				_relevantEntries.Add(combatPredictionHistoryEntry);
			}
		}
	}

	private CombatPredictionHistoryEntry? HandleCardGenerated(CombatPredictionCardGeneratedEntry entry)
	{
		if (entry.ResultKind == CardGenerationResultKind.Fixed)
		{
			return null;
		}
		CombatPredictionCardGenerationResolvedEntry resolvedEntry = _history.GetResolvedEntry<CombatPredictionCardGenerationResolvedEntry>(entry);
		if (resolvedEntry.PreviewCard == null)
		{
			return null;
		}
		AddHoverTip(PredictionHoverTipFactory.Card(resolvedEntry.PreviewCard));
		AddCausalEffect(entry, CausalEffectKind.GenerateCards, new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)resolvedEntry.PreviewCard));
		return resolvedEntry;
	}

	private CombatPredictionHistoryEntry? HandleCardGenerationOptions(CombatPredictionCardGenerationOptionsEntry entry)
	{
		if (entry.PreviewCards.Count == 0)
		{
			return null;
		}
		AddHoverTip(PredictionHoverTipFactory.CardBundle(entry.PreviewCards));
		AddCausalEffect(entry, CausalEffectKind.GenerateCards, (IEnumerable<AbstractModel>)entry.PreviewCards);
		return entry;
	}

	private CombatPredictionHistoryEntry HandlePotionGenerated(CombatPredictionPotionGeneratedEntry entry)
	{
		AddHoverTip(PredictionHoverTipFactory.Potion(entry.PreviewPotion));
		AddCausalEffect(entry, CausalEffectKind.GeneratePotion, new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)entry.PreviewPotion));
		return entry;
	}

	private CombatPredictionHistoryEntry? HandleCardsSelected(CombatPredictionCardsSelectedEntry entry)
	{
		if (entry.PreviewCards.Count == 0)
		{
			return null;
		}
		AddHoverTip(PredictionHoverTipFactory.CardBundle(entry.PreviewCards));
		AddCausalEffect(entry, CausalEffectKind.SelectCards, (IEnumerable<AbstractModel>)entry.PreviewCards);
		_highlightedCards.UnionWith(entry.SourceCards);
		return entry;
	}

	private CombatPredictionHistoryEntry HandleDrawPileAutoPlay(CombatPredictionAutoPlayFromDrawPileEntry entry)
	{
		AddHoverTip(PredictionHoverTipFactory.Card(entry.Card.Preview));
		AddCausalEffect(entry, CausalEffectKind.PlayCard, new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)entry.Card.Preview));
		return entry;
	}

	private CombatPredictionHistoryEntry HandleCardDrawn(CombatPredictionCardDrawnEntry entry)
	{
		CombatPredictionCardDrawResolvedEntry resolvedEntry = _history.GetResolvedEntry<CombatPredictionCardDrawResolvedEntry>(entry);
		AddHoverTip(PredictionHoverTipFactory.Card(resolvedEntry.PreviewCard));
		AddCausalEffect(entry, CausalEffectKind.DrawCards, new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)resolvedEntry.PreviewCard));
		return resolvedEntry;
	}

	private CombatPredictionHistoryEntry? HandleCardCostsRandomized(CombatPredictionCardCostsRandomizedEntry entry)
	{
		if (entry.PreviewCards.Count == 0)
		{
			return null;
		}
		_hoverTips.Clear();
		_hoverTips.AddRange(entry.PreviewCards.ToPredictionHoverTips());
		return entry;
	}

	private CombatPredictionHistoryEntry? HandleOrbChanneled(CombatPredictionOrbChanneledEntry entry)
	{
		AbstractModel source = entry.Trace.Source;
		if ((!(source is Chaos) && !(source is TrashToTreasurePower)) || 1 == 0)
		{
			return null;
		}
		AddHoverTip(PredictionHoverTipFactory.Orb(entry.Orb));
		AddCausalEffect(entry, CausalEffectKind.ChannelOrbs, new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)entry.Orb));
		return entry;
	}

	private CombatPredictionHistoryEntry? HandleDamage(CombatPredictionDamageReceivedEntry entry)
	{
		if (!DamagePredictionProjector.ShouldIncludeEntry(entry))
		{
			return null;
		}
		_damageEntries.Add(entry);
		return entry;
	}

	private CombatPredictionProjection FinalizeProjection()
	{
		DamagePrediction damagePrediction = DamagePredictionProjector.Project(_damageEntries);
		PredictionRisk risk = _history.GetRisk(_relevantEntries);
		IHoverTip val = _causalTips.Build();
		if (val != null)
		{
			_hoverTips.Insert(0, val);
		}
		_hoverTips.AddRange(risk.ToHoverTips());
		return new CombatPredictionProjection(_hoverTips, damagePrediction, _highlightedCards, risk);
	}

	private void AddHoverTip(IHoverTip hoverTip)
	{
		if (_hoverTips.Count < 10)
		{
			_hoverTips.Add(hoverTip);
		}
	}

	private void AddCausalEffect(CombatPredictionHistoryEntry entry, CausalEffectKind effect, IEnumerable<AbstractModel> results)
	{
		_causalTips.AddEffect(entry, effect, results);
	}
}
