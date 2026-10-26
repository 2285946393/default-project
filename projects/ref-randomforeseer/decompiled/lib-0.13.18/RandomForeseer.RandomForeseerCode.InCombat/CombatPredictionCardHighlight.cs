using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatPredictionCardHighlight
{
	private static readonly Color PredictionHighlightColor = new Color(1f, 0.36f, 0f, 0.98f);

	private static readonly HashSet<CardModel> HighlightedCards = new HashSet<CardModel>();

	private static readonly HashSet<CardModel> QueuedCards = new HashSet<CardModel>();

	private static bool _refreshQueued;

	public static void Show(IEnumerable<CardModel> cards)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		QueuedCards.UnionWith(HighlightedCards);
		HighlightedCards.Clear();
		HighlightedCards.UnionWith(cards);
		QueuedCards.UnionWith(HighlightedCards);
		if (QueuedCards.Count != 0 && !_refreshQueued)
		{
			_refreshQueued = true;
			Callable val = Callable.From((Action)FlushQueuedCards);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	public static void Clear()
	{
		Show(Array.Empty<CardModel>());
	}

	public static void ApplyHighlightToHolder(NHandCardHolder holder)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (!((Node)holder).IsNodeReady())
		{
			return;
		}
		NCard cardNode = ((NCardHolder)holder).CardNode;
		if (cardNode != null)
		{
			CardModel model = cardNode.Model;
			if (model != null && HighlightedCards.Contains(model))
			{
				cardNode.CardHighlight.AnimShow();
				((CanvasItem)cardNode.CardHighlight).Modulate = PredictionHighlightColor;
			}
		}
	}

	private static void FlushQueuedCards()
	{
		CardModel[] cards = QueuedCards.ToArray();
		QueuedCards.Clear();
		_refreshQueued = false;
		RefreshHandCards(cards);
	}

	private static void RefreshHandCards(IEnumerable<CardModel> cards)
	{
		NPlayerHand instance = NPlayerHand.Instance;
		if (instance == null)
		{
			return;
		}
		foreach (CardModel card in cards)
		{
			try
			{
				NCardHolder cardHolder = instance.GetCardHolder(card);
				NHandCardHolder val = (NHandCardHolder)(object)((cardHolder is NHandCardHolder) ? cardHolder : null);
				if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val) && GodotObject.IsInstanceValid((GodotObject)(object)((NCardHolder)val).CardNode))
				{
					val.UpdateCard();
				}
			}
			catch (Exception ex)
			{
				Entry.Logger.Warn($"Combat card highlight failed on refresh: {ex}", 1);
				ModTelemetry.CaptureException(ex, "combat_card_highlight", "refresh_card");
			}
		}
	}
}
