using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class RoughForgeRestSiteOption : SmithRestSiteOptionBase
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_ROUGH_FORGE";

	private const int UpgradeCount = 2;

	public override string OptionId => "ROUGH_FORGE";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_ROUGH_FORGE.title");

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
			return new LocString("relics", ((RestSiteOption)this).IsEnabled ? "REST_SITE_EXPANSION_RESTSITE_ROUGH_FORGE.description" : "REST_SITE_EXPANSION_RESTSITE_ROUGH_FORGE.descriptionDisabled");
		}
	}

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/rough_forge.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveExtraForge(((RestSiteOption)this).Owner).RoughEnabled;

	public RoughForgeRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[RoughForge] 狠造选项创建：player={owner.NetId} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override async Task<bool> OnSelect()
	{
		if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
		{
			MainFile.Logger.Info($"[TentBalance] {((RestSiteOption)this).OptionId} 被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(((RestSiteOption)this).OptionId)}）：player={((RestSiteOption)this).Owner.NetId} 已选={TentBalanceGate.DescribeChoices(((RestSiteOption)this).Owner)}", 1);
			return false;
		}
		if (!RestSiteExpansionRuntimeConfig.ResolveExtraForge(((RestSiteOption)this).Owner).RoughEnabled)
		{
			MainFile.Logger.Info($"[RoughForge] 已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 2);
		((CardSelectorPrefs)(ref val)).set_Cancelable(true);
		((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
		CardSelectorPrefs prefs = val;
		List<CardModel> selection = (await CardSelectCmd.FromDeckForUpgrade(((RestSiteOption)this).Owner, prefs)).ToList();
		if (selection.Count == 0)
		{
			MainFile.Logger.Info("[RoughForge] 选牌取消：不升级、不加伤口，选项保留", 1);
			return false;
		}
		foreach (CardModel card in selection)
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)0);
		}
		MarkUpgraded(selection);
		await DeckCardGrant.AddToDeck<Wound>(((RestSiteOption)this).Owner, 1.2f);
		MainFile.Logger.Info($"[RoughForge] 狠造完成：player={((RestSiteOption)this).Owner.NetId} 升级 {selection.Count} 张 + 牌库加入伤口", 1);
		return true;
	}
}
