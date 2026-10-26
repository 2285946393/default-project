using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Models;

namespace RestSiteExpansion;

public sealed class SleepBuffCombatSingleton : HookedSingletonModel
{
	public SleepBuffCombatSingleton()
		: base((HookType)1)
	{
	}

	public override async Task BeforeCombatStart()
	{
		IRunState runState = ((HookedSingletonModel)this).CurrentRunState;
		if (runState == null)
		{
			return;
		}
		foreach (Player player in ((IPlayerCollection)runState).Players)
		{
			SleepBuffState state = SleepBuffRuntime.Read(player);
			if (state.DeepSleep)
			{
				SleepBuffRuntime.SetDeepSleep(player, value: false);
				await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player.Creature, -1m, (Creature)null, (CardModel)null, false);
				await PowerCmd.Apply<DexterityPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player.Creature, -1m, (Creature)null, (CardModel)null, false);
				MainFile.Logger.Info($"[Sleep] 酣眠生效：player={player.NetId} 本场战斗 -1 力量 / -1 敏捷", 1);
			}
			if (state.StayUpLate)
			{
				await ApplyStayUpLate(player);
			}
		}
	}

	private async Task ApplyStayUpLate(Player player)
	{
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			MainFile.Logger.Warn($"[StayUpLate] 找不到战斗状态，跳过熬夜效果：player={player.NetId}", 1);
			return;
		}
		SleepBuffRuntime.SetStayUpLate(player, value: false);
		SleepBuffRuntime.SetStayUpLateActive(player, value: true);
		for (int i = 0; i < 4; i++)
		{
			PoorSleep card = combatState.CreateCard<PoorSleep>(player);
			await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)card, (PileType)2, player, (CardPilePosition)1);
		}
		MainFile.Logger.Info($"[StayUpLate] 熬夜生效：player={player.NetId} 手牌 +{4} 张睡眠不佳、本场最大能量 +{1}", 1);
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return SleepBuffRuntime.Read(player).StayUpLateActive ? (amount + 1m) : amount;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		IRunState currentRunState = ((HookedSingletonModel)this).CurrentRunState;
		if (currentRunState == null)
		{
			return Task.CompletedTask;
		}
		int num = 0;
		foreach (Player player in ((IPlayerCollection)currentRunState).Players)
		{
			if (SleepBuffRuntime.Read(player).StayUpLateActive)
			{
				SleepBuffRuntime.SetStayUpLateActive(player, value: false);
				num++;
			}
		}
		if (num > 0)
		{
			MainFile.Logger.Info($"[StayUpLate] 战斗结束：清除本场最大能量标记（{num} 人）", 1);
		}
		return Task.CompletedTask;
	}

	public override async Task AfterEnergyReset(Player player)
	{
		if (SleepBuffRuntime.Read(player).Nap)
		{
			await PlayerCmd.GainEnergy(1m, player);
			MainFile.Logger.Info($"[Sleep] 打盹能量生效：player={player.NetId} +1 能量（抽牌待本回合抽牌时生效）", 1);
		}
		if (SleepBuffRuntime.Read(player).RecklessDig)
		{
			try
			{
				SleepBuffRuntime.SetRecklessDig(player, value: false);
				await PlayerCmd.SetEnergy(0m, player);
				await AddDebris(player);
				MainFile.Logger.Info($"[RecklessDig] 下战惩戒生效：player={player.NetId} 第一回合能量已清空、碎屑 ×{6}（弃牌堆 {3} + 抽牌堆 {3}）", 1);
			}
			catch (Exception ex)
			{
				MainFile.Logger.Warn($"[RecklessDig] 惩戒生效失败（已忽略，不影响战斗）：player={player.NetId} {ex}", 1);
			}
		}
	}

	private static async Task AddDebris(Player player)
	{
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			MainFile.Logger.Warn($"[RecklessDig] 找不到战斗状态，跳过碎屑：player={player.NetId}", 1);
			return;
		}
		for (int j = 0; j < 3; j++)
		{
			await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)combatState.CreateCard<Debris>(player), (PileType)3, player, (CardPilePosition)1);
		}
		for (int i = 0; i < 3; i++)
		{
			await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)combatState.CreateCard<Debris>(player), (PileType)1, player, (CardPilePosition)3);
		}
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (!SleepBuffRuntime.Read(player).Nap)
		{
			return count;
		}
		SleepBuffRuntime.SetNap(player, value: false);
		MainFile.Logger.Info($"[Sleep] 打盹抽牌生效：player={player.NetId} +2 张（并清除待生效标记）", 1);
		return count + 2m;
	}
}
