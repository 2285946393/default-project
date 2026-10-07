using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class BiasedCognitionEffects
{
	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class BiasedCognitionPatch
	{
		private static async void Prefix(CardModel __instance)
		{
			if (!SettingsUI.IsBiasedCognitionEnabled())
			{
				return;
			}
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				ModelId id2 = ((AbstractModel)__instance).Id;
				obj = ((id2 != null) ? id2.Entry : null);
			}
			string id = (string)obj;
			if (!string.IsNullOrEmpty(id) && id.IndexOf("BIASED_COGNITION", StringComparison.OrdinalIgnoreCase) >= 0 && ShouldTrigger(__instance))
			{
				Player owner = __instance.Owner;
				Creature player = ((owner != null) ? owner.Creature : null);
				if (player != null)
				{
					await ApplyFullEffectAsync(player);
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCreature), "_Ready")]
	private static class CreatureReadyPatch
	{
		private static void Postfix(NCreature __instance)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Invalid comparison between Unknown and I4
			if (__instance != null)
			{
				Creature entity = __instance.Entity;
				if ((int)((entity != null) ? new CombatSide?(entity.Side) : null).GetValueOrDefault() == 1)
				{
					return;
				}
			}
			ApplyMirrorToNode(__instance);
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatResetPatch
	{
		private static void Postfix()
		{
			ResetEffect();
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	[HarmonyPriority(600)]
	private static class BiasedTriggerAnimGatePatch
	{
		public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
		{
			if (_triggerGateBypass.Value)
			{
				return true;
			}
			if (!SettingsUI.IsBiasedCognitionEnabled())
			{
				return true;
			}
			if (!_flipInProgress)
			{
				return true;
			}
			if (creature == null || string.IsNullOrEmpty(triggerName))
			{
				return true;
			}
			if (!SimpleTeleportPatch.IsAttackTriggerName(triggerName))
			{
				return true;
			}
			__result = WaitThenReplay(creature, triggerName, waitTime);
			return false;
		}

		private static async Task WaitThenReplay(Creature creature, string triggerName, float waitTime)
		{
			try
			{
				await WaitForFlipCompleteAsync();
				_triggerGateBypass.Value = true;
				Task task = CreatureCmd.TriggerAnim(creature, triggerName, waitTime);
				if (task != null)
				{
					await task;
				}
			}
			finally
			{
				_triggerGateBypass.Value = false;
			}
		}
	}

	private static CanvasLayer _effectLayer = null;

	private static ColorRect _effectRect = null;

	private static ShaderMaterial _effectMaterial = null;

	private static CancellationTokenSource _effectCts = null;

	private static bool _effectActive = false;

	private static bool _isMirrored = false;

	private static readonly Dictionary<NCreature, (Vector2 pos, Vector2 visualScale)> _originalStates = new Dictionary<NCreature, (Vector2, Vector2)>();

	private static Creature _playerCreature = null;

	private static bool _blurScheduled = false;

	private const float EFFECT_DURATION = 0.6f;

	private const float BLUR_PEAK_STRENGTH = 0.12f;

	private static ColorRect _bgRect = null;

	private static ShaderMaterial _bgMaterial = null;

	private static CancellationTokenSource _bgCts = null;

	private const float BG_CYCLE_DURATION = 4f;

	private const float BG_MAX_ZOOM = 1.15f;

	private const float BG_MIN_ZOOM = 1f;

	private const float BG_MAX_ROTATION_DEG = 15f;

	private const float BG_MIN_ROTATION_DEG = -15f;

	private static volatile bool _flipInProgress = false;

	private static TaskCompletionSource<bool> _flipCompletionTcs = null;

	private static readonly object _flipLock = new object();

	private static readonly AsyncLocal<bool> _triggerGateBypass = new AsyncLocal<bool>();

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

	private static void BeginFlipBarrier()
	{
		lock (_flipLock)
		{
			_flipInProgress = true;
			_flipCompletionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		}
	}

	private static void EndFlipBarrier()
	{
		TaskCompletionSource<bool> flipCompletionTcs;
		lock (_flipLock)
		{
			_flipInProgress = false;
			flipCompletionTcs = _flipCompletionTcs;
			_flipCompletionTcs = null;
		}
		flipCompletionTcs?.TrySetResult(result: true);
	}

	private static async Task WaitForFlipCompleteAsync()
	{
		TaskCompletionSource<bool> tcs;
		lock (_flipLock)
		{
			if (!_flipInProgress || _flipCompletionTcs == null)
			{
				return;
			}
			tcs = _flipCompletionTcs;
		}
		await tcs.Task;
	}

	private static Vector2 GetScreenSize()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		Viewport val = ((instance != null) ? ((Node)instance).GetViewport() : null);
		_003F result;
		if (val == null)
		{
			result = new Vector2(1920f, 1080f);
		}
		else
		{
			Rect2 visibleRect = val.GetVisibleRect();
			result = ((Rect2)(ref visibleRect)).Size;
		}
		return (Vector2)result;
	}

	private static float GetScreenCenterX()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return 0f;
		}
		Viewport viewport = ((Node)instance).GetViewport();
		if (viewport == null)
		{
			return 0f;
		}
		Camera2D camera2D = viewport.GetCamera2D();
		if (camera2D != null)
		{
			return ((Node2D)camera2D).GlobalPosition.X;
		}
		Rect2 visibleRect = viewport.GetVisibleRect();
		return ((Rect2)(ref visibleRect)).Size.X / 2f;
	}

	private static void ApplyWorldMirror(bool mirror)
	{
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		List<NCreature> list = new List<NCreature>();
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (creatureNode != null)
			{
				Creature entity = creatureNode.Entity;
				if (((entity != null) ? new bool?(entity.IsAlive) : null).GetValueOrDefault())
				{
					list.Add(creatureNode);
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		float screenCenterX = GetScreenCenterX();
		if (screenCenterX == 0f)
		{
			return;
		}
		if (mirror)
		{
			_originalStates.Clear();
			Vector2 globalPosition = default(Vector2);
			foreach (NCreature item in list)
			{
				Node2D visualNode = SimpleTeleportPatch.GetVisualNode(item);
				if (visualNode != null)
				{
					_originalStates[item] = (((Control)item).GlobalPosition, visualNode.Scale);
					((Vector2)(ref globalPosition))._002Ector(2f * screenCenterX - ((Control)item).GlobalPosition.X, ((Control)item).GlobalPosition.Y);
					((Control)item).GlobalPosition = globalPosition;
					visualNode.Scale = new Vector2(0f - visualNode.Scale.X, visualNode.Scale.Y);
				}
			}
			FloatingHitEffect.OnWorldMirrored(screenCenterX);
			return;
		}
		foreach (KeyValuePair<NCreature, (Vector2, Vector2)> originalState in _originalStates)
		{
			NCreature key = originalState.Key;
			if (key != null && GodotObject.IsInstanceValid((GodotObject)(object)key))
			{
				var (globalPosition2, scale) = originalState.Value;
				((Control)key).GlobalPosition = globalPosition2;
				Node2D visualNode2 = SimpleTeleportPatch.GetVisualNode(key);
				if (visualNode2 != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode2))
				{
					visualNode2.Scale = scale;
				}
			}
		}
		_originalStates.Clear();
		FloatingHitEffect.OnWorldUnmirrored();
	}

	public static void ApplyMirrorToNode(NCreature node)
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !_isMirrored)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		float screenCenterX = GetScreenCenterX();
		if (screenCenterX == 0f)
		{
			return;
		}
		Node2D visualNode = SimpleTeleportPatch.GetVisualNode(node);
		if (visualNode != null)
		{
			if (!_originalStates.ContainsKey(node))
			{
				_originalStates[node] = (((Control)node).GlobalPosition, visualNode.Scale);
			}
			Vector2 globalPosition = default(Vector2);
			((Vector2)(ref globalPosition))._002Ector(2f * screenCenterX - ((Control)node).GlobalPosition.X, ((Control)node).GlobalPosition.Y);
			((Control)node).GlobalPosition = globalPosition;
			visualNode.Scale = new Vector2(0f - visualNode.Scale.X, visualNode.Scale.Y);
		}
	}

	private static async Task ApplyVisualEffectAsync(bool flipEnabled)
	{
		if (_effectActive)
		{
			return;
		}
		NCombatRoom room = NCombatRoom.Instance;
		if (room == null)
		{
			GD.PrintErr("[BiasedEffects] NCombatRoom.Instance 为空");
			return;
		}
		_effectLayer = new CanvasLayer
		{
			Layer = 1000,
			Name = StringName.op_Implicit("BiasedEffectLayer")
		};
		Shader shader = new Shader();
		shader.Code = "\r\n                shader_type canvas_item;\r\n                render_mode unshaded;\r\n                uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;\r\n                uniform float flip_x = 0.0;\r\n                uniform float blur_strength = 0.0;\r\n\r\n                void fragment() {\r\n                    vec2 uv = SCREEN_UV;\r\n                    uv.x = mix(uv.x, 1.0 - uv.x, flip_x);\r\n                    float total = 0.0;\r\n                    vec4 color = vec4(0.0);\r\n                    int samples = 16;\r\n                    for (int i = 0; i < samples; i++) {\r\n                        float t = float(i) / float(samples - 1) - 0.5;\r\n                        float offset = t * blur_strength;\r\n                        vec2 sample_uv = uv + vec2(offset, 0.0);\r\n                        color += texture(SCREEN_TEXTURE, sample_uv);\r\n                        total += 1.0;\r\n                    }\r\n                    color /= total;\r\n                    COLOR = color;\r\n                }\r\n            ";
		_effectMaterial = new ShaderMaterial
		{
			Shader = shader
		};
		_effectMaterial.SetShaderParameter(StringName.op_Implicit("flip_x"), Variant.op_Implicit(0f));
		_effectMaterial.SetShaderParameter(StringName.op_Implicit("blur_strength"), Variant.op_Implicit(0f));
		Vector2 screenSize = GetScreenSize();
		_effectRect = new ColorRect
		{
			Position = Vector2.Zero,
			Size = screenSize,
			Color = Colors.White,
			MouseFilter = (MouseFilterEnum)2,
			Material = (Material)(object)_effectMaterial
		};
		((Node)_effectLayer).AddChild((Node)(object)_effectRect, false, (InternalMode)0);
		((Node)room).AddChild((Node)(object)_effectLayer, false, (InternalMode)0);
		_effectActive = true;
		_effectCts?.Cancel();
		_effectCts = new CancellationTokenSource();
		try
		{
			await RunVisualAnimation(flipEnabled, _effectCts.Token);
		}
		finally
		{
			int num;
			if (num >= 0)
			{
			}
		}
	}

	private static async Task RunVisualAnimation(bool flipEnabled, CancellationToken token)
	{
		if (!_effectActive || _effectMaterial == null)
		{
			return;
		}
		float elapsed = 0f;
		for (float duration = 0.6f; elapsed < duration; elapsed += 0.016f)
		{
			if (!_effectActive)
			{
				break;
			}
			if (token.IsCancellationRequested)
			{
				break;
			}
			float progress = elapsed / duration;
			float blur2 = 0.12f * Mathf.Sin(progress * (float)Math.PI);
			blur2 = Mathf.Clamp(blur2, 0f, 0.12f);
			float flip = 0f;
			if (flipEnabled)
			{
				flip = (float)(0.5 - 0.5 * Math.Cos((double)progress * Math.PI));
			}
			_effectMaterial.SetShaderParameter(StringName.op_Implicit("flip_x"), Variant.op_Implicit(flip));
			_effectMaterial.SetShaderParameter(StringName.op_Implicit("blur_strength"), Variant.op_Implicit(blur2));
			await Task.Delay(16, token);
		}
		if (_effectMaterial != null && !token.IsCancellationRequested)
		{
			_effectMaterial.SetShaderParameter(StringName.op_Implicit("flip_x"), Variant.op_Implicit(0f));
			_effectMaterial.SetShaderParameter(StringName.op_Implicit("blur_strength"), Variant.op_Implicit(0f));
		}
	}

	private static void EffectCleanup()
	{
		if (_effectLayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_effectLayer))
		{
			((Node)_effectLayer).QueueFree();
			_effectLayer = null;
		}
		_effectRect = null;
		_effectMaterial = null;
		_effectActive = false;
		if (_effectCts != null)
		{
			_effectCts.Cancel();
			_effectCts = null;
		}
	}

	private static void StartBackgroundAnimation()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		StopBackgroundAnimation();
		if (SettingsUI.IsBiasedCognitionEnabled())
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				GD.PrintErr("[BiasedEffects] [BG] NCombatRoom.Instance 为空，无法启动背景动画");
				return;
			}
			Node val = (Node)(((object)instance.BackCombatVfxContainer) ?? ((object)instance));
			CanvasItem val2 = (CanvasItem)(((object)((val is CanvasItem) ? val : null)) ?? ((object)instance));
			Rect2 visibleRect = ((Node)instance).GetViewport().GetVisibleRect();
			Transform2D globalTransform = val2.GetGlobalTransform();
			Transform2D val3 = ((Transform2D)(ref globalTransform)).AffineInverse();
			Vector2 val4 = val3 * ((Rect2)(ref visibleRect)).Position;
			Vector2 val5 = val3 * (((Rect2)(ref visibleRect)).Position + ((Rect2)(ref visibleRect)).Size);
			Shader val6 = new Shader();
			val6.Code = "\r\n                shader_type canvas_item;\r\n                render_mode unshaded;\r\n                uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;\r\n                uniform float zoom = 1.0;\r\n                uniform float rotation = 0.0;\r\n                uniform vec2 center = vec2(0.5, 0.5);\r\n\r\n                void fragment() {\r\n                    vec2 uv = SCREEN_UV;\r\n                    vec2 dir = uv - center;\r\n                    float cos_a = cos(rotation);\r\n                    float sin_a = sin(rotation);\r\n                    vec2 rotated_dir = vec2(\r\n                        dir.x * cos_a - dir.y * sin_a,\r\n                        dir.x * sin_a + dir.y * cos_a\r\n                    );\r\n                    vec2 uv_zoom = center + rotated_dir / zoom;\r\n                    vec4 color = texture(SCREEN_TEXTURE, uv_zoom);\r\n                    COLOR = color;\r\n                }\r\n            ";
			_bgRect = new ColorRect
			{
				Position = val4,
				Size = val5 - val4,
				Color = Colors.White,
				MouseFilter = (MouseFilterEnum)2
			};
			_bgMaterial = new ShaderMaterial
			{
				Shader = val6
			};
			((CanvasItem)_bgRect).Material = (Material)(object)_bgMaterial;
			val.AddChild((Node)(object)_bgRect, false, (InternalMode)0);
			val.MoveChild((Node)(object)_bgRect, 0);
			_bgCts?.Cancel();
			_bgCts = new CancellationTokenSource();
			RunBackgroundAnimation(_bgCts.Token);
		}
	}

	private static async Task RunBackgroundAnimation(CancellationToken token)
	{
		try
		{
			float elapsed = 0f;
			while (!token.IsCancellationRequested)
			{
				float t = elapsed / 4f;
				float scaleFactor = 0.5f + 0.5f * Mathf.Sin(t * (float)Math.PI * 2f);
				float zoom = Mathf.Lerp(1f, 1.15f, scaleFactor);
				float rotFactor = 0.5f - 0.5f * Mathf.Cos(t * (float)Math.PI * 2f);
				float rotationDeg = Mathf.Lerp(-15f, 15f, rotFactor);
				float rotationRad = Mathf.DegToRad(rotationDeg);
				if (_bgMaterial != null && !token.IsCancellationRequested)
				{
					_bgMaterial.SetShaderParameter(StringName.op_Implicit("zoom"), Variant.op_Implicit(zoom));
					_bgMaterial.SetShaderParameter(StringName.op_Implicit("rotation"), Variant.op_Implicit(rotationRad));
				}
				else if (_bgMaterial == null)
				{
					break;
				}
				await Task.Delay(16, token);
				elapsed += 0.016f;
				if (elapsed >= 4f)
				{
					elapsed -= 4f;
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			GD.PrintErr("[BiasedEffects] [BG] 循环异常: " + ex.Message);
		}
		finally
		{
			if (!token.IsCancellationRequested && _bgRect != null)
			{
				StopBackgroundAnimation();
			}
		}
	}

	private static void StopBackgroundAnimation()
	{
		if (_bgCts != null)
		{
			_bgCts.Cancel();
			_bgCts = null;
		}
		if (_bgRect != null && GodotObject.IsInstanceValid((GodotObject)(object)_bgRect))
		{
			((Node)_bgRect).QueueFree();
			_bgRect = null;
		}
		_bgMaterial = null;
	}

	public static void PauseBackground()
	{
		if (_bgRect != null && GodotObject.IsInstanceValid((GodotObject)(object)_bgRect))
		{
			((CanvasItem)_bgRect).Visible = false;
		}
	}

	public static void ResumeBackground()
	{
		if (_bgRect != null && GodotObject.IsInstanceValid((GodotObject)(object)_bgRect))
		{
			((CanvasItem)_bgRect).Visible = true;
		}
	}

	public static async Task ApplyFullEffectAsync(Creature playerCreature)
	{
		if (SettingsUI.IsBiasedCognitionEnabled())
		{
			if (_effectActive)
			{
				EffectCleanup();
			}
			_playerCreature = playerCreature;
			BeginFlipBarrier();
			try
			{
				StartBackgroundAnimation();
				await ApplyVisualEffectAsync(flipEnabled: true);
				bool newMirrorState = !_isMirrored;
				_isMirrored = newMirrorState;
				ApplyWorldMirror(_isMirrored);
			}
			finally
			{
				EndFlipBarrier();
			}
			_blurScheduled = true;
			SimpleTeleportPatch.ClearAllSnapshots();
			if (_playerCreature != null)
			{
				AttackTimeTerminator.StartTurnEndMonitoring(_playerCreature);
			}
			EffectCleanup();
		}
	}

	public static async Task ApplyBlurOnlyAsync()
	{
		if (!SettingsUI.IsBiasedCognitionEnabled() || !_blurScheduled)
		{
			return;
		}
		_blurScheduled = false;
		if (_effectActive)
		{
			EffectCleanup();
		}
		BeginFlipBarrier();
		try
		{
			await ApplyVisualEffectAsync(flipEnabled: false);
			if (_isMirrored)
			{
				ApplyWorldMirror(mirror: false);
				_isMirrored = false;
			}
		}
		finally
		{
			EndFlipBarrier();
		}
		SimpleTeleportPatch.ClearAllSnapshots();
		EffectCleanup();
	}

	public static void ResetEffect()
	{
		if (_isMirrored)
		{
			ApplyWorldMirror(mirror: false);
			_isMirrored = false;
		}
		_blurScheduled = false;
		EffectCleanup();
		StopBackgroundAnimation();
		_originalStates.Clear();
		_playerCreature = null;
		EndFlipBarrier();
	}
}
