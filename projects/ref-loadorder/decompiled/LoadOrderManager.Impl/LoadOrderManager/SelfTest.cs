using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;

namespace LoadOrderManager;

internal static class SelfTest
{
	private sealed record SavedEntry(string Id, string Source, bool IsEnabled);

	private const string ModeEnv = "LOADORDER_SELFTEST";

	private const string WriteEnv = "LOADORDER_SELFTEST_WRITE";

	private const string ApplyEnv = "LOADORDER_SELFTEST_APPLY";

	private const string ProbeEnv = "LOADORDER_SELFTEST_PROBE";

	private const string AutoPresetName = "SelfTest";

	private const double TickSeconds = 0.25;

	private static string _mode = string.Empty;

	private static bool _write;

	private static string _applyPreset = string.Empty;

	private static string _probeId = "LomProbe";

	private static bool _scheduled;

	private static bool _ticking;

	private static bool _finished;

	private static LoadOrderPanel? _panel;

	private static int _step;

	private static int _waitTicks;

	private static int _readyTicks;

	private static int _ticks;

	public static void Schedule()
	{
		if (_scheduled)
		{
			return;
		}
		string text = ReadEnv("LOADORDER_SELFTEST");
		if (!string.IsNullOrWhiteSpace(text))
		{
			_mode = text.Trim().ToLowerInvariant();
			_write = ReadEnv("LOADORDER_SELFTEST_WRITE") == "1";
			_applyPreset = ReadEnv("LOADORDER_SELFTEST_APPLY")?.Trim() ?? string.Empty;
			string text2 = ReadEnv("LOADORDER_SELFTEST_PROBE");
			if (!string.IsNullOrWhiteSpace(text2))
			{
				_probeId = text2.Trim();
			}
			_scheduled = true;
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val == null)
			{
				Log("no SceneTree; aborted");
				return;
			}
			Log($"scheduled: mode={_mode}, write={_write}, apply='{_applyPreset}', probe={_probeId}");
			StartTicking(val);
		}
	}

	private static void StartTicking(SceneTree tree)
	{
		if (!_ticking && !_finished)
		{
			_ticking = true;
			ChainNextTick(tree);
		}
	}

	private static void ChainNextTick(SceneTree tree)
	{
		SceneTree tree2 = tree;
		if (!_finished)
		{
			tree2.CreateTimer(0.25, true, false, false).Timeout += delegate
			{
				_ticking = false;
				Tick(tree2);
			};
		}
	}

	private static void Tick(SceneTree tree)
	{
		try
		{
			_ticks++;
			if (!SaveManagerReady())
			{
				if (++_readyTicks % 40 == 0)
				{
					Log($"waiting for SaveManager.SettingsSave.ModSettings... ({_readyTicks} ticks)");
				}
				if (_readyTicks > 400)
				{
					Log("timed out waiting for the save system; aborted");
					Finish(tree);
				}
				else
				{
					StartTicking(tree);
				}
			}
			else if (_waitTicks > 0)
			{
				_waitTicks--;
				StartTicking(tree);
			}
			else
			{
				Step(tree);
			}
		}
		catch (Exception value)
		{
			Log($"FAILED: {value}");
			Finish(tree);
		}
	}

	private static void Finish(SceneTree tree)
	{
		if (!_finished)
		{
			_finished = true;
			Log("=== self-test end ===");
			Log("quitting");
			tree.Quit(0);
		}
	}

	private static void Step(SceneTree tree)
	{
		switch (_step)
		{
		case 0:
		{
			Log($"=== self-test begin === (sceneTreePaused={tree.Paused})");
			LoadOrderPanel loadOrderPanel = new LoadOrderPanel();
			((Node)loadOrderPanel).Name = StringName.op_Implicit("LoadOrderManager_SelfTestPanel");
			_panel = loadOrderPanel;
			((Node)tree.Root).AddChild((Node)(object)_panel, false, (InternalMode)0);
			_panel.OpenPanel();
			Log("panel opened");
			_waitTicks = 2;
			_step = 1;
			StartTicking(tree);
			break;
		}
		case 1:
			_panel.DebugLogLayoutCheck();
			ReportEntries("initial");
			if (_mode == "panel")
			{
				ProbeModdingScreenInjection();
				Finish(tree);
				break;
			}
			EnsureAutoPreset();
			ReportProfiles("after-ensure");
			SelectPresetByName("Default");
			ReportEntries("Default");
			SelectPresetByName("SelfTest");
			ReportEntries("SelfTest");
			ProbeModdingScreenInjection();
			_waitTicks = 2;
			_step = 2;
			StartTicking(tree);
			break;
		case 2:
			if (_write && !string.IsNullOrEmpty(_applyPreset))
			{
				SelectPresetByName(_applyPreset);
				ReportEntries("before-apply(" + _applyPreset + ")");
				_panel.DebugApplyOrder();
				Log("applied preset '" + _applyPreset + "'");
				_waitTicks = 4;
				_step = 3;
				StartTicking(tree);
			}
			else
			{
				Log("no Apply requested (set LOADORDER_SELFTEST_WRITE=1 and LOADORDER_SELFTEST_APPLY=<preset>)");
				Finish(tree);
			}
			break;
		case 3:
			_panel.DebugLogLayoutCheck();
			ReportSavedState();
			ReportEntries("after-apply(" + _applyPreset + ")");
			Finish(tree);
			break;
		default:
			Finish(tree);
			break;
		}
	}

	private static void ProbeModdingScreenInjection()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen");
			if (type == null)
			{
				Log("injection: NModdingScreen type NOT FOUND (patch cannot bind)");
				return;
			}
			MethodInfo methodInfo = AccessTools.Method(type, "Create", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Log("injection: NModdingScreen.Create() NOT FOUND");
				return;
			}
			object? obj = methodInfo.Invoke(null, null);
			Node val = (Node)((obj is Node) ? obj : null);
			if (val == null)
			{
				Log("injection: Create() returned null (scene load failed - expected in some headless runs)");
				return;
			}
			((Node)((SceneTree)Engine.GetMainLoop()).Root).AddChild(val, false, (InternalMode)0);
			Node val2 = val.FindChild("LoadOrderManager_OpenButton", true, false);
			Node val3 = val.FindChild("LoadOrderManager_Panel", true, false);
			Log($"injection: modding screen ready, open button present = {val2 != null}, panel present = {val3 != null}");
			val.QueueFree();
		}
		catch (Exception ex)
		{
			Log("injection: FAILED " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void ReportEntries(string stage)
	{
		IReadOnlyList<LoadOrderEntry> readOnlyList = _panel?.DebugEntries;
		if (readOnlyList == null)
		{
			Log(stage + ": no entries");
			return;
		}
		ModPreset currentProfile = ModPresetStore.CurrentProfile;
		Log($"{stage}: preset='{currentProfile.Name}' autoEnableNewMods={currentProfile.AutoEnableNewMods} known={currentProfile.KnownMods.Count} disabled={currentProfile.DisabledMods.Count} entries={readOnlyList.Count}");
		LoadOrderEntry loadOrderEntry = readOnlyList.FirstOrDefault((LoadOrderEntry e) => string.Equals(e.Id, _probeId, StringComparison.OrdinalIgnoreCase));
		if (loadOrderEntry == null)
		{
			Log(stage + ": " + _probeId + " NOT in list");
			return;
		}
		Log($"{stage}: {_probeId} known={currentProfile.KnownMods.Contains(loadOrderEntry.Id)} enabled={loadOrderEntry.IsEnabled} isNew={loadOrderEntry.IsNew}");
	}

	private static void ReportProfiles(string stage)
	{
		PresetSaveData presetSaveData = ModPresetStore.Load();
		for (int i = 0; i < presetSaveData.Profiles.Count; i++)
		{
			ModPreset modPreset = presetSaveData.Profiles[i];
			Log($"{stage}: profile[{i}] '{modPreset.Name}' autoEnableNewMods={modPreset.AutoEnableNewMods} known={modPreset.KnownMods.Count} disabled={modPreset.DisabledMods.Count}" + ((i == presetSaveData.CurrentProfileIndex) ? "  <- current" : string.Empty));
		}
	}

	private static void ReportSavedState()
	{
		try
		{
			var (source, text) = ReadSavedModList();
			if (text != null)
			{
				Log("saved: unable to read (" + text + ")");
				return;
			}
			foreach (SavedEntry item in source.Where((SavedEntry e) => e.Id == "LoadOrderManager" || string.Equals(e.Id, _probeId, StringComparison.OrdinalIgnoreCase)))
			{
				Log($"saved: {item.Id} source={item.Source} is_enabled={item.IsEnabled}");
			}
		}
		catch (Exception ex)
		{
			Log("saved: read failed: " + ex.Message);
		}
	}

	private static (List<SavedEntry> Entries, string? Error) ReadSavedModList()
	{
		List<SavedEntry> list = new List<SavedEntry>();
		if (!(GetMember(GetMember(GetMember(AccessTools.Property(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager"), "Instance")?.GetValue(null), "SettingsSave"), "ModSettings"), "ModList") is IEnumerable enumerable))
		{
			return (Entries: list, Error: "ModList unavailable");
		}
		bool flag = default(bool);
		foreach (object item in enumerable)
		{
			string text = GetMember(item, "Id")?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				string? source = GetMember(item, "Source")?.ToString() ?? "?";
				object member = GetMember(item, "IsEnabled");
				int num;
				if (member is bool)
				{
					flag = (bool)member;
					num = 1;
				}
				else
				{
					num = 0;
				}
				list.Add(new SavedEntry(text, source, (byte)((uint)num & (flag ? 1u : 0u)) != 0));
			}
		}
		return (Entries: list, Error: null);
	}

	private static void EnsureAutoPreset()
	{
		if (ModPresetStore.Load().Profiles.Any((ModPreset p) => string.Equals(p.Name, "SelfTest", StringComparison.Ordinal)))
		{
			Log("preset 'SelfTest' already exists");
			return;
		}
		_panel.DebugCreatePreset("SelfTest");
		Log("created preset 'SelfTest' (autoEnableNewMods=false expected)");
	}

	private static void SelectPresetByName(string name)
	{
		string name2 = name;
		int num = ModPresetStore.Load().Profiles.FindIndex((ModPreset p) => string.Equals(p.Name, name2, StringComparison.Ordinal));
		if (num < 0)
		{
			Log("preset '" + name2 + "' not found; skipping switch");
		}
		else
		{
			_panel.DebugSelectPreset(num);
		}
	}

	private static bool SaveManagerReady()
	{
		object obj = AccessTools.Property(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager"), "Instance")?.GetValue(null);
		if (obj == null)
		{
			return false;
		}
		object member = GetMember(obj, "SettingsSave");
		if (member == null)
		{
			return false;
		}
		return GetMember(member, "ModSettings") != null;
	}

	private static object? GetMember(object? obj, string name)
	{
		if (obj == null)
		{
			return null;
		}
		Type type = obj.GetType();
		PropertyInfo propertyInfo = AccessTools.Property(type, name);
		if (propertyInfo != null)
		{
			return propertyInfo.GetValue(obj);
		}
		return AccessTools.Field(type, name)?.GetValue(obj);
	}

	private static string? ReadEnv(string name)
	{
		try
		{
			return Environment.GetEnvironmentVariable(name);
		}
		catch
		{
			return null;
		}
	}

	private static void Log(string message)
	{
		DebugLog.Info("[SelfTest] " + message);
	}
}
