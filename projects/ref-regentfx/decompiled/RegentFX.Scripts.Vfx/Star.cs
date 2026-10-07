using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace RegentFX.Scripts.Vfx;

[ScriptPath("res://Scripts/Vfx/Star.cs")]
public class Star : Node2D
{
	public enum TintMode
	{
		Multiply,
		Screen,
		Overlay,
		HueShift
	}

	public class MethodName : MethodName
	{
		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName UpdateTrailState = StringName.op_Implicit("UpdateTrailState");

		public static readonly StringName ToggleTrail = StringName.op_Implicit("ToggleTrail");

		public static readonly StringName InitializeRandomValues = StringName.op_Implicit("InitializeRandomValues");

		public static readonly StringName ApplyInitialTransform = StringName.op_Implicit("ApplyInitialTransform");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName _Draw = StringName.op_Implicit("_Draw");

		public static readonly StringName UpdateScale = StringName.op_Implicit("UpdateScale");

		public static readonly StringName ReRandomize = StringName.op_Implicit("ReRandomize");

		public static readonly StringName SetBaseScale = StringName.op_Implicit("SetBaseScale");

		public static readonly StringName SetRotationSpeed = StringName.op_Implicit("SetRotationSpeed");

		public static readonly StringName SetPulseSpeed = StringName.op_Implicit("SetPulseSpeed");

		public static readonly StringName ResetColor = StringName.op_Implicit("ResetColor");

		public static readonly StringName ChangeColorImmediate = StringName.op_Implicit("ChangeColorImmediate");

		public static readonly StringName SetTintMode = StringName.op_Implicit("SetTintMode");

		public static readonly StringName UpdateTrailColor = StringName.op_Implicit("UpdateTrailColor");

		public static readonly StringName FadeOutAndDestroy = StringName.op_Implicit("FadeOutAndDestroy");

		public static readonly StringName DisconnectFrom = StringName.op_Implicit("DisconnectFrom");

		public static readonly StringName DisconnectAll = StringName.op_Implicit("DisconnectAll");

		public static readonly StringName IsConnectedTo = StringName.op_Implicit("IsConnectedTo");

		public static readonly StringName SetConnectionAlpha = StringName.op_Implicit("SetConnectionAlpha");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName RotationSpeed = StringName.op_Implicit("RotationSpeed");

		public static readonly StringName RandomRotationDirection = StringName.op_Implicit("RandomRotationDirection");

		public static readonly StringName RotationSpeedVariance = StringName.op_Implicit("RotationSpeedVariance");

		public static readonly StringName EnablePulse = StringName.op_Implicit("EnablePulse");

		public static readonly StringName PulseSpeed = StringName.op_Implicit("PulseSpeed");

		public static readonly StringName PulseMinScale = StringName.op_Implicit("PulseMinScale");

		public static readonly StringName PulseMaxScale = StringName.op_Implicit("PulseMaxScale");

		public static readonly StringName PulseSpeedVariance = StringName.op_Implicit("PulseSpeedVariance");

		public static readonly StringName EnableRandomOffset = StringName.op_Implicit("EnableRandomOffset");

		public static readonly StringName RandomRotationOffset = StringName.op_Implicit("RandomRotationOffset");

		public static readonly StringName RandomPulsePhaseOffset = StringName.op_Implicit("RandomPulsePhaseOffset");

		public static readonly StringName BaseScale = StringName.op_Implicit("BaseScale");

		public static readonly StringName EnableTrail = StringName.op_Implicit("EnableTrail");

		public static readonly StringName ConnectionLineWidth = StringName.op_Implicit("ConnectionLineWidth");

		public static readonly StringName ConnectionLineColor = StringName.op_Implicit("ConnectionLineColor");

		public static readonly StringName ConnectionLineAlpha = StringName.op_Implicit("ConnectionLineAlpha");

		public static readonly StringName EnableConnectionLinePulse = StringName.op_Implicit("EnableConnectionLinePulse");

		public static readonly StringName ConnectionLinePulseSpeed = StringName.op_Implicit("ConnectionLinePulseSpeed");

		public static readonly StringName ConnectionLinePulseMinAlpha = StringName.op_Implicit("ConnectionLinePulseMinAlpha");

		public static readonly StringName ConnectionLinePulseMaxAlpha = StringName.op_Implicit("ConnectionLinePulseMaxAlpha");

		public static readonly StringName _sprite = StringName.op_Implicit("_sprite");

		public static readonly StringName _trailParticles = StringName.op_Implicit("_trailParticles");

		public static readonly StringName _tintMaterial = StringName.op_Implicit("_tintMaterial");

		public static readonly StringName _actualRotationSpeed = StringName.op_Implicit("_actualRotationSpeed");

