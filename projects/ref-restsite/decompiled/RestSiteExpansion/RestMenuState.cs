using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace RestSiteExpansion;

internal static class RestMenuState
{
	private sealed record SubmenuDef(RestSubmenu Menu, string EntryOptionId, string EntryIconResPath, string[] MemberOptionIds, string HintLocKey);

	internal const string BackOptionId = "REST_MENU_BACK";

	internal const string BackIconResPath = "res://art/images/rest_site/rest_back.png";

	private static readonly SubmenuDef[] Definitions = new SubmenuDef[4]
	{
		new SubmenuDef(RestSubmenu.Rest, "REST_MENU", "res://images/ui/rest_site/option_heal.png", new string[5] { "NAP", "HEAL", "DEEP_SLEEP", "MEDITATE", "STAY_UP_LATE" }, "REST_SITE_EXPANSION_RESTSITE_REST_MENU_HINT"),
		new SubmenuDef(RestSubmenu.Forge, "FORGE_MENU", "res://images/ui/rest_site/option_smith.png", new string[4] { "SMITH", "GOLD_FORGE", "ROUGH_FORGE", "BLIND_FORGE" }, "REST_SITE_EXPANSION_RESTSITE_FORGE_MENU_HINT"),
		new SubmenuDef(RestSubmenu.Gold, "GOLD_MENU", "res://art/images/rest_site/gold_menu.png", new string[4] { "BURY_GOLD", "RECOVER_GOLD", "BURY_CARD", "RECOVER_CARD" }, "REST_SITE_EXPANSION_RESTSITE_GOLD_MENU_HINT"),
		new SubmenuDef(RestSubmenu.Fun, "FUN_MENU", "res://art/images/rest_site/fun_menu.png", new string[2] { "WHACK_FLYCONID", "RECKLESS_DIG" }, "REST_SITE_EXPANSION_RESTSITE_FUN_MENU_HINT")
	};

	private const string MetaPrefix = "rse_submenu_";

	internal static IEnumerable<RestSubmenu> AllMenus => Definitions.Select((SubmenuDef d) => d.Menu);

	internal static IReadOnlyList<string> RestGroupMemberIds => Definitions.First((SubmenuDef d) => d.Menu == RestSubmenu.Rest).MemberOptionIds;

	internal static IReadOnlyList<string> ForgeGroupMemberIds => Definitions.First((SubmenuDef d) => d.Menu == RestSubmenu.Forge).MemberOptionIds;

	internal static IReadOnlyList<string> BuriedCardMemberIds => Definitions.First((SubmenuDef d) => d.Menu == RestSubmenu.Gold).MemberOptionIds.Where((string id) => (id == "BURY_CARD" || id == "RECOVER_CARD") ? true : false).ToArray();

	internal static IReadOnlyList<string> FunGroupMemberIds => Definitions.First((SubmenuDef d) => d.Menu == RestSubmenu.Fun).MemberOptionIds;

	internal static string MenuOptionIdOf(RestSubmenu menu)
	{
		return Definitions.FirstOrDefault((SubmenuDef d) => d.Menu == menu)?.EntryOptionId ?? string.Empty;
	}

	internal static RestSubmenu MenuOfEntryOption(string optionId)
	{
		string optionId2 = optionId;
		return Definitions.FirstOrDefault((SubmenuDef d) => d.EntryOptionId == optionId2)?.Menu ?? RestSubmenu.None;
	}

	internal static bool IsMemberOf(string optionId, RestSubmenu menu)
	{
		return Definitions.FirstOrDefault((SubmenuDef d) => d.Menu == menu)?.MemberOptionIds.Contains(optionId) ?? false;
	}

	internal static string HintLocKeyOf(RestSubmenu menu)
	{
		return Definitions.FirstOrDefault((SubmenuDef d) => d.Menu == menu)?.HintLocKey ?? string.Empty;
	}

