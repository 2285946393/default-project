using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace IntentGraph2.Initializer;

[ModInitializer("Initialize")]
public class Initializer
{
	private static nint _holder;

	public static void Initialize()
	{
		LogInfo("IntentGraph entry initialize");
		Libgcc();
		LoadDll("Antlr4.Runtime.Standard");
		string text = SelectVersion();
		LoadDll("intentgraph2core" + text)?.GetType("IntentGraph2.IntentGraphMod")?.GetMethod("InitializeMod", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
		LogInfo("IntentGraph entry initialize done.");
	}

	public static Assembly? LoadDll(string dllName)
	{
		LogInfo("Loading dll: " + dllName);
		try
		{
			Assembly assembly = typeof(Initializer).Assembly;
			AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(assembly);
			if (loadContext != null)
			{
				string assemblyPath = Path.Join(Path.GetDirectoryName(assembly.Location), dllName + ".dll");
				return loadContext.LoadFromAssemblyPath(assemblyPath);
			}
			LogInfo("Failed to get assembly load context for IntentGraphMod.");
		}
		catch (Exception value)
		{
			LogInfo($"Failed to load dll {dllName}: {value}");
		}
		return null;
	}

	private static void LogInfo(string message)
	{
		Log.Info("[IntentGraph] " + message, 2);
	}

	[DllImport("libdl.so.2")]
	private static extern nint dlopen(string filename, int flags);

	[DllImport("libdl.so.2")]
	private static extern nint dlerror();

	private static void Libgcc()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			LogInfo("Running on Linux, manually dlopen libgcc for Harmony");
			_holder = dlopen("libgcc_s.so.1", 258);
			if (_holder == IntPtr.Zero)
			{
				LogInfo("Or Nor: " + Marshal.PtrToStringAnsi(dlerror()));
			}
		}
	}

	private static string SelectVersion()
	{
		try
		{
			if (ReleaseInfoManager.Instance.ReleaseInfo != null && new Version(ReleaseInfoManager.Instance.ReleaseInfo.Version.TrimStart('v')) < new Version("0.110.0"))
			{
				return "-lt-0.110.0";
			}
		}
		catch (Exception value)
		{
			LogInfo($"Failed to select version: {value}");
		}
		return string.Empty;
	}
}
