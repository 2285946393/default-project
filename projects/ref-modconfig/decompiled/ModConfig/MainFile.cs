using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace ModConfig;

[ModInitializer("Initialize")]
public class MainFile
{
	internal const string ModId = "sts2.piyixiajiuhenfen.modconfig";

	internal const string Version = "0.2.3";

	internal static readonly Logger Log = new Logger("sts2.piyixiajiuhenfen.modconfig", (LogType)0);

	public static void Initialize()
	{
		I18n.Initialize();
		ModConfigManager.Initialize();
		SettingsTabInjector.Initialize();
		Log.Info("ModConfig v0.2.3 initialized! (zero Harmony, cross-platform)", 1);
	}
}
