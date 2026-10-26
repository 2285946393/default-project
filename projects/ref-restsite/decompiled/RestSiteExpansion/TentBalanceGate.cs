using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace RestSiteExpansion;

internal static class TentBalanceGate
{
	private sealed record BalanceGroup(string Name, IReadOnlyList<string> OptionIds, string BlockedLocKey);

	private static readonly BalanceGroup[] Groups = new BalanceGroup[4]
	{
		new BalanceGroup("休息", RestMenuState.RestGroupMemberIds, "REST_SITE_EXPANSION_RESTSITE_SLEEP_ALREADY_CHOSEN"),
		new BalanceGroup("锻造", RestMenuState.ForgeGroupMemberIds, "REST_SITE_EXPANSION_RESTSITE_FORGE_ALREADY_CHOSEN"),
		new BalanceGroup("埋藏卡牌", RestMenuState.BuriedCardMemberIds, "REST_SITE_EXPANSION_RESTSITE_BURIAL_ALREADY_CHOSEN"),
		new BalanceGroup("娱乐", RestMenuState.FunGroupMemberIds, "REST_SITE_EXPANSION_RESTSITE_FUN_ALREADY_CHOSEN")
	};

	private static BalanceGroup? GroupOf(string optionId)
	{
		string optionId2 = optionId;
		return Groups.FirstOrDefault((BalanceGroup g) => g.OptionIds.Contains(optionId2));
	}

	private static bool HasChosenInGroup(Player player, BalanceGroup group)
	{
		IRunState runState = player.RunState;
		RunState val = (RunState)(object)((runState is RunState) ? runState : null);
		if (val == null)
		{
			return false;
		}
		MapPointHistoryEntry currentMapPointHistoryEntry = val.CurrentMapPointHistoryEntry;
		PlayerMapPointHistoryEntry val2 = ((currentMapPointHistoryEntry != null) ? currentMapPointHistoryEntry.GetEntry(player.NetId) : null);
		if (((val2 != null) ? val2.RestSiteChoices : null) == null)
		{
			return false;
		}
		foreach (string optionId in group.OptionIds)
		{
			if (val2.RestSiteChoices.Contains(optionId))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool IsBlockedFor(Player player, string optionId)
	{
		BalanceGroup balanceGroup = GroupOf(optionId);
		return (object)balanceGroup != null && RestSiteExpansionRuntimeConfig.ResolveSleep(player).Exclusive && HasChosenInGroup(player, balanceGroup);
	}

	internal static string BlockedLocKeyFor(string optionId)
	{
		return GroupOf(optionId)?.BlockedLocKey ?? string.Empty;
	}

	internal static string GroupNameFor(string optionId)
	{
		return GroupOf(optionId)?.Name ?? "未分组";
	}

	internal static string DescribeChoices(Player player)
	{
		IRunState runState = player.RunState;
		RunState val = (RunState)(object)((runState is RunState) ? runState : null);
		object result;
		if (val != null)
		{
			MapPointHistoryEntry currentMapPointHistoryEntry = val.CurrentMapPointHistoryEntry;
			PlayerMapPointHistoryEntry val2 = ((currentMapPointHistoryEntry != null) ? currentMapPointHistoryEntry.GetEntry(player.NetId) : null);
			if (val2 != null)
			{
				result = string.Join("/", val2.RestSiteChoices);
				goto IL_0042;
			}
		}
		result = "(无)";
		goto IL_0042;
		IL_0042:
		return (string)result;
	}
}
