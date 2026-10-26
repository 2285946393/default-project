using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ModPresetManager.Scripts.Patches;

namespace ModPresetManager.Scripts;

[ModInitializer("Init")]
public class Entry
{
	public const string ModId = "ModPresetManager";

	private static Harmony? _harmony;

	private static readonly SemanticVersion NewerVersionFallbackMinimumGameVersion = new SemanticVersion(0, 112, 0, (string)null, (List<string>)null);

	public static void Init()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		if (_harmony == null)
		{
			WarnWhenUsingNewerVersionFallback();
			_harmony = new Harmony("STS2.ModPresetManager");
			_harmony.PatchAll();
			DuplicateSourceSettingsGuard.CaptureAndScheduleRestore();
			Log.Info("ModPresetManager initialized.", 2);
		}
	}

	private static void WarnWhenUsingNewerVersionFallback()
	{
		ReleaseInfoManager instance = ReleaseInfoManager.Instance;
		SemanticVersion semVer = instance.SemVer;
		if (semVer != null && semVer.CompareTo(NewerVersionFallbackMinimumGameVersion) >= 0)
		{
			ReleaseInfo releaseInfo = instance.ReleaseInfo;
			string text = ((releaseInfo != null) ? releaseInfo.Version : null) ?? ((object)semVer).ToString();
			Log.Warn("ModPresetManager is running on unverified STS2 version " + text + "; the 0.111.0 Runtime was selected as a newer-version fallback. Features may be incompatible.", 2);
		}
	}
}
