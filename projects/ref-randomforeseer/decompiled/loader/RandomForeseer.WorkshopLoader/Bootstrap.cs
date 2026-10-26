using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace RandomForeseer.WorkshopLoader;

[ModInitializer("Initialize")]
public static class Bootstrap
{
	private sealed record VariantCandidate(string ModVersion, SemanticVersion ModSemanticVersion, string MinGameVersion, SemanticVersion MinGameSemanticVersion, string DllPath, string PckPath, IReadOnlyList<DependencyCandidate> Dependencies);

	private sealed record DependencyCandidate(string Id, string? MinVersion, SemanticVersion? MinSemanticVersion);

	private sealed class BundleManifest
	{
		public int Schema { get; set; }

		public List<BundleVariant>? Variants { get; set; }
	}

	private sealed class BundleVariant
	{
		public string? ModVersion { get; set; }

		public string? MinGameVersion { get; set; }

		public string? Directory { get; set; }

		public List<BundleDependency>? Dependencies { get; set; }
	}

	private sealed class BundleDependency
	{
		public string? Id { get; set; }

		[JsonPropertyName("min_version")]
		public string? MinVersion { get; set; }
	}

	private const string ModId = "RandomForeseer";

	private const string RealDllName = "RandomForeseer.dll";

	private const string RealPckName = "RandomForeseer.pck";

	private const string VariantManifestName = "mod-variants.manifest";

	public static void Initialize()
	{
		string? directoryName = Path.GetDirectoryName(typeof(Bootstrap).Assembly.Location);
		if (string.IsNullOrWhiteSpace(directoryName))
		{
			throw new InvalidOperationException("[RandomForeseer.Loader] Could not resolve the loader directory.");
		}
		List<VariantCandidate> variants = LoadVariants(directoryName);
		SemanticVersion val = ResolveHostVersion();
		VariantCandidate variantCandidate = SelectVariant(variants, val);
		Log.Info($"[{"RandomForeseer"}.Loader] Host version {((object)val)?.ToString() ?? "<unknown>"}; selected Mod variant {variantCandidate.ModVersion} (minimum game version {variantCandidate.MinGameVersion}).", 2);
		SynchronizeModManagerVersion(directoryName, variantCandidate);
		if (!TryValidateDependencies(variantCandidate, out List<LocString> errors))
		{
			ReportDependencyFailure(errors);
			return;
		}
		Assembly assembly = (AssemblyLoadContext.GetLoadContext(typeof(Bootstrap).Assembly) ?? AssemblyLoadContext.Default).LoadFromAssemblyPath(variantCandidate.DllPath);
		LoadResourcePack(variantCandidate.PckPath);
		InvokeRealInitializer(assembly);
	}

	private static void SynchronizeModManagerVersion(string loaderDirectory, VariantCandidate selected)
	{
		string loaderDirectory2 = loaderDirectory;
		Mod val = ((IEnumerable<Mod>)ModManager.Mods).SingleOrDefault((Func<Mod, bool>)((Mod mod) => mod.manifest?.id == "RandomForeseer" && PathsEqual(mod.path, loaderDirectory2)));
		if (val?.manifest == null)
		{
			Log.Warn($"[{"RandomForeseer"}.Loader] Could not find the detected ModManager entry for {loaderDirectory2}.", 2);
			return;
		}
		string version = val.manifest.version;
		val.manifest.version = selected.ModVersion;
		val.version = selected.ModSemanticVersion;
		if (version != selected.ModVersion)
		{
			Log.Info($"[{"RandomForeseer"}.Loader] Updated the reported Mod version from {version ?? "<null>"} to {selected.ModVersion}.", 2);
		}
	}

