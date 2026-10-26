using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace LoadOrderManager;

[ScriptPath("res://LoadOrderPanel.cs")]
public class LoadOrderPanel : Control
{
	private sealed class ClipboardPayload
	{
		public string format { get; set; } = "load_order_manager_v2";


		public string exported_at_utc { get; set; } = string.Empty;


		public List<ClipboardEntry> entries { get; set; } = new List<ClipboardEntry>();


		public PresetExportData? presets { get; set; }
	}

	private sealed class PresetExportData
	{
		public string current_preset { get; set; } = string.Empty;


		public List<PresetExportEntry> profiles { get; set; } = new List<PresetExportEntry>();

	}

	private sealed class PresetExportEntry
	{
		public string name { get; set; } = string.Empty;


		public List<string> disabled_mods { get; set; } = new List<string>();


		public bool auto_enable_new_mods { get; set; }

		public List<string> known_mods { get; set; } = new List<string>();

	}

	private sealed class ClipboardEntry
	{
		public string key { get; set; } = string.Empty;


		public string id { get; set; } = string.Empty;


		public int? source { get; set; }

		public bool? is_enabled { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public bool? enabled { get; set; }

		public bool? ResolveEnabled()
		{
			if (is_enabled.HasValue)
			{
				return is_enabled.Value;
			}
			if (enabled.HasValue)
			{
				return enabled.Value;
			}
			return null;
		}
	}

	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName OpenPanel = StringName.op_Implicit("OpenPanel");

		public static readonly StringName BuildUiIfNeeded = StringName.op_Implicit("BuildUiIfNeeded");

		public static readonly StringName LayoutForViewport = StringName.op_Implicit("LayoutForViewport");

		public static readonly StringName ScheduleLayoutVerification = StringName.op_Implicit("ScheduleLayoutVerification");

		public static readonly StringName OnProcessFrame = StringName.op_Implicit("OnProcessFrame");

		public static readonly StringName CorrectLayoutOverflow = StringName.op_Implicit("CorrectLayoutOverflow");

		public static readonly StringName FixedChromeMinHeight = StringName.op_Implicit("FixedChromeMinHeight");

		public static readonly StringName BoundedMinHeight = StringName.op_Implicit("BoundedMinHeight");

		public static readonly StringName LogLayoutCheck = StringName.op_Implicit("LogLayoutCheck");

		public static readonly StringName ReadBoolEnv = StringName.op_Implicit("ReadBoolEnv");

		public static readonly StringName RefreshFromRuntime = StringName.op_Implicit("RefreshFromRuntime");

		public static readonly StringName RefreshOverwriteWarning = StringName.op_Implicit("RefreshOverwriteWarning");

		public static readonly StringName RefreshListOnly = StringName.op_Implicit("RefreshListOnly");

		public static readonly StringName GetSelectedIndex = StringName.op_Implicit("GetSelectedIndex");

		public static readonly StringName MoveSelected = StringName.op_Implicit("MoveSelected");

		public static readonly StringName MoveToTop = StringName.op_Implicit("MoveToTop");

		public static readonly StringName MoveToBottom = StringName.op_Implicit("MoveToBottom");

		public static readonly StringName SortByAlphabet = StringName.op_Implicit("SortByAlphabet");

		public static readonly StringName IsLibraryMod = StringName.op_Implicit("IsLibraryMod");

		public static readonly StringName SmartSort = StringName.op_Implicit("SmartSort");

		public static readonly StringName ToggleSelectedEnabled = StringName.op_Implicit("ToggleSelectedEnabled");

		public static readonly StringName RefreshPresetDropdown = StringName.op_Implicit("RefreshPresetDropdown");

		public static readonly StringName RefreshPolicyCheckbox = StringName.op_Implicit("RefreshPolicyCheckbox");

		public static readonly StringName OnNewModPolicyToggled = StringName.op_Implicit("OnNewModPolicyToggled");

		public static readonly StringName OnPresetSelected = StringName.op_Implicit("OnPresetSelected");

		public static readonly StringName CreatePresetFromCurrent = StringName.op_Implicit("CreatePresetFromCurrent");

		public static readonly StringName RenameCurrentPreset = StringName.op_Implicit("RenameCurrentPreset");

		public static readonly StringName DeleteCurrentPreset = StringName.op_Implicit("DeleteCurrentPreset");

		public static readonly StringName ExportToClipboard = StringName.op_Implicit("ExportToClipboard");

		public static readonly StringName ImportFromClipboard = StringName.op_Implicit("ImportFromClipboard");

		public static readonly StringName ApplyOrder = StringName.op_Implicit("ApplyOrder");

		public static readonly StringName SetStatus = StringName.op_Implicit("SetStatus");

		public static readonly StringName DebugLogLayoutCheck = StringName.op_Implicit("DebugLogLayoutCheck");

		public static readonly StringName DebugSelectPreset = StringName.op_Implicit("DebugSelectPreset");

		public static readonly StringName DebugCreatePreset = StringName.op_Implicit("DebugCreatePreset");

		public static readonly StringName DebugApplyOrder = StringName.op_Implicit("DebugApplyOrder");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _uiBuilt = StringName.op_Implicit("_uiBuilt");

		public static readonly StringName _list = StringName.op_Implicit("_list");

		public static readonly StringName _dialog = StringName.op_Implicit("_dialog");

		public static readonly StringName _margin = StringName.op_Implicit("_margin");

		public static readonly StringName _root = StringName.op_Implicit("_root");

		public static readonly StringName _title = StringName.op_Implicit("_title");

		public static readonly StringName _subtitle = StringName.op_Implicit("_subtitle");

		public static readonly StringName _statusLabel = StringName.op_Implicit("_statusLabel");

		public static readonly StringName _warningLabel = StringName.op_Implicit("_warningLabel");

		public static readonly StringName _presetBar = StringName.op_Implicit("_presetBar");

		public static readonly StringName _footer = StringName.op_Implicit("_footer");

		public static readonly StringName _sideScroll = StringName.op_Implicit("_sideScroll");

		public static readonly StringName _side = StringName.op_Implicit("_side");

		public static readonly StringName _presetDropdown = StringName.op_Implicit("_presetDropdown");

		public static readonly StringName _newModPolicyCheck = StringName.op_Implicit("_newModPolicyCheck");

		public static readonly StringName _suppressPresetEvent = StringName.op_Implicit("_suppressPresetEvent");

		public static readonly StringName _layoutViewport = StringName.op_Implicit("_layoutViewport");

		public static readonly StringName _layoutCheckHooked = StringName.op_Implicit("_layoutCheckHooked");

		public static readonly StringName _layoutCheckFramesPending = StringName.op_Implicit("_layoutCheckFramesPending");

		public static readonly StringName _layoutStepsRemaining = StringName.op_Implicit("_layoutStepsRemaining");
	}

	public class SignalName : SignalName
	{
	}

	private const string ClipboardFormatV1 = "load_order_manager_v1";

	private const string ClipboardFormatV2 = "load_order_manager_v2";

	private static readonly Regex TokenSplitRegex = new Regex("[\\r\\n,;|\\t]+", RegexOptions.Compiled);

	private const float DesignMaxWidth = 940f;

	private const float DesignMaxListHeight = 410f;

	private const float MinListHeight = 120f;

	private const float RootSeparation = 8f;

	private static readonly bool ForceCompact = ReadBoolEnv("LOADORDER_UI_COMPACT");

	private static readonly Vector2? SimulatedViewport = ReadSimulatedViewport();

	private readonly List<LoadOrderEntry> _entries = new List<LoadOrderEntry>();

	private bool _uiBuilt;

	private ItemList _list;

	private PanelContainer _dialog;

	private MarginContainer _margin;

	private VBoxContainer _root;

	private Label _title;

	private Label _subtitle;

	private Label _statusLabel;

	private Label _warningLabel;

	private HFlowContainer _presetBar;

	private HBoxContainer _footer;

	private ScrollContainer _sideScroll;

	private VBoxContainer _side;

	private OptionButton _presetDropdown;

	private CheckBox _newModPolicyCheck;

	private bool _suppressPresetEvent;

	private Vector2 _layoutViewport = new Vector2(1920f, 1080f);

	private bool _layoutCheckHooked;

	private int _layoutCheckFramesPending;

	private int _layoutStepsRemaining;

	private static readonly string[] LibKeywords = new string[8] { "lib", "base", "core", "framework", "common", "api", "helper", "toolkit" };

	internal IReadOnlyList<LoadOrderEntry> DebugEntries => _entries;

