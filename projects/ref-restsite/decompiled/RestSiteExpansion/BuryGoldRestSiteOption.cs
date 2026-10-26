using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class BuryGoldRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_BURY_GOLD";

	public override string OptionId => "BURY_GOLD";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_BURY_GOLD.title");

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/bury_gold.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner).Enabled;

	public override LocString Description
	{
		get
		{
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected O, but got Unknown
			BuriedGoldSettings buriedGoldSettings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner);
			int amount = BuriedGoldRuntime.GetAmount(((RestSiteOption)this).Owner);
			string text = ((!buriedGoldSettings.Enabled) ? "REST_SITE_EXPANSION_RESTSITE_BURY_GOLD.descriptionDisabled" : ((((RestSiteOption)this).Owner.Gold < buriedGoldSettings.Deposit) ? "REST_SITE_EXPANSION_RESTSITE_BURY_GOLD.descriptionNoGold" : "REST_SITE_EXPANSION_RESTSITE_BURY_GOLD.description"));
			LocString val = new LocString("relics", text);
			val.Add("Deposit", (decimal)buriedGoldSettings.Deposit);
			val.Add("Buried", (decimal)amount);
			val.Add("Gold", (decimal)((RestSiteOption)this).Owner.Gold);
			return val;
		}
	}

	public BuryGoldRestSiteOption(Player owner)
		: base(owner)
	{
		BuriedGoldSettings buriedGoldSettings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(owner);
		MainFile.Logger.Info($"[BuriedGold] 掩埋选项创建：player={owner.NetId} 开关={buriedGoldSettings.Enabled} 单次={buriedGoldSettings.Deposit} 金币={owner.Gold} 存款={BuriedGoldRuntime.GetAmount(owner)} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override async Task<bool> OnSelect()
	{
		BuriedGoldSettings settings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner);
		if (!settings.Enabled || ((RestSiteOption)this).Owner.Gold < settings.Deposit)
		{
			MainFile.Logger.Info($"[BuriedGold] 掩埋快速失败：开关={settings.Enabled} 金币={((RestSiteOption)this).Owner.Gold} 需要={settings.Deposit}", 1);
			return false;
		}
		await PlayerCmd.LoseGold((decimal)settings.Deposit, ((RestSiteOption)this).Owner, (GoldLossType)1);
		int balance = BuriedGoldRuntime.GetAmount(((RestSiteOption)this).Owner) + settings.Deposit;
		BuriedGoldRuntime.SetAmount(((RestSiteOption)this).Owner, balance);
		MainFile.Logger.Info($"[BuriedGold] 已掩埋 {settings.Deposit}：存款余额 {balance}", 1);
		return false;
	}
}
