using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx;

[GlobalClass]
[ScriptPath("res://Scripts/Vfx/Blackhole.cs")]
public class Blackhole : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName Burst = StringName.op_Implicit("Burst");

		public static readonly StringName UpdateBurst = StringName.op_Implicit("UpdateBurst");

		public static readonly StringName EndBurst = StringName.op_Implicit("EndBurst");

		public static readonly StringName SetSize = StringName.op_Implicit("SetSize");

		public static readonly StringName SetPulseFrequency = StringName.op_Implicit("SetPulseFrequency");

		public static readonly StringName SetPulseIntensity = StringName.op_Implicit("SetPulseIntensity");

		public static readonly StringName SetSwirlStrength = StringName.op_Implicit("SetSwirlStrength");

		public static readonly StringName UpdateCoreShaderParams = StringName.op_Implicit("UpdateCoreShaderParams");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName BlackholeSize = StringName.op_Implicit("BlackholeSize");

		public static readonly StringName PulseFrequency = StringName.op_Implicit("PulseFrequency");

		public static readonly StringName PulseIntensity = StringName.op_Implicit("PulseIntensity");

		public static readonly StringName SwirlStrength = StringName.op_Implicit("SwirlStrength");

		public static readonly StringName _coreSprite = StringName.op_Implicit("_coreSprite");

		public static readonly StringName _burstSprite = StringName.op_Implicit("_burstSprite");

		public static readonly StringName _coreMaterial = StringName.op_Implicit("_coreMaterial");

		public static readonly StringName _burstMaterial = StringName.op_Implicit("_burstMaterial");

		public static readonly StringName _isBursting = StringName.op_Implicit("_isBursting");

		public static readonly StringName _burstTimer = StringName.op_Implicit("_burstTimer");

		public static readonly StringName _baseScale = StringName.op_Implicit("_baseScale");

		public static readonly StringName _basePulseFrequency = StringName.op_Implicit("_basePulseFrequency");

		public static readonly StringName _basePulseIntensity = StringName.op_Implicit("_basePulseIntensity");
	}

	public class SignalName : SignalName
	{
	}

	private const float BURST_DURATION = 0.4f;

	private const float BURST_PEAK_TIME = 0.15f;

	private const float BURST_MAX_SCALE = 1.5f;

	private const float BURST_MAX_DISTORTION = 0.15f;

	private const float BURST_RADIUS = 300f;

	private const float BURST_PULSE_MULTIPLIER = 3f;

	private Sprite2D? _coreSprite;

	private Sprite2D? _burstSprite;

	private ShaderMaterial? _coreMaterial;

	private ShaderMaterial? _burstMaterial;

	private bool _isBursting;

	private float _burstTimer;

	private float _baseScale = 1f;

	private float _basePulseFrequency = 1.5f;

	private float _basePulseIntensity = 0.15f;

	public const float DEFAULT_SIZE = 100f;

	public static string VfxScenePath = "res://RegentFX/scenes/vfx/Blackhole.tscn";

	public static Dictionary<Creature, Blackhole> Blackholes = new Dictionary<Creature, Blackhole>();

	[Export(/*Could not decode attribute arguments.*/)]
	public float BlackholeSize { get; set; } = 100f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float PulseFrequency { get; set; } = 1.5f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float PulseIntensity { get; set; } = 0.15f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float SwirlStrength { get; set; } = 0.8f;


	public override void _Ready()
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		_coreSprite = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Core"));
		_burstSprite = ((Node)this).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("BurstDistortion"));
		if (_coreSprite != null)
		{
			Material material = ((CanvasItem)_coreSprite).Material;
			Material obj = ((material is ShaderMaterial) ? material : null);
			ref ShaderMaterial? coreMaterial = ref _coreMaterial;
			Resource obj2 = ((obj != null) ? ((Resource)obj).Duplicate(false) : null);
			coreMaterial = (ShaderMaterial?)(object)((obj2 is ShaderMaterial) ? obj2 : null);
			if (_coreMaterial != null)
			{
				((CanvasItem)_coreSprite).Material = (Material)(object)_coreMaterial;
				UpdateCoreShaderParams();
			}
		}
		if (_burstSprite != null)
		{
			Material material2 = ((CanvasItem)_burstSprite).Material;
			Material obj3 = ((material2 is ShaderMaterial) ? material2 : null);
			ref ShaderMaterial? burstMaterial = ref _burstMaterial;
			Resource obj4 = ((obj3 != null) ? ((Resource)obj3).Duplicate(false) : null);
			burstMaterial = (ShaderMaterial?)(object)((obj4 is ShaderMaterial) ? obj4 : null);
			if (_burstMaterial != null)
			{
				((CanvasItem)_burstSprite).Material = (Material)(object)_burstMaterial;
				((CanvasItem)_burstSprite).Visible = false;
			}
		}
		_baseScale = ((Node2D)this).Scale.X;
		_basePulseFrequency = PulseFrequency;
		_basePulseIntensity = PulseIntensity;
	}

	public override void _Process(double delta)
	{
		float num = (float)delta;
		if (_isBursting)
		{
			_burstTimer += num;
			UpdateBurst(num);
		}
		else
		{
			UpdateCoreShaderParams();
		}
	}

	public void Burst()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (!_isBursting)
		{
			FmodLite.Play("event:/RegentFx/sfx/black_hole_1");
			_isBursting = true;
			_burstTimer = 0f;
			_baseScale = ((Node2D)this).Scale.X;
			if (_burstSprite != null)
			{
				((CanvasItem)_burstSprite).Visible = true;
				((Node2D)_burstSprite).GlobalPosition = ((Node2D)this).GlobalPosition;
			}
		}
	}

	private void UpdateBurst(float dt)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		if (_burstTimer / 0.4f >= 1f)
		{
			EndBurst();
			return;
		}
		float num = Mathf.Min(_burstTimer / 0.15f, 1f);
		float num2 = Mathf.Ease(num, 0f);
		float num3 = Mathf.Max(0f, (_burstTimer - 0.15f) / 0.25f);
		float num4 = 1f - Mathf.Ease(num3, 1f);
		float num5 = ((num < 1f) ? num2 : num4);
		float num6 = Mathf.Lerp(_baseScale, _baseScale * 1.5f, num5);
		((Node2D)this).Scale = new Vector2(num6, num6);
		float num7 = _basePulseFrequency * 3f;
		float num8 = _basePulseIntensity * (1f + num5 * 2f);
		if (_coreMaterial != null)
		{
			_coreMaterial.SetShaderParameter(StringName.op_Implicit("pulse_frequency"), Variant.op_Implicit(num7));
			_coreMaterial.SetShaderParameter(StringName.op_Implicit("pulse_intensity"), Variant.op_Implicit(num8));
			_coreMaterial.SetShaderParameter(StringName.op_Implicit("swirl_strength"), Variant.op_Implicit(SwirlStrength * (1f + num5)));
		}
		if (_burstMaterial != null)
		{
			float num9 = 0.15f * num5;
			_burstMaterial.SetShaderParameter(StringName.op_Implicit("distortion_intensity"), Variant.op_Implicit(num9));
			_burstMaterial.SetShaderParameter(StringName.op_Implicit("distortion_radius"), Variant.op_Implicit(300f));
			_burstMaterial.SetShaderParameter(StringName.op_Implicit("center_uv"), Variant.op_Implicit(new Vector2(0.5f, 0.5f)));
		}
		if (_burstSprite != null)
		{
			((Node2D)_burstSprite).GlobalPosition = ((Node2D)this).GlobalPosition;
		}
	}

	private void EndBurst()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		_isBursting = false;
		_burstTimer = 0f;
		((Node2D)this).Scale = new Vector2(_baseScale, _baseScale);
		if (_burstSprite != null)
		{
			((CanvasItem)_burstSprite).Visible = false;
		}
		UpdateCoreShaderParams();
		Entry.Logger.Debug("[Blackhole] Burst ended, returning to normal state", 1);
	}

	public void SetSize(float size)
	{
		BlackholeSize = size;
		UpdateCoreShaderParams();
	}

	public void SetPulseFrequency(float freq)
	{
		PulseFrequency = freq;
		_basePulseFrequency = freq;
		if (!_isBursting)
		{
			UpdateCoreShaderParams();
		}
	}

	public void SetPulseIntensity(float intensity)
	{
		PulseIntensity = intensity;
		_basePulseIntensity = intensity;
		if (!_isBursting)
		{
			UpdateCoreShaderParams();
		}
	}

	public void SetSwirlStrength(float strength)
	{
		SwirlStrength = strength;
		if (!_isBursting)
		{
			UpdateCoreShaderParams();
		}
	}

	private void UpdateCoreShaderParams()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (_coreMaterial != null)
		{
			_coreMaterial.SetShaderParameter(StringName.op_Implicit("blackhole_size"), Variant.op_Implicit(BlackholeSize));
			_coreMaterial.SetShaderParameter(StringName.op_Implicit("pulse_frequency"), Variant.op_Implicit(PulseFrequency));
			_coreMaterial.SetShaderParameter(StringName.op_Implicit("pulse_intensity"), Variant.op_Implicit(PulseIntensity));
			_coreMaterial.SetShaderParameter(StringName.op_Implicit("swirl_strength"), Variant.op_Implicit(SwirlStrength));
		}
	}

	public static Blackhole? Create(Creature creature, float size = 100f)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (TestMode.IsOn)
		{
			return null;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (val == null)
		{
			return null;
		}
		try
		{
			Blackhole blackhole = VFXUtil.GenVFXNode<Blackhole>(VfxScenePath);
			NCombatRoom instance2 = NCombatRoom.Instance;
			Node val2 = (Node)(object)((instance2 != null) ? instance2.BackCombatVfxContainer : null);
			if (val2 == null)
			{
				Entry.Logger.Warn("[Blackhole] No BackCombatVfxContainer available for blackhole", 1);
				((Node)blackhole).QueueFree();
				return null;
			}
			GodotTreeExtensions.AddChildSafely(val2, (Node)(object)blackhole);
			((Node2D)blackhole).GlobalPosition = val.VfxSpawnPosition;
			blackhole.SetSize(size);
			Blackholes[creature] = blackhole;
			return blackhole;
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[Blackhole] Failed to create blackhole: " + ex.Message, 1);
			return null;
		}
	}

	public override void _ExitTree()
	{
		((Node)this)._ExitTree();
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
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(11)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Burst, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateBurst, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("dt"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.EndBurst, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetSize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("size"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetPulseFrequency, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("freq"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetPulseIntensity, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("intensity"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetSwirlStrength, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("strength"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateCoreShaderParams, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
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
		if ((ref method) == MethodName.Burst && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Burst();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateBurst && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateBurst(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.EndBurst && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			EndBurst();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetSize && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetSize(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetPulseFrequency && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetPulseFrequency(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetPulseIntensity && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetPulseIntensity(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetSwirlStrength && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetSwirlStrength(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateCoreShaderParams && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateCoreShaderParams();
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
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.Burst)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateBurst)
		{
			return true;
		}
		if ((ref method) == MethodName.EndBurst)
		{
			return true;
		}
		if ((ref method) == MethodName.SetSize)
		{
			return true;
		}
		if ((ref method) == MethodName.SetPulseFrequency)
		{
			return true;
		}
		if ((ref method) == MethodName.SetPulseIntensity)
		{
			return true;
		}
		if ((ref method) == MethodName.SetSwirlStrength)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateCoreShaderParams)
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
		if ((ref name) == PropertyName.BlackholeSize)
		{
			BlackholeSize = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.PulseFrequency)
		{
			PulseFrequency = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.PulseIntensity)
		{
			PulseIntensity = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.SwirlStrength)
		{
			SwirlStrength = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._coreSprite)
		{
			_coreSprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._burstSprite)
		{
			_burstSprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._coreMaterial)
		{
			_coreMaterial = VariantUtils.ConvertTo<ShaderMaterial>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._burstMaterial)
		{
			_burstMaterial = VariantUtils.ConvertTo<ShaderMaterial>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isBursting)
		{
			_isBursting = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._burstTimer)
		{
			_burstTimer = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._baseScale)
		{
			_baseScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._basePulseFrequency)
		{
			_basePulseFrequency = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._basePulseIntensity)
		{
			_basePulseIntensity = VariantUtils.ConvertTo<float>(ref value);
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
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.BlackholeSize)
		{
			float blackholeSize = BlackholeSize;
			value = VariantUtils.CreateFrom<float>(ref blackholeSize);
			return true;
		}
		if ((ref name) == PropertyName.PulseFrequency)
		{
			float blackholeSize = PulseFrequency;
			value = VariantUtils.CreateFrom<float>(ref blackholeSize);
			return true;
		}
		if ((ref name) == PropertyName.PulseIntensity)
		{
			float blackholeSize = PulseIntensity;
			value = VariantUtils.CreateFrom<float>(ref blackholeSize);
			return true;
		}
		if ((ref name) == PropertyName.SwirlStrength)
		{
			float blackholeSize = SwirlStrength;
			value = VariantUtils.CreateFrom<float>(ref blackholeSize);
			return true;
		}
		if ((ref name) == PropertyName._coreSprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _coreSprite);
			return true;
		}
		if ((ref name) == PropertyName._burstSprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _burstSprite);
			return true;
		}
		if ((ref name) == PropertyName._coreMaterial)
		{
			value = VariantUtils.CreateFrom<ShaderMaterial>(ref _coreMaterial);
			return true;
		}
		if ((ref name) == PropertyName._burstMaterial)
		{
			value = VariantUtils.CreateFrom<ShaderMaterial>(ref _burstMaterial);
			return true;
		}
		if ((ref name) == PropertyName._isBursting)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isBursting);
			return true;
		}
		if ((ref name) == PropertyName._burstTimer)
		{
			value = VariantUtils.CreateFrom<float>(ref _burstTimer);
			return true;
		}
		if ((ref name) == PropertyName._baseScale)
		{
			value = VariantUtils.CreateFrom<float>(ref _baseScale);
			return true;
		}
		if ((ref name) == PropertyName._basePulseFrequency)
		{
			value = VariantUtils.CreateFrom<float>(ref _basePulseFrequency);
			return true;
		}
		if ((ref name) == PropertyName._basePulseIntensity)
		{
			value = VariantUtils.CreateFrom<float>(ref _basePulseIntensity);
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
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.BlackholeSize, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.PulseFrequency, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.PulseIntensity, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.SwirlStrength, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._coreSprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._burstSprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._coreMaterial, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._burstMaterial, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isBursting, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._burstTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._baseScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._basePulseFrequency, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._basePulseIntensity, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName blackholeSize = PropertyName.BlackholeSize;
		float blackholeSize2 = BlackholeSize;
		info.AddProperty(blackholeSize, Variant.From<float>(ref blackholeSize2));
		StringName pulseFrequency = PropertyName.PulseFrequency;
		blackholeSize2 = PulseFrequency;
		info.AddProperty(pulseFrequency, Variant.From<float>(ref blackholeSize2));
		StringName pulseIntensity = PropertyName.PulseIntensity;
		blackholeSize2 = PulseIntensity;
		info.AddProperty(pulseIntensity, Variant.From<float>(ref blackholeSize2));
		StringName swirlStrength = PropertyName.SwirlStrength;
		blackholeSize2 = SwirlStrength;
		info.AddProperty(swirlStrength, Variant.From<float>(ref blackholeSize2));
		info.AddProperty(PropertyName._coreSprite, Variant.From<Sprite2D>(ref _coreSprite));
		info.AddProperty(PropertyName._burstSprite, Variant.From<Sprite2D>(ref _burstSprite));
		info.AddProperty(PropertyName._coreMaterial, Variant.From<ShaderMaterial>(ref _coreMaterial));
		info.AddProperty(PropertyName._burstMaterial, Variant.From<ShaderMaterial>(ref _burstMaterial));
		info.AddProperty(PropertyName._isBursting, Variant.From<bool>(ref _isBursting));
		info.AddProperty(PropertyName._burstTimer, Variant.From<float>(ref _burstTimer));
		info.AddProperty(PropertyName._baseScale, Variant.From<float>(ref _baseScale));
		info.AddProperty(PropertyName._basePulseFrequency, Variant.From<float>(ref _basePulseFrequency));
		info.AddProperty(PropertyName._basePulseIntensity, Variant.From<float>(ref _basePulseIntensity));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.BlackholeSize, ref val))
		{
			BlackholeSize = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.PulseFrequency, ref val2))
		{
			PulseFrequency = ((Variant)(ref val2)).As<float>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.PulseIntensity, ref val3))
		{
			PulseIntensity = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.SwirlStrength, ref val4))
		{
			SwirlStrength = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._coreSprite, ref val5))
		{
			_coreSprite = ((Variant)(ref val5)).As<Sprite2D>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._burstSprite, ref val6))
		{
			_burstSprite = ((Variant)(ref val6)).As<Sprite2D>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._coreMaterial, ref val7))
		{
			_coreMaterial = ((Variant)(ref val7)).As<ShaderMaterial>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._burstMaterial, ref val8))
		{
			_burstMaterial = ((Variant)(ref val8)).As<ShaderMaterial>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._isBursting, ref val9))
		{
			_isBursting = ((Variant)(ref val9)).As<bool>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._burstTimer, ref val10))
		{
			_burstTimer = ((Variant)(ref val10)).As<float>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._baseScale, ref val11))
		{
			_baseScale = ((Variant)(ref val11)).As<float>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._basePulseFrequency, ref val12))
		{
			_basePulseFrequency = ((Variant)(ref val12)).As<float>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._basePulseIntensity, ref val13))
		{
			_basePulseIntensity = ((Variant)(ref val13)).As<float>();
		}
	}
}
