using RegentFX.ThirdParty;

namespace RegentFX.Scripts;

public static class Setting
{
	public static float ExposureThreshold => (float)RitsuLibModConfig.GetRitsuLibSettingDouble("ExposureThreshold");

	public static bool DevTestStartMode => RitsuLibModConfig.GetRitsuLibSettingBool("DevTestStartMode");

	public static bool PreloadEffects => RitsuLibModConfig.GetRitsuLibSettingBool("PreloadEffects");

	public static bool DisableModSounds
	{
		get
		{
			if (!RitsuLibModConfig.GetRitsuLibSettingBool("DisableModSounds"))
			{
				return !Entry.FmodLoaded;
			}
			return true;
		}
	}

	public static bool ToggleEnabled(string key)
	{
		return RitsuLibModConfig.GetRitsuLibSettingBool(key);
	}
}
