using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

namespace RestSiteExpansion;

internal static class SleepBuffRuntime
{
	private const string SlotKey = "sleep_buff";

	private static PlayerRunSavedData<SleepBuffState>? _slot;

	public static void Register()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		RunSavedDataStore obj = RunSavedDataStore.For("RestSiteExpansion");
		Func<SleepBuffState> obj2 = () => new SleepBuffState();
		RunSavedDataOptions val = new RunSavedDataOptions();
		val.set_WritePolicy((RunSavedDataWritePolicy)0);
		val.set_SyncLobbyOnChange(true);
		_slot = obj.RegisterPerPlayer<SleepBuffState>("sleep_buff", obj2, val);
	}

	public static SleepBuffState Read(Player player)
	{
		if (_slot != null)
		{
			IRunState runState = player.RunState;
			RunState val = (RunState)(object)((runState is RunState) ? runState : null);
			SleepBuffState result = default(SleepBuffState);
			if (val != null && _slot.TryGet(val, player.NetId, ref result))
			{
				return result;
			}
		}
		return new SleepBuffState();
	}

	public static void SetNap(Player player, bool value)
	{
		SleepBuffState sleepBuffState = Read(player);
		if (sleepBuffState.Nap != value)
		{
			sleepBuffState.Nap = value;
			Write(player, sleepBuffState);
		}
	}

	public static void SetDeepSleep(Player player, bool value)
	{
		SleepBuffState sleepBuffState = Read(player);
		if (sleepBuffState.DeepSleep != value)
		{
			sleepBuffState.DeepSleep = value;
			Write(player, sleepBuffState);
		}
	}

	public static void SetStayUpLate(Player player, bool value)
	{
		SleepBuffState sleepBuffState = Read(player);
		if (sleepBuffState.StayUpLate != value)
		{
			sleepBuffState.StayUpLate = value;
			Write(player, sleepBuffState);
		}
	}

	public static void SetStayUpLateActive(Player player, bool value)
	{
		SleepBuffState sleepBuffState = Read(player);
		if (sleepBuffState.StayUpLateActive != value)
		{
			sleepBuffState.StayUpLateActive = value;
			Write(player, sleepBuffState);
		}
	}

	public static void SetRecklessDig(Player player, bool value)
	{
		SleepBuffState sleepBuffState = Read(player);
		if (sleepBuffState.RecklessDig != value)
		{
			sleepBuffState.RecklessDig = value;
			Write(player, sleepBuffState);
		}
	}

	private static void Write(Player player, SleepBuffState state)
	{
		if (_slot != null)
		{
			IRunState runState = player.RunState;
			RunState val = (RunState)(object)((runState is RunState) ? runState : null);
			if (val != null)
			{
				_slot.Set(val, player.NetId, state);
			}
		}
	}
}
