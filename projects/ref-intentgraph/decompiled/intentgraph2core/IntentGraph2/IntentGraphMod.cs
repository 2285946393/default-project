using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using IntentGraph2.Crossovers;
using IntentGraph2.Models;
using IntentGraph2.Patches;
using IntentGraph2.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;

namespace IntentGraph2;

public class IntentGraphMod
{
	public const string ModId = "intentgraph2";

	public static Dictionary<string, string> IntentGraphStrings = new Dictionary<string, string>();

	public static Dictionary<string, IntentDefinitionList> IntentDefinitions = new Dictionary<string, IntentDefinitionList>();

	public static readonly ConditionalWeakTable<MonsterModel, Graph> GeneratedGraphs = new ConditionalWeakTable<MonsterModel, Graph>();

	private static IntentGraphModConfig defaultConfig = new IntentGraphModConfig();

	private static IBaseLibHelper? baseLibHelper;

	private static IRitsuLibHelper? ritsuLibHelper;

	public static JsonSerializerOptions SerializeOptions { get; } = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		Converters = { (JsonConverter)new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};


	public static IntentGraphModConfig Config => ritsuLibHelper?.Config ?? baseLibHelper?.Config ?? defaultConfig;

	public static void InitializeMod()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		IgLogger.Info("IntentGraphMod initialize");
		Assembly assembly = typeof(IntentGraphMod).Assembly;
		ScriptManagerBridge.LookupScriptsInAssembly(assembly);
		IgLogger.Info("Patching...");
		new Harmony("chaofan.sts2.intentgraph2").PatchAll(assembly);
		IgLogger.Info("Initializing IntentGraphHost...");
		IntentGraphHost.Initialize();
		IgLogger.Info("IntentGraphMod initialize done.");
	}

	public static void PostInitializeMod()
	{
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		IgLogger.Info("IntentGraphMod post initialize");
		if (GetLoadedMods().Any((Mod m) => m.manifest?.id == "BaseLib"))
		{
			try
			{
				Assembly assembly = LoadDll("intentgraph2baselib");
				if (assembly != null)
				{
					baseLibHelper = (IBaseLibHelper)AccessToolsExtensions.CreateInstance(assembly.GetType("IntentGraph2.BaseLib.BaseLibHelper"));
					baseLibHelper.RegisterConfig();
				}
				else
				{
					IgLogger.Warn("Failed to get assembly load context for IntentGraphMod.");
				}
			}
			catch (Exception ex)
			{
				baseLibHelper = null;
				IgLogger.Warn("Failed to load BaseLib helper: " + ex);
			}
		}
		if (GetLoadedMods().Any((Mod m) => m.manifest?.id == "STS2-RitsuLib"))
		{
			try
			{
				Assembly assembly2 = LoadDll("intentgraph2ritsulib");
				if (assembly2 != null)
				{
					ritsuLibHelper = (IRitsuLibHelper)AccessToolsExtensions.CreateInstance(assembly2.GetType("IntentGraph2.RitsuLib.RitsuLibHelper"));
					ritsuLibHelper.RegisterConfig();
				}
				else
				{
					IgLogger.Warn("Failed to get assembly load context for IntentGraphMod.");
				}
			}
			catch (Exception ex2)
			{
				ritsuLibHelper = null;
				IgLogger.Warn("Failed to load RitsuLib helper: " + ex2);
			}
		}
		if (ritsuLibHelper != null && baseLibHelper != null)
		{
			baseLibHelper.Config.SetFrom(ritsuLibHelper.Config);
			baseLibHelper.SaveConfig();
			baseLibHelper.Config.OnUpdated += delegate
			{
				ritsuLibHelper.Config.SetFrom(baseLibHelper.Config);
				ritsuLibHelper.SaveConfig();
			};
			ritsuLibHelper.Config.OnUpdated += delegate
			{
				baseLibHelper.Config.SetFrom(ritsuLibHelper.Config);
				baseLibHelper.SaveConfig();
			};
		}
		EventHandler<string> value = delegate(object? sender, string e)
		{
			if (e == "ShowMonsterMoveNames" || e == "ShowMoveDetail")
			{
				RegenerateIntentGraphsInCombat();
			}
		};
		if (ritsuLibHelper != null)
		{
			ritsuLibHelper.Config.OnUpdated += value;
		}
		if (baseLibHelper != null)
		{
			baseLibHelper.Config.OnUpdated += value;
		}
		try
		{
			string text = new GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath((string)null, (PlatformType?)null, (ulong?)null) + "/mod_data/intentgraph2").ReadFile("settings.json");
			if (text != null)
			{
				defaultConfig = JsonSerializer.Deserialize<IntentGraphModConfig>(text) ?? new IntentGraphModConfig();
			}
		}
		catch (Exception ex3)
		{
			IgLogger.Warn("Failed to load default config: " + ex3);
		}
		LoadIntentDefinitions();
		IgLogger.Info("IntentGraphMod post initialize done.");
	}

	public static void LoadIntentDefinitions()
	{
		IntentDefinitions.Clear();
		LoadIntentDefinitionForMod("intentgraph2");
		foreach (Mod loadedMod in GetLoadedMods())
		{
			if (loadedMod != null && loadedMod.manifest?.id != null && loadedMod.manifest.id != "intentgraph2")
			{
				LoadIntentDefinitionForMod(loadedMod.manifest.id);
			}
		}
		LoadIntentDefinitionForDev();
	}

	public static void ReloadIntentDefinitionsAndGraphs()
	{
		LoadIntentDefinitions();
		LoadIntentStrings(LocManager.Instance.Language);
		BestiaryPatches.ReloadIntents();
		RegenerateIntentGraphsInCombat();
	}

	public static void LoadIntentStrings(string language)
	{
		IntentGraphStrings.Clear();
		LoadIntentStringsFromMod("intentgraph2", language);
		foreach (Mod loadedMod in GetLoadedMods())
		{
			if (loadedMod != null && loadedMod.manifest?.id != null && loadedMod.manifest.id != "intentgraph2")
			{
				LoadIntentStringsFromMod(loadedMod.manifest.id, language);
			}
		}
		LoadIntentStringsForDev(language);
	}

	public static string GetDevIntentDefinitionFilePath()
	{
		return Path.GetFullPath(Path.Join(Path.GetDirectoryName(typeof(ModManager).Assembly.Location), "..", "intentgraph-intents-dev.json"));
	}

	public static string GetDevIntentStringFilePath(string language)
	{
		return Path.GetFullPath(Path.Join(Path.GetDirectoryName(typeof(ModManager).Assembly.Location), "..", "intentgraph-strings-" + language + "-dev.json"));
	}

	public static Dictionary<string, IntentDefinitionList> LoadIntentDefinitionsFromFile(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
				return new Dictionary<string, IntentDefinitionList>();
			}
			return JsonSerializer.Deserialize<Dictionary<string, IntentDefinitionList>>(File.ReadAllText(filePath), SerializeOptions) ?? new Dictionary<string, IntentDefinitionList>();
		}
		catch (Exception value)
		{
			IgLogger.Warn($"Failed to load intent definitions from {filePath}: {value}");
			return new Dictionary<string, IntentDefinitionList>();
		}
	}

	public static void SaveIntentDefinitionsToFile(string filePath, Dictionary<string, IntentDefinitionList> definitions)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
		JsonSerializerOptions options = new JsonSerializerOptions(SerializeOptions)
		{
			WriteIndented = true
		};
		string contents = JsonSerializer.Serialize(definitions, options);
		File.WriteAllText(filePath, contents);
	}

	public static Dictionary<string, string> LoadIntentStringsFromFile(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
				return new Dictionary<string, string>();
			}
			return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath)) ?? new Dictionary<string, string>();
		}
		catch (Exception value)
		{
			IgLogger.Warn($"Failed to load intent strings from {filePath}: {value}");
			return new Dictionary<string, string>();
		}
	}

	public static void SaveIntentStringsToFile(string filePath, Dictionary<string, string> strings)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
		JsonSerializerOptions options = new JsonSerializerOptions(SerializeOptions)
		{
			WriteIndented = true
		};
		string contents = JsonSerializer.Serialize(strings, options);
		File.WriteAllText(filePath, contents);
	}

	private static IEnumerable<Mod> GetLoadedMods()
	{
		return ModManager.GetLoadedMods();
	}

	private static void RegenerateIntentGraphsInCombat()
	{
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (val == null || val.Encounter == null)
		{
			return;
		}
		GeneratedGraphs.Clear();
		foreach (Creature enemy in val.Enemies)
		{
			MonsterSetupPatch.Postfix(CombatManager.Instance, enemy);
		}
	}

	private static void LoadIntentDefinitionForMod(string modId)
	{
		IgLogger.Info("Searching intent definitions for mod " + modId);
		string errorMessage = "Failed to load intent definitions for mod " + modId;
		LoadIntentDefinitionsFromResource($"res://{"intentgraph2"}/intentgraph-{modId}.json", errorMessage);
		LoadIntentDefinitionsFromResource("res://" + modId + "/intentgraph.json", errorMessage);
		if (ReleaseInfoManager.Instance.ReleaseInfo != null)
		{
			LoadIntentDefinitionsFromResource($"res://{modId}/intentgraph-{ReleaseInfoManager.Instance.ReleaseInfo.Version}.json", "Failed to load version-specific intent definitions for mod " + modId);
		}
	}

	private static void LoadIntentDefinitionsFromResource(string file, string errorMessage)
	{
		try
		{
			if (!FileAccess.FileExists(file))
			{
				return;
			}
			IgLogger.Info("Loading intent definitions from " + file);
			FileAccess val = FileAccess.Open(file, (ModeFlags)1);
			try
			{
				foreach (KeyValuePair<string, IntentDefinitionList> item in JsonSerializer.Deserialize<Dictionary<string, IntentDefinitionList>>(val.GetAsText(false), SerializeOptions) ?? new Dictionary<string, IntentDefinitionList>())
				{
					IntentDefinitions[item.Key] = item.Value;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception value)
		{
			IgLogger.Warn($"{errorMessage}: {value}");
		}
	}

	private static string GetLocalizedResourcePath(string preferredPath, string fallbackPath)
	{
		if (!FileAccess.FileExists(preferredPath))
		{
			return fallbackPath;
		}
		return preferredPath;
	}

	private static void LoadIntentDefinitionForDev()
	{
		string devIntentDefinitionFilePath = GetDevIntentDefinitionFilePath();
		IgLogger.Info("Loading intent definitions from " + devIntentDefinitionFilePath);
		foreach (KeyValuePair<string, IntentDefinitionList> item in LoadIntentDefinitionsFromFile(devIntentDefinitionFilePath))
		{
			IntentDefinitions[item.Key] = item.Value;
		}
	}

	private static void LoadIntentStringsFromMod(string modId, string language)
	{
		IgLogger.Info("Searching intent strings for mod " + modId + ", language " + language);
		string errorMessage = "Failed to load intent strings for mod " + modId + ", language " + language;
		LoadIntentStringsFromResource(GetLocalizedResourcePath($"res://{"intentgraph2"}/localization/{language}/intentgraph-{modId}.json", $"res://{"intentgraph2"}/localization/eng/intentgraph-{modId}.json"), errorMessage);
		LoadIntentStringsFromResource(GetLocalizedResourcePath($"res://{modId}/localization/{language}/intentgraph.json", "res://" + modId + "/localization/eng/intentgraph.json"), errorMessage);
		if (ReleaseInfoManager.Instance.ReleaseInfo != null)
		{
			LoadIntentStringsFromResource(GetLocalizedResourcePath($"res://{modId}/localization/{language}/intentgraph-{ReleaseInfoManager.Instance.ReleaseInfo.Version}.json", $"res://{modId}/localization/eng/intentgraph-{ReleaseInfoManager.Instance.ReleaseInfo.Version}.json"), "Failed to load version-specific intent strings for mod " + modId + ", language " + language);
		}
	}

	private static void LoadIntentStringsFromResource(string file, string errorMessage)
	{
		try
		{
			if (!FileAccess.FileExists(file))
			{
				return;
			}
			IgLogger.Info("Loading intent strings from " + file);
			FileAccess val = FileAccess.Open(file, (ModeFlags)1);
			try
			{
				foreach (KeyValuePair<string, string> item in JsonSerializer.Deserialize<Dictionary<string, string>>(val.GetAsText(false)) ?? new Dictionary<string, string>())
				{
					IntentGraphStrings[item.Key] = item.Value;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception value)
		{
			IgLogger.Warn($"{errorMessage}: {value}");
		}
	}

	private static void LoadIntentStringsForDev(string language)
	{
		string devIntentStringFilePath = GetDevIntentStringFilePath(language);
		IgLogger.Info("Loading intent strings from " + devIntentStringFilePath);
		foreach (KeyValuePair<string, string> item in LoadIntentStringsFromFile(devIntentStringFilePath))
		{
			IntentGraphStrings[item.Key] = item.Value;
		}
	}

	private static Assembly? LoadDll(string dllName)
	{
		try
		{
			Assembly assembly = typeof(IntentGraphMod).Assembly;
			AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(assembly);
			if (loadContext != null)
			{
				string assemblyPath = Path.Join(Path.GetDirectoryName(assembly.Location), dllName + ".dll");
				return loadContext.LoadFromAssemblyPath(assemblyPath);
			}
			IgLogger.Info("Failed to get assembly load context for IntentGraphMod.");
		}
		catch (Exception value)
		{
			IgLogger.Info($"Failed to load dll {dllName}: {value}");
		}
		return null;
	}
}
