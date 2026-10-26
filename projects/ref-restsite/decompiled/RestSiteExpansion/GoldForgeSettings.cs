namespace RestSiteExpansion;

public readonly record struct GoldForgeSettings(bool Enabled, int Cost, int FlatIncrement, bool Infinite)
{
	public static GoldForgeSettings Disabled => new GoldForgeSettings(Enabled: false, 0, 0, Infinite: false);
}
