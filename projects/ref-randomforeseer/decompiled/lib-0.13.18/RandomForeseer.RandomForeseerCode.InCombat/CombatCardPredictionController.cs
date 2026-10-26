using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatCardPredictionController
{
	private sealed class CardPredictionSession : CombatPredictionInteractionSession
	{
		[CompilerGenerated]
		private CardModel _003Ccard_003EP;

		public override AbstractModel Source => (AbstractModel)(object)_003Ccard_003EP;

		public NHandCardHolder Holder { get; }

		public CardPredictionSession(CardModel card, NHandCardHolder holder, CombatPredictionInteractionMode mode)
		{
			_003Ccard_003EP = card;
			Holder = holder;
			base._002Ector(mode);
		}

		protected override CombatPredictionProjection? Predict(Creature? target)
		{
			return CombatCardPrediction.Predict(_003Ccard_003EP, target);
		}
	}

	private static CardPredictionSession? _session;

	public static void OnCardHover(NHandCardHolder holder, bool isHovered)
	{
		if (isHovered)
		{
			BeginSession(CombatPredictionInteractionMode.Hover, holder);
		}
		else
		{
			EndSession(CombatPredictionInteractionMode.Hover, holder);
		}
	}

	public static void OnCardPlayStarted(NHandCardHolder holder)
	{
		BeginSession(CombatPredictionInteractionMode.Action, holder);
	}

	public static void OnCardPlayTargetingStarting(Control control)
	{
		CardPredictionSession session = _session;
		if (session != null && session.Mode == CombatPredictionInteractionMode.Action && !session.IsTargeting && control == ((NCardHolder)session.Holder).CardNode)
		{
			session.BeginTargeting(NTargetManager.Instance);
		}
	}

	public static void OnCardPlayCleanedUp(NHandCardHolder holder)
	{
		ClearCardPlayHoverTips(holder);
		EndSession(CombatPredictionInteractionMode.Action, holder);
	}

	public static bool TryGetActiveHoverTips(CardModel card, out IReadOnlyList<IHoverTip> hoverTips)
	{
		CardPredictionSession session = _session;
		if (session != null && session.Mode == CombatPredictionInteractionMode.Hover && session.Source == card)
		{
			hoverTips = session.Projection?.HoverTips ?? Array.Empty<IHoverTip>();
			return true;
		}
		hoverTips = Array.Empty<IHoverTip>();
		return false;
	}

	private static void OnProjectionChanged(CardPredictionSession session)
	{
		if (session == _session && session.IsTargeting)
		{
			ShowCardPlayHoverTips(session);
		}
	}

	private static void BeginSession(CombatPredictionInteractionMode mode, NHandCardHolder holder)
	{
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.CardPlayPredictionEnabled || !CombatManager.Instance.IsInProgress)
		{
			return;
		}
		CardModel cardModel = ((NCardHolder)holder).CardModel;
		if (cardModel == null)
		{
			return;
		}
		Player owner = cardModel.Owner;
		if (owner == null)
		{
			return;
		}
		Creature creature = owner.Creature;
		if (creature == null || creature.CombatState == null)
		{
			return;
		}
		CardPredictionSession? session2 = _session;
		if (session2 != null && session2.Mode > mode)
		{
			return;
		}
		CardPredictionSession session = new CardPredictionSession(cardModel, holder, mode);
		session.ProjectionChanged += delegate
		{
			OnProjectionChanged(session);
		};
		CardPredictionSession session3 = _session;
		_session = session;
		try
		{
			session.RefreshUntargeted();
		}
		finally
		{
			session3?.Dispose();
		}
	}

	private static void EndSession(CombatPredictionInteractionMode mode, NHandCardHolder holder)
	{
		CardPredictionSession session = _session;
		if (session != null && session.Mode == mode && session.Holder == holder)
		{
			_session = null;
			session.Dispose();
		}
	}

	private static void ShowCardPlayHoverTips(CardPredictionSession session)
	{
		NHandCardHolder holder = session.Holder;
		ClearCardPlayHoverTips(holder);
		IReadOnlyList<IHoverTip> readOnlyList = session.Projection?.HoverTips;
		if (readOnlyList == null || readOnlyList.Count <= 0)
		{
			return;
		}
		bool shouldBlockHoverTips = NHoverTipSet.shouldBlockHoverTips;
		NHoverTipSet.shouldBlockHoverTips = false;
		try
		{
			NHoverTipSet obj = NHoverTipSet.CreateAndShow((Control)(object)holder, (IEnumerable<IHoverTip>)readOnlyList, (HoverTipAlignment)0);
			if (obj != null)
			{
				obj.SetAlignmentForCardHolder((NCardHolder)(object)holder);
			}
		}
		finally
		{
			NHoverTipSet.shouldBlockHoverTips = shouldBlockHoverTips;
		}
	}

	private static void ClearCardPlayHoverTips(NHandCardHolder holder)
	{
		NHoverTipSet.Remove((Control)(object)holder);
	}
}
