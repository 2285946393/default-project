namespace RestSiteExpansion;

public sealed class ModConfigData
{
	public bool EnableRestSiteExpansion { get; set; } = true;


	public int GoldForgeCost { get; set; } = 100;


	public bool InfiniteGoldForge { get; set; }

	public int GoldForgeFlatIncrement { get; set; }

	public bool EnableGoldEnchant { get; set; } = true;


	public int GoldEnchantHpCost { get; set; } = 10;


	public bool InfiniteGoldEnchant { get; set; }

	public int GoldEnchantHpIncrement { get; set; }

	public bool EnableBuriedGold { get; set; } = true;


	public bool BuriedGoldMultiplayer { get; set; }

	public bool EnableBuryCard { get; set; } = true;


	public int BuriedGoldDeposit { get; set; } = 50;


	public int BuriedGoldRecover { get; set; } = 50;


	public int BuriedGoldFee { get; set; } = 10;


	public bool EnableNap { get; set; } = true;


	public bool EnableDeepSleep { get; set; } = true;


	public bool SleepExclusive { get; set; }

	public bool EnableMeditate { get; set; } = true;


	public bool EnableStayUpLate { get; set; } = true;


	public bool EnableRoughForge { get; set; } = true;


	public bool EnableBlindForge { get; set; } = true;


	public bool EnableWhackFlyconid { get; set; } = true;


	public bool EnableRecklessDig { get; set; } = true;


	public bool EnableChoicesLayout { get; set; } = true;

}
