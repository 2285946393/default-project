using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class GoldEnchantRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT";

	private int _enchantCountThisRestSite;

	public override string OptionId => "GOLD_ENCHANT";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT.title");

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/gold_enchant.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveGoldEnchant(((RestSiteOption)this).Owner).Enabled;

	public override LocString Description
	{
		get
		{
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Expected O, but got Unknown
			GoldEnchantSettings settings = RestSiteExpansionRuntimeConfig.ResolveGoldEnchant(((RestSiteOption)this).Owner);
			int num = HpCostFor(in settings);
			string text = ((!settings.Enabled) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT.descriptionDisabled" : ((((RestSiteOption)this).Owner.Creature.MaxHp - num < 1) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT.descriptionNoHp" : ((!GoldEnchantPool.HasAnyEnchantable(((RestSiteOption)this).Owner)) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT.descriptionNoEnchantable" : ((!settings.Infinite) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT.description" : ((settings.HpIncrement <= 0) ? "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT.descriptionInfinite" : "REST_SITE_EXPANSION_RESTSITE_GOLD_ENCHANT.descriptionInfiniteAscending")))));
			LocString val = new LocString("relics", text);
			val.Add("Cost", (decimal)num);
			val.Add("MaxHp", (decimal)((RestSiteOption)this).Owner.Creature.MaxHp);
			val.Add("Increment", (decimal)settings.HpIncrement);
			return val;
		}
	}

	public GoldEnchantRestSiteOption(Player owner)
		: base(owner)
	{
		GoldEnchantSettings goldEnchantSettings = RestSiteExpansionRuntimeConfig.ResolveGoldEnchant(owner);
		MainFile.Logger.Info($"[GoldEnchant] 选项创建：player={owner.NetId} 开关={goldEnchantSettings.Enabled} 生命消耗={goldEnchantSettings.HpCost} 无限={goldEnchantSettings.Infinite} 最大生命={owner.Creature.MaxHp} 池大小={GoldEnchantPool.Count} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	private int HpCostFor(in GoldEnchantSettings settings)
	{
		long num = settings.HpCost + (long)settings.HpIncrement * (long)_enchantCountThisRestSite;
		return (int)((num >= int.MaxValue) ? int.MaxValue : num);
	}

	public override async Task<bool> OnSelect()
	{
		GoldEnchantSettings settings = RestSiteExpansionRuntimeConfig.ResolveGoldEnchant(((RestSiteOption)this).Owner);
		int hpCost = HpCostFor(in settings);
		Creature creature = ((RestSiteOption)this).Owner.Creature;
		if (!settings.Enabled || creature.MaxHp - hpCost < 1)
		{
			MainFile.Logger.Info($"[GoldEnchant] OnSelect 快速失败：开关={settings.Enabled} 最大生命={creature.MaxHp} 消耗={hpCost}", 1);
			return false;
		}
		if (!GoldEnchantPool.HasAnyEnchantable(((RestSiteOption)this).Owner))
		{
			MainFile.Logger.Info("[GoldEnchant] OnSelect 快速失败：牌库中没有可附魔的卡牌", 1);
			return false;
		}
		MainFile.Logger.Info($"[GoldEnchant] OnSelect 进入：player={((RestSiteOption)this).Owner.NetId} 最大生命={creature.MaxHp} 消耗={hpCost} 无限={settings.Infinite} 本休息点已附魔={_enchantCountThisRestSite}（盲选流程：先选牌、后揭晓）", 1);
		CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		((CardSelectorPrefs)(ref val)).set_Cancelable(true);
		((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
		CardSelectorPrefs prefs = val;
		CardModel chosen = (await CardSelectCmd.FromDeckGeneric(((RestSiteOption)this).Owner, prefs, (Func<CardModel, bool>)GoldEnchantPool.CanAnyEnchant, (Func<CardModel, int>)null)).FirstOrDefault();
		if (chosen == null)
		{
			MainFile.Logger.Info("[GoldEnchant] 选牌取消/无候选：未消耗、不扣钱", 1);
			return false;
		}
		GoldEnchantPool.Entry? rolled = GoldEnchantPool.DrawFor(chosen, ((RestSiteOption)this).Owner.RunState.Rng.Niche);
		if (!rolled.HasValue)
		{
			MainFile.Logger.Warn("[GoldEnchant] 所选卡无可用附魔（" + ((AbstractModel)chosen).Id.Entry + "），本次跳过。", 1);
			return false;
		}
		GoldEnchantPool.Entry entry = rolled.Value;
		MainFile.Logger.Info($"[GoldEnchant] 选中卡={((AbstractModel)chosen).Id.Entry} 揭晓附魔={entry.Id} 强度={entry.Amount}", 1);
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), creature, (decimal)hpCost, false);
		EnchantmentModel instance = entry.Canonical.ToMutable();
		if (instance != null)
		{
			CardCmd.Enchant(instance, chosen, (decimal)entry.Amount);
			NCardEnchantVfx vfx = NCardEnchantVfx.Create(chosen);
			if (vfx != null)
			{
				NRun instance2 = NRun.Instance;
				if (instance2 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance2.GlobalUi.CardPreviewContainer, (Node)(object)vfx);
				}
			}
			MainFile.Logger.Info($"[GoldEnchant] 已附魔：{((AbstractModel)chosen).Id.Entry} ← {entry.Id}（强度 {entry.Amount}）", 1);
		}
		if (settings.Infinite)
		{
			_enchantCountThisRestSite++;
			MainFile.Logger.Info($"[GoldEnchant] 无限模式完成：失去 {hpCost} 最大生命，{((AbstractModel)chosen).Id.Entry} ← {entry.Id}（本休息点第 {_enchantCountThisRestSite} 次）", 1);
			return false;
		}
		MainFile.Logger.Info($"[GoldEnchant] 默认模式完成：失去 {hpCost} 最大生命，{((AbstractModel)chosen).Id.Entry} ← {entry.Id}，选项消耗", 1);
		return true;
	}
}