		public static readonly StringName _actualPulseSpeed = StringName.op_Implicit("_actualPulseSpeed");

		public static readonly StringName _pulsePhase = StringName.op_Implicit("_pulsePhase");

		public static readonly StringName _rotationPhase = StringName.op_Implicit("_rotationPhase");

		public static readonly StringName _rotationDirection = StringName.op_Implicit("_rotationDirection");

		public static readonly StringName _connectionLinePulsePhase = StringName.op_Implicit("_connectionLinePulsePhase");
	}

	public class SignalName : SignalName
	{
	}

	public static string VfxScenePath = "res://RegentFX/scenes/Star.tscn";

	private Sprite2D? _sprite;

	private GpuParticles2D? _trailParticles;

	private ShaderMaterial? _tintMaterial;

	private float _actualRotationSpeed;

	private float _actualPulseSpeed;

	private float _pulsePhase;

	private float _rotationPhase;

	private int _rotationDirection;

	private float _connectionLinePulsePhase;

	private readonly Dictionary<Star, float> _connections = new Dictionary<Star, float>();

	[Export(/*Could not decode attribute arguments.*/)]
	public float RotationSpeed { get; set; } = 90f;


	[Export(/*Could not decode attribute arguments.*/)]
	public bool RandomRotationDirection { get; set; } = true;


	[Export(/*Could not decode attribute arguments.*/)]
	public float RotationSpeedVariance { get; set; } = 30f;


	[Export(/*Could not decode attribute arguments.*/)]
	public bool EnablePulse { get; set; } = true;


	[Export(/*Could not decode attribute arguments.*/)]
	public float PulseSpeed { get; set; } = 2f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float PulseMinScale { get; set; } = 0.7f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float PulseMaxScale { get; set; } = 1.1f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float PulseSpeedVariance { get; set; } = 0.5f;


	[Export(/*Could not decode attribute arguments.*/)]
	public bool EnableRandomOffset { get; set; } = true;


	[Export(/*Could not decode attribute arguments.*/)]
	public float RandomRotationOffset { get; set; } = 360f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float RandomPulsePhaseOffset { get; set; } = 6.28f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float BaseScale { get; set; } = 1f;


	[Export(/*Could not decode attribute arguments.*/)]
	public bool EnableTrail { get; set; }

	[ExportGroup("Connection", "")]
	[Export(/*Could not decode attribute arguments.*/)]
	public float ConnectionLineWidth { get; set; } = 2f;


	[Export(/*Could not decode attribute arguments.*/)]
	public Color ConnectionLineColor { get; set; } = Colors.White;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ConnectionLineAlpha { get; set; } = 1f;


	[Export(/*Could not decode attribute arguments.*/)]
	public bool EnableConnectionLinePulse { get; set; } = true;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ConnectionLinePulseSpeed { get; set; } = 1.5f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ConnectionLinePulseMinAlpha { get; set; } = 0.3f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float ConnectionLinePulseMaxAlpha { get; set; } = 1f;


	public static Star Create()
	{
		return VFXUtil.GenVFXNode<Star>(VfxScenePath);
	}

	public override void _Ready()
	{
		_sprite = ((Node)this).GetNode<Sprite2D>(NodePath.op_Implicit("Sprite"));
		_trailParticles = ((Node)this).GetNodeOrNull<GpuParticles2D>(NodePath.op_Implicit("TrailParticles"));
		Sprite2D? sprite = _sprite;
		Material obj = ((sprite != null) ? ((CanvasItem)sprite).Material : null);
		Material obj2 = ((obj is ShaderMaterial) ? obj : null);
		ref ShaderMaterial? tintMaterial = ref _tintMaterial;
		Resource obj3 = ((obj2 != null) ? ((Resource)obj2).Duplicate(false) : null);
		tintMaterial = (ShaderMaterial?)(object)((obj3 is ShaderMaterial) ? obj3 : null);
		if (_sprite != null && _tintMaterial != null)
		{
			((CanvasItem)_sprite).Material = (Material)(object)_tintMaterial;
		}
		InitializeRandomValues();
		ApplyInitialTransform();
		UpdateTrailState();
	}

	private void UpdateTrailState()
	{
		if (_trailParticles != null)
		{
			((CanvasItem)_trailParticles).Visible = EnableTrail;
			_trailParticles.Emitting = EnableTrail;
		}
	}

	public void ToggleTrail(bool trail)
	{
		EnableTrail = trail;
		UpdateTrailState();
	}

