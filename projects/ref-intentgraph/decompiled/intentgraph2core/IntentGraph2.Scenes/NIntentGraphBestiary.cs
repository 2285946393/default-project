using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using IntentGraph2.Models;
using IntentGraph2.Patches;
using IntentGraph2.Utils;
using IntentGraph2.Utils.GraphGenerator;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace IntentGraph2.Scenes;

[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraphBestiary.cs")]
public class NIntentGraphBestiary : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName GenerateGraphs = StringName.op_Implicit("GenerateGraphs");

		public static readonly StringName ClearGraphs = StringName.op_Implicit("ClearGraphs");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName currentGraphIndex = StringName.op_Implicit("currentGraphIndex");

		public static readonly StringName intentGraph = StringName.op_Implicit("intentGraph");

		public static readonly StringName previousGraph = StringName.op_Implicit("previousGraph");

		public static readonly StringName nextGraph = StringName.op_Implicit("nextGraph");

		public static readonly StringName ascension9 = StringName.op_Implicit("ascension9");
	}

	public class SignalName : SignalName
	{
	}

	private MonsterModel? monster;

	private EncounterModel? encounter;

	private List<Graph> graphs = new List<Graph>();

	private int currentGraphIndex;

	private NIntentGraph? intentGraph;

	private NIgArrowButton? previousGraph;

	private NIgArrowButton? nextGraph;

	private bool ascension9;

	public MonsterModel? Monster
	{
		get
		{
			return monster;
		}
		set
		{
			if (monster != value)
			{
				monster = value;
				if (value != null)
				{
					encounter = null;
					GenerateGraphs();
				}
				else
				{
					ClearGraphs();
				}
			}
		}
	}

	public EncounterModel? Encounter
	{
		get
		{
			return encounter;
		}
		set
		{
			if (encounter != value)
			{
				encounter = value;
				if (encounter != null)
				{
					monster = null;
					GenerateGraphs();
				}
				else
				{
					ClearGraphs();
				}
			}
		}
	}

	public override void _Ready()
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected O, but got Unknown
		intentGraph = ((Node)this).GetNode<NIntentGraph>(NodePath.op_Implicit("%IntentGraph"));
		previousGraph = ((Node)this).GetNode<NIgArrowButton>(NodePath.op_Implicit("%PreviousGraph"));
		nextGraph = ((Node)this).GetNode<NIgArrowButton>(NodePath.op_Implicit("%NextGraph"));
		NIgTickbox ascension9Check = ((Node)this).GetNode<NIgTickbox>(NodePath.op_Implicit("%Ascension9"));
		NIgTickbox node = ((Node)this).GetNode<NIgTickbox>(NodePath.op_Implicit("%ShowIntentGraph"));
		Node parent = ((Node)this).GetParent();
		Control description = ((parent != null) ? parent.GetNodeOrNull<Control>(NodePath.op_Implicit("%Description")) : null);
		intentGraph.AnimatedIcons = IntentGraphMod.Config.UseAnimatedIntentIcon;
		((NClickableControl)previousGraph).Released += (ReleasedEventHandler)delegate
		{
			if (graphs.Count > 0)
			{
				currentGraphIndex = (currentGraphIndex - 1 + graphs.Count) % graphs.Count;
				SetGraph(graphs[currentGraphIndex]);
			}
		};
		((NClickableControl)nextGraph).Released += (ReleasedEventHandler)delegate
		{
			if (graphs.Count > 0)
			{
				currentGraphIndex = (currentGraphIndex + 1) % graphs.Count;
				SetGraph(graphs[currentGraphIndex]);
			}
		};
		((NTickbox)ascension9Check).IsTicked = false;
		((NTickbox)ascension9Check).Toggled += (ToggledEventHandler)delegate(NTickbox tickbox)
		{
			ascension9 = tickbox.IsTicked;
			if (monster != null || encounter != null)
			{
				GenerateGraphs();
			}
		};
		Label label = ascension9Check.Label;
		LocString ifExists = LocString.GetIfExists("gameplay_ui", "ASCENSION_LEVEL");
		if (ifExists != null && label != null)
		{
			ifExists.Add("ascension", 9m);
			label.Text = ifExists.GetFormattedText();
		}
		((NTickbox)node).IsTicked = true;
		if (description != null)
		{
			((NTickbox)node).IsTicked = false;
			((CanvasItem)intentGraph).Visible = false;
			((CanvasItem)ascension9Check).Visible = false;
			((CanvasItem)previousGraph).Visible = false;
			((CanvasItem)nextGraph).Visible = false;
		}
		((NTickbox)node).Toggled += (ToggledEventHandler)delegate(NTickbox tickbox)
		{
			((CanvasItem)intentGraph).Visible = tickbox.IsTicked;
			((CanvasItem)ascension9Check).Visible = tickbox.IsTicked;
			((CanvasItem)previousGraph).Visible = tickbox.IsTicked && graphs.Count > 1;
			((CanvasItem)nextGraph).Visible = tickbox.IsTicked && graphs.Count > 1;
			if (description != null)
			{
				((CanvasItem)description).Visible = !tickbox.IsTicked;
			}
		};
		LocString ifExists2 = LocString.GetIfExists("settings_ui", "INTENTGRAPH2.mod_title");
		if (ifExists2 != null && node.Label != null)
		{
			node.Label.Text = ifExists2.GetFormattedText();
		}
		GenerateGraphs();
	}

	public override void _Input(InputEvent evt)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (intentGraph != null)
		{
			InputEventKey val = (InputEventKey)(object)((evt is InputEventKey) ? evt : null);
			if (val != null && ((InputEvent)val).IsPressed() && IntentGraphMod.Config.ToggleIntentGraphKey == val.Keycode)
			{
				((CanvasItem)this).Visible = !((CanvasItem)this).Visible;
				((Node)this).GetViewport().SetInputAsHandled();
			}
		}
	}

	public void GenerateGraphs()
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (monster == null && encounter == null)
		{
			ClearGraphs();
			return;
		}
		HasAscensionLevelPatches.OverwriteAsensionLevel = (AscensionLevel)(ascension9 ? 9 : 0);
		try
		{
			graphs.Clear();
			HashSet<Graph> hashSet = new HashSet<Graph>();
			List<EncounterModel> list;
			if (encounter == null)
			{
				list = ModelDb.AllEncounters.Where((EncounterModel e) => e.AllPossibleMonsters.Any((MonsterModel m) => monster == null || ((AbstractModel)m).Id == ((AbstractModel)monster).Id)).ToList();
			}
			else
			{
				int num = 1;
				list = new List<EncounterModel>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = encounter;
			}
			foreach (EncounterModel item3 in list)
			{
				EncounterModel val = item3.ToMutable();
				new Traverse((object)val).Field("_rng").SetValue((object)NewRng());
				val.GenerateMonstersWithSlots((IRunState)(object)NullRunState.Instance);
				foreach (var item4 in val.MonstersWithSlots.Where<(MonsterModel, string)>(((MonsterModel, string) t) => monster == null || ((AbstractModel)t.Item1).Id == ((AbstractModel)monster).Id))
				{
					MonsterModel item = item4.Item1;
					string item2 = item4.Item2;
					Graph graph = IntentGraphGenerator.GenerateGraphForBestiary(item, val, item2);
					if (graph != null)
					{
						hashSet.Add(graph);
					}
				}
				foreach (MonsterModel item5 in item3.AllPossibleMonsters.Where((MonsterModel m) => monster == null || ((AbstractModel)m).Id == ((AbstractModel)monster).Id))
				{
					IReadOnlyList<string> readOnlyList = item3.Slots;
					if (readOnlyList.Count == 0)
					{
						readOnlyList = new List<string> { "" };
					}
					foreach (string item6 in readOnlyList)
					{
						Graph graph2 = IntentGraphGenerator.GenerateGraphForBestiary(item5.ToMutable(), val, item6);
						if (graph2 != null)
						{
							hashSet.Add(graph2);
						}
					}
				}
			}
			graphs.AddRange(hashSet);
			currentGraphIndex = Math.Max(0, Math.Min(currentGraphIndex, graphs.Count - 1));
			if (intentGraph != null)
			{
				SetGraph((graphs.Count > 0) ? graphs[currentGraphIndex] : null);
			}
			if (previousGraph != null && nextGraph != null)
			{
				((CanvasItem)previousGraph).Visible = graphs.Count > 1;
				((CanvasItem)nextGraph).Visible = graphs.Count > 1;
			}
		}
		catch (Exception value)
		{
			IgLogger.Error($"Error generating graphs for {((monster != null) ? monster.Title.GetFormattedText() : ((encounter != null) ? encounter.Title.GetFormattedText() : null))}: {value}");
			ClearGraphs();
		}
		finally
		{
			HasAscensionLevelPatches.OverwriteAsensionLevel = null;
		}
	}

	private void ClearGraphs()
	{
		graphs.Clear();
		currentGraphIndex = 0;
		if (intentGraph != null)
		{
			intentGraph.Graph = null;
		}
		if (previousGraph != null && nextGraph != null)
		{
			((CanvasItem)previousGraph).Visible = false;
			((CanvasItem)nextGraph).Visible = false;
		}
	}

	private void SetGraph(Graph? graph)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (intentGraph != null)
		{
			intentGraph.Graph = graph;
			if (graph != null)
			{
				float num = ((graph.Height < 3f) ? 1f : Math.Max(0.8f, 3f / graph.Height));
				intentGraph.GraphScale = new Vector2(num, num);
			}
		}
	}

	private Rng NewRng()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		Type typeFromHandle = typeof(Rng);
		ConstructorInfo constructor = typeFromHandle.GetConstructor(new Type[1] { typeof(ulong) });
		if (constructor != null)
		{
			return (Rng)constructor.Invoke(new object[1] { 0uL });
		}
		constructor = typeFromHandle.GetConstructor(new Type[2]
		{
			typeof(uint),
			typeof(int)
		});
		if (constructor != null)
		{
			return (Rng)constructor.Invoke(new object[2] { 0u, 0 });
		}
		throw new Exception("No suitable constructor found for Rng.");
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(4)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("evt"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GenerateGraphs, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ClearGraphs, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GenerateGraphs && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			GenerateGraphs();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ClearGraphs && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ClearGraphs();
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
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName.GenerateGraphs)
		{
			return true;
		}
		if ((ref method) == MethodName.ClearGraphs)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.currentGraphIndex)
		{
			currentGraphIndex = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.intentGraph)
		{
			intentGraph = VariantUtils.ConvertTo<NIntentGraph>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.previousGraph)
		{
			previousGraph = VariantUtils.ConvertTo<NIgArrowButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.nextGraph)
		{
			nextGraph = VariantUtils.ConvertTo<NIgArrowButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ascension9)
		{
			ascension9 = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.currentGraphIndex)
		{
			value = VariantUtils.CreateFrom<int>(ref currentGraphIndex);
			return true;
		}
		if ((ref name) == PropertyName.intentGraph)
		{
			value = VariantUtils.CreateFrom<NIntentGraph>(ref intentGraph);
			return true;
		}
		if ((ref name) == PropertyName.previousGraph)
		{
			value = VariantUtils.CreateFrom<NIgArrowButton>(ref previousGraph);
			return true;
		}
		if ((ref name) == PropertyName.nextGraph)
		{
			value = VariantUtils.CreateFrom<NIgArrowButton>(ref nextGraph);
			return true;
		}
		if ((ref name) == PropertyName.ascension9)
		{
			value = VariantUtils.CreateFrom<bool>(ref ascension9);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, PropertyName.currentGraphIndex, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.intentGraph, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.previousGraph, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.nextGraph, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.ascension9, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName.currentGraphIndex, Variant.From<int>(ref currentGraphIndex));
		info.AddProperty(PropertyName.intentGraph, Variant.From<NIntentGraph>(ref intentGraph));
		info.AddProperty(PropertyName.previousGraph, Variant.From<NIgArrowButton>(ref previousGraph));
		info.AddProperty(PropertyName.nextGraph, Variant.From<NIgArrowButton>(ref nextGraph));
		info.AddProperty(PropertyName.ascension9, Variant.From<bool>(ref ascension9));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.currentGraphIndex, ref val))
		{
			currentGraphIndex = ((Variant)(ref val)).As<int>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.intentGraph, ref val2))
		{
			intentGraph = ((Variant)(ref val2)).As<NIntentGraph>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.previousGraph, ref val3))
		{
			previousGraph = ((Variant)(ref val3)).As<NIgArrowButton>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.nextGraph, ref val4))
		{
			nextGraph = ((Variant)(ref val4)).As<NIgArrowButton>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.ascension9, ref val5))
		{
			ascension9 = ((Variant)(ref val5)).As<bool>();
		}
	}
}
