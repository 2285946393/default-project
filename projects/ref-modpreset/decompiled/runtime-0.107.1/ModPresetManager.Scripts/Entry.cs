using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ModPresetManager.Scripts.Patches;

namespace ModPresetManager.Scripts;

[ModInitializer("Init")]
public class Entry
{
	public const string ModId = "ModPresetManager";

	private static Harmony? _harmony;

	public static void Init()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		if (_harmony == null)
		{
			_harmony = new Harmony("STS2.ModPresetManager");
			_harmony.PatchAll();
			DuplicateSourceSettingsGuard.CaptureAndScheduleRestore();
			Log.Info("ModPresetManager initialized.", 2);
		}
	}
}
