using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class NapRestSiteOption : SleepRestSiteOptionBase
{
	public override string OptionId => "NAP";

	protected override decimal HealRatio => 0.15m;

	public override LocString? CustomTitle => new LocString("relics", base.LocPrefix + ".title");

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/nap.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveSleep(((RestSiteOption)this).Owner).NapEnabled;

	public NapRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[Sleep] 打盹选项创建：player={owner.NetId} 回血={HealAmount(owner):0} 最大生命={owner.Creature.MaxHp} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	protected override void ApplyPendingEffect(Player player)
	{
		SleepBuffRuntime.SetNap(player, value: true);
	}
}
