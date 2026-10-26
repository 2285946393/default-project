using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

namespace LoadOrderManager;

internal static class ModPresetStore
{
	public const int CurrentSchemaVersion = 2;

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	private static PresetSaveData? _cache;

	private static bool _freshStore;

	private static string StorePath
	{
		get
		{
			string text = Path.Combine(OS.GetUserDataDir(), "LoadOrderManager");
			Directory.CreateDirectory(text);
			return Path.Combine(text, "presets.json");
		}
	}

	public static ModPreset CurrentProfile
	{
		get
		{
			PresetSaveData presetSaveData = Load();
			NormalizeIndex(presetSaveData);
			return presetSaveData.Profiles[presetSaveData.CurrentProfileIndex];
		}
	}

	public static PresetSaveData Load()
	{
		if (_cache != null)
		{
			return _cache;
		}
		try
		{
			string storePath = StorePath;
			if (File.Exists(storePath))
			{
				_cache = JsonSerializer.Deserialize<PresetSaveData>(File.ReadAllText(storePath)) ?? CreateDefault();
			}
			else
			{
				_cache = CreateDefault();
				_freshStore = true;
			}
		}
		catch (Exception ex)
		{
			DebugLog.Error("ModPresetStore.Load failed.", ex);
			_cache = CreateDefault();
			_freshStore = true;
		}
		NormalizeIndex(_cache);
		return _cache;
	}

	public static bool Save()
	{
		if (_cache == null)
		{
			return false;
		}
		try
		{
			string storePath = StorePath;
			string contents = JsonSerializer.Serialize(_cache, JsonOpts);
			File.WriteAllText(storePath, contents);
			return true;
		}
		catch (Exception ex)
		{
			DebugLog.Error("ModPresetStore.Save failed.", ex);
			return false;
		}
	}

	public static void MigrateIfNeeded(IReadOnlyList<LoadOrderEntry> entries)
	{
		PresetSaveData presetSaveData = Load();
		bool flag = presetSaveData.SchemaVersion < 2;
		if (!flag && !_freshStore)
		{
			return;
		}
		List<string> list = (from e in entries
			where !string.IsNullOrWhiteSpace(e.Id)
			select e.Id).ToList();
		for (int i = 0; i < presetSaveData.Profiles.Count; i++)
		{
			ModPreset modPreset = presetSaveData.Profiles[i];
			foreach (string item in list)
			{
				modPreset.KnownMods.Add(item);
			}
			if (flag)
			{
				modPreset.AutoEnableNewMods = i == 0;
			}
		}
		if (_freshStore)
		{
			ModPreset modPreset2 = presetSaveData.Profiles[presetSaveData.CurrentProfileIndex];
			foreach (LoadOrderEntry entry in entries)
			{
				if (!string.IsNullOrWhiteSpace(entry.Id) && !entry.IsEnabled)
				{
					modPreset2.DisabledMods.Add(entry.Id);
				}
			}
		}
		presetSaveData.SchemaVersion = 2;
		_freshStore = false;
		Save();
		DebugLog.Info($"Presets initialised (legacy={flag}, schema={2}). profiles={presetSaveData.Profiles.Count}, installed={list.Count}");
	}

	public static void CommitCurrentProfile(IReadOnlyList<LoadOrderEntry> entries)
	{
		ModPreset currentProfile = CurrentProfile;
		currentProfile.DisabledMods.Clear();
		currentProfile.KnownMods.Clear();
		foreach (LoadOrderEntry entry in entries)
		{
			if (!string.IsNullOrWhiteSpace(entry.Id))
			{
				currentProfile.KnownMods.Add(entry.Id);
				if (!entry.IsEnabled)
				{
					currentProfile.DisabledMods.Add(entry.Id);
				}
			}
		}
		Save();
	}

