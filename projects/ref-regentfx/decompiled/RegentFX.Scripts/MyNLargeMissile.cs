using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts;

[ScriptPath("res://Scripts/MyNLargeMissile.cs")]
[ScriptPath("res://Scripts/MyNLargeMissile.cs")]
public class MyNLargeMissile : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName GetProjectileDirection = StringName.op_Implicit("GetProjectileDirection");

		public static readonly StringName GetTopPosition = StringName.op_Implicit("GetTopPosition");

		public static readonly StringName Initialize = StringName.op_Implicit("Initialize");

		public static readonly StringName ModulateParticles = StringName.op_Implicit("ModulateParticles");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName WaitTime = StringName.op_Implicit("WaitTime");

		public static readonly StringName _anticipationParticles = StringName.op_Implicit("_anticipationParticles");

		public static readonly StringName _projectileStartParticles = StringName.op_Implicit("_projectileStartParticles");

		public static readonly StringName _projectileParticles = StringName.op_Implicit("_projectileParticles");

		public static readonly StringName _impactParticles = StringName.op_Implicit("_impactParticles");

		public static readonly StringName _modulateParticles = StringName.op_Implicit("_modulateParticles");

		public static readonly StringName _anticipationContainer = StringName.op_Implicit("_anticipationContainer");

		public static readonly StringName _anticipationDuration = StringName.op_Implicit("_anticipationDuration");

		public static readonly StringName _projectileContainer = StringName.op_Implicit("_projectileContainer");

		public static readonly StringName _projectileStartPoint = StringName.op_Implicit("_projectileStartPoint");

		public static readonly StringName _projectileEndPoint = StringName.op_Implicit("_projectileEndPoint");

		public static readonly StringName _projectileOffset = StringName.op_Implicit("_projectileOffset");
	}

	public class SignalName : SignalName
	{
	}

	public static readonly string scenePath = SceneHelper.GetScenePath("vfx/vfx_large_magic_missile");

	[Export(/*Could not decode attribute arguments.*/)]
	public Array<GpuParticles2D> _anticipationParticles = new Array<GpuParticles2D>();

	[Export(/*Could not decode attribute arguments.*/)]
	public Array<GpuParticles2D> _projectileStartParticles = new Array<GpuParticles2D>();

	[Export(/*Could not decode attribute arguments.*/)]
	public Array<GpuParticles2D> _projectileParticles = new Array<GpuParticles2D>();

	[Export(/*Could not decode attribute arguments.*/)]
	public Array<GpuParticles2D> _impactParticles = new Array<GpuParticles2D>();

	[Export(/*Could not decode attribute arguments.*/)]
	public Array<GpuParticles2D> _modulateParticles = new Array<GpuParticles2D>();

	[Export(/*Could not decode attribute arguments.*/)]
	public Node2D? _anticipationContainer;

	[Export(/*Could not decode attribute arguments.*/)]
	public float _anticipationDuration = 0.2f;

	[Export(/*Could not decode attribute arguments.*/)]
	public Node2D? _projectileContainer;

	[Export(/*Could not decode attribute arguments.*/)]
	public Node2D? _projectileStartPoint;

	[Export(/*Could not decode attribute arguments.*/)]
	public Node2D? _projectileEndPoint;

	[Export(/*Could not decode attribute arguments.*/)]
	public float _projectileOffset = 100f;

	public CancellationTokenSource? _cts;

	[field: Export(/*Could not decode attribute arguments.*/)]
	public float WaitTime { get; set; } = 0.2f;


	public static MyNLargeMissile? Create(Vector2 targetFloorPosition, Color tint)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (TestMode.IsOn)
		{
			return null;
		}
		MyNLargeMissile myNLargeMissile = PreloadManager.Cache.GetScene(scenePath).Instantiate<MyNLargeMissile>((GenEditState)0);
		((Node2D)myNLargeMissile).GlobalPosition = targetFloorPosition;
		myNLargeMissile.Initialize();
		myNLargeMissile.ModulateParticles(tint);
		return myNLargeMissile;
	}

	public Vector2 GetProjectileDirection()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Quaternion.FromEuler(new Vector3(0f, 0f, Mathf.DegToRad(-30f))) * Vector3.Up;
		Vector2 val2 = new Vector2(val.X, val.Y);
		return ((Vector2)(ref val2)).Normalized();
	}

	public Vector2 GetTopPosition(Vector2 projectileDirection)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		return (Vector2)Geometry2D.LineIntersectsLine(((Node2D)this).GlobalPosition, projectileDirection, new Vector2(0f, 80f), Vector2.Right);
	}

	public void Initialize()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		Vector2 projectileDirection = GetProjectileDirection();
		Vector2 topPosition = GetTopPosition(projectileDirection);
		_anticipationContainer.GlobalPosition = topPosition;
		_projectileStartPoint.GlobalPosition = topPosition + projectileDirection * _projectileOffset;
		_projectileEndPoint.GlobalPosition = ((Node2D)this).GlobalPosition + projectileDirection * _projectileOffset;
		((CanvasItem)_projectileContainer).Visible = false;
	}

	public void ModulateParticles(Color tint)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < _modulateParticles.Count; i++)
		{
			((CanvasItem)_modulateParticles[i]).SelfModulate = tint;
		}
	}

	public override void _Ready()
	{
		TaskHelper.RunSafely(PlaySequence());
	}

	public override void _ExitTree()
	{
		_cts?.Cancel();
		_cts?.Dispose();
	}

	public async Task PlaySequence()
	{
		MyNLargeMissile node = this;
		node._cts = new CancellationTokenSource();
		for (int i = 0; i < node._anticipationParticles.Count; i++)
		{
			node._anticipationParticles[i].Restart();
		}
		await Cmd.Wait(node._anticipationDuration, node._cts.Token, false);
		for (int j = 0; j < node._projectileStartParticles.Count; j++)
		{
			node._projectileStartParticles[j].Restart();
		}
		node._projectileContainer.GlobalPosition = node._projectileStartPoint.GlobalPosition;
		((CanvasItem)node._projectileContainer).Visible = true;
		for (int k = 0; k < node._projectileParticles.Count; k++)
		{
			node._projectileParticles[k].Restart();
		}
		double timer = 0.0;
		while (timer < (double)node.WaitTime && !node._cts.IsCancellationRequested)
		{
			float num = (float)timer / node.WaitTime;
			Node2D? projectileContainer = node._projectileContainer;
			Vector2 globalPosition = node._projectileStartPoint.GlobalPosition;
			projectileContainer.GlobalPosition = ((Vector2)(ref globalPosition)).Lerp(node._projectileEndPoint.GlobalPosition, num);
			timer += ((Node)node).GetProcessDeltaTime();
			await ((GodotObject)node).ToSignal((GodotObject)(object)((Node)node).GetTree(), SignalName.ProcessFrame);
		}
		if (!node._cts.IsCancellationRequested)
		{
			((CanvasItem)node._projectileContainer).Visible = false;
			for (int l = 0; l < node._impactParticles.Count; l++)
			{
				node._impactParticles[l].Restart();
			}
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				instance.ScreenShake((ShakeStrength)4, (ShakeDuration)2, -1f);
			}
			await Cmd.Wait(2f, node._cts.Token, false);
			GodotTreeExtensions.QueueFreeSafely((Node)(object)node);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(7)
		{
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("targetFloorPosition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)20, StringName.op_Implicit("tint"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GetProjectileDirection, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetTopPosition, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("projectileDirection"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Initialize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ModulateParticles, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("tint"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			MyNLargeMissile myNLargeMissile = Create(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<MyNLargeMissile>(ref myNLargeMissile);
			return true;
		}
		if ((ref method) == MethodName.GetProjectileDirection && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Vector2 projectileDirection = GetProjectileDirection();
			ret = VariantUtils.CreateFrom<Vector2>(ref projectileDirection);
			return true;
		}
		if ((ref method) == MethodName.GetTopPosition && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Vector2 topPosition = GetTopPosition(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Vector2>(ref topPosition);
			return true;
		}
		if ((ref method) == MethodName.Initialize && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Initialize();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ModulateParticles && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ModulateParticles(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
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
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			MyNLargeMissile myNLargeMissile = Create(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<MyNLargeMissile>(ref myNLargeMissile);
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
		if ((ref method) == MethodName.GetProjectileDirection)
		{
			return true;
		}
		if ((ref method) == MethodName.GetTopPosition)
		{
			return true;
		}
		if ((ref method) == MethodName.Initialize)
		{
			return true;
		}
		if ((ref method) == MethodName.ModulateParticles)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
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
		if ((ref name) == PropertyName.WaitTime)
		{
			WaitTime = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._anticipationParticles)
		{
			_anticipationParticles = VariantUtils.ConvertToArray<GpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._projectileStartParticles)
		{
			_projectileStartParticles = VariantUtils.ConvertToArray<GpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._projectileParticles)
		{
			_projectileParticles = VariantUtils.ConvertToArray<GpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._impactParticles)
		{
			_impactParticles = VariantUtils.ConvertToArray<GpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._modulateParticles)
		{
			_modulateParticles = VariantUtils.ConvertToArray<GpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._anticipationContainer)
		{
			_anticipationContainer = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._anticipationDuration)
		{
			_anticipationDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._projectileContainer)
		{
			_projectileContainer = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._projectileStartPoint)
		{
			_projectileStartPoint = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._projectileEndPoint)
		{
			_projectileEndPoint = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._projectileOffset)
		{
			_projectileOffset = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.WaitTime)
		{
			float waitTime = WaitTime;
			value = VariantUtils.CreateFrom<float>(ref waitTime);
			return true;
		}
		if ((ref name) == PropertyName._anticipationParticles)
		{
			value = VariantUtils.CreateFromArray<GpuParticles2D>(_anticipationParticles);
			return true;
		}
		if ((ref name) == PropertyName._projectileStartParticles)
		{
			value = VariantUtils.CreateFromArray<GpuParticles2D>(_projectileStartParticles);
			return true;
		}
		if ((ref name) == PropertyName._projectileParticles)
		{
			value = VariantUtils.CreateFromArray<GpuParticles2D>(_projectileParticles);
			return true;
		}
		if ((ref name) == PropertyName._impactParticles)
		{
			value = VariantUtils.CreateFromArray<GpuParticles2D>(_impactParticles);
			return true;
		}
		if ((ref name) == PropertyName._modulateParticles)
		{
			value = VariantUtils.CreateFromArray<GpuParticles2D>(_modulateParticles);
			return true;
		}
		if ((ref name) == PropertyName._anticipationContainer)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _anticipationContainer);
			return true;
		}
		if ((ref name) == PropertyName._anticipationDuration)
		{
			value = VariantUtils.CreateFrom<float>(ref _anticipationDuration);
			return true;
		}
		if ((ref name) == PropertyName._projectileContainer)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _projectileContainer);
			return true;
		}
		if ((ref name) == PropertyName._projectileStartPoint)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _projectileStartPoint);
			return true;
		}
		if ((ref name) == PropertyName._projectileEndPoint)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _projectileEndPoint);
			return true;
		}
		if ((ref name) == PropertyName._projectileOffset)
		{
			value = VariantUtils.CreateFrom<float>(ref _projectileOffset);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)28, PropertyName._anticipationParticles, (PropertyHint)23, "24/34:GPUParticles2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)28, PropertyName._projectileStartParticles, (PropertyHint)23, "24/34:GPUParticles2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)28, PropertyName._projectileParticles, (PropertyHint)23, "24/34:GPUParticles2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)28, PropertyName._impactParticles, (PropertyHint)23, "24/34:GPUParticles2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)28, PropertyName._modulateParticles, (PropertyHint)23, "24/34:GPUParticles2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._anticipationContainer, (PropertyHint)34, "Node2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName._anticipationDuration, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._projectileContainer, (PropertyHint)34, "Node2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._projectileStartPoint, (PropertyHint)34, "Node2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)24, PropertyName._projectileEndPoint, (PropertyHint)34, "Node2D", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName._projectileOffset, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.WaitTime, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName waitTime = PropertyName.WaitTime;
		float waitTime2 = WaitTime;
		info.AddProperty(waitTime, Variant.From<float>(ref waitTime2));
		info.AddProperty(PropertyName._anticipationParticles, Variant.CreateFrom<GpuParticles2D>(_anticipationParticles));
		info.AddProperty(PropertyName._projectileStartParticles, Variant.CreateFrom<GpuParticles2D>(_projectileStartParticles));
		info.AddProperty(PropertyName._projectileParticles, Variant.CreateFrom<GpuParticles2D>(_projectileParticles));
		info.AddProperty(PropertyName._impactParticles, Variant.CreateFrom<GpuParticles2D>(_impactParticles));
		info.AddProperty(PropertyName._modulateParticles, Variant.CreateFrom<GpuParticles2D>(_modulateParticles));
		info.AddProperty(PropertyName._anticipationContainer, Variant.From<Node2D>(ref _anticipationContainer));
		info.AddProperty(PropertyName._anticipationDuration, Variant.From<float>(ref _anticipationDuration));
		info.AddProperty(PropertyName._projectileContainer, Variant.From<Node2D>(ref _projectileContainer));
		info.AddProperty(PropertyName._projectileStartPoint, Variant.From<Node2D>(ref _projectileStartPoint));
		info.AddProperty(PropertyName._projectileEndPoint, Variant.From<Node2D>(ref _projectileEndPoint));
		info.AddProperty(PropertyName._projectileOffset, Variant.From<float>(ref _projectileOffset));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.WaitTime, ref val))
		{
			WaitTime = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._anticipationParticles, ref val2))
		{
			_anticipationParticles = ((Variant)(ref val2)).AsGodotArray<GpuParticles2D>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._projectileStartParticles, ref val3))
		{
			_projectileStartParticles = ((Variant)(ref val3)).AsGodotArray<GpuParticles2D>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._projectileParticles, ref val4))
		{
			_projectileParticles = ((Variant)(ref val4)).AsGodotArray<GpuParticles2D>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._impactParticles, ref val5))
		{
			_impactParticles = ((Variant)(ref val5)).AsGodotArray<GpuParticles2D>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._modulateParticles, ref val6))
		{
			_modulateParticles = ((Variant)(ref val6)).AsGodotArray<GpuParticles2D>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._anticipationContainer, ref val7))
		{
			_anticipationContainer = ((Variant)(ref val7)).As<Node2D>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._anticipationDuration, ref val8))
		{
			_anticipationDuration = ((Variant)(ref val8)).As<float>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._projectileContainer, ref val9))
		{
			_projectileContainer = ((Variant)(ref val9)).As<Node2D>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._projectileStartPoint, ref val10))
		{
			_projectileStartPoint = ((Variant)(ref val10)).As<Node2D>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._projectileEndPoint, ref val11))
		{
			_projectileEndPoint = ((Variant)(ref val11)).As<Node2D>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._projectileOffset, ref val12))
		{
			_projectileOffset = ((Variant)(ref val12)).As<float>();
		}
	}
}
