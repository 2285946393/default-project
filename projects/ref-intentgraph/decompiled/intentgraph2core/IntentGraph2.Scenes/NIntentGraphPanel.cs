using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using IntentGraph2.Utils;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IntentGraph2.Scenes;

[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraphPanel.cs")]
public class NIntentGraphPanel : MarginContainer
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _GuiInput = StringName.op_Implicit("_GuiInput");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName OnPinButtonToggled = StringName.op_Implicit("OnPinButtonToggled");

		public static readonly StringName OnCloseButtonPressed = StringName.op_Implicit("OnCloseButtonPressed");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName NCreature = StringName.op_Implicit("NCreature");

		public static readonly StringName Pinned = StringName.op_Implicit("Pinned");

		public static readonly StringName dragging = StringName.op_Implicit("dragging");

		public static readonly StringName dragOffset = StringName.op_Implicit("dragOffset");

		public static readonly StringName pinButton = StringName.op_Implicit("pinButton");

		public static readonly StringName closeButton = StringName.op_Implicit("closeButton");
	}

	public class SignalName : SignalName
	{
	}

	private bool dragging;

	private Vector2 dragOffset = Vector2.Zero;

	private Button? pinButton;

	private Button? closeButton;

	public NCreature? NCreature { get; set; }

	public bool Pinned
	{
		get
		{
			Button? obj = pinButton;
			if (obj == null)
			{
				return false;
			}
			return ((BaseButton)obj).ButtonPressed;
		}
	}

	public override void _Ready()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		if (IntentGraphMod.Config.PinableIntentGraph)
		{
			((CanvasItem)((Node)this).GetNode<BoxContainer>(NodePath.op_Implicit("%ButtonContainer"))).Show();
			((Control)this).MouseFilter = (MouseFilterEnum)1;
		}
		pinButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%PinButton"));
		closeButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%CloseButton"));
		((BaseButton)pinButton).Toggled += new ToggledEventHandler(OnPinButtonToggled);
		((BaseButton)closeButton).Pressed += OnCloseButtonPressed;
	}

	public override void _GuiInput(InputEvent evt)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I8
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		InputEventMouseButton val = (InputEventMouseButton)(object)((evt is InputEventMouseButton) ? evt : null);
		if (val != null && (long)val.ButtonIndex == 1)
		{
			if (val.Pressed)
			{
				((CanvasItem)this).MoveToFront();
				dragging = true;
				dragOffset = ((CanvasItem)this).GetGlobalMousePosition() - ((Control)this).GlobalPosition;
			}
			else
			{
				dragging = false;
			}
		}
		if (evt is InputEventMouseMotion && dragging)
		{
			((Control)this).GlobalPosition = ((CanvasItem)this).GetGlobalMousePosition() - dragOffset;
		}
	}

	public override void _Input(InputEvent evt)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Invalid comparison between Unknown and I8
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		InputEventKey val = (InputEventKey)(object)((evt is InputEventKey) ? evt : null);
		if (val != null && ((InputEvent)val).IsPressed() && IntentGraphMod.Config.ToggleIntentGraphKey == val.Keycode)
		{
			IntentGraphHost.ToggleIntentGraphVisibility();
			((Node)this).GetViewport().SetInputAsHandled();
		}
		InputEventMouseButton val2 = (InputEventMouseButton)(object)((evt is InputEventMouseButton) ? evt : null);
		if (val2 != null && (long)val2.ButtonIndex == 1 && val2.Pressed && NCreature != null && !Pinned)
		{
			Vector2 position = ((InputEventMouse)(InputEventMouseButton)((CanvasItem)this).MakeInputLocal(evt)).Position;
			Rect2 val3 = default(Rect2);
			((Rect2)(ref val3))._002Ector(Vector2.Zero, ((Control)this).Size);
			if (!((Rect2)(ref val3)).HasPoint(position))
			{
				IntentGraphHost.Remove(NCreature);
			}
		}
	}

	private void OnPinButtonToggled(bool toggledOn)
	{
		if (pinButton != null)
		{
			if (toggledOn)
			{
				pinButton.Icon = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/unpin.png", (string)null, (CacheMode)1);
			}
			else
			{
				pinButton.Icon = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/pin.png", (string)null, (CacheMode)1);
			}
		}
	}

	private void OnCloseButtonPressed()
	{
		if (NCreature != null)
		{
			IntentGraphHost.Remove(NCreature);
		}
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
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._GuiInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("evt"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("evt"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnPinButtonToggled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("toggledOn"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnCloseButtonPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._GuiInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Control)this)._GuiInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnPinButtonToggled && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnPinButtonToggled(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnCloseButtonPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnCloseButtonPressed();
			ret = default(godot_variant);
			return true;
		}
		return ((MarginContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._GuiInput)
		{
			return true;
		}
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName.OnPinButtonToggled)
		{
			return true;
		}
		if ((ref method) == MethodName.OnCloseButtonPressed)
		{
			return true;
		}
		return ((MarginContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.NCreature)
		{
			NCreature = VariantUtils.ConvertTo<NCreature>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.dragging)
		{
			dragging = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.dragOffset)
		{
			dragOffset = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.pinButton)
		{
			pinButton = VariantUtils.ConvertTo<Button>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.closeButton)
		{
			closeButton = VariantUtils.ConvertTo<Button>(ref value);
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
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.NCreature)
		{
			NCreature nCreature = NCreature;
			value = VariantUtils.CreateFrom<NCreature>(ref nCreature);
			return true;
		}
		if ((ref name) == PropertyName.Pinned)
		{
			bool pinned = Pinned;
			value = VariantUtils.CreateFrom<bool>(ref pinned);
			return true;
		}
		if ((ref name) == PropertyName.dragging)
		{
			value = VariantUtils.CreateFrom<bool>(ref dragging);
			return true;
		}
		if ((ref name) == PropertyName.dragOffset)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref dragOffset);
			return true;
		}
		if ((ref name) == PropertyName.pinButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref pinButton);
			return true;
		}
		if ((ref name) == PropertyName.closeButton)
		{
			value = VariantUtils.CreateFrom<Button>(ref closeButton);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)1, PropertyName.dragging, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.dragOffset, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.pinButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.closeButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.NCreature, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.Pinned, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		((GodotObject)this).SaveGodotObjectData(info);
		StringName nCreature = PropertyName.NCreature;
		NCreature nCreature2 = NCreature;
		info.AddProperty(nCreature, Variant.From<NCreature>(ref nCreature2));
		info.AddProperty(PropertyName.dragging, Variant.From<bool>(ref dragging));
		info.AddProperty(PropertyName.dragOffset, Variant.From<Vector2>(ref dragOffset));
		info.AddProperty(PropertyName.pinButton, Variant.From<Button>(ref pinButton));
		info.AddProperty(PropertyName.closeButton, Variant.From<Button>(ref closeButton));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.NCreature, ref val))
		{
			NCreature = ((Variant)(ref val)).As<NCreature>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.dragging, ref val2))
		{
			dragging = ((Variant)(ref val2)).As<bool>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.dragOffset, ref val3))
		{
			dragOffset = ((Variant)(ref val3)).As<Vector2>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.pinButton, ref val4))
		{
			pinButton = ((Variant)(ref val4)).As<Button>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.closeButton, ref val5))
		{
			closeButton = ((Variant)(ref val5)).As<Button>();
		}
	}
}
