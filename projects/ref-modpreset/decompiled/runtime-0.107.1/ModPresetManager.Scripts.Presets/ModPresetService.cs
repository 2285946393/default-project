using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;

namespace ModPresetManager.Scripts.Presets;

internal static class ModPresetService
{
	private readonly record struct ModKey(string Id, ModSource Source)
	{
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			builder.Append("Id = ");
			builder.Append((object?)Id);
			builder.Append(", Source = ");
			ModSource source = Source;
			builder.Append(((object)(ModSource)(ref source)).ToString());
			return true;
		}

		[CompilerGenerated]
		public void Deconstruct(out string Id, out ModSource Source)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected I4, but got Unknown
			Id = this.Id;
			Source = (ModSource)(int)this.Source;
		}
	}

	private readonly record struct ModSnapshotKey(string Id, ModSource Source, string Version)
	{
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			builder.Append("Id = ");
			builder.Append((object?)Id);
			builder.Append(", Source = ");
			ModSource source = Source;
			builder.Append(((object)(ModSource)(ref source)).ToString());
			builder.Append(", Version = ");
			builder.Append((object?)Version);
			return true;
		}

		[CompilerGenerated]
		public void Deconstruct(out string Id, out ModSource Source, out string Version)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected I4, but got Unknown
			Id = this.Id;
			Source = (ModSource)(int)this.Source;
			Version = this.Version;
		}
	}

	private const int MaxPresetMods = 2048;

	internal const string RequiredModId = "ModPresetManager";

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = false
	};

	public static IReadOnlyList<CurrentModEntry> CaptureLoaded()
	{
		return (from mod in ModManager.Mods
			where (int)mod.state == 1
			where !string.IsNullOrWhiteSpace(mod.manifest?.id)
			select ToCurrentEntry(mod, enabled: true)).ToList();
	}

	public static IReadOnlyList<CurrentModEntry> CaptureSaved()
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Invalid comparison between Unknown and I4
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Invalid comparison between Unknown and I4
		Dictionary<ModKey, Mod> dictionary = (from mod in ModManager.Mods
			where !string.IsNullOrWhiteSpace(mod.manifest?.id)
			group mod by new ModKey(mod.manifest.id, mod.modSource)).ToDictionary((IGrouping<ModKey, Mod> group) => group.Key, (IGrouping<ModKey, Mod> group) => SelectPreferred(group));
		ModSettings modSettings = SaveManager.Instance.SettingsSave.ModSettings;
		List<SettingsSaveMod> obj = ((modSettings != null) ? modSettings.ModList : null) ?? new List<SettingsSaveMod>();
		List<CurrentModEntry> list = new List<CurrentModEntry>();
		HashSet<ModKey> hashSet = new HashSet<ModKey>();
		foreach (SettingsSaveMod item2 in obj)
		{
			ModKey modKey = new ModKey(item2.Id, item2.Source);
			if (hashSet.Add(modKey) && dictionary.TryGetValue(modKey, out var value))
			{
				list.Add(ToCurrentEntry(value, item2.IsEnabled));
			}
		}
		foreach (var (item, val2) in dictionary)
		{
			if (hashSet.Add(item))
			{
				ModLoadState state = val2.state;
				bool enabled = (int)state != 3 && (int)state != 4;
				list.Add(ToCurrentEntry(val2, enabled));
			}
		}
		return list;
	}

	public static ModConfigurationComparison CompareLoadedAndSaved()
	{
		IReadOnlyList<CurrentModEntry> readOnlyList = CaptureLoaded();
		IReadOnlyList<CurrentModEntry> readOnlyList2 = CaptureSaved();
		List<ModSnapshotKey> first = readOnlyList.Select((CurrentModEntry entry) => new ModSnapshotKey(entry.Id, entry.Source, entry.Version)).ToList();
		List<ModSnapshotKey> second = (from entry in readOnlyList2
			where entry.IsEnabled
			select new ModSnapshotKey(entry.Id, entry.Source, entry.Version)).ToList();
		return new ModConfigurationComparison
		{
			Loaded = readOnlyList,
			Saved = readOnlyList2,
			Matches = first.SequenceEqual(second)
		};
	}

	public static PresetExportResult Export(ModPresetScope scope)
	{
		List<CurrentModEntry> list = (from entry in CaptureSaved()
			where entry.IsEnabled
			select entry).ToList();
		if (!list.Any((CurrentModEntry entry) => string.Equals(entry.Id, "ModPresetManager", StringComparison.Ordinal)))
		{
			CurrentModEntry currentModEntry = CaptureLoaded().FirstOrDefault((CurrentModEntry entry) => string.Equals(entry.Id, "ModPresetManager", StringComparison.Ordinal));
			if (currentModEntry != null)
			{
				list.Add(currentModEntry);
			}
		}
		HashSet<ModKey> knownKeys = list.Select((CurrentModEntry entry) => new ModKey(entry.Id, entry.Source)).ToHashSet();
		ModSettings modSettings = SaveManager.Instance.SettingsSave.ModSettings;
		SettingsSaveMod val = ((modSettings != null) ? ((IEnumerable<SettingsSaveMod>)modSettings.ModList).FirstOrDefault((Func<SettingsSaveMod, bool>)((SettingsSaveMod entry) => entry.IsEnabled && !knownKeys.Contains(new ModKey(entry.Id, entry.Source)))) : null);
		if (val != null)
		{
			return new PresetExportResult
			{
				UnavailableModId = val.Id
			};
		}
		IGrouping<string, CurrentModEntry> grouping = list.GroupBy<CurrentModEntry, string>((CurrentModEntry entry) => entry.Id, StringComparer.Ordinal).FirstOrDefault((IGrouping<string, CurrentModEntry> group) => group.Count() > 1);
		if (grouping != null)
		{
			return new PresetExportResult
			{
				DuplicateModId = grouping.Key
			};
		}
		IReadOnlySet<string> multiplayerIds = ((scope == ModPresetScope.Multiplayer) ? FindMultiplayerDependencyClosure(list) : null);
		List<CurrentModEntry> source = list.Where((CurrentModEntry entry) => multiplayerIds == null || multiplayerIds.Contains(entry.Id)).ToList();
		CurrentModEntry currentModEntry2 = source.FirstOrDefault((CurrentModEntry entry) => string.IsNullOrWhiteSpace(entry.Version));
		if (currentModEntry2 != null)
		{
			return new PresetExportResult
			{
				InvalidModId = currentModEntry2.Id
			};
		}
		ModPresetDocument modPresetDocument = new ModPresetDocument();
		ReleaseInfo releaseInfo = ReleaseInfoManager.Instance.ReleaseInfo;
		modPresetDocument.GameVersion = ((releaseInfo != null) ? releaseInfo.Version : null);
		modPresetDocument.Mods = source.Select((CurrentModEntry entry) => new ModPresetEntry
		{
			Id = entry.Id,
			Version = entry.Version,
			Source = (((int)entry.Source == 2) ? "workshop" : "local"),
			WorkshopId = (((int)entry.Source == 2) ? entry.WorkshopId : null),
			AffectsGameplay = entry.AffectsGameplay
		}).ToList();
		ModPresetDocument modPresetDocument2 = modPresetDocument;
		return new PresetExportResult
		{
			Success = true,
			Text = JsonSerializer.Serialize(modPresetDocument2, JsonOptions),
			ModCount = modPresetDocument2.Mods.Count
		};
	}

	public static bool TryParse(string? text, out ModPresetDocument? document, out PresetParseError error)
	{
		document = null;
		error = PresetParseError.None;
		if (string.IsNullOrWhiteSpace(text))
		{
			error = PresetParseError.Empty;
			return false;
		}
		try
		{
			document = JsonSerializer.Deserialize<ModPresetDocument>(text, JsonOptions);
		}
		catch (JsonException)
		{
			error = PresetParseError.InvalidJson;
			return false;
		}
		if (document == null)
		{
			error = PresetParseError.InvalidJson;
			return false;
		}
		if (!ModPresetDocument.IsSupportedFormat(document.Format))
		{
			error = PresetParseError.UnsupportedFormat;
			return false;
		}
		if (document.Schema != 1)
		{
			error = PresetParseError.UnsupportedSchema;
			return false;
		}
		if (document.Mods == null || document.Mods.Count > 2048 || document.Mods.Any((ModPresetEntry entry) => entry == null || string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Version) || (!string.Equals(entry.Source, "local", StringComparison.OrdinalIgnoreCase) && !string.Equals(entry.Source, "workshop", StringComparison.OrdinalIgnoreCase))))
		{
			error = PresetParseError.InvalidEntry;
			return false;
		}
		foreach (ModPresetEntry mod in document.Mods)
		{
			mod.Id = mod.Id.Trim();
			mod.Version = mod.Version.Trim();
			mod.Source = ((mod.ParsedSource == ModPresetSource.Workshop) ? "workshop" : "local");
			if (mod.WorkshopId == 0)
			{
				mod.WorkshopId = null;
			}
		}
		if (document.Mods.GroupBy<ModPresetEntry, string>((ModPresetEntry entry) => entry.Id, StringComparer.Ordinal).Any((IGrouping<string, ModPresetEntry> group) => group.Count() > 1))
		{
			error = PresetParseError.DuplicateMod;
			return false;
		}
		document.Format = "mpm-preset";
		return true;
	}

	public static PresetImportAnalysis Analyze(ModPresetDocument preset)
	{
		List<PresetImportItem> list = new List<PresetImportItem>(preset.Mods.Count);
		for (int i = 0; i < preset.Mods.Count; i++)
		{
			list.Add(AnalyzeEntry(preset.Mods[i], i));
		}
		return new PresetImportAnalysis
		{
			Preset = preset,
			Items = list
		};
	}

	public static PresetApplyPlan CreateApplyPlan(PresetImportAnalysis analysis, PresetImportMode importMode)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Invalid comparison between Unknown and I4
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Invalid comparison between Unknown and I4
		//IL_02a0: Expected O, but got Unknown
		if (!analysis.CanApply)
		{
			throw new ArgumentException("Cannot create an apply plan for an unresolved preset.", "analysis");
		}
		ModSettings modSettings = SaveManager.Instance.SettingsSave.ModSettings;
		ModSettings val = (ModSettings)(((object)modSettings) ?? ((object)new ModSettings
		{
			PlayerAgreedToModLoading = ModManager.PlayerAgreedToModLoading
		}));
		List<SettingsSaveMod> modList = val.ModList;
		List<SettingsSaveMod> list = new List<SettingsSaveMod>();
		HashSet<ModKey> hashSet = new HashSet<ModKey>();
		HashSet<string> targetIds = analysis.Items.Select((PresetImportItem item) => item.Entry.Id).ToHashSet<string>(StringComparer.Ordinal);
		bool preserveNonGameplayRemainder = importMode == PresetImportMode.Compatible;
		foreach (PresetImportItem item3 in analysis.Items)
		{
			Mod matchedMod = item3.MatchedMod;
			ModKey item2 = new ModKey(item3.Entry.Id, matchedMod.modSource);
			if (hashSet.Add(item2))
			{
				list.Add(new SettingsSaveMod
				{
					Id = item3.Entry.Id,
					Source = matchedMod.modSource,
					IsEnabled = true
				});
			}
		}
		Dictionary<ModKey, Mod> dictionary = (from mod in ModManager.Mods
			where !string.IsNullOrWhiteSpace(mod.manifest?.id)
			group mod by new ModKey(mod.manifest.id, mod.modSource)).ToDictionary((IGrouping<ModKey, Mod> group) => group.Key, (IGrouping<ModKey, Mod> group) => SelectPreferred(group));
		List<SettingsSaveMod> list2 = new List<SettingsSaveMod>();
		HashSet<ModKey> hashSet2 = new HashSet<ModKey>();
		foreach (SettingsSaveMod item4 in modList)
		{
			ModKey modKey = new ModKey(item4.Id, item4.Source);
			if (!hashSet.Contains(modKey) && hashSet2.Add(modKey))
			{
				list2.Add(CreateRemainderEntry(item4, modKey, dictionary, targetIds, preserveNonGameplayRemainder));
			}
		}
		foreach (var (modKey3, val3) in dictionary)
		{
			if (!hashSet.Contains(modKey3) && hashSet2.Add(modKey3))
			{
				SettingsSaveMod val4 = new SettingsSaveMod(val3);
				ModLoadState state = val3.state;
				val4.IsEnabled = (int)state != 3 && (int)state != 4;
				SettingsSaveMod existing = val4;
				list2.Add(CreateRemainderEntry(existing, modKey3, dictionary, targetIds, preserveNonGameplayRemainder));
			}
		}
		Dictionary<string, List<SettingsSaveMod>> dictionary2 = list2.Where((SettingsSaveMod entry) => targetIds.Contains(entry.Id)).GroupBy<SettingsSaveMod, string>((SettingsSaveMod entry) => entry.Id, StringComparer.Ordinal).ToDictionary<IGrouping<string, SettingsSaveMod>, string, List<SettingsSaveMod>>((IGrouping<string, SettingsSaveMod> group) => group.Key, (IGrouping<string, SettingsSaveMod> group) => group.ToList(), StringComparer.Ordinal);
		List<SettingsSaveMod> list3 = new List<SettingsSaveMod>(list.Count + list2.Count);
		foreach (SettingsSaveMod item5 in list)
		{
			if (dictionary2.TryGetValue(item5.Id, out var value))
			{
				list3.AddRange(value);
			}
			list3.Add(item5);
		}
		list3.AddRange(list2.Where((SettingsSaveMod entry) => !targetIds.Contains(entry.Id)));
		return new PresetApplyPlan
		{
			ModSettings = val,
			PreviousModSettings = modSettings,
			PreviousOrder = modList,
			FinalOrder = list3,
			ChangedCount = CountChanges(modList, list3)
		};
	}

	public static PresetApplyResult Apply(PresetImportAnalysis analysis, PresetImportMode importMode)
	{
		if (!analysis.CanApply)
		{
			return new PresetApplyResult
			{
				Error = "unresolved"
			};
		}
		PresetApplyPlan presetApplyPlan = CreateApplyPlan(analysis, importMode);
		SettingsSave settingsSave = SaveManager.Instance.SettingsSave;
		try
		{
			presetApplyPlan.ModSettings.ModList = presetApplyPlan.FinalOrder;
			settingsSave.ModSettings = presetApplyPlan.ModSettings;
			SaveManager.Instance.SaveSettings();
		}
		catch (Exception value)
		{
			presetApplyPlan.ModSettings.ModList = presetApplyPlan.PreviousOrder;
			settingsSave.ModSettings = presetApplyPlan.PreviousModSettings;
			Log.Error($"Failed to apply imported mod preset: {value}", 2);
			return new PresetApplyResult
			{
				Error = "save_failed"
			};
		}
		return new PresetApplyResult
		{
			Success = true,
			ChangedCount = presetApplyPlan.ChangedCount
		};
	}

	public static ulong? GetWorkshopId(Mod mod)
	{
		return ParseWorkshopIdFromPath(mod.path);
	}

	private static IReadOnlySet<string> FindMultiplayerDependencyClosure(IReadOnlyList<CurrentModEntry> enabled)
	{
		Dictionary<string, CurrentModEntry> dictionary = enabled.GroupBy<CurrentModEntry, string>((CurrentModEntry entry) => entry.Id, StringComparer.Ordinal).ToDictionary<IGrouping<string, CurrentModEntry>, string, CurrentModEntry>((IGrouping<string, CurrentModEntry> group) => group.Key, (IGrouping<string, CurrentModEntry> group) => group.First(), StringComparer.Ordinal);
		HashSet<string> hashSet = (from entry in enabled
			where entry.AffectsGameplay
			select entry.Id).ToHashSet<string>(StringComparer.Ordinal);
		if (dictionary.ContainsKey("ModPresetManager"))
		{
			hashSet.Add("ModPresetManager");
		}
		Queue<string> queue = new Queue<string>(hashSet);
		while (queue.Count > 0)
		{
			string key = queue.Dequeue();
			if (!dictionary.TryGetValue(key, out var value))
			{
				continue;
			}
			foreach (ModDependency item in value.Mod.manifest?.dependencies ?? new List<ModDependency>())
			{
				if (!string.IsNullOrWhiteSpace(item.id) && dictionary.ContainsKey(item.id) && hashSet.Add(item.id))
				{
					queue.Enqueue(item.id);
				}
			}
		}
		return hashSet;
	}

	private static PresetImportItem AnalyzeEntry(ModPresetEntry entry, int index)
	{
		ModPresetEntry entry2 = entry;
		List<Mod> list = ModManager.Mods.Where((Mod mod) => string.Equals(mod.manifest?.id, entry2.Id, StringComparison.Ordinal)).ToList();
		List<Mod> source = list.Where((Mod mod) => string.Equals(mod.manifest?.version, entry2.Version, StringComparison.Ordinal)).ToList();
		if (entry2.ParsedSource == ModPresetSource.Local)
		{
			List<Mod> exactMatches = source.Where((Mod mod) => (int)mod.modSource == 1).ToList();
			return ResolveMatches(entry2, index, exactMatches, list);
		}
		List<Mod> list2 = (from mod in source
			where (int)mod.modSource == 2
			where !entry2.WorkshopId.HasValue || GetWorkshopId(mod) == entry2.WorkshopId
			select mod).ToList();
		if (list2.Count > 0)
		{
			return ResolveMatches(entry2, index, list2, list);
		}
		List<Mod> list3 = source.Where((Mod mod) => (int)mod.modSource == 1).ToList();
		if (list3.Count == 1)
		{
			return new PresetImportItem
			{
				Order = index,
				Entry = entry2,
				Status = PresetImportStatus.LocalFallback,
				MatchedMod = list3[0],
				CurrentVersion = list3[0].manifest?.version
			};
		}
		if (list3.Count > 1)
		{
			return CreateUnresolved(entry2, index, PresetImportStatus.Ambiguous, list3);
		}
		if (entry2.WorkshopId.HasValue && list.Any((Mod mod) => (int)mod.modSource == 2 && GetWorkshopId(mod) == entry2.WorkshopId))
		{
			return CreateUnresolved(entry2, index, PresetImportStatus.VersionMismatch, list);
		}
		ulong? workshopId = entry2.WorkshopId;
		if (workshopId.HasValue && workshopId.GetValueOrDefault() != 0)
		{
			return CreateUnresolved(entry2, index, PresetImportStatus.DownloadRequired, list);
		}
		return CreateUnresolved(entry2, index, (list.Count > 0) ? PresetImportStatus.VersionMismatch : PresetImportStatus.Missing, list);
	}

	private static PresetImportItem ResolveMatches(ModPresetEntry entry, int index, IReadOnlyList<Mod> exactMatches, IReadOnlyList<Mod> idMatches)
	{
		if (exactMatches.Count == 1)
		{
			return new PresetImportItem
			{
				Order = index,
				Entry = entry,
				Status = PresetImportStatus.Exact,
				MatchedMod = exactMatches[0],
				CurrentVersion = exactMatches[0].manifest?.version
			};
		}
		if (exactMatches.Count > 1)
		{
			return CreateUnresolved(entry, index, PresetImportStatus.Ambiguous, exactMatches);
		}
		return CreateUnresolved(entry, index, (idMatches.Count > 0) ? PresetImportStatus.VersionMismatch : PresetImportStatus.Missing, idMatches);
	}

	private static PresetImportItem CreateUnresolved(ModPresetEntry entry, int index, PresetImportStatus status, IEnumerable<Mod> candidates)
	{
		List<string> list = (from mod in candidates
			select mod.manifest?.version into version
			where !string.IsNullOrWhiteSpace(version)
			select version).Distinct<string>(StringComparer.Ordinal).ToList();
		return new PresetImportItem
		{
			Order = index,
			Entry = entry,
			Status = status,
			CurrentVersion = ((list.Count == 0) ? null : string.Join(" / ", list))
		};
	}

	private static SettingsSaveMod CreateRemainderEntry(SettingsSaveMod existing, ModKey key, IReadOnlyDictionary<ModKey, Mod> candidates, IReadOnlySet<string> targetIds, bool preserveNonGameplayRemainder)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		bool isEnabled = existing.IsEnabled;
		Mod value;
		if (targetIds.Contains(existing.Id))
		{
			isEnabled = false;
		}
		else if (string.Equals(existing.Id, "ModPresetManager", StringComparison.Ordinal))
		{
			isEnabled = true;
		}
		else if (!preserveNonGameplayRemainder)
		{
			isEnabled = false;
		}
		else if (candidates.TryGetValue(key, out value))
		{
			ModManifest manifest = value.manifest;
			if (manifest != null && manifest.affectsGameplay)
			{
				isEnabled = false;
			}
		}
		return new SettingsSaveMod
		{
			Id = existing.Id,
			Source = existing.Source,
			IsEnabled = isEnabled
		};
	}

	private static int CountChanges(IReadOnlyList<SettingsSaveMod> before, IReadOnlyList<SettingsSaveMod> after)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		int num = Math.Abs(before.Count - after.Count);
		int num2 = Math.Min(before.Count, after.Count);
		for (int i = 0; i < num2; i++)
		{
			if (!string.Equals(before[i].Id, after[i].Id, StringComparison.Ordinal) || before[i].Source != after[i].Source || before[i].IsEnabled != after[i].IsEnabled)
			{
				num++;
			}
		}
		return num;
	}

	private static CurrentModEntry ToCurrentEntry(Mod mod, bool enabled)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		string id = mod.manifest.id;
		return new CurrentModEntry
		{
			Mod = mod,
			Id = id,
			DisplayName = (string.IsNullOrWhiteSpace(mod.manifest.name) ? id : mod.manifest.name),
			Version = (mod.manifest.version ?? ""),
			Source = mod.modSource,
			IsEnabled = enabled,
			AffectsGameplay = mod.manifest.affectsGameplay,
			WorkshopId = GetWorkshopId(mod)
		};
	}

	private static Mod SelectPreferred(IEnumerable<Mod> mods)
	{
		return mods.OrderBy(delegate(Mod mod)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected I4, but got Unknown
			ModLoadState state = mod.state;
			return (int)state switch
			{
				5 => 0, 
				1 => 1, 
				0 => 2, 
				3 => 3, 
				2 => 4, 
				4 => 5, 
				_ => 6, 
			};
		}).First();
	}

	private static ulong? ParseWorkshopIdFromPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		string text = path.Replace('\\', '/');
		int num = text.IndexOf("/workshop/content/2868840/", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return null;
		}
		int num2 = num + "/workshop/content/2868840/".Length;
		int num3 = text.IndexOf('/', num2);
		string s;
		if (num3 >= 0)
		{
			int num4 = num2;
			s = text.Substring(num4, num3 - num4);
		}
		else
		{
			string text2 = text;
			int num4 = num2;
			s = text2.Substring(num4, text2.Length - num4);
		}
		if (!ulong.TryParse(s, out var result))
		{
			return null;
		}
		return result;
	}
}
