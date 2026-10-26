using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace LoadOrderManager;

internal static class LoadOrderRuntime
{
	private sealed class SaveContext
	{
		public object SaveManager { get; }

		public string SettingsPath { get; }

		public ulong? RuntimeUserId { get; }

		public ulong? PathUserId { get; }

		public SaveContext(object saveManager, string settingsPath, ulong? runtimeUserId, ulong? pathUserId)
		{
			SaveManager = saveManager;
			SettingsPath = settingsPath;
			RuntimeUserId = runtimeUserId;
			PathUserId = pathUserId;
		}
	}

	private sealed class ApplySnapshot
	{
		public string SavedAtUtc { get; set; } = string.Empty;


		public string RuntimeUserId { get; set; } = string.Empty;


		public string PathUserId { get; set; } = string.Empty;


		public string SettingsPath { get; set; } = string.Empty;


		public List<SnapshotEntry> Entries { get; set; } = new List<SnapshotEntry>();

	}

	private sealed class SnapshotEntry
	{
		public string Key { get; set; } = string.Empty;


		public bool IsEnabled { get; set; } = true;

	}

	private const string SettingsSaveFileName = "settings.save";

	private static readonly JsonSerializerOptions SnapshotJsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	private static readonly Regex UserIdPathRegex = new Regex("[\\/](?:steam|gog|epic|xbox|none)[\\/](?<id>\\d{1,20})(?:[\\/]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex InvalidSnapshotKeyCharsRegex = new Regex("[^0-9A-Za-z_-]", RegexOptions.Compiled);

	private static bool _diagnosticsLogged;

	public static void LogDiagnosticsOnce()
	{
		if (_diagnosticsLogged)
		{
			return;
		}
		_diagnosticsLogged = true;
		try
		{
			if (!TryResolveSaveContext(out SaveContext context, out string error))
			{
				DebugLog.Warn("Startup diagnostics skipped: " + error);
				return;
			}
			ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
			DebugLog.Info($"Startup diagnostics context: runtimeUserId={FormatUserId(context.RuntimeUserId)}, pathUserId={FormatUserId(context.PathUserId)}, settingsPath={context.SettingsPath}, currentTop={FormatTop(orderedKeys)}");
			if (!TryLoadApplySnapshot(context, out ApplySnapshot snapshot, out string error2))
			{
				DebugLog.Warn("Startup diagnostics failed to load snapshot: " + error2);
				return;
			}
			if (snapshot == null || snapshot.Entries.Count == 0)
			{
				DebugLog.Info("Startup diagnostics: no previous apply snapshot for current user.");
				return;
			}
			List<string> keys = (from e in snapshot.Entries
				where !string.IsNullOrWhiteSpace(e.Key)
				select e.Key).ToList();
			DebugLog.Info($"Startup diagnostics snapshot: savedAtUtc={snapshot.SavedAtUtc}, entries={snapshot.Entries.Count}, top={FormatTop(keys)}");
			if (IsSnapshotConsistent(snapshot, orderedKeys, enabledByKey, out string reason))
			{
				DebugLog.Info("Startup diagnostics: current settings match the last apply snapshot.");
				return;
			}
			DebugLog.Warn($"Startup diagnostics: detected drift from last apply snapshot. reason={reason}, snapshotTop={FormatTop(keys)}, currentTop={FormatTop(orderedKeys)}");
		}
		catch (Exception ex)
		{
			DebugLog.Error("LogDiagnosticsOnce failed.", ex);
		}
	}

	public static bool TryBuildOverwriteWarning(out string warningText)
	{
		warningText = string.Empty;
		try
		{
			if (!TryResolveSaveContext(out SaveContext context, out string error))
			{
				DebugLog.Warn("Overwrite check skipped: " + error);
				return false;
			}
			if (!TryLoadApplySnapshot(context, out ApplySnapshot snapshot, out string error2))
			{
				DebugLog.Warn("Overwrite check skipped: failed to load snapshot. " + error2);
				return false;
			}
			if (snapshot == null || snapshot.Entries.Count == 0)
			{
				return false;
			}
			ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
			if (IsSnapshotConsistent(snapshot, orderedKeys, enabledByKey, out string reason))
			{
				return false;
			}
			string text = FormatSnapshotTime(snapshot.SavedAtUtc);
			warningText = I18n.Tf("warning_overwritten", text);
			List<string> keys = (from e in snapshot.Entries
				where !string.IsNullOrWhiteSpace(e.Key)
				select e.Key).ToList();
			DebugLog.Warn($"Overwrite check detected drift. reason={reason}, snapshotTop={FormatTop(keys)}, currentTop={FormatTop(orderedKeys)}");
			return true;
		}
		catch (Exception ex)
		{
			DebugLog.Error("TryBuildOverwriteWarning failed.", ex);
			return false;
		}
	}

	public static bool TryGetOrderedEntries(out List<LoadOrderEntry> entries, out string error)
	{
		entries = new List<LoadOrderEntry>();
		error = string.Empty;
		try
		{
			Dictionary<string, LoadOrderEntry> dictionary = ReadLoadedMods();
			if (dictionary.Count == 0)
			{
				error = "No mods found in ModManager.Mods.";
				DebugLog.Warn(error);
				return false;
			}
			ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (string item in orderedKeys)
			{
				if (dictionary.TryGetValue(item, out var value))
				{
					value.IsEnabled = enabledByKey.GetValueOrDefault(item, defaultValue: true);
					entries.Add(value);
					hashSet.Add(item);
				}
			}
			foreach (KeyValuePair<string, LoadOrderEntry> item2 in dictionary)
			{
				if (!hashSet.Contains(item2.Key))
				{
					item2.Value.IsEnabled = enabledByKey.GetValueOrDefault(item2.Key, defaultValue: true);
					entries.Add(item2.Value);
				}
			}
			DebugLog.Info($"Read order success. allMods={dictionary.Count}, settingsOrder={orderedKeys.Count}, merged={entries.Count}");
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			DebugLog.Error("TryGetOrderedEntries failed.", ex);
			return false;
		}
	}

	public static bool TrySaveOrderedEntries(IReadOnlyList<LoadOrderEntry> orderedEntries, out string error)
	{
		error = string.Empty;
		try
		{
			if (!TryResolveSaveContext(out SaveContext context, out string error2))
			{
				error = error2;
				DebugLog.Error(error);
				return false;
			}
			if (!ValidateCurrentUserScope(context, out string error3))
			{
				error = error3;
				DebugLog.Error(error);
				return false;
			}
			object saveManager = context.SaveManager;
			object memberValue = GetMemberValue(saveManager, "SettingsSave");
			if (memberValue == null)
			{
				error = "SaveManager.SettingsSave is null.";
				DebugLog.Error(error);
				return false;
			}
			Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Modding.ModSettings");
			Type type2 = AccessTools.TypeByName("MegaCrit.Sts2.Core.Modding.SettingsSaveMod");
			Type type3 = AccessTools.TypeByName("MegaCrit.Sts2.Core.Modding.ModSource");
			if (type == null || type2 == null || type3 == null)
			{
				error = "Mod settings runtime types not found.";
				DebugLog.Error(error);
				return false;
			}
			object obj = GetMemberValue(memberValue, "ModSettings");
			if (obj == null)
			{
				obj = Activator.CreateInstance(type);
				SetMemberValue(memberValue, "ModSettings", obj);
			}
			if (obj == null)
			{
				error = "Failed to create ModSettings object.";
				DebugLog.Error(error);
				return false;
			}
			IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type2));
			if (list == null)
			{
				error = "Failed to create mod list.";
				DebugLog.Error(error);
				return false;
			}
			DebugLog.Info($"Saving manual order with {orderedEntries.Count} entries. runtimeUserId={FormatUserId(context.RuntimeUserId)}, pathUserId={FormatUserId(context.PathUserId)}, settingsPath={context.SettingsPath}, top={FormatTop(orderedEntries)}");
			foreach (LoadOrderEntry orderedEntry in orderedEntries)
			{
				object obj2 = Activator.CreateInstance(type2);
				if (obj2 != null)
				{
					SetMemberValue(obj2, "Id", orderedEntry.Id);
					SetMemberValue(obj2, "Source", Enum.ToObject(type3, orderedEntry.Source));
					SetMemberValue(obj2, "IsEnabled", orderedEntry.IsEnabled);
					list.Add(obj2);
				}
			}
			SetMemberValue(obj, "ModList", list);
			MethodInfo methodInfo = AccessTools.Method(ResolveCoreType("MegaCrit.Sts2.Core.Saves.SaveManager"), "SaveSettings", Type.EmptyTypes, (Type[])null);
			if (methodInfo == null)
			{
				error = "SaveManager.SaveSettings() not found.";
				DebugLog.Error(error);
				return false;
			}
			methodInfo.Invoke(saveManager, null);
			List<string> list2 = orderedEntries.Select((LoadOrderEntry e) => e.Key).ToList();
			ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> _);
			if (!IsOrderMonotonic(list2, orderedKeys))
			{
				error = "Order verification failed after save. Check log for details.";
				DebugLog.Warn("Order verification failed. expectedTop=" + FormatTop(list2) + ", persistedTop=" + FormatTop(orderedKeys));
				return false;
			}
			if (!TryWriteApplySnapshot(context, orderedEntries, out string error4))
			{
				DebugLog.Warn("SaveSettings succeeded but snapshot write failed: " + error4);
			}
			DebugLog.Info("SaveSettings succeeded. Persisted top=" + FormatTop(orderedKeys));
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			DebugLog.Error("TrySaveOrderedEntries failed.", ex);
			return false;
		}
	}

	private static Dictionary<string, LoadOrderEntry> ReadLoadedMods()
	{
		Dictionary<string, LoadOrderEntry> dictionary = new Dictionary<string, LoadOrderEntry>(StringComparer.Ordinal);
		Type type = ResolveCoreType("MegaCrit.Sts2.Core.Modding.ModManager");
		if (type == null)
		{
			DebugLog.Warn("ReadLoadedMods: ModManager type not found.");
			return dictionary;
		}
		if (TryCollectModsFromMember(type, "Mods", dictionary))
		{
			DebugLog.Info($"ReadLoadedMods: collected {dictionary.Count} entries from ModManager.Mods.");
			return dictionary;
		}
		if (TryCollectModsFromMember(type, "_mods", dictionary))
		{
			DebugLog.Warn($"ReadLoadedMods: Mods empty, fallback to ModManager._mods ({dictionary.Count}).");
			return dictionary;
		}
		if (TryCollectModsFromSettings(dictionary))
		{
			DebugLog.Warn($"ReadLoadedMods: ModManager lists empty, fallback to settings.mod_list ({dictionary.Count}).");
		}
		return dictionary;
	}

	private static void ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey)
	{
		orderedKeys = new List<string>();
		enabledByKey = new Dictionary<string, bool>(StringComparer.Ordinal);
		Type type = ResolveCoreType("MegaCrit.Sts2.Core.Saves.SaveManager");
		if (type == null || !(GetMemberValue(GetMemberValue(GetMemberValue(AccessTools.Property(type, "Instance")?.GetValue(null), "SettingsSave"), "ModSettings"), "ModList") is IEnumerable enumerable))
		{
			return;
		}
		foreach (object item in enumerable)
		{
			string text = AsString(GetMemberValue(item, "Id"));
			if (!string.IsNullOrWhiteSpace(text))
			{
				int source = ToInt(GetMemberValue(item, "Source"));
				bool value = ToBool(GetMemberValue(item, "IsEnabled"), fallback: true);
				string text2 = LoadOrderEntry.BuildKey(text, source);
				orderedKeys.Add(text2);
				enabledByKey[text2] = value;
			}
		}
	}

	private static bool TryResolveSaveContext(out SaveContext context, out string error)
	{
		context = null;
		error = string.Empty;
		Type type = ResolveCoreType("MegaCrit.Sts2.Core.Saves.SaveManager");
		if (type == null)
		{
			error = "SaveManager type not found.";
			return false;
		}
		object obj = AccessTools.Property(type, "Instance")?.GetValue(null);
		if (obj == null)
		{
			error = "SaveManager.Instance is null.";
			return false;
		}
		object memberValue = GetMemberValue(obj, "_saveStore");
		if (memberValue == null)
		{
			error = "SaveManager._saveStore is null.";
			return false;
		}
		MethodInfo methodInfo = AccessTools.Method(memberValue.GetType(), "GetFullPath", new Type[1] { typeof(string) }, (Type[])null);
		if (methodInfo == null)
		{
			error = "GetFullPath(string) not found on save store " + memberValue.GetType().FullName + ".";
			return false;
		}
		string text = AsString(methodInfo.Invoke(memberValue, new object[1] { "settings.save" }));
		if (string.IsNullOrWhiteSpace(text))
		{
			error = "Could not resolve full settings.save path from save store.";
			return false;
		}
		ulong? runtimeUserId = TryResolveRuntimeUserId();
		ulong? pathUserId = TryExtractPathUserId(text);
		context = new SaveContext(obj, text, runtimeUserId, pathUserId);
		return true;
	}

	private static bool ValidateCurrentUserScope(SaveContext context, out string error)
	{
		error = string.Empty;
		if (!context.RuntimeUserId.HasValue)
		{
			error = "Current runtime user id is unavailable. Refusing save to avoid cross-account overwrite. settingsPath=" + context.SettingsPath;
			return false;
		}
		if (!context.PathUserId.HasValue)
		{
			if (ContainsUserIdSegment(context.SettingsPath, context.RuntimeUserId.Value))
			{
				return true;
			}
			error = $"Could not verify settings path belongs to runtime user {context.RuntimeUserId}. path={context.SettingsPath}";
			return false;
		}
		if (context.RuntimeUserId.Value != context.PathUserId.Value)
		{
			error = $"Refusing save: runtime user id {context.RuntimeUserId} does not match settings path user id {context.PathUserId}. path={context.SettingsPath}";
			return false;
		}
		return true;
	}

	private static ulong? TryResolveRuntimeUserId()
	{
		try
		{
			Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Platform.PlatformUtil");
			if (type == null)
			{
				return null;
			}
			object obj = AccessTools.Property(type, "PrimaryPlatform")?.GetValue(null);
			if (obj == null)
			{
				return null;
			}
			MethodInfo methodInfo = AccessTools.Method(type, "GetLocalPlayerId", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				return null;
			}
			return ToUlong(methodInfo.Invoke(null, new object[1] { obj }));
		}
		catch
		{
			return null;
		}
	}

	private static Type? ResolveCoreType(string fullTypeName)
	{
		Type type = ResolveCoreAssembly()?.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
		if (type != null)
		{
			return type;
		}
		Type type2 = AccessTools.TypeByName(fullTypeName);
		if (type2 != null)
		{
			DebugLog.Warn("ResolveCoreType fallback used for " + fullTypeName + ": " + type2.Assembly.FullName);
		}
		return type2;
	}

	private static Assembly? ResolveCoreAssembly()
	{
		return AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager")?.Assembly;
	}

	private static bool TryCollectModsFromMember(Type modManagerType, string memberName, Dictionary<string, LoadOrderEntry> dict)
	{
		object obj = null;
		PropertyInfo propertyInfo = AccessTools.Property(modManagerType, memberName);
		if (propertyInfo != null)
		{
			obj = propertyInfo.GetValue(null);
		}
		else
		{
			FieldInfo fieldInfo = AccessTools.Field(modManagerType, memberName);
			if (fieldInfo != null)
			{
				obj = fieldInfo.GetValue(null);
			}
		}
		if (!(obj is IEnumerable modList))
		{
			return false;
		}
		return TryCollectMods(modList, dict);
	}

	private static bool TryCollectMods(IEnumerable modList, Dictionary<string, LoadOrderEntry> dict)
	{
		int num = 0;
		foreach (object mod in modList)
		{
			object memberValue = GetMemberValue(mod, "manifest");
			string text = AsString(GetMemberValue(memberValue, "id"));
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			string text2 = AsString(GetMemberValue(memberValue, "name"));
			int source = ToInt(GetMemberValue(mod, "modSource"));
			string key = LoadOrderEntry.BuildKey(text, source);
			bool flag = !dict.ContainsKey(key);
			LoadOrderEntry loadOrderEntry = new LoadOrderEntry(text, text2 ?? text, source, isEnabled: true);
			if (GetMemberValue(memberValue, "dependencies") is IEnumerable enumerable)
			{
				foreach (object item in enumerable)
				{
					string text3 = AsString(GetMemberValue(item, "id"));
					if (!string.IsNullOrWhiteSpace(text3))
					{
						loadOrderEntry.Dependencies.Add(text3);
					}
				}
			}
			dict[key] = loadOrderEntry;
			if (flag)
			{
				num++;
			}
		}
		return num > 0;
	}

	private static bool TryCollectModsFromSettings(Dictionary<string, LoadOrderEntry> dict)
	{
		ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
		if (orderedKeys.Count == 0)
		{
			return false;
		}
		int num = 0;
		foreach (string item in orderedKeys)
		{
			if (TryParseEntryKey(item, out string id, out int source) && !dict.ContainsKey(item))
			{
				dict[item] = new LoadOrderEntry(id, id, source, enabledByKey.GetValueOrDefault(item, defaultValue: true));
				num++;
			}
		}
		return num > 0;
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
		string text = key.Substring(0, num);
		int num2 = num + 2;
		string s = key.Substring(num2, key.Length - num2);
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

	private static ulong? TryExtractPathUserId(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		Match match = UserIdPathRegex.Match(path);
		if (!match.Success)
		{
			return null;
		}
		if (!ulong.TryParse(match.Groups["id"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return result;
	}

	private static bool ContainsUserIdSegment(string path, ulong userId)
	{
		string value = $"/{userId}/";
		string value2 = $"\\{userId}\\";
		if (!path.Contains(value, StringComparison.OrdinalIgnoreCase))
		{
			return path.Contains(value2, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string BuildSnapshotPath(SaveContext context)
	{
		string directoryName = Path.GetDirectoryName(DebugLog.LogPath);
		string path = Path.Combine(string.IsNullOrWhiteSpace(directoryName) ? "." : directoryName, "state");
		string input = context.RuntimeUserId?.ToString(CultureInfo.InvariantCulture) ?? context.PathUserId?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
		string text = InvalidSnapshotKeyCharsRegex.Replace(input, "_");
		return Path.Combine(path, "last_apply_" + text + ".json");
	}

	private static bool TryWriteApplySnapshot(SaveContext context, IReadOnlyList<LoadOrderEntry> orderedEntries, out string error)
	{
		error = string.Empty;
		try
		{
			ApplySnapshot applySnapshot = new ApplySnapshot
			{
				SavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
				RuntimeUserId = FormatUserId(context.RuntimeUserId),
				PathUserId = FormatUserId(context.PathUserId),
				SettingsPath = context.SettingsPath,
				Entries = orderedEntries.Select((LoadOrderEntry e) => new SnapshotEntry
				{
					Key = e.Key,
					IsEnabled = e.IsEnabled
				}).ToList()
			};
			string text = BuildSnapshotPath(context);
			string directoryName = Path.GetDirectoryName(text);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			string contents = JsonSerializer.Serialize(applySnapshot, SnapshotJsonOptions);
			File.WriteAllText(text, contents);
			List<string> keys = (from e in applySnapshot.Entries
				where !string.IsNullOrWhiteSpace(e.Key)
				select e.Key).ToList();
			DebugLog.Info($"Apply snapshot saved. path={text}, entries={applySnapshot.Entries.Count}, top={FormatTop(keys)}");
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			DebugLog.Error("TryWriteApplySnapshot failed.", ex);
			return false;
		}
	}

	private static bool TryLoadApplySnapshot(SaveContext context, out ApplySnapshot? snapshot, out string error)
	{
		snapshot = null;
		error = string.Empty;
		try
		{
			string text = BuildSnapshotPath(context);
			if (!File.Exists(text))
			{
				return true;
			}
			ApplySnapshot applySnapshot = JsonSerializer.Deserialize<ApplySnapshot>(File.ReadAllText(text));
			if (applySnapshot == null)
			{
				error = "Snapshot json parse returned null. path=" + text;
				return false;
			}
			ApplySnapshot applySnapshot2 = applySnapshot;
			if (applySnapshot2.Entries == null)
			{
				List<SnapshotEntry> list2 = (applySnapshot2.Entries = new List<SnapshotEntry>());
			}
			snapshot = applySnapshot;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			DebugLog.Error("TryLoadApplySnapshot failed.", ex);
			return false;
		}
	}

	private static bool IsSnapshotConsistent(ApplySnapshot snapshot, IReadOnlyList<string> currentOrder, IReadOnlyDictionary<string, bool> currentEnabledByKey, out string reason)
	{
		List<SnapshotEntry> list = snapshot.Entries.Where((SnapshotEntry e) => !string.IsNullOrWhiteSpace(e.Key)).ToList();
		List<string> list2 = list.Select((SnapshotEntry e) => e.Key).ToList();
		if (list2.Count == 0)
		{
			reason = "snapshot_empty";
			return true;
		}
		if (!IsOrderMonotonic(list2, currentOrder))
		{
			reason = "order_changed";
			return false;
		}
		foreach (SnapshotEntry item in list)
		{
			if (!currentEnabledByKey.TryGetValue(item.Key, out var value))
			{
				reason = "missing:" + item.Key;
				return false;
			}
			if (value != item.IsEnabled)
			{
				reason = "enabled_changed:" + item.Key;
				return false;
			}
		}
		reason = "matched";
		return true;
	}

	private static string FormatSnapshotTime(string savedAtUtc)
	{
		if (DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			return result.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}
		return savedAtUtc;
	}

	private static string FormatUserId(ulong? userId)
	{
		return userId?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
	}

	private static object? GetMemberValue(object? obj, string name)
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

	private static void SetMemberValue(object? obj, string name, object? value)
	{
		if (obj != null)
		{
			Type type = obj.GetType();
			PropertyInfo propertyInfo = AccessTools.Property(type, name);
			if (propertyInfo != null)
			{
				propertyInfo.SetValue(obj, value);
			}
			else
			{
				AccessTools.Field(type, name)?.SetValue(obj, value);
			}
		}
	}

	private static string? AsString(object? obj)
	{
		if (obj != null)
		{
			if (obj is string result)
			{
				return result;
			}
			return obj.ToString();
		}
		return null;
	}

	private static ulong? ToUlong(object? obj)
	{
		if (obj != null)
		{
			if (!(obj is ulong value))
			{
				if (!(obj is long num))
				{
					if (!(obj is int num2))
					{
						if (obj is Enum value2)
						{
							return (ulong)Convert.ToInt64(value2, CultureInfo.InvariantCulture);
						}
					}
					else
					{
						int num3 = num2;
						if (num3 >= 0)
						{
							return (ulong)num3;
						}
					}
				}
				else
				{
					long num4 = num;
					if (num4 >= 0)
					{
						return (ulong)num4;
					}
				}
				if (ulong.TryParse(obj.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var result))
				{
					return result;
				}
				return null;
			}
			return value;
		}
		return null;
	}

	private static int ToInt(object? obj)
	{
		if (obj != null)
		{
			if (!(obj is int result))
			{
				if (!(obj is long num))
				{
					if (obj is Enum value)
					{
						return Convert.ToInt32(value, CultureInfo.InvariantCulture);
					}
					if (int.TryParse(obj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
					{
						return result2;
					}
					return 0;
				}
				return (int)num;
			}
			return result;
		}
		return 0;
	}

	private static bool ToBool(object? obj, bool fallback)
	{
		if (obj != null)
		{
			if (obj is bool result)
			{
				return result;
			}
			if (bool.TryParse(obj.ToString(), out var result2))
			{
				return result2;
			}
			return fallback;
		}
		return fallback;
	}

	private static bool IsOrderMonotonic(IReadOnlyList<string> expectedOrder, IReadOnlyList<string> persistedOrder)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < persistedOrder.Count; i++)
		{
			dictionary[persistedOrder[i]] = i;
		}
		int num = -1;
		foreach (string item in expectedOrder)
		{
			if (dictionary.TryGetValue(item, out var value))
			{
				if (value < num)
				{
					return false;
				}
				num = value;
			}
		}
		return true;
	}

	private static string FormatTop(IReadOnlyList<LoadOrderEntry> entries)
	{
		int num = Math.Min(entries.Count, 8);
		if (num == 0)
		{
			return "<empty>";
		}
		IEnumerable<string> values = from e in entries.Take(num)
			select $"{e.Id}({e.Source})";
		return string.Join(" > ", values);
	}

	private static string FormatTop(IReadOnlyList<string> keys)
	{
		int num = Math.Min(keys.Count, 8);
		if (num == 0)
		{
			return "<empty>";
		}
		return string.Join(" > ", keys.Take(num));
	}
}
