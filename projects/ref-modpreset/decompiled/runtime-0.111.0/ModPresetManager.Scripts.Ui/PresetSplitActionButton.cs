using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace ModPresetManager.Scripts.Ui;

[ScriptPath("res://Scripts/Ui/PresetSplitActionButton.cs")]
public sealed class PresetSplitActionButton : HBoxContainer
{
	public class MethodName : MethodName
	{
		public static readonly StringName SetText = StringName.op_Implicit("SetText");

		public static readonly StringName SetTooltips = StringName.op_Implicit("SetTooltips");

		public static readonly StringName SetAttention = StringName.op_Implicit("SetAttention");

		public static readonly StringName SetDanger = StringName.op_Implicit("SetDanger");

		public static readonly StringName SetOptionsVisible = StringName.op_Implicit("SetOptionsVisible");

		public static readonly StringName SelectOption = StringName.op_Implicit("SelectOption");

		public static readonly StringName ShowOptionsMenu = StringName.op_Implicit("ShowOptionsMenu");

		public static readonly StringName AlignOptionsMenuToGear = StringName.op_Implicit("AlignOptionsMenuToGear");

		public static readonly StringName ApplyOptionsMenuTheme = StringName.op_Implicit("ApplyOptionsMenuTheme");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _mainButton = StringName.op_Implicit("_mainButton");

		public static readonly StringName _optionsButton = StringName.op_Implicit("_optionsButton");

		public static readonly StringName _optionsMenu = StringName.op_Implicit("_optionsMenu");

		public static readonly StringName _optionsWereOpenOnGearPress = StringName.op_Implicit("_optionsWereOpenOnGearPress");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Color PopupBackground = new Color("182333");

	private static readonly Color PopupHover = new Color("263a50");

	private static readonly Color PopupBorder = new Color("55718f");

	private static readonly Color PopupText = new Color("f5ead3");

	private readonly PresetActionButton _mainButton;

	private readonly PresetActionButton _optionsButton;

	private readonly PopupMenu _optionsMenu;

	private Action _action;

	private bool _optionsWereOpenOnGearPress;

