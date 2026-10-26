using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class BlindForgeRestSiteOption : SmithRestSiteOptionBase
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_BLIND_FORGE";

	private const int UpgradeCount = 2;

	public override string OptionId => "BLIND_FORGE";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_BLIND_FORGE.title");

	public override LocString Description
	{
		get
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Expected O, but got Unknown
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Expected O, but got Unknown
			if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
			{
				return new LocString("relics", TentBalanceGate.BlockedLocKeyFor(((RestSiteOption)this).OptionId));
			}
			string text = ((!((RestSiteOption)this).IsEnabled) ? "REST_SITE_EXPANSION_RESTSITE_BLIND_FORGE.descriptionDisabled" : ((UpgradableCount(((RestSiteOption)this).Owner) < 2) ? "REST_SITE_EXPANSION_RESTSITE_BLIND_FORGE.descriptionNotEnough" : "REST_SITE_EXPANSION_RESTSITE_BLIND_FORGE.description"));
			return new LocString("relics", text);
		}
	}

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/blind_forge.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveExtraForge(((RestSiteOption)this).Owner).BlindEnabled;

	public BlindForgeRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[BlindForge] 瞎造选项创建：player={owner.NetId} 可升级牌={UpgradableCount(owner)} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override Task<bool> OnSelect()
	{
		if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
		{
			MainFile.Logger.Info($"[TentBalance] {((RestSiteOption)this).OptionId} 被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(((RestSiteOption)this).OptionId)}）：player={((RestSiteOption)this).Owner.NetId} 已选={TentBalanceGate.DescribeChoices(((RestSiteOption)this).Owner)}", 1);
			return Task.FromResult(result: false);
		}
		if (!RestSiteExpansionRuntimeConfig.ResolveExtraForge(((RestSiteOption)this).Owner).BlindEnabled)
		{
			MainFile.Logger.Info($"[BlindForge] 已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return Task.FromResult(result: false);
		}
		List<CardModel> list = ((RestSiteOption)this).Owner.Deck.Cards.Where((CardModel card) => card.IsUpgradable).ToList();
		if (list.Count < 2)
		{
			MainFile.Logger.Info($"[BlindForge] 可升级牌不足 {2} 张（{list.Count}），不消耗：player={((RestSiteOption)this).Owner.NetId}", 1);
			return Task.FromResult(result: false);
		}
		List<CardModel> list2 = IEnumerableExtensions.TakeRandom<CardModel>((IEnumerable<CardModel>)list, 2, ((RestSiteOption)this).Owner.RunState.Rng.Niche).ToList();
		foreach (CardModel item in list2)
		{
			CardCmd.Upgrade(item, (CardPreviewStyle)0);
		}
		MarkUpgraded(list2);
		MainFile.Logger.Info($"[BlindForge] 瞎造完成：player={((RestSiteOption)this).Owner.NetId} 随机升级 {string.Join("、", list2.Select((CardModel c) => ((AbstractModel)c).Id.Entry))}", 1);
		return Task.FromResult(result: true);
	}

	private static int UpgradableCount(Player player)
	{
		return player.Deck.Cards.Count((CardModel card) => card.IsUpgradable);
	}
}
