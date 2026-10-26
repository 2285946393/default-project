using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class StayUpLateRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_STAY_UP_LATE";

	internal const int PoorSleepCards = 4;

	internal const int MaxEnergyBonus = 1;

	public override string OptionId => "STAY_UP_LATE";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_STAY_UP_LATE.title");

	public override LocString Description
	{
		get
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Expected O, but got Unknown
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Expected O, but got Unknown
			if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
			{
				return new LocString("relics", TentBalanceGate.BlockedLocKeyFor(((RestSiteOption)this).OptionId));
			}
			return new LocString("relics", ((RestSiteOption)this).IsEnabled ? "REST_SITE_EXPANSION_RESTSITE_STAY_UP_LATE.description" : "REST_SITE_EXPANSION_RESTSITE_STAY_UP_LATE.descriptionDisabled");
		}
	}

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/stay_up_late.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveSleep(((RestSiteOption)this).Owner).StayUpLateEnabled;

	public StayUpLateRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[StayUpLate] 熬夜选项创建：player={owner.NetId} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override Task<bool> OnSelect()
	{
		if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
		{
			MainFile.Logger.Info($"[TentBalance] {((RestSiteOption)this).OptionId} 被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(((RestSiteOption)this).OptionId)}）：player={((RestSiteOption)this).Owner.NetId} 已选={TentBalanceGate.DescribeChoices(((RestSiteOption)this).Owner)}", 1);
			return Task.FromResult(result: false);
		}
		if (!RestSiteExpansionRuntimeConfig.ResolveSleep(((RestSiteOption)this).Owner).StayUpLateEnabled)
		{
			MainFile.Logger.Info($"[StayUpLate] 已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return Task.FromResult(result: false);
		}
		SleepBuffRuntime.SetStayUpLate(((RestSiteOption)this).Owner, value: true);
		MainFile.Logger.Info($"[StayUpLate] 熬夜完成：player={((RestSiteOption)this).Owner.NetId} 待生效（下战 {4} 张睡眠不佳入手 + 最大能量 +{1}）", 1);
		return Task.FromResult(result: true);
	}
}
