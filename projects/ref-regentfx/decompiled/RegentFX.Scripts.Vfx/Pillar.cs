using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx;

[GlobalClass]
[ScriptPath("res://Scripts/Vfx/Pillar.cs")]
public class Pillar : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetupGlowMaterial = StringName.op_Implicit("SetupGlowMaterial");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName Activate = StringName.op_Implicit("Activate");

		public static readonly StringName SetActiveAlpha = StringName.op_Implicit("SetActiveAlpha");

		public static readonly StringName SetGlowIntensity = StringName.op_Implicit("SetGlowIntensity");

		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName OnLand = StringName.op_Implicit("OnLand");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName FallHeight = StringName.op_Implicit("FallHeight");

		public static readonly StringName FallDuration = StringName.op_Implicit("FallDuration");

		public static readonly StringName ShakeDuration = StringName.op_Implicit("ShakeDuration");

		public static readonly StringName ShakeIntensity = StringName.op_Implicit("ShakeIntensity");

		public static readonly StringName ShakeSpeed = StringName.op_Implicit("ShakeSpeed");

		public static readonly StringName ActivateFlashDuration = StringName.op_Implicit("ActivateFlashDuration");

		public static readonly StringName ActivatePeakTime = StringName.op_Implicit("ActivatePeakTime");

		public static readonly StringName GlowMaxIntensity = StringName.op_Implicit("GlowMaxIntensity");

		public static readonly StringName StartGlow = StringName.op_Implicit("StartGlow");

		public static readonly StringName _topSprite = StringName.op_Implicit("_topSprite");

		public static readonly StringName _spinBase = StringName.op_Implicit("_spinBase");

		public static readonly StringName _spinActive = StringName.op_Implicit("_spinActive");

		public static readonly StringName _mainBase = StringName.op_Implicit("_mainBase");

		public static readonly StringName _mainActive = StringName.op_Implicit("_mainActive");

		public static readonly StringName _lightSprite = StringName.op_Implicit("_lightSprite");

		public static readonly StringName _isShaking = StringName.op_Implicit("_isShaking");

		public static readonly StringName _shakeTimer = StringName.op_Implicit("_shakeTimer");

		public static readonly StringName _shakePhase = StringName.op_Implicit("_shakePhase");

		public static readonly StringName _shakeBasePosition = StringName.op_Implicit("_shakeBasePosition");

		public static readonly StringName _targetPosition = StringName.op_Implicit("_targetPosition");

		public static readonly StringName _fallTween = StringName.op_Implicit("_fallTween");

		public static readonly StringName _activateTween = StringName.op_Implicit("_activateTween");

		public static readonly StringName _scaleTween = StringName.op_Implicit("_scaleTween");

		public static readonly StringName _glowTween = StringName.op_Implicit("_glowTween");
	}

	public class SignalName : SignalName
	{
	}

	private Sprite2D? _topSprite;

	private AnimatedSprite2D? _spinBase;

	private AnimatedSprite2D? _spinActive;

	private Sprite2D? _mainBase;

	private Sprite2D? _mainActive;

	private Sprite2D? _lightSprite;

	private readonly List<ShaderMaterial> _glowMaterials = new List<ShaderMaterial>();

	private bool _isShaking;

	private float _shakeTimer;

	private float _shakePhase;

	private Vector2 _shakeBasePosition;

	private Vector2 _targetPosition;

	private Tween? _fallTween;

	private Tween? _activateTween;

	private Tween? _scaleTween;

	private Tween? _glowTween;

	public const string VfxScenePath = "res://RegentFX/scenes/vfx/pillar.tscn";

	public const string BurstPath = "res://RegentFX/scenes/vfx/p_burst.tscn";

	public static Dictionary<Creature, Pillar> Pillars = new Dictionary<Creature, Pillar>();

	[Export(/*Could not decode attribute arguments.*/)]
	public float FallHeight { get; set; } = 800f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float FallDuration { get; set; } = 0.3f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ShakeDuration { get; set; } = 0.3f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ShakeIntensity { get; set; } = 6f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ShakeSpeed { get; set; } = 40f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ActivateFlashDuration { get; set; } = 0.8f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ActivatePeakTime { get; set; } = 0.12f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float GlowMaxIntensity { get; set; } = 1.5f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float StartGlow { get; set; } = 0.1f;


	public override void _Ready()
	{
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		_topSprite = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Top"));
		_spinBase = ((Node)this).GetNodeOrNull<AnimatedSprite2D>(NodePath.op_Implicit("Spin/Base"));
		_spinActive = ((Node)this).GetNodeOrNull<AnimatedSprite2D>(NodePath.op_Implicit("Spin/Active"));
		_mainBase = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Main/Base"));
		_mainActive = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Main/Active"));
		_lightSprite = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Light"));
		if (_spinBase != null)
		{
			_spinBase.Play(StringName.op_Implicit("default"), 1f, false);
		}
		if (_spinActive != null)
		{
			_spinActive.Play(StringName.op_Implicit("default"), 1f, false);
			((CanvasItem)_spinActive).Modulate = new Color(1f, 1f, 1f, 0f);
		}
		if (_mainActive != null)
		{
			((CanvasItem)_mainActive).Modulate = new Color(1f, 1f, 1f, 0f);
		}
		if (_lightSprite != null)
		{
			((CanvasItem)_lightSprite).Modulate = new Color(1f, 1f, 1f, 0f);
		}
		SetupGlowMaterial((CanvasItem?)(object)_topSprite);
		SetupGlowMaterial((CanvasItem?)(object)_mainBase);
		SetupGlowMaterial((CanvasItem?)(object)_spinBase);
	}

	private void SetupGlowMaterial(CanvasItem? sprite)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		if (sprite == null)
		{
			return;
		}
		ShaderMaterial val = null;
		Material material = sprite.Material;
		ShaderMaterial val2 = (ShaderMaterial)(object)((material is ShaderMaterial) ? material : null);
		if (val2 != null)
		{
			Resource obj = ((Resource)val2).Duplicate(false);
			val = (ShaderMaterial)(object)((obj is ShaderMaterial) ? obj : null);
		}
		if (val == null)
		{
			Shader val3 = GD.Load<Shader>("res://RegentFX/shaders/vfx/pillar/pillar_glow.gdshader");
			if (val3 != null)
			{
				val = new ShaderMaterial
				{
					Shader = val3
				};
			}
		}
		if (val != null)
		{
			val.SetShaderParameter(StringName.op_Implicit("glow_color"), Variant.op_Implicit(Colors.White));
			val.SetShaderParameter(StringName.op_Implicit("glow_intensity"), Variant.op_Implicit(StartGlow));
			val.SetShaderParameter(StringName.op_Implicit("glow_radius"), Variant.op_Implicit(34f));
			val.SetShaderParameter(StringName.op_Implicit("inner_glow"), Variant.op_Implicit(0.2f));
			sprite.Material = (Material)(object)val;
			_glowMaterials.Add(val);
		}
	}

	public override void _Process(double delta)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (_isShaking)
		{
			float num = (float)delta;
			_shakeTimer += num;
			_shakePhase += ShakeSpeed * num;
			if (_shakeTimer >= ShakeDuration)
			{
				_isShaking = false;
				((Node2D)this).GlobalPosition = _shakeBasePosition;
			}
			else
			{
				float num2 = Mathf.Sin(_shakePhase) * ShakeIntensity;
				float num3 = Mathf.Cos(_shakePhase * 1.3f) * ShakeIntensity;
				((Node2D)this).GlobalPosition = _shakeBasePosition + new Vector2(num2, num3);
			}
		}
	}

	public void Activate()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		if (_spinBase != null && _spinActive != null)
		{
			_spinActive.Frame = _spinBase.Frame;
		}
		FmodLite.Play("event:/RegentFx/sfx/seven_stars_hold");
		_activateTween = ((Node)this).CreateTween();
		_activateTween.SetTrans((TransitionType)4);
		_activateTween.SetEase((EaseType)1);
		_activateTween.TweenMethod(Callable.From<float>((Action<float>)SetActiveAlpha), Variant.op_Implicit(0f), Variant.op_Implicit(0.5f), (double)ActivatePeakTime);
		_activateTween.Chain();
		_activateTween.SetTrans((TransitionType)4);
		_activateTween.SetEase((EaseType)0);
		_activateTween.TweenMethod(Callable.From<float>((Action<float>)SetActiveAlpha), Variant.op_Implicit(0.5f), Variant.op_Implicit(0f), (double)(ActivateFlashDuration - ActivatePeakTime));
		_glowTween = ((Node)this).CreateTween();
		_glowTween.SetTrans((TransitionType)4);
		_glowTween.SetEase((EaseType)1);
		_glowTween.TweenMethod(Callable.From<float>((Action<float>)SetGlowIntensity), Variant.op_Implicit(StartGlow), Variant.op_Implicit(GlowMaxIntensity), (double)ActivatePeakTime);
		_glowTween.Chain();
		_glowTween.SetTrans((TransitionType)4);
		_glowTween.SetEase((EaseType)0);
		_glowTween.TweenMethod(Callable.From<float>((Action<float>)SetGlowIntensity), Variant.op_Implicit(GlowMaxIntensity), Variant.op_Implicit(StartGlow), (double)(ActivateFlashDuration - ActivatePeakTime));
	}

	private void SetActiveAlpha(float alpha)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (_spinActive != null)
		{
			((CanvasItem)_spinActive).Modulate = new Color(1f, 1f, 1f, alpha);
		}
		if (_mainActive != null)
		{
			((CanvasItem)_mainActive).Modulate = new Color(1f, 1f, 1f, alpha);
		}
	}

	private void SetGlowIntensity(float intensity)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		foreach (ShaderMaterial glowMaterial in _glowMaterials)
		{
			glowMaterial.SetShaderParameter(StringName.op_Implicit("glow_intensity"), Variant.op_Implicit(intensity));
		}
	}

	public void Create(Vector2 position)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		_targetPosition = position;
		_shakeBasePosition = position;
		((Node2D)this).GlobalPosition = position + new Vector2(0f, 0f - FallHeight);
		((Node2D)this).Scale = new Vector2(1f, 1.3f);
		_fallTween = ((Node)this).CreateTween();
		_fallTween.SetTrans((TransitionType)5);
		_fallTween.SetEase((EaseType)0);
		_fallTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("global_position"), Variant.op_Implicit(position), (double)FallDuration);
		_scaleTween = ((Node)this).CreateTween();
		_scaleTween.SetTrans((TransitionType)4);
		_scaleTween.SetEase((EaseType)1);
		_scaleTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One), (double)FallDuration);
		_fallTween.Finished += OnLand;
	}

	private void OnLand()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		_isShaking = true;
		_shakeTimer = 0f;
		_shakePhase = 0f;
		Node2D val = VFXUtil.PlaySimple("res://RegentFX/scenes/vfx/p_burst.tscn", _targetPosition);
		if (val != null)
		{
			val.Scale *= 3f;
		}
		FmodLite.Play("event:/RegentFx/sfx/pillar_burst");
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)3, (ShakeDuration)2, 90f);
		}
		Tween obj = ((Node)this).CreateTween();
		obj.SetTrans((TransitionType)4);
		obj.SetEase((EaseType)1);
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale:y"), Variant.op_Implicit(0.85f), 0.07999999821186066);
		obj.Chain();
		obj.SetTrans((TransitionType)6);
		obj.SetEase((EaseType)1);
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale:y"), Variant.op_Implicit(1f), 0.25);
		if (_lightSprite != null)
		{
			Tween obj2 = ((Node)this).CreateTween();
			obj2.SetTrans((TransitionType)4);
			obj2.SetEase((EaseType)1);
			obj2.TweenProperty((GodotObject)(object)_lightSprite, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 0.15000000596046448);
		}
	}

	public static Pillar? Spawn(Creature creature, Vector2 position)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if (TestMode.IsOn)
		{
			return null;
		}
		try
		{
			Pillar pillar = VFXUtil.GenVFXNode<Pillar>("res://RegentFX/scenes/vfx/pillar.tscn");
			NCombatRoom instance = NCombatRoom.Instance;
			Node val = (Node)(object)((instance != null) ? instance.BackCombatVfxContainer : null);
			if (val == null)
			{
				Entry.Logger.Warn("[Pillar] No BackCombatVfxContainer available", 1);
				((Node)pillar).QueueFree();
				return null;
			}
			FmodLite.Play("event:/RegentFx/sfx/pillar_create");
			GodotTreeExtensions.AddChildSafely(val, (Node)(object)pillar);
			pillar.Create(position);
			Pillars[creature] = pillar;
			return pillar;
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[Pillar] Failed to create pillar: " + ex.Message, 1);
			return null;
		}
	}

	public override void _ExitTree()
	{
		Tween? fallTween = _fallTween;
		if (fallTween != null)
		{
			fallTween.Kill();
		}
		Tween? activateTween = _activateTween;
		if (activateTween != null)
		{
			activateTween.Kill();
		}
		Tween? scaleTween = _scaleTween;
		if (scaleTween != null)
		{
			scaleTween.Kill();
		}
		Tween? glowTween = _glowTween;
		if (glowTween != null)
		{
			glowTween.Kill();
		}
		((Node)this)._ExitTree();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(9)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetupGlowMaterial, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("sprite"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CanvasItem"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Activate, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetActiveAlpha, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("alpha"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetGlowIntensity, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("intensity"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("position"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnLand, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetupGlowMaterial && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetupGlowMaterial(VariantUtils.ConvertTo<CanvasItem>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Activate && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Activate();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetActiveAlpha && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetActiveAlpha(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetGlowIntensity && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetGlowIntensity(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Create(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnLand && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnLand();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((Node2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.SetupGlowMaterial)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.Activate)
		{
			return true;
		}
		if ((ref method) == MethodName.SetActiveAlpha)
		{
			return true;
		}
		if ((ref method) == MethodName.SetGlowIntensity)
		{
			return true;
		}
		if ((ref method) == MethodName.Create)
		{
			return true;
		}
		if ((ref method) == MethodName.OnLand)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.FallHeight)
		{
			FallHeight = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.FallDuration)
		{
			FallDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ShakeDuration)
		{
			ShakeDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ShakeIntensity)
		{
			ShakeIntensity = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ShakeSpeed)
		{
			ShakeSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ActivateFlashDuration)
		{
			ActivateFlashDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ActivatePeakTime)
		{
			ActivatePeakTime = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.GlowMaxIntensity)
		{
			GlowMaxIntensity = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.StartGlow)
		{
			StartGlow = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._topSprite)
		{
			_topSprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._spinBase)
		{
			_spinBase = VariantUtils.ConvertTo<AnimatedSprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._spinActive)
		{
			_spinActive = VariantUtils.ConvertTo<AnimatedSprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._mainBase)
		{
			_mainBase = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._mainActive)
		{
			_mainActive = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._lightSprite)
		{
			_lightSprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isShaking)
		{
			_isShaking = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._shakeTimer)
		{
			_shakeTimer = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._shakePhase)
		{
			_shakePhase = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._shakeBasePosition)
		{
			_shakeBasePosition = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._targetPosition)
		{
			_targetPosition = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fallTween)
		{
			_fallTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._activateTween)
		{
			_activateTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._scaleTween)
		{
			_scaleTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._glowTween)
		{
			_glowTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.FallHeight)
		{
			float fallHeight = FallHeight;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.FallDuration)
		{
			float fallHeight = FallDuration;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.ShakeDuration)
		{
			float fallHeight = ShakeDuration;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.ShakeIntensity)
		{
			float fallHeight = ShakeIntensity;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.ShakeSpeed)
		{
			float fallHeight = ShakeSpeed;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.ActivateFlashDuration)
		{
			float fallHeight = ActivateFlashDuration;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.ActivatePeakTime)
		{
			float fallHeight = ActivatePeakTime;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.GlowMaxIntensity)
		{
			float fallHeight = GlowMaxIntensity;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName.StartGlow)
		{
			float fallHeight = StartGlow;
			value = VariantUtils.CreateFrom<float>(ref fallHeight);
			return true;
		}
		if ((ref name) == PropertyName._topSprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _topSprite);
			return true;
		}
		if ((ref name) == PropertyName._spinBase)
		{
			value = VariantUtils.CreateFrom<AnimatedSprite2D>(ref _spinBase);
			return true;
		}
		if ((ref name) == PropertyName._spinActive)
		{
			value = VariantUtils.CreateFrom<AnimatedSprite2D>(ref _spinActive);
			return true;
		}
		if ((ref name) == PropertyName._mainBase)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _mainBase);
			return true;
		}
		if ((ref name) == PropertyName._mainActive)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _mainActive);
			return true;
		}
		if ((ref name) == PropertyName._lightSprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _lightSprite);
			return true;
		}
		if ((ref name) == PropertyName._isShaking)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isShaking);
			return true;
		}
		if ((ref name) == PropertyName._shakeTimer)
		{
			value = VariantUtils.CreateFrom<float>(ref _shakeTimer);
			return true;
		}
		if ((ref name) == PropertyName._shakePhase)
		{
			value = VariantUtils.CreateFrom<float>(ref _shakePhase);
			return true;
		}
		if ((ref name) == PropertyName._shakeBasePosition)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _shakeBasePosition);
			return true;
		}
		if ((ref name) == PropertyName._targetPosition)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _targetPosition);
			return true;
		}
		if ((ref name) == PropertyName._fallTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _fallTween);
			return true;
		}
		if ((ref name) == PropertyName._activateTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _activateTween);
			return true;
		}
		if ((ref name) == PropertyName._scaleTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _scaleTween);
			return true;
		}
		if ((ref name) == PropertyName._glowTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _glowTween);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.FallHeight, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.FallDuration, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ShakeDuration, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ShakeIntensity, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ShakeSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ActivateFlashDuration, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ActivatePeakTime, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.GlowMaxIntensity, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.StartGlow, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._topSprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._spinBase, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._spinActive, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._mainBase, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._mainActive, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._lightSprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isShaking, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._shakeTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._shakePhase, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._shakeBasePosition, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._targetPosition, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fallTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._activateTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._scaleTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._glowTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName fallHeight = PropertyName.FallHeight;
		float fallHeight2 = FallHeight;
		info.AddProperty(fallHeight, Variant.From<float>(ref fallHeight2));
		StringName fallDuration = PropertyName.FallDuration;
		fallHeight2 = FallDuration;
		info.AddProperty(fallDuration, Variant.From<float>(ref fallHeight2));
		StringName shakeDuration = PropertyName.ShakeDuration;
		fallHeight2 = ShakeDuration;
		info.AddProperty(shakeDuration, Variant.From<float>(ref fallHeight2));
		StringName shakeIntensity = PropertyName.ShakeIntensity;
		fallHeight2 = ShakeIntensity;
		info.AddProperty(shakeIntensity, Variant.From<float>(ref fallHeight2));
		StringName shakeSpeed = PropertyName.ShakeSpeed;
		fallHeight2 = ShakeSpeed;
		info.AddProperty(shakeSpeed, Variant.From<float>(ref fallHeight2));
		StringName activateFlashDuration = PropertyName.ActivateFlashDuration;
		fallHeight2 = ActivateFlashDuration;
		info.AddProperty(activateFlashDuration, Variant.From<float>(ref fallHeight2));
		StringName activatePeakTime = PropertyName.ActivatePeakTime;
		fallHeight2 = ActivatePeakTime;
		info.AddProperty(activatePeakTime, Variant.From<float>(ref fallHeight2));
		StringName glowMaxIntensity = PropertyName.GlowMaxIntensity;
		fallHeight2 = GlowMaxIntensity;
		info.AddProperty(glowMaxIntensity, Variant.From<float>(ref fallHeight2));
		StringName startGlow = PropertyName.StartGlow;
		fallHeight2 = StartGlow;
		info.AddProperty(startGlow, Variant.From<float>(ref fallHeight2));
		info.AddProperty(PropertyName._topSprite, Variant.From<Sprite2D>(ref _topSprite));
		info.AddProperty(PropertyName._spinBase, Variant.From<AnimatedSprite2D>(ref _spinBase));
		info.AddProperty(PropertyName._spinActive, Variant.From<AnimatedSprite2D>(ref _spinActive));
		info.AddProperty(PropertyName._mainBase, Variant.From<Sprite2D>(ref _mainBase));
		info.AddProperty(PropertyName._mainActive, Variant.From<Sprite2D>(ref _mainActive));
		info.AddProperty(PropertyName._lightSprite, Variant.From<Sprite2D>(ref _lightSprite));
		info.AddProperty(PropertyName._isShaking, Variant.From<bool>(ref _isShaking));
		info.AddProperty(PropertyName._shakeTimer, Variant.From<float>(ref _shakeTimer));
		info.AddProperty(PropertyName._shakePhase, Variant.From<float>(ref _shakePhase));
		info.AddProperty(PropertyName._shakeBasePosition, Variant.From<Vector2>(ref _shakeBasePosition));
		info.AddProperty(PropertyName._targetPosition, Variant.From<Vector2>(ref _targetPosition));
		info.AddProperty(PropertyName._fallTween, Variant.From<Tween>(ref _fallTween));
		info.AddProperty(PropertyName._activateTween, Variant.From<Tween>(ref _activateTween));
		info.AddProperty(PropertyName._scaleTween, Variant.From<Tween>(ref _scaleTween));
		info.AddProperty(PropertyName._glowTween, Variant.From<Tween>(ref _glowTween));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.FallHeight, ref val))
		{
			FallHeight = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.FallDuration, ref val2))
		{
			FallDuration = ((Variant)(ref val2)).As<float>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShakeDuration, ref val3))
		{
			ShakeDuration = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShakeIntensity, ref val4))
		{
			ShakeIntensity = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShakeSpeed, ref val5))
		{
			ShakeSpeed = ((Variant)(ref val5)).As<float>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName.ActivateFlashDuration, ref val6))
		{
			ActivateFlashDuration = ((Variant)(ref val6)).As<float>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName.ActivatePeakTime, ref val7))
		{
			ActivatePeakTime = ((Variant)(ref val7)).As<float>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName.GlowMaxIntensity, ref val8))
		{
			GlowMaxIntensity = ((Variant)(ref val8)).As<float>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName.StartGlow, ref val9))
		{
			StartGlow = ((Variant)(ref val9)).As<float>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._topSprite, ref val10))
		{
			_topSprite = ((Variant)(ref val10)).As<Sprite2D>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._spinBase, ref val11))
		{
			_spinBase = ((Variant)(ref val11)).As<AnimatedSprite2D>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._spinActive, ref val12))
		{
			_spinActive = ((Variant)(ref val12)).As<AnimatedSprite2D>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._mainBase, ref val13))
		{
			_mainBase = ((Variant)(ref val13)).As<Sprite2D>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._mainActive, ref val14))
		{
			_mainActive = ((Variant)(ref val14)).As<Sprite2D>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._lightSprite, ref val15))
		{
			_lightSprite = ((Variant)(ref val15)).As<Sprite2D>();
		}
		Variant val16 = default(Variant);
		if (info.TryGetProperty(PropertyName._isShaking, ref val16))
		{
			_isShaking = ((Variant)(ref val16)).As<bool>();
		}
		Variant val17 = default(Variant);
		if (info.TryGetProperty(PropertyName._shakeTimer, ref val17))
		{
			_shakeTimer = ((Variant)(ref val17)).As<float>();
		}
		Variant val18 = default(Variant);
		if (info.TryGetProperty(PropertyName._shakePhase, ref val18))
		{
			_shakePhase = ((Variant)(ref val18)).As<float>();
		}
		Variant val19 = default(Variant);
		if (info.TryGetProperty(PropertyName._shakeBasePosition, ref val19))
		{
			_shakeBasePosition = ((Variant)(ref val19)).As<Vector2>();
		}
		Variant val20 = default(Variant);
		if (info.TryGetProperty(PropertyName._targetPosition, ref val20))
		{
			_targetPosition = ((Variant)(ref val20)).As<Vector2>();
		}
		Variant val21 = default(Variant);
		if (info.TryGetProperty(PropertyName._fallTween, ref val21))
		{
			_fallTween = ((Variant)(ref val21)).As<Tween>();
		}
		Variant val22 = default(Variant);
		if (info.TryGetProperty(PropertyName._activateTween, ref val22))
		{
			_activateTween = ((Variant)(ref val22)).As<Tween>();
		}
		Variant val23 = default(Variant);
		if (info.TryGetProperty(PropertyName._scaleTween, ref val23))
		{
			_scaleTween = ((Variant)(ref val23)).As<Tween>();
		}
		Variant val24 = default(Variant);
		if (info.TryGetProperty(PropertyName._glowTween, ref val24))
		{
			_glowTween = ((Variant)(ref val24)).As<Tween>();
		}
	}
}
