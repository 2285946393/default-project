namespace RestSiteExpansion;

public readonly record struct GoldEnchantSettings(bool Enabled, int HpCost, int HpIncrement, bool Infinite)
{
	public static GoldEnchantSettings Disabled => new GoldEnchantSettings(Enabled: false, 0, 0, Infinite: false);
}