	private void InitializeRandomValues()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		RandomNumberGenerator val = new RandomNumberGenerator();
		val.Randomize();
		_rotationDirection = ((!RandomRotationDirection) ? 1 : (val.RandiRange(0, 1) * 2 - 1));
		_actualRotationSpeed = RotationSpeed + val.RandfRange(0f - RotationSpeedVariance, RotationSpeedVariance);
		_actualRotationSpeed *= _rotationDirection;
		_rotationPhase = (EnableRandomOffset ? val.RandfRange(0f, RandomRotationOffset) : 0f);
		_actualPulseSpeed = PulseSpeed + val.RandfRange(0f - PulseSpeedVariance, PulseSpeedVariance);
		_pulsePhase = (EnableRandomOffset ? val.RandfRange(0f, RandomPulsePhaseOffset) : 0f);
	}

	private void ApplyInitialTransform()
	{
		((Node2D)this).RotationDegrees = _rotationPhase;
		UpdateScale(0f);
	}

	public override void _Process(double delta)
	{
		float num = (float)delta;
		if (Mathf.Abs(_actualRotationSpeed) > 0.001f)
		{
			((Node2D)this).RotationDegrees = ((Node2D)this).RotationDegrees + _actualRotationSpeed * num;
		}
		if (EnablePulse)
		{
			UpdateScale(num);
		}
		if (EnableConnectionLinePulse && _connections.Count > 0)
		{
			_connectionLinePulsePhase += ConnectionLinePulseSpeed * num;
			float num2 = (Mathf.Sin(_connectionLinePulsePhase) + 1f) / 2f;
			ConnectionLineAlpha = Mathf.Lerp(ConnectionLinePulseMinAlpha, ConnectionLinePulseMaxAlpha, num2);
		}
		if (_connections.Count > 0)
		{
			((CanvasItem)this).QueueRedraw();
		}
	}

	public override void _Draw()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		foreach (var (star2, num2) in _connections)
		{
			if (star2 != null && GodotObject.IsInstanceValid((GodotObject)(object)star2))
			{
				Vector2 globalPosition = ((Node2D)star2).GlobalPosition;
				Vector2 val = ((Node2D)this).ToLocal(((Node2D)this).GlobalPosition);
				Vector2 val2 = ((Node2D)this).ToLocal(globalPosition);
				Color connectionLineColor = ConnectionLineColor;
				connectionLineColor.A = num2 * ConnectionLineAlpha;
				((CanvasItem)this).DrawLine(val, val2, connectionLineColor, ConnectionLineWidth, false);
			}
		}
	}

	private void UpdateScale(float dt)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			_pulsePhase += _actualPulseSpeed * dt;
			float num = (Mathf.Sin(_pulsePhase) + 1f) / 2f;
			float num2 = Mathf.Lerp(PulseMinScale, PulseMaxScale, num) * BaseScale;
			((Node2D)_sprite).Scale = new Vector2(num2, num2);
		}
	}

	public void ReRandomize()
	{
		InitializeRandomValues();
	}

	public void SetBaseScale(float scale)
	{
		BaseScale = scale;
	}

	public void SetRotationSpeed(float speed)
	{
		RotationSpeed = speed;
		_actualRotationSpeed = speed * (float)_rotationDirection;
	}

	public void SetPulseSpeed(float speed)
	{
		PulseSpeed = speed;
		_actualPulseSpeed = speed;
	}

	public void ResetColor()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		ChangeColorImmediate(new Color(0f, 2.855f, 17.829f, 1f));
	}

	public void ChangeColorTo(Color color, float duration = 0.3f, TintMode? mode = null)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (_tintMaterial == null)
		{
			return;
		}
		if (mode.HasValue)
		{
			SetTintMode(mode.Value);
		}
		if (duration <= 0f)
		{
			_tintMaterial.SetShaderParameter(StringName.op_Implicit("tint_color"), Variant.op_Implicit(color));
			UpdateTrailColor(color);
			return;
		}
		Color from = (Color)_tintMaterial.GetShaderParameter(StringName.op_Implicit("tint_color"));
		Tween obj = ((Node)this).CreateTween();
		obj.SetTrans((TransitionType)4);
		obj.SetEase((EaseType)1);
		obj.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			if (_tintMaterial != null)
			{
				Color val = ((Color)(ref from)).Lerp(color, t);
				_tintMaterial.SetShaderParameter(StringName.op_Implicit("tint_color"), Variant.op_Implicit(val));
				UpdateTrailColor(val);
			}
		}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), (double)duration);
	}

	public void ChangeColorImmediate(Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		ChangeColorTo(color, -1f);
	}

	public void SetTintMode(TintMode mode)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (_tintMaterial != null)
		{
			_tintMaterial.SetShaderParameter(StringName.op_Implicit("tint_mode"), Variant.op_Implicit((int)mode));
		}
	}

	private void UpdateTrailColor(Color color)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		GpuParticles2D? trailParticles = _trailParticles;
		Material obj = ((trailParticles != null) ? trailParticles.ProcessMaterial : null);
		ParticleProcessMaterial val = (ParticleProcessMaterial)(object)((obj is ParticleProcessMaterial) ? obj : null);
		if (val != null)
		{
			color.A = 0.17254902f;
			val.Color = color;
		}
	}

	public async void FadeOutAndDestroy(float duration = 0.5f)
	{
		if (_sprite != null)
		{
			Tween val = ((Node)this).CreateTween();
			val.SetTrans((TransitionType)4);
			val.SetEase((EaseType)1);
			val.TweenProperty((GodotObject)(object)_sprite, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)duration);
			val.TweenProperty((GodotObject)(object)_sprite, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.Zero), (double)duration);
			await ((GodotObject)this).ToSignal((GodotObject)(object)val, SignalName.Finished);
			((Node)this).QueueFree();
		}
	}

	public void ConnectTo(Star target, float? alpha = null)
	{
		if (target != null && target != this && !_connections.ContainsKey(target))
		{
			_connections.Add(target, alpha ?? ConnectionLineAlpha);
		}
	}

	public void DisconnectFrom(Star target)
	{
		if (target != null)
		{
			_connections.Remove(target);
			((CanvasItem)this).QueueRedraw();
		}
	}

	public void DisconnectAll()
	{
		_connections.Clear();
		((CanvasItem)this).QueueRedraw();
	}

	public bool IsConnectedTo(Star target)
	{
		if (target != null)
		{
			return _connections.ContainsKey(target);
		}
		return false;
	}

	public void SetConnectionAlpha(Star target, float alpha)
	{
		if (_connections.ContainsKey(target))
		{
			_connections[target] = alpha;
		}
	}

	public IReadOnlyDictionary<Star, float> GetConnections()
	{
		return _connections;
	}

	public override void _ExitTree()
	{
		DisconnectAll();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_0524: Expected O, but got Unknown
		//IL_051f: Unknown result type (might be due to invalid IL or missing references)
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_0559: Unknown result type (might be due to invalid IL or missing references)
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b2: Expected O, but got Unknown
		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0606: Unknown result type (might be due to invalid IL or missing references)
		//IL_0611: Expected O, but got Unknown
		//IL_060c: Unknown result type (might be due to invalid IL or missing references)
		//IL_062d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0638: Unknown result type (might be due to invalid IL or missing references)
		//IL_065e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0667: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(23)
		{
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateTrailState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ToggleTrail, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("trail"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.InitializeRandomValues, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyInitialTransform, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Draw, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateScale, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("dt"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ReRandomize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetBaseScale, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("scale"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetRotationSpeed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("speed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetPulseSpeed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("speed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ResetColor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ChangeColorImmediate, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetTintMode, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("mode"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateTrailColor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.FadeOutAndDestroy, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("duration"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.DisconnectFrom, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("target"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.DisconnectAll, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.IsConnectedTo, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("target"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetConnectionAlpha, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("target"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false),
				new PropertyInfo((Type)3, StringName.op_Implicit("alpha"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Star star = Create();
			ret = VariantUtils.CreateFrom<Star>(ref star);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateTrailState && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateTrailState();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ToggleTrail && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ToggleTrail(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.InitializeRandomValues && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			InitializeRandomValues();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyInitialTransform && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyInitialTransform();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Draw && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((CanvasItem)this)._Draw();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateScale && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateScale(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReRandomize && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ReRandomize();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetBaseScale && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetBaseScale(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetRotationSpeed && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetRotationSpeed(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetPulseSpeed && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetPulseSpeed(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ResetColor && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ResetColor();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ChangeColorImmediate && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ChangeColorImmediate(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetTintMode && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetTintMode(VariantUtils.ConvertTo<TintMode>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateTrailColor && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateTrailColor(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.FadeOutAndDestroy && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			FadeOutAndDestroy(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.DisconnectFrom && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			DisconnectFrom(VariantUtils.ConvertTo<Star>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.DisconnectAll && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			DisconnectAll();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.IsConnectedTo && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = IsConnectedTo(VariantUtils.ConvertTo<Star>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.SetConnectionAlpha && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			SetConnectionAlpha(VariantUtils.ConvertTo<Star>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
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
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Star star = Create();
			ret = VariantUtils.CreateFrom<Star>(ref star);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Create)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateTrailState)
		{
			return true;
		}
		if ((ref method) == MethodName.ToggleTrail)
		{
			return true;
		}
		if ((ref method) == MethodName.InitializeRandomValues)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyInitialTransform)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName._Draw)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateScale)
		{
			return true;
		}
		if ((ref method) == MethodName.ReRandomize)
		{
			return true;
		}
		if ((ref method) == MethodName.SetBaseScale)
		{
			return true;
		}
		if ((ref method) == MethodName.SetRotationSpeed)
		{
			return true;
		}
		if ((ref method) == MethodName.SetPulseSpeed)
		{
			return true;
		}
		if ((ref method) == MethodName.ResetColor)
		{
			return true;
		}
		if ((ref method) == MethodName.ChangeColorImmediate)
		{
			return true;
		}
		if ((ref method) == MethodName.SetTintMode)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateTrailColor)
		{
			return true;
		}
		if ((ref method) == MethodName.FadeOutAndDestroy)
		{
			return true;
		}
		if ((ref method) == MethodName.DisconnectFrom)
		{
			return true;
		}
		if ((ref method) == MethodName.DisconnectAll)
		{
			return true;
		}
		if ((ref method) == MethodName.IsConnectedTo)
		{
			return true;
		}
		if ((ref method) == MethodName.SetConnectionAlpha)
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
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.RotationSpeed)
		{
			RotationSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.RandomRotationDirection)
		{
			RandomRotationDirection = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.RotationSpeedVariance)
		{
			RotationSpeedVariance = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EnablePulse)
		{
			EnablePulse = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.PulseSpeed)
		{
			PulseSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.PulseMinScale)
		{
			PulseMinScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.PulseMaxScale)
		{
			PulseMaxScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.PulseSpeedVariance)
		{
			PulseSpeedVariance = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EnableRandomOffset)
		{
			EnableRandomOffset = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.RandomRotationOffset)
		{
			RandomRotationOffset = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.RandomPulsePhaseOffset)
		{
			RandomPulsePhaseOffset = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.BaseScale)
		{
			BaseScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EnableTrail)
		{
			EnableTrail = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLineWidth)
		{
			ConnectionLineWidth = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLineColor)
		{
			ConnectionLineColor = VariantUtils.ConvertTo<Color>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLineAlpha)
		{
			ConnectionLineAlpha = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.EnableConnectionLinePulse)
		{
			EnableConnectionLinePulse = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLinePulseSpeed)
		{
			ConnectionLinePulseSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLinePulseMinAlpha)
		{
			ConnectionLinePulseMinAlpha = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLinePulseMaxAlpha)
		{
			ConnectionLinePulseMaxAlpha = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			_sprite = VariantUtils.ConvertTo<Sprite2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._trailParticles)
		{
			_trailParticles = VariantUtils.ConvertTo<GpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._tintMaterial)
		{
			_tintMaterial = VariantUtils.ConvertTo<ShaderMaterial>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._actualRotationSpeed)
		{
			_actualRotationSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._actualPulseSpeed)
		{
			_actualPulseSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._pulsePhase)
		{
			_pulsePhase = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rotationPhase)
		{
			_rotationPhase = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rotationDirection)
		{
			_rotationDirection = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._connectionLinePulsePhase)
		{
			_connectionLinePulsePhase = VariantUtils.ConvertTo<float>(ref value);
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
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.RotationSpeed)
		{
			float rotationSpeed = RotationSpeed;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.RandomRotationDirection)
		{
			bool randomRotationDirection = RandomRotationDirection;
			value = VariantUtils.CreateFrom<bool>(ref randomRotationDirection);
			return true;
		}
		if ((ref name) == PropertyName.RotationSpeedVariance)
		{
			float rotationSpeed = RotationSpeedVariance;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.EnablePulse)
		{
			bool randomRotationDirection = EnablePulse;
			value = VariantUtils.CreateFrom<bool>(ref randomRotationDirection);
			return true;
		}
		if ((ref name) == PropertyName.PulseSpeed)
		{
			float rotationSpeed = PulseSpeed;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.PulseMinScale)
		{
			float rotationSpeed = PulseMinScale;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.PulseMaxScale)
		{
			float rotationSpeed = PulseMaxScale;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.PulseSpeedVariance)
		{
			float rotationSpeed = PulseSpeedVariance;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.EnableRandomOffset)
		{
			bool randomRotationDirection = EnableRandomOffset;
			value = VariantUtils.CreateFrom<bool>(ref randomRotationDirection);
			return true;
		}
		if ((ref name) == PropertyName.RandomRotationOffset)
		{
			float rotationSpeed = RandomRotationOffset;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.RandomPulsePhaseOffset)
		{
			float rotationSpeed = RandomPulsePhaseOffset;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.BaseScale)
		{
			float rotationSpeed = BaseScale;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.EnableTrail)
		{
			bool randomRotationDirection = EnableTrail;
			value = VariantUtils.CreateFrom<bool>(ref randomRotationDirection);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLineWidth)
		{
			float rotationSpeed = ConnectionLineWidth;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLineColor)
		{
			Color connectionLineColor = ConnectionLineColor;
			value = VariantUtils.CreateFrom<Color>(ref connectionLineColor);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLineAlpha)
		{
			float rotationSpeed = ConnectionLineAlpha;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.EnableConnectionLinePulse)
		{
			bool randomRotationDirection = EnableConnectionLinePulse;
			value = VariantUtils.CreateFrom<bool>(ref randomRotationDirection);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLinePulseSpeed)
		{
			float rotationSpeed = ConnectionLinePulseSpeed;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLinePulseMinAlpha)
		{
			float rotationSpeed = ConnectionLinePulseMinAlpha;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName.ConnectionLinePulseMaxAlpha)
		{
			float rotationSpeed = ConnectionLinePulseMaxAlpha;
			value = VariantUtils.CreateFrom<float>(ref rotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName._sprite)
		{
			value = VariantUtils.CreateFrom<Sprite2D>(ref _sprite);
			return true;
		}
		if ((ref name) == PropertyName._trailParticles)
		{
			value = VariantUtils.CreateFrom<GpuParticles2D>(ref _trailParticles);
			return true;
		}
		if ((ref name) == PropertyName._tintMaterial)
		{
			value = VariantUtils.CreateFrom<ShaderMaterial>(ref _tintMaterial);
			return true;
		}
		if ((ref name) == PropertyName._actualRotationSpeed)
		{
			value = VariantUtils.CreateFrom<float>(ref _actualRotationSpeed);
			return true;
		}
		if ((ref name) == PropertyName._actualPulseSpeed)
		{
			value = VariantUtils.CreateFrom<float>(ref _actualPulseSpeed);
			return true;
		}
		if ((ref name) == PropertyName._pulsePhase)
		{
			value = VariantUtils.CreateFrom<float>(ref _pulsePhase);
			return true;
		}
		if ((ref name) == PropertyName._rotationPhase)
		{
			value = VariantUtils.CreateFrom<float>(ref _rotationPhase);
			return true;
		}
		if ((ref name) == PropertyName._rotationDirection)
		{
			value = VariantUtils.CreateFrom<int>(ref _rotationDirection);
			return true;
		}
		if ((ref name) == PropertyName._connectionLinePulsePhase)
		{
			value = VariantUtils.CreateFrom<float>(ref _connectionLinePulsePhase);
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
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.RotationSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)1, PropertyName.RandomRotationDirection, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.RotationSpeedVariance, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)1, PropertyName.EnablePulse, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.PulseSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.PulseMinScale, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.PulseMaxScale, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.PulseSpeedVariance, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)1, PropertyName.EnableRandomOffset, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.RandomRotationOffset, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.RandomPulsePhaseOffset, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.BaseScale, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)1, PropertyName.EnableTrail, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)0, StringName.op_Implicit("Connection"), (PropertyHint)0, "", (PropertyUsageFlags)64, true),
			new PropertyInfo((Type)3, PropertyName.ConnectionLineWidth, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)20, PropertyName.ConnectionLineColor, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ConnectionLineAlpha, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)1, PropertyName.EnableConnectionLinePulse, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ConnectionLinePulseSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ConnectionLinePulseMinAlpha, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.ConnectionLinePulseMaxAlpha, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._sprite, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._trailParticles, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._tintMaterial, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._actualRotationSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._actualPulseSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._pulsePhase, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._rotationPhase, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._rotationDirection, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._connectionLinePulsePhase, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName rotationSpeed = PropertyName.RotationSpeed;
		float rotationSpeed2 = RotationSpeed;
		info.AddProperty(rotationSpeed, Variant.From<float>(ref rotationSpeed2));
		StringName randomRotationDirection = PropertyName.RandomRotationDirection;
		bool randomRotationDirection2 = RandomRotationDirection;
		info.AddProperty(randomRotationDirection, Variant.From<bool>(ref randomRotationDirection2));
		StringName rotationSpeedVariance = PropertyName.RotationSpeedVariance;
		rotationSpeed2 = RotationSpeedVariance;
		info.AddProperty(rotationSpeedVariance, Variant.From<float>(ref rotationSpeed2));
		StringName enablePulse = PropertyName.EnablePulse;
		randomRotationDirection2 = EnablePulse;
		info.AddProperty(enablePulse, Variant.From<bool>(ref randomRotationDirection2));
		StringName pulseSpeed = PropertyName.PulseSpeed;
		rotationSpeed2 = PulseSpeed;
		info.AddProperty(pulseSpeed, Variant.From<float>(ref rotationSpeed2));
		StringName pulseMinScale = PropertyName.PulseMinScale;
		rotationSpeed2 = PulseMinScale;
		info.AddProperty(pulseMinScale, Variant.From<float>(ref rotationSpeed2));
		StringName pulseMaxScale = PropertyName.PulseMaxScale;
		rotationSpeed2 = PulseMaxScale;
		info.AddProperty(pulseMaxScale, Variant.From<float>(ref rotationSpeed2));
		StringName pulseSpeedVariance = PropertyName.PulseSpeedVariance;
		rotationSpeed2 = PulseSpeedVariance;
		info.AddProperty(pulseSpeedVariance, Variant.From<float>(ref rotationSpeed2));
		StringName enableRandomOffset = PropertyName.EnableRandomOffset;
		randomRotationDirection2 = EnableRandomOffset;
		info.AddProperty(enableRandomOffset, Variant.From<bool>(ref randomRotationDirection2));
		StringName randomRotationOffset = PropertyName.RandomRotationOffset;
		rotationSpeed2 = RandomRotationOffset;
		info.AddProperty(randomRotationOffset, Variant.From<float>(ref rotationSpeed2));
		StringName randomPulsePhaseOffset = PropertyName.RandomPulsePhaseOffset;
		rotationSpeed2 = RandomPulsePhaseOffset;
		info.AddProperty(randomPulsePhaseOffset, Variant.From<float>(ref rotationSpeed2));
		StringName baseScale = PropertyName.BaseScale;
		rotationSpeed2 = BaseScale;
		info.AddProperty(baseScale, Variant.From<float>(ref rotationSpeed2));
		StringName enableTrail = PropertyName.EnableTrail;
		randomRotationDirection2 = EnableTrail;
		info.AddProperty(enableTrail, Variant.From<bool>(ref randomRotationDirection2));
		StringName connectionLineWidth = PropertyName.ConnectionLineWidth;
		rotationSpeed2 = ConnectionLineWidth;
		info.AddProperty(connectionLineWidth, Variant.From<float>(ref rotationSpeed2));
		StringName connectionLineColor = PropertyName.ConnectionLineColor;
		Color connectionLineColor2 = ConnectionLineColor;
		info.AddProperty(connectionLineColor, Variant.From<Color>(ref connectionLineColor2));
		StringName connectionLineAlpha = PropertyName.ConnectionLineAlpha;
		rotationSpeed2 = ConnectionLineAlpha;
		info.AddProperty(connectionLineAlpha, Variant.From<float>(ref rotationSpeed2));
		StringName enableConnectionLinePulse = PropertyName.EnableConnectionLinePulse;
		randomRotationDirection2 = EnableConnectionLinePulse;
		info.AddProperty(enableConnectionLinePulse, Variant.From<bool>(ref randomRotationDirection2));
		StringName connectionLinePulseSpeed = PropertyName.ConnectionLinePulseSpeed;
		rotationSpeed2 = ConnectionLinePulseSpeed;
		info.AddProperty(connectionLinePulseSpeed, Variant.From<float>(ref rotationSpeed2));
		StringName connectionLinePulseMinAlpha = PropertyName.ConnectionLinePulseMinAlpha;
		rotationSpeed2 = ConnectionLinePulseMinAlpha;
		info.AddProperty(connectionLinePulseMinAlpha, Variant.From<float>(ref rotationSpeed2));
		StringName connectionLinePulseMaxAlpha = PropertyName.ConnectionLinePulseMaxAlpha;
		rotationSpeed2 = ConnectionLinePulseMaxAlpha;
		info.AddProperty(connectionLinePulseMaxAlpha, Variant.From<float>(ref rotationSpeed2));
		info.AddProperty(PropertyName._sprite, Variant.From<Sprite2D>(ref _sprite));
		info.AddProperty(PropertyName._trailParticles, Variant.From<GpuParticles2D>(ref _trailParticles));
		info.AddProperty(PropertyName._tintMaterial, Variant.From<ShaderMaterial>(ref _tintMaterial));
		info.AddProperty(PropertyName._actualRotationSpeed, Variant.From<float>(ref _actualRotationSpeed));
		info.AddProperty(PropertyName._actualPulseSpeed, Variant.From<float>(ref _actualPulseSpeed));
		info.AddProperty(PropertyName._pulsePhase, Variant.From<float>(ref _pulsePhase));
		info.AddProperty(PropertyName._rotationPhase, Variant.From<float>(ref _rotationPhase));
		info.AddProperty(PropertyName._rotationDirection, Variant.From<int>(ref _rotationDirection));
		info.AddProperty(PropertyName._connectionLinePulsePhase, Variant.From<float>(ref _connectionLinePulsePhase));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.RotationSpeed, ref val))
		{
			RotationSpeed = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.RandomRotationDirection, ref val2))
		{
			RandomRotationDirection = ((Variant)(ref val2)).As<bool>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.RotationSpeedVariance, ref val3))
		{
			RotationSpeedVariance = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.EnablePulse, ref val4))
		{
			EnablePulse = ((Variant)(ref val4)).As<bool>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.PulseSpeed, ref val5))
		{
			PulseSpeed = ((Variant)(ref val5)).As<float>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName.PulseMinScale, ref val6))
		{
			PulseMinScale = ((Variant)(ref val6)).As<float>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName.PulseMaxScale, ref val7))
		{
			PulseMaxScale = ((Variant)(ref val7)).As<float>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName.PulseSpeedVariance, ref val8))
		{
			PulseSpeedVariance = ((Variant)(ref val8)).As<float>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName.EnableRandomOffset, ref val9))
		{
			EnableRandomOffset = ((Variant)(ref val9)).As<bool>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName.RandomRotationOffset, ref val10))
		{
			RandomRotationOffset = ((Variant)(ref val10)).As<float>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName.RandomPulsePhaseOffset, ref val11))
		{
			RandomPulsePhaseOffset = ((Variant)(ref val11)).As<float>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName.BaseScale, ref val12))
		{
			BaseScale = ((Variant)(ref val12)).As<float>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName.EnableTrail, ref val13))
		{
			EnableTrail = ((Variant)(ref val13)).As<bool>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName.ConnectionLineWidth, ref val14))
		{
			ConnectionLineWidth = ((Variant)(ref val14)).As<float>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName.ConnectionLineColor, ref val15))
		{
			ConnectionLineColor = ((Variant)(ref val15)).As<Color>();
		}
		Variant val16 = default(Variant);
		if (info.TryGetProperty(PropertyName.ConnectionLineAlpha, ref val16))
		{
			ConnectionLineAlpha = ((Variant)(ref val16)).As<float>();
		}
		Variant val17 = default(Variant);
		if (info.TryGetProperty(PropertyName.EnableConnectionLinePulse, ref val17))
		{
			EnableConnectionLinePulse = ((Variant)(ref val17)).As<bool>();
		}
		Variant val18 = default(Variant);
		if (info.TryGetProperty(PropertyName.ConnectionLinePulseSpeed, ref val18))
		{
			ConnectionLinePulseSpeed = ((Variant)(ref val18)).As<float>();
		}
		Variant val19 = default(Variant);
		if (info.TryGetProperty(PropertyName.ConnectionLinePulseMinAlpha, ref val19))
		{
			ConnectionLinePulseMinAlpha = ((Variant)(ref val19)).As<float>();
		}
		Variant val20 = default(Variant);
		if (info.TryGetProperty(PropertyName.ConnectionLinePulseMaxAlpha, ref val20))
		{
			ConnectionLinePulseMaxAlpha = ((Variant)(ref val20)).As<float>();
		}
		Variant val21 = default(Variant);
		if (info.TryGetProperty(PropertyName._sprite, ref val21))
		{
			_sprite = ((Variant)(ref val21)).As<Sprite2D>();
		}
		Variant val22 = default(Variant);
		if (info.TryGetProperty(PropertyName._trailParticles, ref val22))
		{
			_trailParticles = ((Variant)(ref val22)).As<GpuParticles2D>();
		}
		Variant val23 = default(Variant);
		if (info.TryGetProperty(PropertyName._tintMaterial, ref val23))
		{
			_tintMaterial = ((Variant)(ref val23)).As<ShaderMaterial>();
		}
		Variant val24 = default(Variant);
		if (info.TryGetProperty(PropertyName._actualRotationSpeed, ref val24))
		{
			_actualRotationSpeed = ((Variant)(ref val24)).As<float>();
		}
		Variant val25 = default(Variant);
		if (info.TryGetProperty(PropertyName._actualPulseSpeed, ref val25))
		{
			_actualPulseSpeed = ((Variant)(ref val25)).As<float>();
		}
		Variant val26 = default(Variant);
		if (info.TryGetProperty(PropertyName._pulsePhase, ref val26))
		{
			_pulsePhase = ((Variant)(ref val26)).As<float>();
		}
		Variant val27 = default(Variant);
		if (info.TryGetProperty(PropertyName._rotationPhase, ref val27))
		{
			_rotationPhase = ((Variant)(ref val27)).As<float>();
		}
		Variant val28 = default(Variant);
		if (info.TryGetProperty(PropertyName._rotationDirection, ref val28))
		{
			_rotationDirection = ((Variant)(ref val28)).As<int>();
		}
		Variant val29 = default(Variant);
		if (info.TryGetProperty(PropertyName._connectionLinePulsePhase, ref val29))
		{
			_connectionLinePulsePhase = ((Variant)(ref val29)).As<float>();
		}
	}
}
