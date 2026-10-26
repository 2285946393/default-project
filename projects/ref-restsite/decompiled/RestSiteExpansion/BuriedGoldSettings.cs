namespace RestSiteExpansion;

public readonly record struct BuriedGoldSettings(bool Enabled, bool MultiplayerEnabled, int Deposit, int Recover, int FeePercent, bool CardEnabled)
{
	public static BuriedGoldSettings Disabled => new BuriedGoldSettings(Enabled: false, MultiplayerEnabled: false, 1, 1, 0, CardEnabled: false);
}
