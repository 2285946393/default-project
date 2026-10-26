using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace ModConfig;

internal static class ModConfigManager
{
	private static readonly Dictionary<string, ModRegistration> _registrations = new Dictionary<string, ModRegistration>();

	private static readonly Dictionary<string, Dictionary<string, object>> _values = new Dictionary<string, Dictionary<string, object>>();

	private static readonly HashSet<string> _dirtyMods = new HashSet<string>();

	private static bool _saveScheduled;

	private const string ConfigDir = "user://ModConfig/";

	private const string UiStateFile = "user://ModConfig/_ui_state.json";

	internal static IReadOnlyDictionary<string, ModRegistration> Registrations => _registrations;

	internal static HashSet<string> CollapsedMods { get; private set; } = new HashSet<string>();


	internal static void Initialize()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		DirAccess.MakeDirRecursiveAbsolute("user://ModConfig/");
		LoadUiState();
	}

	internal static void Register(ModRegistration reg)
	{
		NormalizeEntryDefaults(reg);
		_registrations[reg.ModId] = reg;
		if (!_values.ContainsKey(reg.ModId))
		{
			_values[reg.ModId] = new Dictionary<string, object>();
		}
		LoadValues(reg);
		SettingsTabInjector.RefreshUI();
		MainFile.Log.Info($"Registered config: {reg.DisplayName} ({reg.Entries.Length} entries)", 1);
	}

	private static void NormalizeEntryDefaults(ModRegistration reg)
	{
		ConfigEntry[] entries = reg.Entries;
		foreach (ConfigEntry configEntry in entries)
		{
			if (configEntry.Type == ConfigType.ColorPicker)
			{
				string text = NormalizeColorHexOrDefault(configEntry.DefaultValue);
				if (!(configEntry.DefaultValue is string a) || !string.Equals(a, text, StringComparison.Ordinal))
				{
					configEntry.DefaultValue = text;
					MainFile.Log.Info($"Normalized ColorPicker default [{reg.ModId}.{configEntry.Key}] -> {text}", 1);
				}
			}
		}
	}

	private static string NormalizeColorHexOrDefault(object? value)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (value is string text)
		{
			string text2 = text.Trim();
			if (!string.IsNullOrEmpty(text2))
			{
				try
				{
					Color val = Color.FromHtml((ReadOnlySpan<char>)text2.TrimStart('#'));
					return "#" + ((Color)(ref val)).ToHtml(false).ToUpperInvariant();
				}
				catch
				{
				}
			}
		}
		return "#FFFFFF";
	}

	internal static T GetValue<T>(string modId, string key)
	{
		string key2 = key;
		if (_values.TryGetValue(modId, out Dictionary<string, object> value) && value.TryGetValue(key2, out var value2))
		{
			try
			{
				return (T)Convert.ChangeType(value2, typeof(T));
			}
			catch
			{
			}
		}
		if (_registrations.TryGetValue(modId, out ModRegistration value3))
		{
			ConfigEntry configEntry = value3.Entries.FirstOrDefault((ConfigEntry e) => e.Key == key2);
			if (configEntry != null)
			{
				try
				{
					return (T)Convert.ChangeType(configEntry.DefaultValue, typeof(T));
				}
				catch
				{
				}
			}
		}
		return default(T);
	}

	internal static void SetValue(string modId, string key, object value)
	{
		string key2 = key;
		if (!_values.ContainsKey(modId))
		{
			_values[modId] = new Dictionary<string, object>();
		}
		_values[modId][key2] = value;
		if (_registrations.TryGetValue(modId, out ModRegistration value2))
		{
			ConfigEntry configEntry = value2.Entries.FirstOrDefault((ConfigEntry e) => e.Key == key2);
			try
			{
				configEntry?.OnChanged?.Invoke(value);
			}
			catch (Exception value3)
			{
				MainFile.Log.Error($"Config callback error [{modId}.{key2}]: {value3}", 1);
			}
		}
		SettingsTabInjector.NotifyValueChanged(modId, key2, value);
		ScheduleSave(modId);
	}

	internal static bool ResetToDefaults(string modId)
	{
		if (!_registrations.TryGetValue(modId, out ModRegistration value))
		{
			return false;
		}
		if (!_values.ContainsKey(modId))
		{
			_values[modId] = new Dictionary<string, object>();
		}
		bool flag = false;
		ConfigEntry[] entries = value.Entries;
		foreach (ConfigEntry configEntry in entries)
		{
			ConfigType type = configEntry.Type;
			if ((uint)(type - 5) <= 2u)
			{
				continue;
			}
			object? valueOrDefault = _values[modId].GetValueOrDefault(configEntry.Key);
			_values[modId][configEntry.Key] = configEntry.DefaultValue;
			if (!object.Equals(valueOrDefault, configEntry.DefaultValue))
			{
				flag = true;
				try
				{
					configEntry.OnChanged?.Invoke(configEntry.DefaultValue);
				}
				catch (Exception value2)
				{
					MainFile.Log.Error($"Config callback error [{modId}.{configEntry.Key}]: {value2}", 1);
				}
			}
		}
		if (flag)
		{
			ScheduleSave(modId);
		}
		return flag;
	}

	private static void ScheduleSave(string modId)
	{
		_dirtyMods.Add(modId);
		if (!_saveScheduled)
		{
			_saveScheduled = true;
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += FlushSaves;
			}
			else
			{
				FlushSaves();
			}
		}
	}

	private static void FlushSaves()
	{
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (val != null)
		{
			val.ProcessFrame -= FlushSaves;
		}
		_saveScheduled = false;
		foreach (string dirtyMod in _dirtyMods)
		{
			SaveValues(dirtyMod);
		}
		_dirtyMods.Clear();
	}

	private static void LoadValues(ModRegistration reg)
	{
		string text = "user://ModConfig/" + reg.ModId + ".json";
		Dictionary<string, JsonElement> dictionary = null;
		if (FileAccess.FileExists(text))
		{
			try
			{
				FileAccess val = FileAccess.Open(text, (ModeFlags)1);
				try
				{
					dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(val.GetAsText(false));
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			catch (Exception value)
			{
				MainFile.Log.Error($"Failed to load config for {reg.ModId}: {value}", 1);
			}
		}
		ConfigEntry[] entries = reg.Entries;
		foreach (ConfigEntry configEntry in entries)
		{
			ConfigType type = configEntry.Type;
			if ((uint)(type - 5) <= 2u)
			{
				continue;
			}
			if (dictionary != null && dictionary.TryGetValue(configEntry.Key, out var value2))
			{
				try
				{
					Dictionary<string, object> dictionary2 = _values[reg.ModId];
					string key = configEntry.Key;
					dictionary2[key] = configEntry.Type switch
					{
						ConfigType.Toggle => value2.GetBoolean(), 
						ConfigType.Slider => (float)value2.GetDouble(), 
						ConfigType.Dropdown => value2.GetString() ?? ((string)configEntry.DefaultValue), 
						ConfigType.KeyBind => value2.GetInt64(), 
						ConfigType.TextInput => value2.GetString() ?? ((string)configEntry.DefaultValue), 
						ConfigType.ColorPicker => value2.GetString() ?? ((string)configEntry.DefaultValue), 
						_ => configEntry.DefaultValue, 
					};
				}
				catch
				{
					goto IL_01d0;
				}
				continue;
			}
			goto IL_01d0;
			IL_01d0:
			_values[reg.ModId][configEntry.Key] = configEntry.DefaultValue;
		}
	}

	private static void SaveValues(string modId)
	{
		if (!_values.TryGetValue(modId, out Dictionary<string, object> value))
		{
			return;
		}
		string text = "user://ModConfig/" + modId + ".json";
		try
		{
			string text2 = JsonSerializer.Serialize(value, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			FileAccess val = FileAccess.Open(text, (ModeFlags)2);
			try
			{
				val.StoreString(text2);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception value2)
		{
			MainFile.Log.Error($"Failed to save config for {modId}: {value2}", 1);
		}
	}

	internal static void SaveAll()
	{
		foreach (string key in _values.Keys)
		{
			SaveValues(key);
		}
	}

	internal static void SaveUiState()
	{
		try
		{
			string text = JsonSerializer.Serialize(new Dictionary<string, object> { ["collapsed_mods"] = CollapsedMods.ToArray() }, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			FileAccess val = FileAccess.Open("user://ModConfig/_ui_state.json", (ModeFlags)2);
			try
			{
				val.StoreString(text);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception value)
		{
			MainFile.Log.Error($"Failed to save UI state: {value}", 1);
		}
	}

	private static void LoadUiState()
	{
		try
		{
			if (!FileAccess.FileExists("user://ModConfig/_ui_state.json"))
			{
				return;
			}
			FileAccess val = FileAccess.Open("user://ModConfig/_ui_state.json", (ModeFlags)1);
			try
			{
				Dictionary<string, JsonElement> dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(val.GetAsText(false));
				if (dictionary == null || !dictionary.TryGetValue("collapsed_mods", out var value))
				{
					return;
				}
				foreach (JsonElement item in value.EnumerateArray())
				{
					string @string = item.GetString();
					if (@string != null)
					{
						CollapsedMods.Add(@string);
					}
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception value2)
		{
			MainFile.Log.Error($"Failed to load UI state: {value2}", 1);
		}
	}
}
