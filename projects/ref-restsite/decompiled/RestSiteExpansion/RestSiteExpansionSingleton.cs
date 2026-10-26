using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using STS2RitsuLib.Models;

namespace RestSiteExpansion;

public sealed class RestSiteExpansionSingleton : HookedSingletonModel
{
	internal static readonly string[] DesiredOrder = new string[22]
	{
		"REST_MENU_BACK", "REST_MENU", "NAP", "HEAL", "DEEP_SLEEP", "MEDITATE", "STAY_UP_LATE", "GOLD_ENCHANT", "FORGE_MENU", "SMITH",
		"GOLD_FORGE", "ROUGH_FORGE", "BLIND_FORGE", "MEND", "GOLD_MENU", "BURY_GOLD", "RECOVER_GOLD", "BURY_CARD", "RECOVER_CARD", "FUN_MENU",
		"WHACK_FLYCONID", "RECKLESS_DIG"
	};

	public RestSiteExpansionSingleton()
		: base((HookType)2)
	{
	}

	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (!RestSiteExpansionRuntimeConfig.IsModEnabled(player))
		{
			MainFile.Logger.Info($"[RestSiteExpansion] 总开关关闭（启用MOD=false），不添加任何选项：player={player.NetId}", 1);
			return false;
		}
		GoldForgeSettings goldForgeSettings = RestSiteExpansionRuntimeConfig.ResolveGoldForge(player);
		GoldEnchantSettings goldEnchantSettings = RestSiteExpansionRuntimeConfig.ResolveGoldEnchant(player);
		BuriedGoldSettings buriedGoldSettings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(player);
		SleepSettings sleepSettings = RestSiteExpansionRuntimeConfig.ResolveSleep(player);
		ExtraForgeSettings extraForgeSettings = RestSiteExpansionRuntimeConfig.ResolveExtraForge(player);
		FunSettings funSettings = RestSiteExpansionRuntimeConfig.ResolveFun(player);
		bool flag = RestMenuState.IsBuriedGoldUsable(player);
		bool flag2 = RestMenuState.IsBuriedCardUsable(player);
		MainFile.Logger.Info($"[RestSiteExpansion] 休息处选项构建：player={player.NetId} 锻造(开关={goldForgeSettings.Enabled} 费用={goldForgeSettings.Cost} 无限={goldForgeSettings.Infinite}) 附魔(开关={goldEnchantSettings.Enabled} 生命消耗={goldEnchantSettings.HpCost} 无限={goldEnchantSettings.Infinite}) 掩埋金(开关={buriedGoldSettings.Enabled} 可用={flag} 单次存={buriedGoldSettings.Deposit} 取={buriedGoldSettings.Recover} 费={buriedGoldSettings.FeePercent}%) 掩埋牌(开关={buriedGoldSettings.CardEnabled} 可用={flag2}) 睡眠(打盹={sleepSettings.NapEnabled} 酣眠={sleepSettings.DeepSleepEnabled} 冥想={sleepSettings.MeditateEnabled} 熬夜={sleepSettings.StayUpLateEnabled} 休息菜单={RestMenuState.IsRestMenuActive(player)}) 狠造={extraForgeSettings.RoughEnabled} 瞎造={extraForgeSettings.BlindEnabled} 打菌子={funSettings.WhackFlyconidEnabled} 乱挖={funSettings.RecklessDigEnabled} 娱乐菜单={RestMenuState.IsFunMenuActive(player)} 锻造菜单={RestMenuState.IsForgeMenuActive(player)} 金币={player.Gold} 存款={BuriedGoldRuntime.GetAmount(player)} 构建前选项数={options.Count}", 1);
		bool flag3 = false;
		if (goldForgeSettings.Enabled)
		{
			options.Add((RestSiteOption)(object)new GoldForgeRestSiteOption(player));
			flag3 = true;
		}
		if (extraForgeSettings.RoughEnabled)
		{
			options.Add((RestSiteOption)(object)new RoughForgeRestSiteOption(player));
			flag3 = true;
		}
		if (extraForgeSettings.BlindEnabled)
		{
			options.Add((RestSiteOption)(object)new BlindForgeRestSiteOption(player));
			flag3 = true;
		}
		if (goldEnchantSettings.Enabled)
		{
			options.Add((RestSiteOption)(object)new GoldEnchantRestSiteOption(player));
			flag3 = true;
		}
		if (flag)
		{
			options.Add((RestSiteOption)(object)new BuryGoldRestSiteOption(player));
			options.Add((RestSiteOption)(object)new RecoverGoldRestSiteOption(player));
			flag3 = true;
		}
		if (flag2)
		{
			options.Add((RestSiteOption)(object)new BuryCardRestSiteOption(player));
			options.Add((RestSiteOption)(object)new RecoverCardRestSiteOption(player));
			flag3 = true;
		}
		if (sleepSettings.NapEnabled)
		{
			options.Add((RestSiteOption)(object)new NapRestSiteOption(player));
			flag3 = true;
		}
		if (sleepSettings.DeepSleepEnabled)
		{
			options.Add((RestSiteOption)(object)new DeepSleepRestSiteOption(player));
			flag3 = true;
		}
		if (sleepSettings.MeditateEnabled)
		{
			options.Add((RestSiteOption)(object)new MeditateRestSiteOption(player));
			flag3 = true;
		}
		if (sleepSettings.StayUpLateEnabled)
		{
			options.Add((RestSiteOption)(object)new StayUpLateRestSiteOption(player));
			flag3 = true;
		}
		if (funSettings.WhackFlyconidEnabled)
		{
			options.Add((RestSiteOption)(object)new WhackFlyconidRestSiteOption(player));
			flag3 = true;
		}
		if (funSettings.RecklessDigEnabled)
		{
			options.Add((RestSiteOption)(object)new RecklessDigRestSiteOption(player));
			flag3 = true;
		}
		foreach (RestSubmenu allMenu in RestMenuState.AllMenus)
		{
			if (RestMenuState.IsActive(player, allMenu))
			{
				options.Add((RestSiteOption)(object)RestMenuState.CreateEntryOption(player, allMenu));
				flag3 = true;
			}
		}
		if (RestMenuState.HasAnySubmenu(player))
		{
			options.Add((RestSiteOption)(object)RestMenuState.CreateBackOption(player));
			flag3 = true;
		}
		if (flag3)
		{
			MainFile.Logger.Info($"[RestSiteExpansion] 已添加选项（构建后选项数={options.Count}）：" + string.Join("/", options.Select((RestSiteOption o) => o.OptionId)), 1);
		}
		return flag3;
	}

	private static void ApplyDesiredOrder(ICollection<RestSiteOption> options)
	{
		if (options is IList<RestSiteOption> { Count: >1 } list)
		{
			List<(RestSiteOption, int, int)> list2 = new List<(RestSiteOption, int, int)>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				list2.Add((list[i], i, RankOf(list[i].OptionId)));
			}
			list2.Sort(((RestSiteOption Option, int Index, int Rank) a, (RestSiteOption Option, int Index, int Rank) b) => (a.Rank != b.Rank) ? a.Rank.CompareTo(b.Rank) : a.Index.CompareTo(b.Index));
			for (int j = 0; j < list2.Count; j++)
			{
				list[j] = list2[j].Item1;
			}
		}
	}

	internal static int RankOf(string optionId)
	{
		for (int i = 0; i < DesiredOrder.Length; i++)
		{
			if (DesiredOrder[i] == optionId)
			{
				return i;
			}
		}
		return DesiredOrder.Length;
	}
}
