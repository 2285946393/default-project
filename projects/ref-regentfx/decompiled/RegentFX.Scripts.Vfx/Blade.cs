using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts.Vfx;

[ScriptPath("res://Scripts/Vfx/Blade.cs")]
public class Blade : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName Launch = StringName.op_Implicit("Launch");

		public static readonly StringName OnHitTarget = StringName.op_Implicit("OnHitTarget");

		public static readonly StringName StartFadeOut = StringName.op_Implicit("StartFadeOut");

		public static readonly StringName SetGlow = StringName.op_Implicit("SetGlow");

		public static readonly StringName SpawnAndLaunch = StringName.op_Implicit("SpawnAndLaunch");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName PlayBlade = StringName.op_Implicit("PlayBlade");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName MoveDuration = StringName.op_Implicit("MoveDuration");

		public static readonly StringName ShakeDuration = StringName.op_Implicit("ShakeDuration");

		public static readonly StringName ShakeIntensity = StringName.op_Implicit("ShakeIntensity");

		public static readonly StringName ShakeSpeed = StringName.op_Implicit("ShakeSpeed");

		public static readonly StringName FadeOutDuration = StringName.op_Implicit("FadeOutDuration");

		public static readonly StringName StartScaleX = StringName.op_Implicit("StartScaleX");

		public static readonly StringName EndScaleX = StringName.op_Implicit("EndScaleX");

		public static readonly StringName StartGlowIntensity = StringName.op_Implicit("StartGlowIntensity");

		public static readonly StringName EndGlowIntensity = StringName.op_Implicit("EndGlowIntensity");

		public static readonly StringName StartGlowColor = StringName.op_Implicit("StartGlowColor");

		public static readonly StringName EndGlowColor = StringName.op_Implicit("EndGlowColor");

		public static readonly StringName _sprite = StringName.op_Implicit("_sprite");

		public static readonly StringName _shaderMaterial = StringName.op_Implicit("_shaderMaterial");

		public static readonly StringName _moveTween = StringName.op_Implicit("_moveTween");

		public static readonly StringName _shakeTween = StringName.op_Implicit("_shakeTween");

		public static readonly StringName _fadeTween = StringName.op_Implicit("_fadeTween");

		public static readonly StringName _targetPosition = StringName.op_Implicit("_targetPosition");

		public static readonly StringName _isShaking = StringName.op_Implicit("_isShaking");

		public static readonly StringName _shakePhase = StringName.op_Implicit("_shakePhase");

		public static readonly StringName _shakeBasePosition = StringName.op_Implicit("_shakeBasePosition");
	}

	public class SignalName : SignalName
	{
	}

	private Sprite2D? _sprite;

	private ShaderMaterial? _shaderMaterial;

	private Tween? _moveTween;

	private Tween? _shakeTween;

	private Tween? _fadeTween;

	private Vector2 _targetPosition;

	private bool _isShaking;

	private float _shakePhase;

	private Vector2 _shakeBasePosition;

	public static string Blade1Path = "res://RegentFX/scenes/Blade1.tscn";

	public static string Blade2Path = "res://RegentFX/scenes/Blade2.tscn";

	[Export(/*Could not decode attribute arguments.*/)]
	public float MoveDuration { get; set; } = 0.15f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ShakeDuration { get; set; } = 0.5f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ShakeIntensity { get; set; } = 4f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ShakeSpeed { get; set; } = 30f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float FadeOutDuration { get; set; } = 0.3f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float StartScaleX { get; set; } = 3f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float EndScaleX { get; set; } = 1f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float StartGlowIntensity { get; set; } = 2f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float EndGlowIntensity { get; set; } = 0.2f;


	[Export(/*Could not decode attribute arguments.*/)]
	public Color StartGlowColor { get; set; } = new Color(1f, 1f, 1f, 1f);


	[Export(/*Could not decode attribute arguments.*/)]
	public Color EndGlowColor { get; set; } = new Color(1f, 1f, 0.8f, 1f);


	public override void _Ready()
	{
		_sprite = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Sprite"));
		if (_sprite != null)
		{
			Material material = ((CanvasItem)_sprite).Material;
			Material obj = ((material is ShaderMaterial) ? material : null);
			ref ShaderMaterial? shaderMaterial = ref _shaderMaterial;
			Resource obj2 = ((obj != null) ? ((Resource)obj).Duplicate(false) : null);
			shaderMaterial = (ShaderMaterial?)(object)((obj2 is ShaderMaterial) ? obj2 : null);
			if (_shaderMaterial != null)
			{
				((CanvasItem)_sprite).Material = (Material)(object)_shaderMaterial;
			}
		}
	}

	public override void _Process(double delta)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (_isShaking)
		{
			float num = (float)delta;
			_shakePhase += ShakeSpeed * num;
			float num2 = Mathf.Sin(_shakePhase) * ShakeIntensity;
			float num3 = Mathf.Cos(_shakePhase * 1.3f) * ShakeIntensity;
			((Node2D)this).GlobalPosition = _shakeBasePosition + new Vector2(num2, num3);
		}
	}

	public void Launch(Vector2 from, Vector2 to)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)this).GlobalPosition = from;
		_targetPosition = to;
		Vector2 val = to - from;
		((Node2D)this).Rotation = ((Vector2)(ref val)).Angle();
		((Node2D)this).Scale = new Vector2(StartScaleX, 1f);
		SetGlow(StartGlowIntensity, StartGlowColor);
		_moveTween = ((Node)this).CreateTween();
		_moveTween.SetTrans((TransitionType)5);
		_moveTween.SetEase((EaseType)1);
		_moveTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("global_position"), Variant.op_Implicit(to), (double)MoveDuration);
		Tween obj = ((Node)this).CreateTween();
		obj.SetTrans((TransitionType)4);
		obj.SetEase((EaseType)1);
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale:x"), Variant.op_Implicit(EndScaleX), (double)MoveDuration);
		_moveTween.Finished += OnHitTarget;
	}

	private void OnHitTarget()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		_isShaking = true;
		_shakeBasePosition = _targetPosition;
		_shakePhase = 0f;
		Tween obj = ((Node)this).CreateTween();
		obj.TweenInterval((double)ShakeDuration);
		obj.Finished += StartFadeOut;
		Tween obj2 = ((Node)this).CreateTween();
		obj2.SetTrans((TransitionType)4);
		obj2.SetEase((EaseType)1);
		obj2.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			float intensity = Mathf.Lerp(StartGlowIntensity, EndGlowIntensity, t);
			Color startGlowColor = StartGlowColor;
			Color color = ((Color)(ref startGlowColor)).Lerp(EndGlowColor, t);
			SetGlow(intensity, color);
		}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), (double)(ShakeDuration + FadeOutDuration));
	}

	private void StartFadeOut()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		_isShaking = false;
		_fadeTween = ((Node)this).CreateTween();
		_fadeTween.SetTrans((TransitionType)4);
		_fadeTween.SetEase((EaseType)0);
		if (_sprite != null)
		{
			_fadeTween.TweenProperty((GodotObject)(object)_sprite, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)FadeOutDuration);
		}
		_fadeTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale:y"), Variant.op_Implicit(0.2f), (double)FadeOutDuration);
		_fadeTween.Finished += ((Node)this).QueueFree;
	}

	private void SetGlow(float intensity, Color color)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (_shaderMaterial != null)
		{
			_shaderMaterial.SetShaderParameter(StringName.op_Implicit("glow_intensity"), Variant.op_Implicit(intensity));
			_shaderMaterial.SetShaderParameter(StringName.op_Implicit("glow_color"), Variant.op_Implicit(color));
		}
	}

	public static Blade? SpawnAndLaunch(string scenePath, Vector2 from, Vector2 to, Node? parent = null)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Blade blade = VFXUtil.GenVFXNode<Blade>(scenePath);
		if (parent == null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			parent = (Node?)(object)((instance != null) ? instance.CombatVfxContainer : null);
		}
		if (parent == null)
		{
			Entry.Logger.Warn("[Blade] No parent available for blade", 1);
			((Node)blade).QueueFree();
			return null;
		}
		GodotTreeExtensions.AddChildSafely(parent, (Node)(object)blade);
		blade.Launch(from, to);
		return blade;
	}

	public override void _ExitTree()
	{
		Tween? moveTween = _moveTween;
		if (moveTween != null)
		{
			moveTween.Kill();
		}
		Tween? shakeTween = _shakeTween;
		if (shakeTween != null)
		{
			shakeTween.Kill();
		}
		Tween? fadeTween = _fadeTween;
		if (fadeTween != null)
		{
			fadeTween.Kill();
		}
		((Node)this)._ExitTree();
	}

	public static void PlayBlade(Vector2 position)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		if (!TestMode.IsOn)
		{
			Vector2 val = (((double)GD.Randf() < 0.7) ? new Vector2((float)(GD.Randi() % 600), 0f) : new Vector2(0f, (float)(GD.Randi() % 500)));
			Vector2 to = position + VFXUtil.RandVec2(30f);
			SpawnAndLaunch(Blade1Path, val, to);
			if (GD.Randf() < 0.5f)
			{
				SpawnAndLaunch(Blade2Path, val - new Vector2(300f, 300f), to);
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Expected O, but got Unknown
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Expected O, but got Unknown
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(9)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Launch, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("from"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("to"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnHitTarget, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.StartFadeOut, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetGlow, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("intensity"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SpawnAndLaunch, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("scenePath"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("from"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("to"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)24, StringName.op_Implicit("parent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.PlayBlade, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("position"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Launch && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			Launch(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnHitTarget && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnHitTarget();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.StartFadeOut && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StartFadeOut();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetGlow && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			SetGlow(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SpawnAndLaunch && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			Blade blade = SpawnAndLaunch(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = VariantUtils.CreateFrom<Blade>(ref blade);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.PlayBlade && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			PlayBlade(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((Node2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.SpawnAndLaunch && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			Blade blade = SpawnAndLaunch(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = VariantUtils.CreateFrom<Blade>(ref blade);
			return true;
		}
		if ((ref method) == MethodName.PlayBlade && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			PlayBlade(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.Launch)
		{
			return true;
		}
		if ((ref method) == MethodName.OnHitTarget)
		{
			return true;
		}
		if ((ref method) == MethodName.StartFadeOut)
		{
			return true;
		}
		if ((ref method) == MethodName.SetGlow)
		{
			return true;
		}
		if ((ref method) == MethodName.SpawnAndLaunch)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		if ((ref method) == MethodName.PlayBlade)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.MoveDuration)
		{
			MoveDuration = VariantUtils.ConvertTo<float>(ref value);
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
		if ((ref name) == PropertyName.FadeOutDuration)
		{
			FadeOutDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.StartScaleX)
		{
			StartScaleX = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EndScaleX)
		{
			EndScaleX = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.StartGlowIntensity)
		{
			StartGlowIntensity = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EndGlowIntensity)
		{
			EndGlowIntensity = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.StartGlowColor)
		{
			StartGlowColor = VariantUtils.ConvertTo<Color>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EndGlowColor)
		{
			EndGlowColor = VariantUtils.ConvertTo<Color>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			_sprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._shaderMaterial)
		{
			_shaderMaterial = VariantUtils.ConvertTo<ShaderMaterial>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._moveTween)
		{
			_moveTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._shakeTween)
		{
			_shakeTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fadeTween)
		{
			_fadeTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._targetPosition)
		{
			_targetPosition = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isShaking)
		{
			_isShaking = VariantUtils.ConvertTo<bool>(ref value);
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
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.MoveDuration)
		{
			float moveDuration = MoveDuration;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.ShakeDuration)
		{
			float moveDuration = ShakeDuration;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.ShakeIntensity)
		{
			float moveDuration = ShakeIntensity;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.ShakeSpeed)
		{
			float moveDuration = ShakeSpeed;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.FadeOutDuration)
		{
			float moveDuration = FadeOutDuration;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.StartScaleX)
		{
			float moveDuration = StartScaleX;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.EndScaleX)
		{
			float moveDuration = EndScaleX;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.StartGlowIntensity)
		{
			float moveDuration = StartGlowIntensity;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.EndGlowIntensity)
		{
			float moveDuration = EndGlowIntensity;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.StartGlowColor)
		{
			Color startGlowColor = StartGlowColor;
			value = VariantUtils.CreateFrom<Color>(ref startGlowColor);
			return true;
		}
		if ((ref name) == PropertyName.EndGlowColor)
		{
			Color startGlowColor = EndGlowColor;
			value = VariantUtils.CreateFrom<Color>(ref startGlowColor);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _sprite);
			return true;
		}
		if ((ref name) == PropertyName._shaderMaterial)
		{
			value = VariantUtils.CreateFrom<ShaderMaterial>(ref _shaderMaterial);
			return true;
		}
		if ((ref name) == PropertyName._moveTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _moveTween);
			return true;
		}
		if ((ref name) == PropertyName._shakeTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _shakeTween);
			return true;
		}
		if ((ref name) == PropertyName._fadeTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _fadeTween);
			return true;
		}
		if ((ref name) == PropertyName._targetPosition)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _targetPosition);
			return true;
		}
		if ((ref name) == PropertyName._isShaking)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isShaking);
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
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.MoveDuration, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ShakeDuration, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ShakeIntensity, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ShakeSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.FadeOutDuration, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.StartScaleX, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.EndScaleX, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.StartGlowIntensity, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.EndGlowIntensity, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)20, PropertyName.StartGlowColor, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)20, PropertyName.EndGlowColor, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._sprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._shaderMaterial, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._moveTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._shakeTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fadeTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._targetPosition, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isShaking, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._shakePhase, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._shakeBasePosition, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName moveDuration = PropertyName.MoveDuration;
		float moveDuration2 = MoveDuration;
		info.AddProperty(moveDuration, Variant.From<float>(ref moveDuration2));
		StringName shakeDuration = PropertyName.ShakeDuration;
		moveDuration2 = ShakeDuration;
		info.AddProperty(shakeDuration, Variant.From<float>(ref moveDuration2));
		StringName shakeIntensity = PropertyName.ShakeIntensity;
		moveDuration2 = ShakeIntensity;
		info.AddProperty(shakeIntensity, Variant.From<float>(ref moveDuration2));
		StringName shakeSpeed = PropertyName.ShakeSpeed;
		moveDuration2 = ShakeSpeed;
		info.AddProperty(shakeSpeed, Variant.From<float>(ref moveDuration2));
		StringName fadeOutDuration = PropertyName.FadeOutDuration;
		moveDuration2 = FadeOutDuration;
		info.AddProperty(fadeOutDuration, Variant.From<float>(ref moveDuration2));
		StringName startScaleX = PropertyName.StartScaleX;
		moveDuration2 = StartScaleX;
		info.AddProperty(startScaleX, Variant.From<float>(ref moveDuration2));
		StringName endScaleX = PropertyName.EndScaleX;
		moveDuration2 = EndScaleX;
		info.AddProperty(endScaleX, Variant.From<float>(ref moveDuration2));
		StringName startGlowIntensity = PropertyName.StartGlowIntensity;
		moveDuration2 = StartGlowIntensity;
		info.AddProperty(startGlowIntensity, Variant.From<float>(ref moveDuration2));
		StringName endGlowIntensity = PropertyName.EndGlowIntensity;
		moveDuration2 = EndGlowIntensity;
		info.AddProperty(endGlowIntensity, Variant.From<float>(ref moveDuration2));
		StringName startGlowColor = PropertyName.StartGlowColor;
		Color startGlowColor2 = StartGlowColor;
		info.AddProperty(startGlowColor, Variant.From<Color>(ref startGlowColor2));
		StringName endGlowColor = PropertyName.EndGlowColor;
		startGlowColor2 = EndGlowColor;
		info.AddProperty(endGlowColor, Variant.From<Color>(ref startGlowColor2));
		info.AddProperty(PropertyName._sprite, Variant.From<Sprite2D>(ref _sprite));
		info.AddProperty(PropertyName._shaderMaterial, Variant.From<ShaderMaterial>(ref _shaderMaterial));
		info.AddProperty(PropertyName._moveTween, Variant.From<Tween>(ref _moveTween));
		info.AddProperty(PropertyName._shakeTween, Variant.From<Tween>(ref _shakeTween));
		info.AddProperty(PropertyName._fadeTween, Variant.From<Tween>(ref _fadeTween));
		info.AddProperty(PropertyName._targetPosition, Variant.From<Vector2>(ref _targetPosition));
		info.AddProperty(PropertyName._isShaking, Variant.From<bool>(ref _isShaking));
		info.AddProperty(PropertyName._shakePhase, Variant.From<float>(ref _shakePhase));
		info.AddProperty(PropertyName._shakeBasePosition, Variant.From<Vector2>(ref _shakeBasePosition));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.MoveDuration, ref val))
		{
			MoveDuration = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShakeDuration, ref val2))
		{
			ShakeDuration = ((Variant)(ref val2)).As<float>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShakeIntensity, ref val3))
		{
			ShakeIntensity = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShakeSpeed, ref val4))
		{
			ShakeSpeed = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.FadeOutDuration, ref val5))
		{
			FadeOutDuration = ((Variant)(ref val5)).As<float>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName.StartScaleX, ref val6))
		{
			StartScaleX = ((Variant)(ref val6)).As<float>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName.EndScaleX, ref val7))
		{
			EndScaleX = ((Variant)(ref val7)).As<float>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName.StartGlowIntensity, ref val8))
		{
			StartGlowIntensity = ((Variant)(ref val8)).As<float>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName.EndGlowIntensity, ref val9))
		{
			EndGlowIntensity = ((Variant)(ref val9)).As<float>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName.StartGlowColor, ref val10))
		{
			StartGlowColor = ((Variant)(ref val10)).As<Color>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName.EndGlowColor, ref val11))
		{
			EndGlowColor = ((Variant)(ref val11)).As<Color>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._sprite, ref val12))
		{
			_sprite = ((Variant)(ref val12)).As<Sprite2D>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._shaderMaterial, ref val13))
		{
			_shaderMaterial = ((Variant)(ref val13)).As<ShaderMaterial>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._moveTween, ref val14))
		{
			_moveTween = ((Variant)(ref val14)).As<Tween>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._shakeTween, ref val15))
		{
			_shakeTween = ((Variant)(ref val15)).As<Tween>();
		}
		Variant val16 = default(Variant);
		if (info.TryGetProperty(PropertyName._fadeTween, ref val16))
		{
			_fadeTween = ((Variant)(ref val16)).As<Tween>();
		}
		Variant val17 = default(Variant);
		if (info.TryGetProperty(PropertyName._targetPosition, ref val17))
		{
			_targetPosition = ((Variant)(ref val17)).As<Vector2>();
		}
		Variant val18 = default(Variant);
		if (info.TryGetProperty(PropertyName._isShaking, ref val18))
		{
			_isShaking = ((Variant)(ref val18)).As<bool>();
		}
		Variant val19 = default(Variant);
		if (info.TryGetProperty(PropertyName._shakePhase, ref val19))
		{
			_shakePhase = ((Variant)(ref val19)).As<float>();
		}
		Variant val20 = default(Variant);
		if (info.TryGetProperty(PropertyName._shakeBasePosition, ref val20))
		{
			_shakeBasePosition = ((Variant)(ref val20)).As<Vector2>();
		}
	}
}
