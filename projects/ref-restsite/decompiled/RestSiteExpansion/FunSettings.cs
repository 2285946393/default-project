namespace RestSiteExpansion;

public readonly record struct FunSettings(bool WhackFlyconidEnabled, bool RecklessDigEnabled)
{
	public static FunSettings Disabled => new FunSettings(WhackFlyconidEnabled: false, RecklessDigEnabled: false);

	public bool AnyEnabled => WhackFlyconidEnabled || RecklessDigEnabled;
}
