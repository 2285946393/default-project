using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat;
using RandomForeseer.RandomForeseerCode.Integrations;
using RandomForeseer.RandomForeseerCode.Localization;
using RandomForeseer.RandomForeseerCode.Settings;
using RandomForeseer.RandomForeseerCode.Telemetry;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace RandomForeseer.RandomForeseerCode;

[ModInitializer("Initialize")]
public class Entry
{
	public const string ModId = "RandomForeseer";

	public const string ResPath = "res://RandomForeseer";

	internal static IReadOnlyDictionary<string, string> AssemblyMetadata { get; } = typeof(Entry).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToDictionary<AssemblyMetadataAttribute, string, string>((AssemblyMetadataAttribute attribute) => attribute.Key, (AssemblyMetadataAttribute attribute) => attribute.Value ?? string.Empty, StringComparer.Ordinal);


	public static Logger Logger { get; } = new Logger("RandomForeseer", (LogType)0);


	public static void Initialize()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		RitsuLibFramework.EnsureGodotScriptsRegistered(executingAssembly, Logger);
		ModTypeDiscoveryHub.RegisterModAssembly("RandomForeseer", executingAssembly);
		ModData.Register();
		ModLocalization.Register();
		ModTelemetry.Register();
		SettingsBootstrap.Register();
		ModSettingsLoggingController.Register();
		CombatCardPredictionSettingsController.Register();
		RitsuLibFramework.RegisterHealthBarForecast<DamagePredictionHealthBarForecastSource>("RandomForeseer", (string)null);
		Harmony val = new Harmony("RandomForeseer.Harmony");
		val.PatchAllUncategorized(executingAssembly);
		new IntegrationCategoryPatcher(val, executingAssembly).Register("lemonSpire2", "RandomForeseer.LemonSpire");
		Logger.Info("RandomForeseer initialized.", 1);
	}
}