	public override void _Ready()
	{
		BuildUiIfNeeded();
		((CanvasItem)this).Visible = false;
		Viewport viewport = ((Node)this).GetViewport();
		if (viewport != null)
		{
			viewport.SizeChanged += LayoutForViewport;
		}
	}

	public override void _Input(InputEvent @event)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I8
		if (((CanvasItem)this).Visible)
		{
			InputEventKey val = (InputEventKey)(object)((@event is InputEventKey) ? @event : null);
			if (val != null && val.Pressed && !val.Echo && (long)val.Keycode == 4194305)
			{
				((CanvasItem)this).Visible = false;
				((Node)this).GetViewport().SetInputAsHandled();
			}
		}
	}

	public void OpenPanel()
	{
		BuildUiIfNeeded();
		((CanvasItem)this).Visible = true;
		DebugLog.Info("OpenPanel called.");
		LoadOrderRuntime.LogDiagnosticsOnce();
		LayoutForViewport();
		RefreshFromRuntime();
	}

	private void BuildUiIfNeeded()
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Expected O, but got Unknown
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Expected O, but got Unknown
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Expected O, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Expected O, but got Unknown
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Expected O, but got Unknown
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Expected O, but got Unknown
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Expected O, but got Unknown
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Expected O, but got Unknown
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Expected O, but got Unknown
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Expected O, but got Unknown
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Expected O, but got Unknown
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Expected O, but got Unknown
		//IL_0484: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_0491: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Expected O, but got Unknown
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e9: Expected O, but got Unknown
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0503: Expected O, but got Unknown
		//IL_06df: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e9: Expected O, but got Unknown
		//IL_0768: Unknown result type (might be due to invalid IL or missing references)
		//IL_076d: Unknown result type (might be due to invalid IL or missing references)
		//IL_077d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0785: Unknown result type (might be due to invalid IL or missing references)
		//IL_0792: Expected O, but got Unknown
		if (!_uiBuilt)
		{
			_uiBuilt = true;
			((Control)this).AnchorLeft = 0f;
			((Control)this).AnchorTop = 0f;
			((Control)this).AnchorRight = 1f;
			((Control)this).AnchorBottom = 1f;
			((Control)this).OffsetLeft = 0f;
			((Control)this).OffsetTop = 0f;
			((Control)this).OffsetRight = 0f;
			((Control)this).OffsetBottom = 0f;
			((Control)this).MouseFilter = (MouseFilterEnum)0;
			((CanvasItem)this).ZIndex = 100;
			ColorRect val = new ColorRect
			{
				Color = new Color(0f, 0f, 0f, 0.62f),
				MouseFilter = (MouseFilterEnum)0
			};
			((Control)val).AnchorLeft = 0f;
			((Control)val).AnchorTop = 0f;
			((Control)val).AnchorRight = 1f;
			((Control)val).AnchorBottom = 1f;
			((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
			_dialog = new PanelContainer
			{
				MouseFilter = (MouseFilterEnum)0
			};
			((Node)val).AddChild((Node)(object)_dialog, false, (InternalMode)0);
			_margin = new MarginContainer();
			((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 14);
			((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), 12);
			((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 14);
			((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 12);
			((Node)_dialog).AddChild((Node)(object)_margin, false, (InternalMode)0);
			_root = new VBoxContainer();
			((Control)_root).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
			((Node)_margin).AddChild((Node)(object)_root, false, (InternalMode)0);
			_title = new Label
			{
				Text = I18n.T("title"),
				HorizontalAlignment = (HorizontalAlignment)1
			};
			((Control)_title).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
			((Node)_root).AddChild((Node)(object)_title, false, (InternalMode)0);
			_subtitle = new Label
			{
				Text = I18n.T("subtitle"),
				HorizontalAlignment = (HorizontalAlignment)1,
				AutowrapMode = (AutowrapMode)3
			};
			((Node)_root).AddChild((Node)(object)_subtitle, false, (InternalMode)0);
			_warningLabel = new Label
			{
				Visible = false,
				Text = string.Empty,
				Modulate = new Color(1f, 0.77f, 0.3f, 1f),
				AutowrapMode = (AutowrapMode)3,
				HorizontalAlignment = (HorizontalAlignment)1,
				SizeFlagsHorizontal = (SizeFlags)3,
				MaxLinesVisible = 2,
				TextOverrunBehavior = (OverrunBehavior)3
			};
			((Node)_root).AddChild((Node)(object)_warningLabel, false, (InternalMode)0);
			_presetBar = new HFlowContainer();
			((Control)_presetBar).AddThemeConstantOverride(StringName.op_Implicit("h_separation"), 6);
			((Control)_presetBar).AddThemeConstantOverride(StringName.op_Implicit("v_separation"), 4);
			((Node)_root).AddChild((Node)(object)_presetBar, false, (InternalMode)0);
			Label val2 = new Label
			{
				Text = I18n.T("preset_label"),
				VerticalAlignment = (VerticalAlignment)1
			};
			((Node)_presetBar).AddChild((Node)(object)val2, false, (InternalMode)0);
			_presetDropdown = new OptionButton();
			_presetDropdown.ItemSelected += new ItemSelectedEventHandler(OnPresetSelected);
			((Control)_presetDropdown).CustomMinimumSize = new Vector2(200f, 0f);
			((Node)_presetBar).AddChild((Node)(object)_presetDropdown, false, (InternalMode)0);
			((Node)_presetBar).AddChild((Node)(object)MakeButton(I18n.T("btn_preset_new"), CreatePresetFromCurrent), false, (InternalMode)0);
			((Node)_presetBar).AddChild((Node)(object)MakeButton(I18n.T("btn_preset_rename"), RenameCurrentPreset), false, (InternalMode)0);
			((Node)_presetBar).AddChild((Node)(object)MakeButton(I18n.T("btn_preset_delete"), DeleteCurrentPreset), false, (InternalMode)0);
			_newModPolicyCheck = new CheckBox
			{
				Text = I18n.T("policy_auto_enable"),
				TooltipText = I18n.T("policy_auto_enable_tooltip"),
				ButtonPressed = true
			};
			((BaseButton)_newModPolicyCheck).Toggled += new ToggledEventHandler(OnNewModPolicyToggled);
			((Node)_presetBar).AddChild((Node)(object)_newModPolicyCheck, false, (InternalMode)0);
			HBoxContainer val3 = new HBoxContainer();
			((Control)val3).SizeFlagsVertical = (SizeFlags)3;
			((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
			((Node)_root).AddChild((Node)(object)val3, false, (InternalMode)0);
			_list = new ItemList
			{
				SelectMode = (SelectModeEnum)0,
				AllowReselect = true
			};
			((Control)_list).SizeFlagsHorizontal = (SizeFlags)3;
			((Control)_list).SizeFlagsVertical = (SizeFlags)3;
			((Node)val3).AddChild((Node)(object)_list, false, (InternalMode)0);
			_sideScroll = new ScrollContainer
			{
				HorizontalScrollMode = (ScrollMode)0,
				VerticalScrollMode = (ScrollMode)1,
				SizeFlagsVertical = (SizeFlags)3
			};
			((Node)val3).AddChild((Node)(object)_sideScroll, false, (InternalMode)0);
			_side = new VBoxContainer();
			((Control)_side).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
			((Control)_side).SizeFlagsHorizontal = (SizeFlags)3;
			((Node)_sideScroll).AddChild((Node)(object)_side, false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_up"), delegate
			{
				MoveSelected(-1);
			}), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_down"), delegate
			{
				MoveSelected(1);
			}), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_top"), MoveToTop), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_bottom"), MoveToBottom), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_sort_alpha"), SortByAlphabet), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_sort_smart"), SmartSort), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_toggle_enable"), ToggleSelectedEnabled), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_reload"), RefreshFromRuntime), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_export_clipboard"), ExportToClipboard), false, (InternalMode)0);
			((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_import_clipboard"), ImportFromClipboard), false, (InternalMode)0);
			_footer = new HBoxContainer();
			((Control)_footer).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
			((Node)_root).AddChild((Node)(object)_footer, false, (InternalMode)0);
			((Node)_footer).AddChild((Node)(object)MakeButton(I18n.T("btn_apply"), ApplyOrder), false, (InternalMode)0);
			((Node)_footer).AddChild((Node)(object)MakeButton(I18n.T("btn_close"), delegate
			{
				((CanvasItem)this).Visible = false;
			}), false, (InternalMode)0);
			_statusLabel = new Label
			{
				Text = I18n.T("status_ready"),
				SizeFlagsHorizontal = (SizeFlags)3,
				AutowrapMode = (AutowrapMode)3
			};
			_statusLabel.HorizontalAlignment = (HorizontalAlignment)2;
			((Node)_footer).AddChild((Node)(object)_statusLabel, false, (InternalMode)0);
			LayoutForViewport();
		}
	}

	private Button MakeButton(string text, Action onPressed)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		Button val = new Button
		{
			Text = text,
			FocusMode = (FocusModeEnum)2
		};
		((BaseButton)val).Pressed += onPressed;
		return val;
	}

	private void LayoutForViewport()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		if (_uiBuilt && _dialog != null)
		{
			Vector2? simulatedViewport = SimulatedViewport;
			Vector2 layoutViewport;
			if (!simulatedViewport.HasValue)
			{
				Rect2 viewportRect = ((CanvasItem)this).GetViewportRect();
				layoutViewport = ((Rect2)(ref viewportRect)).Size;
			}
			else
			{
				layoutViewport = simulatedViewport.GetValueOrDefault();
			}
			Vector2 val = (_layoutViewport = layoutViewport);
			bool flag = ForceCompact || val.Y < 720f || val.X < 1100f;
			float num = (flag ? 12f : 24f);
			float num2 = (flag ? 12f : 48f);
			float num3 = (flag ? 12f : 48f);
			float num4 = Mathf.Clamp(val.X - num * 2f, 640f, 940f);
			float num5 = Mathf.Max(val.Y - num2 - num3, 260f);
			((Control)_dialog).AnchorLeft = 0.5f;
			((Control)_dialog).AnchorRight = 0.5f;
			((Control)_dialog).AnchorTop = 0f;
			((Control)_dialog).AnchorBottom = 0f;
			((Control)_dialog).OffsetLeft = (0f - num4) / 2f;
			((Control)_dialog).OffsetRight = num4 / 2f;
			((Control)_dialog).OffsetTop = num2;
			((Control)_dialog).OffsetBottom = num2 + num5;
			((CanvasItem)_subtitle).Visible = !flag;
			float num6 = (flag ? 150f : 180f);
			float num7 = Mathf.Clamp(num5 - FixedChromeMinHeight(), 120f, 410f);
			((Control)_list).CustomMinimumSize = new Vector2(Mathf.Max(num4 - num6 - 40f, 320f), num7);
			((Control)_sideScroll).CustomMinimumSize = new Vector2(num6, Mathf.Min(300f, num7));
			DebugLog.Info($"UI layout: viewport={val.X:0}x{val.Y:0}, compact={flag}, dialog={num4:0}x{num5:0}, top={num2:0}, listMinHeight={num7:0}");
			ScheduleLayoutVerification();
		}
	}

	private void ScheduleLayoutVerification()
	{
		_layoutStepsRemaining = 2;
		_layoutCheckFramesPending = 2;
		SceneTree tree = ((Node)this).GetTree();
		if (tree != null && !_layoutCheckHooked)
		{
			_layoutCheckHooked = true;
			tree.ProcessFrame += OnProcessFrame;
		}
	}

	private void OnProcessFrame()
	{
		if (_layoutStepsRemaining > 0)
		{
			if (_layoutCheckFramesPending > 0)
			{
				_layoutCheckFramesPending--;
			}
			else if (_layoutStepsRemaining == 2)
			{
				_layoutStepsRemaining = 1;
				CorrectLayoutOverflow();
				_layoutCheckFramesPending = 2;
			}
			else
			{
				_layoutStepsRemaining = 0;
				LogLayoutCheck();
			}
		}
	}

	private void CorrectLayoutOverflow()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		if (_dialog == null || _list == null)
		{
			return;
		}
		float num = ((Control)_dialog).OffsetBottom - ((Control)_dialog).OffsetTop;
		float num2 = ((Control)_dialog).Size.Y - num;
		if (!(num2 <= 1f))
		{
			float y = ((Control)_list).CustomMinimumSize.Y;
			float num3 = Mathf.Max(120f, y - num2);
			if (!(Mathf.Abs(num3 - y) < 0.5f))
			{
				((Control)_list).CustomMinimumSize = new Vector2(((Control)_list).CustomMinimumSize.X, num3);
				DebugLog.Info($"UI layout correction: panel exceeded its box by {num2:0}px, list minimum {y:0} -> {num3:0}");
			}
		}
	}

	private float FixedChromeMinHeight()
	{
		float sum = 0f;
		int visible = 0;
		Add((Control)(object)_title, 44f);
		Add((Control)(object)_subtitle, 70f);
		Add((Control)(object)_warningLabel, 56f);
		Add((Control)(object)_presetBar, 220f);
		Add((Control)(object)_footer, 80f);
		visible++;
		if (visible > 1)
		{
			sum += 8f * (float)(visible - 1);
		}
		sum += 24f;
		return sum + 8f;
		void Add(Control control, float cap)
		{
			if (((CanvasItem)control).Visible)
			{
				sum += BoundedMinHeight(control, cap);
				visible++;
			}
		}
	}

	private static float BoundedMinHeight(Control control, float cap)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		float y = control.GetCombinedMinimumSize().Y;
		if (float.IsNaN(y) || y <= 0f)
		{
			return cap;
		}
		return Mathf.Min(y, cap);
	}

	private void LogLayoutCheck()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		if (((Node)this).IsInsideTree() && _dialog != null && _footer != null)
		{
			Vector2 layoutViewport = _layoutViewport;
			float num = ((Control)_footer).GlobalPosition.Y + ((Control)_footer).Size.Y;
			float num2 = ((Control)_dialog).Position.Y + ((Control)_dialog).Size.Y;
			bool flag = num <= layoutViewport.Y + 0.5f && ((Control)_footer).GlobalPosition.Y >= -0.5f;
			bool flag2 = num2 <= layoutViewport.Y + 0.5f;
			DebugLog.Info($"UI layout check: dialogPos=({((Control)_dialog).Position.X:0},{((Control)_dialog).Position.Y:0}) dialogSize={((Control)_dialog).Size.X:0}x{((Control)_dialog).Size.Y:0}, footerEndY={num:0}, viewport={layoutViewport.X:0}x{layoutViewport.Y:0}, footerInside={flag}, dialogInside={flag2}" + ((flag && flag2) ? string.Empty : "  <-- LAYOUT OVERFLOW"));
		}
	}

	private static bool ReadBoolEnv(string name)
	{
		try
		{
			string environmentVariable = Environment.GetEnvironmentVariable(name);
			if (string.IsNullOrWhiteSpace(environmentVariable))
			{
				return false;
			}
			return environmentVariable != "0" && !environmentVariable.Equals("false", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static Vector2? ReadSimulatedViewport()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string environmentVariable = Environment.GetEnvironmentVariable("LOADORDER_UI_SIMULATE_H");
			if (string.IsNullOrWhiteSpace(environmentVariable))
			{
				return null;
			}
			if (!float.TryParse(environmentVariable, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return null;
			}
			if (result <= 0f)
			{
				return null;
			}
			float num = 1163f;
			string environmentVariable2 = Environment.GetEnvironmentVariable("LOADORDER_UI_SIMULATE_W");
			if (!string.IsNullOrWhiteSpace(environmentVariable2) && float.TryParse(environmentVariable2, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && result2 > 0f)
			{
				num = result2;
			}
			return new Vector2(num, result);
		}
		catch
		{
			return null;
		}
	}

	private void RefreshFromRuntime()
	{
		if (!LoadOrderRuntime.TryGetOrderedEntries(out List<LoadOrderEntry> entries, out string error))
		{
			_entries.Clear();
			RefreshListOnly();
			SetStatus(I18n.Tf("status_load_failed", error));
			DebugLog.Error("Refresh failed: " + error);
			RefreshOverwriteWarning();
			return;
		}
		_entries.Clear();
		_entries.AddRange(entries);
		ModPresetStore.MigrateIfNeeded(_entries);
		int num = ModPresetStore.ApplyNewModPolicy(ModPresetStore.CurrentProfile, _entries);
		RefreshListOnly();
		RefreshPresetDropdown();
		RefreshPolicyCheckbox();
		if (_entries.Count > 0)
		{
			_list.Select(0, true);
		}
		SetStatus((num > 0) ? I18n.Tf("status_new_mods_seen", num) : I18n.Tf("status_loaded", _entries.Count));
		DebugLog.Info($"Loaded {_entries.Count} mods into panel. newMods={num}");
		RefreshOverwriteWarning();
	}

	private void RefreshOverwriteWarning()
	{
		if (LoadOrderRuntime.TryBuildOverwriteWarning(out string warningText) && !string.IsNullOrWhiteSpace(warningText))
		{
			_warningLabel.Text = warningText;
			((Control)_warningLabel).TooltipText = warningText;
			((CanvasItem)_warningLabel).Visible = true;
		}
		else
		{
			_warningLabel.Text = string.Empty;
			((Control)_warningLabel).TooltipText = string.Empty;
			((CanvasItem)_warningLabel).Visible = false;
		}
		LayoutForViewport();
	}

	private void RefreshListOnly()
	{
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		_list.Clear();
		for (int i = 0; i < _entries.Count; i++)
		{
			LoadOrderEntry loadOrderEntry = _entries[i];
			string value = (loadOrderEntry.IsEnabled ? "  ✓" : "  ✗");
			string value2 = (loadOrderEntry.IsNew ? ("  · " + I18n.T("tag_new")) : string.Empty);
			string text = $"{i + 1:00}. [{loadOrderEntry.SourceText}] {loadOrderEntry.Name} ({loadOrderEntry.Id}){value}{value2}";
			_list.AddItem(text, (Texture2D)null, true);
			if (!loadOrderEntry.IsEnabled)
			{
				_list.SetItemCustomFgColor(i, new Color(0.55f, 0.55f, 0.55f, 1f));
			}
		}
	}

	private int GetSelectedIndex()
	{
		int[] selectedItems = _list.GetSelectedItems();
		if (selectedItems.Length != 0)
		{
			return selectedItems[0];
		}
		return -1;
	}

	private void MoveSelected(int delta)
	{
		int selectedIndex = GetSelectedIndex();
		if (selectedIndex >= 0)
		{
			int num = selectedIndex + delta;
			if (num >= 0 && num < _entries.Count)
			{
				List<LoadOrderEntry> entries = _entries;
				int index = selectedIndex;
				List<LoadOrderEntry> entries2 = _entries;
				int index2 = num;
				LoadOrderEntry value = _entries[num];
				LoadOrderEntry value2 = _entries[selectedIndex];
				entries[index] = value;
				entries2[index2] = value2;
				RefreshListOnly();
				_list.Select(num, true);
				SetStatus(I18n.Tf("status_moved_pos", num + 1));
			}
		}
	}

	private void MoveToTop()
	{
		int selectedIndex = GetSelectedIndex();
		if (selectedIndex > 0)
		{
			LoadOrderEntry item = _entries[selectedIndex];
			_entries.RemoveAt(selectedIndex);
			_entries.Insert(0, item);
			RefreshListOnly();
			_list.Select(0, true);
			SetStatus(I18n.T("status_moved_top"));
		}
	}

	private void MoveToBottom()
	{
		int selectedIndex = GetSelectedIndex();
		if (selectedIndex >= 0 && selectedIndex < _entries.Count - 1)
		{
			LoadOrderEntry item = _entries[selectedIndex];
			_entries.RemoveAt(selectedIndex);
			_entries.Add(item);
			RefreshListOnly();
			_list.Select(_entries.Count - 1, true);
			SetStatus(I18n.T("status_moved_bottom"));
		}
	}

	private void SortByAlphabet()
	{
		if (_entries.Count < 2)
		{
			SetStatus(I18n.T("status_nothing_to_sort"));
			return;
		}
		int selectedIndex = GetSelectedIndex();
		string selectedKey = ((selectedIndex >= 0 && selectedIndex < _entries.Count) ? _entries[selectedIndex].Key : null);
		_entries.Sort(delegate(LoadOrderEntry a, LoadOrderEntry b)
		{
			int num2 = string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
			if (num2 != 0)
			{
				return num2;
			}
			int num3 = string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
			return (num3 != 0) ? num3 : a.Source.CompareTo(b.Source);
		});
		RefreshListOnly();
		if (!string.IsNullOrEmpty(selectedKey))
		{
			int num = _entries.FindIndex((LoadOrderEntry e) => string.Equals(e.Key, selectedKey, StringComparison.Ordinal));
			if (num >= 0)
			{
				_list.Select(num, true);
			}
		}
		SetStatus(I18n.T("status_sorted_alpha"));
		DebugLog.Info($"Manual alphabetical sort completed. entries={_entries.Count}");
	}

	private static string SortKeyFor(LoadOrderEntry e)
	{
		return ((IsLibraryMod(e.Id) || IsLibraryMod(e.Name)) ? "0_" : "1_") + e.Name;
	}

	private static bool IsLibraryMod(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = text.ToLowerInvariant();
		string[] libKeywords = LibKeywords;
		foreach (string text3 in libKeywords)
		{
			if (text2 == text3)
			{
				return true;
			}
			if (text2.StartsWith(text3))
			{
				return true;
			}
			for (int j = 1; j < text2.Length; j++)
			{
				if (char.IsUpper(text[j]) && text2.Substring(0, j) == text3)
				{
					return true;
				}
			}
			if (text2.EndsWith(text3, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private void SmartSort()
	{
		if (_entries.Count < 2)
		{
			SetStatus(I18n.T("status_nothing_to_sort"));
			return;
		}
		int selectedIndex = GetSelectedIndex();
		string selectedKey = ((selectedIndex >= 0 && selectedIndex < _entries.Count) ? _entries[selectedIndex].Key : null);
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < _entries.Count; i++)
		{
			if (!string.IsNullOrWhiteSpace(_entries[i].Id))
			{
				dictionary.TryAdd(_entries[i].Id, i);
			}
		}
		int[] array = new int[_entries.Count];
		List<List<int>> list = new List<List<int>>();
		for (int j = 0; j < _entries.Count; j++)
		{
			list.Add(new List<int>());
		}
		for (int k = 0; k < _entries.Count; k++)
		{
			foreach (string dependency in _entries[k].Dependencies)
			{
				if (dictionary.TryGetValue(dependency, out var value))
				{
					array[k]++;
					list[value].Add(k);
				}
			}
		}
		for (int l = 0; l < _entries.Count; l++)
		{
			if (array[l] > 0)
			{
				continue;
			}
			string text = _entries[l].Id ?? string.Empty;
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			for (int m = 0; m < _entries.Count; m++)
			{
				if (l == m)
				{
					continue;
				}
				foreach (string dependency2 in _entries[m].Dependencies)
				{
					if (string.Equals(dependency2, text, StringComparison.OrdinalIgnoreCase))
					{
						array[m]++;
						list[l].Add(m);
						break;
					}
				}
			}
		}
		PriorityQueue<int, string> priorityQueue = new PriorityQueue<int, string>(StringComparer.CurrentCultureIgnoreCase);
		for (int n = 0; n < _entries.Count; n++)
		{
			if (array[n] == 0)
			{
				priorityQueue.Enqueue(n, SortKeyFor(_entries[n]));
			}
		}
		List<LoadOrderEntry> list2 = new List<LoadOrderEntry>();
		HashSet<int> hashSet = new HashSet<int>();
		while (priorityQueue.Count > 0)
		{
			int num = priorityQueue.Dequeue();
			hashSet.Add(num);
			list2.Add(_entries[num]);
			foreach (int item in list[num])
			{
				array[item]--;
				if (array[item] == 0)
				{
					priorityQueue.Enqueue(item, SortKeyFor(_entries[item]));
				}
			}
		}
		List<LoadOrderEntry> list3 = new List<LoadOrderEntry>();
		for (int num2 = 0; num2 < _entries.Count; num2++)
		{
			if (!hashSet.Contains(num2))
			{
				list3.Add(_entries[num2]);
			}
		}
		list3.Sort((LoadOrderEntry a, LoadOrderEntry b) => string.Compare(SortKeyFor(a), SortKeyFor(b), StringComparison.CurrentCultureIgnoreCase));
		list2.AddRange(list3);
		_entries.Clear();
		_entries.AddRange(list2);
		RefreshListOnly();
		if (!string.IsNullOrEmpty(selectedKey))
		{
			int num3 = _entries.FindIndex((LoadOrderEntry e) => string.Equals(e.Key, selectedKey, StringComparison.Ordinal));
			if (num3 >= 0)
			{
				_list.Select(num3, true);
			}
		}
		int count = list3.Count;
		SetStatus((count > 0) ? I18n.Tf("status_sorted_smart_circular", count) : I18n.T("status_sorted_smart"));
		DebugLog.Info($"Smart sort completed. entries={_entries.Count}, circular={count}");
	}

	private void ToggleSelectedEnabled()
	{
		int selectedIndex = GetSelectedIndex();
		if (selectedIndex >= 0)
		{
			LoadOrderEntry loadOrderEntry = _entries[selectedIndex];
			loadOrderEntry.IsEnabled = !loadOrderEntry.IsEnabled;
			loadOrderEntry.IsNew = false;
			RefreshListOnly();
			_list.Select(selectedIndex, true);
			string text = (loadOrderEntry.IsEnabled ? "ON" : "OFF");
			SetStatus(I18n.Tf("status_toggled", loadOrderEntry.Name, text));
			DebugLog.Info($"Toggled {loadOrderEntry.Id} to {loadOrderEntry.IsEnabled}.");
		}
	}

	private void RefreshPresetDropdown()
	{
		if (_presetDropdown != null)
		{
			_suppressPresetEvent = true;
			PresetSaveData presetSaveData = ModPresetStore.Load();
			_presetDropdown.Clear();
			for (int i = 0; i < presetSaveData.Profiles.Count; i++)
			{
				_presetDropdown.AddItem(presetSaveData.Profiles[i].Name, i);
			}
			_presetDropdown.Select(presetSaveData.CurrentProfileIndex);
			_suppressPresetEvent = false;
		}
	}

	private void RefreshPolicyCheckbox()
	{
		if (_newModPolicyCheck != null)
		{
			ModPreset currentProfile = ModPresetStore.CurrentProfile;
			((BaseButton)_newModPolicyCheck).SetPressedNoSignal(currentProfile.AutoEnableNewMods);
			((Control)_newModPolicyCheck).TooltipText = I18n.T("policy_auto_enable_tooltip");
		}
	}

	private void OnNewModPolicyToggled(bool pressed)
	{
		ModPreset currentProfile = ModPresetStore.CurrentProfile;
		if (!ModPresetStore.SetAutoEnableNewMods(pressed))
		{
			SetStatus(I18n.T("status_save_failed"));
			return;
		}
		int value = ModPresetStore.ApplyNewModPolicy(ModPresetStore.CurrentProfile, _entries);
		RefreshListOnly();
		SetStatus(I18n.Tf("status_policy_changed", currentProfile.Name, I18n.T(pressed ? "policy_on" : "policy_off")));
		DebugLog.Info($"Preset '{currentProfile.Name}' AutoEnableNewMods={pressed}; newMods={value}");
	}

	private void OnPresetSelected(long index)
	{
		if (!_suppressPresetEvent && index >= 0 && index < ModPresetStore.Load().Profiles.Count)
		{
			ModPresetStore.SelectProfile((int)index, _entries);
			ModPreset currentProfile = ModPresetStore.CurrentProfile;
			int num = ModPresetStore.ApplyPreset(currentProfile, _entries);
			RefreshListOnly();
			RefreshPolicyCheckbox();
			string text = I18n.Tf("status_preset_applied", currentProfile.Name);
			if (num > 0)
			{
				text = text + " " + I18n.Tf("status_new_mods_seen", num);
			}
			SetStatus(text);
			DebugLog.Info($"Preset switched to '{currentProfile.Name}'. autoEnableNewMods={currentProfile.AutoEnableNewMods}, newMods={num}");
		}
	}

	private void CreatePresetFromCurrent()
	{
		PresetSaveData presetSaveData = ModPresetStore.Load();
		string name = "Profile " + (presetSaveData.Profiles.Count + 1);
		CreatePresetFromCurrent(name);
	}

	private void CreatePresetFromCurrent(string name)
	{
		ModPresetStore.CreatePreset(name, _entries);
		RefreshPresetDropdown();
		RefreshPolicyCheckbox();
		SetStatus(I18n.Tf("status_preset_created", name));
		DebugLog.Info("Created preset '" + name + "'.");
	}

	private void RenameCurrentPreset()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		ConfirmationDialog val = new ConfirmationDialog
		{
			Title = I18n.T("preset_rename_title"),
			DialogText = I18n.T("preset_rename_prompt")
		};
		LineEdit lineEdit = new LineEdit
		{
			Text = ModPresetStore.CurrentProfile.Name
		};
		((Node)val).AddChild((Node)(object)lineEdit, false, (InternalMode)0);
		((AcceptDialog)val).RegisterTextEnter(lineEdit);
		((AcceptDialog)val).Confirmed += delegate
		{
			string text = lineEdit.Text;
			if (ModPresetStore.RenameCurrentPreset(text))
			{
				RefreshPresetDropdown();
				RefreshPolicyCheckbox();
				SetStatus(I18n.Tf("status_preset_renamed", text.Trim()));
				DebugLog.Info("Preset renamed to '" + text.Trim() + "'.");
			}
		};
		((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
		((Window)val).PopupCentered((Vector2I?)null);
	}

	private void DeleteCurrentPreset()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		if (ModPresetStore.Load().Profiles.Count <= 1)
		{
			SetStatus(I18n.T("status_preset_cannot_delete_last"));
			return;
		}
		string presetName = ModPresetStore.CurrentProfile.Name;
		ConfirmationDialog val = new ConfirmationDialog();
		((Window)val).Title = I18n.T("preset_delete_title");
		((AcceptDialog)val).DialogText = I18n.Tf("preset_delete_prompt", presetName);
		ConfirmationDialog val2 = val;
		((AcceptDialog)val2).Confirmed += delegate
		{
			if (ModPresetStore.DeleteCurrentPreset())
			{
				RefreshPresetDropdown();
				RefreshPolicyCheckbox();
				SetStatus(I18n.Tf("status_preset_deleted", presetName));
				DebugLog.Info("Preset '" + presetName + "' deleted.");
			}
		};
		((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Window)val2).PopupCentered((Vector2I?)null);
	}

	private void ExportToClipboard()
	{
		if (_entries.Count == 0)
		{
			SetStatus(I18n.T("status_nothing_to_export"));
			return;
		}
		try
		{
			PresetSaveData presetSaveData = ModPresetStore.Load();
			PresetExportData presets = new PresetExportData
			{
				current_preset = presetSaveData.Profiles[presetSaveData.CurrentProfileIndex].Name,
				profiles = presetSaveData.Profiles.Select((ModPreset p) => new PresetExportEntry
				{
					name = p.Name,
					disabled_mods = p.DisabledMods.ToList(),
					auto_enable_new_mods = p.AutoEnableNewMods,
					known_mods = p.KnownMods.ToList()
				}).ToList()
			};
			ClipboardPayload clipboardPayload = new ClipboardPayload
			{
				exported_at_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
				entries = _entries.Select((LoadOrderEntry e) => new ClipboardEntry
				{
					key = e.Key,
					id = e.Id,
					source = e.Source,
					is_enabled = e.IsEnabled
				}).ToList(),
				presets = presets
			};
			DisplayServer.ClipboardSet(JsonSerializer.Serialize(clipboardPayload, new JsonSerializerOptions
			{
				WriteIndented = true
			}));
			SetStatus(I18n.Tf("status_exported_clipboard", clipboardPayload.entries.Count));
			DebugLog.Info($"Exported load-order config to clipboard. entries={clipboardPayload.entries.Count}");
		}
		catch (Exception ex)
		{
			SetStatus(I18n.Tf("status_export_failed", ex.Message));
			DebugLog.Error("ExportToClipboard failed.", ex);
		}
	}

	private void ImportFromClipboard()
	{
		try
		{
			string text = DisplayServer.ClipboardGet();
			if (string.IsNullOrWhiteSpace(text))
			{
				SetStatus(I18n.T("status_clipboard_empty"));
				return;
			}
			if (!TryParseClipboard(text, out List<ClipboardEntry> entries, out string error))
			{
				SetStatus(I18n.Tf("status_import_parse_failed", error));
				return;
			}
			if (entries.Count == 0)
			{
				SetStatus(I18n.T("status_import_no_valid"));
				return;
			}
			List<LoadOrderEntry> list = new List<LoadOrderEntry>();
			HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
			int num = 0;
			int num2 = 0;
			foreach (ClipboardEntry item in entries)
			{
				if (!TryResolveImportedEntry(item, out LoadOrderEntry entry))
				{
					num++;
					continue;
				}
				if (!seen.Add(entry.Key))
				{
					num2++;
					continue;
				}
				bool? flag = item.ResolveEnabled();
				if (flag.HasValue)
				{
					entry.IsEnabled = flag.Value;
					entry.IsNew = false;
				}
				list.Add(entry);
			}
			if (list.Count == 0)
			{
				SetStatus(I18n.Tf("status_import_no_match", num));
				DebugLog.Warn($"Clipboard import found no matching mods. parsed={entries.Count}, missing={num}");
				return;
			}
			List<LoadOrderEntry> collection = _entries.Where((LoadOrderEntry e) => !seen.Contains(e.Key)).ToList();
			_entries.Clear();
			_entries.AddRange(list);
			_entries.AddRange(collection);
			RefreshListOnly();
			if (_entries.Count > 0)
			{
				_list.Select(0, true);
			}
			SetStatus(I18n.Tf("status_imported_clipboard", list.Count, num, num2));
			DebugLog.Info($"Imported load-order config from clipboard. parsed={entries.Count}, matched={list.Count}, missing={num}, duplicates={num2}");
		}
		catch (Exception ex)
		{
			SetStatus(I18n.Tf("status_import_failed", ex.Message));
			DebugLog.Error("ImportFromClipboard failed.", ex);
		}
	}

	private bool TryParseClipboard(string raw, out List<ClipboardEntry> entries, out string error)
	{
		entries = new List<ClipboardEntry>();
		error = string.Empty;
		if (TryParseClipboardJson(raw, out entries, out error))
		{
			return true;
		}
		entries = ParsePlainTextClipboard(raw);
		if (entries.Count > 0)
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(error))
		{
			error = I18n.T("status_import_invalid_format");
		}
		return false;
	}

	private bool TryParseClipboardJson(string raw, out List<ClipboardEntry> entries, out string error)
	{
		entries = new List<ClipboardEntry>();
		error = string.Empty;
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(raw);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind == JsonValueKind.Object)
			{
				if (rootElement.TryGetProperty("entries", out var value))
				{
					entries = ParseClipboardJsonArray(value);
					return true;
				}
				if (rootElement.TryGetProperty("mod_list", out var value2))
				{
					entries = ParseClipboardJsonArray(value2);
					return true;
				}
				return false;
			}
			if (rootElement.ValueKind == JsonValueKind.Array)
			{
				entries = ParseClipboardJsonArray(rootElement);
				return true;
			}
			return false;
		}
		catch (JsonException ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private List<ClipboardEntry> ParseClipboardJsonArray(JsonElement element)
	{
		List<ClipboardEntry> list = new List<ClipboardEntry>();
		if (element.ValueKind != JsonValueKind.Array)
		{
			return list;
		}
		foreach (JsonElement item in element.EnumerateArray())
		{
			switch (item.ValueKind)
			{
			case JsonValueKind.String:
			{
				string text = item.GetString() ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(new ClipboardEntry
					{
						key = text.Trim(),
						id = text.Trim()
					});
				}
				break;
			}
			case JsonValueKind.Object:
			{
				ClipboardEntry clipboardEntry = new ClipboardEntry
				{
					key = GetJsonString(item, "key"),
					id = GetJsonString(item, "id"),
					source = GetJsonInt(item, "source"),
					is_enabled = (GetJsonBool(item, "is_enabled") ?? GetJsonBool(item, "isEnabled")),
					enabled = GetJsonBool(item, "enabled")
				};
				if (!string.IsNullOrWhiteSpace(clipboardEntry.key) || !string.IsNullOrWhiteSpace(clipboardEntry.id) || clipboardEntry.source.HasValue)
				{
					list.Add(clipboardEntry);
				}
				break;
			}
			}
		}
		return list;
	}

	private static string GetJsonString(JsonElement obj, string key)
	{
		if (obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
		{
			return value.GetString() ?? string.Empty;
		}
		return string.Empty;
	}

	private static int? GetJsonInt(JsonElement obj, string key)
	{
		if (obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var value2))
		{
			return value2;
		}
		return null;
	}

	private static bool? GetJsonBool(JsonElement obj, string key)
	{
		if (obj.TryGetProperty(key, out var value))
		{
			if (value.ValueKind == JsonValueKind.True)
			{
				return true;
			}
			if (value.ValueKind == JsonValueKind.False)
			{
				return false;
			}
		}
		return null;
	}

	private List<ClipboardEntry> ParsePlainTextClipboard(string raw)
	{
		List<ClipboardEntry> list = new List<ClipboardEntry>();
		string[] array = TokenSplitRegex.Split(raw);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim().Trim('"', '\'');
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(new ClipboardEntry
				{
					key = text,
					id = text
				});
			}
		}
		return list;
	}

	private bool TryResolveImportedEntry(ClipboardEntry imported, out LoadOrderEntry entry)
	{
		entry = null;
		string text = imported.key?.Trim() ?? string.Empty;
		string importedId = imported.id?.Trim() ?? string.Empty;
		int? importedSource = imported.source;
		if (!string.IsNullOrWhiteSpace(text) && TryFindByKey(text, out entry))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(text) && TryParseEntryKey(text, out string id, out int source))
		{
			importedId = id;
			int valueOrDefault = importedSource.GetValueOrDefault();
			if (!importedSource.HasValue)
			{
				valueOrDefault = source;
				importedSource = valueOrDefault;
			}
		}
		if (string.IsNullOrWhiteSpace(importedId) && !string.IsNullOrWhiteSpace(text))
		{
			importedId = text;
		}
		if (string.IsNullOrWhiteSpace(importedId))
		{
			return false;
		}
		if (importedSource.HasValue)
		{
			string key = LoadOrderEntry.BuildKey(importedId, importedSource.Value);
			if (TryFindByKey(key, out entry))
			{
				return true;
			}
		}
		List<LoadOrderEntry> list = _entries.Where((LoadOrderEntry e) => string.Equals(e.Id, importedId, StringComparison.OrdinalIgnoreCase)).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		if (list.Count == 1)
		{
			entry = list[0];
			return true;
		}
		if (importedSource.HasValue)
		{
			LoadOrderEntry loadOrderEntry = list.FirstOrDefault((LoadOrderEntry e) => e.Source == importedSource.Value);
			if (loadOrderEntry != null)
			{
				entry = loadOrderEntry;
				return true;
			}
		}
		entry = list.FirstOrDefault((LoadOrderEntry e) => e.Source == 1) ?? list[0];
		return true;
	}

	private bool TryFindByKey(string key, out LoadOrderEntry entry)
	{
		string key2 = key;
		entry = _entries.FirstOrDefault((LoadOrderEntry e) => string.Equals(e.Key, key2, StringComparison.OrdinalIgnoreCase));
		return entry != null;
	}

	private static bool TryParseEntryKey(string key, out string id, out int source)
	{
		id = string.Empty;
		source = 0;
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		int num = key.LastIndexOf("::", StringComparison.Ordinal);
		if (num <= 0 || num >= key.Length - 2)
		{
			return false;
		}
		string text = key.Substring(0, num).Trim();
		int num2 = num + 2;
		string s = key.Substring(num2, key.Length - num2).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return false;
		}
		id = text;
		source = result;
		return true;
	}

	private void ApplyOrder()
	{
		string error;
		if (_entries.Count == 0)
		{
			SetStatus(I18n.T("status_nothing_to_save"));
		}
		else if (!LoadOrderRuntime.TrySaveOrderedEntries(_entries, out error))
		{
			SetStatus(I18n.Tf("status_save_failed", error));
			DebugLog.Error("Apply failed: " + error);
			RefreshOverwriteWarning();
		}
		else
		{
			ModPresetStore.CommitCurrentProfile(_entries);
			SetStatus(I18n.T("status_saved"));
			DebugLog.Info("Apply succeeded. Preset '" + ModPresetStore.CurrentProfile.Name + "' committed. Restart required for effect.");
			RefreshOverwriteWarning();
		}
	}

	private void SetStatus(string text)
	{
		_statusLabel.Text = text;
	}

	internal void DebugLogLayoutCheck()
	{
		LogLayoutCheck();
	}

	internal void DebugSelectPreset(int index)
	{
		OnPresetSelected(index);
	}

	internal void DebugCreatePreset(string name)
	{
		CreatePresetFromCurrent(name);
	}

	internal void DebugApplyOrder()
	{
		ApplyOrder();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Expected O, but got Unknown
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_040f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_043e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0476: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_0529: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_057e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0587: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0601: Unknown result type (might be due to invalid IL or missing references)
		//IL_0624: Unknown result type (might be due to invalid IL or missing references)
		//IL_062f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0655: Unknown result type (might be due to invalid IL or missing references)
		//IL_065e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0684: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0707: Unknown result type (might be due to invalid IL or missing references)
		//IL_0710: Unknown result type (might be due to invalid IL or missing references)
		//IL_0736: Unknown result type (might be due to invalid IL or missing references)
		//IL_073f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0765: Unknown result type (might be due to invalid IL or missing references)
		//IL_076e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0794: Unknown result type (might be due to invalid IL or missing references)
		//IL_079d: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0817: Unknown result type (might be due to invalid IL or missing references)
		//IL_0820: Unknown result type (might be due to invalid IL or missing references)
		//IL_0846: Unknown result type (might be due to invalid IL or missing references)
		//IL_0869: Unknown result type (might be due to invalid IL or missing references)
		//IL_0874: Unknown result type (might be due to invalid IL or missing references)
		//IL_089a: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f7: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(39)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OpenPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.BuildUiIfNeeded, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.LayoutForViewport, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ScheduleLayoutVerification, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnProcessFrame, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CorrectLayoutOverflow, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.FixedChromeMinHeight, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.BoundedMinHeight, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("control"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
				new PropertyInfo((Type)3, StringName.op_Implicit("cap"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.LogLayoutCheck, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ReadBoolEnv, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("name"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshFromRuntime, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshOverwriteWarning, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshListOnly, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetSelectedIndex, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.MoveSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.MoveToTop, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.MoveToBottom, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SortByAlphabet, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.IsLibraryMod, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SmartSort, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ToggleSelectedEnabled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshPresetDropdown, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshPolicyCheckbox, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnNewModPolicyToggled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("pressed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnPresetSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CreatePresetFromCurrent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreatePresetFromCurrent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("name"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RenameCurrentPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.DeleteCurrentPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ExportToClipboard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ImportFromClipboard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyOrder, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.DebugLogLayoutCheck, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.DebugSelectPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.DebugCreatePreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("name"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.DebugApplyOrder, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_046f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Unknown result type (might be due to invalid IL or missing references)
		//IL_055b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0580: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_060b: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Unknown result type (might be due to invalid IL or missing references)
		//IL_063e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
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
		if ((ref method) == MethodName.OpenPanel && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OpenPanel();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.BuildUiIfNeeded && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			BuildUiIfNeeded();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.LayoutForViewport && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			LayoutForViewport();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ScheduleLayoutVerification && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ScheduleLayoutVerification();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnProcessFrame && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnProcessFrame();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CorrectLayoutOverflow && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CorrectLayoutOverflow();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.FixedChromeMinHeight && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			float num = FixedChromeMinHeight();
			ret = VariantUtils.CreateFrom<float>(ref num);
			return true;
		}
		if ((ref method) == MethodName.BoundedMinHeight && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			float num2 = BoundedMinHeight(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<float>(ref num2);
			return true;
		}
		if ((ref method) == MethodName.LogLayoutCheck && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			LogLayoutCheck();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReadBoolEnv && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = ReadBoolEnv(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.RefreshFromRuntime && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshFromRuntime();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshOverwriteWarning && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshOverwriteWarning();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshListOnly && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshListOnly();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetSelectedIndex && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			int selectedIndex = GetSelectedIndex();
			ret = VariantUtils.CreateFrom<int>(ref selectedIndex);
			return true;
		}
		if ((ref method) == MethodName.MoveSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			MoveSelected(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.MoveToTop && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			MoveToTop();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.MoveToBottom && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			MoveToBottom();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SortByAlphabet && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SortByAlphabet();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.IsLibraryMod && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag2 = IsLibraryMod(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
			return true;
		}
		if ((ref method) == MethodName.SmartSort && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SmartSort();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ToggleSelectedEnabled && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ToggleSelectedEnabled();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshPresetDropdown && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshPresetDropdown();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshPolicyCheckbox && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshPolicyCheckbox();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnNewModPolicyToggled && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnNewModPolicyToggled(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnPresetSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnPresetSelected(VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreatePresetFromCurrent && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CreatePresetFromCurrent();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreatePresetFromCurrent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			CreatePresetFromCurrent(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RenameCurrentPreset && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RenameCurrentPreset();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.DeleteCurrentPreset && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			DeleteCurrentPreset();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ExportToClipboard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ExportToClipboard();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ImportFromClipboard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ImportFromClipboard();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyOrder && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyOrder();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.DebugLogLayoutCheck && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			DebugLogLayoutCheck();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.DebugSelectPreset && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			DebugSelectPreset(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.DebugCreatePreset && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			DebugCreatePreset(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.DebugApplyOrder && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			DebugApplyOrder();
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.BoundedMinHeight && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			float num = BoundedMinHeight(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<float>(ref num);
			return true;
		}
		if ((ref method) == MethodName.ReadBoolEnv && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = ReadBoolEnv(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.IsLibraryMod && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag2 = IsLibraryMod(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
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
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName.OpenPanel)
		{
			return true;
		}
		if ((ref method) == MethodName.BuildUiIfNeeded)
		{
			return true;
		}
		if ((ref method) == MethodName.LayoutForViewport)
		{
			return true;
		}
		if ((ref method) == MethodName.ScheduleLayoutVerification)
		{
			return true;
		}
		if ((ref method) == MethodName.OnProcessFrame)
		{
			return true;
		}
		if ((ref method) == MethodName.CorrectLayoutOverflow)
		{
			return true;
		}
		if ((ref method) == MethodName.FixedChromeMinHeight)
		{
			return true;
		}
		if ((ref method) == MethodName.BoundedMinHeight)
		{
			return true;
		}
		if ((ref method) == MethodName.LogLayoutCheck)
		{
			return true;
		}
		if ((ref method) == MethodName.ReadBoolEnv)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshFromRuntime)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshOverwriteWarning)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshListOnly)
		{
			return true;
		}
		if ((ref method) == MethodName.GetSelectedIndex)
		{
			return true;
		}
		if ((ref method) == MethodName.MoveSelected)
		{
			return true;
		}
		if ((ref method) == MethodName.MoveToTop)
		{
			return true;
		}
		if ((ref method) == MethodName.MoveToBottom)
		{
			return true;
		}
		if ((ref method) == MethodName.SortByAlphabet)
		{
			return true;
		}
		if ((ref method) == MethodName.IsLibraryMod)
		{
			return true;
		}
		if ((ref method) == MethodName.SmartSort)
		{
			return true;
		}
		if ((ref method) == MethodName.ToggleSelectedEnabled)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshPresetDropdown)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshPolicyCheckbox)
		{
			return true;
		}
		if ((ref method) == MethodName.OnNewModPolicyToggled)
		{
			return true;
		}
		if ((ref method) == MethodName.OnPresetSelected)
		{
			return true;
		}
		if ((ref method) == MethodName.CreatePresetFromCurrent)
		{
			return true;
		}
		if ((ref method) == MethodName.RenameCurrentPreset)
		{
			return true;
		}
		if ((ref method) == MethodName.DeleteCurrentPreset)
		{
			return true;
		}
		if ((ref method) == MethodName.ExportToClipboard)
		{
			return true;
		}
		if ((ref method) == MethodName.ImportFromClipboard)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyOrder)
		{
			return true;
		}
		if ((ref method) == MethodName.SetStatus)
		{
			return true;
		}
		if ((ref method) == MethodName.DebugLogLayoutCheck)
		{
			return true;
		}
		if ((ref method) == MethodName.DebugSelectPreset)
		{
			return true;
		}
		if ((ref method) == MethodName.DebugCreatePreset)
		{
			return true;
		}
		if ((ref method) == MethodName.DebugApplyOrder)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._uiBuilt)
		{
			_uiBuilt = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._list)
		{
			_list = VariantUtils.ConvertTo<ItemList>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._dialog)
		{
			_dialog = VariantUtils.ConvertTo<PanelContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._margin)
		{
			_margin = VariantUtils.ConvertTo<MarginContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._root)
		{
			_root = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._title)
		{
			_title = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._subtitle)
		{
			_subtitle = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._statusLabel)
		{
			_statusLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._warningLabel)
		{
			_warningLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._presetBar)
		{
			_presetBar = VariantUtils.ConvertTo<HFlowContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._footer)
		{
			_footer = VariantUtils.ConvertTo<HBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._sideScroll)
		{
			_sideScroll = VariantUtils.ConvertTo<ScrollContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._side)
		{
			_side = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._presetDropdown)
		{
			_presetDropdown = VariantUtils.ConvertTo<OptionButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._newModPolicyCheck)
		{
			_newModPolicyCheck = VariantUtils.ConvertTo<CheckBox>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._suppressPresetEvent)
		{
			_suppressPresetEvent = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._layoutViewport)
		{
			_layoutViewport = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._layoutCheckHooked)
		{
			_layoutCheckHooked = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._layoutCheckFramesPending)
		{
			_layoutCheckFramesPending = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._layoutStepsRemaining)
		{
			_layoutStepsRemaining = VariantUtils.ConvertTo<int>(ref value);
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
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._uiBuilt)
		{
			value = VariantUtils.CreateFrom<bool>(ref _uiBuilt);
			return true;
		}
		if ((ref name) == PropertyName._list)
		{
			value = VariantUtils.CreateFrom<ItemList>(ref _list);
			return true;
		}
		if ((ref name) == PropertyName._dialog)
		{
			value = VariantUtils.CreateFrom<PanelContainer>(ref _dialog);
			return true;
		}
		if ((ref name) == PropertyName._margin)
		{
			value = VariantUtils.CreateFrom<MarginContainer>(ref _margin);
			return true;
		}
		if ((ref name) == PropertyName._root)
		{
			value = VariantUtils.CreateFrom<VBoxContainer>(ref _root);
			return true;
		}
		if ((ref name) == PropertyName._title)
		{
			value = VariantUtils.CreateFrom<Label>(ref _title);
			return true;
		}
		if ((ref name) == PropertyName._subtitle)
		{
			value = VariantUtils.CreateFrom<Label>(ref _subtitle);
			return true;
		}
		if ((ref name) == PropertyName._statusLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _statusLabel);
			return true;
		}
		if ((ref name) == PropertyName._warningLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _warningLabel);
			return true;
		}
		if ((ref name) == PropertyName._presetBar)
		{
			value = VariantUtils.CreateFrom<HFlowContainer>(ref _presetBar);
			return true;
		}
		if ((ref name) == PropertyName._footer)
		{
			value = VariantUtils.CreateFrom<HBoxContainer>(ref _footer);
			return true;
		}
		if ((ref name) == PropertyName._sideScroll)
		{
			value = VariantUtils.CreateFrom<ScrollContainer>(ref _sideScroll);
			return true;
		}
		if ((ref name) == PropertyName._side)
		{
			value = VariantUtils.CreateFrom<VBoxContainer>(ref _side);
			return true;
		}
		if ((ref name) == PropertyName._presetDropdown)
		{
			value = VariantUtils.CreateFrom<OptionButton>(ref _presetDropdown);
			return true;
		}
		if ((ref name) == PropertyName._newModPolicyCheck)
		{
			value = VariantUtils.CreateFrom<CheckBox>(ref _newModPolicyCheck);
			return true;
		}
		if ((ref name) == PropertyName._suppressPresetEvent)
		{
			value = VariantUtils.CreateFrom<bool>(ref _suppressPresetEvent);
			return true;
		}
		if ((ref name) == PropertyName._layoutViewport)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _layoutViewport);
			return true;
		}
		if ((ref name) == PropertyName._layoutCheckHooked)
		{
			value = VariantUtils.CreateFrom<bool>(ref _layoutCheckHooked);
			return true;
		}
		if ((ref name) == PropertyName._layoutCheckFramesPending)
		{
			value = VariantUtils.CreateFrom<int>(ref _layoutCheckFramesPending);
			return true;
		}
		if ((ref name) == PropertyName._layoutStepsRemaining)
		{
			value = VariantUtils.CreateFrom<int>(ref _layoutStepsRemaining);
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
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)1, PropertyName._uiBuilt, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._list, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._dialog, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._margin, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._root, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._title, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._subtitle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._statusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._warningLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._presetBar, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._footer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._sideScroll, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._side, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._presetDropdown, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._newModPolicyCheck, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._suppressPresetEvent, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._layoutViewport, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._layoutCheckHooked, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._layoutCheckFramesPending, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._layoutStepsRemaining, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._uiBuilt, Variant.From<bool>(ref _uiBuilt));
		info.AddProperty(PropertyName._list, Variant.From<ItemList>(ref _list));
		info.AddProperty(PropertyName._dialog, Variant.From<PanelContainer>(ref _dialog));
		info.AddProperty(PropertyName._margin, Variant.From<MarginContainer>(ref _margin));
		info.AddProperty(PropertyName._root, Variant.From<VBoxContainer>(ref _root));
		info.AddProperty(PropertyName._title, Variant.From<Label>(ref _title));
		info.AddProperty(PropertyName._subtitle, Variant.From<Label>(ref _subtitle));
		info.AddProperty(PropertyName._statusLabel, Variant.From<Label>(ref _statusLabel));
		info.AddProperty(PropertyName._warningLabel, Variant.From<Label>(ref _warningLabel));
		info.AddProperty(PropertyName._presetBar, Variant.From<HFlowContainer>(ref _presetBar));
		info.AddProperty(PropertyName._footer, Variant.From<HBoxContainer>(ref _footer));
		info.AddProperty(PropertyName._sideScroll, Variant.From<ScrollContainer>(ref _sideScroll));
		info.AddProperty(PropertyName._side, Variant.From<VBoxContainer>(ref _side));
		info.AddProperty(PropertyName._presetDropdown, Variant.From<OptionButton>(ref _presetDropdown));
		info.AddProperty(PropertyName._newModPolicyCheck, Variant.From<CheckBox>(ref _newModPolicyCheck));
		info.AddProperty(PropertyName._suppressPresetEvent, Variant.From<bool>(ref _suppressPresetEvent));
		info.AddProperty(PropertyName._layoutViewport, Variant.From<Vector2>(ref _layoutViewport));
		info.AddProperty(PropertyName._layoutCheckHooked, Variant.From<bool>(ref _layoutCheckHooked));
		info.AddProperty(PropertyName._layoutCheckFramesPending, Variant.From<int>(ref _layoutCheckFramesPending));
		info.AddProperty(PropertyName._layoutStepsRemaining, Variant.From<int>(ref _layoutStepsRemaining));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._uiBuilt, ref val))
		{
			_uiBuilt = ((Variant)(ref val)).As<bool>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._list, ref val2))
		{
			_list = ((Variant)(ref val2)).As<ItemList>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._dialog, ref val3))
		{
			_dialog = ((Variant)(ref val3)).As<PanelContainer>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._margin, ref val4))
		{
			_margin = ((Variant)(ref val4)).As<MarginContainer>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._root, ref val5))
		{
			_root = ((Variant)(ref val5)).As<VBoxContainer>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._title, ref val6))
		{
			_title = ((Variant)(ref val6)).As<Label>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._subtitle, ref val7))
		{
			_subtitle = ((Variant)(ref val7)).As<Label>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._statusLabel, ref val8))
		{
			_statusLabel = ((Variant)(ref val8)).As<Label>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._warningLabel, ref val9))
		{
			_warningLabel = ((Variant)(ref val9)).As<Label>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._presetBar, ref val10))
		{
			_presetBar = ((Variant)(ref val10)).As<HFlowContainer>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._footer, ref val11))
		{
			_footer = ((Variant)(ref val11)).As<HBoxContainer>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._sideScroll, ref val12))
		{
			_sideScroll = ((Variant)(ref val12)).As<ScrollContainer>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._side, ref val13))
		{
			_side = ((Variant)(ref val13)).As<VBoxContainer>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._presetDropdown, ref val14))
		{
			_presetDropdown = ((Variant)(ref val14)).As<OptionButton>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._newModPolicyCheck, ref val15))
		{
			_newModPolicyCheck = ((Variant)(ref val15)).As<CheckBox>();
		}
		Variant val16 = default(Variant);
		if (info.TryGetProperty(PropertyName._suppressPresetEvent, ref val16))
		{
			_suppressPresetEvent = ((Variant)(ref val16)).As<bool>();
		}
		Variant val17 = default(Variant);
		if (info.TryGetProperty(PropertyName._layoutViewport, ref val17))
		{
			_layoutViewport = ((Variant)(ref val17)).As<Vector2>();
		}
		Variant val18 = default(Variant);
		if (info.TryGetProperty(PropertyName._layoutCheckHooked, ref val18))
		{
			_layoutCheckHooked = ((Variant)(ref val18)).As<bool>();
		}
		Variant val19 = default(Variant);
		if (info.TryGetProperty(PropertyName._layoutCheckFramesPending, ref val19))
		{
			_layoutCheckFramesPending = ((Variant)(ref val19)).As<int>();
		}
		Variant val20 = default(Variant);
		if (info.TryGetProperty(PropertyName._layoutStepsRemaining, ref val20))
		{
			_layoutStepsRemaining = ((Variant)(ref val20)).As<int>();
		}
	}
}
