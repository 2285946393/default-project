using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatTransformPrediction
{
	private sealed class CombatTransformPredictionSession
	{
		[CompilerGenerated]
		private Rng _003CrealRng_003EP;

		private readonly TransformPrediction _prediction;

		public NPlayerHand Hand { get; }

		public CombatTransformPredictionSession(NPlayerHand hand, int maxSelect, Rng realRng)
		{
			_003CrealRng_003EP = realRng;
			_prediction = new TransformPrediction(maxSelect, isInCombat: true);
			Hand = hand;
			base._002Ector();
		}

		public IReadOnlyList<IHoverTip> GetHoverTips(CardModel hoveredCard)
		{
			if (Hand.GetCardHolder(hoveredCard) == null)
			{
				return Array.Empty<IHoverTip>();
			}
			return _prediction.GetHoverTips(hoveredCard, Hand._selectedCards, _003CrealRng_003EP.Clone());
		}
	}

	private static CombatTransformPredictionSession? _session;

	public static void BeginSession(NPlayerHand hand, CardSelectorPrefs prefs, AbstractModel? source)
	{
		_session = null;
		EntropyPower val = (EntropyPower)(object)((source is EntropyPower) ? source : null);
		Rng val2;
		if (val != null)
		{
			Player player = ((PowerModel)val).Owner.Player;
			val2 = ((player != null) ? player.RunState.Rng.CombatCardSelection : null);
		}
		else
		{
			val2 = null;
		}
		Rng val3 = val2;
		if (val3 != null)
		{
			_session = new CombatTransformPredictionSession(hand, ((CardSelectorPrefs)(ref prefs)).MaxSelect, val3);
		}
	}

	public static void EndSession(NPlayerHand hand)
	{
		if (_session?.Hand == hand)
		{
			_session = null;
		}
	}

	public static IReadOnlyList<IHoverTip> GetCardHoverTips(CardModel card)
	{
		CombatTransformPredictionSession session = _session;
		if (session == null)
		{
			return Array.Empty<IHoverTip>();
		}
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.CombatTransformPredictionEnabled)
		{
			return Array.Empty<IHoverTip>();
		}
		return session.GetHoverTips(card);
	}
}
