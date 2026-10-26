using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class RecoverCardRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_RECOVER_CARD";

	public override string OptionId => "RECOVER_CARD";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_RECOVER_CARD.title");

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
			SerializableCard card = BuriedCardRuntime.GetCard(((RestSiteOption)this).Owner);
			string text = ((!((RestSiteOption)this).IsEnabled) ? "REST_SITE_EXPANSION_RESTSITE_RECOVER_CARD.descriptionDisabled" : ((card == null) ? "REST_SITE_EXPANSION_RESTSITE_RECOVER_CARD.descriptionNoBuried" : "REST_SITE_EXPANSION_RESTSITE_RECOVER_CARD.description"));
			LocString val = new LocString("relics", text);
			val.Add("Card", BuriedCardRuntime.CardDisplayName(card));
			return val;
		}
	}

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/recover_card.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveBuriedGold(((RestSiteOption)this).Owner).CardEnabled;

	public RecoverCardRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[BuriedCard] 回收卡牌选项创建：player={owner.NetId} 埋藏={BuriedCardRuntime.CardDisplayName(BuriedCardRuntime.GetCard(owner))} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
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
			MainFile.Logger.Info($"[BuriedCard] 回收卡牌已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		SerializableCard data = BuriedCardRuntime.GetCard(((RestSiteOption)this).Owner);
		if (data == null)
		{
			MainFile.Logger.Info($"[BuriedCard] 埋藏位为空，不消耗：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		CardModel card = ((RestSiteOption)this).Owner.RunState.LoadCard(data, ((RestSiteOption)this).Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
		BuriedCardRuntime.SetCard(((RestSiteOption)this).Owner, null);
		Logger logger = MainFile.Logger;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 4);
		defaultInterpolatedStringHandler.AppendLiteral("[BuriedCard] 已取回 ");
		defaultInterpolatedStringHandler.AppendFormatted(((AbstractModel)card).Id.Entry);
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
			ModelId id = enchantment.Id;
			obj = ((id != null) ? id.Entry : null);
		}
		if (obj == null)
		{
			obj = "无";
		}
		defaultInterpolatedStringHandler.AppendFormatted((string?)obj);
		defaultInterpolatedStringHandler.AppendLiteral("）：player=");
		defaultInterpolatedStringHandler.AppendFormatted(((RestSiteOption)this).Owner.NetId);
		defaultInterpolatedStringHandler.AppendLiteral("，埋藏位已清空");
		logger.Info(defaultInterpolatedStringHandler.ToStringAndClear(), 1);
		return true;
	}
}