	private static List<VariantCandidate> LoadVariants(string loaderDirectory)
	{
		string loaderDirectory2 = loaderDirectory;
		string text = Path.Combine(loaderDirectory2, "mod-variants.manifest");
		if (!File.Exists(text))
		{
			throw new FileNotFoundException("Workshop variant manifest was not found.", text);
		}
		BundleManifest bundleManifest;
		try
		{
			bundleManifest = JsonSerializer.Deserialize<BundleManifest>(File.ReadAllText(text), new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
		}
		catch (Exception innerException)
		{
			throw new InvalidDataException("Failed to read Workshop variant manifest " + text + ".", innerException);
		}
		List<BundleVariant> list = bundleManifest?.Variants;
		if (list == null || list.Count <= 0)
		{
			throw new InvalidDataException("Workshop variant manifest contains no variants: " + text);
		}
		string libRoot = Path.GetFullPath(Path.Combine(loaderDirectory2, "lib"));
		return bundleManifest.Variants.Select((BundleVariant entry) => CreateCandidate(loaderDirectory2, libRoot, entry)).ToList();
	}

	private static VariantCandidate CreateCandidate(string loaderDirectory, string libRoot, BundleVariant entry)
	{
		BundleVariant entry2 = entry;
		string text = entry2.ModVersion?.Trim();
		SemanticVersion val = default(SemanticVersion);
		if (string.IsNullOrWhiteSpace(text) || !SemanticVersion.TryFromString(text, ref val) || val == null)
		{
			throw new InvalidDataException("Invalid variant Mod version: " + entry2.ModVersion);
		}
		string text2 = entry2.MinGameVersion?.Trim();
		SemanticVersion val2 = default(SemanticVersion);
		if (string.IsNullOrWhiteSpace(text2) || !SemanticVersion.TryFromString(text2, ref val2) || val2 == null)
		{
			throw new InvalidDataException("Invalid variant minimum game version: " + entry2.MinGameVersion);
		}
		if (string.IsNullOrWhiteSpace(entry2.Directory))
		{
			throw new InvalidDataException("Variant " + entry2.ModVersion + " has no directory.");
		}
		string fullPath = Path.GetFullPath(Path.Combine(loaderDirectory, entry2.Directory));
		if (!IsUnderDirectory(fullPath, libRoot))
		{
			throw new InvalidDataException("Variant directory is outside the lib directory: " + entry2.Directory);
		}
		string dllPath = Path.Combine(fullPath, "RandomForeseer.dll");
		string pckPath = Path.Combine(fullPath, "RandomForeseer.pck");
		List<DependencyCandidate> dependencies = (entry2.Dependencies ?? new List<BundleDependency>()).Select((BundleDependency dependency) => CreateDependencyCandidate(entry2.ModVersion, dependency)).ToList();
		return new VariantCandidate(text, val, text2, val2, dllPath, pckPath, dependencies);
	}

	private static DependencyCandidate CreateDependencyCandidate(string? modVersion, BundleDependency dependency)
	{
		string text = dependency.Id?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidDataException("Variant " + modVersion + " contains a dependency without an ID.");
		}
		string text2 = dependency.MinVersion?.Trim();
		SemanticVersion val = null;
		if (!string.IsNullOrWhiteSpace(text2) && (!SemanticVersion.TryFromString(text2, ref val) || val == null))
		{
			throw new InvalidDataException($"Variant {modVersion} has an invalid minimum version for dependency {text}: {text2}");
		}
		return new DependencyCandidate(text, text2, val);
	}

	private static VariantCandidate SelectVariant(IReadOnlyCollection<VariantCandidate> variants, SemanticVersion? hostVersion)
	{
		SemanticVersion hostVersion2 = hostVersion;
		if (hostVersion2 != null)
		{
			VariantCandidate variantCandidate = (from candidate in variants
				where candidate.MinGameSemanticVersion.CompareTo(hostVersion2) <= 0
				orderby candidate.ModSemanticVersion
				select candidate).LastOrDefault();
			if ((object)variantCandidate != null)
			{
				return variantCandidate;
			}
			Log.Warn($"[{"RandomForeseer"}.Loader] No bundled variant declares support for game {hostVersion2}; " + "using the newest bundled Mod version as a best-effort fallback.", 2);
		}
		else
		{
			Log.Warn("[RandomForeseer.Loader] Could not determine the game version; using the newest bundled Mod version as a best-effort fallback.", 2);
		}
		return variants.OrderBy((VariantCandidate candidate) => candidate.ModSemanticVersion).Last();
	}

	private static bool TryValidateDependencies(VariantCandidate selected, out List<LocString> errors)
	{
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Expected O, but got Unknown
		List<Mod> source = ModManager.GetLoadedMods().ToList();
		List<string> list = new List<string>();
		errors = new List<LocString>();
		foreach (DependencyCandidate dependency in selected.Dependencies)
		{
			Mod val = ((IEnumerable<Mod>)source).FirstOrDefault((Func<Mod, bool>)((Mod mod) => mod.manifest?.id == dependency.Id));
			if (val == null)
			{
				Log.Error($"[{"RandomForeseer"}.Loader] Selected Mod variant {selected.ModVersion} is missing dependency {dependency.Id}.", 2);
				list.Add(dependency.Id);
			}
			else if (dependency.MinSemanticVersion != null)
			{
				if (val.manifest?.version == null)
				{
					Log.Error($"[{"RandomForeseer"}.Loader] Dependency {dependency.Id} does not declare a version.", 2);
					errors.Add(CreateDependencyVersionError("RandomForeseer", "MOD_ERROR.DEPENDENCY_VERSION_MISSING", dependency, null));
				}
				else if (val.version == null)
				{
					Log.Error($"[{"RandomForeseer"}.Loader] Dependency {dependency.Id} declares invalid version {val.manifest.version}.", 2);
					errors.Add(CreateDependencyVersionError("RandomForeseer", "MOD_ERROR.DEPENDENCY_VERSION_INVALID", dependency, val.manifest.version));
				}
				else if (val.version.CompareTo(dependency.MinSemanticVersion) < 0)
				{
					Log.Error($"[{"RandomForeseer"}.Loader] Selected Mod variant {selected.ModVersion} requires {dependency.Id} {dependency.MinVersion} or newer, but {val.manifest.version} is loaded.", 2);
					errors.Add(CreateDependencyVersionError("RandomForeseer", "MOD_ERROR.DEPENDENCY_VERSION_UNSUPPORTED", dependency, val.manifest.version));
				}
			}
		}
		if (list.Count > 0)
		{
			LocString val2 = new LocString("main_menu_ui", "MOD_ERROR.MISSING_DEPENDENCY");
			val2.Add("id", "RandomForeseer");
			val2.Add("missingCount", (decimal)list.Count);
			val2.Add("missingDependencies", string.Join(",", list));
			errors.Add(val2);
		}
		return errors.Count == 0;
	}

