using System;
using HarmonyLib;

namespace LoadOrderManager;

public static class LoadOrderManagerMod
{
	public const string ModId = "LoadOrderManager";

	private const string SelectedVersionEnvVar = "AMS_LOADER_SELECTED_VERSION";

	private static bool _initialized;

	private static bool _harmonyPatched;

	public static void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			DebugLog.Info("Initialize called.");
			DebugLog.Info("Version bundle: " + ResolveVersionBundle() + ".");
			DebugLog.Info("Log file: " + DebugLog.LogPath);
			ApplyHarmonyPatches();
			DebugLog.Info("Loaded.");
			SelfTest.Schedule();
		}
	}

	private static string ResolveVersionBundle()
	{
		try
		{
			string environmentVariable = Environment.GetEnvironmentVariable("AMS_LOADER_SELECTED_VERSION");
			return string.IsNullOrWhiteSpace(environmentVariable) ? "unknown (no launcher)" : environmentVariable;
		}
		catch
		{
			return "unknown";
		}
	}

	internal static void ApplyHarmonyPatches()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (_harmonyPatched)
		{
			return;
		}
		_harmonyPatched = true;
		try
		{
			new Harmony("LoadOrderManager").PatchAll();
			DebugLog.Info("Harmony patches applied.");
		}
		catch (Exception ex)
		{
			DebugLog.Error("Harmony patch failed.", ex);
		}
	}
}
