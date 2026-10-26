using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.Settings;
using RandomForeseer.RandomForeseerCode.Telemetry;
using STS2RitsuLib.Settings;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class EndTurnPredictionController
{
	private static bool _isSubscribed;

	private static bool _isActionDamageOverrideActive;

	private static bool _isDeferredRefreshScheduled;

	private static bool _refreshPending;

	private static NEndTurnButton? _focusedEndTurnButton;

	public static void Subscribe()
	{
		if (!_isSubscribed)
		{
			CombatManager.Instance.AboutToSwitchToEnemyTurn += OnAboutToSwitchToEnemyTurn;
			CombatManager.Instance.PlayerEndedTurn += OnPlayerEndedTurn;
			CombatManager.Instance.PlayerUnendedTurn += OnPlayerUnendedTurn;
			CombatManager.Instance.CombatEnded += OnCombatEnded;
			CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
			RunManager.Instance.ActionExecutor.AfterActionExecuted += OnActionExecuted;
			ModSettingsBindingWriteEvents.ValueWritten += OnSettingsValueWritten;
			_isSubscribed = true;
			RefreshSafely();
		}
	}

	public static void Unsubscribe()
	{
		if (_isSubscribed)
		{
			CombatManager.Instance.AboutToSwitchToEnemyTurn -= OnAboutToSwitchToEnemyTurn;
			CombatManager.Instance.PlayerEndedTurn -= OnPlayerEndedTurn;
			CombatManager.Instance.PlayerUnendedTurn -= OnPlayerUnendedTurn;
			CombatManager.Instance.CombatEnded -= OnCombatEnded;
			CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
			RunManager.Instance.ActionExecutor.AfterActionExecuted -= OnActionExecuted;
			ModSettingsBindingWriteEvents.ValueWritten -= OnSettingsValueWritten;
			_isSubscribed = false;
			Cleanup();
		}
	}

	public static void OnEndTurnButtonFocused(NEndTurnButton endTurnButton)
	{
		_focusedEndTurnButton = endTurnButton;
		RefreshSafely();
	}

	public static void OnEndTurnButtonUnfocused()
	{
		_focusedEndTurnButton = null;
		RefreshSafely();
	}

	public static void RefreshSafely()
	{
		try
		{
			Refresh();
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"End-turn prediction refresh failed: {ex}", 1);
			ModTelemetry.CaptureException(ex, "end_turn_prediction", "refresh");
		}
	}

	public static void Refresh()
	{
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		if (_isActionDamageOverrideActive)
		{
			EndTurnPredictionCreatureHoverTips.Clear();
			EndTurnButtonHoverTipHelper.HideHoverTips();
			return;
		}
		if (NCombatRoom.Instance == null || !EndTurnPrediction.ShouldPredict())
		{
			Cleanup();
			return;
		}
		if (IsRefreshDeferred())
		{
			_refreshPending = true;
			return;
		}
		_refreshPending = false;
		EndTurnPredictionResult endTurnPredictionResult;
		try
		{
			endTurnPredictionResult = EndTurnPrediction.Predict();
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"End-turn prediction refresh failed: {ex}", 1);
			ModTelemetry.CaptureException(ex, "end_turn_prediction", "refresh");
			endTurnPredictionResult = null;
		}
		if ((object)endTurnPredictionResult != null)
		{
			DamagePrediction damagePrediction = endTurnPredictionResult.DamagePrediction;
			if ((object)damagePrediction != null && damagePrediction.HasTargets)
			{
				EndTurnPredictionCreatureHoverTips.Set(endTurnPredictionResult.DamagePrediction, endTurnPredictionResult.Risk);
				ModSettings settings = ModData.Settings;
				if (ShouldShow(settings.EndTurnPredictionDisplayMode))
				{
					CombatPredictionOverlay.Show(endTurnPredictionResult.DamagePrediction, endTurnPredictionResult.Risk, EndTurnPredictionCreatureHoverTips.GetHoverTips);
				}
				else
				{
					CombatPredictionOverlay.Clear();
				}
				if (ShouldShow(settings.EndTurnHealthBarForecastDisplayMode))
				{
					DamagePredictionHealthBarForecast.Set(endTurnPredictionResult.DamagePrediction);
				}
				else
				{
					DamagePredictionHealthBarForecast.Clear();
				}
				if (settings.EndTurnPredictionDisplayMode == EndTurnPredictionDisplayMode.EndTurnButtonHover && _focusedEndTurnButton != null)
				{
					NEndTurnButton? focusedEndTurnButton = _focusedEndTurnButton;
					List<IHoverTip> list = new List<IHoverTip>();
					list.Add((IHoverTip)(object)PredictionHoverTipFactory.Text("end_turn_prediction_indicator"));
					list.AddRange(endTurnPredictionResult.Risk.ToHoverTips());
					EndTurnButtonHoverTipHelper.ShowHoverTips(focusedEndTurnButton, new _003C_003Ez__ReadOnlyList<IHoverTip>(list));
				}
				else
				{
					EndTurnButtonHoverTipHelper.HideHoverTips();
				}
				return;
			}
		}
		Clear();
	}

	public static void Clear()
	{
		_refreshPending = false;
		EndTurnPredictionCreatureHoverTips.Clear();
		CombatPredictionOverlay.Clear();
		DamagePredictionHealthBarForecast.Clear();
		EndTurnButtonHoverTipHelper.HideHoverTips();
	}

	public static void Cleanup()
	{
		_focusedEndTurnButton = null;
		Clear();
	}

	public static void SetActionDamageOverride(bool active)
	{
		bool isActionDamageOverrideActive = _isActionDamageOverrideActive;
		_isActionDamageOverrideActive = active;
		if (isActionDamageOverrideActive && !active)
		{
			RefreshSafely();
		}
	}

	private static bool ShouldShow(EndTurnPredictionDisplayMode displayMode)
	{
		if (displayMode == EndTurnPredictionDisplayMode.EndTurnButtonHover)
		{
			return _focusedEndTurnButton != null;
		}
		return true;
	}

	private static bool IsRefreshDeferred()
	{
		GameAction currentlyRunningAction = RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
		if (currentlyRunningAction != null)
		{
			return ActionQueueSet.IsGameActionPlayerDriven(currentlyRunningAction);
		}
		return false;
	}

	private static void OnActionExecuted(GameAction _)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (_refreshPending && !_isDeferredRefreshScheduled)
		{
			_isDeferredRefreshScheduled = true;
			Callable val = Callable.From((Action)FlushPendingRefresh);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	private static void FlushPendingRefresh()
	{
		_isDeferredRefreshScheduled = false;
		if (_isSubscribed && _refreshPending)
		{
			RefreshSafely();
		}
	}

	private static void OnAboutToSwitchToEnemyTurn(CombatState _)
	{
		Clear();
	}

	private static void OnPlayerEndedTurn(Player _, bool __)
	{
		RefreshSafely();
	}

	private static void OnPlayerUnendedTurn(Player _)
	{
		RefreshSafely();
	}

	private static void OnCombatEnded(CombatRoom _)
	{
		Cleanup();
	}

	private static void OnCombatStateChanged(CombatState _)
	{
		RefreshSafely();
	}

	private static void OnSettingsValueWritten(IModSettingsBinding binding)
	{
		if (binding == SettingsUiBindings.DamagePredictionHealthBarColor)
		{
			DamagePredictionHealthBarForecast.RefreshActiveForecasts();
		}
		else if (IsEndTurnPredictionRefreshBinding(binding))
		{
			RefreshSafely();
		}
	}

	private static bool IsEndTurnPredictionRefreshBinding(IModSettingsBinding binding)
	{
		if (binding != SettingsUiBindings.SingleplayerPredictionEnabled && binding != SettingsUiBindings.MultiplayerPredictionEnabled && binding != SettingsUiBindings.CombatDamagePredictionEnabled && binding != SettingsUiBindings.RandomTargetAttackPredictionEnabled && binding != SettingsUiBindings.OrbDamagePredictionEnabled && binding != SettingsUiBindings.EndTurnPredictionEnabled && binding != SettingsUiBindings.EndTurnPredictionDisplayMode)
		{
			return binding == SettingsUiBindings.EndTurnHealthBarForecastDisplayMode;
		}
		return true;
	}
}
