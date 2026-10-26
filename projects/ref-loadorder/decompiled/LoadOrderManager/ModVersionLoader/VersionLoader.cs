using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace ModVersionLoader;

[ModInitializer("Initialize")]
public static class VersionLoader
{
	private sealed class VersionDir
	{
		public required string Name { get; init; }

		public Version? Version { get; init; }

		public required string Directory { get; init; }

		public required string ImplFileName { get; init; }
	}

	private const string ImplAssemblySuffix = ".Impl";

	private const string EntryPointMethod = "Initialize";

	private static int _started;

	internal const string SelectedVersionEnvVar = "AMS_LOADER_SELECTED_VERSION";

	private static readonly Lazy<bool> LazyGodotNativeAvailable = new Lazy<bool>(DetectGodotNative);

	private static bool GodotNativeAvailable => LazyGodotNativeAvailable.Value;

	public static void Initialize()
	{
		if (Interlocked.Exchange(ref _started, 1) != 0)
		{
			return;
		}
		string text = ResolveModId();
		string text2 = "[" + text + "/Loader]";
		try
		{
			string text3 = ResolveModDirectory();
			Log(text2 + " mod dir: " + text3);
			List<VersionDir> list = Discover(text3);
			if (list.Count == 0)
			{
				LogError(text2 + " no implementation found under " + Path.Combine(text3, "bin") + "; nothing to load.");
				return;
			}
			VersionDir versionDir = Pick(list);
			if (versionDir == null)
			{
				LogError($"{text2} no usable implementation for game {Describe(GameVersion.Detect())}; available: {string.Join(", ", list.Select((VersionDir c) => c.Name))}");
				return;
			}
			Log($"{text2} game {Describe(GameVersion.Detect())} -> {versionDir.Name} ({versionDir.ImplFileName})");
			try
			{
				Environment.SetEnvironmentVariable("AMS_LOADER_SELECTED_VERSION", versionDir.Name);
			}
			catch
			{
			}
			Load(versionDir, text2);
		}
		catch (Exception value)
		{
			LogError($"{text2} failed: {value}");
		}
	}

