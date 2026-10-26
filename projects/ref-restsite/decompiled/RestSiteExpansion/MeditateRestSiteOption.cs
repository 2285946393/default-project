using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class MeditateRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_MEDITATE";

	private const decimal MaxHpGainRatio = 0.10m;

	public override string OptionId => "MEDITATE";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_MEDITATE.title");

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
			LocString val = new LocString("relics", ((RestSiteOption)this).IsEnabled ? "REST_SITE_EXPANSION_RESTSITE_MEDITATE.description" : "REST_SITE_EXPANSION_RESTSITE_MEDITATE.descriptionDisabled");
			val.Add("MaxHpGain", MaxHpGain(((RestSiteOption)this).Owner));
			return val;
		}
	}

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/meditate.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveSleep(((RestSiteOption)this).Owner).MeditateEnabled;

	public MeditateRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[Meditate] 冥想选项创建：player={owner.NetId} 上限提升={MaxHpGain(owner)} 最大生命={owner.Creature.MaxHp} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override async Task<bool> OnSelect()
	{
		if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
		{
			MainFile.Logger.Info($"[TentBalance] {((RestSiteOption)this).OptionId} 被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(((RestSiteOption)this).OptionId)}）：player={((RestSiteOption)this).Owner.NetId} 已选={TentBalanceGate.DescribeChoices(((RestSiteOption)this).Owner)}", 1);
			return false;
		}
		if (!RestSiteExpansionRuntimeConfig.ResolveSleep(((RestSiteOption)this).Owner).MeditateEnabled)
		{
			MainFile.Logger.Info($"[Meditate] 已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		decimal gain = MaxHpGain(((RestSiteOption)this).Owner);
		if (gain <= 0m)
		{
			MainFile.Logger.Info($"[Meditate] 提升量为 0（最大生命={((RestSiteOption)this).Owner.Creature.MaxHp}），不消耗：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		await CreatureCmd.GainMaxHp(((RestSiteOption)this).Owner.Creature, gain);
		MainFile.Logger.Info($"[Meditate] 冥想完成：player={((RestSiteOption)this).Owner.NetId} 最大生命 +{gain:0} → {((RestSiteOption)this).Owner.Creature.MaxHp}", 1);
		return true;
	}

	private static decimal MaxHpGain(Player player)
	{
		return Math.Floor((decimal)player.Creature.MaxHp * 0.10m);
	}
}
