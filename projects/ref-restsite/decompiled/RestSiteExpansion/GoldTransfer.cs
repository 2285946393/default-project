using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace RestSiteExpansion;

internal static class GoldTransfer
{
	internal static void CreditExact(Player player, int amount)
	{
		if (amount > 0)
		{
			player.Gold += amount;
			MapPointHistoryEntry currentMapPointHistoryEntry = player.RunState.CurrentMapPointHistoryEntry;
			PlayerMapPointHistoryEntry val = ((currentMapPointHistoryEntry != null) ? currentMapPointHistoryEntry.GetEntry(player.NetId) : null);
			if (val != null)
			{
				val.GoldGained += amount;
			}
			if (LocalContext.IsMe(player))
			{
				SfxCmd.Play((amount >= 100) ? "event:/sfx/ui/gold/gold_3" : ((amount > 30) ? "event:/sfx/ui/gold/gold_2" : "event:/sfx/ui/gold/gold_1"), 1f);
			}
		}
	}
}
