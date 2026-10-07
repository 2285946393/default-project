using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class BlackBarEffect
{
	private class BlackBarConfig
	{
		public bool Enabled { get; set; } = true;


		public int BarHeight { get; set; } = 200;


		public float SlideInDuration { get; set; } = 0.5f;


		public float HoldDuration { get; set; } = 1.7f;


		public float SlideOutDuration { get; set; } = 0.5f;

	}

	private static readonly string _modRootDir;

	private static readonly string _configPath;

	private static readonly BlackBarConfig _config;

	private static bool _isActive;

	private static readonly object _lock;

	private static TaskCompletionSource<bool> _holdTcs;

	private static bool _pendingEnd;

	private static float _slideOutDuration;

	private static int _instanceCounter;

	private static int _currentInstanceId;

	private static Control _handCardContainer;

	private static Control _endTurnButton;

	private static Control _energyCounterContainer;

	private static Color _handOriginalColor;

	private static Color _endTurnOriginalColor;

	private static Color _energyOriginalColor;

	private static readonly Color _halfTransparent;

	public static bool IsEnabled => _config.Enabled;

	public static bool IsWaiting
	{
		get
		{
			lock (_lock)
			{
				return _isActive && _holdTcs != null && !_holdTcs.Task.IsCompleted;
			}
		}
	}

	public static event Action FadeOutCompleted;

	static BlackBarEffect()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		_isActive = false;
		_lock = new object();
		_pendingEnd = false;
		_slideOutDuration = 0.3f;
		_instanceCounter = 0;
		_currentInstanceId = 0;
		_handCardContainer = null;
		_endTurnButton = null;
		_energyCounterContainer = null;
		_handOriginalColor = Colors.White;
		_endTurnOriginalColor = Colors.White;
		_energyOriginalColor = Colors.White;
		_halfTransparent = new Color(1f, 1f, 1f, 0.3f);
		string location = typeof(BlackBarEffect).Assembly.Location;
		_modRootDir = Path.GetDirectoryName(location) ?? ".";
		_configPath = Path.Combine(_modRootDir, "MeleeAttack.json");
		_config = LoadConfig() ?? new BlackBarConfig();
	}

	private static void InvokeFadeOutCompleted()
	{
		try
		{
			BlackBarEffect.FadeOutCompleted?.Invoke();
		}
		catch (Exception ex)
		{
			GD.PrintErr("[BlackBarEffect] FadeOutCompleted 回调异常: " + ex.Message);
		}
	}

	private static BlackBarConfig LoadConfig()
	{
		try
		{
			if (!File.Exists(_configPath))
			{
				return null;
			}
			string json = File.ReadAllText(_configPath);
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			if (jsonDocument.RootElement.TryGetProperty("BlackBars", out var value))
			{
				return JsonSerializer.Deserialize<BlackBarConfig>(value.GetRawText());
			}
			return null;
		}
		catch (Exception ex)
		{
			GD.PrintErr("[BlackBarEffect] 配置加载失败，使用默认值: " + ex.Message);
			return null;
		}
	}

	public static async Task PerformFullEffect(NCreature targetNode = null, float? slideInDuration = null, float? holdDuration = null, float? slideOutDuration = null)
	{
		if (!_config.Enabled)
		{
			return;
		}
		int myInstanceId;
		lock (_lock)
		{
			if (_isActive)
			{
				return;
			}
			_isActive = true;
			_holdTcs = null;
			_pendingEnd = false;
			_instanceCounter++;
			myInstanceId = (_currentInstanceId = _instanceCounter);
		}
		float finalSlideIn = slideInDuration ?? _config.SlideInDuration;
		_slideOutDuration = slideOutDuration ?? _config.SlideOutDuration;
		SetUIAlpha(_halfTransparent, myInstanceId);
		NCombatRoom instance = NCombatRoom.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			SceneTree tree = ((Node)instance).GetTree();
			obj = ((tree != null) ? tree.Root : null);
		}
		Window root = (Window)obj;
		if (root == null)
		{
			GD.PrintErr("[BlackBarEffect] root 为空，退出");
			SetUIAlpha(Colors.White, myInstanceId);
			lock (_lock)
			{
				_isActive = false;
			}
			InvokeFadeOutCompleted();
			return;
		}
		Vector2 screenSize = new Vector2(1920f, 1080f);
		Viewport viewport = ((Node)root).GetViewport();
		if (viewport != null)
		{
			Rect2 visibleRect = viewport.GetVisibleRect();
			screenSize = ((Rect2)(ref visibleRect)).Size;
		}
		CanvasLayer layer = new CanvasLayer();
		layer.Layer = 999;
		((Node)layer).Name = StringName.op_Implicit($"BlackBarLayer_{myInstanceId}");
		ColorRect topBar = new ColorRect();
		((Control)topBar).Size = new Vector2(screenSize.X, (float)_config.BarHeight);
		topBar.Color = Colors.Black;
		((Control)topBar).Position = new Vector2(0f, (float)(-_config.BarHeight));
		((CanvasItem)topBar).ZIndex = 100;
		((CanvasItem)topBar).ZAsRelative = false;
		((Control)topBar).MouseFilter = (MouseFilterEnum)2;
		((Node)layer).AddChild((Node)(object)topBar, false, (InternalMode)0);
		ColorRect bottomBar = new ColorRect();
		((Control)bottomBar).Size = new Vector2(screenSize.X, (float)_config.BarHeight);
		bottomBar.Color = Colors.Black;
		((Control)bottomBar).Position = new Vector2(0f, screenSize.Y);
		((CanvasItem)bottomBar).ZIndex = 100;
		((CanvasItem)bottomBar).ZAsRelative = false;
		((Control)bottomBar).MouseFilter = (MouseFilterEnum)2;
		((Node)layer).AddChild((Node)(object)bottomBar, false, (InternalMode)0);
		((Node)root).AddChild((Node)(object)layer, false, (InternalMode)0);
		TaskCompletionSource<bool> myHoldTcs = null;
		try
		{
			try
			{
				Tween tween = ((Node)layer).CreateTween();
				tween.SetParallel(true);
				tween.TweenProperty((GodotObject)(object)topBar, NodePath.op_Implicit("position:y"), Variant.op_Implicit(0), (double)finalSlideIn).SetTrans((TransitionType)4).SetEase((EaseType)1);
				tween.TweenProperty((GodotObject)(object)bottomBar, NodePath.op_Implicit("position:y"), Variant.op_Implicit(screenSize.Y - (float)_config.BarHeight), (double)finalSlideIn).SetTrans((TransitionType)4).SetEase((EaseType)1);
				await ((GodotObject)layer).ToSignal((GodotObject)(object)tween, SignalName.Finished);
				lock (_lock)
				{
					if (_currentInstanceId != myInstanceId)
					{
						return;
					}
				}
				lock (_lock)
				{
					myHoldTcs = (_holdTcs = new TaskCompletionSource<bool>());
					if (_pendingEnd)
					{
						_pendingEnd = false;
						Task.Run(() => EndBlackBar(_slideOutDuration));
					}
				}
				await myHoldTcs.Task;
				goto end_IL_03e9;
			}
			catch (Exception ex3)
			{
				Exception ex2 = ex3;
				GD.PrintErr($"[BlackBarEffect] 执行异常 (instanceId={myInstanceId}): {ex2.Message}");
				goto end_IL_03e9;
			}
			end_IL_03e9:;
		}
		finally
		{
			try
			{
				await CleanupLayer(layer, topBar, bottomBar, myInstanceId);
			}
			catch (Exception ex3)
			{
				Exception ex = ex3;
				GD.PrintErr($"[BlackBarEffect] CleanupLayer 异常 (instanceId={myInstanceId}): {ex.Message}");
			}
			SetUIAlpha(Colors.White, myInstanceId);
			lock (_lock)
			{
				if (_currentInstanceId == myInstanceId)
				{
					_isActive = false;
					_holdTcs = null;
				}
			}
		}
	}

	public static Task EndBlackBar(float? slideOutDuration = null)
	{
		TaskCompletionSource<bool> holdTcs;
		lock (_lock)
		{
			if (!_isActive)
			{
				return Task.CompletedTask;
			}
			if (slideOutDuration.HasValue)
			{
				_slideOutDuration = slideOutDuration.Value;
			}
			if (_holdTcs == null || _holdTcs.Task.IsCompleted)
			{
				_pendingEnd = true;
				return Task.CompletedTask;
			}
			holdTcs = _holdTcs;
		}
		holdTcs.TrySetResult(result: true);
		return Task.CompletedTask;
	}

	private static async Task CleanupLayer(CanvasLayer layer, ColorRect topBar, ColorRect bottomBar, int instanceId)
	{
		if (layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)layer))
		{
			InvokeFadeOutCompleted();
			return;
		}
		Vector2 screenSize = new Vector2(1920f, 1080f);
		Viewport vp = ((Node)layer).GetViewport();
		if (vp != null)
		{
			Rect2 visibleRect = vp.GetVisibleRect();
			screenSize = ((Rect2)(ref visibleRect)).Size;
		}
		float duration = _slideOutDuration;
		if (topBar != null && GodotObject.IsInstanceValid((GodotObject)(object)topBar) && bottomBar != null && GodotObject.IsInstanceValid((GodotObject)(object)bottomBar))
		{
			int barHeight = _config.BarHeight;
			Tween tween = ((Node)layer).CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty((GodotObject)(object)topBar, NodePath.op_Implicit("position:y"), Variant.op_Implicit(-barHeight), (double)duration).SetTrans((TransitionType)4).SetEase((EaseType)0);
			tween.TweenProperty((GodotObject)(object)bottomBar, NodePath.op_Implicit("position:y"), Variant.op_Implicit(screenSize.Y), (double)duration).SetTrans((TransitionType)4).SetEase((EaseType)0);
			await ((GodotObject)layer).ToSignal((GodotObject)(object)tween, SignalName.Finished);
		}
		if (GodotObject.IsInstanceValid((GodotObject)(object)layer))
		{
			((Node)layer).QueueFree();
		}
		InvokeFadeOutCompleted();
	}

	private static void SetUIAlpha(Color color, int instanceId)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (color == Colors.White)
			{
				RestoreUIElement(ref _handCardContainer, _handOriginalColor);
				RestoreUIElement(ref _endTurnButton, _endTurnOriginalColor);
				RestoreUIElement(ref _energyCounterContainer, _energyOriginalColor);
				return;
			}
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				return;
			}
			NCombatUi ui = instance.Ui;
			if (ui == null)
			{
				return;
			}
			NPlayerHand hand = ui.Hand;
			if (hand != null)
			{
				Control node = ((Node)hand).GetNode<Control>(NodePath.op_Implicit("CardHolderContainer"));
				if (node != null)
				{
					if (_handCardContainer == null)
					{
						_handCardContainer = node;
						_handOriginalColor = ((CanvasItem)node).Modulate;
					}
					((CanvasItem)node).Modulate = color;
				}
			}
			NEndTurnButton endTurnButton = ui.EndTurnButton;
			if (endTurnButton != null)
			{
				if (_endTurnButton == null)
				{
					_endTurnButton = (Control)(object)endTurnButton;
					_endTurnOriginalColor = ((CanvasItem)endTurnButton).Modulate;
				}
				((CanvasItem)endTurnButton).Modulate = color;
			}
			Control energyCounterContainer = ui.EnergyCounterContainer;
			if (energyCounterContainer != null)
			{
				if (_energyCounterContainer == null)
				{
					_energyCounterContainer = energyCounterContainer;
					_energyOriginalColor = ((CanvasItem)energyCounterContainer).Modulate;
				}
				((CanvasItem)energyCounterContainer).Modulate = color;
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[BlackBarEffect] 修改 UI 透明度异常: " + ex.Message);
		}
	}

	private static void RestoreUIElement(ref Control reference, Color originalColor)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (reference == null)
		{
			return;
		}
		if (GodotObject.IsInstanceValid((GodotObject)(object)reference))
		{
			try
			{
				((CanvasItem)reference).Modulate = originalColor;
			}
			catch (Exception ex)
			{
				GD.PrintErr("[BlackBarEffect] 恢复 UI 透明度失败: " + ex.Message);
			}
		}
		reference = null;
	}
}
