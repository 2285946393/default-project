namespace RestSiteExpansion;

public readonly record struct ExtraForgeSettings(bool RoughEnabled, bool BlindEnabled)
{
	public static ExtraForgeSettings Disabled => new ExtraForgeSettings(RoughEnabled: false, BlindEnabled: false);

	public bool AnyEnabled => RoughEnabled || BlindEnabled;
}
