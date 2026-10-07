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

[ScriptPath("res://Scripts/Vfx/StardustVfx.cs")]
public class StardustVfx : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName Launch = StringName.op_Implicit("Launch");

		public static readonly StringName OnHitTarget = StringName.op_Implicit("OnHitTarget");

		public static readonly StringName Finished = StringName.op_Implicit("Finished");

		public static readonly StringName SetVisualActive = StringName.op_Implicit("SetVisualActive");

		public static readonly StringName SpawnAndLaunch = StringName.op_Implicit("SpawnAndLaunch");

		public static readonly StringName PlayStardust = StringName.op_Implicit("PlayStardust");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName MoveDuration = StringName.op_Implicit("MoveDuration");

		public static readonly StringName FadeOutDuration = StringName.op_Implicit("FadeOutDuration");

		public static readonly StringName StartScale = StringName.op_Implicit("StartScale");

		public static readonly StringName EndScale = StringName.op_Implicit("EndScale");

		public static readonly StringName RotationAmount = StringName.op_Implicit("RotationAmount");

		public static readonly StringName _trailParticles = StringName.op_Implicit("_trailParticles");

		public static readonly StringName _glow = StringName.op_Implicit("_glow");

		public static readonly StringName _sprite = StringName.op_Implicit("_sprite");

		public static readonly StringName _tintMaterial = StringName.op_Implicit("_tintMaterial");

		public static readonly StringName _moveTween = StringName.op_Implicit("_moveTween");

		public static readonly StringName _fadeTween = StringName.op_Implicit("_fadeTween");

		public static readonly StringName _rotationTween = StringName.op_Implicit("_rotationTween");
	}

	public class SignalName : SignalName
	{
	}

	public const string ScenePath = "res://RegentFX/scenes/Stardust.tscn";

	private GpuParticles2D? _trailParticles;

	private Sprite2D? _glow;

	private Sprite2D? _sprite;

	private ShaderMaterial? _tintMaterial;

	private Tween? _moveTween;

	private Tween? _fadeTween;

	private Tween? _rotationTween;

	public float MoveDuration { get; set; } = 0.35f;


	public float FadeOutDuration { get; set; } = 0.45f;


	public float StartScale { get; set; } = 0.6f;


	public float EndScale { get; set; } = 1.2f;


	public float RotationAmount { get; set; } = (float)Math.PI * 2f;


	public override void _Ready()
	{
		_trailParticles = ((Node)this).GetNodeOrNull<GpuParticles2D>(NodePath.op_Implicit("TrailParticles"));
		_glow = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Glow"));
		_sprite = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Sprite"));
		if (_sprite != null)
		{
			Material material = ((CanvasItem)_sprite).Material;
			Material obj = ((material is ShaderMaterial) ? material : null);
			ref ShaderMaterial? tintMaterial = ref _tintMaterial;
			Resource obj2 = ((obj != null) ? ((Resource)obj).Duplicate(false) : null);
			tintMaterial = (ShaderMaterial?)(object)((obj2 is ShaderMaterial) ? obj2 : null);
			if (_tintMaterial != null)
			{
				((CanvasItem)_sprite).Material = (Material)(object)_tintMaterial;
			}
		}
		SetVisualActive(active: false);
	}

	public void Launch(Vector2 from, Vector2 to)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)this).GlobalPosition = from;
		Vector2 val = to - from;
		((Node2D)this).Rotation = ((Vector2)(ref val)).Angle();
		SetVisualActive(active: true);
		((Node2D)this).Scale = Vector2.One * StartScale;
		GpuParticles2D? trailParticles = _trailParticles;
		if (trailParticles != null)
		{
			trailParticles.Restart();
		}
		_moveTween = ((Node)this).CreateTween();
		_moveTween.SetTrans((TransitionType)4);
		_moveTween.SetEase((EaseType)1);
		_moveTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("global_position"), Variant.op_Implicit(to), (double)MoveDuration);
		_rotationTween = ((Node)this).CreateTween();
		_rotationTween.SetTrans((TransitionType)0);
		_rotationTween.SetEase((EaseType)2);
		float rotation = ((Node2D)this).Rotation;
		float num = rotation + RotationAmount;
		_rotationTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float r)
		{
			((Node2D)this).Rotation = r;
		}), Variant.op_Implicit(rotation), Variant.op_Implicit(num), (double)MoveDuration);
		Tween obj = ((Node)this).CreateTween();
		obj.SetTrans((TransitionType)4);
		obj.SetEase((EaseType)1);
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One * EndScale), (double)MoveDuration);
		_moveTween.Finished += OnHitTarget;
	}

	private void OnHitTarget()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (_trailParticles != null)
		{
			_trailParticles.Emitting = false;
		}
		_fadeTween = ((Node)this).CreateTween();
		_fadeTween.SetTrans((TransitionType)4);
		_fadeTween.SetEase((EaseType)0);
		if (_sprite != null)
		{
			_fadeTween.TweenProperty((GodotObject)(object)_sprite, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)FadeOutDuration);
		}
		if (_glow != null)
		{
			_fadeTween.Parallel().TweenProperty((GodotObject)(object)_glow, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)FadeOutDuration);
		}
		_fadeTween.Parallel().TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.Zero), (double)FadeOutDuration);
		_fadeTween.Finished += Finished;
	}

	private async void Finished()
	{
		await VFXUtil.Wait(1f);
		((Node)this).QueueFree();
	}

	private void SetVisualActive(bool active)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((CanvasItem)_sprite).Visible = active;
			((CanvasItem)_sprite).Modulate = new Color(((CanvasItem)_sprite).Modulate.R, ((CanvasItem)_sprite).Modulate.G, ((CanvasItem)_sprite).Modulate.B, 1f);
		}
		if (_glow != null)
		{
			((CanvasItem)_glow).Visible = active;
			((CanvasItem)_glow).Modulate = new Color(((CanvasItem)_glow).Modulate.R, ((CanvasItem)_glow).Modulate.G, ((CanvasItem)_glow).Modulate.B, 1f);
		}
		if (_trailParticles != null)
		{
			_trailParticles.Emitting = active;
		}
	}

	public static StardustVfx? SpawnAndLaunch(Vector2 from, Vector2 to, Node? parent = null)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		StardustVfx stardustVfx = VFXUtil.GenVFXNode<StardustVfx>("res://RegentFX/scenes/Stardust.tscn");
		if (parent == null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			parent = (Node?)(object)((instance != null) ? instance.CombatVfxContainer : null);
		}
		if (parent == null)
		{
			Entry.Logger.Warn("[StardustVfx] No parent available for stardust", 1);
			((Node)stardustVfx).QueueFree();
			return null;
		}
		GodotTreeExtensions.AddChildSafely(parent, (Node)(object)stardustVfx);
		stardustVfx.Launch(from, to);
		return stardustVfx;
	}

	public static void PlayStardust(Vector2 startPos, Vector2 targetPos)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!TestMode.IsOn)
		{
			SpawnAndLaunch(startPos, targetPos + VFXUtil.RandVec2(20f));
		}
	}

	public override void _ExitTree()
	{
		Tween? moveTween = _moveTween;
		if (moveTween != null)
		{
			moveTween.Kill();
		}
		Tween? rotationTween = _rotationTween;
		if (rotationTween != null)
		{
			rotationTween.Kill();
		}
		Tween? fadeTween = _fadeTween;
		if (fadeTween != null)
		{
			fadeTween.Kill();
		}
		((Node)this)._ExitTree();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Expected O, but got Unknown
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Expected O, but got Unknown
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(8)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Launch, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("from"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("to"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnHitTarget, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Finished, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetVisualActive, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("active"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SpawnAndLaunch, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("from"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("to"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)24, StringName.op_Implicit("parent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.PlayStardust, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("startPos"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("targetPos"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
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
		if ((ref method) == MethodName.Finished && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Finished();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetVisualActive && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetVisualActive(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SpawnAndLaunch && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			StardustVfx stardustVfx = SpawnAndLaunch(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<StardustVfx>(ref stardustVfx);
			return true;
		}
		if ((ref method) == MethodName.PlayStardust && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			PlayStardust(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
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
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.SpawnAndLaunch && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			StardustVfx stardustVfx = SpawnAndLaunch(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<StardustVfx>(ref stardustVfx);
			return true;
		}
		if ((ref method) == MethodName.PlayStardust && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			PlayStardust(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
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
		if ((ref method) == MethodName.Launch)
		{
			return true;
		}
		if ((ref method) == MethodName.OnHitTarget)
		{
			return true;
		}
		if ((ref method) == MethodName.Finished)
		{
			return true;
		}
		if ((ref method) == MethodName.SetVisualActive)
		{
			return true;
		}
		if ((ref method) == MethodName.SpawnAndLaunch)
		{
			return true;
		}
		if ((ref method) == MethodName.PlayStardust)
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
		if ((ref name) == PropertyName.MoveDuration)
		{
			MoveDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.FadeOutDuration)
		{
			FadeOutDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.StartScale)
		{
			StartScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EndScale)
		{
			EndScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.RotationAmount)
		{
			RotationAmount = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._trailParticles)
		{
			_trailParticles = VariantUtils.ConvertTo<GpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._glow)
		{
			_glow = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			_sprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._tintMaterial)
		{
			_tintMaterial = VariantUtils.ConvertTo<ShaderMaterial>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._moveTween)
		{
			_moveTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fadeTween)
		{
			_fadeTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rotationTween)
		{
			_rotationTween = VariantUtils.ConvertTo<Tween>(ref value);
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
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.MoveDuration)
		{
			float moveDuration = MoveDuration;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.FadeOutDuration)
		{
			float moveDuration = FadeOutDuration;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.StartScale)
		{
			float moveDuration = StartScale;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.EndScale)
		{
			float moveDuration = EndScale;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName.RotationAmount)
		{
			float moveDuration = RotationAmount;
			value = VariantUtils.CreateFrom<float>(ref moveDuration);
			return true;
		}
		if ((ref name) == PropertyName._trailParticles)
		{
			value = VariantUtils.CreateFrom<GpuParticles2D>(ref _trailParticles);
			return true;
		}
		if ((ref name) == PropertyName._glow)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _glow);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _sprite);
			return true;
		}
		if ((ref name) == PropertyName._tintMaterial)
		{
			value = VariantUtils.CreateFrom<ShaderMaterial>(ref _tintMaterial);
			return true;
		}
		if ((ref name) == PropertyName._moveTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _moveTween);
			return true;
		}
		if ((ref name) == PropertyName._fadeTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _fadeTween);
			return true;
		}
		if ((ref name) == PropertyName._rotationTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _rotationTween);
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
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.MoveDuration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.FadeOutDuration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.StartScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.EndScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.RotationAmount, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._trailParticles, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._glow, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._sprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._tintMaterial, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._moveTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fadeTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._rotationTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName moveDuration = PropertyName.MoveDuration;
		float moveDuration2 = MoveDuration;
		info.AddProperty(moveDuration, Variant.From<float>(ref moveDuration2));
		StringName fadeOutDuration = PropertyName.FadeOutDuration;
		moveDuration2 = FadeOutDuration;
		info.AddProperty(fadeOutDuration, Variant.From<float>(ref moveDuration2));
		StringName startScale = PropertyName.StartScale;
		moveDuration2 = StartScale;
		info.AddProperty(startScale, Variant.From<float>(ref moveDuration2));
		StringName endScale = PropertyName.EndScale;
		moveDuration2 = EndScale;
		info.AddProperty(endScale, Variant.From<float>(ref moveDuration2));
		StringName rotationAmount = PropertyName.RotationAmount;
		moveDuration2 = RotationAmount;
		info.AddProperty(rotationAmount, Variant.From<float>(ref moveDuration2));
		info.AddProperty(PropertyName._trailParticles, Variant.From<GpuParticles2D>(ref _trailParticles));
		info.AddProperty(PropertyName._glow, Variant.From<Sprite2D>(ref _glow));
		info.AddProperty(PropertyName._sprite, Variant.From<Sprite2D>(ref _sprite));
		info.AddProperty(PropertyName._tintMaterial, Variant.From<ShaderMaterial>(ref _tintMaterial));
		info.AddProperty(PropertyName._moveTween, Variant.From<Tween>(ref _moveTween));
		info.AddProperty(PropertyName._fadeTween, Variant.From<Tween>(ref _fadeTween));
		info.AddProperty(PropertyName._rotationTween, Variant.From<Tween>(ref _rotationTween));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.MoveDuration, ref val))
		{
			MoveDuration = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.FadeOutDuration, ref val2))
		{
			FadeOutDuration = ((Variant)(ref val2)).As<float>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.StartScale, ref val3))
		{
			StartScale = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.EndScale, ref val4))
		{
			EndScale = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.RotationAmount, ref val5))
		{
			RotationAmount = ((Variant)(ref val5)).As<float>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._trailParticles, ref val6))
		{
			_trailParticles = ((Variant)(ref val6)).As<GpuParticles2D>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._glow, ref val7))
		{
			_glow = ((Variant)(ref val7)).As<Sprite2D>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._sprite, ref val8))
		{
			_sprite = ((Variant)(ref val8)).As<Sprite2D>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._tintMaterial, ref val9))
		{
			_tintMaterial = ((Variant)(ref val9)).As<ShaderMaterial>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._moveTween, ref val10))
		{
			_moveTween = ((Variant)(ref val10)).As<Tween>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._fadeTween, ref val11))
		{
			_fadeTween = ((Variant)(ref val11)).As<Tween>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._rotationTween, ref val12))
		{
			_rotationTween = ((Variant)(ref val12)).As<Tween>();
		}
	}
}
