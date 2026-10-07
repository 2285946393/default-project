using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;
using RegentFX.Scripts;
using RegentFX.Scripts.Vfx.Cards;
using RegentFX.Scripts.Vfx.Powers;

namespace RegentFX.ThirdParty;

public static class RitsuLibModConfig
{
	private const string ModId = "RegentFX";

	private static readonly object FileLock = new object();

	private static readonly ConcurrentDictionary<string, JsonNode?> Hot = new ConcurrentDictionary<string, JsonNode>(StringComparer.Ordinal);

	private static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>
	{
		["ExposureThreshold"] = 1,
		["DevTestStartMode"] = false,
		["PreloadEffects"] = true,
		["DisableModSounds"] = false
	};

	private static string DataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlayTheSpire2", "RegentFX");

	private static string SchemaPath => Path.Combine(DataDir, "ritsu_interop_schema.json");

	private static string StatePath => Path.Combine(DataDir, "ritsu_interop_state.json");

	public static object CreateRitsuLibSettingsSchema()
	{
		Directory.CreateDirectory(DataDir);
		File.WriteAllText(SchemaPath, BuildDefaultSchemaJson());
		return SchemaPath;
	}

	public static void SetDefaults()
	{
		CardFX.EnsureRegistry();
		foreach (Type value in CardFX.Registry.Values)
		{
			Defaults["card_" + value.Name] = true;
		}
		PowerFX.EnsureRegistry();
		foreach (Type value2 in PowerFX.Registry.Values)
		{
			Defaults["power_" + value2.Name] = true;
		}
	}

	private static string BuildDefaultSchemaJson()
	{
		RitsuLibModConfigEntity ritsuLibModConfigEntity = new RitsuLibModConfigEntity();
		ritsuLibModConfigEntity.modDisplayName = SimpleLocUtil.Simple("万象辉星", "RegentFX");
		RitsuLibModConfigEntity value = ritsuLibModConfigEntity;
		RLMCPage item = new RLMCPage();
		item.pageId = "main";
		item.title = SimpleLocUtil.Simple("主要设置", "Main");
		item.description = SimpleLocUtil.Simple("主要设置", "Main");
		item.sortOrder = 1;
		RLMCSection item2 = new RLMCSection();
		item2.id = "core";
		item2.title = SimpleLocUtil.Simple("基础", "Basics");
		SliderEntry sliderEntry = new SliderEntry();
		sliderEntry.id = "master_vol";
		sliderEntry.key = "ExposureThreshold";
		sliderEntry.label = SimpleLocUtil.Simple("曝光光效强度", "Light Exposure");
		sliderEntry.description = SimpleLocUtil.Simple("设置为0将关闭光效", "Set 0 to disable exposure");
		sliderEntry.min = 0.0;
		sliderEntry.max = 2.0;
		sliderEntry.step = 0.05;
		item2.entries.Add(sliderEntry);
		ToggleEntry toggleEntry = new ToggleEntry();
		toggleEntry.id = "PreloadEffects";
		toggleEntry.key = toggleEntry.id;
		toggleEntry.description = SimpleLocUtil.Simple("需重启游戏生效，能解决第一次打出特效卡顿问题，但是内存占用会提升。(Mac系统如果卡顿建议关闭此选项)", "Require game restart. Can resolve the lag issue when playing effects for the first time, but will increase memory usage");
		toggleEntry.label = SimpleLocUtil.Simple("特效预加载", "Preload Cache");
		item2.entries.Add(toggleEntry);
		ToggleEntry toggleEntry2 = new ToggleEntry();
		toggleEntry2.id = "DisableModSounds";
		toggleEntry2.key = toggleEntry2.id;
		toggleEntry2.description = SimpleLocUtil.Simple("禁用后会恢复至原版游戏默认攻击音效", "Fallback to original attack sound effects.");
		toggleEntry2.label = SimpleLocUtil.Simple("禁用mod音效", "Disable Mod Sounds");
		item2.entries.Add(toggleEntry2);
		RLMCSection item3 = new RLMCSection();
		item3.id = "cards";
		item3.title = SimpleLocUtil.Simple("卡牌", "Cards");
		RLMCSection item4 = new RLMCSection();
		item4.id = "powers";
		item4.title = SimpleLocUtil.Simple("能力", "Powers");
		CardFX.EnsureRegistry();
		foreach (Type key in CardFX.Registry.Keys)
		{
			Type type = CardFX.Registry[key];
			ToggleEntry toggleEntry3 = new ToggleEntry();
			CardModel val = null;
			try
			{
				AbstractModel obj = ModelDb.Get(key);
				val = (CardModel)(object)((obj is CardModel) ? obj : null);
			}
			catch (Exception)
			{
				Entry.Logger.Warn("[RitsuConfig] Cannot find cardModel: " + key.Name, 1);
				continue;
			}
			toggleEntry3.id = CardFX.GetToggleKey(type);
			toggleEntry3.key = toggleEntry3.id;
			toggleEntry3.label = val.TitleLocString.GetFormattedText();
			toggleEntry3.description = null;
			item3.entries.Add(toggleEntry3);
		}
		PowerFX.EnsureRegistry();
		foreach (Type key2 in PowerFX.Registry.Keys)
		{
			Type type2 = PowerFX.Registry[key2];
			ToggleEntry toggleEntry4 = new ToggleEntry();
			PowerModel val2 = null;
			try
			{
				AbstractModel obj2 = ModelDb.Get(key2);
				val2 = (PowerModel)(object)((obj2 is PowerModel) ? obj2 : null);
			}
			catch (Exception)
			{
				Entry.Logger.Warn("[RitsuConfig] Cannot find powerModel: " + key2.Name, 1);
				continue;
			}
			toggleEntry4.id = PowerFX.GetToggleKey(type2);
			toggleEntry4.key = toggleEntry4.id;
			toggleEntry4.label = val2.Title.GetFormattedText();
			toggleEntry4.description = null;
			item4.entries.Add(toggleEntry4);
		}
		item.sections.Add(item2);
		item.sections.Add(item3);
		item.sections.Add(item4);
		value.pages.Add(item);
		RLMCPage item5 = new RLMCPage();
		item5.pageId = "debug";
		item5.title = SimpleLocUtil.Simple("调试设置", "Debug Settings");
		item5.description = SimpleLocUtil.Simple("测试Mod使用，会影响游戏性，请勿修改！", "Only for debugging. Do not change!");
		item5.sortOrder = 3;
		RLMCSection item6 = new RLMCSection();
		item6.id = "debug_core";
		item6.title = SimpleLocUtil.Simple("基础", "Basics");
		ToggleEntry toggleEntry5 = new ToggleEntry();
		toggleEntry5.id = "DevTestStartMode";
		toggleEntry5.key = toggleEntry5.id;
		toggleEntry5.label = SimpleLocUtil.Simple("测试模式", "Test Mode");
		item6.entries.Add(toggleEntry5);
		item5.sections.Add(item6);
		value.pages.Add(item5);
		return JsonSerializer.Serialize(value);
	}

