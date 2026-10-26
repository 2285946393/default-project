using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.addons.mega_text;
using ModPresetManager.Scripts.Localization;
using ModPresetManager.Scripts.Presets;

namespace ModPresetManager.Scripts.Ui;

[ScriptPath("res://Scripts/Ui/ModPresetModal.cs")]
public class ModPresetModal : Control, IScreenContext
{
	private enum ExportMode
	{
		Essential,
		All
	}

	private enum ImportMode
	{
		Compatible,
		Overwrite
	}

	private readonly record struct PreviewRowData(string Order, string Status, Color StatusColor, string StatusTooltip, string ModName, bool AffectsMultiplayer, string CurrentVersion, string TargetVersion, string Source, string Action, Color ActionColor, string ActionTooltip)
	{
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			builder.Append("Order = ");
			builder.Append((object?)Order);
			builder.Append(", Status = ");
			builder.Append((object?)Status);
			builder.Append(", StatusColor = ");
			Color val = StatusColor;
			builder.Append(((object)(Color)(ref val)).ToString());
			builder.Append(", StatusTooltip = ");
			builder.Append((object?)StatusTooltip);
			builder.Append(", ModName = ");
			builder.Append((object?)ModName);
			builder.Append(", AffectsMultiplayer = ");
			builder.Append(AffectsMultiplayer.ToString());
			builder.Append(", CurrentVersion = ");
			builder.Append((object?)CurrentVersion);
			builder.Append(", TargetVersion = ");
			builder.Append((object?)TargetVersion);
			builder.Append(", Source = ");
			builder.Append((object?)Source);
			builder.Append(", Action = ");
			builder.Append((object?)Action);
			builder.Append(", ActionColor = ");
			val = ActionColor;
			builder.Append(((object)(Color)(ref val)).ToString());
			builder.Append(", ActionTooltip = ");
			builder.Append((object?)ActionTooltip);
			return true;
		}
	}

	private readonly record struct TransitionPresentation(string Text, Color Color, string Tooltip)
	{
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			builder.Append("Text = ");
			builder.Append((object?)Text);
			builder.Append(", Color = ");
			Color color = Color;
			builder.Append(((object)(Color)(ref color)).ToString());
			builder.Append(", Tooltip = ");
			builder.Append((object?)Tooltip);
			return true;
		}
	}

	public class MethodName : MethodName
	{
		public static readonly StringName Show = StringName.op_Implicit("Show");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName CreateWindow = StringName.op_Implicit("CreateWindow");

		public static readonly StringName CreateTooltipTheme = StringName.op_Implicit("CreateTooltipTheme");

		public static readonly StringName CreateTopActions = StringName.op_Implicit("CreateTopActions");

		public static readonly StringName GetExportButtonText = StringName.op_Implicit("GetExportButtonText");

		public static readonly StringName GetImportButtonText = StringName.op_Implicit("GetImportButtonText");

		public static readonly StringName GetExportOptionDescription = StringName.op_Implicit("GetExportOptionDescription");

		public static readonly StringName GetImportOptionDescription = StringName.op_Implicit("GetImportOptionDescription");

		public static readonly StringName UpdateActionTooltips = StringName.op_Implicit("UpdateActionTooltips");

		public static readonly StringName SelectExportMode = StringName.op_Implicit("SelectExportMode");

		public static readonly StringName ExportSelectedPresetToClipboard = StringName.op_Implicit("ExportSelectedPresetToClipboard");

		public static readonly StringName SelectImportMode = StringName.op_Implicit("SelectImportMode");

		public static readonly StringName SetImportActionAsApply = StringName.op_Implicit("SetImportActionAsApply");

		public static readonly StringName SetImportActionAsImport = StringName.op_Implicit("SetImportActionAsImport");

		public static readonly StringName CreateSummary = StringName.op_Implicit("CreateSummary");

		public static readonly StringName CreatePreview = StringName.op_Implicit("CreatePreview");

		public static readonly StringName CreateHeaderRow = StringName.op_Implicit("CreateHeaderRow");

		public static readonly StringName CreateFooter = StringName.op_Implicit("CreateFooter");

		public static readonly StringName ExportToClipboard = StringName.op_Implicit("ExportToClipboard");

		public static readonly StringName ImportOrApplyClipboardPreset = StringName.op_Implicit("ImportOrApplyClipboardPreset");

		public static readonly StringName CancelPendingImport = StringName.op_Implicit("CancelPendingImport");

		public static readonly StringName AnalyzeAndRenderPendingPreset = StringName.op_Implicit("AnalyzeAndRenderPendingPreset");

		public static readonly StringName AttachActiveDownload = StringName.op_Implicit("AttachActiveDownload");

		public static readonly StringName UpdateDownloadProgressDisplay = StringName.op_Implicit("UpdateDownloadProgressDisplay");

		public static readonly StringName HideDownloadProgress = StringName.op_Implicit("HideDownloadProgress");

		public static readonly StringName FormatBytes = StringName.op_Implicit("FormatBytes");

		public static readonly StringName ReanalyzeAfterDownloads = StringName.op_Implicit("ReanalyzeAfterDownloads");

		public static readonly StringName ApplyPendingPreset = StringName.op_Implicit("ApplyPendingPreset");

		public static readonly StringName RenderCurrentPreview = StringName.op_Implicit("RenderCurrentPreview");

		public static readonly StringName SyncHeaderWithScrollbar = StringName.op_Implicit("SyncHeaderWithScrollbar");

		public static readonly StringName RefreshModdingScreenTickboxes = StringName.op_Implicit("RefreshModdingScreenTickboxes");

		public static readonly StringName RestartGame = StringName.op_Implicit("RestartGame");

		public static readonly StringName SetRestartButtonVisible = StringName.op_Implicit("SetRestartButtonVisible");

		public static readonly StringName GetParseErrorKey = StringName.op_Implicit("GetParseErrorKey");

		public static readonly StringName GetSourceText = StringName.op_Implicit("GetSourceText");

		public static readonly StringName EmptyAsDash = StringName.op_Implicit("EmptyAsDash");

		public static readonly StringName CreateTableRow = StringName.op_Implicit("CreateTableRow");

		public static readonly StringName AddCell = StringName.op_Implicit("AddCell");

		public static readonly StringName CreateLabel = StringName.op_Implicit("CreateLabel");

		public static readonly StringName CreateDivider = StringName.op_Implicit("CreateDivider");

		public static readonly StringName CreateDownloadProgressBar = StringName.op_Implicit("CreateDownloadProgressBar");

		public static readonly StringName CreateFlatPanel = StringName.op_Implicit("CreateFlatPanel");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName DefaultFocusedControl = StringName.op_Implicit("DefaultFocusedControl");

		public static readonly StringName _moddingScreen = StringName.op_Implicit("_moddingScreen");

		public static readonly StringName _exportAction = StringName.op_Implicit("_exportAction");

		public static readonly StringName _importAction = StringName.op_Implicit("_importAction");

		public static readonly StringName _restartButton = StringName.op_Implicit("_restartButton");

		public static readonly StringName _statusLabel = StringName.op_Implicit("_statusLabel");

		public static readonly StringName _downloadProgressLabel = StringName.op_Implicit("_downloadProgressLabel");

		public static readonly StringName _downloadProgressBar = StringName.op_Implicit("_downloadProgressBar");

		public static readonly StringName _summaryLabel = StringName.op_Implicit("_summaryLabel");

		public static readonly StringName _previewTitleLabel = StringName.op_Implicit("_previewTitleLabel");

		public static readonly StringName _headerMargin = StringName.op_Implicit("_headerMargin");

		public static readonly StringName _previewScroll = StringName.op_Implicit("_previewScroll");

		public static readonly StringName _previewRows = StringName.op_Implicit("_previewRows");

		public static readonly StringName _downloadPollElapsed = StringName.op_Implicit("_downloadPollElapsed");

		public static readonly StringName _exportMode = StringName.op_Implicit("_exportMode");

		public static readonly StringName _importMode = StringName.op_Implicit("_importMode");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Color Cream = new Color("fff6e2");

	private static readonly Color Muted = new Color("aeb9c8");

	private static readonly Color Gold = new Color("f0b400");

	private static readonly Color Green = new Color("78c96d");

	private static readonly Color Yellow = new Color("efc851");

	private static readonly Color Blue = new Color("76b9ed");

	private static readonly Color Red = new Color("e67676");

	private NModdingScreen? _moddingScreen;

	private PresetSplitActionButton? _exportAction;

	private PresetSplitActionButton? _importAction;

	private PresetActionButton? _restartButton;

	private MegaLabel? _statusLabel;

	private MegaLabel? _downloadProgressLabel;

	private ProgressBar? _downloadProgressBar;

	private MegaLabel? _summaryLabel;

	private MegaLabel? _previewTitleLabel;

	private MarginContainer? _headerMargin;

	private ScrollContainer? _previewScroll;

	private VBoxContainer? _previewRows;

	private ModPresetDocument? _pendingPreset;

	private PresetImportAnalysis? _pendingAnalysis;

	private WorkshopSubscriptionBatch? _downloadBatch;

	private double _downloadPollElapsed;

	private ExportMode _exportMode;

	private ImportMode _importMode;

	public Control? DefaultFocusedControl => null;

	public ModPresetModal()
	{
		((Node)this).Name = StringName.op_Implicit("ModPresetModal");
		((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)this).GrowHorizontal = (GrowDirection)2;
		((Control)this).GrowVertical = (GrowDirection)2;
		((Control)this).MouseFilter = (MouseFilterEnum)0;
		((Node)this).SetProcess(false);
	}

	public static void Show(NModdingScreen moddingScreen)
	{
		NModalContainer instance = NModalContainer.Instance;
		if (instance != null && instance.OpenModal == null)
		{
			instance.Add((Node)(object)new ModPresetModal
			{
				_moddingScreen = moddingScreen
			}, true);
		}
	}

	public override void _Ready()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).Theme = CreateTooltipTheme();
		Panel val = CreateWindow();
		((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
		MarginContainer val2 = new MarginContainer();
		((Control)val2).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 34);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), 26);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 34);
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 26);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		VBoxContainer val3 = new VBoxContainer
		{
			SizeFlagsHorizontal = (SizeFlags)3,
			SizeFlagsVertical = (SizeFlags)3
		};
		((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 14);
		((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)CreateLabel(ModLocalization.Text("preset.title"), 44, Cream, (HorizontalAlignment)1), false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)CreateLabel(ModLocalization.Text("preset.subtitle"), 21, Muted, (HorizontalAlignment)1), false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)CreateDivider(Gold, 0.55f), false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)CreateTopActions(), false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)CreateSummary(), false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)CreatePreview(), false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)CreateFooter(), false, (InternalMode)0);
		if (!AttachActiveDownload())
		{
			RenderCurrentPreview();
		}
	}

	public override void _Process(double delta)
	{
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if (_downloadBatch == null)
		{
			return;
		}
		_downloadPollElapsed += delta;
		if (_downloadPollElapsed < 0.1)
		{
			return;
		}
		_downloadPollElapsed = 0.0;
		_downloadBatch.Poll();
		UpdateDownloadProgressDisplay();
		if (!_downloadBatch.IsComplete)
		{
			int num = _downloadBatch.Jobs.Count((WorkshopDownloadJob job) => job.IsTerminal);
			SetStatus("preset.import.download_progress", Blue, ("done", num.ToString()), ("total", _downloadBatch.Jobs.Count.ToString()));
			return;
		}
		int installed = _downloadBatch.InstalledCount;
		int failed = _downloadBatch.FailedCount;
		_downloadBatch.Dispose();
		_downloadBatch = null;
		HideDownloadProgress();
		((Node)this).SetProcess(false);
		Callable val = Callable.From((Action)delegate
		{
			ReanalyzeAfterDownloads(installed, failed);
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed(MegaInput.cancel, false, false) || inputEvent.IsActionPressed(MegaInput.pauseAndBack, false, false) || inputEvent.IsActionPressed(MegaInput.back, false, false))
		{
			NModalContainer instance = NModalContainer.Instance;
			if (instance != null)
			{
				instance.Clear();
			}
			((Node)this).GetViewport().SetInputAsHandled();
		}
	}

	public override void _ExitTree()
	{
		_downloadBatch = null;
	}

	private static Panel CreateWindow()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected O, but got Unknown
		StyleBoxFlat val = new StyleBoxFlat
		{
			BgColor = new Color(0.035f, 0.065f, 0.105f, 0.98f),
			BorderColor = new Color("527ca5"),
			BorderWidthLeft = 3,
			BorderWidthTop = 3,
			BorderWidthRight = 3,
			BorderWidthBottom = 3,
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12,
			CornerRadiusBottomRight = 12,
			ShadowColor = new Color(0f, 0f, 0f, 0.65f),
			ShadowSize = 18
		};
		Panel val2 = new Panel
		{
			MouseFilter = (MouseFilterEnum)0
		};
		((Control)val2).SetAnchorsPreset((LayoutPreset)8, false);
		((Control)val2).OffsetLeft = -720f;
		((Control)val2).OffsetTop = -450f;
		((Control)val2).OffsetRight = 720f;
		((Control)val2).OffsetBottom = 450f;
		((Control)val2).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)val);
		return val2;
	}

	private static Theme CreateTooltipTheme()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Expected O, but got Unknown
		Theme val = new Theme();
		StyleBoxFlat val2 = new StyleBoxFlat
		{
			BgColor = new Color(0.025f, 0.045f, 0.075f, 1f),
			BorderColor = new Color("527ca5"),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 14f,
			ContentMarginTop = 9f,
			ContentMarginRight = 14f,
			ContentMarginBottom = 9f
		};
		val.SetStylebox(StringName.op_Implicit("panel"), StringName.op_Implicit("TooltipPanel"), (StyleBox)(object)val2);
		val.SetFont(StringName.op_Implicit("font"), StringName.op_Implicit("TooltipLabel"), ResourceLoader.Load<Font>("res://themes/kreon_regular_shared.tres", (string)null, (CacheMode)1));
		val.SetFontSize(StringName.op_Implicit("font_size"), StringName.op_Implicit("TooltipLabel"), 22);
		val.SetColor(StringName.op_Implicit("font_color"), StringName.op_Implicit("TooltipLabel"), Cream);
		return val;
	}

	private HBoxContainer CreateTopActions()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		HBoxContainer val = new HBoxContainer();
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 12);
		_exportAction = new PresetSplitActionButton(GetExportButtonText(), new _003C_003Ez__ReadOnlyArray<PresetSplitActionOption>(new PresetSplitActionOption[2]
		{
			new PresetSplitActionOption(ModLocalization.Text("preset.export.option.default_essential"), ModLocalization.Text("preset.export.option.default_essential.tooltip")),
			new PresetSplitActionOption(ModLocalization.Text("preset.export.option.all"), ModLocalization.Text("preset.export.option.all.tooltip"))
		}), ExportSelectedPresetToClipboard, SelectExportMode);
		((Node)val).AddChild((Node)(object)_exportAction, false, (InternalMode)0);
		_importAction = new PresetSplitActionButton(GetImportButtonText(), new _003C_003Ez__ReadOnlyArray<PresetSplitActionOption>(new PresetSplitActionOption[2]
		{
			new PresetSplitActionOption(ModLocalization.Text("preset.import.option.default_compatible"), ModLocalization.Text("preset.import.option.default_compatible.tooltip")),
			new PresetSplitActionOption(ModLocalization.Text("preset.import.option.overwrite"), ModLocalization.Text("preset.import.option.overwrite.tooltip"))
		}), ImportOrApplyClipboardPreset, SelectImportMode);
		((Node)val).AddChild((Node)(object)_importAction, false, (InternalMode)0);
		UpdateActionTooltips();
		return val;
	}

	private string GetExportButtonText()
	{
		return ModLocalization.Text((_exportMode == ExportMode.Essential) ? "preset.button.export_essential" : "preset.button.export_all");
	}

	private string GetImportButtonText()
	{
		return ModLocalization.Text((_importMode == ImportMode.Compatible) ? "preset.button.import_compatible" : "preset.button.import_overwrite");
	}

	private string GetExportOptionDescription()
	{
		return ModLocalization.Text((_exportMode == ExportMode.Essential) ? "preset.export.option.default_essential.tooltip" : "preset.export.option.all.tooltip");
	}

	private string GetImportOptionDescription()
	{
		return ModLocalization.Text((_importMode == ImportMode.Compatible) ? "preset.import.option.default_compatible.tooltip" : "preset.import.option.overwrite.tooltip");
	}

	private void UpdateActionTooltips()
	{
		_exportAction?.SetTooltips(ModLocalization.Text("preset.tooltip.export.action", ("description", GetExportOptionDescription())), ModLocalization.Text("preset.tooltip.export.options", ("description", GetExportOptionDescription())));
		_importAction?.SetTooltips(ModLocalization.Text("preset.tooltip.import.action", ("description", GetImportOptionDescription())), ModLocalization.Text("preset.tooltip.import.options", ("description", GetImportOptionDescription())));
	}

	private void SelectExportMode(int option)
	{
		_exportMode = ((option == 1) ? ExportMode.All : ExportMode.Essential);
		_exportAction?.SetText(GetExportButtonText());
		UpdateActionTooltips();
	}

	private void ExportSelectedPresetToClipboard()
	{
		ModPresetScope scope = ((_exportMode == ExportMode.Essential) ? ModPresetScope.Multiplayer : ModPresetScope.All);
		ExportToClipboard(scope);
	}

	private void SelectImportMode(int option)
	{
		_importMode = ((option == 1) ? ImportMode.Overwrite : ImportMode.Compatible);
		_importAction?.SetText(GetImportButtonText());
		UpdateActionTooltips();
	}

	private void SetImportActionAsApply(string localizationKey)
	{
		_exportAction?.SetOptionsVisible(visible: false);
		_exportAction?.SetText(ModLocalization.Text("preset.button.cancel"));
		_exportAction?.SetDanger(danger: true);
		_exportAction?.SetAction(CancelPendingImport);
		_exportAction?.SetTooltips(ModLocalization.Text("preset.tooltip.cancel.action"), ModLocalization.Text("preset.tooltip.export.options", ("description", GetExportOptionDescription())));
		_importAction?.SetOptionsVisible(visible: false);
		_importAction?.SetText(ModLocalization.Text(localizationKey));
		_importAction?.SetAttention(attention: true);
		_importAction?.SetTooltips(ModLocalization.Text("preset.tooltip.import.apply", ("description", GetImportOptionDescription())), ModLocalization.Text("preset.tooltip.import.options", ("description", GetImportOptionDescription())));
	}

	private void SetImportActionAsImport()
	{
		_exportAction?.SetOptionsVisible(visible: true);
		_exportAction?.SetText(GetExportButtonText());
		_exportAction?.SetDanger(danger: false);
		_exportAction?.SetAction(ExportSelectedPresetToClipboard);
		_importAction?.SetOptionsVisible(visible: true);
		_importAction?.SetText(GetImportButtonText());
		_importAction?.SetAttention(attention: false);
		UpdateActionTooltips();
	}

	private Control CreateSummary()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		Panel obj = CreateFlatPanel(new Color(0.08f, 0.13f, 0.19f, 0.9f), new Color("34506c"), 1);
		((Control)obj).CustomMinimumSize = new Vector2(0f, 52f);
		_summaryLabel = CreateLabel("", 22, Cream, (HorizontalAlignment)0);
		((Control)_summaryLabel).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)_summaryLabel).OffsetLeft = 18f;
		((Control)_summaryLabel).OffsetRight = -18f;
		((Node)obj).AddChild((Node)(object)_summaryLabel, false, (InternalMode)0);
		return (Control)(object)obj;
	}

	private Control CreatePreview()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Expected O, but got Unknown
		Panel obj = CreateFlatPanel(new Color(0.025f, 0.045f, 0.075f, 0.95f), new Color("466889"), 2);
		((Control)obj).SizeFlagsVertical = (SizeFlags)3;
		((Control)obj).CustomMinimumSize = new Vector2(0f, 470f);
		VBoxContainer val = new VBoxContainer();
		((Control)val).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)val).OffsetLeft = 14f;
		((Control)val).OffsetTop = 10f;
		((Control)val).OffsetRight = -14f;
		((Control)val).OffsetBottom = -10f;
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
		((Node)obj).AddChild((Node)(object)val, false, (InternalMode)0);
		_previewTitleLabel = CreateLabel("", 26, Cream, (HorizontalAlignment)0);
		((Node)val).AddChild((Node)(object)_previewTitleLabel, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)CreateHeaderRow(), false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)CreateDivider(new Color("8ba7c4"), 0.35f), false, (InternalMode)0);
		_previewScroll = new ScrollContainer
		{
			SizeFlagsVertical = (SizeFlags)3,
			HorizontalScrollMode = (ScrollMode)0
		};
		((Node)val).AddChild((Node)(object)_previewScroll, false, (InternalMode)0);
		_previewRows = new VBoxContainer
		{
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)_previewRows).AddThemeConstantOverride(StringName.op_Implicit("separation"), 5);
		((Node)_previewScroll).AddChild((Node)(object)_previewRows, false, (InternalMode)0);
		return (Control)(object)obj;
	}

	private Control CreateHeaderRow()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		_headerMargin = new MarginContainer
		{
			CustomMinimumSize = new Vector2(0f, 38f)
		};
		((Control)_headerMargin).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 8);
		((Control)_headerMargin).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 8);
		HBoxContainer val = CreateTableRow();
		AddCell(val, ModLocalization.Text("preset.preview.column.order"), 92f, Muted, expand: false, ModLocalization.Text("preset.preview.tooltip.column.order"));
		AddCell(val, ModLocalization.Text("preset.preview.column.status"), 160f, Muted, expand: false, ModLocalization.Text("preset.preview.tooltip.column.status"));
		AddCell(val, ModLocalization.Text("preset.preview.column.mod"), 0f, Muted, expand: true);
		AddCell(val, ModLocalization.Text("preset.preview.column.affects_multiplayer"), 130f, Muted, expand: false, ModLocalization.Text("preset.preview.tooltip.column.multiplayer"));
		AddCell(val, ModLocalization.Text("preset.preview.column.current_version"), 120f, Muted);
		AddCell(val, ModLocalization.Text("preset.preview.column.target_version"), 120f, Muted);
		AddCell(val, ModLocalization.Text("preset.preview.column.source"), 140f, Muted);
		AddCell(val, ModLocalization.Text("preset.preview.column.action"), 170f, Muted, expand: false, ModLocalization.Text("preset.preview.tooltip.column.transition"));
		((Node)_headerMargin).AddChild((Node)(object)val, false, (InternalMode)0);
		return (Control)(object)_headerMargin;
	}

	private static Control CreatePreviewRow(PreviewRowData data, bool alternate)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		Panel obj = CreateFlatPanel(alternate ? new Color(0.07f, 0.105f, 0.145f, 0.72f) : new Color(0.045f, 0.075f, 0.11f, 0.72f), Colors.Transparent, 0);
		((Control)obj).CustomMinimumSize = new Vector2(0f, 52f);
		HBoxContainer val = CreateTableRow();
		((Control)val).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)val).OffsetLeft = 8f;
		((Control)val).OffsetRight = -8f;
		((Node)obj).AddChild((Node)(object)val, false, (InternalMode)0);
		AddCell(val, data.Order, 92f, Muted);
		AddCell(val, data.Status, 160f, data.StatusColor, expand: false, data.StatusTooltip);
		AddCell(val, data.ModName, 0f, Cream, expand: true, data.ModName);
		AddCell(val, ModLocalization.Text(data.AffectsMultiplayer ? "preset.preview.multiplayer.yes" : "preset.preview.multiplayer.no"), 130f, data.AffectsMultiplayer ? Yellow : Muted, expand: false, ModLocalization.Text(data.AffectsMultiplayer ? "preset.preview.tooltip.multiplayer.yes" : "preset.preview.tooltip.multiplayer.no"));
		AddCell(val, data.CurrentVersion, 120f, Muted);
		AddCell(val, data.TargetVersion, 120f, Muted);
		AddCell(val, data.Source, 140f, Muted);
		AddCell(val, data.Action, 170f, data.ActionColor, expand: false, data.ActionTooltip);
		return (Control)(object)obj;
	}

	private Control CreateFooter()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Expected O, but got Unknown
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 76f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		VBoxContainer val2 = new VBoxContainer
		{
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("separation"), 2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		_statusLabel = CreateLabel(ModLocalization.Text("preset.ready"), 19, Muted, (HorizontalAlignment)0);
		((Control)_statusLabel).SizeFlagsHorizontal = (SizeFlags)3;
		((Node)val2).AddChild((Node)(object)_statusLabel, false, (InternalMode)0);
		_downloadProgressLabel = CreateLabel("", 16, Blue, (HorizontalAlignment)0);
		((CanvasItem)_downloadProgressLabel).Visible = false;
		((Node)val2).AddChild((Node)(object)_downloadProgressLabel, false, (InternalMode)0);
		_downloadProgressBar = CreateDownloadProgressBar();
		((CanvasItem)_downloadProgressBar).Visible = false;
		((Node)val2).AddChild((Node)(object)_downloadProgressBar, false, (InternalMode)0);
		PresetActionButton presetActionButton = new PresetActionButton(ModLocalization.Text("preset.button.restart"), primary: true);
		((Control)presetActionButton).CustomMinimumSize = new Vector2(150f, 58f);
		((Control)presetActionButton).SizeFlagsHorizontal = (SizeFlags)8;
		((CanvasItem)presetActionButton).Visible = false;
		_restartButton = presetActionButton;
		((GodotObject)_restartButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
		{
			RestartGame();
		}), 0u);
		((Node)val).AddChild((Node)(object)_restartButton, false, (InternalMode)0);
		PresetActionButton presetActionButton2 = new PresetActionButton(ModLocalization.Text("preset.button.close"));
		((Control)presetActionButton2).CustomMinimumSize = new Vector2(150f, 58f);
		((Control)presetActionButton2).SizeFlagsHorizontal = (SizeFlags)8;
		PresetActionButton presetActionButton3 = presetActionButton2;
		((GodotObject)presetActionButton3).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
		{
			NModalContainer instance = NModalContainer.Instance;
			if (instance != null)
			{
				instance.Clear();
			}
		}), 0u);
		((Node)val).AddChild((Node)(object)presetActionButton3, false, (InternalMode)0);
		return (Control)val;
	}

	private void ExportToClipboard(ModPresetScope scope)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		PresetExportResult presetExportResult = ModPresetService.Export(scope);
		(string, string) tuple;
		if (!presetExportResult.Success)
		{
			if (presetExportResult == null)
			{
				goto IL_004c;
			}
			string unavailableModId = presetExportResult.UnavailableModId;
			if (unavailableModId == null)
			{
				string duplicateModId = presetExportResult.DuplicateModId;
				if (duplicateModId == null)
				{
					goto IL_004c;
				}
				tuple = ("preset.export_duplicate_mod", duplicateModId);
			}
			else
			{
				tuple = ("preset.export_unavailable_mod", unavailableModId);
			}
			goto IL_0067;
		}
		DisplayServer.ClipboardSet(presetExportResult.Text);
		SetStatus("preset.export_copied", Green, ("count", presetExportResult.ModCount.ToString()));
		return;
		IL_004c:
		tuple = ("preset.export_missing_version", presetExportResult.InvalidModId ?? "?");
		goto IL_0067;
		IL_0067:
		var (key, item) = tuple;
		SetStatus(key, Red, ("id", item));
	}

	private void ImportOrApplyClipboardPreset()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (_downloadBatch == null)
		{
			if (_pendingPreset != null)
			{
				ApplyPendingPreset();
				return;
			}
			if (!ModPresetService.TryParse(DisplayServer.ClipboardGet(), out ModPresetDocument document, out PresetParseError error))
			{
				SetStatus(GetParseErrorKey(error), Red);
				return;
			}
			_pendingPreset = document;
			AnalyzeAndRenderPendingPreset();
		}
	}

	private void CancelPendingImport()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (_downloadBatch != null)
		{
			_downloadBatch.Dispose();
			_downloadBatch = null;
			_downloadPollElapsed = 0.0;
			HideDownloadProgress();
			((Node)this).SetProcess(false);
		}
		_pendingPreset = null;
		_pendingAnalysis = null;
		SetImportActionAsImport();
		RenderCurrentPreview();
		SetStatus("preset.import.cancelled", Muted);
	}

	private void AnalyzeAndRenderPendingPreset()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (_pendingPreset != null)
		{
			PresetImportAnalysis presetImportAnalysis = (_pendingAnalysis = ModPresetService.Analyze(_pendingPreset));
			RenderImportPreview(presetImportAnalysis);
			SetImportActionAsApply("preset.import.apply_button");
			if (presetImportAnalysis.CanApply)
			{
				SetStatus("preset.import.ready", Green);
			}
			else if (presetImportAnalysis.DownloadCount > 0)
			{
				SetStatus("preset.import.preview_downloads", Yellow, ("count", presetImportAnalysis.DownloadCount.ToString()));
			}
			else
			{
				SetStatus("preset.import.preview_blocked", Red, ("count", presetImportAnalysis.ProblemCount.ToString()));
			}
		}
	}

	private void StartWorkshopDownloads(PresetImportAnalysis analysis)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (_pendingPreset == null)
		{
			return;
		}
		if (WorkshopSubscriptionBatch.Active != null)
		{
			AttachActiveDownload();
			return;
		}
		WorkshopSubscriptionBatch workshopSubscriptionBatch = new WorkshopSubscriptionBatch(_pendingPreset);
		if (!workshopSubscriptionBatch.IsSteamAvailable)
		{
			workshopSubscriptionBatch.Dispose();
			SetStatus("preset.import.steam_unavailable", Red);
			return;
		}
		workshopSubscriptionBatch.Start(analysis.Items);
		if (workshopSubscriptionBatch.Jobs.Count == 0)
		{
			workshopSubscriptionBatch.Dispose();
			SetStatus("preset.import.blocked", Red, ("count", analysis.ProblemCount.ToString()));
			return;
		}
		_downloadBatch = workshopSubscriptionBatch;
		_downloadPollElapsed = 0.0;
		SetImportActionAsApply("preset.import.applying_button");
		((Node)this).SetProcess(true);
		UpdateDownloadProgressDisplay();
		SetRestartButtonVisible(shouldRestart: false);
		SetStatus("preset.import.download_started", Blue, ("count", workshopSubscriptionBatch.Jobs.Count.ToString()));
	}

	private bool AttachActiveDownload()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		WorkshopSubscriptionBatch active = WorkshopSubscriptionBatch.Active;
		if (active == null)
		{
			return false;
		}
		_downloadBatch = active;
		_pendingPreset = active.Preset;
		active.Poll();
		_pendingAnalysis = ModPresetService.Analyze(active.Preset);
		SetImportActionAsApply("preset.import.applying_button");
		RenderImportPreview(_pendingAnalysis);
		_downloadPollElapsed = 0.0;
		((Node)this).SetProcess(true);
		UpdateDownloadProgressDisplay();
		SetStatus("preset.import.download_progress", Blue, ("done", active.Jobs.Count((WorkshopDownloadJob job) => job.IsTerminal).ToString()), ("total", active.Jobs.Count.ToString()));
		return true;
	}

	private void UpdateDownloadProgressDisplay()
	{
		if (_downloadBatch != null && _downloadProgressLabel != null && _downloadProgressBar != null)
		{
			List<WorkshopDownloadJob> list = _downloadBatch.Jobs.ToList();
			WorkshopDownloadJob workshopDownloadJob = list.FirstOrDefault((WorkshopDownloadJob candidate) => !candidate.IsTerminal) ?? list.LastOrDefault();
			if (workshopDownloadJob == null)
			{
				HideDownloadProgress();
				return;
			}
			bool flag = workshopDownloadJob.State == WorkshopDownloadState.WaitingForInstall;
			bool flag2 = workshopDownloadJob.BytesTotal != 0;
			double num = (flag ? 100.0 : (flag2 ? Math.Clamp((double)workshopDownloadJob.BytesDownloaded * 100.0 / (double)workshopDownloadJob.BytesTotal, 0.0, 100.0) : 2.0));
			((Range)_downloadProgressBar).Value = num;
			((CanvasItem)_downloadProgressBar).Visible = true;
			((CanvasItem)_downloadProgressLabel).Visible = true;
			_downloadProgressLabel.SetTextAutoSize(flag2 ? ModLocalization.Text("preset.import.progress.item", ("id", workshopDownloadJob.ModId), ("index", (list.IndexOf(workshopDownloadJob) + 1).ToString()), ("count", list.Count.ToString()), ("downloaded", FormatBytes(workshopDownloadJob.BytesDownloaded)), ("total", FormatBytes(workshopDownloadJob.BytesTotal)), ("percent", Math.Floor(num).ToString("0"))) : ModLocalization.Text(flag ? "preset.import.progress.installing" : "preset.import.progress.waiting_size", ("id", workshopDownloadJob.ModId), ("index", (list.IndexOf(workshopDownloadJob) + 1).ToString()), ("count", list.Count.ToString())));
		}
	}

	private void HideDownloadProgress()
	{
		if (_downloadProgressLabel != null)
		{
			((CanvasItem)_downloadProgressLabel).Visible = false;
		}
		if (_downloadProgressBar != null)
		{
			((CanvasItem)_downloadProgressBar).Visible = false;
		}
	}

	private static string FormatBytes(ulong bytes)
	{
		if (!((double)bytes >= 1073741824.0))
		{
			if (!((double)bytes >= 1048576.0))
			{
				if (!((double)bytes >= 1024.0))
				{
					return $"{bytes} B";
				}
				return $"{(double)bytes / 1024.0:0.0} KiB";
			}
			return $"{(double)bytes / 1048576.0:0.0} MiB";
		}
		return $"{(double)bytes / 1073741824.0:0.00} GiB";
	}

	private void ReanalyzeAfterDownloads(int installed, int failed)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (_pendingPreset != null)
		{
			PresetImportAnalysis presetImportAnalysis = (_pendingAnalysis = ModPresetService.Analyze(_pendingPreset));
			RenderImportPreview(presetImportAnalysis);
			if (presetImportAnalysis.CanApply)
			{
				ApplyResolvedPreset(presetImportAnalysis);
				return;
			}
			SetImportActionAsApply("preset.import.apply_button");
			SetStatus("preset.import.download_finished_blocked", Red, ("installed", installed.ToString()), ("failed", failed.ToString()), ("problems", presetImportAnalysis.ProblemCount.ToString()));
		}
	}

	private void ApplyPendingPreset()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (_pendingPreset == null)
		{
			return;
		}
		PresetImportAnalysis presetImportAnalysis = ModPresetService.Analyze(_pendingPreset);
		if (!presetImportAnalysis.CanApply)
		{
			_pendingAnalysis = presetImportAnalysis;
			RenderImportPreview(presetImportAnalysis);
			if (presetImportAnalysis.DownloadCount > 0)
			{
				StartWorkshopDownloads(presetImportAnalysis);
				return;
			}
			SetImportActionAsApply("preset.import.apply_button");
			SetStatus("preset.import.blocked", Red, ("count", presetImportAnalysis.ProblemCount.ToString()));
		}
		else
		{
			ApplyResolvedPreset(presetImportAnalysis);
		}
	}

	private void ApplyResolvedPreset(PresetImportAnalysis analysis)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		PresetApplyResult presetApplyResult = ModPresetService.Apply(analysis, (_importMode == ImportMode.Compatible) ? PresetImportMode.Compatible : PresetImportMode.Overwrite);
		if (!presetApplyResult.Success)
		{
			SetStatus("preset.import.apply_failed", Red);
			return;
		}
		RefreshModdingScreenTickboxes();
		_pendingPreset = null;
		_pendingAnalysis = null;
		SetImportActionAsImport();
		RenderCurrentPreview();
		SetStatus("preset.import.applied", Green, ("count", presetApplyResult.ChangedCount.ToString()));
	}

	private void RenderCurrentPreview()
	{
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Invalid comparison between Unknown and I4
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		ModConfigurationComparison modConfigurationComparison = ModPresetService.CompareLoadedAndSaved();
		List<CurrentModEntry> list = modConfigurationComparison.Saved.Where((CurrentModEntry entry) => entry.IsEnabled).ToList();
		Dictionary<(string, ModSource), CurrentModEntry> dictionary = (from entry in modConfigurationComparison.Loaded
			group entry by (Id: entry.Id, Source: entry.Source)).ToDictionary((IGrouping<(string Id, ModSource Source), CurrentModEntry> group) => group.Key, (IGrouping<(string Id, ModSource Source), CurrentModEntry> group) => group.First());
		Dictionary<(string, ModSource), int> dictionary2 = (from item in modConfigurationComparison.Loaded.Select((CurrentModEntry entry, int index) => (((string Id, ModSource Source) Key, int Order))(Key: (entry.Id, entry.Source), Order: index + 1))
			group item by item.Key).ToDictionary((IGrouping<(string Id, ModSource Source), ((string Id, ModSource Source) Key, int Order)> group) => group.Key, (IGrouping<(string Id, ModSource Source), ((string Id, ModSource Source) Key, int Order)> group) => group.First().Order);
		HashSet<(string, ModSource)> hashSet = list.Select((CurrentModEntry entry) => (Id: entry.Id, Source: entry.Source)).ToHashSet();
		MegaLabel? previewTitleLabel = _previewTitleLabel;
		if (previewTitleLabel != null)
		{
			previewTitleLabel.SetTextAutoSize(ModLocalization.Text("preset.preview.title"));
		}
		MegaLabel? summaryLabel = _summaryLabel;
		if (summaryLabel != null)
		{
			summaryLabel.SetTextAutoSize(ModLocalization.Text(modConfigurationComparison.Matches ? "preset.preview.current_summary_match" : "preset.preview.current_summary_pending", ("loaded", modConfigurationComparison.Loaded.Count.ToString()), ("saved", list.Count.ToString())));
		}
		MegaLabel? summaryLabel2 = _summaryLabel;
		if (summaryLabel2 != null)
		{
			((Control)summaryLabel2).AddThemeColorOverride(StringName.op_Implicit("font_color"), modConfigurationComparison.Matches ? Cream : Yellow);
		}
		List<PreviewRowData> list2 = new List<PreviewRowData>();
		for (int i = 0; i < list.Count; i++)
		{
			CurrentModEntry currentModEntry = list[i];
			dictionary.TryGetValue((currentModEntry.Id, currentModEntry.Source), out var value);
			dictionary2.TryGetValue((currentModEntry.Id, currentModEntry.Source), out var value2);
			bool flag = value == null && (int)currentModEntry.Mod.state == 2;
			TransitionPresentation transition = GetTransition(value != null, afterEnabled: true);
			list2.Add(new PreviewRowData(FormatOrderTransition((value == null) ? null : new int?(value2), i + 1), ModLocalization.Text((value != null) ? "preset.preview.status.loaded" : (flag ? "preset.preview.status.load_failed" : "preset.preview.status.pending_load")), (value != null) ? Green : (flag ? Red : Yellow), ModLocalization.Text((value != null) ? "preset.preview.tooltip.status.loaded" : (flag ? "preset.preview.tooltip.status.load_failed" : "preset.preview.tooltip.status.pending_load")), currentModEntry.DisplayName, currentModEntry.AffectsGameplay, (value == null) ? "—" : EmptyAsDash(value.Version), EmptyAsDash(currentModEntry.Version), GetSourceText(currentModEntry.Source), transition.Text, transition.Color, transition.Tooltip));
		}
		for (int j = 0; j < modConfigurationComparison.Loaded.Count; j++)
		{
			CurrentModEntry currentModEntry2 = modConfigurationComparison.Loaded[j];
			if (!hashSet.Contains((currentModEntry2.Id, currentModEntry2.Source)))
			{
				TransitionPresentation transition2 = GetTransition(beforeEnabled: true, afterEnabled: false);
				list2.Add(new PreviewRowData(FormatOrderTransition(j + 1, null), ModLocalization.Text("preset.preview.status.loaded"), Green, ModLocalization.Text("preset.preview.tooltip.status.loaded"), currentModEntry2.DisplayName, currentModEntry2.AffectsGameplay, EmptyAsDash(currentModEntry2.Version), "—", GetSourceText(currentModEntry2.Source), transition2.Text, transition2.Color, transition2.Tooltip));
			}
		}
		ReplacePreviewRows(list2);
		SetRestartButtonVisible(!modConfigurationComparison.Matches);
		SetStatus(modConfigurationComparison.Matches ? "preset.ready" : "preset.restart_required", modConfigurationComparison.Matches ? Muted : Yellow);
	}

	private void RenderImportPreview(PresetImportAnalysis analysis)
	{
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		MegaLabel? previewTitleLabel = _previewTitleLabel;
		if (previewTitleLabel != null)
		{
			previewTitleLabel.SetTextAutoSize(ModLocalization.Text("preset.preview.imported_title"));
		}
		List<CurrentModEntry> source = ModPresetService.CaptureSaved().ToList();
		List<CurrentModEntry> source2 = source.Where((CurrentModEntry entry) => entry.IsEnabled).ToList();
		HashSet<(string Id, ModSource Source)> savedKeys = source2.Select((CurrentModEntry entry) => (Id: entry.Id, Source: entry.Source)).ToHashSet();
		Dictionary<(string Id, ModSource Source), int> savedOrderByKey = (from item in source2.Select((CurrentModEntry entry, int index) => (((string Id, ModSource Source) Key, int Order))(Key: (entry.Id, entry.Source), Order: index + 1))
			group item by item.Key).ToDictionary((IGrouping<(string Id, ModSource Source), ((string Id, ModSource Source) Key, int Order)> group) => group.Key, (IGrouping<(string Id, ModSource Source), ((string Id, ModSource Source) Key, int Order)> group) => group.First().Order);
		PresetImportMode presetImportMode = ((_importMode == ImportMode.Compatible) ? PresetImportMode.Compatible : PresetImportMode.Overwrite);
		PresetApplyPlan presetApplyPlan = (analysis.CanApply ? ModPresetService.CreateApplyPlan(analysis, presetImportMode) : null);
		Dictionary<(string Id, ModSource Source), SettingsSaveMod> plannedEntriesByKey = new Dictionary<(string, ModSource), SettingsSaveMod>();
		Dictionary<(string Id, ModSource Source), int> plannedEnabledOrderByKey = new Dictionary<(string, ModSource), int>();
		if (presetApplyPlan != null)
		{
			foreach (SettingsSaveMod item5 in presetApplyPlan.FinalOrder)
			{
				plannedEntriesByKey[(item5.Id, item5.Source)] = item5;
			}
			int num = 0;
			foreach (SettingsSaveMod item6 in presetApplyPlan.FinalOrder.Where((SettingsSaveMod entry) => entry.IsEnabled))
			{
				num = (plannedEnabledOrderByKey[(item6.Id, item6.Source)] = num + 1);
			}
		}
		HashSet<(string Id, ModSource Source)> targetKeys = new HashSet<(string, ModSource)>();
		List<PreviewRowData> list = analysis.Items.Select(delegate(PresetImportItem item)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0199: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
			(string StatusKey, string TooltipKey, Color Color) importPresentation = GetImportPresentation(item.Status);
			string item2 = importPresentation.StatusKey;
			string item3 = importPresentation.TooltipKey;
			Color item4 = importPresentation.Color;
			Mod? matchedMod = item.MatchedMod;
			ModSource val = (ModSource)((matchedMod == null) ? ((item.Entry.ParsedSource != ModPresetSource.Workshop) ? 1 : 2) : ((int)matchedMod.modSource));
			targetKeys.Add((item.Entry.Id, val));
			bool num5 = savedKeys.Contains((item.Entry.Id, val));
			savedOrderByKey.TryGetValue((item.Entry.Id, val), out var value4);
			SettingsSaveMod value5;
			bool flag3 = !plannedEntriesByKey.TryGetValue((item.Entry.Id, val), out value5) || value5.IsEnabled;
			int value6 = item.Order + 1;
			if (flag3 && plannedEnabledOrderByKey.TryGetValue((item.Entry.Id, val), out var value7))
			{
				value6 = value7;
			}
			TransitionPresentation transition2 = GetTransition(num5, flag3);
			return new PreviewRowData(FormatOrderTransition(num5 ? new int?(value4) : null, flag3 ? new int?(value6) : null), ModLocalization.Text(item2), item4, ModLocalization.Text(item3), item.MatchedMod?.manifest?.name ?? item.Entry.Id, item.Entry.AffectsGameplay, EmptyAsDash(item.CurrentVersion), item.Entry.Version, GetSourceText(val), transition2.Text, transition2.Color, transition2.Tooltip);
		}).ToList();
		HashSet<string> hashSet = analysis.Items.Select((PresetImportItem item) => item.Entry.Id).ToHashSet<string>(StringComparer.Ordinal);
		bool flag = presetImportMode == PresetImportMode.Compatible;
		int num3 = analysis.Items.Count;
		int num4 = presetApplyPlan?.FinalOrder.Count((SettingsSaveMod entry) => entry.IsEnabled) ?? analysis.Items.Count;
		foreach (CurrentModEntry item7 in source.Where((CurrentModEntry entry) => !targetKeys.Contains((entry.Id, entry.Source))))
		{
			(string, ModSource) key = (item7.Id, item7.Source);
			SettingsSaveMod value;
			bool flag2 = ((presetApplyPlan == null) ? (!hashSet.Contains(item7.Id) && (string.Equals(item7.Id, "ModPresetManager", StringComparison.Ordinal) || (item7.IsEnabled && flag && !item7.AffectsGameplay))) : (plannedEntriesByKey.TryGetValue(key, out value) && value.IsEnabled));
			if (item7.IsEnabled || flag2)
			{
				if (presetApplyPlan == null && flag2)
				{
					num4++;
				}
				TransitionPresentation transition = GetTransition(item7.IsEnabled, flag2);
				savedOrderByKey.TryGetValue((item7.Id, item7.Source), out var value2);
				int? after = null;
				if (flag2)
				{
					after = (plannedEnabledOrderByKey.TryGetValue(key, out var value3) ? value3 : (++num3));
				}
				string key2 = (item7.IsEnabled ? "preset.preview.status.extra" : "preset.preview.status.disabled");
				string key3 = (item7.IsEnabled ? "preset.preview.tooltip.status.extra" : "preset.preview.tooltip.status.disabled");
				list.Add(new PreviewRowData(FormatOrderTransition(item7.IsEnabled ? new int?(value2) : null, after), ModLocalization.Text(key2), item7.IsEnabled ? Green : Muted, ModLocalization.Text(key3), item7.DisplayName, item7.AffectsGameplay, EmptyAsDash(item7.Version), flag2 ? EmptyAsDash(item7.Version) : "—", GetSourceText(item7.Source), transition.Text, transition.Color, transition.Tooltip));
			}
		}
		MegaLabel? summaryLabel = _summaryLabel;
		if (summaryLabel != null)
		{
			summaryLabel.SetTextAutoSize(BuildImportSummary(num4, analysis));
		}
		MegaLabel? summaryLabel2 = _summaryLabel;
		if (summaryLabel2 != null)
		{
			((Control)summaryLabel2).AddThemeColorOverride(StringName.op_Implicit("font_color"), Cream);
		}
		ReplacePreviewRows(list);
		SetRestartButtonVisible(shouldRestart: false);
	}

	private static string BuildImportSummary(int finalEnabledCount, PresetImportAnalysis analysis)
	{
		List<string> list = new List<string>();
		list.Add(ModLocalization.Text("preset.preview.import_summary.result"));
		list.Add(ModLocalization.Text("preset.preview.import_summary.enabled", ("count", finalEnabledCount.ToString())));
		list.Add(ModLocalization.Text("preset.preview.import_summary.exact", ("count", analysis.ExactCount.ToString())));
		List<string> list2 = list;
		if (analysis.FallbackCount > 0)
		{
			list2.Add(ModLocalization.Text("preset.preview.import_summary.fallback", ("count", analysis.FallbackCount.ToString())));
		}
		if (analysis.DownloadCount > 0)
		{
			list2.Add(ModLocalization.Text("preset.preview.import_summary.download", ("count", analysis.DownloadCount.ToString())));
		}
		if (analysis.ProblemCount > 0)
		{
			list2.Add(ModLocalization.Text("preset.preview.import_summary.problems", ("count", analysis.ProblemCount.ToString())));
		}
		return string.Join("  ·  ", list2);
	}

	private void ReplacePreviewRows(IEnumerable<PreviewRowData> data)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (_previewRows == null)
		{
			return;
		}
		foreach (Node child in ((Node)_previewRows).GetChildren(false))
		{
			((Node)_previewRows).RemoveChild(child);
			child.QueueFree();
		}
		int num = 0;
		foreach (PreviewRowData datum in data)
		{
			((Node)_previewRows).AddChild((Node)(object)CreatePreviewRow(datum, num++ % 2 == 1), false, (InternalMode)0);
		}
		Callable val = Callable.From((Action)SyncHeaderWithScrollbar);
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private void SyncHeaderWithScrollbar()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (_headerMargin != null && _previewScroll != null)
		{
			VScrollBar vScrollBar = _previewScroll.GetVScrollBar();
			int num = (((CanvasItem)vScrollBar).Visible ? Mathf.CeilToInt(((Control)vScrollBar).Size.X) : 0);
			((Control)_headerMargin).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 8 + num);
		}
	}

	private void RefreshModdingScreenTickboxes()
	{
		if (_moddingScreen == null || !GodotObject.IsInstanceValid((GodotObject)(object)_moddingScreen))
		{
			return;
		}
		foreach (NModMenuRow item in ((IEnumerable)((Node)((Node)((Node)_moddingScreen).GetNode<Control>(NodePath.op_Implicit("%ModsScrollContainer"))).GetNode<Control>(NodePath.op_Implicit("Mask/Content"))).GetChildren(false)).OfType<NModMenuRow>())
		{
			if (item.Mod != null)
			{
				ModSettings modSettings = SaveManager.Instance.SettingsSave.ModSettings;
				bool flag = modSettings == null || !modSettings.IsModDisabled(item.Mod);
				NTickbox node = ((Node)item).GetNode<NTickbox>(NodePath.op_Implicit("Tickbox"));
				if (node.IsTicked != flag)
				{
					node.ForceToggleTick();
				}
			}
		}
		SaveManager.Instance.SaveSettings();
		_moddingScreen.OnModEnabledOrDisabled();
	}

	private void RestartGame()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (WorkshopSubscriptionBatch.Active == null && _pendingPreset == null)
		{
			string error;
			if (ModPresetService.CompareLoadedAndSaved().Matches)
			{
				SetRestartButtonVisible(shouldRestart: false);
			}
			else if (!GameRestartService.TryRestart(out error))
			{
				SetStatus("preset.restart_failed", Red);
			}
		}
	}

	private void SetRestartButtonVisible(bool shouldRestart)
	{
		if (_restartButton != null)
		{
			((CanvasItem)_restartButton).Visible = shouldRestart && _pendingPreset == null && WorkshopSubscriptionBatch.Active == null && GameRestartService.IsSupported;
		}
	}

	private void SetStatus(string key, Color color, params (string Name, string Value)[] variables)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (_statusLabel != null)
		{
			_statusLabel.SetTextAutoSize(ModLocalization.Text(key, variables));
			((Control)_statusLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), color);
		}
	}

	private static string GetParseErrorKey(PresetParseError error)
	{
		return error switch
		{
			PresetParseError.Empty => "preset.import.error.empty", 
			PresetParseError.UnsupportedFormat => "preset.import.error.format", 
			PresetParseError.UnsupportedSchema => "preset.import.error.schema", 
			PresetParseError.InvalidEntry => "preset.import.error.entry", 
			PresetParseError.DuplicateMod => "preset.import.error.duplicate", 
			_ => "preset.import.error.json", 
		};
	}

	private static (string StatusKey, string TooltipKey, Color Color) GetImportPresentation(PresetImportStatus status)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		return status switch
		{
			PresetImportStatus.Exact => (StatusKey: "preset.preview.status.exact", TooltipKey: "preset.preview.tooltip.status.exact", Color: Green), 
			PresetImportStatus.LocalFallback => (StatusKey: "preset.preview.status.fallback", TooltipKey: "preset.preview.tooltip.status.fallback", Color: Yellow), 
			PresetImportStatus.DownloadRequired => (StatusKey: "preset.preview.status.download", TooltipKey: "preset.preview.tooltip.status.download", Color: Blue), 
			PresetImportStatus.Missing => (StatusKey: "preset.preview.status.missing", TooltipKey: "preset.preview.tooltip.status.missing", Color: Red), 
			PresetImportStatus.VersionMismatch => (StatusKey: "preset.preview.status.mismatch", TooltipKey: "preset.preview.tooltip.status.mismatch", Color: Red), 
			PresetImportStatus.Ambiguous => (StatusKey: "preset.preview.status.ambiguous", TooltipKey: "preset.preview.tooltip.status.ambiguous", Color: Red), 
			_ => throw new ArgumentOutOfRangeException("status", status, null), 
		};
	}

	private static TransitionPresentation GetTransition(bool beforeEnabled, bool afterEnabled)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		string text = (beforeEnabled ? ((!afterEnabled) ? "disable" : "keep_enabled") : ((!afterEnabled) ? "keep_disabled" : "enable"));
		string text2 = text;
		Color val = (beforeEnabled ? (afterEnabled ? Green : Red) : ((!afterEnabled) ? Muted : Green));
		Color color = val;
		return new TransitionPresentation(ModLocalization.Text("preset.preview.transition." + text2), color, ModLocalization.Text("preset.preview.tooltip.transition." + text2));
	}

	private static string GetSourceText(ModSource source)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		return ModLocalization.Text(((int)source == 2) ? "preset.preview.source.workshop" : "preset.preview.source.local");
	}

	private static string FormatOrderTransition(int? before, int? after)
	{
		if (before.HasValue && before == after)
		{
			return before.Value.ToString("D2");
		}
		string obj = before?.ToString("D2") ?? "—";
		string text = after?.ToString("D2") ?? "—";
		return obj + " → " + text;
	}

	private static string EmptyAsDash(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "—";
	}

	private static HBoxContainer CreateTableRow()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		HBoxContainer val = new HBoxContainer
		{
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
		return val;
	}

	private static void AddCell(HBoxContainer row, string text, float width, Color color, bool expand = false, string? tooltip = null)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		MegaLabel val = CreateLabel(text, 19, color, (HorizontalAlignment)0);
		((Label)val).TextOverrunBehavior = (OverrunBehavior)3;
		((Control)val).CustomMinimumSize = new Vector2(width, 0f);
		((Control)val).SizeFlagsHorizontal = (SizeFlags)(expand ? 3 : 0);
		if (!string.IsNullOrWhiteSpace(tooltip))
		{
			((Control)val).TooltipText = tooltip;
			((Control)val).MouseFilter = (MouseFilterEnum)1;
		}
		((Node)row).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	private static MegaLabel CreateLabel(string text, int fontSize, Color color, HorizontalAlignment alignment = 0L)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		MegaLabel val = new MegaLabel
		{
			Text = text,
			HorizontalAlignment = alignment,
			VerticalAlignment = (VerticalAlignment)1,
			AutoSizeEnabled = false,
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val).AddThemeFontOverride(StringName.op_Implicit("font"), ResourceLoader.Load<Font>("res://themes/kreon_regular_shared.tres", (string)null, (CacheMode)1));
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), fontSize);
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), color);
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_shadow_color"), new Color(0f, 0f, 0f, 0.55f));
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("shadow_offset_x"), 2);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("shadow_offset_y"), 2);
		return val;
	}

	private static ColorRect CreateDivider(Color color, float alpha)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		color.A = alpha;
		return new ColorRect
		{
			Color = color,
			CustomMinimumSize = new Vector2(0f, 2f),
			MouseFilter = (MouseFilterEnum)2
		};
	}

	private static ProgressBar CreateDownloadProgressBar()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		StyleBoxFlat val = new StyleBoxFlat
		{
			BgColor = new Color("152333"),
			CornerRadiusTopLeft = 5,
			CornerRadiusTopRight = 5,
			CornerRadiusBottomLeft = 5,
			CornerRadiusBottomRight = 5
		};
		StyleBoxFlat val2 = new StyleBoxFlat
		{
			BgColor = Blue,
			CornerRadiusTopLeft = 5,
			CornerRadiusTopRight = 5,
			CornerRadiusBottomLeft = 5,
			CornerRadiusBottomRight = 5
		};
		ProgressBar val3 = new ProgressBar
		{
			MinValue = 0.0,
			MaxValue = 100.0,
			Value = 0.0,
			ShowPercentage = false,
			CustomMinimumSize = new Vector2(0f, 12f),
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val3).AddThemeStyleboxOverride(StringName.op_Implicit("background"), (StyleBox)(object)val);
		((Control)val3).AddThemeStyleboxOverride(StringName.op_Implicit("fill"), (StyleBox)(object)val2);
		return val3;
	}

	private static Panel CreateFlatPanel(Color background, Color border, int borderWidth)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		StyleBoxFlat val = new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6
		};
		Panel val2 = new Panel
		{
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val2).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)val);
		return val2;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Expected O, but got Unknown
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Expected O, but got Unknown
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Expected O, but got Unknown
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Expected O, but got Unknown
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Expected O, but got Unknown
		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Expected O, but got Unknown
		//IL_0508: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_053c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0547: Expected O, but got Unknown
		//IL_0542: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0571: Unknown result type (might be due to invalid IL or missing references)
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_059f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0623: Unknown result type (might be due to invalid IL or missing references)
		//IL_062c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0652: Unknown result type (might be due to invalid IL or missing references)
		//IL_065b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0681: Unknown result type (might be due to invalid IL or missing references)
		//IL_068a: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0703: Unknown result type (might be due to invalid IL or missing references)
		//IL_070e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0734: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Unknown result type (might be due to invalid IL or missing references)
		//IL_0778: Unknown result type (might be due to invalid IL or missing references)
		//IL_0783: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0807: Unknown result type (might be due to invalid IL or missing references)
		//IL_0810: Unknown result type (might be due to invalid IL or missing references)
		//IL_0836: Unknown result type (might be due to invalid IL or missing references)
		//IL_083f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0865: Unknown result type (might be due to invalid IL or missing references)
		//IL_086e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0894: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_090c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0917: Unknown result type (might be due to invalid IL or missing references)
		//IL_093d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0961: Unknown result type (might be due to invalid IL or missing references)
		//IL_096c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0992: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f7: Expected O, but got Unknown
		//IL_09f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a22: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a56: Expected O, but got Unknown
		//IL_0a51: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a72: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b38: Expected O, but got Unknown
		//IL_0b33: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b57: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b78: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bbb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bc6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bfc: Expected O, but got Unknown
		//IL_0bf7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c48: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c7e: Expected O, but got Unknown
		//IL_0c79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c83: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb9: Expected O, but got Unknown
		//IL_0cb4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cfb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d27: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(46)
		{
			new MethodInfo(MethodName.Show, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("moddingScreen"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("inputEvent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateWindow, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Panel"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateTooltipTheme, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Theme"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateTopActions, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("HBoxContainer"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetExportButtonText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetImportButtonText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetExportOptionDescription, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetImportOptionDescription, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateActionTooltips, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SelectExportMode, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("option"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ExportSelectedPresetToClipboard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SelectImportMode, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("option"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetImportActionAsApply, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("localizationKey"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetImportActionAsImport, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateSummary, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreatePreview, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateHeaderRow, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateFooter, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ExportToClipboard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("scope"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ImportOrApplyClipboardPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CancelPendingImport, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AnalyzeAndRenderPendingPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AttachActiveDownload, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateDownloadProgressDisplay, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.HideDownloadProgress, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.FormatBytes, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("bytes"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ReanalyzeAfterDownloads, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("installed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("failed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ApplyPendingPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RenderCurrentPreview, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SyncHeaderWithScrollbar, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshModdingScreenTickboxes, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RestartGame, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetRestartButtonVisible, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("shouldRestart"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GetParseErrorKey, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("error"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GetSourceText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("source"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.EmptyAsDash, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CreateTableRow, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("HBoxContainer"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AddCell, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("row"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("HBoxContainer"), false),
				new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("width"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)1, StringName.op_Implicit("expand"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("tooltip"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CreateLabel, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Label"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("fontSize"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("alignment"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CreateDivider, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("ColorRect"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("alpha"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CreateDownloadProgressBar, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("ProgressBar"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateFlatPanel, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Panel"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("background"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)20, StringName.op_Implicit("border"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("borderWidth"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_051a: Unknown result type (might be due to invalid IL or missing references)
		//IL_051f: Unknown result type (might be due to invalid IL or missing references)
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0617: Unknown result type (might be due to invalid IL or missing references)
		//IL_064a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_0685: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0720: Unknown result type (might be due to invalid IL or missing references)
		//IL_0725: Unknown result type (might be due to invalid IL or missing references)
		//IL_0772: Unknown result type (might be due to invalid IL or missing references)
		//IL_0797: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_081f: Unknown result type (might be due to invalid IL or missing references)
		//IL_083b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0840: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0867: Unknown result type (might be due to invalid IL or missing references)
		//IL_086c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0892: Unknown result type (might be due to invalid IL or missing references)
		//IL_089f: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c0: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Show && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Show(VariantUtils.ConvertTo<NModdingScreen>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
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
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateWindow && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Panel val = CreateWindow();
			ret = VariantUtils.CreateFrom<Panel>(ref val);
			return true;
		}
		if ((ref method) == MethodName.CreateTooltipTheme && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Theme val2 = CreateTooltipTheme();
			ret = VariantUtils.CreateFrom<Theme>(ref val2);
			return true;
		}
		if ((ref method) == MethodName.CreateTopActions && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			HBoxContainer val3 = CreateTopActions();
			ret = VariantUtils.CreateFrom<HBoxContainer>(ref val3);
			return true;
		}
		if ((ref method) == MethodName.GetExportButtonText && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			string exportButtonText = GetExportButtonText();
			ret = VariantUtils.CreateFrom<string>(ref exportButtonText);
			return true;
		}
		if ((ref method) == MethodName.GetImportButtonText && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			string importButtonText = GetImportButtonText();
			ret = VariantUtils.CreateFrom<string>(ref importButtonText);
			return true;
		}
		if ((ref method) == MethodName.GetExportOptionDescription && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			string exportOptionDescription = GetExportOptionDescription();
			ret = VariantUtils.CreateFrom<string>(ref exportOptionDescription);
			return true;
		}
		if ((ref method) == MethodName.GetImportOptionDescription && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			string importOptionDescription = GetImportOptionDescription();
			ret = VariantUtils.CreateFrom<string>(ref importOptionDescription);
			return true;
		}
		if ((ref method) == MethodName.UpdateActionTooltips && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateActionTooltips();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SelectExportMode && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SelectExportMode(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ExportSelectedPresetToClipboard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ExportSelectedPresetToClipboard();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SelectImportMode && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SelectImportMode(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetImportActionAsApply && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetImportActionAsApply(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetImportActionAsImport && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SetImportActionAsImport();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateSummary && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Control val4 = CreateSummary();
			ret = VariantUtils.CreateFrom<Control>(ref val4);
			return true;
		}
		if ((ref method) == MethodName.CreatePreview && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Control val5 = CreatePreview();
			ret = VariantUtils.CreateFrom<Control>(ref val5);
			return true;
		}
		if ((ref method) == MethodName.CreateHeaderRow && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Control val6 = CreateHeaderRow();
			ret = VariantUtils.CreateFrom<Control>(ref val6);
			return true;
		}
		if ((ref method) == MethodName.CreateFooter && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Control val7 = CreateFooter();
			ret = VariantUtils.CreateFrom<Control>(ref val7);
			return true;
		}
		if ((ref method) == MethodName.ExportToClipboard && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ExportToClipboard(VariantUtils.ConvertTo<ModPresetScope>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ImportOrApplyClipboardPreset && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ImportOrApplyClipboardPreset();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CancelPendingImport && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			CancelPendingImport();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AnalyzeAndRenderPendingPreset && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AnalyzeAndRenderPendingPreset();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AttachActiveDownload && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			bool flag = AttachActiveDownload();
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.UpdateDownloadProgressDisplay && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateDownloadProgressDisplay();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.HideDownloadProgress && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			HideDownloadProgress();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.FormatBytes && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text = FormatBytes(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text);
			return true;
		}
		if ((ref method) == MethodName.ReanalyzeAfterDownloads && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			ReanalyzeAfterDownloads(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ApplyPendingPreset && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ApplyPendingPreset();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RenderCurrentPreview && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RenderCurrentPreview();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SyncHeaderWithScrollbar && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SyncHeaderWithScrollbar();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshModdingScreenTickboxes && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshModdingScreenTickboxes();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RestartGame && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RestartGame();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetRestartButtonVisible && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetRestartButtonVisible(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetParseErrorKey && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string parseErrorKey = GetParseErrorKey(VariantUtils.ConvertTo<PresetParseError>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref parseErrorKey);
			return true;
		}
		if ((ref method) == MethodName.GetSourceText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string sourceText = GetSourceText(VariantUtils.ConvertTo<ModSource>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref sourceText);
			return true;
		}
		if ((ref method) == MethodName.EmptyAsDash && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text2 = EmptyAsDash(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text2);
			return true;
		}
		if ((ref method) == MethodName.CreateTableRow && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			HBoxContainer val8 = CreateTableRow();
			ret = VariantUtils.CreateFrom<HBoxContainer>(ref val8);
			return true;
		}
		if ((ref method) == MethodName.AddCell && ((NativeVariantPtrArgs)(ref args)).Count == 6)
		{
			AddCell(VariantUtils.ConvertTo<HBoxContainer>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[3]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[4]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[5]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateLabel && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			MegaLabel val9 = CreateLabel(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<HorizontalAlignment>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = VariantUtils.CreateFrom<MegaLabel>(ref val9);
			return true;
		}
		if ((ref method) == MethodName.CreateDivider && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			ColorRect val10 = CreateDivider(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<ColorRect>(ref val10);
			return true;
		}
		if ((ref method) == MethodName.CreateDownloadProgressBar && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ProgressBar val11 = CreateDownloadProgressBar();
			ret = VariantUtils.CreateFrom<ProgressBar>(ref val11);
			return true;
		}
		if ((ref method) == MethodName.CreateFlatPanel && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			Panel val12 = CreateFlatPanel(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<Panel>(ref val12);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Show && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Show(VariantUtils.ConvertTo<NModdingScreen>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateWindow && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Panel val = CreateWindow();
			ret = VariantUtils.CreateFrom<Panel>(ref val);
			return true;
		}
		if ((ref method) == MethodName.CreateTooltipTheme && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Theme val2 = CreateTooltipTheme();
			ret = VariantUtils.CreateFrom<Theme>(ref val2);
			return true;
		}
		if ((ref method) == MethodName.FormatBytes && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text = FormatBytes(VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text);
			return true;
		}
		if ((ref method) == MethodName.GetParseErrorKey && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string parseErrorKey = GetParseErrorKey(VariantUtils.ConvertTo<PresetParseError>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref parseErrorKey);
			return true;
		}
		if ((ref method) == MethodName.GetSourceText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string sourceText = GetSourceText(VariantUtils.ConvertTo<ModSource>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref sourceText);
			return true;
		}
		if ((ref method) == MethodName.EmptyAsDash && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			string text2 = EmptyAsDash(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<string>(ref text2);
			return true;
		}
		if ((ref method) == MethodName.CreateTableRow && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			HBoxContainer val3 = CreateTableRow();
			ret = VariantUtils.CreateFrom<HBoxContainer>(ref val3);
			return true;
		}
		if ((ref method) == MethodName.AddCell && ((NativeVariantPtrArgs)(ref args)).Count == 6)
		{
			AddCell(VariantUtils.ConvertTo<HBoxContainer>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[3]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[4]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[5]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CreateLabel && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			MegaLabel val4 = CreateLabel(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<HorizontalAlignment>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = VariantUtils.CreateFrom<MegaLabel>(ref val4);
			return true;
		}
		if ((ref method) == MethodName.CreateDivider && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			ColorRect val5 = CreateDivider(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<ColorRect>(ref val5);
			return true;
		}
		if ((ref method) == MethodName.CreateDownloadProgressBar && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ProgressBar val6 = CreateDownloadProgressBar();
			ret = VariantUtils.CreateFrom<ProgressBar>(ref val6);
			return true;
		}
		if ((ref method) == MethodName.CreateFlatPanel && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			Panel val7 = CreateFlatPanel(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<Panel>(ref val7);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Show)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateWindow)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateTooltipTheme)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateTopActions)
		{
			return true;
		}
		if ((ref method) == MethodName.GetExportButtonText)
		{
			return true;
		}
		if ((ref method) == MethodName.GetImportButtonText)
		{
			return true;
		}
		if ((ref method) == MethodName.GetExportOptionDescription)
		{
			return true;
		}
		if ((ref method) == MethodName.GetImportOptionDescription)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateActionTooltips)
		{
			return true;
		}
		if ((ref method) == MethodName.SelectExportMode)
		{
			return true;
		}
		if ((ref method) == MethodName.ExportSelectedPresetToClipboard)
		{
			return true;
		}
		if ((ref method) == MethodName.SelectImportMode)
		{
			return true;
		}
		if ((ref method) == MethodName.SetImportActionAsApply)
		{
			return true;
		}
		if ((ref method) == MethodName.SetImportActionAsImport)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateSummary)
		{
			return true;
		}
		if ((ref method) == MethodName.CreatePreview)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateHeaderRow)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateFooter)
		{
			return true;
		}
		if ((ref method) == MethodName.ExportToClipboard)
		{
			return true;
		}
		if ((ref method) == MethodName.ImportOrApplyClipboardPreset)
		{
			return true;
		}
		if ((ref method) == MethodName.CancelPendingImport)
		{
			return true;
		}
		if ((ref method) == MethodName.AnalyzeAndRenderPendingPreset)
		{
			return true;
		}
		if ((ref method) == MethodName.AttachActiveDownload)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateDownloadProgressDisplay)
		{
			return true;
		}
		if ((ref method) == MethodName.HideDownloadProgress)
		{
			return true;
		}
		if ((ref method) == MethodName.FormatBytes)
		{
			return true;
		}
		if ((ref method) == MethodName.ReanalyzeAfterDownloads)
		{
			return true;
		}
		if ((ref method) == MethodName.ApplyPendingPreset)
		{
			return true;
		}
		if ((ref method) == MethodName.RenderCurrentPreview)
		{
			return true;
		}
		if ((ref method) == MethodName.SyncHeaderWithScrollbar)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshModdingScreenTickboxes)
		{
			return true;
		}
		if ((ref method) == MethodName.RestartGame)
		{
			return true;
		}
		if ((ref method) == MethodName.SetRestartButtonVisible)
		{
			return true;
		}
		if ((ref method) == MethodName.GetParseErrorKey)
		{
			return true;
		}
		if ((ref method) == MethodName.GetSourceText)
		{
			return true;
		}
		if ((ref method) == MethodName.EmptyAsDash)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateTableRow)
		{
			return true;
		}
		if ((ref method) == MethodName.AddCell)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateLabel)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateDivider)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateDownloadProgressBar)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateFlatPanel)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._moddingScreen)
		{
			_moddingScreen = VariantUtils.ConvertTo<NModdingScreen>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._exportAction)
		{
			_exportAction = VariantUtils.ConvertTo<PresetSplitActionButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._importAction)
		{
			_importAction = VariantUtils.ConvertTo<PresetSplitActionButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._restartButton)
		{
			_restartButton = VariantUtils.ConvertTo<PresetActionButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._statusLabel)
		{
			_statusLabel = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._downloadProgressLabel)
		{
			_downloadProgressLabel = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._downloadProgressBar)
		{
			_downloadProgressBar = VariantUtils.ConvertTo<ProgressBar>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._summaryLabel)
		{
			_summaryLabel = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._previewTitleLabel)
		{
			_previewTitleLabel = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._headerMargin)
		{
			_headerMargin = VariantUtils.ConvertTo<MarginContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._previewScroll)
		{
			_previewScroll = VariantUtils.ConvertTo<ScrollContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._previewRows)
		{
			_previewRows = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._downloadPollElapsed)
		{
			_downloadPollElapsed = VariantUtils.ConvertTo<double>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._exportMode)
		{
			_exportMode = VariantUtils.ConvertTo<ExportMode>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._importMode)
		{
			_importMode = VariantUtils.ConvertTo<ImportMode>(ref value);
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
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.DefaultFocusedControl)
		{
			Control defaultFocusedControl = DefaultFocusedControl;
			value = VariantUtils.CreateFrom<Control>(ref defaultFocusedControl);
			return true;
		}
		if ((ref name) == PropertyName._moddingScreen)
		{
			value = VariantUtils.CreateFrom<NModdingScreen>(ref _moddingScreen);
			return true;
		}
		if ((ref name) == PropertyName._exportAction)
		{
			value = VariantUtils.CreateFrom<PresetSplitActionButton>(ref _exportAction);
			return true;
		}
		if ((ref name) == PropertyName._importAction)
		{
			value = VariantUtils.CreateFrom<PresetSplitActionButton>(ref _importAction);
			return true;
		}
		if ((ref name) == PropertyName._restartButton)
		{
			value = VariantUtils.CreateFrom<PresetActionButton>(ref _restartButton);
			return true;
		}
		if ((ref name) == PropertyName._statusLabel)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _statusLabel);
			return true;
		}
		if ((ref name) == PropertyName._downloadProgressLabel)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _downloadProgressLabel);
			return true;
		}
		if ((ref name) == PropertyName._downloadProgressBar)
		{
			value = VariantUtils.CreateFrom<ProgressBar>(ref _downloadProgressBar);
			return true;
		}
		if ((ref name) == PropertyName._summaryLabel)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _summaryLabel);
			return true;
		}
		if ((ref name) == PropertyName._previewTitleLabel)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _previewTitleLabel);
			return true;
		}
		if ((ref name) == PropertyName._headerMargin)
		{
			value = VariantUtils.CreateFrom<MarginContainer>(ref _headerMargin);
			return true;
		}
		if ((ref name) == PropertyName._previewScroll)
		{
			value = VariantUtils.CreateFrom<ScrollContainer>(ref _previewScroll);
			return true;
		}
		if ((ref name) == PropertyName._previewRows)
		{
			value = VariantUtils.CreateFrom<VBoxContainer>(ref _previewRows);
			return true;
		}
		if ((ref name) == PropertyName._downloadPollElapsed)
		{
			value = VariantUtils.CreateFrom<double>(ref _downloadPollElapsed);
			return true;
		}
		if ((ref name) == PropertyName._exportMode)
		{
			value = VariantUtils.CreateFrom<ExportMode>(ref _exportMode);
			return true;
		}
		if ((ref name) == PropertyName._importMode)
		{
			value = VariantUtils.CreateFrom<ImportMode>(ref _importMode);
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
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._moddingScreen, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._exportAction, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._importAction, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._restartButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._statusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._downloadProgressLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._downloadProgressBar, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._summaryLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._previewTitleLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._headerMargin, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._previewScroll, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._previewRows, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._downloadPollElapsed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._exportMode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._importMode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.DefaultFocusedControl, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._moddingScreen, Variant.From<NModdingScreen>(ref _moddingScreen));
		info.AddProperty(PropertyName._exportAction, Variant.From<PresetSplitActionButton>(ref _exportAction));
		info.AddProperty(PropertyName._importAction, Variant.From<PresetSplitActionButton>(ref _importAction));
		info.AddProperty(PropertyName._restartButton, Variant.From<PresetActionButton>(ref _restartButton));
		info.AddProperty(PropertyName._statusLabel, Variant.From<MegaLabel>(ref _statusLabel));
		info.AddProperty(PropertyName._downloadProgressLabel, Variant.From<MegaLabel>(ref _downloadProgressLabel));
		info.AddProperty(PropertyName._downloadProgressBar, Variant.From<ProgressBar>(ref _downloadProgressBar));
		info.AddProperty(PropertyName._summaryLabel, Variant.From<MegaLabel>(ref _summaryLabel));
		info.AddProperty(PropertyName._previewTitleLabel, Variant.From<MegaLabel>(ref _previewTitleLabel));
		info.AddProperty(PropertyName._headerMargin, Variant.From<MarginContainer>(ref _headerMargin));
		info.AddProperty(PropertyName._previewScroll, Variant.From<ScrollContainer>(ref _previewScroll));
		info.AddProperty(PropertyName._previewRows, Variant.From<VBoxContainer>(ref _previewRows));
		info.AddProperty(PropertyName._downloadPollElapsed, Variant.From<double>(ref _downloadPollElapsed));
		info.AddProperty(PropertyName._exportMode, Variant.From<ExportMode>(ref _exportMode));
		info.AddProperty(PropertyName._importMode, Variant.From<ImportMode>(ref _importMode));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._moddingScreen, ref val))
		{
			_moddingScreen = ((Variant)(ref val)).As<NModdingScreen>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._exportAction, ref val2))
		{
			_exportAction = ((Variant)(ref val2)).As<PresetSplitActionButton>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._importAction, ref val3))
		{
			_importAction = ((Variant)(ref val3)).As<PresetSplitActionButton>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._restartButton, ref val4))
		{
			_restartButton = ((Variant)(ref val4)).As<PresetActionButton>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._statusLabel, ref val5))
		{
			_statusLabel = ((Variant)(ref val5)).As<MegaLabel>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._downloadProgressLabel, ref val6))
		{
			_downloadProgressLabel = ((Variant)(ref val6)).As<MegaLabel>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._downloadProgressBar, ref val7))
		{
			_downloadProgressBar = ((Variant)(ref val7)).As<ProgressBar>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._summaryLabel, ref val8))
		{
			_summaryLabel = ((Variant)(ref val8)).As<MegaLabel>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._previewTitleLabel, ref val9))
		{
			_previewTitleLabel = ((Variant)(ref val9)).As<MegaLabel>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._headerMargin, ref val10))
		{
			_headerMargin = ((Variant)(ref val10)).As<MarginContainer>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._previewScroll, ref val11))
		{
			_previewScroll = ((Variant)(ref val11)).As<ScrollContainer>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._previewRows, ref val12))
		{
			_previewRows = ((Variant)(ref val12)).As<VBoxContainer>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._downloadPollElapsed, ref val13))
		{
			_downloadPollElapsed = ((Variant)(ref val13)).As<double>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._exportMode, ref val14))
		{
			_exportMode = ((Variant)(ref val14)).As<ExportMode>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._importMode, ref val15))
		{
			_importMode = ((Variant)(ref val15)).As<ImportMode>();
		}
	}
}
