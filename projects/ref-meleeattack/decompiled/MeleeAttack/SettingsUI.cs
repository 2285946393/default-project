using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace MeleeAttack;

public static class SettingsUI
{
	public const string KEY_SILENT = "meleeattack_silent";

	public const string KEY_IRONCLAD = "meleeattack_ironclad";

	public const string KEY_DEFECT = "meleeattack_defect";

	public const string KEY_NECROBINDER = "meleeattack_necro";

	public const string KEY_REGENT = "meleeattack_regent";

	public const string KEY_OSTY = "meleeattack_osty";

	public const string KEY_BYRD = "meleeattack_byrd";

	public const string KEY_IRONCLAD_HEAVY = "meleeattack_ironclad_heavy";

	public const string KEY_IRONCLAD_HEAVY_DELAY = "meleeattack_delay_ironclad_heavy";

	public const string KEY_DELAY_SILENT = "meleeattack_delay_silent";

	public const string KEY_DELAY_IRONCLAD = "meleeattack_delay_ironclad";

	public const string KEY_DELAY_DEFECT = "meleeattack_delay_defect";

	public const string KEY_DELAY_NECROBINDER = "meleeattack_delay_necro";

	public const string KEY_DELAY_REGENT = "meleeattack_delay_regent";

	public const string KEY_DELAY_OSTY = "meleeattack_delay_osty";

	public const string KEY_DELAY_BYRD = "meleeattack_delay_byrd";

	public const string KEY_DELAY_OTHER = "meleeattack_delay_other";

	public const string KEY_MONSTER_GLOBAL = "meleeattack_monster_global";

	public const string KEY_OTHER_TELEPORT = "meleeattack_global_teleport";

	public const string KEY_GLOBAL_SKILLS = "meleeattack_global_skills";

	public const string KEY_FAST_MODE = "meleeattack_fast_mode";

	public const string KEY_SKILL_BULLET_TIME = "meleeattack_skill_bullet_time";

	public const string KEY_SKILL_KNIFE_TRAP = "meleeattack_skill_knife_trap";

	public const string KEY_SKILL_BACKFLIP = "meleeattack_skill_backflip";

	public const string KEY_SKILL_GRAND_FINALE = "meleeattack_skill_grand_finale";

	public const string KEY_SKILL_WRAITH_FORM = "meleeattack_skill_wraith_form";

	public const string KEY_SKILL_AFTERIMAGE = "meleeattack_skill_afterimage";

	public const string KEY_SKILL_DEMON_FORM = "meleeattack_skill_demon_form";

	public const string KEY_SKILL_BIASED_COGNITION = "meleeattack_skill_biased_cognition";

	public const string KEY_SKILL_THUNDER_STORM = "meleeattack_skill_thunder_storm";

	public const string KEY_SKILL_ZOOM_EFFECT = "meleeattack_skill_zoom_effect";

	public const string KEY_SKILL_SLICE = "meleeattack_skill_slice";

	public const string KEY_SKILL_PINPOINT = "meleeattack_skill_pinpoint";

	public const string KEY_SKILL_BLOODLETTING = "meleeattack_skill_bloodletting";

	public const string KEY_SKILL_CONFLAGRATION = "meleeattack_skill_conflagration";

	public const string KEY_SKILL_BLUDGEON = "meleeattack_skill_bludgeon";

	public const string KEY_SKILL_UPPERCUT = "meleeattack_skill_uppercut";

	public const string KEY_SKILL_REAPER_FORM = "meleeattack_skill_reaper_form";

	public const string KEY_SKILL_BLUR = "meleeattack_skill_blur";

	public const string KEY_SKILL_BURST = "meleeattack_skill_burst";

	public const string KEY_SKILL_MURDER = "meleeattack_skill_murder";

	public const string KEY_SKILL_INTANGIBLE = "meleeattack_skill_intangible";

	public const string KEY_SKILL_BODY_SLAM = "meleeattack_skill_body_slam";

	public const string KEY_SKILL_WHIRLWIND = "meleeattack_skill_whirlwind";

	private const bool DEFAULT_MONSTER_GLOBAL = false;

	private const bool DEFAULT_OTHER_TELEPORT = true;

	private const bool DEFAULT_SILENT = true;

	private const bool DEFAULT_IRONCLAD = true;

	private const bool DEFAULT_DEFECT = true;

	private const bool DEFAULT_NECROBINDER = true;

	private const bool DEFAULT_REGENT = false;

	private const bool DEFAULT_OSTY = true;

	private const bool DEFAULT_BYRD = true;

	private const bool DEFAULT_IRONCLAD_HEAVY = true;

	private const float DEFAULT_DELAY = 0.4f;

	private const bool DEFAULT_SKILL = true;

	private const bool DEFAULT_ZOOM = true;

	private const bool DEFAULT_FAST_MODE = false;

	private const float DEFAULT_IRONCLAD_HEAVY_DELAY = 0.35f;

	private static readonly Color CreamGold = new Color(0.831f, 0.784f, 0.557f, 1f);

	private static readonly Color DimText = new Color(0.541f, 0.494f, 0.361f, 1f);

	private static readonly Color TextColor = new Color(0.9f, 0.85f, 0.75f, 1f);

	private static readonly Color NavIdleBg = new Color(0.118f, 0.157f, 0.173f, 0.84f);

	private static readonly Color NavSelectedBg = new Color(0.2f, 0.31f, 0.36f, 0.96f);

	private static readonly Color RowSeparatorColor = new Color(0.52f, 0.49f, 0.42f, 0.55f);

	private static NSettingsPanel _panel;

	private static NSettingsTab _tab;

	private static bool _tabConnected;

	private static VBoxContainer _characterPage;

	private static VBoxContainer _skillPage;

	private static VBoxContainer _settingsVBox;

	private static Button _characterNavBtn;

	private static Button _skillNavBtn;

	private static Dictionary<string, TextureButton> _characterToggleButtons = new Dictionary<string, TextureButton>();

	private static Dictionary<string, HSlider> _characterSliders = new Dictionary<string, HSlider>();

	private static Dictionary<string, Label> _sliderValueLabels = new Dictionary<string, Label>();

	private static Dictionary<string, TextureButton> _skillToggleButtons = new Dictionary<string, TextureButton>();

	private static TextureButton _monsterGlobalToggle;

	private static TextureButton _otherCharacterToggle;

	private static TextureButton _globalSkillsToggle;

	private static Dictionary<string, bool> _configCache;

	private static Dictionary<string, float> _delayCache;

	private static readonly HashSet<string> _mainCharacters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Silent", "Ironclad", "Defect", "Necrobinder", "Regent", "Osty", "Byrd" };

	private static PackedScene _tickboxScene;

	private static string ConfigFilePath => "user://MeleeAttack_settings.json";