	private static List<VersionDir> Discover(string modDir)
	{
		List<VersionDir> list = new List<VersionDir>();
		string path = Path.Combine(modDir, "bin");
		if (!Directory.Exists(path))
		{
			return list;
		}
		string[] directories = Directory.GetDirectories(path);
		foreach (string text in directories)
		{
			string fileName = Path.GetFileName(text);
			string text2 = (from f in Directory.EnumerateFiles(text, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileName)
				where f?.EndsWith(".Impl.dll", StringComparison.OrdinalIgnoreCase) ?? false
				select f).OrderBy<string, string>((string f) => f, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
			if (text2 == null)
			{
				Log($"[Loader] skipping bin/{fileName}: no *{".Impl"}.dll");
			}
			else
			{
				list.Add(new VersionDir
				{
					Name = fileName,
					Version = ParseFolderVersion(fileName),
					Directory = text,
					ImplFileName = text2
				});
			}
		}
		return list;
	}

	internal static Version? ParseFolderVersion(string folderName)
	{
		if (string.IsNullOrEmpty(folderName))
		{
			return null;
		}
		if (folderName[0] != 'g' && folderName[0] != 'G')
		{
			return null;
		}
		string text = folderName.Substring(1);
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		string text2 = text;
		foreach (char c2 in text2)
		{
			if (char.IsDigit(c2))
			{
				stringBuilder.Append(c2);
				continue;
			}
			if (c2 != '.' || num >= 3 || stringBuilder.Length <= 0)
			{
				break;
			}
			stringBuilder.Append('.');
			num++;
		}
		string text3 = stringBuilder.ToString().TrimEnd('.');
		if (text3.Length == 0)
		{
			return null;
		}
		int num2 = 3 - text3.Count((char c) => c == '.');
		for (int j = 0; j < num2; j++)
		{
			text3 += ".0";
		}
		if (!Version.TryParse(text3, out Version result))
		{
			return null;
		}
		return result;
	}

	internal static string FolderNameFor(Version version)
	{
		return $"g{version.Major}.{version.Minor}.{((version.Build >= 0) ? version.Build : 0)}";
	}

	private static VersionDir? Pick(List<VersionDir> candidates)
	{
		Version host = GameVersion.Detect();
		if (host != null)
		{
			string exact = FolderNameFor(host);
			VersionDir versionDir = candidates.FirstOrDefault((VersionDir c) => string.Equals(c.Name, exact, StringComparison.OrdinalIgnoreCase));
			if (versionDir != null)
			{
				return versionDir;
			}
		}
		if (host != null)
		{
			VersionDir versionDir2 = (from c in candidates
				where c.Version != null && c.Version <= host
				orderby c.Version descending
				select c).FirstOrDefault();
			if (versionDir2 != null)
			{
				return versionDir2;
			}
		}
		VersionDir versionDir3 = candidates.FirstOrDefault((VersionDir c) => string.Equals(c.Name, "latest", StringComparison.OrdinalIgnoreCase));
		if (versionDir3 != null)
		{
			return versionDir3;
		}
		if (host == null)
		{
			return (from c in candidates
				where c.Version != null
				orderby c.Version descending
				select c).FirstOrDefault() ?? candidates.OrderBy<VersionDir, string>((VersionDir c) => c.Name, StringComparer.OrdinalIgnoreCase).Last();
		}
		return null;
	}

	private static void Load(VersionDir picked, string tag)
	{
		string assemblyPath = Path.Combine(picked.Directory, picked.ImplFileName);
		MethodInfo methodInfo = FindEntryPoint((AssemblyLoadContext.GetLoadContext(typeof(VersionLoader).Assembly) ?? AssemblyLoadContext.Default).LoadFromAssemblyPath(assemblyPath));
		if (methodInfo == null)
		{
			LogError($"{tag} {picked.ImplFileName} has no public static void {"Initialize"}(); not initializing.");
			return;
		}
		Log($"{tag} invoking {methodInfo.DeclaringType?.FullName}.{"Initialize"}()");
		if (Environment.GetEnvironmentVariable("AMS_LOADER_DRYRUN") == "1")
		{
			Log(tag + " AMS_LOADER_DRYRUN=1, skipping entry point execution");
		}
		else
		{
			methodInfo.Invoke(null, null);
		}
	}

	private static MethodInfo? FindEntryPoint(Assembly assembly)
	{
		try
		{
			MethodInfo methodInfo = Search(assembly.GetExportedTypes());
			if (methodInfo != null)
			{
				return methodInfo;
			}
		}
		catch (Exception ex)
		{
			LogError("[Loader] enumerating exported types failed: " + ex.Message);
		}
		try
		{
			return Search(assembly.GetTypes());
		}
		catch (ReflectionTypeLoadException ex2)
		{
			LogError("[Loader] partial type load: " + ex2.Message);
			return Search(ex2.Types.Where((Type t) => t != null));
		}
		catch (Exception ex3)
		{
			LogError("[Loader] enumerating types failed: " + ex3.Message);
			return null;
		}
		static MethodInfo? Search(IEnumerable<Type> types)
		{
			foreach (Type type in types)
			{
				MethodInfo method;
				try
				{
					method = type.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
				}
				catch
				{
					continue;
				}
				if (method != null)
				{
					return method;
				}
			}
			return null;
		}
	}

	private static string ResolveModId()
	{
		try
		{
			string location = typeof(VersionLoader).Assembly.Location;
			if (!string.IsNullOrEmpty(location))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(location);
				if (!string.IsNullOrEmpty(fileNameWithoutExtension))
				{
					return fileNameWithoutExtension;
				}
			}
		}
		catch
		{
		}
		return "ModVersionLoader";
	}

	private static string ResolveModDirectory()
	{
		string location = typeof(VersionLoader).Assembly.Location;
		if (string.IsNullOrEmpty(location))
		{
			throw new InvalidOperationException("Loader assembly has no location on disk.");
		}
		return Path.GetDirectoryName(location) ?? throw new InvalidOperationException("Could not resolve loader directory.");
	}

	private static string Describe(Version? version)
	{
		return version?.ToString() ?? "unknown";
	}

	internal static void Log(string message)
	{
		if (GodotNativeAvailable)
		{
			GD.Print(message);
		}
		else
		{
			Console.WriteLine(message);
		}
	}

	internal static void LogError(string message)
	{
		if (GodotNativeAvailable)
		{
			GD.PrintErr(message);
		}
		else
		{
			Console.Error.WriteLine(message);
		}
	}

	private static bool DetectGodotNative()
	{
		try
		{
			Type type = typeof(GD).Assembly.GetType("Godot.NativeInterop.NativeFuncs", throwOnError: false);
			if (type == null)
			{
				return false;
			}
			string[] array = new string[3] { "godotsharp_string_new_with_utf16_chars", "godot_icall_String_new", "ManagedStringToNative" };
			foreach (string name in array)
			{
				if (type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) != null)
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}
}