	private static LocString CreateDependencyVersionError(string modId, string localizationKey, DependencyCandidate dependency, string? installedVersion)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		LocString val = new LocString("main_menu_ui", localizationKey);
		val.Add("id", modId);
		val.Add("dependency", dependency.Id);
		val.Add("minVersion", dependency.MinVersion ?? "<null>");
		if (installedVersion != null)
		{
			val.Add("version", installedVersion);
		}
		return val;
	}

	private static void ReportDependencyFailure(List<LocString> errors)
	{
		List<LocString> errors2 = errors;
		ModManager.OnModDetected += OnModDetected;
		void OnModDetected(Mod mod)
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			if (mod.manifest?.id != "RandomForeseer")
			{
				return;
			}
			ModManager.OnModDetected -= OnModDetected;
			try
			{
				mod.state = (ModLoadState)2;
				if (mod.errors == null)
				{
					mod.errors = new List<LocString>();
				}
				mod.errors.AddRange(errors2);
			}
			catch (Exception value)
			{
				Log.Error($"[{"RandomForeseer"}.Loader] Failed to report dependency error: {value}", 2);
			}
		}
	}

	private static SemanticVersion? ResolveHostVersion()
	{
		try
		{
			return ReleaseInfoManager.Instance.SemVer;
		}
		catch (Exception ex)
		{
			Log.Warn("[RandomForeseer.Loader] Failed to read the host game version: " + ex.Message, 2);
			return null;
		}
	}

	private static void LoadResourcePack(string pckPath)
	{
		Type type2 = Type.GetType("Godot.ProjectSettings, GodotSharp", throwOnError: false) ?? (from assembly in AppDomain.CurrentDomain.GetAssemblies()
			select assembly.GetType("Godot.ProjectSettings", throwOnError: false)).FirstOrDefault((Type type) => (object)type != null) ?? throw new TypeLoadException("Godot.ProjectSettings could not be resolved.");
		MethodInfo methodInfo = (from candidate in type2.GetMethods(BindingFlags.Static | BindingFlags.Public)
			where candidate.Name == "LoadResourcePack"
			select candidate).FirstOrDefault(delegate(MethodInfo candidate)
		{
			ParameterInfo[] parameters2 = candidate.GetParameters();
			return parameters2.Length != 0 && parameters2[0].ParameterType == typeof(string);
		}) ?? throw new MissingMethodException(type2.FullName, "LoadResourcePack");
		ParameterInfo[] parameters = methodInfo.GetParameters();
		object[] array = new object[parameters.Length];
		array[0] = pckPath;
		for (int i = 1; i < parameters.Length; i++)
		{
			array[i] = (parameters[i].HasDefaultValue ? parameters[i].DefaultValue : GetDefault(parameters[i].ParameterType));
		}
		object obj = methodInfo.Invoke(null, array);
		if (!(obj is bool) || !(bool)obj)
		{
			throw new InvalidOperationException("Godot failed to load resource pack " + pckPath + ".");
		}
	}

	private static object? GetDefault(Type type)
	{
		if (!type.IsValueType)
		{
			return null;
		}
		return Activator.CreateInstance(type);
	}

	private static void InvokeRealInitializer(Assembly assembly)
	{
		List<Type> list = (from type in GetLoadableTypes(assembly)
			where ((MemberInfo)type).GetCustomAttribute<ModInitializerAttribute>() != null
			select type).ToList();
		if (list.Count == 0)
		{
			throw new InvalidOperationException("No Mod initializer was found in " + assembly.FullName + ".");
		}
		foreach (Type item in list)
		{
			ModInitializerAttribute customAttribute = ((MemberInfo)item).GetCustomAttribute<ModInitializerAttribute>();
			(item.GetMethod(customAttribute.initializerMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMethodException(item.FullName, customAttribute.initializerMethod)).Invoke(null, null);
		}
	}

	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			Log.Warn($"[{"RandomForeseer"}.Loader] Only part of {assembly.FullName} could be inspected: {ex.Message}", 2);
			return ex.Types.OfType<Type>();
		}
	}

	private static bool IsUnderDirectory(string path, string root)
	{
		ReadOnlySpan<char> readOnlySpan = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		char reference = Path.DirectorySeparatorChar;
		string value = string.Concat(readOnlySpan, new ReadOnlySpan<char>(ref reference));
		ReadOnlySpan<char> readOnlySpan2 = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		char reference2 = Path.DirectorySeparatorChar;
		return string.Concat(readOnlySpan2, new ReadOnlySpan<char>(ref reference2)).StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PathsEqual(string left, string right)
	{
		StringComparison comparisonType = (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), comparisonType);
	}
}
