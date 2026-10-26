using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace RestSiteExpansion;

public static class RestSiteExpansionRuntimeConfig
{
	private const string SlotKey = "gold_forge_snapshot";

	private static RunSavedData<RestSiteExpansionSnapshot>? _slot;

	private static bool _lobbyStagingInProgress;

	private static bool _warnedMissingSnapshot;

	public static void Register()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		RunSavedDataStore obj = RunSavedDataStore.For("RestSiteExpansion");
		Func<RestSiteExpansionSnapshot> obj2 = () => new RestSiteExpansionSnapshot();
		RunSavedDataOptions val = new RunSavedDataOptions();
		val.set_WritePolicy((RunSavedDataWritePolicy)1);
		val.set_SyncLobbyOnChange(true);
		_slot = obj.Register<RestSiteExpansionSnapshot>("gold_forge_snapshot", obj2, val);
		RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>((Action<RunSavedDataLobbyStagingEvent>)OnLobbyStaging, true);
		BuriedGoldRuntime.Register();
		BuriedCardRuntime.Register();
		SleepBuffRuntime.Register();
		RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>((Action<RunSavedDataPreparingEvent>)OnRunDataPreparing, true);
	}

	public static bool IsModEnabled(Player player)
	{
		if (TryGetMultiplayerSnapshot(player, out RestSiteExpansionSnapshot snapshot))
		{
			return snapshot?.Enabled ?? false;
		}
		return ModConfigPage.ReadConfig().EnableRestSiteExpansion;
	}

	public static GoldForgeSettings ResolveGoldForge(Player player)
	{
		if (TryGetMultiplayerSnapshot(player, out RestSiteExpansionSnapshot snapshot))
		{
			if (snapshot == null)
			{
				return GoldForgeSettings.Disabled;
			}
			return new GoldForgeSettings(Enabled: true, Clamp(snapshot.Cost, 0, 999), Clamp(snapshot.FlatIncrement, 0, 999), snapshot.Infinite);
		}
		ModConfigData modConfigData = ModConfigPage.ReadConfig();
		return new GoldForgeSettings(modConfigData.EnableRestSiteExpansion, ModConfigPage.GoldForgeCost, ModConfigPage.GoldForgeFlatIncrement, modConfigData.InfiniteGoldForge);
	}

	public static GoldEnchantSettings ResolveGoldEnchant(Player player)
	{
		if (TryGetMultiplayerSnapshot(player, out RestSiteExpansionSnapshot snapshot))
		{
			if (snapshot == null)
			{
				return GoldEnchantSettings.Disabled;
			}
			return new GoldEnchantSettings(Enabled: true, Clamp(snapshot.EnchantHpCost, 0, 99), Clamp(snapshot.EnchantHpIncrement, 0, 99), snapshot.EnchantInfinite);
		}
		ModConfigData modConfigData = ModConfigPage.ReadConfig();
		return new GoldEnchantSettings(modConfigData.EnableGoldEnchant, ModConfigPage.GoldEnchantHpCost, ModConfigPage.GoldEnchantHpIncrement, modConfigData.InfiniteGoldEnchant);
	}

	public static BuriedGoldSettings ResolveBuriedGold(Player player)
	{
		if (TryGetMultiplayerSnapshot(player, out RestSiteExpansionSnapshot snapshot))
		{
			if (snapshot == null)
			{
				return BuriedGoldSettings.Disabled;
			}
			return new BuriedGoldSettings(snapshot.BuryEnabled, snapshot.BuryMultiplayerEnabled, Clamp(snapshot.BuryDeposit, 1, 999), Clamp(snapshot.BuryRecover, 1, 999), Clamp(snapshot.BuryFeePercent, 0, 95), snapshot.BuryCardEnabled);
		}
		ModConfigData modConfigData = ModConfigPage.ReadConfig();
		return new BuriedGoldSettings(modConfigData.EnableBuriedGold, modConfigData.BuriedGoldMultiplayer, ModConfigPage.BuriedGoldDeposit, ModConfigPage.BuriedGoldRecover, ModConfigPage.BuriedGoldFee, modConfigData.EnableBuryCard);
	}

	public static SleepSettings ResolveSleep(Player player)
	{
		if (TryGetMultiplayerSnapshot(player, out RestSiteExpansionSnapshot snapshot))
		{
			if (snapshot == null)
			{
				return SleepSettings.Disabled;
			}
			return new SleepSettings(snapshot.NapEnabled, snapshot.DeepSleepEnabled, snapshot.SleepExclusive, snapshot.MeditateEnabled, snapshot.StayUpLateEnabled);
		}
		ModConfigData modConfigData = ModConfigPage.ReadConfig();
		return new SleepSettings(modConfigData.EnableNap, modConfigData.EnableDeepSleep, modConfigData.SleepExclusive, modConfigData.EnableMeditate, modConfigData.EnableStayUpLate);
	}

	public static ExtraForgeSettings ResolveExtraForge(Player player)
	{
		if (TryGetMultiplayerSnapshot(player, out RestSiteExpansionSnapshot snapshot))
		{
			if (snapshot == null)
			{
				return ExtraForgeSettings.Disabled;
			}
			return new ExtraForgeSettings(snapshot.RoughForgeEnabled, snapshot.BlindForgeEnabled);
		}
		ModConfigData modConfigData = ModConfigPage.ReadConfig();
		return new ExtraForgeSettings(modConfigData.EnableRoughForge, modConfigData.EnableBlindForge);
	}

	public static FunSettings ResolveFun(Player player)
	{
		if (TryGetMultiplayerSnapshot(player, out RestSiteExpansionSnapshot snapshot))
		{
			if (snapshot == null)
			{
				return FunSettings.Disabled;
			}
			return new FunSettings(snapshot.WhackFlyconidEnabled, snapshot.RecklessDigEnabled);
		}
		ModConfigData modConfigData = ModConfigPage.ReadConfig();
		return new FunSettings(modConfigData.EnableWhackFlyconid, modConfigData.EnableRecklessDig);
	}

	private static void OnRunDataPreparing(RunSavedDataPreparingEvent evt)
	{
		if (!evt.IsMultiplayer && BuriedGoldRuntime.Slot != null)
		{
			Player val = evt.RunState.Players.FirstOrDefault();
			if (val != null)
			{
				BuriedGoldRuntime.Slot.Set(evt.RunState, val.NetId, new BuriedGoldState
				{
					Amount = BuriedGoldRuntime.ReadPersistentAmount()
				});
				BuriedCardRuntime.Slot?.Set(evt.RunState, val.NetId, new BuriedCardState
				{
					Card = BuriedCardRuntime.EnsureDefaultCard()
				});
			}
		}
	}

	private static bool TryGetMultiplayerSnapshot(Player player, out RestSiteExpansionSnapshot? snapshot)
	{
		snapshot = null;
		IRunState runState = player.RunState;
		RunState val = (RunState)(object)((runState is RunState) ? runState : null);
		if (val != null)
		{
			IReadOnlyList<Player> players = val.Players;
			if (players != null && players.Count > 1)
			{
				RestSiteExpansionSnapshot restSiteExpansionSnapshot = default(RestSiteExpansionSnapshot);
				if (_slot != null && _slot.TryGet(val, ref restSiteExpansionSnapshot))
				{
					snapshot = restSiteExpansionSnapshot;
				}
				else if (!_warnedMissingSnapshot)
				{
					_warnedMissingSnapshot = true;
					MainFile.Logger.Warn("[RestSiteExpansion] 联机跑局缺少休息处扩展快照，本次按禁用处理（两端一致，不分歧）。", 1);
				}
				return true;
			}
		}
		return false;
	}

	private static int Clamp(int value, int min, int max)
	{
		return Math.Clamp(value, min, max);
	}

	private static void OnLobbyStaging(RunSavedDataLobbyStagingEvent evt)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		if ((int)evt.Reason != 3)
		{
			return;
		}
		if (_lobbyStagingInProgress)
		{
			MainFile.Logger.Warn("[RestSiteExpansion] 检测到大厅暂存写入重入，已跳过本次写入以避免无限递归。", 1);
			return;
		}
		_lobbyStagingInProgress = true;
		try
		{
			if (!evt.IsMultiplayer || evt.IsHost)
			{
				WriteConfigSnapshot(evt.Lobby);
			}
			if (evt.IsMultiplayer)
			{
				BuriedGoldRuntime.Slot?.Lobby.Set(evt.Lobby, evt.Lobby.NetService.NetId, new BuriedGoldState
				{
					Amount = BuriedGoldRuntime.ReadPersistentAmount()
				});
			}
			if (evt.IsMultiplayer)
			{
				BuriedCardRuntime.Slot?.Lobby.Set(evt.Lobby, evt.Lobby.NetService.NetId, new BuriedCardState
				{
					Card = BuriedCardRuntime.EnsureDefaultCard()
				});
			}
		}
		finally
		{
			_lobbyStagingInProgress = false;
		}
	}

	private static void WriteConfigSnapshot(StartRunLobby lobby)
	{
		if (_slot != null)
		{
			ModConfigData modConfigData = ModConfigPage.ReadConfig();
			if (!modConfigData.EnableRestSiteExpansion)
			{
				_slot.Lobby.Remove(lobby);
				return;
			}
			_slot.Lobby.Set(lobby, new RestSiteExpansionSnapshot
			{
				Enabled = true,
				Cost = ModConfigPage.GoldForgeCost,
				FlatIncrement = ModConfigPage.GoldForgeFlatIncrement,
				Infinite = modConfigData.InfiniteGoldForge,
				EnchantEnabled = modConfigData.EnableGoldEnchant,
				EnchantHpCost = ModConfigPage.GoldEnchantHpCost,
				EnchantHpIncrement = ModConfigPage.GoldEnchantHpIncrement,
				EnchantInfinite = modConfigData.InfiniteGoldEnchant,
				BuryEnabled = modConfigData.EnableBuriedGold,
				BuryCardEnabled = modConfigData.EnableBuryCard,
				BuryMultiplayerEnabled = modConfigData.BuriedGoldMultiplayer,
				BuryDeposit = ModConfigPage.BuriedGoldDeposit,
				BuryRecover = ModConfigPage.BuriedGoldRecover,
				BuryFeePercent = ModConfigPage.BuriedGoldFee,
				NapEnabled = modConfigData.EnableNap,
				DeepSleepEnabled = modConfigData.EnableDeepSleep,
				SleepExclusive = modConfigData.SleepExclusive,
				MeditateEnabled = modConfigData.EnableMeditate,
				StayUpLateEnabled = modConfigData.EnableStayUpLate,
				RoughForgeEnabled = modConfigData.EnableRoughForge,
				BlindForgeEnabled = modConfigData.EnableBlindForge,
				WhackFlyconidEnabled = modConfigData.EnableWhackFlyconid,
				RecklessDigEnabled = modConfigData.EnableRecklessDig
			});
		}
	}
}
