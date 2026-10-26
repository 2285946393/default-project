namespace RestSiteExpansion;

public sealed class RestSiteExpansionSnapshot
{
	public bool Enabled { get; set; } = true;


	public int Cost { get; set; } = 100;


	public int FlatIncrement { get; set; }

	public bool Infinite { get; set; }

	public bool EnchantEnabled { get; set; } = true;


	public int EnchantHpCost { get; set; } = 10;


	public int EnchantHpIncrement { get; set; }

	public bool EnchantInfinite { get; set; }

	public bool BuryEnabled { get; set; } = true;


	public bool BuryCardEnabled { get; set; } = true;


	public bool BuryMultiplayerEnabled { get; set; }

	public int BuryDeposit { get; set; } = 50;


	public int BuryRecover { get; set; } = 50;


	public int BuryFeePercent { get; set; } = 10;


	public bool NapEnabled { get; set; } = true;


	public bool DeepSleepEnabled { get; set; } = true;


	public bool SleepExclusive { get; set; }

	public bool MeditateEnabled { get; set; } = true;


	public bool StayUpLateEnabled { get; set; } = true;


	public bool RoughForgeEnabled { get; set; } = true;


	public bool BlindForgeEnabled { get; set; } = true;


	public bool WhackFlyconidEnabled { get; set; } = true;


	public bool RecklessDigEnabled { get; set; } = true;

}
