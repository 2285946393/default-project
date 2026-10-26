using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

[ModInitializer("Initialize")]
public class MainFile
{
	public const string ModId = "RestSiteExpansion";

	public const string ResPath = "res://RestSiteExpansion";

	public static Logger Logger { get; } = RitsuLibFramework.CreateLogger("RestSiteExpansion", (LogType)0);


	public static void Initialize()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		RitsuLibFramework.EnsureGodotScriptsRegistered(executingAssembly, Logger);
		ModTypeDiscoveryHub.RegisterModAssembly("RestSiteExpansion", executingAssembly);
		ModConfigPage.Register();
		RestSiteExpansionRuntimeConfig.Register();
		RegisterContent();
		RestMenuState.ValidateOrWarn(RestSiteExpansionSingleton.DesiredOrder);
		try
		{
			new Harmony("RestSiteExpansion").PatchAll(executingAssembly);
		}
		catch (Exception value)
		{
			Logger.Warn($"{"RestSiteExpansion"} Harmony 补丁应用失败（选项布局适配将不可用）：{value}", 1);
		}
		Logger.Info("RestSiteExpansion initialized successfully.", 1);
	}

	private static void RegisterContent()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		ModContentPackBuilder val = RitsuLibFramework.CreateContentPack("RestSiteExpansion");
		val.Singleton<RestSiteExpansionSingleton>();
		val.Singleton<SleepBuffCombatSingleton>();
		val.Apply();
	}
}
