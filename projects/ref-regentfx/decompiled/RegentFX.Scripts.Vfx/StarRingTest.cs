using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace RegentFX.Scripts.Vfx;

[ScriptPath("res://Scripts/Vfx/StarRingTest.cs")]
public class StarRingTest : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName UpdateStarPositions = StringName.op_Implicit("UpdateStarPositions");

		public static readonly StringName SetRadius = StringName.op_Implicit("SetRadius");

		public static readonly StringName SetRotationSpeed = StringName.op_Implicit("SetRotationSpeed");

		public static readonly StringName ToggleDirection = StringName.op_Implicit("ToggleDirection");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Radius = StringName.op_Implicit("Radius");

		public static readonly StringName RotationSpeed = StringName.op_Implicit("RotationSpeed");

		public static readonly StringName Clockwise = StringName.op_Implicit("Clockwise");

		public static readonly StringName _stars = StringName.op_Implicit("_stars");

		public static readonly StringName _currentAngle = StringName.op_Implicit("_currentAngle");
	}

	public class SignalName : SignalName
	{
	}

	private Node2D[]? _stars;

	private float _currentAngle;

	[Export(/*Could not decode attribute arguments.*/)]
	public float Radius { get; set; } = 100f;


	[Export(/*Could not decode attribute arguments.*/)]
	public float RotationSpeed { get; set; } = 90f;


	[Export(/*Could not decode attribute arguments.*/)]
	public bool Clockwise { get; set; } = true;


	public override void _Ready()
	{
		_stars = (Node2D[]?)(object)new Node2D[4];
		for (int i = 0; i < 4; i++)
		{
			_stars[i] = ((Node)this).GetNode<Node2D>(NodePath.op_Implicit($"Star{i + 1}"));
		}
		UpdateStarPositions();
	}

	public override void _Process(double delta)
	{
		float num = (float)delta;
		float num2 = (Clockwise ? 1f : (-1f));
		_currentAngle += RotationSpeed * num2 * num * (float)Math.PI / 180f;
		UpdateStarPositions();
	}

	private void UpdateStarPositions()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (_stars != null)
		{
			for (int i = 0; i < 4; i++)
			{
				float num = _currentAngle + (float)i * (float)Math.PI / 2f;
				float num2 = Mathf.Cos(num) * Radius;
				float num3 = Mathf.Sin(num) * Radius;
				_stars[i].Position = new Vector2(num2, num3);
			}
		}
	}

	public void SetRadius(float radius)
	{
		Radius = radius;
	}

	public void SetRotationSpeed(float speed)
	{
		RotationSpeed = speed;
	}

	public void ToggleDirection()
	{
		Clockwise = !Clockwise;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(6)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateStarPositions, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetRadius, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("radius"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetRotationSpeed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("speed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ToggleDirection, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
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
		if ((ref method) == MethodName.UpdateStarPositions && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateStarPositions();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetRadius && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetRadius(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetRotationSpeed && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetRotationSpeed(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ToggleDirection && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ToggleDirection();
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
		if ((ref method) == MethodName.UpdateStarPositions)
		{
			return true;
		}
		if ((ref method) == MethodName.SetRadius)
		{
			return true;
		}
		if ((ref method) == MethodName.SetRotationSpeed)
		{
			return true;
		}
		if ((ref method) == MethodName.ToggleDirection)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.Radius)
		{
			Radius = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.RotationSpeed)
		{
			RotationSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.Clockwise)
		{
			Clockwise = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._stars)
		{
			_stars = VariantUtils.ConvertToSystemArrayOfGodotObject<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentAngle)
		{
			_currentAngle = VariantUtils.ConvertTo<float>(ref value);
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
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Radius)
		{
			float radius = Radius;
			value = VariantUtils.CreateFrom<float>(ref radius);
			return true;
		}
		if ((ref name) == PropertyName.RotationSpeed)
		{
			float radius = RotationSpeed;
			value = VariantUtils.CreateFrom<float>(ref radius);
			return true;
		}
		if ((ref name) == PropertyName.Clockwise)
		{
			bool clockwise = Clockwise;
			value = VariantUtils.CreateFrom<bool>(ref clockwise);
			return true;
		}
		if ((ref name) == PropertyName._stars)
		{
			GodotObject[] stars = (GodotObject[])(object)_stars;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(stars);
			return true;
		}
		if ((ref name) == PropertyName._currentAngle)
		{
			value = VariantUtils.CreateFrom<float>(ref _currentAngle);
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
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.Radius, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)3, PropertyName.RotationSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)1, PropertyName.Clockwise, (PropertyHint)0, "", (PropertyUsageFlags)4102, true),
			new PropertyInfo((Type)28, PropertyName._stars, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._currentAngle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName radius = PropertyName.Radius;
		float radius2 = Radius;
		info.AddProperty(radius, Variant.From<float>(ref radius2));
		StringName rotationSpeed = PropertyName.RotationSpeed;
		radius2 = RotationSpeed;
		info.AddProperty(rotationSpeed, Variant.From<float>(ref radius2));
		StringName clockwise = PropertyName.Clockwise;
		bool clockwise2 = Clockwise;
		info.AddProperty(clockwise, Variant.From<bool>(ref clockwise2));
		StringName stars = PropertyName._stars;
		GodotObject[] stars2 = (GodotObject[])(object)_stars;
		info.AddProperty(stars, Variant.CreateFrom(stars2));
		info.AddProperty(PropertyName._currentAngle, Variant.From<float>(ref _currentAngle));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.Radius, ref val))
		{
			Radius = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.RotationSpeed, ref val2))
		{
			RotationSpeed = ((Variant)(ref val2)).As<float>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.Clockwise, ref val3))
		{
			Clockwise = ((Variant)(ref val3)).As<bool>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._stars, ref val4))
		{
			_stars = ((Variant)(ref val4)).AsGodotObjectArray<Node2D>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentAngle, ref val5))
		{
			_currentAngle = ((Variant)(ref val5)).As<float>();
		}
	}
}
