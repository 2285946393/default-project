using System;
using System.Collections.Generic;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace RestSiteExpansion;

public static class ModConfigPage
{
	private const string DataKey = "mod_config";

	private const string LocTable = "relics";

	private const string KeyPrefix = "REST_SITE_EXPANSION_SETTINGS_";

	public const int GoldForgeCostMin = 0;

	public const int GoldForgeCostMax = 999;

	public const int GoldForgeFlatIncrementMin = 0;

	public const int GoldForgeFlatIncrementMax = 999;

	public const int GoldEnchantHpCostMin = 0;

	public const int GoldEnchantHpCostMax = 99;

	public const int GoldEnchantHpIncrementMin = 0;

	public const int GoldEnchantHpIncrementMax = 99;

	public const int BuriedGoldDepositMin = 1;

	public const int BuriedGoldDepositMax = 999;

	public const int BuriedGoldRecoverMin = 1;

	public const int BuriedGoldRecoverMax = 999;

	public const int BuriedGoldFeeMin = 0;

	public const int BuriedGoldFeeMax = 95;

	public const decimal CostGrowthPerUse = 1.5m;

	private static readonly ModSettingsValueBinding<ModConfigData, ModConfigData> RootBinding = new ModSettingsValueBinding<ModConfigData, ModConfigData>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, ModConfigData>)((ModConfigData s) => s), (Action<ModConfigData, ModConfigData>)delegate
	{
	});

	public static ModSettingsValueBinding<ModConfigData, bool> EnableRestSiteExpansionBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, int> GoldForgeCostBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> InfiniteGoldForgeBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, int> GoldForgeFlatIncrementBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableGoldEnchantBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, int> GoldEnchantHpCostBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> InfiniteGoldEnchantBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, int> GoldEnchantHpIncrementBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableBuriedGoldBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> BuriedGoldMultiplayerBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableBuryCardBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, int> BuriedGoldDepositBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, int> BuriedGoldRecoverBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, int> BuriedGoldFeeBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableChoicesLayoutBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableNapBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableDeepSleepBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> SleepExclusiveBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableMeditateBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableStayUpLateBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableRoughForgeBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableBlindForgeBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableWhackFlyconidBinding { get; private set; } = null;


	public static ModSettingsValueBinding<ModConfigData, bool> EnableRecklessDigBinding { get; private set; } = null;


	public static int GoldForgeCost => Math.Clamp(ReadConfig().GoldForgeCost, 0, 999);

	public static int GoldForgeFlatIncrement => Math.Clamp(ReadConfig().GoldForgeFlatIncrement, 0, 999);

	public static int GoldEnchantHpCost => Math.Clamp(ReadConfig().GoldEnchantHpCost, 0, 99);

	public static int GoldEnchantHpIncrement => Math.Clamp(ReadConfig().GoldEnchantHpIncrement, 0, 99);

	public static int BuriedGoldDeposit => Math.Clamp(ReadConfig().BuriedGoldDeposit, 1, 999);

	public static int BuriedGoldRecover => Math.Clamp(ReadConfig().BuriedGoldRecover, 1, 999);

	public static int BuriedGoldFee => Math.Clamp(ReadConfig().BuriedGoldFee, 0, 95);

	private static ModSettingsText Loc(string keySuffix, string fallback)
	{
		return ModSettingsText.LocString("relics", "REST_SITE_EXPANSION_SETTINGS_" + keySuffix, fallback);
	}

	public static ModConfigData ReadConfig()
	{
		return RootBinding.Read();
	}

	public static void Register()
	{
		EnableRestSiteExpansionBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableRestSiteExpansion), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableRestSiteExpansion = v;
		});
		GoldForgeCostBinding = new ModSettingsValueBinding<ModConfigData, int>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, int>)((ModConfigData s) => s.GoldForgeCost), (Action<ModConfigData, int>)delegate(ModConfigData s, int v)
		{
			s.GoldForgeCost = v;
		});
		InfiniteGoldForgeBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.InfiniteGoldForge), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.InfiniteGoldForge = v;
		});
		GoldForgeFlatIncrementBinding = new ModSettingsValueBinding<ModConfigData, int>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, int>)((ModConfigData s) => s.GoldForgeFlatIncrement), (Action<ModConfigData, int>)delegate(ModConfigData s, int v)
		{
			s.GoldForgeFlatIncrement = v;
		});
		EnableGoldEnchantBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableGoldEnchant), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableGoldEnchant = v;
		});
		GoldEnchantHpCostBinding = new ModSettingsValueBinding<ModConfigData, int>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, int>)((ModConfigData s) => s.GoldEnchantHpCost), (Action<ModConfigData, int>)delegate(ModConfigData s, int v)
		{
			s.GoldEnchantHpCost = v;
		});
		InfiniteGoldEnchantBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.InfiniteGoldEnchant), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.InfiniteGoldEnchant = v;
		});
		GoldEnchantHpIncrementBinding = new ModSettingsValueBinding<ModConfigData, int>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, int>)((ModConfigData s) => s.GoldEnchantHpIncrement), (Action<ModConfigData, int>)delegate(ModConfigData s, int v)
		{
			s.GoldEnchantHpIncrement = v;
		});
		EnableBuriedGoldBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableBuriedGold), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableBuriedGold = v;
		});
		BuriedGoldMultiplayerBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.BuriedGoldMultiplayer), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.BuriedGoldMultiplayer = v;
		});
		EnableBuryCardBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableBuryCard), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableBuryCard = v;
		});
		BuriedGoldDepositBinding = new ModSettingsValueBinding<ModConfigData, int>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, int>)((ModConfigData s) => s.BuriedGoldDeposit), (Action<ModConfigData, int>)delegate(ModConfigData s, int v)
		{
			s.BuriedGoldDeposit = v;
		});
		BuriedGoldRecoverBinding = new ModSettingsValueBinding<ModConfigData, int>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, int>)((ModConfigData s) => s.BuriedGoldRecover), (Action<ModConfigData, int>)delegate(ModConfigData s, int v)
		{
			s.BuriedGoldRecover = v;
		});
		BuriedGoldFeeBinding = new ModSettingsValueBinding<ModConfigData, int>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, int>)((ModConfigData s) => s.BuriedGoldFee), (Action<ModConfigData, int>)delegate(ModConfigData s, int v)
		{
			s.BuriedGoldFee = v;
		});
		EnableChoicesLayoutBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableChoicesLayout), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableChoicesLayout = v;
		});
		EnableNapBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableNap), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableNap = v;
		});
		EnableDeepSleepBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableDeepSleep), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableDeepSleep = v;
		});
		SleepExclusiveBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.SleepExclusive), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.SleepExclusive = v;
		});
		EnableMeditateBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableMeditate), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableMeditate = v;
		});
		EnableStayUpLateBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableStayUpLate), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableStayUpLate = v;
		});
		EnableRoughForgeBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableRoughForge), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableRoughForge = v;
		});
		EnableBlindForgeBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableBlindForge), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableBlindForge = v;
		});
		EnableWhackFlyconidBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableWhackFlyconid), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableWhackFlyconid = v;
		});
		EnableRecklessDigBinding = new ModSettingsValueBinding<ModConfigData, bool>("RestSiteExpansion", "mod_config", (SaveScope)0, (Func<ModConfigData, bool>)((ModConfigData s) => s.EnableRecklessDig), (Action<ModConfigData, bool>)delegate(ModConfigData s, bool v)
		{
			s.EnableRecklessDig = v;
		});
		ModDataStore.For("RestSiteExpansion").Register<ModConfigData>("mod_config", "mod_config.json", (SaveScope)0, (Func<ModConfigData>)(() => new ModConfigData()), true, (ModDataMigrationConfig)null, (IEnumerable<IMigration>)null);
		RitsuLibFramework.RegisterModSettings("RestSiteExpansion", (Action<ModSettingsPageBuilder>)delegate(ModSettingsPageBuilder page)
		{
			page.WithTitle(Loc("PAGE_TITLE", "休息处扩展")).WithModDisplayName(Loc("PAGE_TITLE", "休息处扩展")).WithVisibleOnHostSurfaces((ModSettingsHostSurface)3);
			page.AddSection("notice", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("NOTICE_TITLE", "⚠ 注意")).AddButton("notice_body", Loc("NOTICE_BODY", "单击改动即生效，联机时以房主配置为主"), Loc("NOTICE_BUTTON", "我知道了"), (Action<IModSettingsUiActionHost>)delegate
				{
				}, (ModSettingsButtonTone)1, (ModSettingsText)null);
			});
			page.AddSection("general", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_GENERAL", "全局")).AddToggle("enable_mod", Loc("TOGGLE_ENABLE", "启用MOD"), (IModSettingsValueBinding<bool>)(object)EnableRestSiteExpansionBinding, Loc("DESC_ENABLE", "关闭后本 mod 完全失效，休息处不再出现本 mod 的选项。"), (Func<bool>)null).AddToggle("sleep_exclusive", Loc("TOGGLE_SLEEP_EXCLUSIVE", "帐篷平衡"), (IModSettingsValueBinding<bool>)(object)SleepExclusiveBinding, Loc("DESC_SLEEP_EXCLUSIVE", "开启后，各子菜单内部（休息 / 锻造 / 埋藏卡牌）只能选一项（即使有帐篷），不同子菜单之间互不影响。默认关闭。"), (Func<bool>)null);
			});
			page.AddSection("gold_forge", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_GOLD_FORGE", "金币锻造")).AddIntSlider("gold_forge_cost", Loc("SLIDER_COST", "锻造费用"), (IModSettingsValueBinding<int>)(object)GoldForgeCostBinding, 0, 999, 1, (Func<int, string>)null, Loc("DESC_COST", "基础花费（金币）。")).AddToggle("infinite_gold_forge", Loc("TOGGLE_INFINITE", "无限锻造（涨价）"), (IModSettingsValueBinding<bool>)(object)InfiniteGoldForgeBinding, Loc("DESC_INFINITE", "开启后可反复使用、每次涨价；关闭时每个休息点限一次。"), (Func<bool>)null)
					.AddIntSlider("gold_forge_flat_increment", Loc("SLIDER_FLAT", "固定金额增量"), (IModSettingsValueBinding<int>)(object)GoldForgeFlatIncrementBinding, 0, 999, 1, (Func<int, string>)null, Loc("DESC_FLAT", "每次涨价额外增加的固定金币数（0 = 仅复利）。"))
					.WithEntryVisibleWhen("gold_forge_flat_increment", (Func<bool>)(() => ReadConfig().InfiniteGoldForge));
			});
			page.AddSection("gold_enchant", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_GOLD_ENCHANT", "生命附魔")).AddToggle("enable_gold_enchant", Loc("TOGGLE_ENCHANT", "启用生命附魔"), (IModSettingsValueBinding<bool>)(object)EnableGoldEnchantBinding, Loc("DESC_ENCHANT", "关闭后休息处不再出现「生命附魔」。"), (Func<bool>)null).AddIntSlider("gold_enchant_hp_cost", Loc("SLIDER_ENCHANT_COST", "生命消耗"), (IModSettingsValueBinding<int>)(object)GoldEnchantHpCostBinding, 0, 99, 1, (Func<int, string>)null, Loc("DESC_ENCHANT_COST", "每次消耗的最大生命（默认 10）。"))
					.AddToggle("infinite_gold_enchant", Loc("TOGGLE_ENCHANT_INFINITE", "无限附魔"), (IModSettingsValueBinding<bool>)(object)InfiniteGoldEnchantBinding, Loc("DESC_ENCHANT_INFINITE", "开启后可反复使用；关闭时每个休息点限一次。"), (Func<bool>)null)
					.AddIntSlider("gold_enchant_hp_increment", Loc("SLIDER_ENCHANT_FLAT", "生命消耗递增"), (IModSettingsValueBinding<int>)(object)GoldEnchantHpIncrementBinding, 0, 99, 1, (Func<int, string>)null, Loc("DESC_ENCHANT_FLAT", "每次使用后额外增加的最大生命消耗（0 = 不递增）。"))
					.WithEntryVisibleWhen("gold_enchant_hp_increment", (Func<bool>)(() => ReadConfig().InfiniteGoldEnchant));
			});
			page.AddSection("buried_gold", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_BURIED_GOLD", "埋藏")).AddToggle("buried_gold_multiplayer", Loc("TOGGLE_BURIED_MP", "联机时启用"), (IModSettingsValueBinding<bool>)(object)BuriedGoldMultiplayerBinding, Loc("DESC_BURIED_MP", "联机跑局中也启用整个埋藏模块（金币与卡牌）。默认关闭。"), (Func<bool>)null).AddToggle("enable_buried_gold", Loc("TOGGLE_BURIED_GOLD", "启用掩埋金币"), (IModSettingsValueBinding<bool>)(object)EnableBuriedGoldBinding, Loc("DESC_BURIED_GOLD", "关闭后休息处不再出现「掩埋金币」「回收金币」。"), (Func<bool>)null)
					.AddToggle("enable_bury_card", Loc("TOGGLE_BURY_CARD", "启用掩埋卡牌"), (IModSettingsValueBinding<bool>)(object)EnableBuryCardBinding, Loc("DESC_BURY_CARD", "休息处可把一张牌埋藏起来（容量 1 张，跨战斗/跨会话保留），随时取回。"), (Func<bool>)null)
					.AddIntSlider("buried_gold_deposit", Loc("SLIDER_BURY_DEPOSIT", "单次存款额"), (IModSettingsValueBinding<int>)(object)BuriedGoldDepositBinding, 1, 999, 1, (Func<int, string>)null, Loc("DESC_BURY_DEPOSIT", "每次「掩埋金币」埋入的金币数（默认 50）。"))
					.AddIntSlider("buried_gold_recover", Loc("SLIDER_BURY_RECOVER", "单次取款额"), (IModSettingsValueBinding<int>)(object)BuriedGoldRecoverBinding, 1, 999, 1, (Func<int, string>)null, Loc("DESC_BURY_RECOVER", "每次「回收金币」取出的金币数（默认 50；余额不足时取出剩余全部）。"))
					.AddIntSlider("buried_gold_fee", Loc("SLIDER_BURY_FEE", "取款手续费（%）"), (IModSettingsValueBinding<int>)(object)BuriedGoldFeeBinding, 0, 95, 1, (Func<int, string>)null, Loc("DESC_BURY_FEE", "回收时扣除的手续费百分比（默认 10，范围 0-95）。"));
			});
			page.AddSection("sleep", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_SLEEP", "睡眠")).AddToggle("enable_nap", Loc("TOGGLE_NAP", "启用打盹"), (IModSettingsValueBinding<bool>)(object)EnableNapBinding, Loc("DESC_NAP", "恢复 15% 最大生命；下一场战斗开始时额外获得 1 点能量并额外抽 2 张牌。"), (Func<bool>)null).AddToggle("enable_deep_sleep", Loc("TOGGLE_DEEP_SLEEP", "启用酣眠"), (IModSettingsValueBinding<bool>)(object)EnableDeepSleepBinding, Loc("DESC_DEEP_SLEEP", "恢复 50% 最大生命；下一场战斗开始时失去 1 点力量和 1 点敏捷。"), (Func<bool>)null)
					.AddToggle("enable_meditate", Loc("TOGGLE_MEDITATE", "启用冥想"), (IModSettingsValueBinding<bool>)(object)EnableMeditateBinding, Loc("DESC_MEDITATE", "最大生命上限 +10%（当前生命同步提升）。"), (Func<bool>)null)
					.AddToggle("enable_stay_up_late", Loc("TOGGLE_STAY_UP_LATE", "启用熬夜"), (IModSettingsValueBinding<bool>)(object)EnableStayUpLateBinding, Loc("DESC_STAY_UP_LATE", "下一场战斗开始时将 4 张「睡眠不佳」加入手牌；该场战斗最大能量 +1。"), (Func<bool>)null);
			});
			page.AddSection("forge_extra", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_FORGE_EXTRA", "锻造")).AddToggle("enable_rough_forge", Loc("TOGGLE_ROUGH_FORGE", "启用狠造"), (IModSettingsValueBinding<bool>)(object)EnableRoughForgeBinding, Loc("DESC_ROUGH_FORGE", "选择两张牌升级，并往牌库加入一张「伤口」。"), (Func<bool>)null).AddToggle("enable_blind_forge", Loc("TOGGLE_BLIND_FORGE", "启用瞎造"), (IModSettingsValueBinding<bool>)(object)EnableBlindForgeBinding, Loc("DESC_BLIND_FORGE", "从牌库随机升级两张可升级的牌（你无法选牌）。"), (Func<bool>)null);
			});
			page.AddSection("fun", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_FUN", "娱乐")).AddToggle("enable_whack_flyconid", Loc("TOGGLE_WHACK_FLYCONID", "启用打飞蝇菌子"), (IModSettingsValueBinding<bool>)(object)EnableWhackFlyconidBinding, Loc("DESC_WHACK_FLYCONID", "休息处可拍打飞蝇菌子：打满 60 次回复 5 点生命、失去 1 点最大生命。"), (Func<bool>)null).AddToggle("enable_reckless_dig", Loc("TOGGLE_RECKLESS_DIG", "启用乱挖一通"), (IModSettingsValueBinding<bool>)(object)EnableRecklessDigBinding, Loc("DESC_RECKLESS_DIG", "休息处可挖出一个随机遗物；下一场战斗的第一回合能量会被清空。"), (Func<bool>)null);
			});
			page.AddSection("ui", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
			{
				section.WithTitle(Loc("SECTION_UI", "界面")).AddToggle("choices_layout", Loc("TOGGLE_CHOICES_LAYOUT", "多选项布局适配"), (IModSettingsValueBinding<bool>)(object)EnableChoicesLayoutBinding, Loc("DESC_CHOICES_LAYOUT", "选项多到一行放不下时自动换行并收紧间距；超过两行可用滚轮滚动。仅影响本机显示。"), (Func<bool>)null);
			});
		}, (string)null);
	}
}
