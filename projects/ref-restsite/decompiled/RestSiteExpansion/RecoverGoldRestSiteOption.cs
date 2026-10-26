using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class RecoverGoldRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_RECOVER_GOLD";

	public override string OptionId => "RECOVER_GOLD";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_RECOVER_GOLD.title");

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/recover_gold.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner).Enabled;

	public override LocString Description
	{
		get
		{
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Expected O, but got Unknown
			BuriedGoldSettings settings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner);
			int amount = BuriedGoldRuntime.GetAmount(((RestSiteOption)this).Owner);
			string text = ((!settings.Enabled) ? "REST_SITE_EXPANSION_RESTSITE_RECOVER_GOLD.descriptionDisabled" : ((amount <= 0) ? "REST_SITE_EXPANSION_RESTSITE_RECOVER_GOLD.descriptionNoBuried" : "REST_SITE_EXPANSION_RESTSITE_RECOVER_GOLD.description"));
			(int Gross, int Net) tuple = Compute(amount, in settings);
			int item = tuple.Gross;
			int item2 = tuple.Net;
			LocString val = new LocString("relics", text);
			val.Add("Recover", (decimal)item);
			val.Add("Net", (decimal)item2);
			val.Add("Fee", (decimal)settings.FeePercent);
			val.Add("Buried", (decimal)amount);
			return val;
		}
	}

	public RecoverGoldRestSiteOption(Player owner)
		: base(owner)
	{
		BuriedGoldSettings buriedGoldSettings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(owner);
		MainFile.Logger.Info($"[BuriedGold] 回收选项创建：player={owner.NetId} 开关={buriedGoldSettings.Enabled} 单次={buriedGoldSettings.Recover} 手续费={buriedGoldSettings.FeePercent}% 存款={BuriedGoldRuntime.GetAmount(owner)} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	private static (int Gross, int Net) Compute(int balance, in BuriedGoldSettings settings)
	{
		int num = Math.Min(settings.Recover, balance);
		int item = (int)Math.Floor((decimal)(num * (100 - settings.FeePercent)) / 100m);
		return (Gross: num, Net: item);
	}

	public override Task<bool> OnSelect()
	{
		BuriedGoldSettings settings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner);
		int amount = BuriedGoldRuntime.GetAmount(((RestSiteOption)this).Owner);
		if (!settings.Enabled || amount <= 0)
		{
			MainFile.Logger.Info($"[BuriedGold] 回收快速失败：开关={settings.Enabled} 存款={amount}", 1);
			return Task.FromResult(result: false);
		}
		var (num, num2) = Compute(amount, in settings);
		GoldTransfer.CreditExact(((RestSiteOption)this).Owner, num2);
		BuriedGoldRuntime.SetAmount(((RestSiteOption)this).Owner, amount - num);
		MainFile.Logger.Info($"[BuriedGold] 已回收 {num}（手续费 {settings.FeePercent}% → 面额到账 {num2}）：存款余额 {amount - num}", 1);
		return Task.FromResult(result: false);
	}
}