	private static void BuildSettingsUI(VBoxContainer parentVBox)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		HBoxContainer val = new HBoxContainer();
		((Node)val).Name = StringName.op_Implicit("MeleeAttackShell");
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)1;
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 12);
		((Node)parentVBox).AddChild((Node)(object)val, false, (InternalMode)0);
		VBoxContainer val2 = new VBoxContainer();
		((Node)val2).Name = StringName.op_Implicit("Sidebar");
		((Control)val2).CustomMinimumSize = new Vector2(118f, 0f);
		((Control)val2).SizeFlagsVertical = (SizeFlags)1;
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		_characterNavBtn = CreateNavButton("角色", "character", val2);
		_skillNavBtn = CreateNavButton("技能", "skill", val2);
		VBoxContainer val3 = new VBoxContainer();
		((Node)val3).Name = StringName.op_Implicit("ContentHost");
		((Control)val3).CustomMinimumSize = new Vector2(882f, 0f);
		((Control)val3).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val3).SizeFlagsVertical = (SizeFlags)1;
		((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 0);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		_characterPage = CreatePage(val3, "character");
		_skillPage = CreatePage(val3, "skill");
		FillCharacterPage(_characterPage);
		FillSkillPage(_skillPage);
	}

	private static VBoxContainer CreatePage(VBoxContainer host, string pageId)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		VBoxContainer val = new VBoxContainer();
		((Node)val).Name = StringName.op_Implicit("Page_" + pageId);
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)1;
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 0);
		((GodotObject)val).SetMeta(StringName.op_Implicit("meleeattack_page_id"), Variant.op_Implicit(pageId));
		((Node)host).AddChild((Node)(object)val, false, (InternalMode)0);
		return val;
	}

	private static Button CreateNavButton(string label, string pageId, VBoxContainer sidebar)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		Button val = new Button();
		val.Text = label;
		((BaseButton)val).ToggleMode = true;
		val.Flat = true;
		((Control)val).CustomMinimumSize = new Vector2(96f, 96f);
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((GodotObject)val).SetMeta(StringName.op_Implicit("meleeattack_page_id"), Variant.op_Implicit(pageId));
		ApplyNavButtonStyle(val, selected: false);
		((GodotObject)val).Connect(StringName.op_Implicit("pressed"), Callable.From((Action)delegate
		{
			OnNavButtonPressed(pageId);
		}), 0u);
		((Node)sidebar).AddChild((Node)(object)val, false, (InternalMode)0);
		return val;
	}

	private static void OnNavButtonPressed(string pageId)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (_panel != null)
		{
			((GodotObject)_panel).SetMeta(StringName.op_Implicit("meleeattack_selected_page"), Variant.op_Implicit(pageId));
		}
		RefreshAll();
	}

	private static string GetSelectedPage()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (_panel == null)
		{
			return "character";
		}
		if (((GodotObject)_panel).HasMeta(StringName.op_Implicit("meleeattack_selected_page")))
		{
			NSettingsPanel panel = _panel;
			StringName obj = StringName.op_Implicit("meleeattack_selected_page");
			Variant val = default(Variant);
			val = ((GodotObject)panel).GetMeta(obj, val);
			return ((Variant)(ref val)).AsString();
		}
		return "character";
	}

	private static void RefreshAll()
	{
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		string selectedPage = GetSelectedPage();
		if (_characterPage != null)
		{
			((CanvasItem)_characterPage).Visible = selectedPage == "character";
		}
		if (_skillPage != null)
		{
			((CanvasItem)_skillPage).Visible = selectedPage == "skill";
		}
		if (_characterNavBtn != null)
		{
			((BaseButton)_characterNavBtn).ButtonPressed = selectedPage == "character";
			ApplyNavButtonStyle(_characterNavBtn, selectedPage == "character");
		}
		if (_skillNavBtn != null)
		{
			((BaseButton)_skillNavBtn).ButtonPressed = selectedPage == "skill";
			ApplyNavButtonStyle(_skillNavBtn, selectedPage == "skill");
		}
		Callable val = Callable.From((Action)delegate
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			Callable val2 = Callable.From((Action)RecalcPanelHeight);
			((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private static void FillCharacterPage(VBoxContainer page)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Expected O, but got Unknown
		//IL_052a: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label();
		val.Text = "关闭后该角色将使用原始攻击动画（不位移），滑条决定角色攻击停留时间。\n怪物开关控制所有怪物的位移总开关。\n第三方mod开关控制除游戏本体的mod角色。";
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 14);
		((CanvasItem)val).Modulate = DimText;
		((Node)page).AddChild((Node)(object)val, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton) tuple = CreateGlobalToggleRow("怪物(Monster)", "meleeattack_monster_global");
		_monsterGlobalToggle = tuple.Item2;
		((Node)page).AddChild((Node)(object)tuple.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		AddSectionHeader(page, "角色独立设置");
		(HBoxContainer, TextureButton, HSlider, Label) tuple2 = CreateCharacterRow("静默猎手 (Silent)", "meleeattack_silent", "meleeattack_delay_silent");
		_characterToggleButtons["meleeattack_silent"] = tuple2.Item2;
		_characterSliders["meleeattack_silent"] = tuple2.Item3;
		_sliderValueLabels["meleeattack_silent"] = tuple2.Item4;
		((Node)page).AddChild((Node)(object)tuple2.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton, HSlider, Label) tuple3 = CreateCharacterRow("铁甲战士 (Ironclad)", "meleeattack_ironclad", "meleeattack_delay_ironclad");
		_characterToggleButtons["meleeattack_ironclad"] = tuple3.Item2;
		_characterSliders["meleeattack_ironclad"] = tuple3.Item3;
		_sliderValueLabels["meleeattack_ironclad"] = tuple3.Item4;
		((Node)page).AddChild((Node)(object)tuple3.Item1, false, (InternalMode)0);
		HBoxContainer val2 = CreateHeavyRow();
		((Node)page).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton, HSlider, Label) tuple4 = CreateCharacterRow("故障机器人 (Defect)", "meleeattack_defect", "meleeattack_delay_defect");
		_characterToggleButtons["meleeattack_defect"] = tuple4.Item2;
		_characterSliders["meleeattack_defect"] = tuple4.Item3;
		_sliderValueLabels["meleeattack_defect"] = tuple4.Item4;
		((Node)page).AddChild((Node)(object)tuple4.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton, HSlider, Label) tuple5 = CreateCharacterRow("亡灵契约师 (Necrobinder)", "meleeattack_necro", "meleeattack_delay_necro");
		_characterToggleButtons["meleeattack_necro"] = tuple5.Item2;
		_characterSliders["meleeattack_necro"] = tuple5.Item3;
		_sliderValueLabels["meleeattack_necro"] = tuple5.Item4;
		((Node)page).AddChild((Node)(object)tuple5.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton, HSlider, Label) tuple6 = CreateCharacterRow("储君 (Regent)", "meleeattack_regent", "meleeattack_delay_regent");
		_characterToggleButtons["meleeattack_regent"] = tuple6.Item2;
		_characterSliders["meleeattack_regent"] = tuple6.Item3;
		_sliderValueLabels["meleeattack_regent"] = tuple6.Item4;
		((Node)page).AddChild((Node)(object)tuple6.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton, HSlider, Label) tuple7 = CreateCharacterRow("奥斯提 (Osty)", "meleeattack_osty", "meleeattack_delay_osty");
		_characterToggleButtons["meleeattack_osty"] = tuple7.Item2;
		_characterSliders["meleeattack_osty"] = tuple7.Item3;
		_sliderValueLabels["meleeattack_osty"] = tuple7.Item4;
		((Node)page).AddChild((Node)(object)tuple7.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton, HSlider, Label) tuple8 = CreateCharacterRow("异鸟 (Byrd)", "meleeattack_byrd", "meleeattack_delay_byrd", 1f);
		_characterToggleButtons["meleeattack_byrd"] = tuple8.Item2;
		_characterSliders["meleeattack_byrd"] = tuple8.Item3;
		_sliderValueLabels["meleeattack_byrd"] = tuple8.Item4;
		((Node)page).AddChild((Node)(object)tuple8.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton, HSlider, Label) tuple9 = CreateOtherCharacterRow("第三方mod角色(third-party mod character)", "meleeattack_global_teleport", "meleeattack_delay_other");
		_otherCharacterToggle = tuple9.Item2;
		_characterSliders["meleeattack_global_teleport"] = tuple9.Item3;
		_sliderValueLabels["meleeattack_global_teleport"] = tuple9.Item4;
		((Node)page).AddChild((Node)(object)tuple9.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		(HBoxContainer, TextureButton) tuple10 = CreateGlobalToggleRow("极速模式 (Fast Mode)", "meleeattack_fast_mode");
		_characterToggleButtons["meleeattack_fast_mode"] = tuple10.Item2;
		((Node)page).AddChild((Node)(object)tuple10.Item1, false, (InternalMode)0);
		Label val3 = new Label();
		val3.Text = "开启后：加速攻击动作。";
		((Control)val3).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 14);
		((CanvasItem)val3).Modulate = DimText;
		((Node)page).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
	}

	private static HBoxContainer CreateHeavyRow()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Expected O, but got Unknown
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Expected O, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		HBoxContainer val = new HBoxContainer();
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)4;
		((Control)val).CustomMinimumSize = new Vector2(0f, 44f);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		((BoxContainer)val).Alignment = (AlignmentMode)1;
		Label val2 = new Label();
		val2.Text = "    └ 重击（跳劈） (Heavy Attack / Leap Strike)";
		((Control)val2).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val2).SizeFlagsVertical = (SizeFlags)4;
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 15);
		((CanvasItem)val2).Modulate = TextColor;
		((Control)val2).CustomMinimumSize = new Vector2(180f, 0f);
		val2.VerticalAlignment = (VerticalAlignment)1;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		HBoxContainer val3 = new HBoxContainer();
		((Control)val3).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val3).SizeFlagsVertical = (SizeFlags)4;
		((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
		((BoxContainer)val3).Alignment = (AlignmentMode)1;
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		HSlider slider = new HSlider();
		((Range)slider).MinValue = 0.0;
		((Range)slider).MaxValue = 1.0;
		((Range)slider).Step = 0.05000000074505806;
		((Range)slider).Value = 0.3499999940395355;
		((Control)slider).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)slider).SizeFlagsVertical = (SizeFlags)4;
		((Control)slider).CustomMinimumSize = new Vector2(120f, 0f);
		((Range)slider).ValueChanged += (ValueChangedEventHandler)delegate(double value)
		{
			float num = (float)Math.Round(value / 0.05000000074505806) * 0.05f;
			((Range)slider).Value = num;
			SetIroncladHeavyDelay(num);
			if (_sliderValueLabels.TryGetValue("meleeattack_ironclad_heavy", out var value2))
			{
				value2.Text = num.ToString("F2");
			}
		};
		((Node)val3).AddChild((Node)(object)slider, false, (InternalMode)0);
		Label val4 = new Label();
		val4.Text = 0.35f.ToString("F2");
		((Control)val4).SizeFlagsVertical = (SizeFlags)4;
		((Control)val4).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 14);
		((CanvasItem)val4).Modulate = CreamGold;
		((Control)val4).CustomMinimumSize = new Vector2(45f, 0f);
		val4.VerticalAlignment = (VerticalAlignment)1;
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		TextureButton val5 = CreateToggleButton("meleeattack_ironclad_heavy", defaultState: true);
		((Control)val5).SizeFlagsHorizontal = (SizeFlags)4;
		((Control)val5).SizeFlagsVertical = (SizeFlags)4;
		((Control)val5).CustomMinimumSize = new Vector2(36f, 36f);
		((Node)val).AddChild((Node)(object)val5, false, (InternalMode)0);
		_characterToggleButtons["meleeattack_ironclad_heavy"] = val5;
		_characterSliders["meleeattack_ironclad_heavy"] = slider;
		_sliderValueLabels["meleeattack_ironclad_heavy"] = val4;
		return val;
	}

	private static void FillSkillPage(VBoxContainer page)
	{
		(HBoxContainer, TextureButton) tuple = CreateGlobalToggleRow("全局技能开关 (Global Skill Toggle)", "meleeattack_global_skills");
		_globalSkillsToggle = tuple.Item2;
		((Node)page).AddChild((Node)(object)tuple.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSpacer(8f), false, (InternalMode)0);
		AddSkillCategory(page, "静默猎手 (Silent)", new(string, string)[11]
		{
			("子弹时间 (Bullet Time)", "meleeattack_skill_bullet_time"),
			("刀刃陷阱 (Knife Trap)", "meleeattack_skill_knife_trap"),
			("后空翻 (Backflip)", "meleeattack_skill_backflip"),
			("华丽收场 (Grand Finale)", "meleeattack_skill_grand_finale"),
			("幽魂形态 (Wraith Form)", "meleeattack_skill_wraith_form"),
			("余像 (Afterimage)", "meleeattack_skill_afterimage"),
			("切割 (Slice)", "meleeattack_skill_slice"),
			("精准瞄准 (Pinpoint)", "meleeattack_skill_pinpoint"),
			("残影 (Blur)", "meleeattack_skill_blur"),
			("爆发 (Burst)", "meleeattack_skill_burst"),
			("谋杀 (Murder)", "meleeattack_skill_murder")
		});
		AddSkillCategory(page, "铁甲战士 (Ironclad)", new(string, string)[7]
		{
			("恶魔形态 (Demon Form)", "meleeattack_skill_demon_form"),
			("全身撞击 (Body Slam)", "meleeattack_skill_body_slam"),
			("旋风斩 (Whirlwind)", "meleeattack_skill_whirlwind"),
			("上勾拳 (Uppercut)", "meleeattack_skill_uppercut"),
			("放血 (Bloodletting)", "meleeattack_skill_bloodletting"),
			("焚烧 (Conflagration)", "meleeattack_skill_conflagration"),
			("重锤 (Bludgeon)", "meleeattack_skill_bludgeon")
		});
		AddSkillCategory(page, "故障机器人 (Defect)", new(string, string)[2]
		{
			("偏差认知 (Biased Cognition)", "meleeattack_skill_biased_cognition"),
			("雷霆/雷暴 (Thunder / Storm)", "meleeattack_skill_thunder_storm")
		});
		AddSkillCategory(page, "亡灵契约师 (Necrobinder)", new(string, string)[1] { ("死神形态 (Reaper Form)", "meleeattack_skill_reaper_form") });
		AddSkillCategory(page, "buff", new(string, string)[1] { ("无实体 (Intangible)", "meleeattack_skill_intangible") });
		(HBoxContainer, TextureButton) tuple2 = CreateZoomEffectRow();
		_skillToggleButtons["meleeattack_skill_zoom_effect"] = tuple2.Item2;
		((Node)page).AddChild((Node)(object)tuple2.Item1, false, (InternalMode)0);
		((Node)page).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
	}

	private static void AddSkillCategory(VBoxContainer parent, string title, (string name, string key)[] skills)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		AddCategoryHeader(parent, title);
		if (skills.Length != 0)
		{
			GridContainer val = new GridContainer();
			((Node)val).Name = StringName.op_Implicit("SkillGrid_" + Math.Abs(title.GetHashCode()));
			val.Columns = 2;
			((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("h_separation"), 40);
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("v_separation"), 12);
			((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
			for (int i = 0; i < skills.Length; i++)
			{
				(string name, string key) tuple = skills[i];
				string item = tuple.name;
				string item2 = tuple.key;
				(HBoxContainer, TextureButton) tuple2 = CreateSkillRow(item, item2);
				_skillToggleButtons[item2] = tuple2.Item2;
				((Node)val).AddChild((Node)(object)tuple2.Item1, false, (InternalMode)0);
			}
		}
		((Node)parent).AddChild((Node)(object)CreateSpacer(10f), false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)CreateSeparator(), false, (InternalMode)0);
	}

	private static (HBoxContainer Row, TextureButton Toggle, HSlider Slider, Label ValueLabel) CreateCharacterRow(string displayName, string toggleKey, string delayKey, float defaultDelay = 0.4f)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Expected O, but got Unknown
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Expected O, but got Unknown
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		HBoxContainer val = new HBoxContainer();
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)4;
		((Control)val).CustomMinimumSize = new Vector2(0f, 44f);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		((BoxContainer)val).Alignment = (AlignmentMode)1;
		Label val2 = new Label();
		val2.Text = displayName;
		((Control)val2).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val2).SizeFlagsVertical = (SizeFlags)4;
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 15);
		((CanvasItem)val2).Modulate = TextColor;
		((Control)val2).CustomMinimumSize = new Vector2(180f, 0f);
		val2.VerticalAlignment = (VerticalAlignment)1;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		HBoxContainer val3 = new HBoxContainer();
		((Control)val3).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val3).SizeFlagsVertical = (SizeFlags)4;
		((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
		((BoxContainer)val3).Alignment = (AlignmentMode)1;
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		HSlider slider = new HSlider();
		((Range)slider).MinValue = 0.10000000149011612;
		((Range)slider).MaxValue = 1.0;
		((Range)slider).Step = 0.10000000149011612;
		((Range)slider).Value = defaultDelay;
		((Control)slider).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)slider).SizeFlagsVertical = (SizeFlags)4;
		((Control)slider).CustomMinimumSize = new Vector2(120f, 0f);
		((Range)slider).ValueChanged += (ValueChangedEventHandler)delegate(double value)
		{
			float num = (float)Math.Round(value / 0.10000000149011612) * 0.1f;
			((Range)slider).Value = num;
			SetDelay(delayKey, num);
			if (_sliderValueLabels.TryGetValue(toggleKey, out var value2))
			{
				value2.Text = num.ToString("F2");
			}
		};
		((Node)val3).AddChild((Node)(object)slider, false, (InternalMode)0);
		Label val4 = new Label();
		val4.Text = defaultDelay.ToString("F2");
		((Control)val4).SizeFlagsVertical = (SizeFlags)4;
		((Control)val4).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 14);
		((CanvasItem)val4).Modulate = CreamGold;
		((Control)val4).CustomMinimumSize = new Vector2(45f, 0f);
		val4.VerticalAlignment = (VerticalAlignment)1;
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		TextureButton val5 = CreateToggleButton(toggleKey, defaultState: true);
		((Control)val5).SizeFlagsHorizontal = (SizeFlags)4;
		((Control)val5).SizeFlagsVertical = (SizeFlags)4;
		((Control)val5).CustomMinimumSize = new Vector2(36f, 36f);
		((Node)val).AddChild((Node)(object)val5, false, (InternalMode)0);
		return (Row: val, Toggle: val5, Slider: slider, ValueLabel: val4);
	}

	private static (HBoxContainer Row, TextureButton Toggle, HSlider Slider, Label ValueLabel) CreateOtherCharacterRow(string displayName, string toggleKey, string delayKey)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Expected O, but got Unknown
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Expected O, but got Unknown
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		HBoxContainer val = new HBoxContainer();
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)4;
		((Control)val).CustomMinimumSize = new Vector2(0f, 44f);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		((BoxContainer)val).Alignment = (AlignmentMode)1;
		Label val2 = new Label();
		val2.Text = displayName;
		((Control)val2).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val2).SizeFlagsVertical = (SizeFlags)4;
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 17);
		((CanvasItem)val2).Modulate = CreamGold;
		((Control)val2).CustomMinimumSize = new Vector2(180f, 0f);
		val2.VerticalAlignment = (VerticalAlignment)1;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		HBoxContainer val3 = new HBoxContainer();
		((Control)val3).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val3).SizeFlagsVertical = (SizeFlags)4;
		((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
		((BoxContainer)val3).Alignment = (AlignmentMode)1;
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		HSlider slider = new HSlider();
		((Range)slider).MinValue = 0.10000000149011612;
		((Range)slider).MaxValue = 1.0;
		((Range)slider).Step = 0.10000000149011612;
		((Range)slider).Value = 0.4000000059604645;
		((Control)slider).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)slider).SizeFlagsVertical = (SizeFlags)4;
		((Control)slider).CustomMinimumSize = new Vector2(120f, 0f);
		((Range)slider).ValueChanged += (ValueChangedEventHandler)delegate(double value)
		{
			float num = (float)Math.Round(value / 0.10000000149011612) * 0.1f;
			((Range)slider).Value = num;
			SetDelay(delayKey, num);
			if (_sliderValueLabels.TryGetValue(toggleKey, out var value2))
			{
				value2.Text = num.ToString("F2");
			}
		};
		((Node)val3).AddChild((Node)(object)slider, false, (InternalMode)0);
		Label val4 = new Label();
		val4.Text = 0.4f.ToString("F2");
		((Control)val4).SizeFlagsVertical = (SizeFlags)4;
		((Control)val4).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 14);
		((CanvasItem)val4).Modulate = CreamGold;
		((Control)val4).CustomMinimumSize = new Vector2(45f, 0f);
		val4.VerticalAlignment = (VerticalAlignment)1;
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		TextureButton val5 = CreateToggleButton(toggleKey, defaultState: true);
		((Control)val5).SizeFlagsHorizontal = (SizeFlags)4;
		((Control)val5).SizeFlagsVertical = (SizeFlags)4;
		((Control)val5).CustomMinimumSize = new Vector2(36f, 36f);
		((Node)val).AddChild((Node)(object)val5, false, (InternalMode)0);
		return (Row: val, Toggle: val5, Slider: slider, ValueLabel: val4);
	}

	private static (HBoxContainer Row, TextureButton Toggle) CreateGlobalToggleRow(string label, string toggleKey)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		HBoxContainer val = new HBoxContainer();
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)4;
		((Control)val).CustomMinimumSize = new Vector2(0f, 44f);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		((BoxContainer)val).Alignment = (AlignmentMode)1;
		Label val2 = new Label();
		val2.Text = label;
		((Control)val2).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val2).SizeFlagsVertical = (SizeFlags)4;
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 17);
		((CanvasItem)val2).Modulate = CreamGold;
		((Control)val2).CustomMinimumSize = new Vector2(180f, 0f);
		val2.VerticalAlignment = (VerticalAlignment)1;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		TextureButton val3 = CreateToggleButton(toggleKey, defaultState: true);
		((Control)val3).SizeFlagsHorizontal = (SizeFlags)4;
		((Control)val3).SizeFlagsVertical = (SizeFlags)4;
		((Control)val3).CustomMinimumSize = new Vector2(36f, 36f);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		return (Row: val, Toggle: val3);
	}

	private static (HBoxContainer Row, TextureButton Toggle) CreateSkillRow(string displayName, string toggleKey)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		HBoxContainer val = new HBoxContainer();
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)4;
		((Control)val).CustomMinimumSize = new Vector2(0f, 42f);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
		Label val2 = new Label();
		val2.Text = displayName;
		((Control)val2).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val2).SizeFlagsVertical = (SizeFlags)4;
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 14);
		((CanvasItem)val2).Modulate = TextColor;
		((Control)val2).CustomMinimumSize = new Vector2(120f, 0f);
		val2.VerticalAlignment = (VerticalAlignment)1;
		val2.TextOverrunBehavior = (OverrunBehavior)3;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		TextureButton val3 = CreateToggleButton(toggleKey, defaultState: true);
		((Control)val3).SizeFlagsHorizontal = (SizeFlags)8;
		((Control)val3).SizeFlagsVertical = (SizeFlags)4;
		((Control)val3).CustomMinimumSize = new Vector2(32f, 32f);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		return (Row: val, Toggle: val3);
	}

	private static (HBoxContainer Row, TextureButton Toggle) CreateZoomEffectRow()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		HBoxContainer val = new HBoxContainer();
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val).SizeFlagsVertical = (SizeFlags)4;
		((Control)val).CustomMinimumSize = new Vector2(0f, 48f);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
		((BoxContainer)val).Alignment = (AlignmentMode)1;
		Label val2 = new Label();
		val2.Text = "高伤特写 (Close-up Shot)";
		((Control)val2).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)val2).SizeFlagsVertical = (SizeFlags)4;
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
		((CanvasItem)val2).Modulate = CreamGold;
		((Control)val2).CustomMinimumSize = new Vector2(180f, 0f);
		val2.VerticalAlignment = (VerticalAlignment)1;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		TextureButton val3 = CreateToggleButton("meleeattack_skill_zoom_effect", defaultState: true);
		((Control)val3).SizeFlagsHorizontal = (SizeFlags)4;
		((Control)val3).SizeFlagsVertical = (SizeFlags)4;
		((Control)val3).CustomMinimumSize = new Vector2(36f, 36f);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		return (Row: val, Toggle: val3);
	}

	private static void AddSectionHeader(VBoxContainer parent, string title)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label();
		val.Text = title;
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
		((CanvasItem)val).Modulate = CreamGold;
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		HSeparator val2 = new HSeparator();
		((Control)val2).CustomMinimumSize = new Vector2(0f, 4f);
		((Node)parent).AddChild((Node)(object)val2, false, (InternalMode)0);
	}

	private static void AddCategoryHeader(VBoxContainer parent, string title)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		((Node)parent).AddChild((Node)(object)CreateSpacer(12f), false, (InternalMode)0);
		Label val = new Label();
		val.Text = title;
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		((CanvasItem)val).Modulate = CreamGold;
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)CreateSpacer(6f), false, (InternalMode)0);
		HSeparator val2 = new HSeparator();
		((Control)val2).CustomMinimumSize = new Vector2(0f, 4f);
		((Node)parent).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)CreateSpacer(6f), false, (InternalMode)0);
	}

	private static HSeparator CreateSeparator()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		HSeparator val = new HSeparator();
		((Control)val).CustomMinimumSize = new Vector2(0f, 2f);
		StyleBoxFlat val2 = new StyleBoxFlat();
		val2.BgColor = RowSeparatorColor;
		((Control)val).AddThemeStyleboxOverride(StringName.op_Implicit("separator"), (StyleBox)(object)val2);
		return val;
	}

	private static Control CreateSpacer(float height)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		Control val = new Control();
		val.CustomMinimumSize = new Vector2(0f, height);
		val.SizeFlagsHorizontal = (SizeFlags)3;
		val.MouseFilter = (MouseFilterEnum)2;
		return val;
	}

	private static void ApplyNavButtonStyle(Button btn, bool selected)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		Color bgColor = (selected ? NavSelectedBg : NavIdleBg);
		Color borderColor = (Color)(selected ? CreamGold : new Color(CreamGold.R, CreamGold.G, CreamGold.B, 0.24f));
		int num = ((!selected) ? 1 : 2);
		StyleBoxFlat val = new StyleBoxFlat();
		val.BgColor = bgColor;
		val.BorderColor = borderColor;
		val.BorderWidthLeft = num;
		val.BorderWidthTop = num;
		val.BorderWidthRight = num;
		val.BorderWidthBottom = num;
		val.CornerRadiusTopLeft = 6;
		val.CornerRadiusTopRight = 6;
		val.CornerRadiusBottomLeft = 6;
		val.CornerRadiusBottomRight = 6;
		((StyleBox)val).ContentMarginLeft = 8f;
		((StyleBox)val).ContentMarginRight = 8f;
		((StyleBox)val).ContentMarginTop = 8f;
		((StyleBox)val).ContentMarginBottom = 8f;
		((Control)btn).AddThemeStyleboxOverride(StringName.op_Implicit("normal"), (StyleBox)(object)val);
		((Control)btn).AddThemeStyleboxOverride(StringName.op_Implicit("hover"), (StyleBox)(object)val);
		((Control)btn).AddThemeStyleboxOverride(StringName.op_Implicit("pressed"), (StyleBox)(object)val);
		((Control)btn).AddThemeStyleboxOverride(StringName.op_Implicit("focus"), (StyleBox)(object)val);
		((CanvasItem)btn).Modulate = (Color)(selected ? Colors.White : new Color(0.7f, 0.7f, 0.7f, 1f));
	}

	private static float GetDefaultDelayFromConfig(string charId)
	{
		if (SimpleTeleportPatch._extraBufferByCharacter.TryGetValue(charId, out var value))
		{
			return value;
		}
		return 0.4f;
	}

	private static void EnsureCacheLoaded()
	{
		if (_configCache == null)
		{
			_configCache = LoadConfigFromFile();
			if (_configCache.Count == 0)
			{
				_configCache = new Dictionary<string, bool>
				{
					["meleeattack_monster_global"] = false,
					["meleeattack_global_teleport"] = true,
					["meleeattack_silent"] = true,
					["meleeattack_ironclad"] = true,
					["meleeattack_defect"] = true,
					["meleeattack_necro"] = true,
					["meleeattack_regent"] = false,
					["meleeattack_osty"] = true,
					["meleeattack_byrd"] = true,
					["meleeattack_ironclad_heavy"] = true,
					["meleeattack_global_skills"] = true,
					["meleeattack_skill_bullet_time"] = true,
					["meleeattack_skill_knife_trap"] = true,
					["meleeattack_skill_backflip"] = true,
					["meleeattack_skill_grand_finale"] = true,
					["meleeattack_skill_wraith_form"] = true,
					["meleeattack_skill_afterimage"] = true,
					["meleeattack_skill_slice"] = true,
					["meleeattack_skill_pinpoint"] = true,
					["meleeattack_skill_blur"] = true,
					["meleeattack_skill_burst"] = true,
					["meleeattack_skill_murder"] = true,
					["meleeattack_skill_demon_form"] = true,
					["meleeattack_skill_body_slam"] = true,
					["meleeattack_skill_whirlwind"] = true,
					["meleeattack_skill_uppercut"] = true,
					["meleeattack_skill_bloodletting"] = true,
					["meleeattack_skill_conflagration"] = true,
					["meleeattack_skill_bludgeon"] = true,
					["meleeattack_skill_biased_cognition"] = true,
					["meleeattack_skill_thunder_storm"] = true,
					["meleeattack_skill_reaper_form"] = true,
					["meleeattack_skill_intangible"] = true,
					["meleeattack_skill_zoom_effect"] = true,
					["meleeattack_fast_mode"] = false
				};
				SaveConfigToFile(_configCache);
			}
			_delayCache = LoadDelayConfigFromFile();
			if (_delayCache.Count == 0)
			{
				_delayCache = new Dictionary<string, float>
				{
					["meleeattack_delay_silent"] = GetDefaultDelayFromConfig("Silent"),
					["meleeattack_delay_ironclad"] = GetDefaultDelayFromConfig("Ironclad"),
					["meleeattack_delay_defect"] = GetDefaultDelayFromConfig("Defect"),
					["meleeattack_delay_necro"] = GetDefaultDelayFromConfig("Necrobinder"),
					["meleeattack_delay_regent"] = GetDefaultDelayFromConfig("Regent"),
					["meleeattack_delay_osty"] = GetDefaultDelayFromConfig("Osty"),
					["meleeattack_delay_byrd"] = GetDefaultDelayFromConfig("Byrd"),
					["meleeattack_delay_other"] = 0.4f,
					["meleeattack_delay_ironclad_heavy"] = 0.35f
				};
				SaveDelayConfigToFile(_delayCache);
			}
		}
	}

	private static Dictionary<string, bool> LoadConfigFromFile()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I8
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Invalid comparison between Unknown and I8
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Invalid comparison between Unknown and I8
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Invalid comparison between Unknown and I8
		try
		{
			if (!FileAccess.FileExists(ConfigFilePath))
			{
				return new Dictionary<string, bool>();
			}
			FileAccess val = FileAccess.Open(ConfigFilePath, (ModeFlags)1);
			try
			{
				if (val == null)
				{
					return new Dictionary<string, bool>();
				}
				string asText = val.GetAsText(false);
				Variant val2 = Json.ParseString(asText);
				if ((long)((Variant)(ref val2)).VariantType == 27)
				{
					Dictionary val3 = ((Variant)(ref val2)).AsGodotDictionary();
					Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
					foreach (Variant key2 in val3.Keys)
					{
						Variant current = key2;
						string key = ((Variant)(ref current)).AsString();
						Variant val4 = val3[current];
						if ((long)((Variant)(ref val4)).VariantType == 1)
						{
							dictionary[key] = ((Variant)(ref val4)).AsBool();
						}
						else if ((long)((Variant)(ref val4)).VariantType == 2)
						{
							dictionary[key] = ((Variant)(ref val4)).AsInt32() != 0;
						}
						else if ((long)((Variant)(ref val4)).VariantType == 3)
						{
							dictionary[key] = ((Variant)(ref val4)).AsSingle() != 0f;
						}
					}
					return dictionary;
				}
				return new Dictionary<string, bool>();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[MeleeAttack] 加载配置失败: " + ex.Message);
			return new Dictionary<string, bool>();
		}
	}

	private static void SaveConfigToFile(Dictionary<string, bool> data)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Dictionary val = new Dictionary();
			foreach (KeyValuePair<string, bool> datum in data)
			{
				val[Variant.op_Implicit(datum.Key)] = Variant.op_Implicit(datum.Value);
			}
			if (_delayCache != null)
			{
				foreach (KeyValuePair<string, float> item in _delayCache)
				{
					val[Variant.op_Implicit(item.Key)] = Variant.op_Implicit(item.Value);
				}
			}
			string text = Json.Stringify(Variant.op_Implicit(val), "", true, false);
			FileAccess val2 = FileAccess.Open(ConfigFilePath, (ModeFlags)2);
			try
			{
				if (val2 != null)
				{
					val2.StoreString(text);
					val2.Flush();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[MeleeAttack] 保存配置失败: " + ex.Message);
		}
	}

	private static Dictionary<string, float> LoadDelayConfigFromFile()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Invalid comparison between Unknown and I8
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Invalid comparison between Unknown and I8
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Invalid comparison between Unknown and I8
		try
		{
			if (!FileAccess.FileExists(ConfigFilePath))
			{
				return new Dictionary<string, float>();
			}
			FileAccess val = FileAccess.Open(ConfigFilePath, (ModeFlags)1);
			try
			{
				if (val == null)
				{
					return new Dictionary<string, float>();
				}
				string asText = val.GetAsText(false);
				Variant val2 = Json.ParseString(asText);
				if ((long)((Variant)(ref val2)).VariantType == 27)
				{
					Dictionary val3 = ((Variant)(ref val2)).AsGodotDictionary();
					Dictionary<string, float> dictionary = new Dictionary<string, float>();
					foreach (Variant key in val3.Keys)
					{
						Variant current = key;
						string text = ((Variant)(ref current)).AsString();
						if (text.StartsWith("meleeattack_delay_"))
						{
							Variant val4 = val3[current];
							if ((long)((Variant)(ref val4)).VariantType == 3 || (long)((Variant)(ref val4)).VariantType == 2)
							{
								dictionary[text] = (float)((Variant)(ref val4)).AsDouble();
							}
						}
					}
					return dictionary;
				}
				return new Dictionary<string, float>();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
			return new Dictionary<string, float>();
		}
	}

	private static void SaveDelayConfigToFile(Dictionary<string, float> data)
	{
		if (_configCache == null)
		{
			EnsureCacheLoaded();
		}
		SaveConfigToFile(_configCache);
	}

	private static bool GetConfig(string key, bool defaultValue)
	{
		EnsureCacheLoaded();
		if (_configCache.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	private static void SetConfig(string key, bool value)
	{
		EnsureCacheLoaded();
		_configCache[key] = value;
		SaveConfigToFile(_configCache);
	}

	private static float GetDelay(string key, float defaultValue)
	{
		EnsureCacheLoaded();
		if (_delayCache.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	private static void SetDelay(string key, float value)
	{
		EnsureCacheLoaded();
		_delayCache[key] = value;
		SaveConfigToFile(_configCache);
	}

	public static void LoadAndApplyConfig()
	{
		EnsureCacheLoaded();
		if (_monsterGlobalToggle != null)
		{
			((BaseButton)_monsterGlobalToggle).ButtonPressed = _configCache.GetValueOrDefault("meleeattack_monster_global", defaultValue: false);
			UpdateToggleButtonVisual(_monsterGlobalToggle);
		}
		if (_otherCharacterToggle != null)
		{
			((BaseButton)_otherCharacterToggle).ButtonPressed = _configCache.GetValueOrDefault("meleeattack_global_teleport", defaultValue: true);
			UpdateToggleButtonVisual(_otherCharacterToggle);
		}
		if (_globalSkillsToggle != null)
		{
			((BaseButton)_globalSkillsToggle).ButtonPressed = _configCache.GetValueOrDefault("meleeattack_global_skills", defaultValue: true);
			UpdateToggleButtonVisual(_globalSkillsToggle);
		}
		foreach (KeyValuePair<string, TextureButton> characterToggleButton in _characterToggleButtons)
		{
			string key = characterToggleButton.Key;
			TextureButton value = characterToggleButton.Value;
			if (1 == 0)
			{
			}
			bool flag = !(key == "meleeattack_regent") && !(key == "meleeattack_fast_mode");
			if (1 == 0)
			{
			}
			bool defaultValue = flag;
			((BaseButton)value).ButtonPressed = _configCache.GetValueOrDefault(key, defaultValue);
			UpdateToggleButtonVisual(value);
		}
		foreach (KeyValuePair<string, HSlider> characterSlider in _characterSliders)
		{
			string key2 = characterSlider.Key;
			HSlider value2 = characterSlider.Value;
			float num = CollectionExtensions.GetValueOrDefault(key: (!(key2 == "meleeattack_global_teleport")) ? ("meleeattack_delay_" + key2.Substring("meleeattack_".Length)) : "meleeattack_delay_other", dictionary: _delayCache, defaultValue: 0.4f);
			((Range)value2).Value = num;
			if (_sliderValueLabels.TryGetValue(key2, out var value3))
			{
				value3.Text = num.ToString("F2");
			}
		}
		foreach (KeyValuePair<string, TextureButton> skillToggleButton in _skillToggleButtons)
		{
			string key4 = skillToggleButton.Key;
			TextureButton value4 = skillToggleButton.Value;
			bool config = GetConfig(key4, defaultValue: true);
			((BaseButton)value4).ButtonPressed = config;
			UpdateToggleButtonVisual(value4);
		}
	}

	private static void EnsureTickboxScene()
	{
		if (_tickboxScene == null)
		{
			_tickboxScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/tickbox.tscn", (string)null, (CacheMode)1);
			if (_tickboxScene == null)
			{
				GD.PrintErr("[MeleeAttack] 无法加载 tickbox.tscn，请检查路径。");
			}
		}
	}

	private static void UpdateToggleButtonVisual(TextureButton btn)
	{
		if (btn == null || ((Node)btn).GetChildCount(false) == 0)
		{
			return;
		}
		Node child = ((Node)btn).GetChild(0, false);
		Control val = (Control)(object)((child is Control) ? child : null);
		if (val != null)
		{
			TextureRect nodeOrNull = ((Node)val).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("Ticked"));
			TextureRect nodeOrNull2 = ((Node)val).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("NotTicked"));
			if (nodeOrNull != null)
			{
				((CanvasItem)nodeOrNull).Visible = ((BaseButton)btn).ButtonPressed;
			}
			if (nodeOrNull2 != null)
			{
				((CanvasItem)nodeOrNull2).Visible = !((BaseButton)btn).ButtonPressed;
			}
		}
	}

	private static TextureButton CreateToggleButton(string key, bool defaultState)
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		EnsureTickboxScene();
		if (_tickboxScene == null)
		{
			TextureButton val = new TextureButton();
			((Node)val).Name = StringName.op_Implicit(key + "Toggle");
			((BaseButton)val).ToggleMode = true;
			((Control)val).CustomMinimumSize = new Vector2(40f, 40f);
			((BaseButton)val).Toggled += (ToggledEventHandler)delegate(bool pressed)
			{
				SetConfig(key, pressed);
			};
			return val;
		}
		Control val2 = (Control)_tickboxScene.Instantiate((GenEditState)0);
		val2.AnchorsPreset = 15;
		val2.SizeFlagsHorizontal = (SizeFlags)3;
		val2.SizeFlagsVertical = (SizeFlags)3;
		TextureButton btn = new TextureButton();
		((Node)btn).Name = StringName.op_Implicit(key + "Toggle");
		((BaseButton)btn).ToggleMode = true;
		((Control)btn).CustomMinimumSize = new Vector2(40f, 40f);
		((Node)btn).AddChild((Node)(object)val2, false, (InternalMode)0);
		UpdateToggleButtonVisual(btn);
		((BaseButton)btn).Toggled += (ToggledEventHandler)delegate(bool pressed)
		{
			UpdateToggleButtonVisual(btn);
			SetConfig(key, pressed);
		};
		return btn;
	}

	public static bool IsMonsterGlobalEnabled()
	{
		return GetConfig("meleeattack_monster_global", defaultValue: false);
	}

	public static void SetMonsterGlobalEnabled(bool value)
	{
		SetConfig("meleeattack_monster_global", value);
	}

	public static bool IsOtherCharacterEnabled()
	{
		return GetConfig("meleeattack_global_teleport", defaultValue: true);
	}

	public static void SetOtherCharacterEnabled(bool value)
	{
		SetConfig("meleeattack_global_teleport", value);
	}

	public static bool IsSilentEnabled()
	{
		return GetConfig("meleeattack_silent", defaultValue: true);
	}

	public static bool IsIroncladEnabled()
	{
		return GetConfig("meleeattack_ironclad", defaultValue: true);
	}

	public static bool IsDefectEnabled()
	{
		return GetConfig("meleeattack_defect", defaultValue: true);
	}

	public static bool IsNecrobinderEnabled()
	{
		return GetConfig("meleeattack_necro", defaultValue: true);
	}

	public static bool IsRegentEnabled()
	{
		return GetConfig("meleeattack_regent", defaultValue: false);
	}

	public static bool IsOstyEnabled()
	{
		return GetConfig("meleeattack_osty", defaultValue: true);
	}

	public static bool IsByrdEnabled()
	{
		return GetConfig("meleeattack_byrd", defaultValue: true);
	}

	public static bool IsIroncladHeavyEnabled()
	{
		if (!IsIroncladEnabled())
		{
			return false;
		}
		return GetConfig("meleeattack_ironclad_heavy", defaultValue: true);
	}

	public static void SetIroncladHeavyEnabled(bool value)
	{
		SetConfig("meleeattack_ironclad_heavy", value);
	}

	public static float GetIroncladHeavyDelay()
	{
		return GetDelay("meleeattack_delay_ironclad_heavy", 0.35f);
	}

	public static void SetIroncladHeavyDelay(float value)
	{
		SetDelay("meleeattack_delay_ironclad_heavy", value);
	}

	public static bool IsTeleportEnabledForCharacter(string charId)
	{
		if (string.IsNullOrEmpty(charId))
		{
			return true;
		}
		if (_mainCharacters.Contains(charId))
		{
			if (string.Equals(charId, "Silent", StringComparison.OrdinalIgnoreCase))
			{
				return IsSilentEnabled();
			}
			if (string.Equals(charId, "Ironclad", StringComparison.OrdinalIgnoreCase))
			{
				return IsIroncladEnabled();
			}
			if (string.Equals(charId, "Defect", StringComparison.OrdinalIgnoreCase))
			{
				return IsDefectEnabled();
			}
			if (string.Equals(charId, "Necrobinder", StringComparison.OrdinalIgnoreCase))
			{
				return IsNecrobinderEnabled();
			}
			if (string.Equals(charId, "Regent", StringComparison.OrdinalIgnoreCase))
			{
				return IsRegentEnabled();
			}
			if (string.Equals(charId, "Osty", StringComparison.OrdinalIgnoreCase))
			{
				return IsOstyEnabled();
			}
			if (string.Equals(charId, "Byrd", StringComparison.OrdinalIgnoreCase))
			{
				return IsByrdEnabled();
			}
			return true;
		}
		return IsOtherCharacterEnabled();
	}

	public static float GetDelayForCharacter(string charId)
	{
		if (string.IsNullOrEmpty(charId))
		{
			return 0.4f;
		}
		if (string.Equals(charId, "Silent", StringComparison.OrdinalIgnoreCase))
		{
			return GetDelay("meleeattack_delay_silent", 0.4f);
		}
		if (string.Equals(charId, "Ironclad", StringComparison.OrdinalIgnoreCase))
		{
			return GetDelay("meleeattack_delay_ironclad", 0.4f);
		}
		if (string.Equals(charId, "Defect", StringComparison.OrdinalIgnoreCase))
		{
			return GetDelay("meleeattack_delay_defect", 0.4f);
		}
		if (string.Equals(charId, "Necrobinder", StringComparison.OrdinalIgnoreCase))
		{
			return GetDelay("meleeattack_delay_necro", 0.4f);
		}
		if (string.Equals(charId, "Regent", StringComparison.OrdinalIgnoreCase))
		{
			return GetDelay("meleeattack_delay_regent", 0.4f);
		}
		if (string.Equals(charId, "Osty", StringComparison.OrdinalIgnoreCase))
		{
			return GetDelay("meleeattack_delay_osty", 0.4f);
		}
		if (string.Equals(charId, "Byrd", StringComparison.OrdinalIgnoreCase))
		{
			return GetDelay("meleeattack_delay_byrd", 0.4f);
		}
		return GetDelay("meleeattack_delay_other", 0.4f);
	}

	public static bool IsSkillEnabled(string skillKey)
	{
		if (!IsGlobalSkillsEnabled())
		{
			return false;
		}
		return GetConfig(skillKey, defaultValue: true);
	}

	public static void SetSkillEnabled(string skillKey, bool value)
	{
		SetConfig(skillKey, value);
	}

	public static bool IsGlobalSkillsEnabled()
	{
		return GetConfig("meleeattack_global_skills", defaultValue: true);
	}

	public static void SetGlobalSkillsEnabled(bool value)
	{
		SetConfig("meleeattack_global_skills", value);
	}

	public static bool IsBulletTimeEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_bullet_time");
	}

	public static bool IsKnifeTrapEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_knife_trap");
	}

	public static bool IsBackflipEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_backflip");
	}

	public static bool IsGrandFinaleEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_grand_finale");
	}

	public static bool IsWraithFormEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_wraith_form");
	}

	public static bool IsAfterimageEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_afterimage");
	}

	public static bool IsSliceEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_slice");
	}

	public static bool IsPinpointEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_pinpoint");
	}

	public static bool IsBlurEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_blur");
	}

	public static bool IsBurstEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_burst");
	}

	public static bool IsMurderEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_murder");
	}

	public static bool IsDemonFormEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_demon_form");
	}

	public static bool IsBloodlettingEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_bloodletting");
	}

	public static bool IsConflagrationEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_conflagration");
	}

	public static bool IsBludgeonEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_bludgeon");
	}

	public static bool IsUppercutEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_uppercut");
	}

	public static bool IsBodySlamEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_body_slam");
	}

	public static void SetBodySlamEnabled(bool value)
	{
		SetConfig("meleeattack_skill_body_slam", value);
	}

	public static bool IsWhirlwindEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_whirlwind");
	}

	public static void SetWhirlwindEnabled(bool value)
	{
		SetConfig("meleeattack_skill_whirlwind", value);
	}

	public static bool IsBiasedCognitionEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_biased_cognition");
	}

	public static bool IsThunderStormEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_thunder_storm");
	}

	public static bool IsReaperFormEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_reaper_form");
	}

	public static bool IsIntangibleEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_intangible");
	}

	public static bool IsZoomEffectEnabled()
	{
		return IsSkillEnabled("meleeattack_skill_zoom_effect");
	}

	public static void SetZoomEffectEnabled(bool value)
	{
		SetConfig("meleeattack_skill_zoom_effect", value);
	}

	public static bool IsFastModeEnabled()
	{
		return GetConfig("meleeattack_fast_mode", defaultValue: false);
	}

	public static void SetFastModeEnabled(bool value)
	{
		SetConfig("meleeattack_fast_mode", value);
	}

	public static void TryInject(NSettingsScreen settingsScreen)
	{
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Expected O, but got Unknown
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Expected O, but got Unknown
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		if (settingsScreen == null || !((Node)settingsScreen).IsInsideTree())
		{
			return;
		}
		NSettingsTabManager tabManager = ((Node)settingsScreen).GetNodeOrNull<NSettingsTabManager>(NodePath.op_Implicit("SettingsTabManager"));
		if (tabManager == null)
		{
			GD.PrintErr("[MeleeAttack] 无法找到 SettingsTabManager");
			return;
		}
		_tab = ((Node)tabManager).GetNodeOrNull<NSettingsTab>(NodePath.op_Implicit("MeleeAttackSettingsTab"));
		Callable val2;
		if (_tab == null)
		{
			PackedScene val = ResourceLoader.Load<PackedScene>("res://scenes/screens/settings_tab.tscn", (string)null, (CacheMode)1);
			if (val == null)
			{
				GD.PrintErr("[MeleeAttack] 无法加载 settings_tab.tscn");
				return;
			}
			_tab = val.Instantiate<NSettingsTab>((GenEditState)0);
			((Node)_tab).Name = StringName.op_Implicit("MeleeAttackSettingsTab");
			((Node)tabManager).AddChild((Node)(object)_tab, false, (InternalMode)0);
			val2 = Callable.From((Action)delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)_tab))
				{
					_tab.SetLabel("角色&技能\n(Characters & Skills）");
				}
			});
			((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
			_tabConnected = false;
		}
		Control nodeOrNull = ((Node)settingsScreen).GetNodeOrNull<Control>(NodePath.op_Implicit("ScrollContainer/Mask/Clipper"));
		if (nodeOrNull == null)
		{
			GD.PrintErr("[MeleeAttack] 无法找到 Clipper");
			return;
		}
		_panel = ((Node)nodeOrNull).GetNodeOrNull<NSettingsPanel>(NodePath.op_Implicit("MeleeAttackSettingsPanel"));
		if (_panel == null)
		{
			NSettingsPanel nodeOrNull2 = ((Node)nodeOrNull).GetNodeOrNull<NSettingsPanel>(NodePath.op_Implicit("SoundSettings"));
			if (nodeOrNull2 != null)
			{
				Node obj = ((Node)nodeOrNull2).Duplicate(15);
				_panel = (NSettingsPanel)(object)((obj is NSettingsPanel) ? obj : null);
			}
			else
			{
				_panel = new NSettingsPanel();
			}
			if (_panel == null)
			{
				GD.PrintErr("[MeleeAttack] 创建面板失败");
				return;
			}
			((Node)_panel).Name = StringName.op_Implicit("MeleeAttackSettingsPanel");
			((CanvasItem)_panel).Visible = true;
			((Control)_panel).CustomMinimumSize = new Vector2(1012f, 2000f);
			((Control)_panel).SizeFlagsHorizontal = (SizeFlags)3;
			((Control)_panel).SizeFlagsVertical = (SizeFlags)1;
			((Node)nodeOrNull).AddChild((Node)(object)_panel, false, (InternalMode)0);
			VBoxContainer val3 = ((Node)_panel).GetNodeOrNull<VBoxContainer>(NodePath.op_Implicit("VBoxContainer"));
			if (val3 != null)
			{
				foreach (Node child in ((Node)val3).GetChildren(false))
				{
					child.QueueFree();
				}
				((Control)val3).CustomMinimumSize = new Vector2(1012f, 0f);
				((Control)val3).SizeFlagsHorizontal = (SizeFlags)3;
				((Control)val3).SizeFlagsVertical = (SizeFlags)3;
				((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 0);
			}
			else
			{
				val3 = new VBoxContainer();
				((Node)val3).Name = StringName.op_Implicit("VBoxContainer");
				((Control)val3).CustomMinimumSize = new Vector2(1012f, 0f);
				((Control)val3).SizeFlagsHorizontal = (SizeFlags)3;
				((Control)val3).SizeFlagsVertical = (SizeFlags)3;
				((Node)_panel).AddChild((Node)(object)val3, false, (InternalMode)0);
			}
			_settingsVBox = val3;
			BuildSettingsUI(val3);
			val2 = Callable.From((Action)delegate
			{
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				Callable val4 = Callable.From((Action)RecalcPanelHeight);
				((Callable)(ref val4)).CallDeferred(Array.Empty<Variant>());
			});
			((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
		}
		BindTabToPanel(tabManager, _tab, _panel);
		if (!_tabConnected)
		{
			((GodotObject)_tab).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
			{
				SwitchTabTo(tabManager, _tab);
			}), 0u);
			_tabConnected = true;
		}
		RebindControls();
		LoadAndApplyConfig();
		RefreshAll();
	}

	internal static void RecalcPanelHeight()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (_panel != null && GodotObject.IsInstanceValid((GodotObject)(object)_panel) && _settingsVBox != null && GodotObject.IsInstanceValid((GodotObject)(object)_settingsVBox))
		{
			float y = ((Control)_settingsVBox).GetCombinedMinimumSize().Y;
			if (!(y < 100f))
			{
				float num = y + 20f;
				((Control)_panel).CustomMinimumSize = new Vector2(1012f, num);
			}
		}
	}

	private static void BindTabToPanel(NSettingsTabManager tabManager, NSettingsTab tab, NSettingsPanel panel)
	{
		try
		{
			FieldInfo field = typeof(NSettingsTabManager).GetField("_tabs", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
			{
				GD.PrintErr("[MeleeAttack] 无法找到 _tabs 字段");
			}
			else if (!(field.GetValue(tabManager) is Dictionary<NSettingsTab, NSettingsPanel> dictionary))
			{
				GD.PrintErr("[MeleeAttack] _tabs 不是 Dictionary 类型");
			}
			else
			{
				dictionary[tab] = panel;
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[MeleeAttack] Tab 绑定失败: " + ex.Message);
		}
	}

	private static void SwitchTabTo(NSettingsTabManager tabManager, NSettingsTab tab)
	{
		try
		{
			MethodInfo method = typeof(NSettingsTabManager).GetMethod("SwitchTabTo", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
			{
				method.Invoke(tabManager, new object[1] { tab });
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[MeleeAttack] 切换 Tab 失败: " + ex.Message);
		}
	}

	private static void RebindControls()
	{
		if (_panel == null)
		{
			return;
		}
		HBoxContainer nodeOrNull = ((Node)_panel).GetNodeOrNull<HBoxContainer>(NodePath.op_Implicit("MeleeAttackShell"));
		if (nodeOrNull == null)
		{
			return;
		}
		VBoxContainer nodeOrNull2 = ((Node)nodeOrNull).GetNodeOrNull<VBoxContainer>(NodePath.op_Implicit("ContentHost"));
		if (nodeOrNull2 == null)
		{
			return;
		}
		_characterPage = ((Node)nodeOrNull2).GetNodeOrNull<VBoxContainer>(NodePath.op_Implicit("Page_character"));
		_skillPage = ((Node)nodeOrNull2).GetNodeOrNull<VBoxContainer>(NodePath.op_Implicit("Page_skill"));
		VBoxContainer nodeOrNull3 = ((Node)nodeOrNull).GetNodeOrNull<VBoxContainer>(NodePath.op_Implicit("Sidebar"));
		if (nodeOrNull3 != null)
		{
			foreach (Node child in ((Node)nodeOrNull3).GetChildren(false))
			{
				Button val = (Button)(object)((child is Button) ? child : null);
				if (val != null)
				{
					if (val.Text == "角色")
					{
						_characterNavBtn = val;
					}
					else if (val.Text == "技能")
					{
						_skillNavBtn = val;
					}
				}
			}
		}
		_characterToggleButtons.Clear();
		_characterSliders.Clear();
		_sliderValueLabels.Clear();
		_skillToggleButtons.Clear();
		if (_characterPage != null)
		{
			foreach (Node child2 in ((Node)_characterPage).GetChildren(false))
			{
				HBoxContainer val2 = (HBoxContainer)(object)((child2 is HBoxContainer) ? child2 : null);
				if (val2 == null)
				{
					continue;
				}
				TextureButton val3 = null;
				HSlider val4 = null;
				Label val5 = null;
				foreach (Node child3 in ((Node)val2).GetChildren(false))
				{
					TextureButton val6 = (TextureButton)(object)((child3 is TextureButton) ? child3 : null);
					if (val6 != null)
					{
						val3 = val6;
						continue;
					}
					HBoxContainer val7 = (HBoxContainer)(object)((child3 is HBoxContainer) ? child3 : null);
					if (val7 == null)
					{
						continue;
					}
					foreach (Node child4 in ((Node)val7).GetChildren(false))
					{
						HSlider val8 = (HSlider)(object)((child4 is HSlider) ? child4 : null);
						if (val8 != null)
						{
							val4 = val8;
							continue;
						}
						Label val9 = (Label)(object)((child4 is Label) ? child4 : null);
						if (val9 != null)
						{
							val5 = val9;
						}
					}
				}
				if (val3 != null && val4 != null && val5 != null)
				{
					string key = ((object)((Node)val3).Name).ToString().Replace("Toggle", "");
					_characterToggleButtons[key] = val3;
					_characterSliders[key] = val4;
					_sliderValueLabels[key] = val5;
				}
				else if (val3 != null && val4 == null)
				{
					string key2 = ((object)((Node)val3).Name).ToString().Replace("Toggle", "");
					_characterToggleButtons[key2] = val3;
				}
			}
		}
		if (_characterPage != null)
		{
			foreach (Node child5 in ((Node)_characterPage).GetChildren(false))
			{
				HBoxContainer val10 = (HBoxContainer)(object)((child5 is HBoxContainer) ? child5 : null);
				if (val10 == null)
				{
					continue;
				}
				foreach (Node child6 in ((Node)val10).GetChildren(false))
				{
					TextureButton val11 = (TextureButton)(object)((child6 is TextureButton) ? child6 : null);
					if (val11 != null)
					{
						string text = StringName.op_Implicit(((Node)val11).Name);
						if (text == "meleeattack_monster_globalToggle")
						{
							_monsterGlobalToggle = val11;
						}
						else if (text == "meleeattack_global_teleportToggle")
						{
							_otherCharacterToggle = val11;
						}
					}
				}
			}
		}
		if (_skillPage != null)
		{
			foreach (Node child7 in ((Node)_skillPage).GetChildren(false))
			{
				HBoxContainer val12 = (HBoxContainer)(object)((child7 is HBoxContainer) ? child7 : null);
				if (val12 == null)
				{
					continue;
				}
				foreach (Node child8 in ((Node)val12).GetChildren(false))
				{
					TextureButton val13 = (TextureButton)(object)((child8 is TextureButton) ? child8 : null);
					if (val13 != null && ((Node)val13).Name == StringName.op_Implicit("meleeattack_global_skillsToggle"))
					{
						_globalSkillsToggle = val13;
						break;
					}
				}
			}
		}
		if (_skillPage != null)
		{
			FindAllToggleButtonsRecursive((Node)(object)_skillPage, ref _skillToggleButtons);
		}
		string[] array = new string[23]
		{
			"meleeattack_skill_bullet_time", "meleeattack_skill_knife_trap", "meleeattack_skill_backflip", "meleeattack_skill_grand_finale", "meleeattack_skill_wraith_form", "meleeattack_skill_afterimage", "meleeattack_skill_slice", "meleeattack_skill_pinpoint", "meleeattack_skill_blur", "meleeattack_skill_burst",
			"meleeattack_skill_murder", "meleeattack_skill_demon_form", "meleeattack_skill_body_slam", "meleeattack_skill_whirlwind", "meleeattack_skill_uppercut", "meleeattack_skill_bloodletting", "meleeattack_skill_conflagration", "meleeattack_skill_bludgeon", "meleeattack_skill_biased_cognition", "meleeattack_skill_thunder_storm",
			"meleeattack_skill_reaper_form", "meleeattack_skill_intangible", "meleeattack_skill_zoom_effect"
		};
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!_skillToggleButtons.ContainsKey(text2))
			{
				VBoxContainer skillPage = _skillPage;
				TextureButton val14 = ((skillPage != null) ? ((Node)skillPage).GetNodeOrNull<TextureButton>(NodePath.op_Implicit(text2 + "Toggle")) : null);
				if (val14 != null)
				{
					_skillToggleButtons[text2] = val14;
				}
			}
		}
	}

	private static void FindAllToggleButtonsRecursive(Node node, ref Dictionary<string, TextureButton> dict)
	{
		foreach (Node child in node.GetChildren(false))
		{
			TextureButton val = (TextureButton)(object)((child is TextureButton) ? child : null);
			if (val != null)
			{
				string text = StringName.op_Implicit(((Node)val).Name);
				if (text.EndsWith("Toggle") && text.StartsWith("meleeattack_skill_"))
				{
					string key = text.Replace("Toggle", "");
					if (!dict.ContainsKey(key))
					{
						dict[key] = val;
					}
				}
			}
			else
			{
				FindAllToggleButtonsRecursive(child, ref dict);
			}
		}
	}
}
