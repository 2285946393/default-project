using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Potions;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatPotionPredictionController
{
	private sealed class PotionPredictionSession : CombatPredictionInteractionSession
	{
		[CompilerGenerated]
		private PotionModel _003Cpotion_003EP;

		public override AbstractModel Source => (AbstractModel)(object)_003Cpotion_003EP;

		public NPotionHolder Holder { get; }

		public PotionPredictionSession(PotionModel potion, NPotionHolder holder, CombatPredictionInteractionMode mode)
		{
			_003Cpotion_003EP = potion;
			Holder = holder;
			base._002Ector(mode);
		}

		protected override CombatPredictionProjection? Predict(Creature? target)
		{
			return CombatPotionPrediction.Predict(_003Cpotion_003EP, target);
		}
	}

	private static PotionPredictionSession? _session;

	public static void OnPotionFocus(NPotionHolder holder)
	{
		if (!holder._isFocused)
		{
			BeginSession(CombatPredictionInteractionMode.Hover, holder);
		}
	}

	public static void OnPotionUnfocus(NPotionHolder holder)
	{
		EndSession(CombatPredictionInteractionMode.Hover, holder);
	}

	public static void OnPotionRemoved(NPotionHolder holder)
	{
		PotionPredictionSession session = _session;
		if (session != null && session.Holder == holder)
		{
			ClearHolderHoverTips(holder);
			_session = null;
			session.Dispose();
		}
	}

	public static void OnPotionPopupOpen(NPotionHolder holder)
	{
		BeginSession(CombatPredictionInteractionMode.Action, holder);
	}

	public static void OnPotionTargetingStart(NPotionHolder holder)
	{
		PotionPredictionSession session = _session;
		if (session != null && session.Mode == CombatPredictionInteractionMode.Action && !session.IsTargeting && session.Holder == holder)
		{
			session.BeginTargeting(NTargetManager.Instance);
		}
	}

	public static void OnPotionPopupClose(NPotionHolder holder)
	{
		PotionPredictionSession session = _session;
		if (session == null || !session.IsTargeting)
		{
			EndSession(CombatPredictionInteractionMode.Action, holder);
		}
	}

	public static bool TryGetActiveHoverTips(PotionModel potion, out IReadOnlyList<IHoverTip> hoverTips)
	{
		PotionPredictionSession session = _session;
		if (session != null && session.Source == potion)
		{
			hoverTips = session.Projection?.HoverTips ?? Array.Empty<IHoverTip>();
			return true;
		}
		hoverTips = Array.Empty<IHoverTip>();
		return false;
	}

	private static void OnProjectionChanged(PotionPredictionSession session)
	{
		if (session == _session && session.IsTargeting)
		{
			ShowTargetingHoverTips(session);
		}
	}

	private static void OnTargetingFinishing(PotionPredictionSession session)
	{
		if (_session == session)
		{
			ClearHolderHoverTips(session.Holder);
			_session = null;
			session.Dispose();
		}
	}

	private static void BeginSession(CombatPredictionInteractionMode mode, NPotionHolder holder)
	{
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.PotionPredictionEnabled || !CombatManager.Instance.IsInProgress)
		{
			return;
		}
		NPotion potion = holder.Potion;
		PotionModel val = ((potion != null) ? potion.Model : null);
		if (val == null)
		{
			return;
		}
		Player owner = val.Owner;
		if (owner == null)
		{
			return;
		}
		Creature creature = owner.Creature;
		if (creature == null || creature.CombatState == null)
		{
			return;
		}
		PotionPredictionSession? session2 = _session;
		if (session2 != null && session2.Mode > mode)
		{
			return;
		}
		PotionPredictionSession session = new PotionPredictionSession(val, holder, mode);
		session.ProjectionChanged += delegate
		{
			OnProjectionChanged(session);
		};
		session.TargetingFinishing += delegate
		{
			OnTargetingFinishing(session);
		};
		PotionPredictionSession session3 = _session;
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

	private static void EndSession(CombatPredictionInteractionMode mode, NPotionHolder holder)
	{
		PotionPredictionSession session = _session;
		if (session != null && session.Mode == mode && session.Holder == holder)
		{
			_session = null;
			session.Dispose();
		}
	}

	private static void ShowTargetingHoverTips(PotionPredictionSession session)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		NPotionHolder holder = session.Holder;
		ClearHolderHoverTips(holder);
		IReadOnlyList<IHoverTip> readOnlyList = session.Projection?.HoverTips;
		if (readOnlyList == null || readOnlyList.Count <= 0)
		{
			return;
		}
		bool shouldBlockHoverTips = NHoverTipSet.shouldBlockHoverTips;
		NHoverTipSet.shouldBlockHoverTips = false;
		try
		{
			NHoverTipSet obj = NHoverTipSet.CreateAndShow((Control)(object)holder, (IEnumerable<IHoverTip>)readOnlyList, (HoverTipAlignment)3);
			if (obj != null)
			{
				((Control)obj).SetGlobalPosition(((Control)holder).GlobalPosition + Vector2.Down * ((Control)holder).Size.Y * Mathf.Max(1.5f, ((Control)holder).Scale.Y), false);
			}
		}
		finally
		{
			NHoverTipSet.shouldBlockHoverTips = shouldBlockHoverTips;
		}
	}

	private static void ClearHolderHoverTips(NPotionHolder holder)
	{
		NHoverTipSet.Remove((Control)(object)holder);
	}
}