	public PresetSplitActionButton(string text, IReadOnlyList<PresetSplitActionOption> options, Action action, Action<int> selectOption)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Expected O, but got Unknown
		Action<int> selectOption2 = selectOption;
		((HBoxContainer)this)._002Ector();
		PresetSplitActionButton presetSplitActionButton = this;
		_action = action;
		((Control)this).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)this).AddThemeConstantOverride(StringName.op_Implicit("separation"), 0);
		_mainButton = new PresetActionButton(text);
		((GodotObject)_mainButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
		{
			presetSplitActionButton._action();
		}), 0u);
		((Node)this).AddChild((Node)(object)_mainButton, false, (InternalMode)0);
		_optionsMenu = new PopupMenu
		{
			MinSize = new Vector2I(220, 0)
		};
		PresetActionButton presetActionButton = new PresetActionButton("⚙");
		((Control)presetActionButton).CustomMinimumSize = new Vector2(58f, 58f);
		((Control)presetActionButton).SizeFlagsHorizontal = (SizeFlags)8;
		_optionsButton = presetActionButton;
		((GodotObject)_optionsButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
		{
			presetSplitActionButton.ShowOptionsMenu();
		}), 0u);
		((GodotObject)_optionsButton).Connect(SignalName.MousePressed, Callable.From<InputEvent>((Action<InputEvent>)delegate
		{
			presetSplitActionButton._optionsWereOpenOnGearPress = ((Window)presetSplitActionButton._optionsMenu).Visible;
		}), 0u);
		((Node)this).AddChild((Node)(object)_optionsButton, false, (InternalMode)0);
		ApplyOptionsMenuTheme();
		for (int i = 0; i < options.Count; i++)
		{
			_optionsMenu.AddItem(options[i].Text, i, (Key)0);
			_optionsMenu.SetItemAsRadioCheckable(i, true);
			_optionsMenu.SetItemTooltip(i, options[i].Tooltip);
		}
		_optionsMenu.IdPressed += (IdPressedEventHandler)delegate(long id)
		{
			presetSplitActionButton.SelectOption((int)id);
			selectOption2((int)id);
		};
		((Node)_optionsButton).AddChild((Node)(object)_optionsMenu, false, (InternalMode)0);
		SelectOption(0);
	}

	public void SetText(string text)
	{
		_mainButton.SetText(text);
	}

	public void SetTooltips(string actionTooltip, string optionsTooltip)
	{
		((Control)_mainButton).TooltipText = actionTooltip;
		((Control)_optionsButton).TooltipText = optionsTooltip;
	}

	public void SetAttention(bool attention)
	{
		_mainButton.SetAttention(attention);
	}

	public void SetDanger(bool danger)
	{
		_mainButton.SetDanger(danger);
	}

	public void SetAction(Action action)
	{
		_action = action;
	}

	public void SetOptionsVisible(bool visible)
	{
		((Window)_optionsMenu).Hide();
		((CanvasItem)_optionsButton).Visible = visible;
	}

	private void SelectOption(int selectedIndex)
	{
		for (int i = 0; i < _optionsMenu.ItemCount; i++)
		{
			_optionsMenu.SetItemChecked(i, i == selectedIndex);
		}
	}

	private void ShowOptionsMenu()
	{
		if (_optionsWereOpenOnGearPress || ((Window)_optionsMenu).Visible)
		{
			((Window)_optionsMenu).Hide();
			_optionsWereOpenOnGearPress = false;
		}
		else
		{
			((Window)_optionsMenu).Popup((Rect2I?)null);
			AlignOptionsMenuToGear();
		}
	}

	private void AlignOptionsMenuToGear()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		Rect2 globalRect = ((Control)_optionsButton).GetGlobalRect();
		Vector2I size = ((Window)_optionsMenu).Size;
		((Window)_optionsMenu).Position = new Vector2I((int)(((Rect2)(ref globalRect)).Position.X + ((Rect2)(ref globalRect)).Size.X - (float)size.X), (int)(((Rect2)(ref globalRect)).Position.Y + ((Rect2)(ref globalRect)).Size.Y - 2f));
	}

	private void ApplyOptionsMenuTheme()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		StyleBoxFlat val = new StyleBoxFlat
		{
			BgColor = PopupBackground,
			BorderColor = PopupBorder,
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 16f,
			ContentMarginTop = 7f,
			ContentMarginRight = 16f,
			ContentMarginBottom = 7f
		};
		StyleBoxFlat val2 = new StyleBoxFlat
		{
			BgColor = PopupHover,
			CornerRadiusTopLeft = 5,
			CornerRadiusTopRight = 5,
			CornerRadiusBottomLeft = 5,
			CornerRadiusBottomRight = 5,
			ContentMarginLeft = 16f,
			ContentMarginTop = 7f,
			ContentMarginRight = 16f,
			ContentMarginBottom = 7f
		};
		((Window)_optionsMenu).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)val);
		((Window)_optionsMenu).AddThemeStyleboxOverride(StringName.op_Implicit("hover"), (StyleBox)(object)val2);
		((Window)_optionsMenu).AddThemeFontOverride(StringName.op_Implicit("font"), ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres", (string)null, (CacheMode)1));
		((Window)_optionsMenu).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 25);
		((Window)_optionsMenu).AddThemeColorOverride(StringName.op_Implicit("font_color"), PopupText);
		((Window)_optionsMenu).AddThemeColorOverride(StringName.op_Implicit("font_hover_color"), PopupText);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(9)
		{
			new MethodInfo(MethodName.SetText, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetTooltips, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("actionTooltip"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("optionsTooltip"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetAttention, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("attention"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetDanger, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("danger"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetOptionsVisible, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("visible"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SelectOption, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("selectedIndex"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ShowOptionsMenu, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AlignOptionsMenuToGear, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyOptionsMenuTheme, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.SetText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetTooltips && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			SetTooltips(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetAttention && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetAttention(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetDanger && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetDanger(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetOptionsVisible && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetOptionsVisible(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SelectOption && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SelectOption(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ShowOptionsMenu && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ShowOptionsMenu();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AlignOptionsMenuToGear && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AlignOptionsMenuToGear();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyOptionsMenuTheme && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyOptionsMenuTheme();
			ret = default(godot_variant);
			return true;
		}
		return ((HBoxContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.SetText)
		{
			return true;
		}
		if ((ref method) == MethodName.SetTooltips)
		{
			return true;
		}
		if ((ref method) == MethodName.SetAttention)
		{
			return true;
		}
		if ((ref method) == MethodName.SetDanger)
		{
			return true;
		}
		if ((ref method) == MethodName.SetOptionsVisible)
		{
			return true;
		}
		if ((ref method) == MethodName.SelectOption)
		{
			return true;
		}
		if ((ref method) == MethodName.ShowOptionsMenu)
		{
			return true;
		}
		if ((ref method) == MethodName.AlignOptionsMenuToGear)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyOptionsMenuTheme)
		{
			return true;
		}
		return ((HBoxContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._optionsWereOpenOnGearPress)
		{
			_optionsWereOpenOnGearPress = VariantUtils.ConvertTo<bool>(ref value);
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
		if ((ref name) == PropertyName._mainButton)
		{
			value = VariantUtils.CreateFrom<PresetActionButton>(ref _mainButton);
			return true;
		}
		if ((ref name) == PropertyName._optionsButton)
		{
			value = VariantUtils.CreateFrom<PresetActionButton>(ref _optionsButton);
			return true;
		}
		if ((ref name) == PropertyName._optionsMenu)
		{
			value = VariantUtils.CreateFrom<PopupMenu>(ref _optionsMenu);
			return true;
		}
		if ((ref name) == PropertyName._optionsWereOpenOnGearPress)
		{
			value = VariantUtils.CreateFrom<bool>(ref _optionsWereOpenOnGearPress);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._mainButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._optionsButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._optionsMenu, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._optionsWereOpenOnGearPress, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._optionsWereOpenOnGearPress, Variant.From<bool>(ref _optionsWereOpenOnGearPress));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._optionsWereOpenOnGearPress, ref val))
		{
			_optionsWereOpenOnGearPress = ((Variant)(ref val)).As<bool>();
		}
	}
}
