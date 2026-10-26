using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace IntentGraph2.Scenes;

[ScriptPath("res://intentgraph2/src/Scenes/NIgArrowButton.cs")]
public class NIgArrowButton : NGoldArrowButton
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Texture = StringName.op_Implicit("Texture");

		public static readonly StringName texture = StringName.op_Implicit("texture");
	}

	public class SignalName : SignalName
	{
	}

	private Texture2D? texture;

	[Export(/*Could not decode attribute arguments.*/)]
	public Texture2D? Texture
	{
		get
		{
			return texture;
		}
		set
		{
			texture = value;
			if (base._icon != null)
			{
				base._icon.Texture = texture;
			}
		}
	}

	public override void _Ready()
	{
		((NGoldArrowButton)this)._Ready();
		if (texture != null)
		{
			base._icon.Texture = texture;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		return ((NGoldArrowButton)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((NGoldArrowButton)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.Texture)
		{
			Texture = VariantUtils.ConvertTo<Texture2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.texture)
		{
			texture = VariantUtils.ConvertTo<Texture2D>(ref value);
			return true;
		}
		return ((NGoldArrowButton)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Texture)
		{
			Texture2D val = Texture;
			value = VariantUtils.CreateFrom<Texture2D>(ref val);
			return true;
		}
		if ((ref name) == PropertyName.texture)
		{
			value = VariantUtils.CreateFrom<Texture2D>(ref texture);
			return true;
		}
		return ((NGoldArrowButton)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.texture, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.Texture, (PropertyHint)17, "Texture2D", (PropertyUsageFlags)4102, true)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((NGoldArrowButton)this).SaveGodotObjectData(info);
		StringName obj = PropertyName.Texture;
		Texture2D val = Texture;
		info.AddProperty(obj, Variant.From<Texture2D>(ref val));
		info.AddProperty(PropertyName.texture, Variant.From<Texture2D>(ref texture));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NGoldArrowButton)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.Texture, ref val))
		{
			Texture = ((Variant)(ref val)).As<Texture2D>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.texture, ref val2))
		{
			texture = ((Variant)(ref val2)).As<Texture2D>();
		}
	}
}
