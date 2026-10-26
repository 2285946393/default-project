using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class GoldForgeRestSiteOption : SmithRestSiteOptionBase
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE";

	private int _forgedThisRestSite;

	public override string OptionId => "GOLD_FORGE";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE.title");

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/gold_forge.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveGoldForge(((RestSiteOption)this).Owner).Enabled;

	public override LocString Description
	{
		get
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Expected O, but got Unknown
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Expected O, but got Unknown
			if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
			{
				return new LocString("relics", TentBalanceGate.BlockedLocKeyFor(((RestSiteOption)this).OptionId));
			}
			GoldForgeSettings settings = RestSiteExpansionRuntimeConfig.ResolveGoldForge(((RestSiteOption)this).Owner);
			int num = CostFor(in settings);
			string text = ((!settings.Enabled) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE.descriptionDisabled" : ((((RestSiteOption)this).Owner.Gold < num) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE.descriptionNoGold" : ((((RestSiteOption)this).Owner.Deck.UpgradableCardCount == 0) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE.descriptionNoUpgradable" : ((!settings.Infinite) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE.description" : ((settings.FlatIncrement <= 0) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE.descriptionInfinite" : "REST_SITE_EXPANSION_RESTSITE_GOLD_FORGE.descriptionInfiniteWithFlat")))));
			LocString val = new LocString("relics", text);
			val.Add("Cost", (decimal)num);
			val.Add("Gold", (decimal)((RestSiteOption)this).Owner.Gold);
			val.Add("Increment", (decimal)settings.FlatIncrement);
			return val;
		}
	}

	public GoldForgeRestSiteOption(Player owner)
		: base(owner)
	{
		GoldForgeSettings goldForgeSettings = RestSiteExpansionRuntimeConfig.ResolveGoldForge(owner);
		MainFile.Logger.Info($"[GoldForge] 选项创建：player={owner.NetId} 开关={goldForgeSettings.Enabled} 费用={goldForgeSettings.Cost} 无限={goldForgeSettings.Infinite} 金币={owner.Gold} 可升级牌={owner.Deck.UpgradableCardCount} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	private int CostFor(in GoldForgeSettings settings)
	{
		decimal num = settings.Cost;
		for (int i = 0; i < _forgedThisRestSite; i++)
		{
			num = Math.Round(num * 1.5m, 0, MidpointRounding.AwayFromZero) + (decimal)settings.FlatIncrement;
			if (num >= 2147483647m)
			{
				return int.MaxValue;
			}
		}
		return (int)num;
	}

	public override async Task<bool> OnSelect()
	{
		if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
		{
			MainFile.Logger.Info($"[TentBalance] {((RestSiteOption)this).OptionId} 被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(((RestSiteOption)this).OptionId)}）：player={((RestSiteOption)this).Owner.NetId} 已选={TentBalanceGate.DescribeChoices(((RestSiteOption)this).Owner)}", 1);
			return false;
		}
		GoldForgeSettings settings = RestSiteExpansionRuntimeConfig.ResolveGoldForge(((RestSiteOption)this).Owner);
		int cost = CostFor(in settings);
		int upgradable = ((RestSiteOption)this).Owner.Deck.UpgradableCardCount;
		MainFile.Logger.Info($"[GoldForge] OnSelect 进入：player={((RestSiteOption)this).Owner.NetId} 金币={((RestSiteOption)this).Owner.Gold} 价格={cost} 开关={settings.Enabled} 无限={settings.Infinite} 固定增量={settings.FlatIncrement} 可升级牌={upgradable} 本休息点已锻造={_forgedThisRestSite}", 1);
		if (!settings.Enabled || ((RestSiteOption)this).Owner.Gold < cost || upgradable == 0)
		{
			MainFile.Logger.Info($"[GoldForge] OnSelect 快速失败（未消耗、选项保留）：开关={settings.Enabled} 金币够={((RestSiteOption)this).Owner.Gold >= cost} 有可升级牌={upgradable != 0}", 1);
			return false;
		}
		CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
		((CardSelectorPrefs)(ref val)).set_Cancelable(true);
		((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
		CardSelectorPrefs prefs = val;
		IEnumerable<CardModel> selection = await CardSelectCmd.FromDeckForUpgrade(((RestSiteOption)this).Owner, prefs);
		int selected = selection.Count();
		MainFile.Logger.Info($"[GoldForge] 选牌返回 {selected} 张", 1);
		if (selected == 0)
		{
			return false;
		}
		await PlayerCmd.LoseGold((decimal)cost, ((RestSiteOption)this).Owner, (GoldLossType)1);
		foreach (CardModel card in selection)
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)0);
		}
		MarkUpgraded(selection);
		if (settings.Infinite)
		{
			PlayLocalSmithVfx();
			_forgedThisRestSite++;
			MainFile.Logger.Info($"[GoldForge] 无限模式完成：扣 {cost} 金升级 {selected} 张，选项保留（本休息点第 {_forgedThisRestSite} 次）", 1);
			return false;
		}
		MainFile.Logger.Info($"[GoldForge] 默认模式完成：扣 {cost} 金升级 {selected} 张，选项消耗", 1);
		return true;
	}
}
