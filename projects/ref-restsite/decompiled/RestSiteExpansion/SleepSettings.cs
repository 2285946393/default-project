namespace RestSiteExpansion;

public readonly record struct SleepSettings(bool NapEnabled, bool DeepSleepEnabled, bool Exclusive, bool MeditateEnabled, bool StayUpLateEnabled)
{
	public static SleepSettings Disabled => new SleepSettings(NapEnabled: false, DeepSleepEnabled: false, Exclusive: false, MeditateEnabled: false, StayUpLateEnabled: false);

	public bool AnyEnabled => NapEnabled || DeepSleepEnabled || MeditateEnabled || StayUpLateEnabled;
}
