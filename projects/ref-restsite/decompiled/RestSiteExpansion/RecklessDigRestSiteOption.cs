using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class RecklessDigRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_RECKLESS_DIG";

	internal const int DebrisPerPile = 3;

	public override string OptionId => "RECKLESS_DIG";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_RECKLESS_DIG.title");

	public override LocString Description => new LocString("relics", ((RestSiteOption)this).IsEnabled ? "REST_SITE_EXPANSION_RESTSITE_RECKLESS_DIG.description" : "REST_SITE_EXPANSION_RESTSITE_RECKLESS_DIG.descriptionDisabled");

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/reckless_dig.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveFun(((RestSiteOption)this).Owner).RecklessDigEnabled;

	public RecklessDigRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[RecklessDig] 选项创建：player={owner.NetId} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override async Task<bool> OnSelect()
	{
		if (!((RestSiteOption)this).IsEnabled)
		{
			MainFile.Logger.Info($"[RecklessDig] 已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		RelicModel relic;
		try
		{
			relic = RelicFactory.PullNextRelicFromFront(((RestSiteOption)this).Owner).ToMutable();
			await RelicCmd.Obtain(relic, ((RestSiteOption)this).Owner, -1);
		}
		catch (Exception ex)
		{
			MainFile.Logger.Warn($"[RecklessDig] 遗物发放失败（不消耗、选项保留）：player={((RestSiteOption)this).Owner.NetId} {ex}", 1);
			return false;
		}
		SleepBuffRuntime.SetRecklessDig(((RestSiteOption)this).Owner, value: true);
		MainFile.Logger.Info($"[RecklessDig] 乱挖一通：player={((RestSiteOption)this).Owner.NetId} 获得遗物 {((AbstractModel)relic).Id.Entry}；" + "下一场战斗第一回合将清空能量", 1);
		return true;
	}
}
