using System;
using System.Collections.Generic;
using Godot;
using IntentGraph2.Crossovers;
using IntentGraph2.Utils;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace IntentGraph2.RitsuLib;

public class RitsuLibHelper : IRitsuLibHelper
{
	private const string SettingKey = "Settings";

	public IntentGraphModConfig Config => RitsuLibFramework.GetDataStore("intentgraph2").Get<IntentGraphModConfig>("Settings");

	public void RegisterConfig()
	{
		using (RitsuLibFramework.BeginModDataRegistration("intentgraph2", true))
		{
			RitsuLibFramework.GetDataStore("intentgraph2").Register<IntentGraphModConfig>("Settings", "settings.json", (SaveScope)0, (Func<IntentGraphModConfig>)(() => new IntentGraphModConfig()), true, (ModDataMigrationConfig)null, (IEnumerable<IMigration>)null);
		}
		ModSettingsValueBinding<IntentGraphModConfig, Key> toggleIntentGraph = new ModSettingsValueBinding<IntentGraphModConfig, Key>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, Key>)((IntentGraphModConfig settings) => settings.ToggleIntentGraphKey), (Action<IntentGraphModConfig, Key>)delegate(IntentGraphModConfig settings, Key value)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			settings.ToggleIntentGraphKey = value;
			settings.NotifyUpdated("ToggleIntentGraphKey");
		});
		ModSettingsValueBinding<IntentGraphModConfig, bool> showMonsterMoveNames = new ModSettingsValueBinding<IntentGraphModConfig, bool>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, bool>)((IntentGraphModConfig settings) => settings.ShowMonsterMoveNames), (Action<IntentGraphModConfig, bool>)delegate(IntentGraphModConfig settings, bool value)
		{
			settings.ShowMonsterMoveNames = value;
			settings.NotifyUpdated("ShowMonsterMoveNames");
		});
		ModSettingsValueBinding<IntentGraphModConfig, bool> useAnimatedIntentIcon = new ModSettingsValueBinding<IntentGraphModConfig, bool>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, bool>)((IntentGraphModConfig settings) => settings.UseAnimatedIntentIcon), (Action<IntentGraphModConfig, bool>)delegate(IntentGraphModConfig settings, bool value)
		{
			settings.UseAnimatedIntentIcon = value;
			settings.NotifyUpdated("UseAnimatedIntentIcon");
		});
		ModSettingsValueBinding<IntentGraphModConfig, bool> showMoveDetail = new ModSettingsValueBinding<IntentGraphModConfig, bool>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, bool>)((IntentGraphModConfig settings) => settings.ShowMoveDetail), (Action<IntentGraphModConfig, bool>)delegate(IntentGraphModConfig settings, bool value)
		{
			settings.ShowMoveDetail = value;
			settings.NotifyUpdated("ShowMoveDetail");
		});
		ModSettingsValueBinding<IntentGraphModConfig, bool> showCurrentMove = new ModSettingsValueBinding<IntentGraphModConfig, bool>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, bool>)((IntentGraphModConfig settings) => settings.ShowCurrentMove), (Action<IntentGraphModConfig, bool>)delegate(IntentGraphModConfig settings, bool value)
		{
			settings.ShowCurrentMove = value;
			settings.NotifyUpdated("ShowCurrentMove");
		});
		ModSettingsValueBinding<IntentGraphModConfig, IntentGraphPosition> intentGraphPosition = new ModSettingsValueBinding<IntentGraphModConfig, IntentGraphPosition>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, IntentGraphPosition>)((IntentGraphModConfig settings) => settings.IntentGraphPosition), (Action<IntentGraphModConfig, IntentGraphPosition>)delegate(IntentGraphModConfig settings, IntentGraphPosition value)
		{
			settings.IntentGraphPosition = value;
			settings.NotifyUpdated("IntentGraphPosition");
		});
		ModSettingsValueBinding<IntentGraphModConfig, double> intentGraphScale = new ModSettingsValueBinding<IntentGraphModConfig, double>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, double>)((IntentGraphModConfig settings) => settings.IntentGraphScale * 100f), (Action<IntentGraphModConfig, double>)delegate(IntentGraphModConfig settings, double value)
		{
			settings.IntentGraphScale = (float)value / 100f;
			settings.NotifyUpdated("IntentGraphScale");
		});
		ModSettingsValueBinding<IntentGraphModConfig, string> attackDamageFormat = new ModSettingsValueBinding<IntentGraphModConfig, string>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, string>)((IntentGraphModConfig settings) => settings.AttackDamageFormat), (Action<IntentGraphModConfig, string>)delegate(IntentGraphModConfig settings, string value)
		{
			settings.AttackDamageFormat = value;
			settings.NotifyUpdated("AttackDamageFormat");
		});
		ModSettingsValueBinding<IntentGraphModConfig, bool> pinableIntentGraph = new ModSettingsValueBinding<IntentGraphModConfig, bool>("intentgraph2", "Settings", (SaveScope)0, (Func<IntentGraphModConfig, bool>)((IntentGraphModConfig settings) => settings.PinableIntentGraph), (Action<IntentGraphModConfig, bool>)delegate(IntentGraphModConfig settings, bool value)
		{
			settings.PinableIntentGraph = value;
			settings.NotifyUpdated("PinableIntentGraph");
		});
		RitsuLibFramework.RegisterModSettings("intentgraph2", (Action<ModSettingsPageBuilder>)delegate(ModSettingsPageBuilder page)
		{
			page.WithTitle(ModSettingsText.LocString("settings_ui", "INTENTGRAPH2.mod_title", "Intent Graph"));
			page.WithDescription(ModSettingsText.LocString("settings_ui", "INTENTGRAPH2.mod_description", "Show monster intent as a state machine."));
			page.WithModDisplayName(ModSettingsText.LocString("settings_ui", "INTENTGRAPH2.mod_title", "Intent Graph"));
			page.AddSection("display", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-DISPLAY.title", "Display"));
				section.AddToggle("show_monster_move_names", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-SHOW_MONSTER_MOVE_NAMES.title", "Show Monster Move Names"), (IModSettingsValueBinding<bool>)(object)showMonsterMoveNames, (ModSettingsText)null, (Func<bool>)null);
				section.AddToggle("show_move_detail", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-SHOW_MOVE_DETAIL.title", "Show Move Detail"), (IModSettingsValueBinding<bool>)(object)showMoveDetail, (ModSettingsText)null, (Func<bool>)null);
				section.AddToggle("use_animated_intent_icon", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-USE_ANIMATED_INTENT_ICON.title", "Use Animated Intent Icon"), (IModSettingsValueBinding<bool>)(object)useAnimatedIntentIcon, (ModSettingsText)null, (Func<bool>)null);
				section.AddToggle("show_current_move", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-SHOW_CURRENT_MOVE.title", "Show Current Move"), (IModSettingsValueBinding<bool>)(object)showCurrentMove, (ModSettingsText)null, (Func<bool>)null);
				section.AddEnumChoice<IntentGraphPosition>("intent_graph_position", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-INTENT_GRAPH_POSITION.title", "Intent Graph Position"), (IModSettingsValueBinding<IntentGraphPosition>)(object)intentGraphPosition, (Func<IntentGraphPosition, ModSettingsText>)((IntentGraphPosition position) => ModSettingsText.LocString("settings_ui", $"INTENTGRAPH2-INTENT_GRAPH_POSITION.{position}", position.ToString())), (ModSettingsText)null, (ModSettingsChoicePresentation)0);
				section.AddSlider("intent_graph_scale", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-INTENT_GRAPH_SCALE.title", "Intent Graph Scale"), (IModSettingsValueBinding<double>)(object)intentGraphScale, 50.0, 150.0, 10.0, (Func<double, string>)((double value) => $"{value:0.##}%"), (ModSettingsText)null);
				ModSettingsText obj = ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-ATTACK_DAMAGE_FORMAT.title", "Attack Damage Format");
				ModSettingsValueBinding<IntentGraphModConfig, string> obj2 = attackDamageFormat;
				ModSettingsText val = ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-ATTACK_DAMAGE_FORMAT.hover.desc", "Attack Damage Format Description");
				section.AddString("attack_damage_format", obj, (IModSettingsValueBinding<string>)(object)obj2, (ModSettingsText)null, (int?)null, val);
			});
			page.AddSection("control", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-CONTROL.title", "Control"));
				section.AddToggle("pinable_intent_graph", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-PINABLE_INTENT_GRAPH.title", "Pinable Intent Graph"), (IModSettingsValueBinding<bool>)(object)pinableIntentGraph, (ModSettingsText)null, (Func<bool>)null);
			});
			page.AddSection("hotkey", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-HOTKEY.title", "Hotkey"));
				section.AddEnumChoice<Key>("toggle_intent_graph", ModSettingsText.LocString("settings_ui", "INTENTGRAPH2-TOGGLE_INTENT_GRAPH_KEY.title", "Toggle Intent Graph"), (IModSettingsValueBinding<Key>)(object)toggleIntentGraph, (Func<Key, ModSettingsText>)null, (ModSettingsText)null, (ModSettingsChoicePresentation)1);
			});
		}, (string)null);
	}

	public void SaveConfig()
	{
		RitsuLibFramework.GetDataStore("intentgraph2").Save("Settings");
	}
}
