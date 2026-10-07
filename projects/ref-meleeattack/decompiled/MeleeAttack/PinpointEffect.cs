using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class PinpointEffect
{
	[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
	private static class PinpointSelectPatch
	{
		private static void Postfix(NHandCardHolder holder, bool startedViaShortcut)
		{
			if (!SettingsUI.IsPinpointEnabled())
			{
				_isPinpointSelected = false;
				RadialBlurTrigger.Stop("Pinpoint");
				return;
			}
			CardModel val = ((holder != null) ? ((NCardHolder)holder).CardModel : null);
			int num;
			if (val != null)
			{
				ModelId id = ((AbstractModel)val).Id;
				if (((id != null) ? id.Entry : null) != null)
				{
					num = (((AbstractModel)val).Id.Entry.Equals("PINPOINT", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
					goto IL_0061;
				}
			}
			num = 0;
			goto IL_0061;
			IL_0061:
			bool flag = (byte)num != 0;
			if (flag && !_isPinpointSelected)
			{
				if (!RadialBlur.IsActive)
				{
					RadialBlurTrigger.Trigger(0.03f, 0f, "Pinpoint");
				}
				ForceHideNativeReticle();
				if (_currentTarget != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentTarget))
				{
					ShowInternal(_currentTarget);
				}
				else
				{
					NCreature firstAliveEnemy = GetFirstAliveEnemy();
					if (firstAliveEnemy != null)
					{
						ShowInternal(firstAliveEnemy);
					}
				}
			}
			else if (!flag && _isPinpointSelected)
			{
				RadialBlurTrigger.Stop("Pinpoint");
			}
			_isPinpointSelected = flag;
		}
	}

	[HarmonyPatch(typeof(NCardPlay), "CancelPlayCard")]
	private static class CancelPlayPatch
	{
		private static void Postfix()
		{
			if (SettingsUI.IsPinpointEnabled())
			{
				_isPinpointSelected = false;
				Hide();
				RadialBlurTrigger.Stop("Pinpoint");
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class PinpointTriggerPatch
	{
		private static void Prefix(CardModel __instance)
		{
			if (!SettingsUI.IsPinpointEnabled())
			{
				_isPinpointBeingPlayed = false;
			}
			else if (__instance != null)
			{
				ModelId id = ((AbstractModel)__instance).Id;
				if (((id != null) ? id.Entry : null) != null && ((AbstractModel)__instance).Id.Entry.Equals("PINPOINT", StringComparison.OrdinalIgnoreCase) && ShouldTrigger(__instance))
				{
					_isPinpointBeingPlayed = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCardPlay), "OnCreatureHover")]
	private static class PatchOnCreatureHover
	{
		private static void Postfix(NCardPlay __instance, NCreature __0)
		{
			if (!SettingsUI.IsPinpointEnabled())
			{
				return;
			}
			if (__0 != null)
			{
				Creature entity = __0.Entity;
				if (entity == null || entity.IsAlive)
				{
					if (_isPinpointSelected)
					{
						ShowInternal(__0);
						return;
					}
					bool flag = false;
					try
					{
						PropertyInfo property = ((object)__instance).GetType().GetProperty("Card");
						if (property == null)
						{
							property = ((object)__instance).GetType().GetProperty("CardModel");
						}
						if (property != null)
						{
							object? value = property.GetValue(__instance);
							CardModel val = (CardModel)((value is CardModel) ? value : null);
							bool? obj;
							if (val == null)
							{
								obj = null;
							}
							else
							{
								ModelId id = ((AbstractModel)val).Id;
								obj = ((id == null) ? null : id.Entry?.Equals("PINPOINT", StringComparison.OrdinalIgnoreCase));
							}
							bool? flag2 = obj;
							flag = flag2.GetValueOrDefault();
						}
					}
					catch
					{
					}
					if (flag)
					{
						ShowInternal(__0);
					}
					else
					{
						Hide();
					}
					return;
				}
			}
			Hide();
		}
	}

	[HarmonyPatch(typeof(NCardPlay), "OnCreatureUnhover")]
	private static class PatchOnCreatureUnhover
	{
		private static void Postfix()
		{
			if (SettingsUI.IsPinpointEnabled())
			{
				Hide();
			}
		}
	}

	[HarmonyPatch(typeof(NCardPlay), "HideTargetingVisuals")]
	private static class HideTargetingPatch
	{
		private static void Postfix()
		{
			if (SettingsUI.IsPinpointEnabled())
			{
				bool isPinpointBeingPlayed = _isPinpointBeingPlayed;
				Hide(isPinpointBeingPlayed);
				_isPinpointBeingPlayed = false;
				_isPinpointSelected = false;
				RadialBlurTrigger.Stop("Pinpoint");
			}
		}
	}

	[HarmonyPatch(typeof(NHealthBar), "RefreshValues")]
	private static class PatchHealthBarRefresh
	{
		private static void Postfix(NHealthBar __instance)
		{
			if (!SettingsUI.IsPinpointEnabled() || !_isPinpointBeingPlayed || _currentTarget == null)
			{
				return;
			}
			try
			{
				if (_healthBarCreatureField == null)
				{
					_healthBarCreatureField = typeof(NHealthBar).GetField("_creature", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				object? obj = _healthBarCreatureField?.GetValue(__instance);
				Creature val = (Creature)((obj is Creature) ? obj : null);
				if (val != null && val == _currentTarget.Entity)
				{
					_isPinpointBeingPlayed = false;
					_isPinpointSelected = false;
					HideImmediate();
					_currentTarget = null;
				}
			}
			catch
			{
			}
		}
	}

	private const float OFFSET_Y = 0f;

	private const float BIG_RADIUS = 250f;

	private const float MID_RADIUS = 80f;

	private const float SMALL_RADIUS = 15f;

	private const float CENTER_DOT_RADIUS = 12f;

	private const float LINE_WIDTH = 5f;

	private const float TICK_LENGTH = 12f;

	private const float TICK_INTERVAL = 30f;

	private const float CIRCLE_SEGMENTS = 64f;

	private const float FADE_OUT_DURATION = 0.35f;

	private const int CANVAS_LAYER = 10;

	private const float ANIMATION_DURATION = 0.25f;

	private const float START_SCALE = 0.15f;

	private const float START_ROTATION_DEG = -20f;

	private const float RECOIL_DELAY = 0.1f;

	private const float RECOIL_OFFSET_Y = -100f;

	private const float RECOIL_DURATION = 0.2f;

	private const float UP_RATIO = 0.6f;

	private const float RESTORE_RATIO = 0.4f;

	private const float RECOIL_FADE_DURATION = 0.35f;

	private const int TARGET_Z_INDEX_BOOST = 50;

	private const int TARGET_Z_INDEX_RESTORE = 0;

	private const float DRIFT_RADIUS = 10f;

	private const float DRIFT_MOVE_DURATION = 2f;

	private const float WHITE_FLASH_DURATION = 0.1f;

	private const float BLUR_STRENGTH = 0.03f;

	private const string PINPOINT_SCENE_PATH = "res://scenes/精准瞄准.tscn";

	private const float PINPOINT_SCENE_HOLD_DURATION = 0.1f;

	private const float PINPOINT_SCENE_FADE_DURATION = 0.2f;

	private static readonly Color BIG_COLOR = Colors.White;

	private static readonly Color MID_COLOR = Colors.Yellow;

	private static readonly Color SMALL_COLOR = Colors.Red;

	private static readonly Color DOT_COLOR = Colors.Red;

	private static readonly Color CROSSHAIR_COLOR = Colors.Red;

	private static Texture2D _dotTexture;

	private static readonly Random _rand = new Random();

	private static bool _isPinpointSelected = false;

	private static bool _isPinpointBeingPlayed = false;

	private static CanvasLayer _currentCanvas;

	private static Node2D _currentContainer;

	private static bool _isShowing = false;

	private static bool _isHiding = false;

	private static NCreature _currentTarget;

	private static Vector2 _baseTargetPos;

	private static NCreature _targetNodeForRestore;

	private static bool _targetZIndexSaved = false;

	private static NSelectionReticle _currentReticle;

	private static bool _originalReticleVisible;

	private static Tween _showTween;

	private static Tween _hideFadeTween;

	private static Tween _recoilMoveTween;

	private static Tween _recoilResetTween;

	private static Tween _driftTween;

	private static FieldInfo _healthBarCreatureField;

	private static readonly Dictionary<CardModel, float> _recentlyTriggered = new Dictionary<CardModel, float>();

	private static readonly object _dedupLock = new object();

	private static bool ShouldTrigger(CardModel card)
	{
		if (card == null)
		{
			return false;
		}
		float num = (float)((double)Time.GetTicksMsec() / 1000.0);
		lock (_dedupLock)
		{
			if (_recentlyTriggered.TryGetValue(card, out var value) && num - value < 0.2f)
			{
				return false;
			}
			_recentlyTriggered[card] = num;
			if (_recentlyTriggered.Count > 32)
			{
				List<CardModel> list = new List<CardModel>();
				foreach (KeyValuePair<CardModel, float> item in _recentlyTriggered)
				{
					if (num - item.Value > 1f)
					{
						list.Add(item.Key);
					}
				}
				foreach (CardModel item2 in list)
				{
					_recentlyTriggered.Remove(item2);
				}
			}
			return true;
		}
	}

	private static void ForceHideNativeReticle()
	{
		if (_currentTarget != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentTarget))
		{
			HideNativeReticle(_currentTarget);
			return;
		}
		NCreature firstAliveEnemy = GetFirstAliveEnemy();
		if (firstAliveEnemy != null)
		{
			HideNativeReticle(firstAliveEnemy);
		}
	}

	private static void ShowInternal(NCreature targetNode)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		if (!SettingsUI.IsPinpointEnabled() || targetNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)targetNode))
		{
			return;
		}
		HideNativeReticle(targetNode);
		Vector2 center = TargetCenter.GetCenter(targetNode);
		if (_isShowing && _currentContainer != null && _currentTarget == targetNode)
		{
			_baseTargetPos = center;
			_currentContainer.GlobalPosition = center;
			BoostTargetLayer(targetNode);
			if (_driftTween == null)
			{
				StartDrift();
			}
			return;
		}
		if (_isHiding)
		{
			CleanupImmediate();
		}
		if (_isShowing && _currentContainer != null)
		{
			CleanupImmediate();
		}
		_currentTarget = targetNode;
		_baseTargetPos = center;
		CreateInstance(center);
		BoostTargetLayer(targetNode);
		if (!RadialBlur.IsActive)
		{
			RadialBlurTrigger.Trigger(0.03f, 0f, "Pinpoint");
		}
		StartDrift();
	}

	private static void BoostTargetLayer(NCreature targetNode)
	{
		if (targetNode != null)
		{
			_targetNodeForRestore = targetNode;
			_targetZIndexSaved = true;
			((CanvasItem)targetNode).ZIndex = 50;
			Node2D body = targetNode.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				((CanvasItem)body).ZIndex = 50;
			}
		}
	}

	private static void RestoreTargetLayer()
	{
		if (!_targetZIndexSaved)
		{
			return;
		}
		if (_targetNodeForRestore != null && GodotObject.IsInstanceValid((GodotObject)(object)_targetNodeForRestore))
		{
			((CanvasItem)_targetNodeForRestore).ZIndex = 0;
			Node2D body = _targetNodeForRestore.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				((CanvasItem)body).ZIndex = 0;
			}
		}
		_targetZIndexSaved = false;
		_targetNodeForRestore = null;
	}

	public static void Hide(bool playRecoil = false)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		if (!SettingsUI.IsPinpointEnabled())
		{
			return;
		}
		StopDrift();
		RestoreTargetLayer();
		RadialBlurTrigger.Stop("Pinpoint");
		RestoreNativeReticle();
		if (!_isShowing || _currentContainer == null || _currentCanvas == null || !GodotObject.IsInstanceValid((GodotObject)(object)_currentContainer))
		{
			_isHiding = false;
		}
		else
		{
			if (!playRecoil && _isHiding)
			{
				return;
			}
			KillFadeTweens();
			_isHiding = true;
			Vector2 originalPos = _currentContainer.Position;
			if (playRecoil)
			{
				DoWhiteFlash(0.1f, 0.1f);
				float num = 0.120000005f;
				_recoilMoveTween = ((Node)_currentContainer).CreateTween();
				_recoilMoveTween.TweenProperty((GodotObject)(object)_currentContainer, NodePath.op_Implicit("position:y"), Variant.op_Implicit(originalPos.Y + -100f), (double)num).SetDelay(0.10000000149011612).SetTrans((TransitionType)4)
					.SetEase((EaseType)1);
				_recoilMoveTween.Finished += delegate
				{
					//IL_006e: Unknown result type (might be due to invalid IL or missing references)
					//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
					if (_currentContainer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_currentContainer))
					{
						CleanupImmediate();
						_isHiding = false;
					}
					else
					{
						float num2 = 0.080000006f;
						_recoilResetTween = ((Node)_currentContainer).CreateTween();
						_recoilResetTween.SetParallel(true);
						_recoilResetTween.TweenProperty((GodotObject)(object)_currentContainer, NodePath.op_Implicit("position:y"), Variant.op_Implicit(originalPos.Y), (double)num2).SetTrans((TransitionType)4).SetEase((EaseType)0);
						_recoilResetTween.TweenProperty((GodotObject)(object)_currentContainer, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.3499999940395355).SetTrans((TransitionType)0).SetEase((EaseType)0);
						_recoilResetTween.Finished += delegate
						{
							CleanupImmediate();
							_isHiding = false;
						};
					}
				};
			}
			else
			{
				_hideFadeTween = ((Node)_currentContainer).CreateTween();
				_hideFadeTween.TweenProperty((GodotObject)(object)_currentContainer, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.3499999940395355).SetTrans((TransitionType)0).SetEase((EaseType)0);
				_hideFadeTween.Finished += delegate
				{
					CleanupImmediate();
					_isHiding = false;
				};
			}
		}
	}

	public static void HideImmediate()
	{
		if (SettingsUI.IsPinpointEnabled())
		{
			StopDrift();
			RestoreTargetLayer();
			RadialBlurTrigger.Stop("Pinpoint");
			RestoreNativeReticle();
			KillFadeTweens();
			CleanupImmediate();
		}
	}

	private static async Task DoWhiteFlash(float duration, float delay = 0f)
	{
		if (delay > 0f)
		{
			await Task.Delay((int)(delay * 1000f));
		}
		try
		{
			NCombatRoom room = NCombatRoom.Instance;
			if (room == null)
			{
				return;
			}
			Rect2 viewportRect = ((CanvasItem)room).GetViewportRect();
			PlayPinpointScene(room, viewportRect);
			ColorRect flash = new ColorRect
			{
				Color = Colors.White,
				Size = ((Rect2)(ref viewportRect)).Size * 2f,
				ZIndex = 1000,
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)flash).GlobalPosition = -((Rect2)(ref viewportRect)).Size * 0.5f;
			((CanvasItem)flash).Modulate = new Color(1f, 1f, 1f, 0f);
			((Node)room.CombatVfxContainer).AddChild((Node)(object)flash, false, (InternalMode)0);
			Tween tween = ((Node)flash).CreateTween();
			tween.TweenProperty((GodotObject)(object)flash, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), (double)(duration * 0.3f)).SetTrans((TransitionType)0).SetEase((EaseType)1);
			tween.TweenProperty((GodotObject)(object)flash, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)(duration * 0.7f)).SetTrans((TransitionType)0).SetEase((EaseType)0);
			tween.Finished += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)flash))
				{
					((Node)flash).QueueFree();
				}
			};
			viewportRect = default(Rect2);
		}
		catch (Exception e)
		{
			GD.PrintErr("[PinpointEffect] 白闪失败: " + e.Message);
		}
	}

	private static void PlayPinpointScene(NCombatRoom room, Rect2 viewportRect)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			PackedScene val = GD.Load<PackedScene>("res://scenes/精准瞄准.tscn");
			if (val == null)
			{
				GD.PrintErr("[PinpointEffect] 无法加载 res://scenes/精准瞄准.tscn");
				return;
			}
			Node2D instance = val.Instantiate<Node2D>((GenEditState)0);
			if (instance == null)
			{
				return;
			}
			((Node)room.CombatVfxContainer).AddChild((Node)(object)instance, false, (InternalMode)0);
			Vector2 globalPosition = ((_currentContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentContainer)) ? _currentContainer.GlobalPosition : (((Rect2)(ref viewportRect)).Position + ((Rect2)(ref viewportRect)).Size * 0.5f));
			instance.GlobalPosition = globalPosition;
			((CanvasItem)instance).ZIndex = 1000;
			((CanvasItem)instance).ZAsRelative = false;
			Tween val2 = ((Node)instance).CreateTween();
			val2.TweenInterval(0.10000000149011612);
			val2.TweenProperty((GodotObject)(object)instance, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.20000000298023224).SetTrans((TransitionType)0).SetEase((EaseType)0);
			val2.Finished += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)instance))
				{
					((Node)instance).QueueFree();
				}
			};
		}
		catch (Exception ex)
		{
			GD.PrintErr("[PinpointEffect] 精准瞄准场景播放失败: " + ex.Message);
		}
	}

	private static void KillFadeTweens()
	{
		if (_showTween != null && GodotObject.IsInstanceValid((GodotObject)(object)_showTween))
		{
			_showTween.Kill();
		}
		if (_hideFadeTween != null && GodotObject.IsInstanceValid((GodotObject)(object)_hideFadeTween))
		{
			_hideFadeTween.Kill();
		}
		if (_recoilMoveTween != null && GodotObject.IsInstanceValid((GodotObject)(object)_recoilMoveTween))
		{
			_recoilMoveTween.Kill();
		}
		if (_recoilResetTween != null && GodotObject.IsInstanceValid((GodotObject)(object)_recoilResetTween))
		{
			_recoilResetTween.Kill();
		}
		_showTween = null;
		_hideFadeTween = null;
		_recoilMoveTween = null;
		_recoilResetTween = null;
	}

	private static void CreateInstance(Vector2 targetPos)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		CleanupImmediate();
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			CanvasLayer val = new CanvasLayer
			{
				Name = StringName.op_Implicit("PinpointEffectCanvas"),
				Layer = 10
			};
			Node2D val2 = new Node2D
			{
				Name = StringName.op_Implicit("PinpointEffectContainer"),
				GlobalPosition = targetPos,
				Modulate = new Color(1f, 1f, 1f, 0f),
				Scale = Vector2.One * 0.15f,
				RotationDegrees = -20f
			};
			AddCrosshair(val2, 250f, 15f, 2.5f, CROSSHAIR_COLOR);
			AddTicks(val2, 15f, 250f, 30f, 12f, 1.5f, CROSSHAIR_COLOR);
			AddCircle(val2, 250f, 5f, BIG_COLOR);
			AddCircle(val2, 80f, 3.5f, MID_COLOR);
			AddGlowInner(val2, 80f, 20f, 0.3f);
			AddGlowOuter(val2, 80f, 20f, 0.3f);
			AddCircle(val2, 15f, 3f, SMALL_COLOR);
			AddSolidDot(val2, 12f, DOT_COLOR);
			((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
			((Node)instance).AddChild((Node)(object)val, false, (InternalMode)0);
			_showTween = ((Node)val2).CreateTween();
			_showTween.SetParallel(true);
			_showTween.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 0.25).SetTrans((TransitionType)0).SetEase((EaseType)1);
			_showTween.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One), 0.25).SetTrans((TransitionType)10).SetEase((EaseType)1);
			_showTween.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("rotation_degrees"), Variant.op_Implicit(0f), 0.25).SetTrans((TransitionType)4).SetEase((EaseType)1);
			_currentCanvas = val;
			_currentContainer = val2;
			_isShowing = true;
			_isHiding = false;
		}
	}

	private static void CleanupImmediate()
	{
		StopDrift();
		RestoreTargetLayer();
		if (_currentContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentContainer))
		{
			((Node)_currentContainer).QueueFree();
		}
		if (_currentCanvas != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentCanvas))
		{
			((Node)_currentCanvas).QueueFree();
		}
		_currentCanvas = null;
		_currentContainer = null;
		_isShowing = false;
		_isHiding = false;
		_currentTarget = null;
		KillFadeTweens();
	}

	private static void StartDrift()
	{
		StopDrift();
		if (_currentContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentContainer))
		{
			ScheduleNextDrift();
		}
	}

	private static void ScheduleNextDrift()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (_currentContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentContainer))
		{
			float num = (float)(_rand.NextDouble() * 2.0 * Math.PI);
			float num2 = (float)(_rand.NextDouble() * 10.0);
			Vector2 val = _baseTargetPos + new Vector2(Mathf.Cos(num) * num2, Mathf.Sin(num) * num2);
			_driftTween = ((Node)_currentContainer).CreateTween();
			_driftTween.SetTrans((TransitionType)0);
			_driftTween.TweenProperty((GodotObject)(object)_currentContainer, NodePath.op_Implicit("global_position"), Variant.op_Implicit(val), 2.0);
			_driftTween.Finished += delegate
			{
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0021: Unknown result type (might be due to invalid IL or missing references)
				Callable val2 = Callable.From((Action)ScheduleNextDrift);
				((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
			};
		}
	}

	private static void StopDrift()
	{
		if (_driftTween != null && GodotObject.IsInstanceValid((GodotObject)(object)_driftTween))
		{
			_driftTween.Kill();
		}
		_driftTween = null;
	}

	private static void HideNativeReticle(NCreature creature)
	{
		RestoreNativeReticle();
		NSelectionReticle val = ((creature != null) ? ((Node)creature).GetNodeOrNull<NSelectionReticle>(NodePath.op_Implicit("%SelectionReticle")) : null);
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			_currentReticle = val;
			_originalReticleVisible = ((CanvasItem)val).Visible;
			((CanvasItem)val).Visible = false;
		}
	}

	private static void RestoreNativeReticle()
	{
		if (_currentReticle != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentReticle))
		{
			((CanvasItem)_currentReticle).Visible = _originalReticleVisible;
		}
		_currentReticle = null;
	}

	private static void AddCircle(Node2D parent, float radius, float width, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		Line2D val = new Line2D
		{
			Width = width,
			DefaultColor = color,
			Antialiased = true,
			ZIndex = 0,
			ZAsRelative = true,
			Points = GenerateCirclePoints(radius, 64)
		};
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	private static void AddGlowInner(Node2D parent, float startRadius, float range, float alpha)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 7; i++)
		{
			float num = (float)i / 6f;
			float radius = startRadius - num * range;
			float num2 = alpha * (1f - num);
			float width = 2f + 2f * (1f - num);
			AddCircle(parent, radius, width, new Color(1f, 0f, 0f, num2));
		}
	}

	private static void AddGlowOuter(Node2D parent, float startRadius, float range, float alpha)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 7; i++)
		{
			float num = (float)i / 6f;
			float radius = startRadius + num * range;
			float num2 = alpha * (1f - num);
			float width = 2f + 2f * (1f - num);
			AddCircle(parent, radius, width, new Color(1f, 0f, 0f, num2));
		}
	}

	private static void AddSolidDot(Node2D parent, float radius, Color color)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (_dotTexture == null)
		{
			_dotTexture = CreateCircleTexture(8, Colors.White);
		}
		Sprite2D val = new Sprite2D
		{
			Texture = _dotTexture,
			Modulate = color,
			Scale = Vector2.One * (radius / 8f),
			Centered = true,
			ZIndex = 0,
			ZAsRelative = true
		};
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	private static Texture2D CreateCircleTexture(int size, Color color)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		Image val = Image.Create(size, size, false, (Format)5);
		val.Fill(Colors.Transparent);
		float num = (float)size / 2f;
		float num2 = (float)size / 2f;
		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				float num3 = (float)i - num;
				float num4 = (float)j - num;
				if (num3 * num3 + num4 * num4 <= num2 * num2)
				{
					val.SetPixel(i, j, color);
				}
			}
		}
		return (Texture2D)(object)ImageTexture.CreateFromImage(val);
	}

	private static void AddCrosshair(Node2D parent, float outerRadius, float innerRadius, float width, Color color)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = (Vector2[])(object)new Vector2[4]
		{
			Vector2.Left,
			Vector2.Right,
			Vector2.Up,
			Vector2.Down
		};
		Vector2[] array2 = array;
		foreach (Vector2 val in array2)
		{
			Line2D val2 = new Line2D();
			val2.Width = width;
			val2.DefaultColor = color;
			val2.Antialiased = true;
			((CanvasItem)val2).ZIndex = 0;
			((CanvasItem)val2).ZAsRelative = true;
			val2.Points = (Vector2[])(object)new Vector2[2]
			{
				val * innerRadius,
				val * outerRadius
			};
			Line2D val3 = val2;
			((Node)parent).AddChild((Node)(object)val3, false, (InternalMode)0);
		}
	}

	private static void AddTicks(Node2D parent, float innerRadius, float outerRadius, float interval, float tickLength, float width, Color color)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = (Vector2[])(object)new Vector2[4]
		{
			Vector2.Right,
			Vector2.Left,
			Vector2.Up,
			Vector2.Down
		};
		Vector2[] array2 = array;
		foreach (Vector2 val in array2)
		{
			float num = innerRadius + interval;
			float num2 = outerRadius - 5f;
			for (float num3 = num; num3 < num2; num3 += interval)
			{
				Vector2 val2 = val * num3;
				Vector2 val3 = ((val == Vector2.Right || val == Vector2.Left) ? Vector2.Up : Vector2.Right);
				Vector2 val4 = val3 * (tickLength / 2f);
				Line2D val5 = new Line2D();
				val5.Width = width;
				val5.DefaultColor = color;
				val5.Antialiased = true;
				((CanvasItem)val5).ZIndex = 0;
				((CanvasItem)val5).ZAsRelative = true;
				val5.Points = (Vector2[])(object)new Vector2[2]
				{
					val2 - val4,
					val2 + val4
				};
				Line2D val6 = val5;
				((Node)parent).AddChild((Node)(object)val6, false, (InternalMode)0);
			}
		}
	}

	private static Vector2[] GenerateCirclePoints(float radius, int segments)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = (Vector2[])(object)new Vector2[segments + 1];
		float num = (float)Math.PI * 2f / (float)segments;
		for (int i = 0; i < segments; i++)
		{
			float num2 = (float)i * num;
			array[i] = new Vector2(Mathf.Cos(num2) * radius, Mathf.Sin(num2) * radius);
		}
		array[segments] = array[0];
		return array;
	}

	private static NCreature GetFirstAliveEnemy()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return null;
		}
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (creatureNode != null)
			{
				Creature entity = creatureNode.Entity;
				if ((int)((entity != null) ? new CombatSide?(entity.Side) : null).GetValueOrDefault() == 2 && creatureNode.Entity.IsAlive)
				{
					return creatureNode;
				}
			}
		}
		return null;
	}
}
