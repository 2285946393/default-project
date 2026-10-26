using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class BuryCardRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_BURY_CARD";

	public override string OptionId => "BURY_CARD";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_BURY_CARD.title");

	public override LocString Description
	{
		get
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Expected O, but got Unknown
			if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
			{
				return new LocString("relics", TentBalanceGate.BlockedLocKeyFor(((RestSiteOption)this).OptionId));
			}
			bool flag = BuriedCardRuntime.HasCard(((RestSiteOption)this).Owner);
			string text = ((!((RestSiteOption)this).IsEnabled) ? "REST_SITE_EXPANSION_RESTSITE_BURY_CARD.descriptionDisabled" : (flag ? "REST_SITE_EXPANSION_RESTSITE_BURY_CARD.descriptionFull" : "REST_SITE_EXPANSION_RESTSITE_BURY_CARD.description"));
			LocString val = new LocString("relics", text);
			val.Add("Card", BuriedCardRuntime.CardDisplayName(BuriedCardRuntime.GetCard(((RestSiteOption)this).Owner)));
			return val;
		}
	}

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/bury_card.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner).CardEnabled;

	public BuryCardRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[BuriedCard] 掩埋卡牌选项创建：player={owner.NetId} 已埋={(BuriedCardRuntime.HasCard(owner) ? "有" : "无")} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override async Task<bool> OnSelect()
	{
		if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
		{
			MainFile.Logger.Info($"[TentBalance] {((RestSiteOption)this).OptionId} 被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(((RestSiteOption)this).OptionId)}）：player={((RestSiteOption)this).Owner.NetId} 已选={TentBalanceGate.DescribeChoices(((RestSiteOption)this).Owner)}", 1);
			return false;
		}
		if (!RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner).CardEnabled)
		{
			MainFile.Logger.Info($"[BuriedCard] 掩埋卡牌已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		if (BuriedCardRuntime.HasCard(((RestSiteOption)this).Owner))
		{
			MainFile.Logger.Info($"[BuriedCard] 埋藏位已满（容量 1），不消耗：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		((CardSelectorPrefs)(ref val)).set_Cancelable(true);
		((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
		CardSelectorPrefs prefs = val;
		List<CardModel> selection = (await CardSelectCmd.FromDeckForRemoval(((RestSiteOption)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList();
		if (selection.Count == 0)
		{
			MainFile.Logger.Info($"[BuriedCard] 选牌取消/无可选牌，不消耗：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		CardModel card = selection[0];
		SerializableCard data = card.ToSerializable();
		await CardPileCmd.RemoveFromDeck(card, true);
		BuriedCardRuntime.SetCard(((RestSiteOption)this).Owner, data);
		Logger logger = MainFile.Logger;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 4);
		defaultInterpolatedStringHandler.AppendLiteral("[BuriedCard] 已埋藏 ");
		ModelId id = data.Id;
		defaultInterpolatedStringHandler.AppendFormatted((id != null) ? id.Entry : null);
		defaultInterpolatedStringHandler.AppendLiteral("（升级=");
		defaultInterpolatedStringHandler.AppendFormatted(data.CurrentUpgradeLevel);
		defaultInterpolatedStringHandler.AppendLiteral(" ");
		defaultInterpolatedStringHandler.AppendLiteral("附魔=");
		SerializableEnchantment enchantment = data.Enchantment;
		object obj;
		if (enchantment == null)
		{
			obj = null;
		}
		else
		{
			ModelId id2 = enchantment.Id;
			obj = ((id2 != null) ? id2.Entry : null);
		}
		if (obj == null)
		{
			obj = "无";
		}
		defaultInterpolatedStringHandler.AppendFormatted((string?)obj);
		defaultInterpolatedStringHandler.AppendLiteral("）：player=");
		defaultInterpolatedStringHandler.AppendFormatted(((RestSiteOption)this).Owner.NetId);
		logger.Info(defaultInterpolatedStringHandler.ToStringAndClear(), 1);
		return true;
	}
}
