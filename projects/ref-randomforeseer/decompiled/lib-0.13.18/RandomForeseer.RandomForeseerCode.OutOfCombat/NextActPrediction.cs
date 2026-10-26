using System;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.OutOfCombat.Nodes;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class NextActPrediction
{
	private sealed class NextActPredictionIcons
	{
		private readonly NNextActPredictionIcon _ancientIcon;

		private readonly NNextActPredictionIcon _bossIcon;

		public NTopBar TopBar { get; }

		public NextActPredictionIcons(NTopBar topBar)
		{
			TopBar = topBar;
			_ancientIcon = NNextActPredictionIcon.Create(NextActPredictionIconKind.Ancient);
			((Node)_ancientIcon).Name = StringName.op_Implicit("RandomForeseer_NextActPrediction_AncientIcon");
			((CanvasItem)_ancientIcon).Visible = false;
			_bossIcon = NNextActPredictionIcon.Create(NextActPredictionIconKind.Boss);
			((Node)_bossIcon).Name = StringName.op_Implicit("RandomForeseer_NextActPrediction_BossIcon");
			((CanvasItem)_bossIcon).Visible = false;
			Node parent = ((Node)topBar.FloorIcon).GetParent();
			GodotTreeExtensions.AddChildSafely(parent, (Node)(object)_ancientIcon);
			GodotTreeExtensions.AddChildSafely(parent, (Node)(object)_bossIcon);
			GodotTreeExtensions.MoveChildSafely(parent, (Node)(object)_ancientIcon, ((Node)topBar.FloorIcon).GetIndex(false) + 1);
			GodotTreeExtensions.MoveChildSafely(parent, (Node)(object)_bossIcon, ((Node)_ancientIcon).GetIndex(false) + 1);
			((Control)_ancientIcon).FocusNeighborTop = ((Node)_ancientIcon).GetPath();
			((Control)_bossIcon).FocusNeighborTop = ((Node)_bossIcon).GetPath();
		}

		public void ShowIcons(ActModel nextAct)
		{
			if (nextAct._rooms._ancient != null)
			{
				_ancientIcon.SetPrediction(nextAct);
				ShowIcon(_ancientIcon);
			}
			else
			{
				HideIcon(_ancientIcon);
			}
			if (nextAct._rooms._boss != null)
			{
				_bossIcon.SetPrediction(nextAct);
				ShowIcon(_bossIcon);
			}
			else
			{
				HideIcon(_bossIcon);
			}
			UpdateNavigation();
		}

		public void HideIcons()
		{
			HideIcon(_ancientIcon);
			HideIcon(_bossIcon);
			UpdateNavigation();
		}

		private static void ShowIcon(NNextActPredictionIcon icon)
		{
			((CanvasItem)icon).Visible = true;
			((Control)icon).FocusMode = (FocusModeEnum)2;
			((Control)icon).MouseFilter = (MouseFilterEnum)0;
		}

		private static void HideIcon(NNextActPredictionIcon icon)
		{
			((CanvasItem)icon).Visible = false;
			((Control)icon).FocusMode = (FocusModeEnum)0;
			((Control)icon).MouseFilter = (MouseFilterEnum)2;
		}

		private void UpdateNavigation()
		{
			if (!((CanvasItem)_ancientIcon).Visible && !((CanvasItem)_bossIcon).Visible)
			{
				((Control)TopBar.FloorIcon).FocusNeighborRight = ((Node)TopBar.BossIcon).GetPath();
				((Control)TopBar.BossIcon).FocusNeighborLeft = ((Node)TopBar.FloorIcon).GetPath();
				return;
			}
			NNextActPredictionIcon nNextActPredictionIcon = (((CanvasItem)_ancientIcon).Visible ? _ancientIcon : _bossIcon);
			NNextActPredictionIcon nNextActPredictionIcon2 = (((CanvasItem)_bossIcon).Visible ? _bossIcon : _ancientIcon);
			((Control)TopBar.FloorIcon).FocusNeighborRight = ((Node)nNextActPredictionIcon).GetPath();
			((Control)nNextActPredictionIcon).FocusNeighborLeft = ((Node)TopBar.FloorIcon).GetPath();
			if (((CanvasItem)_ancientIcon).Visible && ((CanvasItem)_bossIcon).Visible)
			{
				((Control)_ancientIcon).FocusNeighborRight = ((Node)_bossIcon).GetPath();
				((Control)_bossIcon).FocusNeighborLeft = ((Node)_ancientIcon).GetPath();
			}
			((Control)nNextActPredictionIcon2).FocusNeighborRight = (((CanvasItem)TopBar.BossIcon).IsVisible() ? ((Node)TopBar.BossIcon).GetPath() : ((Node)nNextActPredictionIcon2).GetPath());
			((Control)TopBar.BossIcon).FocusNeighborLeft = ((Node)nNextActPredictionIcon2).GetPath();
		}
	}

	private static NextActPredictionIcons? _icons;

	private static bool _isSubscribed;

	public static void Initialize(NTopBar topBar)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		NTopBar topBar2 = topBar;
		if (_icons?.TopBar != topBar2)
		{
			_icons = new NextActPredictionIcons(topBar2);
			((GodotObject)topBar2).Connect(SignalName.TreeExiting, Callable.From((Action)delegate
			{
				Release(topBar2);
			}), 0u);
			if (!_isSubscribed)
			{
				_isSubscribed = true;
				RunManager.Instance.RoomExited += Hide;
				RunManager.Instance.ActEntered += Hide;
			}
		}
	}

	public static void ShowIfEligible(bool isTerminal, IRunState runState)
	{
		if (_icons == null || !ShouldShow(isTerminal, runState))
		{
			Hide();
			return;
		}
		ActModel nextAct = runState.Acts[runState.CurrentActIndex + 1];
		_icons.ShowIcons(nextAct);
	}

	public static void Hide()
	{
		_icons?.HideIcons();
	}

	private static bool ShouldShow(bool isTerminal, IRunState runState)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		ModSettings settings = ModData.Settings;
		if (isTerminal && ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.NextActPredictionEnabled && runState.CurrentActIndex + 1 < runState.Acts.Count)
		{
			AbstractRoom currentRoom = runState.CurrentRoom;
			if (currentRoom != null && (int)currentRoom.RoomType == 3)
			{
				return IsActEndingBoss(runState);
			}
		}
		return false;
	}

	private static bool IsActEndingBoss(IRunState runState)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		MapCoord? currentMapCoord = runState.CurrentMapCoord;
		if (!currentMapCoord.HasValue)
		{
			return false;
		}
		MapPoint obj = runState.Map.SecondBossMapPoint ?? runState.Map.BossMapPoint;
		MapCoord? val = currentMapCoord;
		MapCoord coord = obj.coord;
		if (!val.HasValue)
		{
			return false;
		}
		return val.GetValueOrDefault() == coord;
	}

	private static void Release(NTopBar topBar)
	{
		if (_icons?.TopBar == topBar)
		{
			_icons = null;
		}
	}
}