	internal static SubmenuEntryRestSiteOption CreateEntryOption(Player player, RestSubmenu menu)
	{
		SubmenuDef submenuDef = Definitions.First((SubmenuDef d) => d.Menu == menu);
		return new SubmenuEntryRestSiteOption(player, menu, submenuDef.EntryOptionId, submenuDef.EntryIconResPath);
	}

	internal static SubmenuEntryRestSiteOption CreateBackOption(Player player)
	{
		return new SubmenuEntryRestSiteOption(player, RestSubmenu.None, "REST_MENU_BACK", "res://art/images/rest_site/rest_back.png");
	}

	internal static void ValidateOrWarn(IReadOnlyList<string> desiredOrder)
	{
		if (desiredOrder == null || desiredOrder.Count == 0)
		{
			MainFile.Logger.Warn("[RestMenu] 自检：排序表为空，子菜单入口会被排到列表最后。", 1);
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(desiredOrder);
		SubmenuDef[] definitions = Definitions;
		foreach (SubmenuDef submenuDef in definitions)
		{
			if (!hashSet.Contains(submenuDef.EntryOptionId))
			{
				MainFile.Logger.Warn($"[RestMenu] 自检：排序表缺少子菜单入口 {submenuDef.EntryOptionId}（{Describe(submenuDef.Menu)}）→ 它会排在列表最后。", 1);
			}
			string[] memberOptionIds = submenuDef.MemberOptionIds;
			foreach (string text in memberOptionIds)
			{
				if (!hashSet.Contains(text))
				{
					MainFile.Logger.Warn($"[RestMenu] 自检：排序表缺少 {Describe(submenuDef.Menu)}菜单成员 {text} → 它会排在列表最后。", 1);
				}
			}
		}
		Dictionary<string, RestSubmenu> dictionary = new Dictionary<string, RestSubmenu>();
		SubmenuDef[] definitions2 = Definitions;
		foreach (SubmenuDef submenuDef2 in definitions2)
		{
			string[] memberOptionIds2 = submenuDef2.MemberOptionIds;
			foreach (string text2 in memberOptionIds2)
			{
				if (dictionary.TryGetValue(text2, out var value))
				{
					MainFile.Logger.Warn($"[RestMenu] 自检：选项 {text2} 同时属于 {Describe(value)} 与 {Describe(submenuDef2.Menu)} 两组 → 可见性判定会自相矛盾，请修正定义表。", 1);
				}
				else
				{
					dictionary[text2] = submenuDef2.Menu;
				}
			}
		}
	}

	internal static bool IsRestMenuActive(Player player)
	{
		return RestSiteExpansionRuntimeConfig.ResolveSleep(player).AnyEnabled;
	}

	internal static bool IsForgeMenuActive(Player player)
	{
		return RestSiteExpansionRuntimeConfig.ResolveGoldForge(player).Enabled || RestSiteExpansionRuntimeConfig.ResolveExtraForge(player).AnyEnabled;
	}

	internal static bool IsBuriedGoldUsable(Player player)
	{
		BuriedGoldSettings buriedGoldSettings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(player);
		return buriedGoldSettings.Enabled && IsMultiplayerAllowed(player, buriedGoldSettings.MultiplayerEnabled);
	}

	internal static bool IsBuriedCardUsable(Player player)
	{
		BuriedGoldSettings buriedGoldSettings = RestSiteExpansionRuntimeConfig.ResolveBuriedGold(player);
		return buriedGoldSettings.CardEnabled && IsMultiplayerAllowed(player, buriedGoldSettings.MultiplayerEnabled);
	}

	private static bool IsMultiplayerAllowed(Player player, bool multiplayerEnabled)
	{
		return ((IPlayerCollection)player.RunState).Players.Count == 1 || multiplayerEnabled;
	}

	internal static bool IsGoldMenuActive(Player player)
	{
		return IsBuriedGoldUsable(player) || IsBuriedCardUsable(player);
	}

	internal static bool IsFunMenuActive(Player player)
	{
		return RestSiteExpansionRuntimeConfig.ResolveFun(player).AnyEnabled;
	}

	internal static bool IsActive(Player player, RestSubmenu menu)
	{
		if (1 == 0)
		{
		}
		bool result = menu switch
		{
			RestSubmenu.Rest => IsRestMenuActive(player), 
			RestSubmenu.Forge => IsForgeMenuActive(player), 
			RestSubmenu.Gold => IsGoldMenuActive(player), 
			RestSubmenu.Fun => IsFunMenuActive(player), 
			_ => false, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	internal static bool HasAnySubmenu(Player player)
	{
		Player player2 = player;
		return AllMenus.Any((RestSubmenu menu) => IsActive(player2, menu));
	}

	internal static RestSubmenu ActiveMask(IEnumerable<string> optionIds)
	{
		ICollection<string> collection = (optionIds as ICollection<string>) ?? optionIds.ToList();
		RestSubmenu restSubmenu = RestSubmenu.None;
		SubmenuDef[] definitions = Definitions;
		foreach (SubmenuDef submenuDef in definitions)
		{
			if (collection.Contains(submenuDef.EntryOptionId))
			{
				restSubmenu |= submenuDef.Menu;
			}
		}
		return restSubmenu;
	}

	internal static IEnumerable<RestSubmenu> MenusOf(RestSubmenu mask)
	{
		return from d in Definitions
			where (mask & d.Menu) != 0
			select d.Menu;
	}

	internal static bool IsVisible(string optionId, RestSubmenu open, RestSubmenu mask)
	{
		string optionId2 = optionId;
		if (mask == RestSubmenu.None)
		{
			return true;
		}
		if (open != 0 && (mask & open) != 0)
		{
			return optionId2 == "REST_MENU_BACK" || IsMemberOf(optionId2, open);
		}
		if (optionId2 == "REST_MENU_BACK")
		{
			return false;
		}
		return MenusOf(mask).All((RestSubmenu menu) => !IsMemberOf(optionId2, menu));
	}

	internal static RestSubmenu GetOpenSubmenu(NRestSiteRoom? room, ulong netId)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (room == null || !GodotObject.IsInstanceValid((GodotObject)(object)room))
		{
			return RestSubmenu.None;
		}
		string text = "rse_submenu_" + netId;
		int result;
		if (!((GodotObject)room).HasMeta(StringName.op_Implicit(text)))
		{
			result = 0;
		}
		else
		{
			StringName obj = StringName.op_Implicit(text);
			Variant val = default(Variant);
			val = ((GodotObject)room).GetMeta(obj, val);
			result = ((Variant)(ref val)).AsInt32();
		}
		return (RestSubmenu)result;
	}

	internal static void OpenSubmenu(Player player, RestSubmenu menu)
	{
		SetOpen(player, menu);
	}

	internal static void CloseSubmenu(Player player)
	{
		SetOpen(player, RestSubmenu.None);
	}

	private static void SetOpen(Player player, RestSubmenu menu)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		NRestSiteRoom instance = NRestSiteRoom.Instance;
		if (instance == null || !GodotObject.IsInstanceValid((GodotObject)(object)instance))
		{
			MainFile.Logger.Warn($"[RestMenu] 切换子菜单时找不到休息处房间节点（player={player.NetId} → {menu}），本机显示不变。", 1);
		}
		else
		{
			((GodotObject)instance).SetMeta(StringName.op_Implicit("rse_submenu_" + player.NetId), Variant.op_Implicit((int)menu));
			MainFile.Logger.Info($"[RestMenu] 子菜单切换：player={player.NetId} → {Describe(menu)}", 1);
			RestSiteSubmenuFilter.RefreshForSubmenu(instance);
		}
	}

	internal static string Describe(RestSubmenu menu)
	{
		if (1 == 0)
		{
		}
		string result = menu switch
		{
			RestSubmenu.Rest => "休息", 
			RestSubmenu.Forge => "锻造", 
			RestSubmenu.Gold => "金币", 
			RestSubmenu.Fun => "娱乐", 
			_ => "主层", 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