	public static int ApplyPreset(ModPreset preset, List<LoadOrderEntry> entries)
	{
		int num = 0;
		foreach (LoadOrderEntry entry in entries)
		{
			if (string.IsNullOrWhiteSpace(entry.Id) || !preset.KnownMods.Contains(entry.Id))
			{
				entry.IsEnabled = preset.AutoEnableNewMods;
				entry.IsNew = true;
				num++;
			}
			else
			{
				entry.IsEnabled = !preset.DisabledMods.Contains(entry.Id);
				entry.IsNew = false;
			}
		}
		return num;
	}

	public static int ApplyNewModPolicy(ModPreset preset, List<LoadOrderEntry> entries)
	{
		int num = 0;
		foreach (LoadOrderEntry entry in entries)
		{
			if (string.IsNullOrWhiteSpace(entry.Id) || preset.KnownMods.Contains(entry.Id))
			{
				entry.IsNew = false;
				continue;
			}
			entry.IsEnabled = preset.AutoEnableNewMods;
			entry.IsNew = true;
			num++;
		}
		return num;
	}

	public static bool DecideEnabled(bool isKnown, bool isDisabled, bool autoEnableNewMods)
	{
		if (!isKnown)
		{
			return autoEnableNewMods;
		}
		return !isDisabled;
	}

	public static ModPreset CreatePreset(string name, IReadOnlyList<LoadOrderEntry> entries)
	{
		PresetSaveData presetSaveData = Load();
		ModPreset modPreset = new ModPreset
		{
			Name = name,
			KnownMods = (from e in entries
				where !string.IsNullOrWhiteSpace(e.Id)
				select e.Id).ToHashSet(),
			DisabledMods = (from e in entries
				where !e.IsEnabled && !string.IsNullOrWhiteSpace(e.Id)
				select e.Id).ToHashSet(),
			AutoEnableNewMods = false
		};
		presetSaveData.Profiles.Add(modPreset);
		presetSaveData.CurrentProfileIndex = presetSaveData.Profiles.Count - 1;
		Save();
		return modPreset;
	}

	public static bool SetAutoEnableNewMods(bool value)
	{
		PresetSaveData presetSaveData = Load();
		presetSaveData.Profiles[presetSaveData.CurrentProfileIndex].AutoEnableNewMods = value;
		return Save();
	}

	public static bool DeleteCurrentPreset()
	{
		PresetSaveData presetSaveData = Load();
		if (presetSaveData.Profiles.Count <= 1)
		{
			return false;
		}
		presetSaveData.Profiles.RemoveAt(presetSaveData.CurrentProfileIndex);
		if (presetSaveData.CurrentProfileIndex >= presetSaveData.Profiles.Count)
		{
			presetSaveData.CurrentProfileIndex = presetSaveData.Profiles.Count - 1;
		}
		Save();
		return true;
	}

	public static bool RenameCurrentPreset(string newName)
	{
		string text = newName.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		PresetSaveData presetSaveData = Load();
		presetSaveData.Profiles[presetSaveData.CurrentProfileIndex].Name = text;
		return Save();
	}

	public static void SelectProfile(int index, IReadOnlyList<LoadOrderEntry> entries)
	{
		PresetSaveData presetSaveData = Load();
		CommitCurrentProfile(entries);
		presetSaveData.CurrentProfileIndex = index;
		NormalizeIndex(presetSaveData);
		Save();
	}

	private static PresetSaveData CreateDefault()
	{
		return new PresetSaveData
		{
			SchemaVersion = 2,
			Profiles = new List<ModPreset>
			{
				new ModPreset
				{
					Name = "Default",
					AutoEnableNewMods = true
				}
			},
			CurrentProfileIndex = 0
		};
	}

	private static void NormalizeIndex(PresetSaveData? data)
	{
		if (data != null)
		{
			if (data.Profiles.Count == 0)
			{
				data.Profiles.Add(new ModPreset
				{
					Name = "Default",
					AutoEnableNewMods = true
				});
			}
			if (data.CurrentProfileIndex < 0)
			{
				data.CurrentProfileIndex = 0;
			}
			if (data.CurrentProfileIndex >= data.Profiles.Count)
			{
				data.CurrentProfileIndex = data.Profiles.Count - 1;
			}
		}
	}
}
