using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class RadialBlur
{
	private const int BLUR_SAMPLES = 24;

	private const float STRENGTH_MAX = 1f;

	private const float STRENGTH_MIN = 0f;

	private const float BASE_BLUR = 0.08f;

	private const float VIGNETTE = 0.3f;

	private const int PINPOINT_Z_INDEX = 1;

	private const int VFX_Z_INDEX = 1000;

	private const string PINPOINT_OWNER = "Pinpoint";

	private static ColorRect _blurRect;

	private static ShaderMaterial _material;

	private static CanvasLayer _blurLayer;

	private static bool _isActive;

	private static bool _fadingOut;

	private static bool _isShowing;

	private static CancellationTokenSource _activeCts;

	private static DateTime _lastShowTime = DateTime.MinValue;

	private static readonly TimeSpan _debounceSpan = TimeSpan.FromMilliseconds(200L, 0L);

	private static string _owner = null;

	private static readonly string ShaderCode = $"\r\nshader_type canvas_item;\r\nuniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;\r\nuniform float strength : hint_range(0.0, 1.0) = 0.0;\r\nuniform vec2 center = vec2(0.5, 0.5);\r\nuniform float intensity = 1.0;\r\nuniform float vignette_amount = {0.3f};\r\nuniform float base_blur : hint_range(0.0, 1.0) = {0.08f};\r\n\r\nconst int SAMPLES = {24};\r\n\r\nvoid fragment() {{\r\n    vec2 uv = SCREEN_UV;\r\n    vec2 offset = uv - center;\r\n    float dist = length(offset);\r\n\r\n    vec2 nDir = dist > 1e-5 ? offset / dist : vec2(1.0, 0.0);\r\n\r\n    float amount = strength * (dist + base_blur);\r\n    vec2 end_uv = uv - nDir * amount * intensity;\r\n\r\n    vec4 sum = vec4(0.0);\r\n    for (int i = 0; i < SAMPLES; i++) {{\r\n        float t = float(i) / float(SAMPLES - 1);\r\n        vec2 sample_uv = mix(uv, end_uv, t);\r\n        sample_uv = clamp(sample_uv, vec2(0.0), vec2(1.0));\r\n        sum += texture(screen_tex, sample_uv);\r\n    }}\r\n    vec4 result = sum / float(SAMPLES);\r\n\r\n    float v = 1.0 - dist * vignette_amount;\r\n    result.rgb *= v;\r\n\r\n    COLOR = result;\r\n}}\r\n";

	private static readonly Shader _sharedShader = new Shader
	{
		Code = ShaderCode
	};

	public static bool IsActive => _isActive;

	private static int GetZIndexForOwner(string owner)
	{
		if (string.Equals(owner, "Pinpoint", StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}
		return 1000;
	}

	public static void Show(float strength = 0.35f, float duration = 0f, string owner = null)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected O, but got Unknown
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Expected O, but got Unknown
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Expected O, but got Unknown
		if (DateTime.Now - _lastShowTime < _debounceSpan)
		{
			return;
		}
		_lastShowTime = DateTime.Now;
		if (_isActive || _isShowing)
		{
			return;
		}
		_isShowing = true;
		try
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				Log.Error("[RadialBlur] 无法获取战斗房间。", 2);
				return;
			}
			if (_blurRect != null && GodotObject.IsInstanceValid((GodotObject)(object)_blurRect))
			{
				Hide();
			}
			_isActive = true;
			_fadingOut = false;
			_owner = owner;
			float num = Mathf.Clamp(strength, 0f, 1f);
			int zIndexForOwner = GetZIndexForOwner(owner);
			_material = new ShaderMaterial
			{
				Shader = _sharedShader
			};
			_material.SetShaderParameter(StringName.op_Implicit("strength"), Variant.op_Implicit(num));
			_material.SetShaderParameter(StringName.op_Implicit("center"), Variant.op_Implicit(new Vector2(0.5f, 0.5f)));
			_material.SetShaderParameter(StringName.op_Implicit("intensity"), Variant.op_Implicit(1f));
			_material.SetShaderParameter(StringName.op_Implicit("vignette_amount"), Variant.op_Implicit(0.3f));
			_material.SetShaderParameter(StringName.op_Implicit("base_blur"), Variant.op_Implicit(0.08f));
			bool flag = !string.Equals(owner, "Pinpoint", StringComparison.OrdinalIgnoreCase);
			_blurRect = new ColorRect
			{
				Material = (Material)(object)_material,
				Color = Colors.White,
				MouseFilter = (MouseFilterEnum)2,
				ZIndex = zIndexForOwner
			};
			if (flag)
			{
				if (_blurLayer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_blurLayer))
				{
					_blurLayer = new CanvasLayer
					{
						Layer = 128
					};
					((Node)instance).AddChild((Node)(object)_blurLayer, false, (InternalMode)0);
				}
				((Control)_blurRect).SetAnchorsPreset((LayoutPreset)15, false);
				((Control)_blurRect).SetOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
				((Node)_blurLayer).AddChild((Node)(object)_blurRect, false, (InternalMode)0);
			}
			else
			{
				Rect2 viewportRect = ((CanvasItem)instance).GetViewportRect();
				((Control)_blurRect).Size = ((Rect2)(ref viewportRect)).Size * 2f;
				((Control)_blurRect).GlobalPosition = -((Rect2)(ref viewportRect)).Size * 0.5f;
				((Node)instance.CombatVfxContainer).AddChild((Node)(object)_blurRect, false, (InternalMode)0);
			}
			BiasedCognitionEffects.PauseBackground();
			_activeCts?.Cancel();
			_activeCts?.Dispose();
			_activeCts = new CancellationTokenSource();
			if (duration > 0f)
			{
				AutoFadeOut(duration, _activeCts.Token);
			}
		}
		finally
		{
			_isShowing = false;
		}
	}

	private static async Task AutoFadeOut(float duration, CancellationToken token)
	{
		float elapsed = 0f;
		float startStrength = (float)_material.GetShaderParameter(StringName.op_Implicit("strength"));
		while (_isActive && !_fadingOut && elapsed < duration && !token.IsCancellationRequested)
		{
			await Cmd.Wait(0.016f, true);
			elapsed += 0.016f;
			float t = Mathf.Clamp(elapsed / duration, 0f, 1f);
			float current = Mathf.Lerp(startStrength, 0f, t);
			ShaderMaterial material = _material;
			if (material != null)
			{
				material.SetShaderParameter(StringName.op_Implicit("strength"), Variant.op_Implicit(current));
			}
		}
		if (_isActive && !_fadingOut && !token.IsCancellationRequested)
		{
			Hide();
		}
	}

	public static void Hide()
	{
		if (_isActive)
		{
			_isActive = false;
			_fadingOut = false;
			_owner = null;
			_activeCts?.Cancel();
			_activeCts?.Dispose();
			_activeCts = null;
			if (_blurRect != null && GodotObject.IsInstanceValid((GodotObject)(object)_blurRect))
			{
				((Node)_blurRect).QueueFree();
			}
			_blurRect = null;
			_material = null;
			if (_blurLayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_blurLayer))
			{
				((Node)_blurLayer).QueueFree();
			}
			_blurLayer = null;
			BiasedCognitionEffects.ResumeBackground();
		}
	}

	public static void Stop(string owner = null)
	{
		if (_isActive && (owner == null || !(_owner != owner)) && (owner != null || _owner == null))
		{
			Hide();
		}
	}

	public static void FadeOut(float duration = 0.3f)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (_blurRect == null || !GodotObject.IsInstanceValid((GodotObject)(object)_blurRect))
		{
			Hide();
		}
		else
		{
			if (_fadingOut)
			{
				return;
			}
			_fadingOut = true;
			if (_material != null)
			{
				Tween val = ((Node)_blurRect).CreateTween();
				val.SetTrans((TransitionType)0);
				val.TweenProperty((GodotObject)(object)_material, NodePath.op_Implicit("shader_parameter/strength"), Variant.op_Implicit(0f), (double)duration);
				val.Finished += delegate
				{
					Hide();
				};
			}
			else
			{
				Hide();
			}
		}
	}
}
