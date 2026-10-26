using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using IntentGraph2.Models;
using MegaCrit.Sts2.Core.Models;

namespace IntentGraph2.Scenes;

[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraph.cs")]
public class NIntentGraph : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName GraphScale = StringName.op_Implicit("GraphScale");

		public static readonly StringName AnimatedIcons = StringName.op_Implicit("AnimatedIcons");

		public static readonly StringName ShowCurrentMove = StringName.op_Implicit("ShowCurrentMove");

		public static readonly StringName graphScale = StringName.op_Implicit("graphScale");

		public static readonly StringName canvas = StringName.op_Implicit("canvas");

		public static readonly StringName animatedIcons = StringName.op_Implicit("animatedIcons");

		public static readonly StringName showCurrentMove = StringName.op_Implicit("showCurrentMove");
	}

	public class SignalName : SignalName
	{
	}

	public const float GridSize = 80f;

	public const int LabelFontSize = 18;

	public const float LabelLinePadding = 2f;

	private Graph? graph;

	private Vector2 graphScale = new Vector2(1f, 1f);

	private MonsterModel? monster;

	private NIntentGraphCanvas? canvas;

	private bool animatedIcons;

	private bool showCurrentMove;

	public Graph? Graph
	{
		get
		{
			return graph;
		}
		set
		{
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			graph = value;
			((Control)this).CustomMinimumSize = new Vector2((80f * graph?.Width).GetValueOrDefault(80f), (80f * graph?.Height).GetValueOrDefault(80f)) * graphScale;
			if (canvas != null)
			{
				canvas.Graph = value;
			}
		}
	}

	public Vector2 GraphScale
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return graphScale;
		}
		set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			graphScale = value;
			((Control)this).CustomMinimumSize = new Vector2((80f * graph?.Width).GetValueOrDefault(80f), (80f * graph?.Height).GetValueOrDefault(80f)) * graphScale;
			if (canvas != null)
			{
				((Control)canvas).Scale = value;
			}
		}
	}

	public MonsterModel? Monster
	{
		get
		{
			return monster;
		}
		set
		{
			monster = value;
			if (canvas != null)
			{
				canvas.Monster = value;
			}
		}
	}

	public bool AnimatedIcons
	{
		get
		{
			return animatedIcons;
		}
		set
		{
			animatedIcons = value;
			if (canvas != null)
			{
				canvas.AnimatedIcons = value;
			}
		}
	}

	public bool ShowCurrentMove
	{
		get
		{
			return showCurrentMove;
		}
		set
		{
			showCurrentMove = value;
			if (canvas != null)
			{
				canvas.ShowCurrentMove = value;
			}
		}
	}

	public override void _Ready()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		canvas = ((Node)this).GetNode<NIntentGraphCanvas>(NodePath.op_Implicit("%IntentGraphCanvas"));
		((Control)canvas).Scale = graphScale;
		canvas.Graph = graph;
		canvas.Monster = monster;
		canvas.AnimatedIcons = animatedIcons;
		canvas.ShowCurrentMove = showCurrentMove;
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
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.GraphScale)
		{
			GraphScale = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.AnimatedIcons)
		{
			AnimatedIcons = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ShowCurrentMove)
		{
			ShowCurrentMove = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.graphScale)
		{
			graphScale = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.canvas)
		{
			canvas = VariantUtils.ConvertTo<NIntentGraphCanvas>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.animatedIcons)
		{
			animatedIcons = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.showCurrentMove)
		{
			showCurrentMove = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.GraphScale)
		{
			Vector2 val = GraphScale;
			value = VariantUtils.CreateFrom<Vector2>(ref val);
			return true;
		}
		if ((ref name) == PropertyName.AnimatedIcons)
		{
			bool flag = AnimatedIcons;
			value = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref name) == PropertyName.ShowCurrentMove)
		{
			bool flag = ShowCurrentMove;
			value = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref name) == PropertyName.graphScale)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref graphScale);
			return true;
		}
		if ((ref name) == PropertyName.canvas)
		{
			value = VariantUtils.CreateFrom<NIntentGraphCanvas>(ref canvas);
			return true;
		}
		if ((ref name) == PropertyName.animatedIcons)
		{
			value = VariantUtils.CreateFrom<bool>(ref animatedIcons);
			return true;
		}
		if ((ref name) == PropertyName.showCurrentMove)
		{
			value = VariantUtils.CreateFrom<bool>(ref showCurrentMove);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)5, PropertyName.graphScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.canvas, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.animatedIcons, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.showCurrentMove, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.GraphScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.AnimatedIcons, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.ShowCurrentMove, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName obj = PropertyName.GraphScale;
		Vector2 val = GraphScale;
		info.AddProperty(obj, Variant.From<Vector2>(ref val));
		StringName obj2 = PropertyName.AnimatedIcons;
		bool flag = AnimatedIcons;
		info.AddProperty(obj2, Variant.From<bool>(ref flag));
		StringName obj3 = PropertyName.ShowCurrentMove;
		flag = ShowCurrentMove;
		info.AddProperty(obj3, Variant.From<bool>(ref flag));
		info.AddProperty(PropertyName.graphScale, Variant.From<Vector2>(ref graphScale));
		info.AddProperty(PropertyName.canvas, Variant.From<NIntentGraphCanvas>(ref canvas));
		info.AddProperty(PropertyName.animatedIcons, Variant.From<bool>(ref animatedIcons));
		info.AddProperty(PropertyName.showCurrentMove, Variant.From<bool>(ref showCurrentMove));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.GraphScale, ref val))
		{
			GraphScale = ((Variant)(ref val)).As<Vector2>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.AnimatedIcons, ref val2))
		{
			AnimatedIcons = ((Variant)(ref val2)).As<bool>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShowCurrentMove, ref val3))
		{
			ShowCurrentMove = ((Variant)(ref val3)).As<bool>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.graphScale, ref val4))
		{
			graphScale = ((Variant)(ref val4)).As<Vector2>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.canvas, ref val5))
		{
			canvas = ((Variant)(ref val5)).As<NIntentGraphCanvas>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName.animatedIcons, ref val6))
		{
			animatedIcons = ((Variant)(ref val6)).As<bool>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName.showCurrentMove, ref val7))
		{
			showCurrentMove = ((Variant)(ref val7)).As<bool>();
		}
	}
}