	public static void SetRitsuLibSettingValue(string key, object? value)
	{
		SetCore(key, value);
	}

	public static object? GetRitsuLibSettingValue(string key)
	{
		return GetCore(key);
	}

	public static void SaveRitsuLibSettings()
	{
		lock (FileLock)
		{
			string statePath = StatePath;
			JsonObject jsonObject = new JsonObject();
			foreach (KeyValuePair<string, JsonNode> item in Hot)
			{
				jsonObject[item.Key] = ((item.Value == null) ? null : JsonNode.Parse(item.Value.ToJsonString()));
			}
			File.WriteAllText(statePath, jsonObject.ToJsonString(new JsonSerializerOptions
			{
				WriteIndented = true
			}));
		}
	}

	public static bool GetRitsuLibSettingBool(string key)
	{
		return CoerceBool(GetCore(key));
	}

	public static void SetRitsuLibSettingBool(string key, bool value)
	{
		SetCore(key, value);
	}

	public static double GetRitsuLibSettingDouble(string key)
	{
		return CoerceDouble(GetCore(key));
	}

	public static void SetRitsuLibSettingDouble(string key, double value)
	{
		SetCore(key, value);
	}

	public static int GetRitsuLibSettingInt(string key)
	{
		return CoerceInt(GetCore(key));
	}

	public static void SetRitsuLibSettingInt(string key, int value)
	{
		SetCore(key, value);
	}

	public static string? GetRitsuLibSettingString(string key)
	{
		return GetCore(key)?.ToString();
	}

	public static void SetRitsuLibSettingString(string key, string value)
	{
		SetCore(key, value);
	}

	public static void InvokeRitsuLibSettingAction(string key)
	{
		if (!(key == "reset_all"))
		{
			return;
		}
		Hot.Clear();
		try
		{
			if (File.Exists(StatePath))
			{
				File.Delete(StatePath);
			}
		}
		catch
		{
		}
	}

	private static void SetCore(string key, object? value)
	{
		LoadIfNeeded();
		Hot[key] = JsonSerializer.SerializeToNode(value);
	}

	private static object? GetCore(string key)
	{
		LoadIfNeeded();
		if (!Hot.TryGetValue(key, out JsonNode value) || value == null)
		{
			return Defaults.GetValueOrDefault(key);
		}
		if (value is JsonValue jsonValue)
		{
			return jsonValue.GetValue<object>();
		}
		return value;
	}

	private static void LoadIfNeeded()
	{
		if (Hot.Count > 0)
		{
			return;
		}
		lock (FileLock)
		{
			if (Hot.Count > 0 || !File.Exists(StatePath))
			{
				return;
			}
			JsonObject jsonObject = JsonNode.Parse(File.ReadAllText(StatePath))?.AsObject();
			if (jsonObject == null)
			{
				return;
			}
			foreach (KeyValuePair<string, JsonNode> item in jsonObject)
			{
				Hot[item.Key] = item.Value;
			}
		}
	}

	private static bool CoerceBool(object? o)
	{
		if (o is bool)
		{
			if ((bool)o)
			{
				return true;
			}
			return false;
		}
		if (o != null)
		{
			if (o is JsonValue jsonValue)
			{
				bool value;
				return jsonValue.TryGetValue<bool>(out value) && value;
			}
			bool result;
			return bool.TryParse(o.ToString(), out result) && result;
		}
		return false;
	}

	private static double CoerceDouble(object? o)
	{
		if (o != null)
		{
			if (!(o is JsonValue jsonValue))
			{
				if (o is IConvertible value)
				{
					return Convert.ToDouble(value);
				}
				double result;
				return double.TryParse(o.ToString(), out result) ? result : 0.0;
			}
			return jsonValue.GetValue<double>();
		}
		return 0.0;
	}

	private static int CoerceInt(object? o)
	{
		if (o != null)
		{
			if (!(o is JsonValue jsonValue))
			{
				if (o is IConvertible value)
				{
					return Convert.ToInt32(value);
				}
				int result;
				return int.TryParse(o.ToString(), out result) ? result : 0;
			}
			return jsonValue.GetValue<int>();
		}
		return 0;
	}
}
